using UnityEngine;
using UnityEngine.UI; // For Herding UI

public class PeacefulGoat : BaseAI // Inherit from BaseAI
{
    [Header("Goat Specific UI")]
    public Canvas herdingUICanvas;
    public Image herdingProgressFillImage;

    [Header("Herding Behavior")]
    [Tooltip("Speed multiplier when following the player (applied to base moveSpeed).")]
    public float followPlayerSpeedMultiplier = 1.0f;
    public float followDistanceToPlayer = 2.0f;
    public float maxFollowDistance = 10f;
    public float timeToUnherdWhenTooFar = 5f;

    [Header("Separation (Goat Specific)")]
    public float separationRadius = 1.0f;
    public float separationForce = 3.0f; // Adjusted to be a force magnitude
    public LayerMask peacefulGoatLayer;
    private float separationCheckInterval = 0.2f;
    private float currentSeparationCheckTimer = 0f;

    [Header("Eating Behavior")]
    public float eatingDuration = 3f;
    private float currentEatingTimer = 0f;
    private Transform grassTarget = null;

    [Header("Conversion")]
    public GameObject cyborgGoatPrefab;

    public enum GoatState { Wandering, Herded, Converting, BeingFocusedForHerding, Eating }
    [SerializeField] public GoatState currentState = GoatState.Wandering; // Made public for easier debugging/external checks if needed

    private bool _isCurrentlyMoving = true; // Internal flag primarily for animation/state logic

    // Herding Focus
    private float currentHerdingFocusProgress = 0f;
    private float totalHerdingFocusDuration = 0f;

    // Timers
    private float timeSpentTooFarFromPlayer = 0f;
    private float distanceCheckIntervalPlayer = 0.5f;
    private float currentDistanceCheckTimerPlayer = 0f;

    private Transform playerTransformRef;
    private Collider2D mainCollider;

    protected override void Awake()
    {
        base.Awake();
        mainCollider = GetComponent<Collider2D>();
        if (mainCollider == null) Debug.LogError("PeacefulGoat requires a Collider2D for separation checks.", this);
    }

    protected override void Start()
    {
        base.Start();

        if (PlayerController.Instance != null) // Assuming a singleton PlayerController
        {
            playerTransformRef = PlayerController.Instance.transform;
        }
        else
        {
            // Fallback if PlayerController.Instance is not available
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransformRef = playerObj.transform;
            else Debug.LogWarning("PeacefulGoat: Player not found! Herding may not work.", this);
        }

        GameManager.Instance?.RegisterPeacefulGoat(); // Assuming GameManager singleton

        if (herdingUICanvas != null) herdingUICanvas.gameObject.SetActive(false);
        if (herdingProgressFillImage != null) herdingProgressFillImage.fillAmount = 0;

        currentSeparationCheckTimer = Random.Range(0, separationCheckInterval); // Stagger initial checks
        UpdateVisualState();
    }

    protected override void Update()
    {
        base.Update(); // Handles base knockback timer
        if (currentKnockbackTimer > 0) return; // Don't process other updates during knockback

        // State-specific logic that runs every frame (non-physics)
        switch (currentState)
        {
            case GoatState.Herded:
                CheckIfTooFarFromPlayer();
                break;
            case GoatState.Eating:
                HandleEatingStateTimers();
                break;
            case GoatState.BeingFocusedForHerding:
                if (herdingUICanvas != null && Camera.main != null) // Billboard UI
                {
                    herdingUICanvas.transform.rotation = Quaternion.LookRotation(herdingUICanvas.transform.position - Camera.main.transform.position);
                }
                break;
        }
    }

    protected override void FixedUpdate()
    {
        if (currentKnockbackTimer > 0 || rb == null)
        {
            base.FixedUpdate(); // Still apply boundary constraints if needed (though knockback usually stops at boundary)
            return;
        }

        _isCurrentlyMoving = true; // Assume moving, states can override

        switch (currentState)
        {
            case GoatState.Wandering:
                PerformWanderBehavior(); // Uses base.moveSpeed * base.wanderSpeedMultiplier
                break;
            case GoatState.Herded:
                PerformFollowPlayerBehavior();
                break;
            case GoatState.Eating:
            case GoatState.BeingFocusedForHerding:
            case GoatState.Converting:
                StopMovement(); // Use base class stop
                _isCurrentlyMoving = false;
                break;
        }

        // Apply separation force if moving and in an appropriate state
        if (_isCurrentlyMoving && (currentState == GoatState.Wandering || currentState == GoatState.Herded))
        {
            currentSeparationCheckTimer -= Time.fixedDeltaTime;
            if (currentSeparationCheckTimer <= 0f)
            {
                ApplySeparationForce();
                currentSeparationCheckTimer = separationCheckInterval;
            }
        }
        
        // Face direction logic after movement intent is set
        if (_isCurrentlyMoving && rb.linearVelocity.sqrMagnitude > 0.01f) {
            // Base movement methods (PerformWander, MoveTowardsTarget) already call FaceDirection.
            // If using custom velocity setting, call FaceDirection(rb.position + rb.velocity) or similar.
        } else if (currentState == GoatState.Eating && grassTarget != null) {
            FaceDirection(grassTarget.position);
        } else if ((currentState == GoatState.BeingFocusedForHerding || currentState == GoatState.Herded) && playerTransformRef != null) {
            FaceDirection(playerTransformRef.position);
        }

        UpdateVisualState(); // Update animator based on movement/state
        base.FixedUpdate(); // IMPORTANT: Apply boundary constraints AFTER all movement logic
    }

    void PerformFollowPlayerBehavior()
    {
        if (playerTransformRef == null)
        {
            BecomeWandering(); return;
        }

        Vector2 targetPos = playerTransformRef.position;
        float distanceToPlayerSqr = (rb.position - targetPos).sqrMagnitude;

        if (distanceToPlayerSqr > followDistanceToPlayer * followDistanceToPlayer)
        {
            MoveTowardsTarget(targetPos, moveSpeed * followPlayerSpeedMultiplier);
        }
        else
        {
            // Smoothly stop when close enough
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 10f);
        }
    }

    void ApplySeparationForce()
    {
        if (rb == null || rb.isKinematic || !mainCollider || separationForce <= 0f) return;

        Collider2D[] nearbyGoats = Physics2D.OverlapCircleAll(rb.position, separationRadius, peacefulGoatLayer);
        Vector2 separationAccumulator = Vector2.zero;
        int separationCount = 0;

        foreach (Collider2D goatCollider in nearbyGoats)
        {
            if (goatCollider != mainCollider && goatCollider.gameObject != gameObject)
            {
                Vector2 directionAwayFromOther = rb.position - (Vector2)goatCollider.transform.position;
                float distance = directionAwayFromOther.magnitude;
                if (distance < separationRadius && distance > 0.01f) // distance > 0 to avoid div by zero if perfectly overlapped
                {
                    // Force inversely proportional to distance (stronger when closer)
                    separationAccumulator += directionAwayFromOther.normalized / distance;
                    separationCount++;
                }
            }
        }

        if (separationCount > 0)
        {
            // Average the separation vector and apply it as a continuous force
            Vector2 finalSeparationForce = (separationAccumulator / separationCount).normalized * separationForce;
            rb.AddForce(finalSeparationForce, ForceMode2D.Force); // Continuous force
        }
    }

    void CheckIfTooFarFromPlayer() // Called from Update
    {
        currentDistanceCheckTimerPlayer -= Time.deltaTime;
        if (currentDistanceCheckTimerPlayer <= 0f)
        {
            currentDistanceCheckTimerPlayer = distanceCheckIntervalPlayer;
            if (playerTransformRef == null || currentState != GoatState.Herded)
            {
                if (currentState == GoatState.Herded) BecomeWandering(); // Lost player ref while herded
                return;
            }

            float distanceSqr = (transform.position - playerTransformRef.position).sqrMagnitude;
            if (distanceSqr > maxFollowDistance * maxFollowDistance)
            {
                timeSpentTooFarFromPlayer += distanceCheckIntervalPlayer;
                if (timeSpentTooFarFromPlayer >= timeToUnherdWhenTooFar)
                {
                    BecomeWandering();
                }
            }
            else
            {
                timeSpentTooFarFromPlayer = 0f; // Reset timer if back in range
            }
        }
    }

    #region State Transitions (Herding, Eating)
    public void StartPlayerHerdingFocus(float totalFocusTime)
    {
        if (currentState == GoatState.Wandering || currentState == GoatState.Eating)
        {
            if (currentState == GoatState.Eating) ExitEatingState(false); // Stop eating but don't immediately wander

            currentState = GoatState.BeingFocusedForHerding;
            totalHerdingFocusDuration = totalFocusTime;
            currentHerdingFocusProgress = 0f;
            if (herdingUICanvas != null) herdingUICanvas.gameObject.SetActive(true);
            if (herdingProgressFillImage != null) herdingProgressFillImage.fillAmount = 0;
            _isCurrentlyMoving = false;
            StopMovement();
            UpdateVisualState();
        }
    }

    public void UpdatePlayerHerdingFocusProgress(float deltaTime) // Called by Player
    {
        if (currentState == GoatState.BeingFocusedForHerding)
        {
            currentHerdingFocusProgress += deltaTime;
            float fill = (totalHerdingFocusDuration > 0) ? (currentHerdingFocusProgress / totalHerdingFocusDuration) : 0;
            if (herdingProgressFillImage != null) herdingProgressFillImage.fillAmount = Mathf.Clamp01(fill);
        }
    }

    public void CancelPlayerHerdingFocus()
    {
        if (currentState == GoatState.BeingFocusedForHerding)
        {
            if (herdingUICanvas != null) herdingUICanvas.gameObject.SetActive(false);
            currentHerdingFocusProgress = 0f;
            BecomeWandering(); // Revert to wandering
        }
    }

    public void CompletePlayerHerdingFocusAndBecomeHerded()
    {
        if (currentState == GoatState.BeingFocusedForHerding)
        {
            if (herdingUICanvas != null) herdingUICanvas.gameObject.SetActive(false);
            currentHerdingFocusProgress = 0f;
            BecomeHerded();
        }
    }

    public void BecomeHerded()
    {
        if (currentState == GoatState.Herded || currentState == GoatState.Converting) return;
        if (currentState == GoatState.Eating) ExitEatingState(false);

        currentState = GoatState.Herded;
        Debug.Log(gameObject.name + " became herded.");
        PlayerController.Instance?.AddGoatToHerd(this); // Notify Player
        timeSpentTooFarFromPlayer = 0f; // Reset this timer
        _isCurrentlyMoving = true; // Will start following
        UpdateVisualState();
        GameManager.Instance?.GoatHerded(); // Notify GameManager
    }

    public void BecomeWandering()
    {
        if (currentState == GoatState.Wandering || currentState == GoatState.Converting) return;
        GoatState oldState = currentState;

        if (currentState == GoatState.Eating) ExitEatingState(false);

        currentState = GoatState.Wandering;
        if (oldState == GoatState.Herded && PlayerController.Instance != null) PlayerController.Instance.RemoveGoatFromHerd(this);
        if (oldState == GoatState.BeingFocusedForHerding && herdingUICanvas != null) herdingUICanvas.gameObject.SetActive(false);

        timeSpentTooFarFromPlayer = 0f;
        _isCurrentlyMoving = true;
        SetNewWanderDestination(); // Ensure it picks a new wander point
        UpdateVisualState();
    }

    void EnterEatingState(Transform targetGrass)
    {
        if (currentState != GoatState.Wandering) return; // Can only start eating if wandering

        currentState = GoatState.Eating;
        grassTarget = targetGrass;
        currentEatingTimer = eatingDuration;
        _isCurrentlyMoving = false;
        StopMovement();
        if (grassTarget != null) FaceDirection(grassTarget.position);
        UpdateVisualState();
    }

    void HandleEatingStateTimers() // Called from Update
    {
        if (grassTarget != null) FaceDirection(grassTarget.position); // Keep facing grass

        currentEatingTimer -= Time.deltaTime;
        if (currentEatingTimer <= 0f)
        {
            ExitEatingState(true); // Exit and become wandering
        }
    }

    void ExitEatingState(bool transitionToWander)
    {
        grassTarget = null;
        currentEatingTimer = 0f;
        // Animator stuff for stopping eating animation would go in UpdateVisualState
        if (transitionToWander)
        {
            BecomeWandering();
        } else {
            // If not transitioning to wander, ensure isMoving is appropriately set for the next state.
            // For now, this is typically followed by a state change that sets isMoving.
            _isCurrentlyMoving = true; // Default to allow movement unless next state stops it.
            UpdateVisualState();
        }
    }
    #endregion

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (currentKnockbackTimer > 0) return;

        // Collision with grass to start eating
        if (collision.gameObject.CompareTag("Grass") && currentState == GoatState.Wandering)
        {
            EnterEatingState(collision.transform);
        }
        // Other collision logic (e.g. bumping into other goats, though separation is preferred)
    }

    public void StartConversionProcess() // Called by BasicEnemyAI
    {
        if (currentState == GoatState.Converting) return;

        // Clean up from current state
        GoatState oldState = currentState;
        if (oldState == GoatState.Eating) ExitEatingState(false);
        if (oldState == GoatState.Herded) PlayerController.Instance?.RemoveGoatFromHerd(this);
        if (oldState == GoatState.BeingFocusedForHerding && herdingUICanvas != null) herdingUICanvas.gameObject.SetActive(false);

        currentState = GoatState.Converting;
        _isCurrentlyMoving = false;
        StopMovement();
        // animator?.SetTrigger("startConverting"); // Optional animation
        UpdateVisualState(); // Reflect converting state

        CompleteConversion();
    }

    private void CompleteConversion()
    {
        GameManager.Instance?.GoatConvertedToCyborg(this); // Handles unregistering etc.
        if (cyborgGoatPrefab != null) Instantiate(cyborgGoatPrefab, transform.position, transform.rotation);
        else Debug.LogError("CyborgGoatPrefab not assigned to " + gameObject.name, this);
        Destroy(gameObject);
    }

    protected override void OnKnockbackStart()
    {
        base.OnKnockbackStart();
        // Goat-specific reactions to knockback
        if (currentState == GoatState.Eating) ExitEatingState(false);
        if (currentState == GoatState.BeingFocusedForHerding) CancelPlayerHerdingFocus();
        
        _isCurrentlyMoving = false; // Stop active movement logic
        // Animator might play a "hit" animation
        // Debug.Log($"{gameObject.name} (Goat) knockback started.");
        UpdateVisualState();
    }

    protected override void OnKnockbackEnd()
    {
        base.OnKnockbackEnd();
        // After knockback, decide next state. Usually wander unless it was herded and still near player.
        if (currentState != GoatState.Herded && currentState != GoatState.Converting)
        {
             BecomeWandering();
        } else if (currentState == GoatState.Herded) {
            // If herded, it might have been pushed away. CheckIfTooFarFromPlayer will eventually unherd if needed.
            // For now, just allow it to resume herding behavior.
            _isCurrentlyMoving = true;
        }
        // Debug.Log($"{gameObject.name} (Goat) knockback ended.");
        UpdateVisualState();
    }

    void UpdateVisualState() // Call this when state changes or movement status changes
    {
        if (animator == null) return;

        bool isActuallyMoving = _isCurrentlyMoving && rb != null && rb.linearVelocity.sqrMagnitude > 0.02f;
        animator.SetBool("isGrazing", currentState == GoatState.Eating);
        // Add other animator parameters based on currentState if needed
        // e.g., animator.SetBool("isHerded", currentState == GoatState.Herded);
    }

    void OnDestroy() // Called when GameObject is destroyed
    {
        // Ensure proper unregistration if not being converted
        // (conversion handles its own unregistration via GameManager.NotifyPeacefulGoatConverted)
        if (currentState != GoatState.Converting)
        {
            if (currentState == GoatState.Herded && PlayerController.Instance != null)
            {
                PlayerController.Instance.RemoveGoatFromHerd(this);
            }
            GameManager.Instance?.UnregisterPeacefulGoat();
        }
    }

public void PullTowards(Vector2 targetPosition, float pullForce, float pullDuration)
    {
        if (rb == null ) // currentHealth is from BasicEnemyAI
        {
            Debug.LogWarning($"{gameObject.name} cannot be pulled: no Rigidbody or has no health.", this);
            return;
        }

        // Calculate the direction from the goat towards the target position.
        Vector2 directionToTarget = (targetPosition - (Vector2)transform.position);
    
        // If already very close to the target, might not need a strong pull or any pull.
        if (directionToTarget.sqrMagnitude < 0.01f) // Roughly 0.1 units
        {
            return;
        }
        base.ApplyKnockback(directionToTarget.normalized, pullForce, pullDuration);

    }

    /// <summary>
    /// Convenience method to pull the Cyborg Goat specifically towards the player.
    /// </summary>
    /// <param name="pullForce">The magnitude of the impulse force.</param>
    /// <param name="pullDuration">How long the goat's normal AI is paused due to the pull.</param>
    public void PullTowardsPlayer(float pullForce, float pullDuration)
    {
        if (PlayerController.Instance == null || PlayerController.Instance.transform == null)
        {
            Debug.LogWarning("PlayerController.Instance or its transform not found. Cannot pull CybordGoat towards player.", this);
            return;
        }
        PullTowards(PlayerController.Instance.transform.position, pullForce, pullDuration);
    }


    public bool IsGoatHerded()
    {
        return currentState == GoatState.Herded;
    }
    public bool IsGoatEating()
    {
        return currentState == GoatState.Eating;
    }
}