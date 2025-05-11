using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    // Player Movement
    public float moveSpeed = 5f;

    // Focused Herding Ability Fields
    public float herdingRadius = 4f;
    public float herdingScanInterval = 1.0f;
    public float timeToFocusHerd = 1.0f;
    public LayerMask goatLayer;

    private PlayerControls playerControls;
    private Rigidbody2D rb;
    private Vector2 moveInput;

    private float currentScanTimer;
    private float currentPlayerFocusTimer;
    private PeacefulGoat currentHerdingTargetGoat;
    private bool isCurrentlyFocusingHerd;

    [SerializeField] private Transform visualTransform; // Reference to the visual representation of the player

    [SerializeField] private Animator animator;

    // Public list to track herded goats. Goats will add/remove themselves via PlayerController methods.
    public List<PeacefulGoat> herdedGoats = new List<PeacefulGoat>();
    public static PlayerController Instance { get; private set; } // Singleton for easy access

    public GameObject MainProjectilePrefab;
    public GameObject SecondaryProjectilePrefab; // Add this line to declare the secondary projectile prefab

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        rb = GetComponent<Rigidbody2D>();
        playerControls = new PlayerControls();

        // Ensure your Input Action Map is named "Player" or adjust here
        playerControls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        playerControls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        currentScanTimer = 0f;
    }

    private void OnEnable()
    {
        playerControls.Player.Enable();
    }

    private void OnDisable()
    {
        playerControls.Player.Disable();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed; // Changed from linearVelocity to velocity for clarity

// flip sprite based on movement direction
        if (moveInput.x < 0)
        {
            visualTransform.localScale = new Vector3(1, 1, 1); // Facing right
        }
        else if (moveInput.x > 0)
        {
            visualTransform.localScale = new Vector3(-1, 1, 1); // Facing left
        }

        if (moveInput != Vector2.zero)
        {
            animator.SetBool("Walking", true);
        }
        else
        {
            animator.SetBool("Walking", false);
        }
    }

    private void Update()
    {
        HandleFocusedHerding();
        // Optionally, manage herded goats here if needed (e.g., formation, commands)
        // For basic follow, each goat manages itself.
    }

    private void HandleFocusedHerding()
    {
        if (isCurrentlyFocusingHerd)
        {
            if (currentHerdingTargetGoat == null || !currentHerdingTargetGoat.gameObject.activeInHierarchy ||
                currentHerdingTargetGoat.currentState == PeacefulGoat.GoatState.Herded ||
                currentHerdingTargetGoat.currentState == PeacefulGoat.GoatState.Converting)
            {
                StopHerdingFocus(true);
                return;
            }

            currentPlayerFocusTimer += Time.deltaTime;
            currentHerdingTargetGoat.UpdatePlayerHerdingFocusProgress(Time.deltaTime);

            if (currentPlayerFocusTimer >= timeToFocusHerd)
            {
                Debug.Log($"Player successfully focused on {currentHerdingTargetGoat.name}. Telling goat to complete herding.");
                currentHerdingTargetGoat.CompletePlayerHerdingFocusAndBecomeHerded();
                // The goat will add itself to the list via AddGoatToHerd()
                StopHerdingFocus(false);
                currentScanTimer = herdingScanInterval;
                // currentHerdingTargetGoat is reset in StopHerdingFocus
            }
        }
        else
        {
            currentScanTimer -= Time.deltaTime;
            if (currentScanTimer <= 0f)
            {
                currentScanTimer = herdingScanInterval;
                TryStartFocusingOnNearbyGoat();
            }
        }
    }

    private void TryStartFocusingOnNearbyGoat()
    {
        if (isCurrentlyFocusingHerd) return;

        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, herdingRadius, goatLayer);
        PeacefulGoat bestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;

        foreach (Collider2D col in nearbyColliders)
        {
            PeacefulGoat goat = col.GetComponent<PeacefulGoat>();
            if (goat != null &&
                (goat.currentState == PeacefulGoat.GoatState.Wandering))
            {
                if (herdedGoats.Contains(goat)) continue; // Don't try to re-focus an already herded goat

                float distSqr = (col.transform.position - transform.position).sqrMagnitude;
                if (distSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distSqr;
                    bestTarget = goat;
                }
            }
        }

        if (bestTarget != null)
        {
            isCurrentlyFocusingHerd = true;
            currentHerdingTargetGoat = bestTarget;
            currentPlayerFocusTimer = 0f;
            currentHerdingTargetGoat.StartPlayerHerdingFocus(timeToFocusHerd);
            Debug.Log($"Player started focusing on: {currentHerdingTargetGoat.name} to herd it.");
        }
    }

    private void StopHerdingFocus(bool tellGoatToCancelUI)
    {
        if (tellGoatToCancelUI && currentHerdingTargetGoat != null && currentHerdingTargetGoat.currentState == PeacefulGoat.GoatState.BeingFocusedForHerding)
        {
            currentHerdingTargetGoat.CancelPlayerHerdingFocus();
        }
        isCurrentlyFocusingHerd = false;
        currentHerdingTargetGoat = null; // Clear the specific target being focused on
        currentPlayerFocusTimer = 0f;
    }

    // Called by PeacefulGoat when it becomes herded
    public void AddGoatToHerd(PeacefulGoat goat)
    {
        if (!herdedGoats.Contains(goat))
        {
            herdedGoats.Add(goat);
            Debug.Log($"{goat.name} added to player's active herd. Total: {herdedGoats.Count}");
        }
    }

    // Called by PeacefulGoat when it stops being herded
    public void RemoveGoatFromHerd(PeacefulGoat goat)
    {
        if (herdedGoats.Contains(goat))
        {
            herdedGoats.Remove(goat);
            Debug.Log($"{goat.name} removed from player's active herd. Total: {herdedGoats.Count}");
        }
    }

    public void TakeDamage(int amount)
    {
        Debug.Log("Player took " + amount + " damage.");
        GameManager.Instance?.PlayerDamaged(amount);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, herdingRadius);

        if (isCurrentlyFocusingHerd && currentHerdingTargetGoat != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, currentHerdingTargetGoat.transform.position);
        }
    }

}