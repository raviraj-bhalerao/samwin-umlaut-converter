using System.Diagnostics;
namespace Samwin.UmlautConverter.Api.Services
{
    public interface IActivityService
    {
        Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal);
    }
}