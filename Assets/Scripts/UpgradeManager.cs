using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class UpgradeManager : MonoBehaviour
{
    public List<UpgradeData> allPossibleUpgrades; // Assign all your UpgradeData ScriptableObjects here in Inspector
    private List<UpgradeData> availableUpgradePool = new List<UpgradeData>();

    void Awake()
    {
        InitializeUpgradePool();
    }

    void InitializeUpgradePool()
    {
        availableUpgradePool.Clear();
        foreach (UpgradeData upgrade in allPossibleUpgrades)
        {
            upgrade.timesApplied = 0; // Reset application count for this game session
            availableUpgradePool.Add(upgrade);
        }
    }

    public List<UpgradeData> GetUpgradeChoices(int count)
    {
        List<UpgradeData> choices = new List<UpgradeData>();
        List<UpgradeData> eligibleUpgrades = availableUpgradePool
            .Where(u => u.timesApplied < u.maxApplications)
            .ToList(); // Filter out upgrades that reached max applications

        if (eligibleUpgrades.Count == 0)
        {
            Debug.LogWarning("No eligible upgrades left in the pool!");
            return choices; // Return empty list
        }

        // Shuffle for randomness
        for (int i = 0; i < eligibleUpgrades.Count; i++)
        {
            UpgradeData temp = eligibleUpgrades[i];
            int randomIndex = Random.Range(i, eligibleUpgrades.Count);
            eligibleUpgrades[i] = eligibleUpgrades[randomIndex];
            eligibleUpgrades[randomIndex] = temp;
        }

        for (int i = 0; i < Mathf.Min(count, eligibleUpgrades.Count); i++)
        {
            choices.Add(eligibleUpgrades[i]);
        }
        return choices;
    }

    public void ApplyPlayerUpgrade(UpgradeData chosenUpgrade, PlayerController player, PlayerProgression progression)
    {
        if (chosenUpgrade == null || player == null)
        {
            Debug.LogError("Cannot apply null upgrade or to null player!");
            return;
        }

        // Apply the effect based on upgrade type
        switch (chosenUpgrade.type)
        {
            // === PLAYER STATS ===
            case UpgradeType.PlayerMoveSpeed:
                if (chosenUpgrade.isPercentage) player.moveSpeed *= (1f + chosenUpgrade.value);
                else player.moveSpeed += chosenUpgrade.value;
                Debug.Log($"Player Move Speed set to: {player.moveSpeed}");
                break;
            case UpgradeType.HerdingRadius:
                if (chosenUpgrade.isPercentage) player.herdingRadius *= (1f + chosenUpgrade.value);
                else player.herdingRadius += chosenUpgrade.value;
                Debug.Log($"Player Herding Radius set to: {player.herdingRadius}");
                break;
            case UpgradeType.HerdingFocusSpeed: // Reduces timeToFocusHerd
                float reduction = chosenUpgrade.isPercentage ? player.timeToFocusHerd * chosenUpgrade.value : chosenUpgrade.value;
                player.timeToFocusHerd = Mathf.Max(0.1f, player.timeToFocusHerd - reduction); // Ensure it doesn't go <= 0
                Debug.Log($"Player Time To Focus Herd set to: {player.timeToFocusHerd}");
                break;
            case UpgradeType.PlayerMaxHealth:
                // Assuming PlayerController or a Health component on player handles health
                // Example: if(player.GetComponent<PlayerHealth>() != null) player.GetComponent<PlayerHealth>().IncreaseMaxHealth( (int)chosenUpgrade.value );
                Debug.LogWarning("PlayerMaxHealth upgrade chosen, but no PlayerHealth component logic implemented in UpgradeManager example.");
                break;

            // === GOAT/HERD BUFFS ===
            // These might require iterating through current herd or setting a global flag/multiplier
            // that individual goats then read. For simplicity, direct modification if goats have such public static vars or manager.
            case UpgradeType.GoatFollowSpeed:
                // Example: PeacefulGoat.GlobalFollowSpeedMultiplier += chosenUpgrade.value; (if you implement such a static)
                // Or, if playerController tracks this: player.goatFollowSpeedBonus += chosenUpgrade.value;
                // Then PeacefulGoat reads player.goatFollowSpeedBonus.
                Debug.LogWarning("GoatFollowSpeed upgrade chosen, actual implementation depends on how goat stats are managed.");
                break;
            case UpgradeType.GoatMaxFollowDistance:
                 Debug.LogWarning("GoatMaxFollowDistance upgrade chosen, actual implementation depends on how goat stats are managed.");
                break;
             case UpgradeType.GoatSeparationForce:
                 Debug.LogWarning("GoatSeparationForce upgrade chosen, actual implementation depends on how goat stats are managed.");
                break;

            // === OTHER ===
            case UpgradeType.ReducedUnherdTime: // This means timeToUnherdWhenTooFar INCREASES
                // Example: if playerController tracks this: player.timeToUnherdBonus += chosenUpgrade.value;
                // Then PeacefulGoat reads this bonus to adjust its internal timer.
                Debug.LogWarning("ReducedUnherdTime upgrade chosen, actual implementation depends on how goat stats are managed.");
                break;

            default:
                Debug.LogWarning($"Upgrade type {chosenUpgrade.type} not handled yet.");
                break;
        }

        chosenUpgrade.timesApplied++;
        // If it was unique or reached max applications, it will be filtered out next time by GetUpgradeChoices
    }
}