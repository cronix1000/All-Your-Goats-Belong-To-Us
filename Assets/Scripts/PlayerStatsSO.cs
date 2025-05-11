using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStatsSO", menuName = "GoatGame", order = 0)]
public class PlayerStatsSO : ScriptableObject {
    // move speed, herding radius, herding focus speed, max health, pull force, pull duration, goat follow speed, goat max follow distance, goat separation force, goat attraction passive chance, reduced unherd time
    public float moveSpeed = 5f; // Default player move speed
    public float herdingRadius = 2f; // Default herding radius
    public float herdingFocusSpeed = 1f; // Default herding focus speed
    public float maxHealth = 100f; // Default max health
    public float pullForce = 5f; // Default pull force
    public float pullDuration = 0.2f; // Default pull duration
    public float goatFollowSpeed = 2f; // Default goat follow speed
    public float goatMaxFollowDistance = 5f; // Default max follow distance for goats
    public float goatSeparationForce = 1f; // Default separation force for goats
    
    
}