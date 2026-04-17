using System.Collections.Generic;
using Samwin.UmlautConverter.Api.Services.Telemetry;

namespace Samwin.UmlautConverter.Api.Utils
{
    public static class ServiceExtensions
    {
        private const string Result = "result";
        private const string Hit = "hit";
        private const string Miss = "miss";
        public static void RecordCacheHit(this MetricsService metricsService, int counter = 1)
        {
            metricsService.Record(Hit, counter);
        }

        public static void RecordCacheMiss(this MetricsService metricsService, int counter = 1)
        {
            metricsService.Record(Miss, counter);
        }

        private static void Record(this MetricsService metricsService, string result, int counter = 1)
        {
            metricsService.TokenConversionCacheEvent.Add(counter,
                new KeyValuePair<string, object?>(Result, result));
        }
    }
}