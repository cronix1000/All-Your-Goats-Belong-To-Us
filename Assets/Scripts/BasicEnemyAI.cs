using UnityEngine;

public class BasicEnemyAI : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float detectionRadius = 10f;
    public int damageToPlayer = 10;
    public int xpValue = 50;
    public LayerMask goatLayer; // Assign the layer your PeacefulGoats are on
    public LayerMask playerLayer;

    private Transform _targetGoatTransform;
    private PeacefulGoat _targetGoatScript; // Keep a reference to the script
    private Transform _player;
    private Rigidbody2D _rb;

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb != null) _rb.gravityScale = 0;
        InvokeRepeating("FindTarget", 0f, 0.5f); // Scan more frequently
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
        }
    }

    // ReSharper disable Unity.PerformanceAnalysis
    void FindTarget()
    {
        // If current target is converting or no longer valid, find a new one
        if (_targetGoatScript && _targetGoatScript.currentState == PeacefulGoat.GoatState.Converting)
        {
            _targetGoatTransform = null;
            _targetGoatScript = null;
        }
        if (_targetGoatTransform && _targetGoatTransform.gameObject.activeInHierarchy) return;


        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, detectionRadius, goatLayer);
        float closestDistance = Mathf.Infinity;
        PeacefulGoat closestGoatScript = null;

        foreach (var hitCollider in hitColliders)
        {
            PeacefulGoat potentialGoat = hitCollider.GetComponent<PeacefulGoat>();
            // Ensure it's a PeacefulGoat and not already being converted
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
            _targetGoatScript = closestGoatScript;
            _targetGoatTransform = closestGoatScript.transform;
        }
        else
        {
            _targetGoatTransform = null; // No valid goat target found
            _targetGoatScript = null;
        }
    }

    void Update()
    {
        if (_targetGoatTransform != null && _targetGoatScript != null && _targetGoatScript.currentState != PeacefulGoat.GoatState.Converting)
        {
            Vector2 direction = ((Vector2)_targetGoatTransform.position - _rb.position).normalized;
            _rb.linearVelocity = direction * moveSpeed;
        }
        else if (_player != null) // Optional: Fallback to player if no goats
        {
            // Vector2 directionToPlayer = ((Vector2)player.position - rb.position).normalized;
            // rb.velocity = directionToPlayer * moveSpeed;
            FindTarget(); // Keep looking for goats even if player is fallback
        }
        else
        {
            _rb.linearVelocity = Vector2.zero;
            FindTarget();
        }
    }

    // For solid goat colliders
    void OnCollisionEnter2D(Collision2D collision)
    {
        TryConvertGoat(collision.gameObject);
        TryDamagePlayer(collision.gameObject);
    }

    // For herded goat trigger colliders
    void OnTriggerEnter2D(Collider2D otherCollider)
    {
        TryConvertGoat(otherCollider.gameObject);
        TryDamagePlayer(otherCollider.gameObject);
    }

    void TryConvertGoat(GameObject collidedObject)
    {
        PeacefulGoat peacefulGoat = collidedObject.GetComponent<PeacefulGoat>();
        if (peacefulGoat != null && peacefulGoat.currentState != PeacefulGoat.GoatState.Converting)
        {
            Debug.Log(gameObject.name + " contacted " + peacefulGoat.name + " for conversion.");
            peacefulGoat.StartConversionProcess();
            // Clear target so enemy looks for a new one
            _targetGoatTransform = null;
            _targetGoatScript = null;
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


    public void TakeDamage(int amount) // Called by player's staff attack
    {
        // Add health logic here
        Die();
    }

    void Die()
    {
        GameManager.Instance?.AddXP(xpValue);
        FindFirstObjectByType<AISpawner>()?.EnemyDefeated(this.gameObject);
        Destroy(gameObject);
    }
}