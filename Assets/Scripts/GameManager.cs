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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: if you want it to persist across scenes
        }

        UpdateUI();
        if(gameOverScreen) gameOverScreen.SetActive(false);
        if(winScreen) winScreen.SetActive(false);
    }

    // --- Goat Management ---
    public void GoatConvertedToCyborg()
    {
        peacefulGoatsCount--;
        cyborgGoatsCount++;
        CheckLossCondition();
        UpdateUI();
    }

    public void CyborgGoatRestored()
    {
        cyborgGoatsCount--;
        peacefulGoatsCount++;
        // Potentially add XP for restoring
        AddXP(20);
        UpdateUI();
    }

    public void RegisterPeacefulGoat()
    {
        peacefulGoatsCount++;
        UpdateUI();
    }
    public void UnregisterPeacefulGoat() // if a goat is destroyed/removed
    {
        peacefulGoatsCount--;
        UpdateUI();
    }


    // --- XP and Leveling ---
    public void AddXP(int amount)
    {
        playerXP += amount;
        Debug.Log("Player gained " + amount + " XP. Total XP: " + playerXP);
        if (playerXP >= xpToNextLevel)
        {
            LevelUp();
        }
        UpdateUI();
    }

    private void LevelUp()
    {
        playerLevel++;
        playerXP -= xpToNextLevel; // Carry over extra XP
        xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * 1.5f); // Increase XP needed for next level
        Debug.Log("Player Leveled Up to Level " + playerLevel + "! Next level in " + xpToNextLevel + " XP.");
        // Add level up benefits here (e.g., increase player stats, unlock abilities)
        playerMaxHealth += 20;
        playerHealth = playerMaxHealth; // Heal on level up
        UpdateUI();
    }

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
        if (peacefulGoatsCount <= 0 && cyborgGoatsCount > 0) // All remaining goats are cyborgs
        {
            GameOver("All your goats belong to them!");
        }
        if (cyborgGoatsCount >= maxCyborgGoatsAllowed)
        {
            GameOver("Too many goats have been assimilated!");
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
        if(gameOverScreen) gameOverScreen.SetActive(true);
        // Add logic like Time.timeScale = 0; to pause game, show game over UI, etc.
        Time.timeScale = 0f; // Pause game
    }

    public void WinGame(string message)
    {
        Debug.Log("YOU WIN: " + message);
        if(winScreen) winScreen.SetActive(true);
        // Add logic like Time.timeScale = 0; show win UI, etc.
        Time.timeScale = 0f; // Pause game
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
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