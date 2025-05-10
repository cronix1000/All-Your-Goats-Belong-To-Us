using UnityEngine;
using System.Collections.Generic; // For prerequisites if you add them

public enum UpgradeType
{
    // Player Stats
    PlayerMoveSpeed,
    HerdingRadius,      // For focused herding initiation
    HerdingFocusSpeed,  // How fast the 1-second focus completes
    PlayerMaxHealth,    // If player has health

    // Goat/Herd Buffs
    GoatFollowSpeed,
    GoatMaxFollowDistance, // How far they stay before "too far" timer
    GoatSeparationForce, // How strongly goats push each other

    // New Abilities or Modifiers
    // Example: DashAbility, TemporaryInvulnerability, HerdAttackCommand (if you add such things)

    // Economy/Other
    GoatAttractionPassive, // Small chance for nearby neutral goats to auto-join herd over time
    ReducedUnherdTime    // Takes longer for goats to unherd when too far
}

[CreateAssetMenu(fileName = "NewUpgradeData", menuName = "GoatGame/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    public string upgradeName = "New Upgrade";
    [TextArea(3, 5)]
    public string description = "Description of the upgrade effect.";
    public Sprite icon; // Assign in Inspector

    public UpgradeType type;
    public float value = 0f;           // e.g., +0.5 speed, +10% radius (use 0.1 for 10%)
    public float value2 = 0f;          // For upgrades needing a second parameter
    public bool isPercentage = false;  // If true, 'value' is treated as a percentage modifier

    public int maxApplications = 1; // How many times this specific upgrade can be chosen (1 for unique, more for stackable stats)
    [HideInInspector] public int timesApplied = 0; // Internal tracking

    // Future expansion:
    // public List<UpgradeData> prerequisites;
    // public bool isAbilityUnlock = false;
    // public string abilityID;
}