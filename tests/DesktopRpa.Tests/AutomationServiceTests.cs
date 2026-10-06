using DesktopRpa.Core.Models;
using DesktopRpa.Core.Services;
using Xunit;

namespace DesktopRpa.Tests;

public class AutomationServiceTests
{
    [Fact]
    public async Task RunReconciliationAutomationAsync_CompletesSuccessfully()
    {
        var testGen = new TestDataGenerator();
        var tempDir = Path.Combine(Path.GetTempPath(), "RpaTests_" + Guid.NewGuid().ToString("N"));

        try
        {
            var (stmtPath, regPath) = testGen.GenerateSampleDataset(tempDir, 10);

            var stmtParser = new StatementParser();
            var regParser = new RegistryParser();
            var engine = new ReconciliationEngine();
            var exporter = new ReportExporter();

            var service = new AutomationService(stmtParser, regParser, engine, exporter);

            var logs = new List<AutomationLogEntry>();
            var logProgress = new Progress<AutomationLogEntry>(logs.Add);

            var options = new AutomationOptions
            {
                EnableUiEmulation = false,
                AutoExportReport = true,
                OutputDirectory = Path.Combine(tempDir, "Reports")
            };

            var report = await service.RunReconciliationAutomationAsync(
                stmtPath,
                regPath,
                options,
                logProgress);

            Assert.NotNull(report);
            Assert.Equal(ReconciliationRunStatus.Completed, report.Status);
            Assert.True(report.Items.Count > 0);
            Assert.True(Directory.Exists(options.OutputDirectory));
            Assert.NotEmpty(Directory.GetFiles(options.OutputDirectory, "*.csv"));
            Assert.NotEmpty(Directory.GetFiles(options.OutputDirectory, "*.md"));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
