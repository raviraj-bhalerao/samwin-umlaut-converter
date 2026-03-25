using System.Diagnostics.Metrics;
namespace Samwin.UmlautConverter.Api.Services
{
    public class MetricsService
    {
        public const string MeterName = "samwin-umlaut-converter-api.Metrics";
        public const string MeterDescription = "Metrics for samwin-umlaut-converter-api";
        public const string MeterVersion = "1.0.0";

        private static readonly Meter _samwinPerformanceMeter = new(MeterName, MeterVersion);
        public Counter<int> LoginAttempts { get; }
        public Counter<int> RequestCounter { get; }
        public Histogram<double> JwtGenerationTime { get; }

        public MetricsService()
        {
            LoginAttempts = _samwinPerformanceMeter.CreateCounter<int>("login_attempts", description: "Number of login attempts");
            RequestCounter = _samwinPerformanceMeter.CreateCounter<int>("no_of_requests", description: "Number of requests");
            JwtGenerationTime = _samwinPerformanceMeter.CreateHistogram<double>("jwt_generation_duration_ms", description: "Time to generate JWT in ms");
        }
    }
}