using NotificationService.Core.Models;

namespace NotificationService.Core.Logic;

public interface ISeverityEvaluator
{
    bool TryParse(string? levelString, out NotificationSeverity severity);
    bool ShouldForward(NotificationSeverity severity);
}

public class SeverityEvaluator : ISeverityEvaluator
{
    public bool TryParse(string? levelString, out NotificationSeverity severity)
    {
        severity = NotificationSeverity.Info;
        if (string.IsNullOrWhiteSpace(levelString))
        {
            return false;
        }

        return levelString.Trim().ToLowerInvariant() switch
        {
            "info" => SetAndReturn(NotificationSeverity.Info, out severity),
            "warning" => SetAndReturn(NotificationSeverity.Warning, out severity),
            "error" => SetAndReturn(NotificationSeverity.Error, out severity),
            "critical" => SetAndReturn(NotificationSeverity.Critical, out severity),
            _ => false
        };
    }

    public bool ShouldForward(NotificationSeverity severity)
    {
        return severity >= NotificationSeverity.Warning;
    }

    private static bool SetAndReturn(NotificationSeverity value, out NotificationSeverity target)
    {
        target = value;
        return true;
    }
}
