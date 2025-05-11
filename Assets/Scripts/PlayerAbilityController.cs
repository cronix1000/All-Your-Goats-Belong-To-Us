using UnityEngine;
using System;
using UnityEngine.InputSystem; // Required for Action
using UnityEngine.UI; // Required for UI elements like Image

public class PlayerAbilityController : MonoBehaviour
{
    private PlayerControls playerControls;

    // Define events for each ability (optional, but good practice if other systems need to react)
    public static event Action OnConvertEnemiesAbilityTriggered;
    public static event Action OnPullGoatsAbilityTriggered;
    public static event Action OnPushEnemiesAbilityTriggered;

    [Header("Conversion Ability")]
    [SerializeField] public GameObject ConversionCirclePrefab;
    [SerializeField] private float conversionCircleRadius = 5f; // Changed from int to float for OverlapCircle
    [SerializeField] private float conversionAbilityCooldown = 10f;
    [SerializeField] private Image conversionCooldownUI; // Assign in Inspector
    private float currentConversionCooldown = 0f;
    private GameObject activeConversionCircle; // To store the instantiated circle
    private bool isConversionCircleActive = false;

    [Header("Pull Ability")]
    [SerializeField] public GameObject PullCirclePrefab; // Assign the pull circle prefab in the inspector
    [SerializeField] private float pullCircleRadius = 20f; // Changed from int to float
    [SerializeField] private float pullAbilityCooldown = 8f;
    [SerializeField] private Image pullCooldownUI; // Assign in Inspector
    private float currentPullCooldown = 0f;
    // Note: The original script had pull circle duration/timer but didn't seem to use the prefab for it directly,
    // it just performed the pull. If you want a visual circle for pull like conversion, let me know.

    [Header("Push Ability (Main Projectile)")]
    [SerializeField] private GameObject MainProjectilePrefab;
    [SerializeField] private float pushAbilityCooldown = 2f;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private Image pushCooldownUI; // Assign in Inspector
    private float currentPushCooldown = 0f;


    private void Awake()
    {
        playerControls = new PlayerControls();

        // Bind the input actions to the methods
        playerControls.Player.SecondaryAttack.performed += ctx => HandleConvertEnemiesInput();
        playerControls.Player.PrimaryAttack.performed += ctx => AttemptPushEnemies(); // Renamed for clarity
        playerControls.Player.TertiaryAbility.performed += ctx => AttemptPullGoats(); // Renamed for clarity
    }

    private void Update()
    {
        // Update Cooldowns
        if (currentConversionCooldown > 0)
        {
            currentConversionCooldown -= Time.deltaTime;
            if (conversionCooldownUI != null)
            {
                conversionCooldownUI.fillAmount = currentConversionCooldown / conversionAbilityCooldown;
            }
        }
        else if (conversionCooldownUI != null && conversionCooldownUI.fillAmount != 0) // Ensure UI resets if not already
        {
            conversionCooldownUI.fillAmount = 0;
        }


        if (currentPullCooldown > 0)
        {
            currentPullCooldown -= Time.deltaTime;
            if (pullCooldownUI != null)
            {
                pullCooldownUI.fillAmount = currentPullCooldown / pullAbilityCooldown;
            }
        }
        else if (pullCooldownUI != null && pullCooldownUI.fillAmount != 0)
        {
            pullCooldownUI.fillAmount = 0;
        }


        if (currentPushCooldown > 0)
        {
            currentPushCooldown -= Time.deltaTime;
            if (pushCooldownUI != null)
            {
                pushCooldownUI.fillAmount = currentPushCooldown / pushAbilityCooldown;
            }
        }
        else if (pushCooldownUI != null && pushCooldownUI.fillAmount != 0)
        {
             pushCooldownUI.fillAmount = 0;
        }


        // Keep conversion circle at player's feet if active before conversion
        if (isConversionCircleActive && activeConversionCircle != null)
        {
            activeConversionCircle.transform.position = transform.position;
        }
    }

    private void OnEnable()
    {
        playerControls.Player.Enable();
    }

    private void OnDisable()
    {
        playerControls.Player.Disable();
        // Clean up the circle if the player is disabled while it's active
        if (activeConversionCircle != null)
        {
            Destroy(activeConversionCircle);
            isConversionCircleActive = false;
        }
    }

    private void HandleConvertEnemiesInput()
    {
        if (currentConversionCooldown > 0)
        {
            Debug.Log("Conversion ability is on cooldown.");
            return;
        }

        if (!isConversionCircleActive)
        {
            // First press: Activate and place the circle
            if (ConversionCirclePrefab != null)
            {
                if (activeConversionCircle == null) // Instantiate if it doesn't exist
                {
                    activeConversionCircle = Instantiate(ConversionCirclePrefab, transform.position, Quaternion.identity);
                    activeConversionCircle.transform.localScale = new Vector3(conversionCircleRadius * 2, conversionCircleRadius * 2, 1f); // Diameter
                }
                else // Reuse if it exists
                {
                    activeConversionCircle.transform.position = transform.position;
                    activeConversionCircle.SetActive(true);
                }
                isConversionCircleActive = true;
                Debug.Log("Conversion circle activated. Press again to convert.");
            }
            else
            {
                Debug.LogError("ConversionCirclePrefab is not assigned!");
            }
        }
        else
        {
            // Second press: Convert enemies and deactivate circle
            Debug.Log("Converting enemies in area!");
            Collider2D[] enemiesInRange = Physics2D.OverlapCircleAll(activeConversionCircle.transform.position, conversionCircleRadius, LayerMask.GetMask("Goat")); // Use circle's position

            foreach (Collider2D enemy in enemiesInRange)
            {
                if (enemy.TryGetComponent(out CybordGoat goat))
                {
                    goat.ConvertToFriendly();
                    Debug.Log($"Converted {enemy.name} to friendly.");
                }
            }

            if (activeConversionCircle != null)
            {
                activeConversionCircle.SetActive(false); // Deactivate instead of destroying if you want to reuse
                // Or Destroy(activeConversionCircle); if you prefer to always recreate
            }
            isConversionCircleActive = false;
            currentConversionCooldown = conversionAbilityCooldown; // Start cooldown
            if (conversionCooldownUI != null) conversionCooldownUI.fillAmount = 1; // Show full cooldown on UI
            OnConvertEnemiesAbilityTriggered?.Invoke();
            Debug.Log("Conversion complete, circle deactivated. Cooldown started.");
        }
    }

    private void AttemptPullGoats()
    {
        if (currentPullCooldown > 0)
        {
            Debug.Log("Pull ability is on cooldown.");
            return;
        }

        Debug.Log("Pulling goats!");
        // Optional: Spawn a visual effect for pull if PullCirclePrefab is intended for that
        if (PullCirclePrefab != null)
        {
            GameObject pullVisual = Instantiate(PullCirclePrefab, transform.position, Quaternion.identity);
            pullVisual.transform.localScale = new Vector3(pullCircleRadius * 2, pullCircleRadius * 2, 1f);
            // Assuming the pull visual has a script to destroy itself after a short duration
            Destroy(pullVisual, 0.5f); // Example: destroy visual after 0.5 seconds
        }

        Collider2D[] goatsInRange = Physics2D.OverlapCircleAll(transform.position, pullCircleRadius, LayerMask.GetMask("Goat"));
        foreach (Collider2D goatCollider in goatsInRange) // Renamed to avoid conflict
        {
            if (goatCollider.TryGetComponent(out PeacefulGoat peacefulGoat))
            {
                peacefulGoat.PullTowardsPlayer(5f, .2f);
            }
            else if (goatCollider.TryGetComponent(out CybordGoat cybordGoat))
            {
                cybordGoat.PullTowardsPlayer(5f, .2f);
            }
        }
        currentPullCooldown = pullAbilityCooldown;
        if (pullCooldownUI != null) pullCooldownUI.fillAmount = 1;
        OnPullGoatsAbilityTriggered?.Invoke();
        Debug.Log("Goats pulled. Cooldown started.");
    }

    private void AttemptPushEnemies()
    {
        if (currentPushCooldown > 0)
        {
            Debug.Log("Push ability (Main Attack) is on cooldown.");
            return;
        }

        Debug.Log("Main attack executed (Pushing enemies).");
        if (MainProjectilePrefab != null)
        {
            GameObject projectile = Instantiate(MainProjectilePrefab, transform.position, Quaternion.identity);
            Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
            if (projectileRb != null)
            {
                Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                Vector2 direction = (mousePosition - (Vector2)transform.position).normalized;
                projectileRb.linearVelocity = direction * projectileSpeed; // Used velocity for consistent speed
            }
            else
            {
                Debug.LogError("MainProjectilePrefab is missing a Rigidbody2D component!");
            }
        }
        else
        {
            Debug.LogError("MainProjectilePrefab is not assigned!");
        }

        currentPushCooldown = pushAbilityCooldown;
        if (pushCooldownUI != null) pushCooldownUI.fillAmount = 1;
        OnPushEnemiesAbilityTriggered?.Invoke();
        Debug.Log("Push attack performed. Cooldown started.");
    }

    void OnDrawGizmos()
    {
        // ConversionGizmo
        if (isConversionCircleActive && activeConversionCircle != null)
        {
            Gizmos.color = Color.green; // Green when active and waiting for second press
            Gizmos.DrawWireSphere(activeConversionCircle.transform.position, conversionCircleRadius);
        }
        else
        {
            Gizmos.color = Color.gray; // Gray when inactive or on cooldown (shows potential area)
            Gizmos.DrawWireSphere(transform.position, conversionCircleRadius);
        }


        // Pull Gizmo (always show potential area)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, pullCircleRadius);
    }
}