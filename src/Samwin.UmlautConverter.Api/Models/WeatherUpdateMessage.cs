using System;

namespace Samwin.UmlautConverter.Api.Models
{
    public record WeatherUpdateMessage(
        string Message,
        string[] Inputs,
        DateTime SentAt
    );
}