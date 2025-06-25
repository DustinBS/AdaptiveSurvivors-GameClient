// GameClient/Assets/Scripts/Managers/StatisticsTracker.cs

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A static service class for aggregating gameplay statistics during a single run.
/// It acts as a decoupled "stats bus" that other systems can report to.
/// GameManager retrieves the final summary from this service at the end of a run.
/// </summary>
public static class StatisticsTracker
{
    private static Dictionary<string, long> _counters;
    private static int _finalLevel;

    // Static constructor to initialize the tracker.
    static StatisticsTracker()
    {
        _counters = new Dictionary<string, long>();
        _finalLevel = 1;
    }

    /// <summary>
    /// Resets all statistics for the beginning of a new run.
    /// Should be called by GameManager.
    /// </summary>
    public static void Reset()
    {
        _counters.Clear();
        _finalLevel = 1;
    }

    /// <summary>
    /// Records that an enemy of a specific type was killed.
    /// </summary>
    /// <param name="enemyType">The ID of the enemy type (e.g., "ED_Zombie").</param>
    public static void RecordEnemyKilled(string enemyType)
    {
        string key = $"enemies_killed_{enemyType}";
        if (!_counters.ContainsKey(key))
        {
            _counters[key] = 0;
        }
        _counters[key]++;
    }

    /// <summary>
    /// Records the player's final level for the run.
    /// </summary>
    public static void RecordFinalLevel(int level)
    {
        _finalLevel = level;
    }

    /// <summary>
    /// Generates the final run summary dictionary.
    /// </summary>
    /// <returns>A dictionary containing all tracked statistics for the run.</returns>
    public static Dictionary<string, object> GenerateRunSummary()
    {
        // Convert the internal 'long' dictionary to an 'object' dictionary.
        var summary = _counters.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);

        // Add stats that aren't simple counters.
        summary["final_level"] = _finalLevel;

        long totalKills = _counters.Where(kvp => kvp.Key.StartsWith("enemies_killed_")).Sum(kvp => kvp.Value);
        summary["enemies_killed_total"] = totalKills;

        return summary;
    }
}