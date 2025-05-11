using UnityEngine;
using UnityEngine.SceneManagement; // For restarting or changing scenes
using TMPro; // If using TextMeshPro for UI

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int peacefulGoatsCount = 0; // Initialize with starting goat count
    public int cyborgGoatsCount = 0;
    public int totalGoatsToSave = 10; // Example win condition
    public int maxCyborgGoatsAllowed = 5; // Example loss condition
    public int goatsToHerd;
    public int goatsHerded = 0; // Number of goats herded in the current wave

    public int playerXP = 0;
    public int playerLevel = 1;
    public int xpToNextLevel = 100;

    public int playerHealth = 100; // Example
    public int playerMaxHealth = 100;

    // UI References (Optional, assign in Inspector)
    public TextMeshProUGUI peacefulGoatsText;
    public TextMeshProUGUI cyborgGoatsText;
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI playerHealthText;
    public GameObject gameOverScreen;
    public GameObject winScreen;

    private void Awake()
    {
        Debug.Log($"Awake called for {gameObject.name}. Current Instance is {(Instance == null ? "null" : Instance.gameObject.name)}"); // Optional debug line
        if (Instance != null && Instance != this)
        {
            // Debug.LogWarning($"Duplicate GameManager: {gameObject.name} is destroying itself because Instance is already {Instance.gameObject.name}."); // Optional debug line
            Destroy(gameObject);
            return; // Return after destroying to prevent further execution on this instance
        }
        else
        {
            Instance = this;
             Debug.Log($"{gameObject.name} has been set as GameManager.Instance."); // Optional debug line
        }

        UpdateUI();
        if (gameOverScreen) gameOverScreen.SetActive(false);
        if (winScreen) winScreen.SetActive(false);
    }

    // --- Goat Management ---
    public void GoatConvertedToCyborg(PeacefulGoat goat)
    {
        if (peacefulGoatsCount > 0) // Ensure we don't go negative if something unexpected happens
        {
            peacefulGoatsCount--;
        }
        if(goat.IsGoatHerded())
        {
            goatsHerded--;
            WaveManager.Instance.NotifyHerdedGoatConverted(); // Notify WaveManager of the conversion
        }
        UpdateUI();
        CheckLossCondition(); // Keep your original loss condition logic


    }

    public void GoatHerded()
    {
                // --- Notify WaveManager ---
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.NotifyPeacefulGoatHerded();
        }
        peacefulGoatsCount++;
        UpdateUI();
    }

    public void CyborgGoatRestored()
    {
        if (cyborgGoatsCount > 0) // Prevent going negative
        {
            cyborgGoatsCount--;
        }
        peacefulGoatsCount++;
        
        UpdateUI();
    }

    public void RegisterPeacefulGoat()
    {
        peacefulGoatsCount++;
        UpdateUI();
    }
    public void UnregisterPeacefulGoat() // if a goat is destroyed/removed not by conversion
    {
        if (peacefulGoatsCount > 0)
        {
            peacefulGoatsCount--;
        }
        UpdateUI();
    }


    // --- XP and Leveling ---

    // --- Player Health ---
    public void PlayerDamaged(int amount)
    {
        playerHealth -= amount;
        if (playerHealth <= 0)
        {
            playerHealth = 0;
            GameOver("The Shepherd has fallen!");
        }
        UpdateUI();
    }


    // --- Win/Loss Conditions ---
    private void CheckLossCondition()
    {
        // This condition might need refinement based on WaveManager logic.
        // Does "peacefulGoatsCount <= 0" mean all peaceful goats globally, or just those from the current wave?
        // For now, it's a global check.
        if (peacefulGoatsCount <= 0 && cyborgGoatsCount > 0 && WaveManager.Instance == null) // Example: if no WaveManager, this simple loss condition applies
        {
             // If WaveManager is present, it might control the overall win/loss for goat population.
             // This specific condition "All your goats belong to them!" might be better triggered
             // if ALL peaceful goats from ALL waves are converted and no more can be spawned.
            // GameOver("All your goats belong to them!");
            // Consider making this more specific, e.g., if there are no active waves and no peaceful goats left.
        }

        if (peacefulGoatsCount <= 0 || cyborgGoatsCount >= maxCyborgGoatsAllowed)
        {
            GameOver("Too many goats have been assimilated! The pasture is overrun.");
        }
    }

    public void CheckWinConditionByElimination() // Call this when an important enemy/boss is defeated
    {
        // Example: if all enemies of a certain type are cleared or a boss is defeated
        WinGame("The cybernetic menace has been vanquished!");
    }


    public void GameOver(string message)
    {
        Debug.Log("GAME OVER: " + message);
        if (gameOverScreen) gameOverScreen.SetActive(true);
        Time.timeScale = 0f; // Pause game
    }

    public void WinGame(string message)
    {
        Debug.Log("YOU WIN: " + message);
        if (winScreen) winScreen.SetActive(true);
        Time.timeScale = 0f; // Pause game
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        // Reset static instance if this GameManager is part of the scene being reloaded.
        // If it's DontDestroyOnLoad, and you reload the scene it was originally in,
        // the Awake logic should handle the duplicate.
        // However, if you are completely resetting game state, more might be needed.
        Instance = null; // Allow the next scene load to set its GameManager as the instance
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }


    // --- UI Update ---
    private void UpdateUI()
    {
        if (peacefulGoatsText) peacefulGoatsText.text = "Peaceful Goats: " + peacefulGoatsCount;
        if (cyborgGoatsText) cyborgGoatsText.text = "Cyborg Goats: " + cyborgGoatsCount;
        if (xpText) xpText.text = "XP: " + playerXP + " / " + xpToNextLevel;
        if (levelText) levelText.text = "Level: " + playerLevel;
        if (playerHealthText) playerHealthText.text = "Health: " + playerHealth + " / " + playerMaxHealth;
    }
}