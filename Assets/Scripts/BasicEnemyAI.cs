// using UnityEditor.PackageManager; // This using statement seems unused.
using UnityEngine;
using UnityEngine.UI;

public class BasicEnemyAI : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float detectionRadius = 10f;
    public int damageToPlayer = 10;
    public int xpValue = 50;
    public LayerMask goatLayer;
    public LayerMask playerLayer;

    public float wanderSpeedMultiplier = 0.6f;
    public float wanderRadius = 5f;
    public float wanderPointReachedThreshold = 0.5f;
    public float wanderTimerDurationMin = 2.0f;
    public float wanderTimerDurationMax = 5.0f;

    private Transform _targetGoatTransform;
    private PeacefulGoat _targetGoatScript;
    private Transform _player;
    private Rigidbody2D _rb;

    private float knockbackDuration = 0.3f;
    private float currentKnockbackTime = 0f;

    private Vector2 _currentWanderTargetPoint;
    private float _currentWanderTimer;

    public Image healthBarFill;
    public int totalHealth = 20;
    public int health;

    // Conversion Process Variables
    public float conversionTimeToConvert = 1.5f; // Time needed to be in contact to convert
    private float currentConversionProgress = 0f;  // How long contact has been maintained with current target
    private PeacefulGoat _goatBeingConverted = null; // Tracks which goat is currently being "charged up" for conversion

    void Awake()
    {
        health = totalHealth;
    }

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb != null)
        {
            _rb.gravityScale = 0;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        InvokeRepeating("FindTarget", 0f, 0.5f);
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
        }
        SetNewWanderTargetPoint();
    }

    void FindTarget()
    {
        // If currently converting a goat, don't switch targets unless that goat is gone/converted by other means
        if (_goatBeingConverted != null && _goatBeingConverted.currentState == PeacefulGoat.GoatState.Converting)
        {
            _goatBeingConverted = null; // Our job with this one is done or interrupted
            currentConversionProgress = 0f;
        }

        // If we lost our primary target and it was the one we were trying to convert, reset progress
        if (_targetGoatTransform != null && _targetGoatScript != null && _targetGoatScript == _goatBeingConverted)
        {
             if (!_targetGoatTransform.gameObject.activeInHierarchy || _targetGoatScript.currentState == PeacefulGoat.GoatState.Converting)
             {
                _targetGoatTransform = null;
                _targetGoatScript = null;
                _goatBeingConverted = null;
                currentConversionProgress = 0f;
             }
        }
        // If we are busy "charging" a conversion, maybe don't look for new targets yet or be less aggressive
        // For now, FindTarget will still run to pick a new target if the current one is lost.

        if (_targetGoatTransform && _targetGoatTransform.gameObject.activeInHierarchy) return; // Already have a valid target

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, detectionRadius, goatLayer);
        float closestDistance = Mathf.Infinity;
        PeacefulGoat closestGoatScript = null;

        foreach (var hitCollider in hitColliders)
        {
            PeacefulGoat potentialGoat = hitCollider.GetComponent<PeacefulGoat>();
            if (potentialGoat != null && potentialGoat.currentState != PeacefulGoat.GoatState.Converting)
            {
                float distanceToHit = Vector2.Distance(transform.position, hitCollider.transform.position);
                if (distanceToHit < closestDistance)
                {
                    closestDistance = distanceToHit;
                    closestGoatScript = potentialGoat;
                }
            }
        }

        if (closestGoatScript != null)
        {
            if (_targetGoatScript != closestGoatScript) // If target changed
            {
                ResetConversionProgress(); // Reset progress if target switches
                _goatBeingConverted = null;
            }
            _targetGoatScript = closestGoatScript;
            _targetGoatTransform = closestGoatScript.transform;
        }
        else
        {
            // If no target is found and we were targeting something, reset conversion
            if(_targetGoatTransform != null) ResetConversionProgress();
            _targetGoatTransform = null;
            _targetGoatScript = null;
            _goatBeingConverted = null;
        }
    }

    void Update()
    {
        if (currentKnockbackTime > 0)
        {
            currentKnockbackTime -= Time.deltaTime;
            return;
        }

        if (_targetGoatTransform != null && _targetGoatScript != null && _targetGoatScript.currentState != PeacefulGoat.GoatState.Converting)
        {
            ChaseTarget();
        }
        else
        {
            Wander();
        }
    }

    void ChaseTarget()
    {
        if (_targetGoatTransform == null) return;

        float distanceToTarget = Vector2.Distance(transform.position, _targetGoatTransform.position);
        float stoppingDistance = 0.6f; // Adjust so the enemy stops close enough to trigger/collide

        if (distanceToTarget > stoppingDistance)
        {
            Vector2 direction = ((Vector2)_targetGoatTransform.position - _rb.position).normalized;
            _rb.linearVelocity = direction * moveSpeed;
        }
        else
        {
            _rb.linearVelocity = Vector2.zero; // Stop when close enough
            // Conversion attempt will be handled by OnCollisionStay/OnTriggerStay
        }
    }

    void Wander()
    {
        _currentWanderTimer -= Time.deltaTime;
        float distanceToWanderPoint = Vector2.Distance(transform.position, _currentWanderTargetPoint);
        if (_currentWanderTimer <= 0f || distanceToWanderPoint < wanderPointReachedThreshold)
        {
            SetNewWanderTargetPoint();
        }
        Vector2 directionToWanderPoint = (_currentWanderTargetPoint - (Vector2)transform.position).normalized;
        _rb.linearVelocity = directionToWanderPoint * (moveSpeed * wanderSpeedMultiplier);
    }

    void SetNewWanderTargetPoint()
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized * Random.Range(wanderRadius * 0.5f, wanderRadius);
        _currentWanderTargetPoint = (Vector2)transform.position + randomDirection;
        _currentWanderTimer = Random.Range(wanderTimerDurationMin, wanderTimerDurationMax);
    }

    // --- COLLISION AND TRIGGER HANDLING FOR CONVERSION ---

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleContactStart(collision.gameObject);
        TryDamagePlayer(collision.gameObject);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        ProcessContact(collision.gameObject);
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        HandleContactEnd(collision.gameObject);
    }

    void HandleContactStart(GameObject contactObject)
    {
        if (_targetGoatTransform != null && contactObject == _targetGoatTransform.gameObject)
        {
            PeacefulGoat goat = contactObject.GetComponent<PeacefulGoat>();
            if (goat != null && goat.currentState != PeacefulGoat.GoatState.Converting)
            {
                _goatBeingConverted = goat; // Mark this goat as the one we are "charging up"
                // currentConversionProgress is NOT reset here, only on target switch or successful conversion/exit.
                // Debug.Log($"Contact started with target goat: {contactObject.name}");
            }
        }
    }

    void ProcessContact(GameObject contactObject)
    {
        // Only process conversion if the contact is with the goat we specifically targeted and started "charging"
        if (_goatBeingConverted != null && contactObject == _goatBeingConverted.gameObject)
        {
            if (_goatBeingConverted.currentState != PeacefulGoat.GoatState.Converting)
            {
                currentConversionProgress += Time.deltaTime;
                // Debug.Log($"Processing contact with {_goatBeingConverted.name}. Progress: {currentConversionProgress}/{conversionTimeToConvert}");
                if (currentConversionProgress >= conversionTimeToConvert)
                {
                    TryConvertGoat(_goatBeingConverted.gameObject); // Pass the specific goat we've been charging
                    // TryConvertGoat will reset target and progress
                }
            }
            else // Goat got converted by other means while we were charging
            {
                ResetConversionProgress();
            }
        }
    }

    void HandleContactEnd(GameObject contactObject)
    {
        // If we lose contact with the specific goat we were "charging up"
        if (_goatBeingConverted != null && contactObject == _goatBeingConverted.gameObject)
        {
            // Debug.Log($"Contact ended with target goat: {contactObject.name}. Resetting conversion progress.");
            ResetConversionProgress();
        }
    }

    void ResetConversionProgress()
    {
        currentConversionProgress = 0f;
        _goatBeingConverted = null;
    }


    void TryConvertGoat(GameObject collidedObject)
    {
        PeacefulGoat peacefulGoat = collidedObject.GetComponent<PeacefulGoat>();
        // This check is important: ensure it's the correct goat and it's still convertible
        if (peacefulGoat != null && peacefulGoat == _goatBeingConverted && peacefulGoat.currentState != PeacefulGoat.GoatState.Converting)
        {
            Debug.Log($"{gameObject.name} successfully converted {peacefulGoat.name} after {currentConversionProgress:F2}s of contact.");
            peacefulGoat.StartConversionProcess();

            // Reset states after successful conversion
            _targetGoatTransform = null;
            _targetGoatScript = null;
            ResetConversionProgress(); // This also nullifies _goatBeingConverted
            SetNewWanderTargetPoint();
        }
        else if (peacefulGoat != null && peacefulGoat != _goatBeingConverted)
        {
             // This case should ideally not happen if targeting is strict.
             // Means we tried to convert a goat we weren't "charging".
             Debug.LogWarning($"{gameObject.name} tried to convert {peacefulGoat.name} but was focusing on {_goatBeingConverted?.name}.");
        }
    }

    void TryDamagePlayer(GameObject collidedObject)
    {
        PlayerController playerController = collidedObject.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.TakeDamage(damageToPlayer);
        }
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        if (healthBarFill)
        {
            healthBarFill.fillAmount = (float)health / totalHealth;
        }
        if (health <= 0)
        {
            Die();
        }
    }

    public void ApplyKnockbackForce(Vector2 direction, float force)
    {
        if (_rb && _rb.bodyType == RigidbodyType2D.Dynamic)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.AddForce(direction * force, ForceMode2D.Impulse);
            currentKnockbackTime = knockbackDuration;
            ResetConversionProgress(); // Knockback should interrupt conversion charging
        }
    }

    void Die()
    {
        GameManager.Instance?.AddXP(xpValue);
        AISpawner spawner = FindFirstObjectByType<AISpawner>();
        if (spawner != null)
        {
            spawner.EnemyDefeated(this.gameObject);
        }
        Destroy(gameObject);
    }
}