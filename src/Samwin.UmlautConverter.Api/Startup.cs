using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Samwin.UmlautConverter.Api.Services;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using OpenTelemetry.Metrics;
using Samwin.UmlautConverter.Api.Settings;

namespace Samwin.UmlautConverter.Api
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // ConfigureServices method
        public void ConfigureServices(IServiceCollection services)
        {

            var endpoint = Environment.GetEnvironmentVariable("GRAFANA_OTLP_ENDPOINT");
            var instanceId = Environment.GetEnvironmentVariable("GRAFANA_OTLP_INSTANCE_ID");
            var apiKey = Environment.GetEnvironmentVariable("GRAFANA_OTLP_ACCESS_TOKEN");

            var authHeader = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{instanceId}:{apiKey}"));

            services.AddControllers();
            services.AddDirectoryBrowser();
            services.AddScoped<ICreateTokenService, TokenGeneratorService>();
            services.AddSingleton<IActivityService, ActivityService>();
            services.AddSingleton<MetricsService>();

            services.AddOpenTelemetry()
                .ConfigureResource(r => r.AddService("samwin-umlaut-converter-api"))

                .WithTracing(tracing => tracing
                    .AddSource("samwin-umlaut-converter-api")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri($"{endpoint}/v1/traces");
                        o.Headers = $"Authorization=Basic {authHeader}";
                        o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;

                        Console.WriteLine($"Tracing OTLP Endpoint: {o.Endpoint} Protocol: {o.Protocol}");
                        Console.WriteLine($"Tracing OTLP Header: {o.Headers}");
                    })
#if DEBUG
                    .AddConsoleExporter()
#endif
                    )

                .WithMetrics(metrics => metrics
                    .AddMeter(MetricsService.MeterName, MetricsService.MeterDescription)
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri($"{endpoint}/v1/metrics");
                        o.Headers = $"Authorization=Basic {authHeader}";
                        o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                        Console.WriteLine($"Metrics OTLP Endpoint: {o.Endpoint} Protocol: {o.Protocol}");
                        Console.WriteLine($"Metrics OTLP Header: {o.Headers}");
                    })
#if DEBUG
                    .AddConsoleExporter()
#endif
                    );

            // This registers the JwtSettings class, binds it to the "JwtSettings" section of your configuration,
            // and enables validation based on the data annotations in the JwtSettings class.
            services.AddOptions<JwtSettings>()
                .Bind(Configuration.GetSection(JwtSettings.SectionName))
                .ValidateDataAnnotations();

            // JWT authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // We retrieve the strongly-typed settings from the configuration.
                // The '!' null-forgiving operator is safe here because ValidateDataAnnotations()
                // will throw an exception on startup if the settings are not configured correctly.
                var jwtSettings = Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.TokenIssuer,
                    ValidAudience = jwtSettings.TokenAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.TokenKey))
                };
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine("Authentication failed: " + context.Exception.Message);
                        return Task.CompletedTask;
                    }
                };
            });

            // Swagger with JWT support
            services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header
                });

                c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            });
        }

        // Configure middleware pipeline
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();

            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }

            var fileProvider = new PhysicalFileProvider(logPath);
            var requestPath = "/logs";

            app.UseDirectoryBrowser(new DirectoryBrowserOptions
            {
                FileProvider = fileProvider,
                RequestPath = requestPath
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = fileProvider,
                RequestPath = requestPath,
                ServeUnknownFileTypes = true,
                DefaultContentType = "text/plain"
            });

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}