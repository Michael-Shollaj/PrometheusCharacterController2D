using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PrometheusPlayerController
{
    public class DamagePlayer : MonoBehaviour
    {
        public int damage = 1; // Amount of damage dealt to the player

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // Check if the colliding object has the HealthPrometheus script (i.e., is the player)
            HealthPrometheus playerHealth = collision.GetComponent<HealthPrometheus>();

            // If the playerHealth is not null, then the colliding object is the player
            if (playerHealth != null)
            {
                playerHealth.health -= damage; // Decrease the player's health

                // Optional: Add feedback for damage here, such as a sound effect or visual effect

                // Example: Destroy the enemy upon collision
                // Destroy(gameObject);
            }
        }
    }
}