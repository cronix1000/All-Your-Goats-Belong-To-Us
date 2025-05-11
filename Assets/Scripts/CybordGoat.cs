using System;
using UnityEngine;

public class CybordGoat : BasicEnemyAI
{

    public GameObject peacefulGoatPrefab; // Assign the prefab in the inspector
    public bool isAbleToConvert = false; // Flag to check if the goat can be converted

    // If they are in the conversion circle, they can be converted
    public void ReadyToConvert()
    {
        // Check if the goat is ready to be converted
        if (isAbleToConvert)
        {
            // Call the conversion method
            ConvertToFriendly();
        }
        else
        {
            Debug.LogWarning("Cybord Goat is not ready to convert.", this);
        }
    }

// This method is called when the Cybord Goat is converted to a friendly goat when they are clicked on in the conversion circle
    public void ConvertToFriendly()
    {
        // Instantiate the peaceful goat prefab at the current position
        GameObject peacefulGoat = Instantiate(peacefulGoatPrefab, transform.position, Quaternion.identity);

        // Set the peaceful goat's properties (if needed)
        // peacefulGoat.GetComponent<PeacefulGoat>().SetProperties(...);

        // Destroy the Cybord Goat instance
        Destroy(gameObject);
    }

public void PullTowards(Vector2 targetPosition, float pullForce, float pullDuration)
    {
        if (rb == null ) // currentHealth is from BasicEnemyAI
        {
            Debug.LogWarning($"{gameObject.name} cannot be pulled: no Rigidbody or has no health.", this);
            return;
        }

        // Calculate the direction from the goat towards the target position.
        Vector2 directionToTarget = (targetPosition - (Vector2)transform.position);

        // If already very close to the target, might not need a strong pull or any pull.
        if (directionToTarget.sqrMagnitude < 0.01f) // Roughly 0.1 units
        {
            return;
        }
        base.ApplyKnockback(directionToTarget.normalized, pullForce, pullDuration);

    }

    /// <summary>
    /// Convenience method to pull the Cyborg Goat specifically towards the player.
    /// </summary>
    /// <param name="pullForce">The magnitude of the impulse force.</param>
    /// <param name="pullDuration">How long the goat's normal AI is paused due to the pull.</param>
    public void PullTowardsPlayer(float pullForce, float pullDuration)
    {
        if (PlayerController.Instance == null || PlayerController.Instance.transform == null)
        {
            Debug.LogWarning("PlayerController.Instance or its transform not found. Cannot pull CybordGoat towards player.", this);
            return;
        }
        PullTowards(PlayerController.Instance.transform.position, pullForce, pullDuration);
    }


}