namespace DesktopRpa.Core.Models;

public enum TransactionType
{
    Debit,
    Credit
}

public enum MatchStatus
{
    Matched,
    AmountMismatch,
    CounterpartyMismatch,
    MissingInBankStatement,
    MissingInInternalRegistry
}

public enum ReconciliationRunStatus
{
    NotStarted,
    Running,
    Completed,
    Failed,
    Canceled
}

public enum AutomationLogLevel
{
    Information,
    Warning,
    Error,
    Success
}
