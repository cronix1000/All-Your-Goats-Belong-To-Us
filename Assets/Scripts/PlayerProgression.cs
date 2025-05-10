using UnityEngine;
using System.Collections.Generic;

public class PlayerProgression : MonoBehaviour
{
    public PlayerController playerController; // Assign in Inspector
    public UIManager uiManager;             // Assign your UIManager in Inspector
    public UpgradeManager upgradeManager;     // Assign your UpgradeManager in Inspector

    public int currentLevel = 0; // Start at level 0, first threshold makes them level 1
    private int maxReachedLevel = 0; // Tracks the highest level achieved this session

    // Herd sizes needed to REACH the next level.
    // Example: thresholds[0] = 5 means 5 goats to reach level 1.
    // thresholds[1] = 12 means 12 goats to reach level 2.
    public List<int> herdSizeThresholdsForLevelUp; // e.g., [5, 12, 20, 30, 45, 60]
                                                  // Size of this list determines max level - 1

    private int previousHerdCount = -1; // Initialize to a value that ensures first check runs
    private bool canTriggerLevelUpProcess = true; // Prevents multiple upgrade UIs if multiple levels gained at once

    void Start()
    {
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (playerController == null) Debug.LogError("PlayerController not found on PlayerProgression!");
        if (uiManager == null) Debug.LogError("UIManager not assigned to PlayerProgression!");
        if (upgradeManager == null) Debug.LogError("UpgradeManager not assigned to PlayerProgression!");

        // Initialize level based on starting herd (if any)
        // For simplicity, let's assume player starts with 0 goats and level 0.
        // Level 1 is achieved upon reaching the first threshold.
        UpdateLevelBasedOnHerdCount(playerController.herdedGoats.Count, true); // Initial check
        previousHerdCount = playerController.herdedGoats.Count;
    }

    void Update()
    {
        if (playerController == null) return;

        int currentHerdCount = playerController.herdedGoats.Count;
        if (currentHerdCount != previousHerdCount)
        {
            UpdateLevelBasedOnHerdCount(currentHerdCount, false);
            previousHerdCount = currentHerdCount;
        }
    }

    void UpdateLevelBasedOnHerdCount(int currentHerdCount, bool isInitialSetup)
    {
        int newPotentialLevel = 0; // Start at base level
        for (int i = 0; i < herdSizeThresholdsForLevelUp.Count; i++)
        {
            if (currentHerdCount >= herdSizeThresholdsForLevelUp[i])
            {
                newPotentialLevel = i + 1; // Thresholds index + 1 = level
            }
            else
            {
                break; // No need to check further thresholds
            }
        }

        if (newPotentialLevel > currentLevel && canTriggerLevelUpProcess)
        {
            // LEVEL UP!
            int levelsGained = newPotentialLevel - currentLevel;
            currentLevel = newPotentialLevel;
            maxReachedLevel = Mathf.Max(maxReachedLevel, currentLevel); // Update max reached level

            Debug.Log($"Player Leveled Up to Level {currentLevel}! (Herd: {currentHerdCount})");
            canTriggerLevelUpProcess = false; // Prevent immediate re-trigger if multiple level thresholds passed
            Time.timeScale = 0f; // PAUSE GAME
            List<UpgradeData> choices = upgradeManager.GetUpgradeChoices(3);
            if(choices.Count > 0)
            {
                uiManager.ShowUpgradeOptions(choices, this);
            }
            else
            {
                Debug.LogWarning("Leveled up, but no available upgrades to choose from!");
                ResumeGameAfterUpgradeChoice(); // Resume if no choices
            }
        }
        else if (newPotentialLevel < currentLevel)
        {
            // LEVEL DOWN!
            Debug.Log($"Player Leveled Down to Level {newPotentialLevel}! (Herd: {currentHerdCount})");
            currentLevel = newPotentialLevel;
            // Note: We are NOT un-applying upgrades when leveling down for simplicity.
            // Player just needs to re-acquire goats to regain higher level benefits or new upgrade prompts.
            canTriggerLevelUpProcess = true; // Allow level up again if they regain goats
        }
        // If newPotentialLevel == currentLevel, no change in level number.
        // uiManager.UpdateLevelDisplay(currentLevel); // If you have a level display
    }

    public void OnUpgradeChosen(UpgradeData chosenUpgrade)
    {
        if (chosenUpgrade == null) // Handle case where UI might be closed without choice
        {
            Debug.Log("No upgrade chosen, or UI closed.");
            ResumeGameAfterUpgradeChoice();
            return;
        }

        Debug.Log($"Upgrade Chosen: {chosenUpgrade.upgradeName}");
        upgradeManager.ApplyPlayerUpgrade(chosenUpgrade, playerController, this);
        ResumeGameAfterUpgradeChoice();

        // Check if eligible for another level up immediately (e.g. gained multiple levels worth of goats)
        // Or if an upgrade itself caused a change that might trigger another level.
        // Small delay or next frame check might be smoother than immediate re-check.
        Invoke(nameof(DeferredLevelCheck), 0.01f);
    }
    
    void DeferredLevelCheck()
    {
         UpdateLevelBasedOnHerdCount(playerController.herdedGoats.Count, false);
    }

    public void ResumeGameAfterUpgradeChoice()
    {
        uiManager.HideUpgradeOptions();
        Time.timeScale = 1f; // UNPAUSE GAME
        canTriggerLevelUpProcess = true; // Allow next level up trigger
    }
}