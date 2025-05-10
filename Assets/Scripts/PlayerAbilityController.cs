using UnityEngine;
using System;
using UnityEngine.InputSystem; // Required for Action

public class PlayerAbilityController : MonoBehaviour {


    private PlayerControls playerControls;
    // Define events for each ability
    public static event Action OnConvertEnemiesAbility;
    public static event Action OnPullGoatsAbility;
    public static event Action OnPushEnemiesAbility;

[SerializeField]
    private GameObject MainProjectilePrefab; // Assign the projectile prefab in the inspector

    private void Awake() {
        playerControls = new PlayerControls();
        // Bind the input actions to the methods
        playerControls.Player.SecondaryAttack.performed += ctx => ConvertEnemiesInArea();
        playerControls.Player.PrimaryAttack.performed += ctx => PushEnemiesAway();
        playerControls.Player.TertiaryAbility.performed += ctx => PullGoats();
    }
    private void OnEnable()
    {
        playerControls.Player.Enable();
    }

    private void OnDisable()
    {
        playerControls.Player.Disable();
    }

    private void ConvertEnemiesInArea() {
        // Placeholder for conversion logic
        Debug.Log("Converting enemies in area!");
        Collider2D[] enemiesInRange = Physics2D.OverlapCircleAll(transform.position, 5f, LayerMask.GetMask("Goat"));

        foreach (Collider2D enemy in enemiesInRange) {
            // Example: Convert the enemy to a friendly goat
            
            if ((enemy.TryGetComponent(out CybordGoat goat))) {
                goat.ConvertToFriendly(); // Assuming this method exists in the PeacefulGoat class
            }
        }
    }

    private void PullGoats() {
        // Placeholder for goat pulling logic
        Debug.Log("Pulling goats!");
        
        Collider2D[] goatsInRange = Physics2D.OverlapCircleAll(transform.position, 20f, LayerMask.GetMask("Goat"));
        foreach (Collider2D goat in goatsInRange) {
            // Pull Goats towards the player
            if (goat.TryGetComponent(out PeacefulGoat peacefulGoat)) {
                 peacefulGoat.PullTowardsPlayer(5f, 1f); // Adjust pull force as needed
            }
            else if (goat.TryGetComponent(out CybordGoat cybordGoat)) {
                cybordGoat.PullTowardsPlayer(5f, 1); // Adjust pull force as needed
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 5f); // Draw a sphere for the conversion range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 20f); // Draw a sphere for the pulling range
    }

    private void PushEnemiesAway() {
        Debug.Log("Main attack executed.");
        // Example: Instantiate a projectile
        GameObject projectile = Instantiate(MainProjectilePrefab, transform.position, Quaternion.identity);
        Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
        Vector2 direction = (Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - transform.position).normalized;
        projectileRb.linearVelocity = direction * 10f; // Adjust speed as needed
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Alpha1)) {
            OnConvertEnemiesAbility?.Invoke();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2)) {
            OnPullGoatsAbility?.Invoke();
        }
        if (Input.GetKeyDown(KeyCode.Alpha3)) {
            OnPushEnemiesAbility?.Invoke();
        }
    }
}