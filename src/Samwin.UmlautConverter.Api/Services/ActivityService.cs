using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
namespace Samwin.UmlautConverter.Api.Services
{
    [ExcludeFromCodeCoverage]
    public class ActivityService : IActivityService
    {
        private readonly ActivitySource _activitySource;

        public ActivityService()
        {
            _activitySource = new ActivitySource("samwin-umlaut-converter-api");
        }

        public Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
        {
            return _activitySource.StartActivity(name, kind);
        }
    }
}