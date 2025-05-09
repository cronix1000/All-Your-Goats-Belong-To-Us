using UnityEngine;
using UnityEngine.InputSystem; // Required for the new Input System

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Transform staffTip; // Assign a child GameObject representing the staff's tip for ability origins
    public GameObject primaryAbilityProjectilePrefab; // Example: For a shooting ability
    public GameObject secondaryAbilityEffectPrefab; // Example: For an AoE effect

    private PlayerControls playerControls;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Camera mainCamera;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;

        playerControls = new PlayerControls();

        // Movement
        playerControls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        playerControls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        // Abilities
        playerControls.Player.PrimaryAttack.performed += ctx => PerformPrimaryAbility();
        playerControls.Player.SecondaryAttack.performed += ctx => PerformSecondaryAbility();
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
        // Movement
        rb.linearVelocity = moveInput * moveSpeed;

        // Optional: Rotate player to face mouse (if desired in a top-down 2D game)
        // LookAtMouse();
    }

    void LookAtMouse()
    {
        if (mainCamera == null) return;
        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, mainCamera.nearClipPlane + 10f)); // Ensure Z is in front of camera
        Vector2 direction = (Vector2)mouseWorldPosition - (Vector2)transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle -90f, Vector3.forward); // -90 if sprite faces up
    }


    private void PerformPrimaryAbility()
    {
        Debug.Log("Primary Ability Used!");
        if (primaryAbilityProjectilePrefab && staffTip)
        {
      
            Instantiate(primaryAbilityProjectilePrefab, staffTip.position, staffTip.rotation);
        }
    }

    private void PerformSecondaryAbility()
    {
        Debug.Log("Secondary Ability Used!");
        if (secondaryAbilityEffectPrefab)
        {
            Instantiate(secondaryAbilityEffectPrefab, staffTip ? staffTip.position : transform.position, Quaternion.identity);
        }
    }

    public void TakeDamage(int amount)
    {
        
    }
}