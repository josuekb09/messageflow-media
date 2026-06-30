namespace MessageFlow.App.ViewModels;

public sealed record ProductionVerificationItem(
    string Name,
    bool Passed,
    string Message)
{
    public string StatusText => Passed ? "Pass" : "Review";
}
