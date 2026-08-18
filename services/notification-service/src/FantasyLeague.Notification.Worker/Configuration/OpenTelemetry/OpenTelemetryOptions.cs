using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace FantasyLeague.Notification.Worker.Configuration.OpenTelemetry;

internal class OpenTelemetryOptions
{
    public const string SectionName = "Alloy";

    [Required]
    [Url]
    public string OtlpEndpoint { get; init; } = string.Empty;
}
