using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Added for LINQ operations like Count

public class PeacefulGoat : MonoBehaviour
{
    // Existing wander variables
    public float wanderSpeed = 1.5f;
    public float wanderRadius = 5f;
    public float minWanderTime = 2f;
    public float maxWanderTime = 5f;
    private Vector2 _wanderTarget;
    private float _wanderTimer;
    private Vector3 _initialPosition;

    // Stacking variables
    public float goatHeight = 0.5f;
    private PeacefulGoat _goatBelowMe = null;
    private readonly List<PeacefulGoat> _goatsOnTopOfMe = new List<PeacefulGoat>();

    // Herding variables
    public float followPlayerSpeed = 2.5f;
    public float followDistanceToPlayer = 1.5f; // How close it tries to get
    public float herdDetectionRadiusPlayer = 1.0f; // How close player needs to be to start herding (on interaction)
    public bool autoHerdOnPlayerContact = true; // If true, touching a non-stacked goat herds it

    private Transform _playerTransform;
    private Rigidbody2D _rb;
    private Collider2D _mainCollider; // We'll cache this

    public enum GoatState { Wandering, Stacked, Herded, Converting }
    public GoatState currentState = GoatState.Wandering;

    private bool isBaseOfStack = true;
    private bool isStackedOnAnother = false; // True if this goat is on top of another

    // For conversion
    public GameObject cyborgGoatPrefab; // Assign your Cyborg Goat prefab here
    private bool _turnedIntoCyborgForReal = false;

    public PeacefulGoat(float wanderTimer)
    {
        this._wanderTimer = wanderTimer;
    }


    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _mainCollider = GetComponent<Collider2D>(); // Cache the main collider

        if (_rb.bodyType != RigidbodyType2D.Kinematic)
        {
            _rb.gravityScale = 0;
        }
        _initialPosition = transform.position;
        SetNewWanderTarget();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("PeacefulGoat: Player not found! Herding will not work.");
        }

        GameManager.Instance?.RegisterPeacefulGoat();
        UpdateVisualState();
    }

    void Update()
    {
        switch (currentState)
        {
            case GoatState.Wandering:
                Wander();
                // Player can initiate herding via proximity/interaction here if not autoHerdOnPlayerContact
                break;
            case GoatState.Stacked:
                // Movement is controlled by the base of the stack or this goat if it's the base
                if (isBaseOfStack)
                {
                    Wander(); // Base of stack can still wander
                }
                else if (_goatBelowMe != null)
                {
                    // Follow the goat below it precisely, accounting for the full stack height
                    UpdateStackedPosition();
                }
                break;
            case GoatState.Herded:
                FollowPlayer();
                break;
            case GoatState.Converting:
                // Play converting animation/effect, then switch to cyborg
                break;
        }
    }

    void FixedUpdate()
    {
        // Using FixedUpdate for Rigidbody movement if not kinematic
        if (currentState == GoatState.Wandering && _rb && _rb.bodyType != RigidbodyType2D.Kinematic)
        {
            Vector2 direction = (_wanderTarget - (Vector2)transform.position).normalized;
            _rb.linearVelocity = direction * wanderSpeed;
            if (Vector2.Distance(transform.position, _wanderTarget) < 0.2f)
            {
                SetNewWanderTarget(); // Get new target sooner if moving with Rigidbody
            }
        }
        else if (currentState == GoatState.Herded && _playerTransform && _rb && _rb.bodyType != RigidbodyType2D.Kinematic)
        {
            if (Vector2.Distance(transform.position, _playerTransform.position) > followDistanceToPlayer)
            {
                Vector2 directionToPlayer = ((Vector2)_playerTransform.position - _rb.position).normalized;
                _rb.linearVelocity = directionToPlayer * followPlayerSpeed;
            }
            else
            {
                _rb.linearVelocity = Vector2.Lerp(_rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 5f); // Slow down smoothly
            }
        }
        else if (_rb && _rb.bodyType != RigidbodyType2D.Kinematic)
        {
            // rb.velocity = Vector2.zero; // Stop if no specific movement logic
        }
    }


    #region State Transitions
    public void BecomeHerded()
    {
        if (currentState == GoatState.Herded || currentState == GoatState.Converting) return;

        Debug.Log(gameObject.name + " is now herded.");
        currentState = GoatState.Herded;

        // If it was part of a stack, unstack it
        if (isStackedOnAnother || _goatsOnTopOfMe.Any())
        {
            UnstackFully();
        }

        if (_mainCollider != null)
        {
            _mainCollider.isTrigger = true; // Turn off solid hitbox
            Debug.Log(gameObject.name + " main collider set to trigger.");
        }
        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Dynamic; // Ensure it can move with its own velocity if using dynamic RB
            _rb.mass = 0.5f; // Herded goats might be lighter to follow more easily
        }
        UpdateVisualState();
    }

    public void BecomeWandering() // e.g. if player gets too far or an event unherds them
    {
        if (currentState == GoatState.Wandering || currentState == GoatState.Converting) return;

        currentState = GoatState.Wandering;
        if (_mainCollider != null)
        {
            _mainCollider.isTrigger = false; // Restore solid hitbox
        }
        if (_rb != null)
        {
             _rb.mass = 1f; // Restore original mass
        }
        SetNewWanderTarget();
        UpdateVisualState();
    }
    #endregion


    #region Movement Behaviours
    void Wander()
    {
        // Using transform.position for kinematic or simple movement
        // If using dynamic Rigidbody, velocity is set in FixedUpdate
        if (_rb == null || _rb.bodyType == RigidbodyType2D.Kinematic) {
             transform.position = Vector2.MoveTowards(transform.position, _wanderTarget, wanderSpeed * Time.deltaTime);
        }

        _wanderTimer -= Time.deltaTime;
        if (_wanderTimer <= 0f || Vector2.Distance(transform.position, _wanderTarget) < 0.1f)
        {
            SetNewWanderTarget();
        }
    }

    void SetNewWanderTarget()
    {
        _wanderTarget = _initialPosition + (Vector3)(Random.insideUnitCircle * wanderRadius);
        _wanderTimer = Random.Range(minWanderTime, maxWanderTime);
    }

    void FollowPlayer()
    {
        if (_playerTransform == null)
        {
            BecomeWandering(); // No player to follow
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, _playerTransform.position);

        if (distanceToPlayer > followDistanceToPlayer)
        {
            // If using transform.position for kinematic or simple movement
            if (!_rb ||_rb.bodyType == RigidbodyType2D.Kinematic)
            {
                Vector2 direction = ((Vector2)_playerTransform.position - (Vector2)transform.position).normalized;
                transform.position += (Vector3)direction * followPlayerSpeed * Time.deltaTime;
            }
            // If using dynamic Rigidbody, velocity is set in FixedUpdate
        }
        else
        {
             if (_rb && _rb.bodyType != RigidbodyType2D.Kinematic) _rb.linearVelocity = Vector2.Lerp(_rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 5f); // Smooth stop
        }

        // Optional: Add a max follow distance, if player gets too far, goat stops being herded
        // if (distanceToPlayer > maxFollowDistanceThreshold) { BecomeWandering(); }
    }
    #endregion


    #region Stacking Logic
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (currentState == GoatState.Converting) return;

        // Auto-herding on player contact
        if (autoHerdOnPlayerContact && collision.gameObject.CompareTag("Player") && currentState == GoatState.Wandering)
        {
            BecomeHerded();
            return; // Don't process stacking if just herded
        }

        // Stacking logic - only if wandering or base of a stack
        if (currentState == GoatState.Wandering || (currentState == GoatState.Stacked && isBaseOfStack))
        {
            PeacefulGoat otherGoat = collision.gameObject.GetComponent<PeacefulGoat>();
            if (otherGoat != null && otherGoat != this && otherGoat.currentState != GoatState.Herded && otherGoat.currentState != GoatState.Converting)
            {
                // Check if this goat is landing on top of the otherGoat
                ContactPoint2D[] contacts = new ContactPoint2D[collision.contactCount];
                collision.GetContacts(contacts);

                foreach (ContactPoint2D contact in contacts)
                {
                    // If this goat's bottom (-up direction) hits the other goat
                    if (Vector2.Dot(contact.normal, Vector2.up) > 0.7f) // contact.normal points away from otherGoat's surface
                    {
                        // This means 'this' goat is likely landing on 'otherGoat'.
                        TryStackOn(otherGoat.GetBaseOfStack()); // Try to stack on the true base
                        break;
                    }
                }
            }
        }
    }

    void TryStackOn(PeacefulGoat bottomGoatCandidate)
    {
        if (currentState == GoatState.Stacked && isStackedOnAnother) return; // Already stacked on another
        if (bottomGoatCandidate == this || bottomGoatCandidate.currentState == GoatState.Herded) return; // Can't stack on self or herded goat

        Debug.Log(gameObject.name + " is trying to stack on " + bottomGoatCandidate.gameObject.name);

        currentState = GoatState.Stacked;
        isStackedOnAnother = true;
        isBaseOfStack = false;
        _goatBelowMe = bottomGoatCandidate;
        bottomGoatCandidate.AddGoatOnTop(this);

        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.isKinematic = true;
        }
        UpdateStackedPosition();
        UpdateVisualState();
    }

    public void AddGoatOnTop(PeacefulGoat goatToAdd)
    {
        if (!_goatsOnTopOfMe.Contains(goatToAdd))
        {
            _goatsOnTopOfMe.Add(goatToAdd);
            goatToAdd._goatBelowMe = this;
            goatToAdd.currentState = GoatState.Stacked;
            goatToAdd.isStackedOnAnother = true;
            goatToAdd.isBaseOfStack = false;
            if (goatToAdd._rb != null)_rb.bodyType = RigidbodyType2D.Kinematic;

            // Ensure this goat is also marked as stacked and potentially kinematic if it's now a middle piece
            if (currentState != GoatState.Stacked) currentState = GoatState.Stacked;
            if (_rb != null && !_rb.isKinematic && _goatBelowMe != null) _rb.bodyType = RigidbodyType2D.Kinematic; // If this is now a middle piece

            UpdateVisualState(); // For this goat
            goatToAdd.UpdateVisualState(); // For the added goat
        }
    }

    public void UpdateStackedPosition()
    {
        if (_goatBelowMe != null && currentState == GoatState.Stacked && isStackedOnAnother)
        {
            // Calculate position based on the goat directly below this one in its stack
            int myIndexInStackAboveGoatBelow = _goatBelowMe._goatsOnTopOfMe.IndexOf(this);
            if (myIndexInStackAboveGoatBelow != -1)
            {
                 transform.position = _goatBelowMe.transform.position + Vector3.up * goatHeight * (myIndexInStackAboveGoatBelow + 1);
                 transform.rotation = _goatBelowMe.transform.rotation;
            }
        }
    }
    
    private void UnstackFully()
    {
        // Tell the goat below this one is no longer on it
        if (_goatBelowMe != null)
        {
            _goatBelowMe._goatsOnTopOfMe.Remove(this);
            _goatBelowMe = null;
        }

        // Tell all goats on top of this one they are no longer stacked (they should fall or become wandering)
        foreach (var goatAbove in new List<PeacefulGoat>(_goatsOnTopOfMe)) // Iterate on a copy
        {
            goatAbove.BecomeBaseAndWander(); // Make them independent
        }
        _goatsOnTopOfMe.Clear();

        isStackedOnAnother = false;
        isBaseOfStack = true; // It's now independent (unless it becomes herded or re-stacks)
        if (_rb != null) _rb.bodyType = RigidbodyType2D.Kinematic; // Allow physics to take over or wander
    }

    public void BecomeBaseAndWander() // Called when the goat it was stacked on is removed/herded
    {
        _goatBelowMe = null;
        isStackedOnAnother = false;
        isBaseOfStack = true;
        currentState = GoatState.Wandering;
        if (_rb != null) _rb.isKinematic = false;
        SetNewWanderTarget();
        UpdateVisualState();
        // Any goats on top of *this* one will still follow it if it's now the base of their stack.
    }


    public PeacefulGoat GetBaseOfStack()
    {
        if (isBaseOfStack || _goatBelowMe == null)
        {
            return this;
        }
        return _goatBelowMe.GetBaseOfStack();
    }
    #endregion


    #region Conversion
    // Called by an enemy
    public void StartConversionProcess()
    {
        if (currentState == GoatState.Converting) return;

        Debug.Log(gameObject.name + " is being converted!");
        currentState = GoatState.Converting;
        _turnedIntoCyborgForReal = true; // Mark for GameManager accounting
        // Stop all movement
        if (_rb != null) _rb.linearVelocity = Vector2.zero;

        // Unstack if part of a stack
        UnstackFully();


        // After a delay or animation, complete conversion
        // For now, let's make it instant for testing, replace with a coroutine for actual game
        Invoke("CompleteConversion", 0.1f); // Small delay to allow state change to register
        UpdateVisualState();
    }

    private void CompleteConversion()
    {
        GameManager.Instance?.GoatConvertedToCyborg();
        if (cyborgGoatPrefab != null)
        {
            Instantiate(cyborgGoatPrefab, transform.position, transform.rotation);
        }
        else
        {
            Debug.LogError("CyborgGoatPrefab not assigned to " + gameObject.name);
        }
        Destroy(gameObject);
    }
    #endregion

    void UpdateVisualState() // For debugging or changing sprite based on state
    {
       // Example: Change color based on state
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;
        switch (currentState)
        {
            case GoatState.Wandering: sr.color = Color.white; break;
            case GoatState.Stacked: sr.color = Color.gray; break;
            case GoatState.Herded: sr.color = Color.cyan; break;
            case GoatState.Converting: sr.color = Color.magenta; break;
        }
    }


    void OnDestroy()
    {
        // If this goat was part of a stack, notify others to adjust
        if (isStackedOnAnother && _goatBelowMe != null)
        {
            _goatBelowMe._goatsOnTopOfMe.Remove(this);
        }
        // foreach (var goatAbove in _goatsOnTopOfMe)
        // {
        //     if (goatAbove != null) goatAbove.BecomeBaseAndWander();
        // }

        if (!_turnedIntoCyborgForReal) 
        {
            GameManager.Instance?.UnregisterPeacefulGoat();
        }
    }
}