using System.Diagnostics.Metrics;
namespace Samwin.UmlautConverter.Api.Services.Telemetry
{
    public class MetricsService
    {
        public const string MeterName = "samwin-umlaut-converter-api.Metrics";
        public const string MeterDescription = "Metrics for samwin-umlaut-converter-api";
        public const string MeterVersion = "1.0.0";

        private static readonly Meter _samwinPerformanceMeter = new(MeterName, MeterVersion);
        public Counter<int> TokenConversionCacheEvent { get; }
        public Counter<int> LoginAttempts { get; }
        public Counter<int> RequestCounter { get; }
        public Counter<int> TokensConverted { get; }
        public Counter<int> QueriesGenerated { get; }
        public Counter<int> VariationsCreated { get; }
        public Histogram<double> JwtGenerationTime { get; }

        public MetricsService()
        {
            RequestCounter = _samwinPerformanceMeter.CreateCounter<int>("umlaut_converter_requests_total", description: "Number of requests");

            LoginAttempts = _samwinPerformanceMeter.CreateCounter<int>("umlaut_converter_login_attempts_total", description: "Number of login attempts");
            JwtGenerationTime = _samwinPerformanceMeter.CreateHistogram<double>("umlaut_converter_jwt_generation_duration_ms", description: "Time to generate JWT in ms");

            TokensConverted = _samwinPerformanceMeter.CreateCounter<int>("umlaut_converter_tokens_converted_total", description: "Number of tokens converted for umlauts");
            QueriesGenerated = _samwinPerformanceMeter.CreateCounter<int>("umlaut_converter_queries_generated_total", description: "Number of Queries generated");
            VariationsCreated = _samwinPerformanceMeter.CreateCounter<int>("umlaut_converter_variations_created_total", description: "Number of Varitions created");

            TokenConversionCacheEvent = _samwinPerformanceMeter.CreateCounter<int>("umlaut_converter_cache_events_total", description: "Cache hit/miss events for umlaut conversion");
        }
    }
}