using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
// No longer need System.Linq if stacking is fully removed and no OrderBy is used.

public class PeacefulGoat : MonoBehaviour
{
    // UI for Herding Progress
    public Canvas herdingUICanvas;
    public Image herdingProgressFillImage;

    // Wander variables
    public float wanderSpeed = 1.5f;
    public float wanderRadius = 5f;
    public float minWanderTime = 2f;
    public float maxWanderTime = 5f;
    private Vector2 wanderTarget;
    private float wanderTimer;
    private Vector3 initialPosition;

    // --- REMOVED STACKING VARIABLES ---
    // public float goatHeight = 0.5f;
    // private PeacefulGoat goatBelowMe = null;
    // private List<PeacefulGoat> goatsOnTopOfMe = new List<PeacefulGoat>();

    // Herding variables
    public float followPlayerSpeed = 2.5f;
    public float followDistanceToPlayer = 1.5f;
    public float maxFollowDistance = 10f;
    public float timeToUnherdWhenTooFar = 5f;

    // Separation variables
    public float separationRadius = 1.0f; // How close other goats need to be to push away
    public float separationForce = 5.0f;  // Strength of the push
    public LayerMask peacefulGoatLayer;   // Set this in Inspector to the layer PeacefulGoats are on
    private float separationCheckInterval = 0.1f; // How often to apply separation
    private float currentSeparationCheckTimer = 0f;

    private Transform playerTransformRef;
    private Rigidbody2D rb;
    private Collider2D mainCollider;

    // Simplified states without 'Stacked'
    public enum GoatState { Wandering, Herded, Converting, BeingFocusedForHerding }
    public GoatState currentState = GoatState.Wandering;

    // --- REMOVED STACKING STATE FLAGS ---
    // private bool isBaseOfStack = true;
    // private bool isStackedOnAnother = false;

    private float currentHerdingFocusProgress = 0f;
    private float totalHerdingFocusDuration = 0f;
    private float timeSpentTooFarFromPlayer = 0f;
    private float distanceCheckInterval = 0.5f;
    private float currentDistanceCheckTimer = 0f;

    public GameObject cyborgGoatPrefab;
    private bool turnedIntoCyborgForReal = false;



    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCollider = GetComponent<Collider2D>();

        if (rb != null) // Ensure Rigidbody exists
        {
            if (rb.bodyType != RigidbodyType2D.Kinematic)
            {
                rb.gravityScale = 0;
            }
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        else
        {
            Debug.LogError($"{gameObject.name} is missing a Rigidbody2D component needed for movement and separation!");
        }

        initialPosition = transform.position;
        SetNewWanderTarget();

        if (PlayerController.Instance != null)
        {
            playerTransformRef = PlayerController.Instance.transform;
        }
        else
        {
            Debug.LogWarning("PeacefulGoat: PlayerController.Instance not found! Herding/Following might not work correctly.");
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransformRef = playerObj.transform;
        }

        GameManager.Instance?.RegisterPeacefulGoat();

        if (herdingUICanvas != null) herdingUICanvas.gameObject.SetActive(false);
        if (herdingProgressFillImage != null) herdingProgressFillImage.fillAmount = 0;

        currentSeparationCheckTimer = Random.Range(0, separationCheckInterval); // Stagger initial checks slightly

        UpdateVisualState();
    }

    // function to push goats away from each other
    void ApplySeparationForce()
    {
        if (rb == null || rb.isKinematic) return; // Only apply if not kinematic

        Collider2D[] nearbyGoats = Physics2D.OverlapCircleAll(transform.position, separationRadius, peacefulGoatLayer);
        Vector2 separationVector = Vector2.zero;

        foreach (Collider2D goat in nearbyGoats)
        {
            if (goat != null && goat != mainCollider) // Avoid self-collision
            {
                Vector2 direction = (Vector2)transform.position - (Vector2)goat.transform.position;
                float distance = direction.magnitude;
                if (distance < separationRadius && distance > 0f)
                {
                    separationVector += direction.normalized * separationForce / distance; // Inverse distance for stronger push when closer
                }
            }
        }

        rb.AddForce(separationVector, ForceMode2D.Force);
    }

    void Update()
    {
        switch (currentState)
        {
            case GoatState.Wandering:
                Wander();
                break;
            // --- REMOVED STACKED STATE ---
            // case GoatState.Stacked:
            // if (isBaseOfStack) Wander();
            // else if (goatBelowMe != null) UpdateStackedPosition();
            // break;
            case GoatState.Herded:
                FollowPlayer();
                CheckIfTooFarFromPlayer();
                break;
            case GoatState.BeingFocusedForHerding:
                break;
            case GoatState.Converting:
                break;
        }

        if (currentState == GoatState.BeingFocusedForHerding && herdingUICanvas != null && Camera.main != null)
        {
            herdingUICanvas.transform.rotation = Quaternion.LookRotation(herdingUICanvas.transform.position - Camera.main.transform.position);
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        


        if (!rb.isKinematic)
        {
            currentSeparationCheckTimer -= Time.fixedDeltaTime;
            if (currentSeparationCheckTimer <= 0f)
            {
                currentSeparationCheckTimer = separationCheckInterval;
                ApplySeparationForce();
            }

            if (currentState == GoatState.Wandering)
            {
                Vector2 direction = (wanderTarget - (Vector2)transform.position).normalized;
                rb.linearVelocity = direction * wanderSpeed;
                if (Vector2.Distance(transform.position, wanderTarget) < 0.2f)
                {
                    SetNewWanderTarget();
                }
            }
            else if (currentState == GoatState.Herded && playerTransformRef != null)
            {
                if (Vector2.Distance(transform.position, playerTransformRef.position) > followDistanceToPlayer)
                {
                    Vector2 directionToPlayer = ((Vector2)playerTransformRef.position - rb.position).normalized;
                    rb.linearVelocity = directionToPlayer * followPlayerSpeed;
                }
                else
                {
                    rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 10f); // Increased lerp factor for quicker stop
                }
            }
            else if (currentState != GoatState.BeingFocusedForHerding && currentState != GoatState.Converting) // Don't halt if being focused or converting
            {
                 // If not wandering or herded, and dynamic, consider what its velocity should be.
                 // For now, let it be affected by separation or other forces.
                 // To make it stop if not actively moving:
                 // rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, Time.fixedDeltaTime * 2f);
            }
        }
    }


    #region Movement Behaviours (Wander, FollowPlayer, Separation)
    void Wander()
    {
        if (rb == null || rb.isKinematic)
        {
            transform.position = Vector2.MoveTowards(transform.position, wanderTarget, wanderSpeed * Time.deltaTime);
        }
        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f || Vector2.Distance(transform.position, wanderTarget) < 0.1f)
        {
            SetNewWanderTarget();
        }
    }

    void SetNewWanderTarget()
    {
        wanderTarget = initialPosition + (Vector3)(Random.insideUnitCircle * wanderRadius);
        wanderTimer = Random.Range(minWanderTime, maxWanderTime);
    }

    void FollowPlayer()
    {
        if (playerTransformRef == null)
        {
            BecomeWandering();
            return;
        }
        if (rb == null || rb.isKinematic)
        {
            if (Vector2.Distance(transform.position, playerTransformRef.position) > followDistanceToPlayer)
            {
                Vector2 direction = ((Vector2)playerTransformRef.position - (Vector2)transform.position).normalized;
                transform.position += (Vector3)direction * followPlayerSpeed * Time.deltaTime;
            }
        }
    }

    void CheckIfTooFarFromPlayer()
    {
        currentDistanceCheckTimer -= Time.deltaTime;
        if (currentDistanceCheckTimer <= 0f)
        {
            currentDistanceCheckTimer = distanceCheckInterval;
            if (playerTransformRef == null)
            {
                BecomeWandering();
                return;
            }
            float distanceSqr = (transform.position - playerTransformRef.position).sqrMagnitude;
            if (distanceSqr > maxFollowDistance * maxFollowDistance)
            {
                timeSpentTooFarFromPlayer += distanceCheckInterval;
                if (timeSpentTooFarFromPlayer >= timeToUnherdWhenTooFar)
                {
                    BecomeWandering();
                }
            }
            else
            {
                timeSpentTooFarFromPlayer = 0f;
            }
        }
    }
    #endregion

    #region State Transitions and Player Focus
    public void StartPlayerHerdingFocus(float totalFocusTime)
    {
        // Can only be focused if Wandering (no longer Stacked as an option here)
        if (currentState == GoatState.Wandering)
        {
            currentState = GoatState.BeingFocusedForHerding;
            totalHerdingFocusDuration = totalFocusTime;
            currentHerdingFocusProgress = 0f;
            if (herdingUICanvas != null) herdingUICanvas.gameObject.SetActive(true);
            if (herdingProgressFillImage != null) herdingProgressFillImage.fillAmount = 0;
            UpdateVisualState();
        }
    }

    public void UpdatePlayerHerdingFocusProgress(float deltaTime)
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
            currentState = GoatState.Wandering;
            if (herdingUICanvas != null) herdingUICanvas.gameObject.SetActive(false);
            currentHerdingFocusProgress = 0f;
            UpdateVisualState();
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
        // GoatState previousState = currentState; // No longer need to check for previous Stacked state specifically
        currentState = GoatState.Herded;
        Debug.Log(gameObject.name + " became herded.");

        if (PlayerController.Instance != null) PlayerController.Instance.AddGoatToHerd(this);
        timeSpentTooFarFromPlayer = 0f;
        currentDistanceCheckTimer = distanceCheckInterval;


        if (rb != null) { rb.bodyType = RigidbodyType2D.Dynamic; rb.mass = 0.5f; rb.gravityScale = 0; } // Ensure dynamic for forces
        UpdateVisualState();
    }

    public void BecomeWandering()
    {
        if (currentState == GoatState.Wandering || currentState == GoatState.Converting) return;
        GoatState oldState = currentState;
        currentState = GoatState.Wandering;

        if (oldState == GoatState.Herded && PlayerController.Instance != null) PlayerController.Instance.RemoveGoatFromHerd(this);
        timeSpentTooFarFromPlayer = 0f;
        if (mainCollider != null) mainCollider.isTrigger = false; // Make it solid again when wandering
        if (rb != null) { rb.mass = 1f; rb.bodyType = RigidbodyType2D.Dynamic; } // Ensure dynamic for wander physics
        SetNewWanderTarget();
        UpdateVisualState();
    }
    #endregion
    void OnCollisionEnter2D(Collision2D collision)
    {
        // If you want wandering goats to have some physical reaction to bumping each other (besides separation force)
        // you can add it here, but be careful it doesn't fight the separation.
        // For now, let separation handle inter-goat pushing.
        // Example: if (currentState == GoatState.Wandering && collision.gameObject.GetComponent<PeacefulGoat>() != null) { // maybe a small bounce }
    }

    #region Conversion
    public void StartConversionProcess()
    {
        if (currentState == GoatState.Converting) return;
        GoatState prevState = currentState;
        currentState = GoatState.Converting;
        turnedIntoCyborgForReal = true;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (prevState == GoatState.Herded && PlayerController.Instance != null) PlayerController.Instance.RemoveGoatFromHerd(this);
        if (prevState == GoatState.BeingFocusedForHerding && herdingUICanvas != null) herdingUICanvas.gameObject.SetActive(false);
        CompleteConversion();
        UpdateVisualState();
    }
    private void CompleteConversion()
    {
        GameManager.Instance?.GoatConvertedToCyborg(); 
        if (cyborgGoatPrefab != null) Instantiate(cyborgGoatPrefab, transform.position, transform.rotation);
        else Debug.LogError("CyborgGoatPrefab not assigned to " + gameObject.name);
        Destroy(gameObject);
    }
    #endregion

    void UpdateVisualState() { /* Optional: Change sprite based on state, or for debugging */ }

    void OnDestroy()
    {
        if (currentState == GoatState.Herded && PlayerController.Instance != null) PlayerController.Instance.RemoveGoatFromHerd(this);
        if (!turnedIntoCyborgForReal) GameManager.Instance?.UnregisterPeacefulGoat(); 
    }
}