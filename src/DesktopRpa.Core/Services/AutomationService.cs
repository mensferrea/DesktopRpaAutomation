using DesktopRpa.Core.Models;

namespace DesktopRpa.Core.Services;

public sealed class AutomationService : IAutomationService
{
    private readonly IStatementParser _statementParser;
    private readonly IRegistryParser _registryParser;
    private readonly IReconciliationEngine _reconciliationEngine;
    private readonly IReportExporter _reportExporter;

    public AutomationService(
        IStatementParser statementParser,
        IRegistryParser registryParser,
        IReconciliationEngine reconciliationEngine,
        IReportExporter reportExporter)
    {
        _statementParser = statementParser;
        _registryParser = registryParser;
        _reconciliationEngine = reconciliationEngine;
        _reportExporter = reportExporter;
    }

    public async Task<ReconciliationReport> RunReconciliationAutomationAsync(
        string statementFilePath,
        string registryFilePath,
        AutomationOptions options,
        IProgress<AutomationLogEntry>? logProgress = null,
        IProgress<double>? percentageProgress = null,
        CancellationToken cancellationToken = default)
    {
        void Log(string message, AutomationLogLevel level = AutomationLogLevel.Information)
        {
            logProgress?.Report(new AutomationLogEntry
            {
                Message = message,
                Level = level,
                Timestamp = DateTime.Now,
                Source = nameof(AutomationService)
            });
        }

        Log("Инициализация RPA-робота сверки межбанковских платежей...");
        percentageProgress?.Report(5);

        await DelayIfEmulatingAsync(options.EnableUiEmulation, options.EmulationStepDelayMs, cancellationToken);

        Log($"Чтение банковской выписки: {Path.GetFileName(statementFilePath)}");
        var statementTx = await _statementParser.ParseStatementAsync(statementFilePath, cancellationToken);
        Log($"Загружено записей выписки: {statementTx.Count}");
        percentageProgress?.Report(25);

        await DelayIfEmulatingAsync(options.EnableUiEmulation, options.EmulationStepDelayMs, cancellationToken);

        Log($"Чтение внутреннего реестра платежей: {Path.GetFileName(registryFilePath)}");
        var registryPayments = await _registryParser.ParseRegistryAsync(registryFilePath, cancellationToken);
        Log($"Загружено записей реестра: {registryPayments.Count}");
        percentageProgress?.Report(45);

        await DelayIfEmulatingAsync(options.EnableUiEmulation, options.EmulationStepDelayMs, cancellationToken);

        Log("Запуск алгоритма сверки: сопоставление по референсам, ИНН и суммам...");
        var report = _reconciliationEngine.Reconcile(statementTx, registryPayments, options);
        percentageProgress?.Report(75);

        Log($"Результаты сверки: Совпало = {report.Summary.MatchedCount}, Расхождение по сумме = {report.Summary.AmountMismatchCount}, Расхождение по контрагенту = {report.Summary.CounterpartyMismatchCount}, Отсутствует в выписке = {report.Summary.MissingInStatementCount}, Отсутствует в реестре = {report.Summary.MissingInRegistryCount}",
            report.Summary.TotalDiscrepancyAmount > 0 ? AutomationLogLevel.Warning : AutomationLogLevel.Success);

        if (options.AutoExportReport)
        {
            await DelayIfEmulatingAsync(options.EnableUiEmulation, options.EmulationStepDelayMs, cancellationToken);

            var exportDir = Path.IsPathRooted(options.OutputDirectory)
                ? options.OutputDirectory
                : Path.Combine(AppContext.BaseDirectory, options.OutputDirectory);

            if (!Directory.Exists(exportDir))
            {
                Directory.CreateDirectory(exportDir);
            }

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var csvPath = Path.Combine(exportDir, $"reconciliation_result_{timestamp}.csv");
            var mdPath = Path.Combine(exportDir, $"reconciliation_summary_{timestamp}.md");

            await _reportExporter.ExportCsvAsync(report, csvPath, cancellationToken);
            await _reportExporter.ExportMarkdownSummaryAsync(report, mdPath, cancellationToken);

            Log($"Сформированы отчеты: {Path.GetFileName(csvPath)} и {Path.GetFileName(mdPath)}", AutomationLogLevel.Success);
        }

        percentageProgress?.Report(100);
        Log("Автоматизированный процесс успешно завершен.", AutomationLogLevel.Success);

        return report;
    }

    private static async Task DelayIfEmulatingAsync(bool enabled, int delayMs, CancellationToken cancellationToken)
    {
        if (enabled && delayMs > 0)
        {
            await Task.Delay(delayMs, cancellationToken);
        }
    }
}
