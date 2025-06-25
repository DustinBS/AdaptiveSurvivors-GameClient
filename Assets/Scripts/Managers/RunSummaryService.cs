// GameClient/Assets/Scripts/Managers/RunSummaryService.cs

using System.Collections.Generic;

/// <summary>
/// A static service class to hold the statistics of the most recently completed run.
/// This provides a decoupled way to transfer run data from the battle scene (GameManager)
/// to the hub scene (DialogueManager) without relying on singletons or direct references.
/// </summary>
public static class RunSummaryService
{
    public static Dictionary<string, object> LastRunSummary { get; private set; }
    public static bool IsNewSummaryAvailable { get; private set; }

    static RunSummaryService()
    {
        LastRunSummary = null;
        IsNewSummaryAvailable = false;
    }

    public static void SetRunSummary(Dictionary<string, object> summary)
    {
        LastRunSummary = summary;
        IsNewSummaryAvailable = true;
    }

    public static void ConsumeSummary()
    {
        IsNewSummaryAvailable = false;
        // We keep the data in LastRunSummary until the next run starts,
        // in case multiple NPCs want to comment on it.
    }

    public static void ClearSummary()
    {
        LastRunSummary = null;
        IsNewSummaryAvailable = false;
    }
}