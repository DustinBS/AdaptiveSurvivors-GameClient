// GameClient/Assets/Scripts/Kafka/DataModels/SeerDataModels.cs

using System.Collections.Generic;

/// <summary>
/// A data structure representing a single buff or debuff.
/// </summary>
[System.Serializable]
public class BargainEffect
{
    public BargainTargetStat targetStat;
    public float modifier;
    public bool isPercentage;
    public float duration;
}

/// <summary>
/// An enum to define all stats that a bargain can possibly target.
/// </summary>
public enum BargainTargetStat
{
    MaxHealth,
    Armor,
    MoveSpeed,
    DashCooldown,
    AttackDamage,
    AttackSpeed
}

/// <summary>
/// Represents a single bargain choice offered by the Seer.
/// </summary>
[System.Serializable]
public class BargainChoice
{
    public string description;
    public List<BargainEffect> buffs;
    public List<BargainEffect> debuffs;
}

/// <summary>
/// The payload for messages on the 'seer_results' topic.
/// </summary>
[System.Serializable]
public class SeerResultPayload
{
    public string playerId;
    public string dialogue;
    public List<BargainChoice> choices;
}