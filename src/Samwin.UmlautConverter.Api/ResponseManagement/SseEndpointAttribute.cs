using System;
using System.Diagnostics.CodeAnalysis;

namespace Samwin.UmlautConverter.Api.ResponseManagement
{
    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Method)]
    public class SseEndpointAttribute : Attribute { }
}