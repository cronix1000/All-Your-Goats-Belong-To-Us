using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeCardUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public Image iconImage;
    public Button selectButton;

    private UpgradeData currentUpgradeData;
    private PlayerProgression playerProgressionInstance; // To call back

    public void DisplayUpgrade(UpgradeData data, PlayerProgression progression)
    {
        currentUpgradeData = data;
        playerProgressionInstance = progression;

        if (nameText != null) nameText.text = data.upgradeName;
        if (descriptionText != null) descriptionText.text = data.description;
        if (iconImage != null)
        {
            iconImage.sprite = data.icon;
            iconImage.enabled = (data.icon != null);
        }

        selectButton.onClick.RemoveAllListeners(); // Clear previous listeners
        selectButton.onClick.AddListener(OnCardSelected);
    }

    void OnCardSelected()
    {
        if (currentUpgradeData != null && playerProgressionInstance != null)
        {
            playerProgressionInstance.OnUpgradeChosen(currentUpgradeData);
        }
        else
        {
            Debug.LogError("Upgrade data or progression reference missing on card selection!");
        }
    }
}