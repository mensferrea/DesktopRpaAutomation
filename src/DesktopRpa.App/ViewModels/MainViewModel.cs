using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopRpa.App.Services;
using DesktopRpa.Core.Models;
using DesktopRpa.Core.Services;

namespace DesktopRpa.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IAutomationService _automationService;
    private readonly ITestDataGenerator _testDataGenerator;
    private readonly IReportExporter _reportExporter;
    private readonly IDialogService _dialogService;
    private readonly IClipboardService _clipboardService;

    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private string _statementFilePath = string.Empty;

    [ObservableProperty]
    private string _registryFilePath = string.Empty;

    [ObservableProperty]
    private decimal _allowedTolerance = 0.00m;

    [ObservableProperty]
    private bool _enableEmulation = true;

    [ObservableProperty]
    private int _emulationDelayMs = 35;

    [ObservableProperty]
    private bool _autoExportReports = true;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _statusText = "Готов к работе";

    [ObservableProperty]
    private ReconciliationReport? _currentReport;

    [ObservableProperty]
    private ReconciliationItem? _selectedItem;

    [ObservableProperty]
    private string _selectedFilter = "Все";

    public ObservableCollection<AutomationLogEntry> Logs { get; } = [];
    public ObservableCollection<ReconciliationItem> FilteredItems { get; } = [];

    public IReadOnlyList<string> FilterOptions { get; } =
    [
        "Все",
        "Совпало",
        "Только расхождения",
        "Расхождение по сумме",
        "Расхождение по контрагенту",
        "Отсутствует в выписке",
        "Отсутствует в реестре"
    ];

    public MainViewModel(
        IAutomationService automationService,
        ITestDataGenerator testDataGenerator,
        IReportExporter reportExporter,
        IDialogService dialogService,
        IClipboardService clipboardService)
    {
        _automationService = automationService;
        _testDataGenerator = testDataGenerator;
        _reportExporter = reportExporter;
        _dialogService = dialogService;
        _clipboardService = clipboardService;
    }

    [RelayCommand]
    private void BrowseStatement()
    {
        var file = _dialogService.ShowOpenFileDialog("Выберите файл выписки банка", "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*");
        if (!string.IsNullOrWhiteSpace(file))
        {
            StatementFilePath = file;
        }
    }

    [RelayCommand]
    private void BrowseRegistry()
    {
        var file = _dialogService.ShowOpenFileDialog("Выберите файл реестра платежей", "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*");
        if (!string.IsNullOrWhiteSpace(file))
        {
            RegistryFilePath = file;
        }
    }

    [RelayCommand]
    private void GenerateSampleData()
    {
        try
        {
            var targetDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SampleData");
            var (statement, registry) = _testDataGenerator.GenerateSampleDataset(targetDir, 25);

            StatementFilePath = statement;
            RegistryFilePath = registry;

            AddLog("Сгенерирован и подключен тестовый пакет банковских данных (25 операций)", AutomationLogLevel.Success);
            _dialogService.ShowInformation("Тестовый комплект файлов выписки и реестра сгенерирован и загружен в форму.");
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Ошибка генерации тестовых данных: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task StartAutomationAsync()
    {
        if (string.IsNullOrWhiteSpace(StatementFilePath) || !File.Exists(StatementFilePath))
        {
            _dialogService.ShowWarning("Пожалуйста, выберите существующий файл банковской выписки.");
            return;
        }

        if (string.IsNullOrWhiteSpace(RegistryFilePath) || !File.Exists(RegistryFilePath))
        {
            _dialogService.ShowWarning("Пожалуйста, выберите существующий файл внутреннего реестра.");
            return;
        }

        IsRunning = true;
        ProgressPercentage = 0;
        StatusText = "RPA процесс выполняется...";
        _cts = new CancellationTokenSource();

        var logProgress = new Progress<AutomationLogEntry>(AddLog);
        var percentageProgress = new Progress<double>(p => ProgressPercentage = p);

        var options = new AutomationOptions
        {
            AllowedAmountTolerance = AllowedTolerance,
            EnableUiEmulation = EnableEmulation,
            EmulationStepDelayMs = EmulationDelayMs,
            AutoExportReport = AutoExportReports,
            OutputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports")
        };

        try
        {
            var report = await _automationService.RunReconciliationAutomationAsync(
                StatementFilePath,
                RegistryFilePath,
                options,
                logProgress,
                percentageProgress,
                _cts.Token);

            CurrentReport = report;
            ApplyFilter();
            StatusText = $"Сверка завершена. Расхождений: {report.Summary.AmountMismatchCount + report.Summary.CounterpartyMismatchCount + report.Summary.MissingInStatementCount + report.Summary.MissingInRegistryCount}";
        }
        catch (OperationCanceledException)
        {
            AddLog("Операция отменена пользователем.", AutomationLogLevel.Warning);
            StatusText = "Процесс отменен";
        }
        catch (Exception ex)
        {
            AddLog($"Ошибка выполнения: {ex.Message}", AutomationLogLevel.Error);
            _dialogService.ShowError($"Произошла ошибка при выполнении: {ex.Message}");
            StatusText = "Ошибка выполнения";
        }
        finally
        {
            IsRunning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void CancelAutomation()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
            AddLog("Отправлен сигнал остановки процесса...", AutomationLogLevel.Warning);
        }
    }

    [RelayCommand]
    private async Task ExportReportCsvAsync()
    {
        if (CurrentReport == null)
        {
            _dialogService.ShowWarning("Нет результатов для экспорта. Сначала запустите сверку.");
            return;
        }

        var path = _dialogService.ShowSaveFileDialog("Экспорт отчета в CSV", "CSV Files (*.csv)|*.csv", $"reconciliation_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            await _reportExporter.ExportCsvAsync(CurrentReport, path);
            AddLog($"Отчет сохранен в CSV: {path}", AutomationLogLevel.Success);
            _dialogService.ShowInformation("Отчет успешно экспортирован.");
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Ошибка экспорта: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ExportReportMarkdownAsync()
    {
        if (CurrentReport == null)
        {
            _dialogService.ShowWarning("Нет результатов для экспорта. Сначала запустите сверку.");
            return;
        }

        var path = _dialogService.ShowSaveFileDialog("Экспорт протокола в Markdown", "Markdown Files (*.md)|*.md", $"reconciliation_summary_{DateTime.Now:yyyyMMdd_HHmm}.md");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            await _reportExporter.ExportMarkdownSummaryAsync(CurrentReport, path);
            AddLog($"Протокол сохранен в MD: {path}", AutomationLogLevel.Success);
            _dialogService.ShowInformation("Протокол успешно сформирован.");
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Ошибка экспорта: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ClearLogs()
    {
        Logs.Clear();
    }

    [RelayCommand]
    private void CopyDetailsToClipboard()
    {
        if (SelectedItem == null)
        {
            return;
        }

        var text = $"Идентификатор: {SelectedItem.ItemId}\nСтатус: {SelectedItem.Status}\nСумма выписки: {SelectedItem.StatementAmount}\nСумма реестра: {SelectedItem.RegistryAmount}\nДельта: {SelectedItem.Discrepancy}\nОписание: {SelectedItem.StatusDescription}";
        _clipboardService.SetText(text);
        AddLog("Сведения о выбранной операции скопированы в буфер обмена.", AutomationLogLevel.Information);
    }

    partial void OnSelectedFilterChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredItems.Clear();
        if (CurrentReport == null)
        {
            return;
        }

        var query = CurrentReport.Items.AsEnumerable();

        query = SelectedFilter switch
        {
            "Совпало" => query.Where(i => i.Status == MatchStatus.Matched),
            "Только расхождения" => query.Where(i => i.Status != MatchStatus.Matched),
            "Расхождение по сумме" => query.Where(i => i.Status == MatchStatus.AmountMismatch),
            "Расхождение по контрагенту" => query.Where(i => i.Status == MatchStatus.CounterpartyMismatch),
            "Отсутствует в выписке" => query.Where(i => i.Status == MatchStatus.MissingInBankStatement),
            "Отсутствует в реестре" => query.Where(i => i.Status == MatchStatus.MissingInInternalRegistry),
            _ => query
        };

        foreach (var item in query)
        {
            FilteredItems.Add(item);
        }
    }

    private void AddLog(AutomationLogEntry entry)
    {
        Logs.Insert(0, entry);
    }

    private void AddLog(string message, AutomationLogLevel level = AutomationLogLevel.Information)
    {
        AddLog(new AutomationLogEntry
        {
            Message = message,
            Level = level,
            Timestamp = DateTime.Now
        });
    }
}
