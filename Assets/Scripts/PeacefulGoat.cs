using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

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

    // Stacking variables
    public float goatHeight = 0.5f;
    private PeacefulGoat goatBelowMe = null;
    private List<PeacefulGoat> goatsOnTopOfMe = new List<PeacefulGoat>();

    // Herding variables
    public float followPlayerSpeed = 2.5f;
    public float followDistanceToPlayer = 1.5f; // How close it tries to stay
    public float maxFollowDistance = 10f;       // Max distance before "too far" timer starts
    public float timeToUnherdWhenTooFar = 5f;   // Duration to be "too far" before unherding
    // public bool autoHerdOnPlayerContact = false; // Set to false if focused herding is primary

    private Transform playerTransformRef; // Renamed for clarity
    private Rigidbody2D rb;
    private Collider2D mainCollider;

    public enum GoatState { Wandering, Stacked, Herded, Converting, BeingFocusedForHerding }
    public GoatState currentState = GoatState.Wandering;

    private bool isBaseOfStack = true;
    private bool isStackedOnAnother = false;

    private float currentHerdingFocusProgress = 0f;
    private float totalHerdingFocusDuration = 0f;

    private float timeSpentTooFarFromPlayer = 0f;
    private float distanceCheckInterval = 0.5f; // How often to check distance when herded
    private float currentDistanceCheckTimer = 0f;


    public GameObject cyborgGoatPrefab;
    private bool turnedIntoCyborgForReal = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCollider = GetComponent<Collider2D>();

        if (rb.bodyType != RigidbodyType2D.Kinematic)
        {
            rb.gravityScale = 0;
        }
        initialPosition = transform.position;
        SetNewWanderTarget();

        // Find player transform for following
        if (PlayerController.Instance != null) // Use the singleton instance
        {
            playerTransformRef = PlayerController.Instance.transform;
        }
        else
        {
            Debug.LogWarning("PeacefulGoat: PlayerController.Instance not found! Herding/Following might not work correctly.");
            // Fallback if needed, though singleton is preferred
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransformRef = playerObj.transform;
        }


        GameManager.Instance?.RegisterPeacefulGoat();

        if (herdingUICanvas != null) herdingUICanvas.gameObject.SetActive(false);
        if (herdingProgressFillImage != null) herdingProgressFillImage.fillAmount = 0;

        UpdateVisualState();
    }

    void Update()
    {
        switch (currentState)
        {
            case GoatState.Wandering:
                Wander();
                break;
            case GoatState.Stacked:
                if (isBaseOfStack) Wander();
                else if (goatBelowMe != null) UpdateStackedPosition();
                break;
            case GoatState.Herded:
                FollowPlayer();
                CheckIfTooFarFromPlayer(); // New check
                break;
            case GoatState.BeingFocusedForHerding:
                // UI progress is handled externally by player calls
                break;
            case GoatState.Converting:
                // Conversion animation/logic
                break;
        }

        // Ensure World Space UI faces camera (optional, simple version)
        if (currentState == GoatState.BeingFocusedForHerding && herdingUICanvas != null && Camera.main != null)
        {
            herdingUICanvas.transform.rotation = Quaternion.LookRotation(herdingUICanvas.transform.position - Camera.main.transform.position);
        }
    }

    void FixedUpdate()
    {
        // Movement logic for dynamic rigidbodies
        if (rb != null && !rb.isKinematic)
        {
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
                    rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 5f);
                }
            }
            else
            {
                // rb.velocity = Vector2.zero; // Stop if no specific movement logic for other states
            }
        }
    }


    #region Movement Behaviours
    void Wander()
    {
        // For kinematic or transform-based movement
        if (rb == null || rb.isKinematic) {
             transform.position = Vector2.MoveTowards(transform.position, wanderTarget, wanderSpeed * Time.deltaTime);
        }
        // For dynamic rigidbody, velocity is set in FixedUpdate

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
            Debug.LogWarning($"{gameObject.name} trying to follow, but playerTransformRef is null. Becoming wandering.");
            BecomeWandering(); // No player to follow
            return;
        }

        // For kinematic or transform-based movement
        if (rb == null || rb.isKinematic)
        {
            if (Vector2.Distance(transform.position, playerTransformRef.position) > followDistanceToPlayer)
            {
                Vector2 direction = ((Vector2)playerTransformRef.position - (Vector2)transform.position).normalized;
                transform.position += (Vector3)direction * followPlayerSpeed * Time.deltaTime;
            }
        }
        // For dynamic rigidbody, velocity is set in FixedUpdate
    }

    void CheckIfTooFarFromPlayer()
    {
        currentDistanceCheckTimer -= Time.deltaTime;
        if (currentDistanceCheckTimer <= 0f)
        {
            currentDistanceCheckTimer = distanceCheckInterval; // Reset cooldown

            if (playerTransformRef == null)
            {
                BecomeWandering(); // Player disappeared
                return;
            }

            float distanceSqr = (transform.position - playerTransformRef.position).sqrMagnitude;
            if (distanceSqr > maxFollowDistance * maxFollowDistance)
            {
                timeSpentTooFarFromPlayer += distanceCheckInterval;
                // Debug.Log($"{gameObject.name} is too far from player. Time spent too far: {timeSpentTooFarFromPlayer}");
                if (timeSpentTooFarFromPlayer >= timeToUnherdWhenTooFar)
                {
                    Debug.Log($"{gameObject.name} has been too far for too long. Unherding.");
                    BecomeWandering(); // This will also notify PlayerController
                }
            }
            else
            {
                timeSpentTooFarFromPlayer = 0f; // Reset if back in range
            }
        }
    }
    #endregion

    #region State Transitions and Player Focus
    public void StartPlayerHerdingFocus(float totalFocusTime)
    {
        if (currentState == GoatState.Wandering || currentState == GoatState.Stacked)
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
            if (isStackedOnAnother || goatsOnTopOfMe.Any()) currentState = GoatState.Stacked;
            else currentState = GoatState.Wandering;
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
            BecomeHerded(); // This sets the state and calls PlayerController.Instance.AddGoatToHerd
        }
    }

    public void BecomeHerded()
    {
        if (currentState == GoatState.Herded || currentState == GoatState.Converting) return;
        GoatState previousState = currentState;
        currentState = GoatState.Herded;
        Debug.Log(gameObject.name + " became herded.");

        if (PlayerController.Instance != null) // Notify PlayerController to add to its list
        {
            PlayerController.Instance.AddGoatToHerd(this);
        }

        timeSpentTooFarFromPlayer = 0f; // Reset this timer when becoming herded
        currentDistanceCheckTimer = distanceCheckInterval; // Start checking distance soon

        if (previousState == GoatState.Stacked || isStackedOnAnother || goatsOnTopOfMe.Any()) UnstackFully();
        if (mainCollider != null) mainCollider.isTrigger = true;
        if (rb != null) { rb.isKinematic = false; rb.mass = 0.5f; }
        UpdateVisualState();
    }

    public void BecomeWandering() // Can be called by self (too far) or externally
    {
        if (currentState == GoatState.Wandering || currentState == GoatState.Converting) return;

        GoatState oldState = currentState;
        currentState = GoatState.Wandering;
        Debug.Log($"{gameObject.name} is now wandering (was {oldState}).");

        if (oldState == GoatState.Herded && PlayerController.Instance != null) // If it was herded, notify PlayerController
        {
            PlayerController.Instance.RemoveGoatFromHerd(this);
        }

        timeSpentTooFarFromPlayer = 0f; // Reset this specific timer
        if (mainCollider != null) mainCollider.isTrigger = false;
        if (rb != null) rb.mass = 1f;
        SetNewWanderTarget();
        UpdateVisualState();
    }
    #endregion

    // Stacking Logic (largely unchanged, ensure state checks are robust)
    #region Stacking Logic
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (currentState == GoatState.Converting || currentState == GoatState.Herded || currentState == GoatState.BeingFocusedForHerding) return;

        // Optional: autoHerdOnPlayerContact logic removed for clarity if focused herding is primary
        // If you want it, re-add checks similar to previous versions.

        if (currentState == GoatState.Wandering || (currentState == GoatState.Stacked && isBaseOfStack))
        {
            PeacefulGoat otherGoat = collision.gameObject.GetComponent<PeacefulGoat>();
            if (otherGoat != null && otherGoat != this &&
                otherGoat.currentState != GoatState.Herded &&
                otherGoat.currentState != GoatState.Converting &&
                otherGoat.currentState != GoatState.BeingFocusedForHerding)
            {
                ContactPoint2D[] contacts = new ContactPoint2D[collision.contactCount];
                collision.GetContacts(contacts);
                foreach (ContactPoint2D contact in contacts)
                {
                    if (Vector2.Dot(contact.normal, Vector2.up) > 0.7f) // This goat's bottom hit something
                    {
                        TryStackOn(otherGoat.GetBaseOfStack());
                        break;
                    }
                }
            }
        }
    }
    void TryStackOn(PeacefulGoat bottomGoatCandidate)
    {
        if (currentState == GoatState.Stacked && isStackedOnAnother) return;
        if (bottomGoatCandidate == this || bottomGoatCandidate.currentState == GoatState.Herded || bottomGoatCandidate.currentState == GoatState.BeingFocusedForHerding) return;

        currentState = GoatState.Stacked; isStackedOnAnother = true; isBaseOfStack = false;
        goatBelowMe = bottomGoatCandidate; bottomGoatCandidate.AddGoatOnTop(this);
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.isKinematic = true; }
        UpdateStackedPosition(); UpdateVisualState();
    }
    public void AddGoatOnTop(PeacefulGoat goatToAdd)
    {
        if (!goatsOnTopOfMe.Contains(goatToAdd))
        {
            goatsOnTopOfMe.Add(goatToAdd); goatToAdd.goatBelowMe = this;
            goatToAdd.currentState = GoatState.Stacked; goatToAdd.isStackedOnAnother = true; goatToAdd.isBaseOfStack = false;
            if (goatToAdd.rb != null) goatToAdd.rb.isKinematic = true;
            if (currentState != GoatState.Stacked) currentState = GoatState.Stacked;
            if (rb != null && !rb.isKinematic && goatBelowMe != null) rb.isKinematic = true;
            UpdateVisualState(); goatToAdd.UpdateVisualState();
        }
    }
    public void UpdateStackedPosition()
    {
        if (goatBelowMe != null && currentState == GoatState.Stacked && isStackedOnAnother)
        {
            int myIndexInStackAboveGoatBelow = goatBelowMe.goatsOnTopOfMe.IndexOf(this);
            if (myIndexInStackAboveGoatBelow != -1)
            {
                 transform.position = goatBelowMe.transform.position + Vector3.up * goatHeight * (myIndexInStackAboveGoatBelow + 1);
                 transform.rotation = goatBelowMe.transform.rotation;
            }
        }
    }
    private void UnstackFully()
    {
        if (goatBelowMe != null) { goatBelowMe.goatsOnTopOfMe.Remove(this); goatBelowMe = null; }
        foreach (var goatAbove in new List<PeacefulGoat>(goatsOnTopOfMe)) { goatAbove?.BecomeBaseAndWander(); }
        goatsOnTopOfMe.Clear();
        isStackedOnAnother = false; isBaseOfStack = true;
        if (rb != null) rb.isKinematic = false; // Allow physics again
    }
    public void BecomeBaseAndWander()
    {
        goatBelowMe = null; isStackedOnAnother = false; isBaseOfStack = true;
        currentState = GoatState.Wandering;
        if (rb != null) rb.isKinematic = false;
        SetNewWanderTarget(); UpdateVisualState();
    }
    public PeacefulGoat GetBaseOfStack()
    {
        if (isBaseOfStack || goatBelowMe == null) return this;
        return goatBelowMe.GetBaseOfStack();
    }
    #endregion

    #region Conversion
    public void StartConversionProcess() // Called by enemy
    {
        if (currentState == GoatState.Converting) return;
        Debug.Log(gameObject.name + " is being converted by enemy!");
        GoatState prevState = currentState;
        currentState = GoatState.Converting;
        turnedIntoCyborgForReal = true;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (prevState == GoatState.Herded && PlayerController.Instance != null) PlayerController.Instance.RemoveGoatFromHerd(this); // Remove from herd if converting
        if (prevState == GoatState.BeingFocusedForHerding && herdingUICanvas != null) herdingUICanvas.gameObject.SetActive(false);
        UnstackFully();
        Invoke("CompleteConversion", 0.1f); // Small delay for visual state change
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

    void UpdateVisualState() { /* Optional: Change sprite/color based on state */ }

    void OnDestroy()
    {
        if (currentState == GoatState.Herded && PlayerController.Instance != null) PlayerController.Instance.RemoveGoatFromHerd(this); // Ensure removal if destroyed while herded
        if (isStackedOnAnother && goatBelowMe != null) goatBelowMe.goatsOnTopOfMe.Remove(this);
        foreach (var goatAbove in goatsOnTopOfMe) { goatAbove?.BecomeBaseAndWander(); }
        if (!turnedIntoCyborgForReal) GameManager.Instance?.UnregisterPeacefulGoat();
    }
}