using UnityEngine;

public abstract class BaseAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Base movement speed for the AI.")]
    public float moveSpeed = 3f;
    [Tooltip("Multiplier for speed when the AI is wandering.")]
    public float wanderSpeedMultiplier = 0.6f;

    [Header("Wandering Behavior")]
    [Tooltip("Radius within which the AI will pick new wander destinations.")]
    public float wanderRadius = 5f;
    [Tooltip("Minimum time the AI will move towards a wander destination.")]
    public float minWanderTime = 2f;
    [Tooltip("Maximum time the AI will move towards a wander destination.")]
    public float maxWanderTime = 5f;
    [Tooltip("How close the AI needs to be to its wander target to consider it reached.")]
    public float wanderPointReachedThreshold = 0.5f;
    [Tooltip("If true, AI wanders around its initial spawn position. If false, wanders around its current position when a new point is chosen.")]
    public bool wanderAroundInitialPosition = true;

    [Header("Visuals")]
    [Tooltip("Reference to the child Transform that holds the AI's main visuals (for flipping). If null, attempts to find 'Visuals' child or defaults to this transform.")]
    [SerializeField] protected Transform visualsTransform;

    [Header("Map Boundaries")]
    [Tooltip("Should the AI be restricted by map boundaries?")]
    public bool useMapBoundaries = true;
    [Tooltip("Minimum X and Y coordinates the AI can reach.")]
    public Vector2 mapMinBounds = new Vector2(0, 0); // Example: Lower-left corner
    [Tooltip("Maximum X and Y coordinates the AI can reach.")]
    public Vector2 mapMaxBounds = new Vector2(50f, 50f);   // Example: Upper-right corner

    protected Rigidbody2D rb;
    protected Animator animator; // Optional: Assign in Inspector or will try to find

    protected Vector2 currentWanderDestination;
    protected float currentWanderTimer;
    protected Vector3 _initialPosition; // Stores the position at Start

    protected float knockbackActiveDuration = 0f; // Total duration of current knockback
    protected float currentKnockbackTimer = 0f;   // Countdown for knockback


    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Attempt to get Animator if not assigned
        if (animator == null) animator = GetComponent<Animator>();
        if (visualsTransform != null && animator == null) animator = visualsTransform.GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Setup visualsTransform
        if (visualsTransform == null)
        {
            Transform foundVisuals = transform.Find("Visuals");
            if (foundVisuals != null)
            {
                visualsTransform = foundVisuals;
            }
            else
            {
                // Fallback: use the main transform. This might not be ideal for complex hierarchies.
                visualsTransform = transform;
                // Debug.LogWarning($"VisualsTransform not set on {gameObject.name} and no 'Visuals' child found. Defaulting to root transform. Sprite flipping might affect children unexpectedly.", this);
            }
        }
    }

    protected virtual void Start()
    {
        if (rb != null)
        {
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        else
        {
            Debug.LogError($"Rigidbody2D not found on {gameObject.name}. AI movement and boundary checks will not work correctly.", this);
        }
        _initialPosition = transform.position;
        SetNewWanderDestination();
    }

    protected virtual void Update()
    {
        if (currentKnockbackTimer > 0)
        {
            currentKnockbackTimer -= Time.deltaTime;
            if (currentKnockbackTimer <= 0)
            {
                OnKnockbackEnd();
            }
            // During knockback, usually no other logic (like state changes or new movements) should run.
            return;
        }
        // Derived classes can add non-physics updates here.
    }

    protected virtual void FixedUpdate()
    {
        if (currentKnockbackTimer > 0 || rb == null)
        {
            // If being knocked back, velocity is typically handled by ApplyKnockback.
            // If no Rigidbody, can't move or apply constraints.
            return;
        }

        // IMPORTANT: Derived classes should implement their movement logic (setting rb.velocity or calling rb.MovePosition)
        // in their own FixedUpdate BEFORE calling base.FixedUpdate() OR this base FixedUpdate will apply constraints
        // to whatever position/velocity was set.

        if (useMapBoundaries)
        {
            ApplyBoundaryConstraints();
        }
    }

    protected virtual void SetNewWanderDestination()
    {
        Vector2 wanderOrigin = wanderAroundInitialPosition ? (Vector2)_initialPosition : rb.position;
        float randomAngle = Random.Range(0f, 2f * Mathf.PI);
        // Ensure a minimum wander distance to prevent tiny movements, but also respect wanderRadius
        float randomDistance = Random.Range(Mathf.Min(wanderRadius * 0.25f, wanderRadius), wanderRadius);
        Vector2 offset = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * randomDistance;
        currentWanderDestination = wanderOrigin + offset;

        if (useMapBoundaries)
        {
            currentWanderDestination = ClampToBoundaries(currentWanderDestination);
        }
        currentWanderTimer = Random.Range(minWanderTime, maxWanderTime);
    }

    protected virtual void PerformWanderBehavior()
    {
        if (rb == null || currentKnockbackTimer > 0) return;

        currentWanderTimer -= Time.fixedDeltaTime;

        Vector2 directionToTarget = (currentWanderDestination - rb.position).normalized;
        float distanceToTarget = Vector2.Distance(rb.position, currentWanderDestination);

        if (distanceToTarget > wanderPointReachedThreshold)
        {
            rb.linearVelocity = directionToTarget * (moveSpeed * wanderSpeedMultiplier);
            FaceDirection(currentWanderDestination);
        }
        else
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 10f); // Smooth stop
        }

        if (currentWanderTimer <= 0f || distanceToTarget <= wanderPointReachedThreshold)
        {
            SetNewWanderDestination();
        }
    }

    protected virtual void MoveTowardsTarget(Vector2 targetPosition, float speed)
    {
        if (rb == null || currentKnockbackTimer > 0) return;

        Vector2 direction = (targetPosition - rb.position).normalized;
        rb.linearVelocity = direction * speed;
        FaceDirection(targetPosition);
    }

    protected virtual void StopMovement()
    {
        if (rb == null) return;
        rb.linearVelocity = Vector2.zero;
    }

    protected virtual void ApplyBoundaryConstraints()
    {
        if (rb == null || !useMapBoundaries) return;

        Vector2 currentPosition = rb.position;
        Vector2 clampedPosition = ClampToBoundaries(currentPosition);

        if (currentPosition != clampedPosition)
        {
            rb.position = clampedPosition;
            // If the AI was moved due to clamping, also stop its velocity to prevent
            // it from trying to push against the boundary or jittering.
            if ((clampedPosition - currentPosition).sqrMagnitude > 0.0001f) // If a clamp actually occurred
            {
                 rb.linearVelocity = Vector2.zero;
            }
        }
    }

    protected Vector2 ClampToBoundaries(Vector2 position)
    {
        float clampedX = Mathf.Clamp(position.x, mapMinBounds.x, mapMaxBounds.x);
        float clampedY = Mathf.Clamp(position.y, mapMinBounds.y, mapMaxBounds.y);
        return new Vector2(clampedX, clampedY);
    }

    protected virtual void FaceDirection(Vector2 targetPosition)
    {
        if (visualsTransform == null || rb == null) return;

        Vector2 direction;
        if (rb.linearVelocity.sqrMagnitude > 0.01f) // If moving significantly, use velocity direction
        {
            direction = rb.linearVelocity.normalized;
        }
        else // If not moving (or very slowly), face the explicit target position
        {
            if ((targetPosition - rb.position).sqrMagnitude < 0.001f) return; // Already at target, no direction
            direction = (targetPosition - rb.position).normalized;
        }

        if (Mathf.Abs(direction.x) > 0.01f)
        {
            Vector3 currentScale = visualsTransform.localScale;
            if (direction.x < 0 && currentScale.x > 0) // Moving/facing left, visuals currently right
            {
                currentScale.x *= -1;
            }
            else if (direction.x > 0 && currentScale.x < 0) // Moving/facing right, visuals currently left
            {
                currentScale.x *= -1;
            }
            visualsTransform.localScale = currentScale;
        }
    }

    public virtual void ApplyKnockback(Vector2 direction, float force, float duration)
    {
        if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
        {
            rb.linearVelocity = Vector2.zero; // Stop current movement before applying force
            rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);
            currentKnockbackTimer = duration;
            knockbackActiveDuration = duration;
            OnKnockbackStart();
        }
    }

    protected virtual void OnKnockbackStart()
    {
        // Base implementation can be empty or log. Derived classes override for specific reactions.
        // Debug.Log($"{gameObject.name} knockback started.");
      //  if (rb != null) rb.linearVelocity = Vector2.zero; // Ensure velocity is killed if AddForce isn't perfectly replacing it.
    }

    protected virtual void OnKnockbackEnd()
    {
        // Base implementation. Derived classes can call base.OnKnockbackEnd() then add logic.
        // Debug.Log($"{gameObject.name} knockback ended.");
        if (rb != null) rb.linearVelocity = Vector2.zero; // Ensure AI is stopped after knockback.
    }
}