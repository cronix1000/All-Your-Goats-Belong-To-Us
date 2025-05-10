using UnityEngine;
using UnityEngine.UI; // For health bar

public class BasicEnemyAI : BaseAI // Inherit from BaseAI
{
    [Header("Enemy Specifics")]
    public float detectionRadius = 10f;
    public int damageToPlayer = 10;
    public int xpValue = 50;
    public LayerMask goatLayer;
    public LayerMask playerLayer; // For detecting player

    [Header("Health & UI")]
    public Image healthBarFill;
    public int totalHealth = 20;
    [SerializeField] private int currentHealth; // Use [SerializeField] for private field visibility in Inspector

    [Header("Conversion Behavior")]
    public float conversionTimeToConvert = 1.5f;
    private float currentConversionProgress = 0f;
    private PeacefulGoat _goatBeingConverted = null;

    private Transform _targetGoatTransform;
    private PeacefulGoat _targetGoatScript;
    private Transform _playerTransform;

    private enum AiState { Wandering, ChasingGoat, ConvertingGoat } // Simplified states for this example
    private AiState currentState = AiState.Wandering;

    protected override void Awake()
    {
        base.Awake(); // Call base Awake for rb, animator, visualsTransform, etc.
        currentHealth = totalHealth;
        // BaseAI.moveSpeed is used directly or can be adjusted here if needed.
        // BaseAI.wanderSpeedMultiplier applies to moveSpeed during PerformWanderBehavior.
    }

    protected override void Start()
    {
        base.Start(); // Call base Start for initialPosition, rb setup, initial wander destination

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
        }
        InvokeRepeating(nameof(FindPotentialTargets), 0f, 0.5f);
        UpdateHealthBar();
    }

    void FindPotentialTargets()
    {
        if (currentKnockbackTimer > 0 || currentState == AiState.ConvertingGoat) return;

        // If currently "charging" a conversion, ensure that goat is still valid
        if (_goatBeingConverted != null)
        {
            if (!_goatBeingConverted.gameObject.activeInHierarchy || _goatBeingConverted.currentState == PeacefulGoat.GoatState.Converting)
            {
                ClearConversionState(); // Goat gone or already converted by someone else
                // Continue to find a new target
            }
            else
            {
                // Still focused on converting this goat, ensure we are in chase/convert state
                _targetGoatScript = _goatBeingConverted;
                _targetGoatTransform = _goatBeingConverted.transform;
                currentState = AiState.ChasingGoat; // Or ConvertingGoat if already in contact
                return;
            }
        }

        // If we have a valid primary target goat that's not being converted (by us or others)
        if (_targetGoatTransform != null && _targetGoatScript != null &&
            _targetGoatScript.gameObject.activeInHierarchy &&
            _targetGoatScript.currentState != PeacefulGoat.GoatState.Converting)
        {
            currentState = AiState.ChasingGoat;
            return; // Stick to current target
        }


        // Search for a new goat target if no valid current target or conversion target
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, detectionRadius, goatLayer);
        PeacefulGoat closestGoat = null;
        float closestDistanceSqr = Mathf.Infinity;

        foreach (var hitCollider in hitColliders)
        {
            PeacefulGoat potentialGoat = hitCollider.GetComponent<PeacefulGoat>();
            if (potentialGoat != null && potentialGoat.currentState != PeacefulGoat.GoatState.Converting)
            {
                float distanceSqr = (transform.position - hitCollider.transform.position).sqrMagnitude;
                if (distanceSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    closestGoat = potentialGoat;
                }
            }
        }

        if (closestGoat != null)
        {
            if (_targetGoatScript != closestGoat) // If target changed
            {
                ClearConversionState(); // Reset progress if target switches
            }
            _targetGoatScript = closestGoat;
            _targetGoatTransform = closestGoat.transform;
            currentState = AiState.ChasingGoat;
        }
        else // No target found
        {
            ClearGoatTarget();
            currentState = AiState.Wandering;
        }
    }
    
    private void ClearGoatTarget()
    {
        _targetGoatTransform = null;
        _targetGoatScript = null;
        // Don't clear _goatBeingConverted here unless logic dictates, FindPotentialTargets might re-assign it
    }

    private void ClearConversionState()
    {
        currentConversionProgress = 0f;
        _goatBeingConverted = null;
        // If was in ConvertingGoat state, switch out
        if(currentState == AiState.ConvertingGoat)
        {
            currentState = AiState.Wandering; // Or ChasingGoat if target still valid
        }
    }

    protected override void FixedUpdate()
    {
        if (currentKnockbackTimer > 0 || rb == null)
        {
            // Knockback is active or no Rigidbody, base FixedUpdate handles this boundary check part.
            base.FixedUpdate();
            return;
        }
        switch (currentState)
        {
            case AiState.Wandering:
                PerformWanderBehavior(); // Use base class wander
                break;
            case AiState.ChasingGoat:
                ChaseTargetGoat();
                break;
            case AiState.ConvertingGoat:
                // Usually stationary during conversion, facing the target.
                StopMovement();
                if (_goatBeingConverted != null) FaceDirection(_goatBeingConverted.transform.position);
                // Conversion progress itself is handled in OnCollisionStay2D
                break;
        }

        base.FixedUpdate(); // IMPORTANT: Apply boundary constraints from base class AFTER movement logic
    }

    void ChaseTargetGoat()
    {
        if (_targetGoatTransform == null || !_targetGoatTransform.gameObject.activeInHierarchy ||
            (_targetGoatScript != null && _targetGoatScript.currentState == PeacefulGoat.GoatState.Converting))
        {
            ClearGoatTarget();
            ClearConversionState();
            currentState = AiState.Wandering;
            // FindPotentialTargets(); // Let Invoke call it, or call if immediate re-target is desired
            return;
        }

        float distanceToTarget = Vector2.Distance(rb.position, _targetGoatTransform.position);
        // Stopping distance should be close enough for collision to register reliably for conversion
        float stoppingDistance = 0.7f; // Adjust based on enemy/goat collider sizes

        if (distanceToTarget > stoppingDistance)
        {
            MoveTowardsTarget(_targetGoatTransform.position, moveSpeed);
        }
        else
        {
            StopMovement(); // Stop when close enough
            FaceDirection(_targetGoatTransform.position);
            // If in range, collision events will handle starting/progressing conversion.
            // Consider changing state to ConvertingGoat here if contact is virtually guaranteed.
        }
    }

    public virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (currentKnockbackTimer > 0) return;

        HandleContactStart(collision.gameObject);
        TryDamagePlayer(collision.gameObject);
    }

    public virtual void OnCollisionStay2D(Collision2D collision)
    {
        if (currentKnockbackTimer > 0) return;
        ProcessContactForConversion(collision.gameObject);
    }

    public virtual void OnCollisionExit2D(Collision2D collision)
    {
        HandleContactEnd(collision.gameObject);
    }

    void HandleContactStart(GameObject contactObject)
    {
        PeacefulGoat goat = contactObject.GetComponent<PeacefulGoat>();
        // Start "charging" if it's our primary target and not already being converted.
        if (goat != null && _targetGoatScript == goat && goat.currentState != PeacefulGoat.GoatState.Converting)
        {
            _goatBeingConverted = goat; // Mark this goat as the one we are "charging up"
            // currentConversionProgress is NOT reset here. It resets on target switch, successful conversion, or contact end.
        }
    }

    void ProcessContactForConversion(GameObject contactObject)
    {
        if (_goatBeingConverted != null && contactObject == _goatBeingConverted.gameObject)
        {
            if (_goatBeingConverted.currentState != PeacefulGoat.GoatState.Converting)
            {
                currentState = AiState.ConvertingGoat; // Explicitly set state
                currentConversionProgress += Time.deltaTime; // Use Time.deltaTime as collision events are not strictly FixedUpdate

                if (currentConversionProgress >= conversionTimeToConvert)
                {
                    TryConvertGoat(_goatBeingConverted);
                }
            }
            else // Goat got converted by other means (or state changed unexpectedly)
            {
                ClearConversionState();
                FindPotentialTargets(); // Look for a new purpose
            }
        }
    }

    void HandleContactEnd(GameObject contactObject)
    {
        // If we lose contact with the specific goat we were "charging up"
        if (_goatBeingConverted != null && contactObject == _goatBeingConverted.gameObject)
        {
            // Reset progress for THIS goat as contact is broken. It might still be the primary target.
            currentConversionProgress = 0f;
            // Do not nullify _goatBeingConverted here, as we might regain contact.
            // If currentState was ConvertingGoat, switch back to ChasingGoat.
            if (currentState == AiState.ConvertingGoat)
            {
                currentState = AiState.ChasingGoat;
            }
        }
    }

    void TryConvertGoat(PeacefulGoat goatToConvert)
    {
        if (goatToConvert != null && goatToConvert == _goatBeingConverted && goatToConvert.currentState != PeacefulGoat.GoatState.Converting)
        {
            Debug.Log($"{gameObject.name} successfully converted {goatToConvert.name} after {currentConversionProgress:F2}s of contact.");
            goatToConvert.StartConversionProcess(); // Tell the goat to convert

            // Award XP or other benefits (ensure player exists, etc.)
            // if (PlayerStats.Instance != null) PlayerStats.Instance.AddXP(xpValue);

            ClearGoatTarget();
            ClearConversionState();
            currentState = AiState.Wandering;
            FindPotentialTargets(); // Proactively look for another target
        }
    }

    void TryDamagePlayer(GameObject collidedObject)
    {
        // Using CompareTag is often more performant if the player GameObject has the "Player" tag.
        if (collidedObject.CompareTag("Player"))
        {
            PlayerController playerController = collidedObject.GetComponent<PlayerController>(); // Assuming PlayerController script
            if (playerController != null)
            {
                playerController.TakeDamage(damageToPlayer); // Assuming PlayerController has TakeDamage
                // Optionally, apply knockback to player or self here
            }
        }
    }

    public void TakeDamage(int amount) // Public for other scripts (e.g., player attacks) to call
    {
        if (currentHealth <= 0) return; // Already dead

        currentHealth -= amount;
        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void UpdateHealthBar()
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = (float)currentHealth / totalHealth;
        }
    }

    protected override void OnKnockbackStart()
    {
        base.OnKnockbackStart(); // Important to call base for timer setup
        // Enemy-specific reactions to knockback
        ClearConversionState(); // Interrupt any conversion
        // Knockback might make it lose its target, so briefly wander before FindPotentialTargets re-evaluates.
        currentState = AiState.Wandering;
        // Debug.Log($"{gameObject.name} (Enemy) knockback started, conversion interrupted.");
    }

    protected override void OnKnockbackEnd()
    {
        base.OnKnockbackEnd();
        // After knockback, actively find a new target or resume wandering.
        FindPotentialTargets();
        // Debug.Log($"{gameObject.name} (Enemy) knockback ended.");
    }

    void Die()
    {
        // Grant XP, spawn effects, etc.
        // Example: if (PlayerXPSystem.Instance != null) PlayerXPSystem.Instance.GrantXP(xpValue);
        Debug.Log($"{gameObject.name} died. XP Value: {xpValue}");
        Destroy(gameObject);
    }
}