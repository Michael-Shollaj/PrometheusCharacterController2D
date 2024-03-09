using UnityEngine;

namespace PrometheusPlayerController
{
    public class HealthPickUp : MonoBehaviour
    {
        public int healthBonus = 1; // Amount of health this pickup restores

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // Try to get the HealthPrometheus component from the colliding object
            HealthPrometheus playerHealth = collision.GetComponent<HealthPrometheus>();

            // If the colliding object has the HealthPrometheus component...
            if (playerHealth != null)
            {
                // Increase the player's health
                playerHealth.health += healthBonus;

                // Ensure player's health does not exceed maximum hearts
                playerHealth.health = Mathf.Min(playerHealth.health, playerHealth.numOfHearts);

                // Optional: Add any effects or sounds for picking up health here

                // Destroy the health pickup object
                Destroy(gameObject);
            }
        }
    }
}
