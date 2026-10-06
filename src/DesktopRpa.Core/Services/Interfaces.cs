using DesktopRpa.Core.Models;

namespace DesktopRpa.Core.Services;

public interface IStatementParser
{
    Task<IReadOnlyList<BankTransaction>> ParseStatementAsync(string filePath, CancellationToken cancellationToken = default);
}

public interface IRegistryParser
{
    Task<IReadOnlyList<RegistryPayment>> ParseRegistryAsync(string filePath, CancellationToken cancellationToken = default);
}

public interface IReconciliationEngine
{
    ReconciliationReport Reconcile(
        IReadOnlyList<BankTransaction> statementTransactions,
        IReadOnlyList<RegistryPayment> registryPayments,
        AutomationOptions options);
}

public interface IAutomationService
{
    Task<ReconciliationReport> RunReconciliationAutomationAsync(
        string statementFilePath,
        string registryFilePath,
        AutomationOptions options,
        IProgress<AutomationLogEntry>? logProgress = null,
        IProgress<double>? percentageProgress = null,
        CancellationToken cancellationToken = default);
}

public interface IReportExporter
{
    Task<string> ExportCsvAsync(ReconciliationReport report, string filePath, CancellationToken cancellationToken = default);
    Task<string> ExportMarkdownSummaryAsync(ReconciliationReport report, string filePath, CancellationToken cancellationToken = default);
}

public interface ITestDataGenerator
{
    (string statementFile, string registryFile) GenerateSampleDataset(string outputDirectory, int totalRows = 25);
}
