using NotificationService.Core.Logic;
using NotificationService.Core.Models;
using Xunit;

namespace NotificationService.UnitTests;

public class SeverityEvaluatorTests
{
    private readonly SeverityEvaluator _evaluator = new();

    [Theory]
    [InlineData("info", NotificationSeverity.Info)]
    [InlineData("INFO", NotificationSeverity.Info)]
    [InlineData("Info", NotificationSeverity.Info)]
    [InlineData("warning", NotificationSeverity.Warning)]
    [InlineData("WARNING", NotificationSeverity.Warning)]
    [InlineData("Warning", NotificationSeverity.Warning)]
    [InlineData("  warning  ", NotificationSeverity.Warning)]
    [InlineData("error", NotificationSeverity.Error)]
    [InlineData("ERROR", NotificationSeverity.Error)]
    [InlineData("critical", NotificationSeverity.Critical)]
    [InlineData("CRITICAL", NotificationSeverity.Critical)]
    public void TryParse_ValidLevel_ReturnsTrueAndCorrectSeverity(string input, NotificationSeverity expected)
    {
        var success = _evaluator.TryParse(input, out var severity);

        Assert.True(success);
        Assert.Equal(expected, severity);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("debug")]
    [InlineData("trace")]
    [InlineData("fatal")]
    [InlineData("warn")]
    [InlineData("123")]
    public void TryParse_InvalidLevel_ReturnsFalse(string? input)
    {
        var success = _evaluator.TryParse(input, out var severity);

        Assert.False(success);
        Assert.Equal(NotificationSeverity.Info, severity);
    }

    [Fact]
    public void SeverityOrder_InfoIsLessThanWarningErrorCritical()
    {
        Assert.True(NotificationSeverity.Info < NotificationSeverity.Warning);
        Assert.True(NotificationSeverity.Warning < NotificationSeverity.Error);
        Assert.True(NotificationSeverity.Error < NotificationSeverity.Critical);
    }

    [Theory]
    [InlineData(NotificationSeverity.Info, false)]
    [InlineData(NotificationSeverity.Warning, true)]
    [InlineData(NotificationSeverity.Error, true)]
    [InlineData(NotificationSeverity.Critical, true)]
    public void ShouldForward_ReturnsExpectedForSeverity(NotificationSeverity severity, bool expectedShouldForward)
    {
        var result = _evaluator.ShouldForward(severity);

        Assert.Equal(expectedShouldForward, result);
    }
}
