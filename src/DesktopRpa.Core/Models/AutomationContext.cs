namespace DesktopRpa.Core.Models;

public sealed class AutomationLogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public AutomationLogLevel Level { get; set; } = AutomationLogLevel.Information;
    public string Message { get; set; } = string.Empty;
    public string? Source { get; set; }
}

public sealed class AutomationOptions
{
    public decimal AllowedAmountTolerance { get; set; } = 0.00m;
    public bool EnableUiEmulation { get; set; } = true;
    public int EmulationStepDelayMs { get; set; } = 40;
    public bool AutoExportReport { get; set; } = true;
    public string OutputDirectory { get; set; } = "Reports";
}
