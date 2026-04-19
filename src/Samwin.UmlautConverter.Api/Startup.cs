using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Samwin.UmlautConverter.Api.Services.Jwt;
using Samwin.UmlautConverter.Api.Services.Messaging;
using Samwin.UmlautConverter.Api.Services.Telemetry;
using Samwin.UmlautConverter.Api.Services.UmlautConversion;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using OpenTelemetry.Metrics;
using Samwin.UmlautConverter.Api.Settings;
using Samwin.UmlautConverterLib.Step2;
using Samwin.UmlautConverterLib.Step3;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Threading.RateLimiting;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.HttpOverrides;
using Samwin.UmlautConverter.Api.ResponseManagement;

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

            services.AddControllers(options =>
            {
                options.Filters.Add<ApiResponseFilter>();
            });
            
            services.AddDirectoryBrowser();
            services.AddHttpContextAccessor();
            services.AddTransient<ICreateTokenService, TokenGeneratorService>();
            services.AddSingleton<IActivityService, ActivityService>();
            services.AddSingleton<MetricsService>();
            services.AddSingleton<IMessageBusClient, MessageBusClient>();
            services.AddTransient<IVariationGenerator, BranchingVariationBufferGenerator>();
            services.AddTransient<ISqlQueryGenerator<SqlQuery>, ParameterizedSqlGenerator>();
            services.AddTransient<IConvertUmlaut, UmlautConversionService>();

            services.AddHostedService<QueueConsumerService>();

            services.AddMemoryCache();
            services.AddOpenTelemetry()
                .ConfigureResource(r => r.AddService("samwin-umlaut-converter-api"))

                .WithTracing(tracing => tracing
                    .AddSource("samwin-umlaut-converter-api")
                    .AddAspNetCoreInstrumentation()
                    .AddRabbitMQInstrumentation()
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
                    // .AddConsoleExporter()
#endif
                    )

                .WithMetrics(metrics => metrics
                    .AddMeter(MetricsService.MeterName, MetricsService.MeterDescription)
                    .ConfigureResource(resource =>
                                {
                                    resource.AddService(
                                        serviceName: "samwin-umlaut-converter-api",
                                        serviceVersion: "1.0.0")
                                        .AddAttributes(new List<KeyValuePair<string, object>>
                                        {
                                            // This will show up as a tag/label in your metrics
                                            new("server-name", Environment.MachineName),
                                            // Often used in demos to show the physical or virtual host
                                            new("host-name", Environment.MachineName)
                                        });
                                })
                    .AddAspNetCoreInstrumentation()
                    .AddMeter("RabbitMQ.Client")
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
                    // .AddConsoleExporter()
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

            services.AddRateLimiter(options =>
            {
                var retryAfter = TimeSpan.FromSeconds(10);
                // Return 429 immediately when limit is exceeded
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    var logger = httpContext.RequestServices.GetRequiredService<ILogger<Startup>>();

                    var clientId =
                        httpContext.User?.Identity?.IsAuthenticated == true
                            ? httpContext.User.Identity!.Name!
                            : (httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                               ?? httpContext.Connection.RemoteIpAddress?.ToString()
                               ?? "unknown");

                    logger.LogInformation("ClientId: {ClientId}", clientId);

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: clientId,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,                 // max 10 requests
                            Window = retryAfter, // per 10 seconds
                            QueueLimit = 0,                   // ❌ no queuing
                            AutoReplenishment = true
                        });
                });
                options.OnRejected = async (httpContext, token) =>
                {
                    var traceId = httpContext.HttpContext.TraceIdentifier;
                    var logger = httpContext.HttpContext.RequestServices.GetRequiredService<ILogger<Startup>>();
                    var clientId =
                        httpContext.HttpContext.User?.Identity?.IsAuthenticated == true
                            ? httpContext.HttpContext.User.Identity!.Name!
                            : (httpContext.HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                               ?? httpContext.HttpContext.Connection.RemoteIpAddress?.ToString()
                               ?? "unknown");

                    var path = httpContext.HttpContext.Request.Path;

                    // Log the event
                    logger.LogWarning("Rate limit exceeded for client {ClientId} on {Path}",
                                    clientId, path);

                    httpContext.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    httpContext.HttpContext.Response.Headers["Retry-After"] = retryAfter.TotalSeconds.ToString();
                    httpContext.HttpContext.Response.ContentType = "application/json";

                    var response = new
                    {
                        error = "rate_limit_exceeded",
                        message = "Too many requests. Please try again later.",
                        retryAfterSeconds = retryAfter.TotalSeconds
                    };

                    await httpContext.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken: token);
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

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
            app.UseRouting();
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseMiddleware<MetricsMiddleware>();
            app.UseAuthentication();
            app.UseRateLimiter(); //user-based partitioning works
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}