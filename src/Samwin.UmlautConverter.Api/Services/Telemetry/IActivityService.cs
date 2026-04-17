using System.Diagnostics;
namespace Samwin.UmlautConverter.Api.Services.Telemetry
{
    public interface IActivityService
    {
        Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal, ActivityContext parentContext = default);
    }
}