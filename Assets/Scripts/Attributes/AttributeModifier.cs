// GameClient/Assets/Scripts/Attributes/AttributeModifier.cs

/// <summary>
/// A simple class that defines a modification to an attribute.
/// Can be a flat bonus or a percentage multiplier.
/// </summary>
public class AttributeModifier
{
    public readonly float Value;
    public readonly bool IsPercentage;
    public readonly object Source; // The object that applied this modifier (e.g., an UpgradeData or BargainEffect)

    public AttributeModifier(float value, bool isPercentage, object source)
    {
        Value = value;
        IsPercentage = isPercentage;
        Source = source;
    }
}