using UnityEngine;
using UnityEngine.UI; // For Button
using TMPro;        // For TextMeshPro
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public GameObject upgradePanel;         // Assign the parent Panel for upgrade cards
    public List<UpgradeCardUI> upgradeCardSlots; // Assign 3 UI Card GameObjects/prefabs that have UpgradeCardUI script

    // private PlayerProgression currentPlayerProgression; // To call back when an upgrade is chosen

    void Start()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);
        if (upgradeCardSlots == null || upgradeCardSlots.Count < 3)
        {
            Debug.LogError("UIManager: Not enough UpgradeCardUI slots assigned!");
        }
    }

    public void ShowUpgradeOptions(List<UpgradeData> choices, PlayerProgression progressionCallback)
    {
        if (upgradePanel == null || choices == null || choices.Count == 0)
        {
            Debug.LogWarning("Cannot show upgrade options: Panel missing, no choices, or no progression reference.");
            progressionCallback?.ResumeGameAfterUpgradeChoice(); // Resume if we can't show anything
            return;
        }

        // this.currentPlayerProgression = progressionCallback;
        upgradePanel.SetActive(true);

        for (int i = 0; i < upgradeCardSlots.Count; i++)
        {
            if (i < choices.Count)
            {
                upgradeCardSlots[i].gameObject.SetActive(true);
                upgradeCardSlots[i].DisplayUpgrade(choices[i], progressionCallback); // Pass PlayerProgression for callback
            }
            else
            {
                upgradeCardSlots[i].gameObject.SetActive(false); // Hide unused card slots
            }
        }
    }

    public void HideUpgradeOptions()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);
    }

    // public void NotifyUpgradeChosen(UpgradeData chosenUpgrade)
    // {
    //     if (currentPlayerProgression != null)
    //     {
    //         currentPlayerProgression.OnUpgradeChosen(chosenUpgrade);
    //     }
    //     HideUpgradeOptions();
    // }
}