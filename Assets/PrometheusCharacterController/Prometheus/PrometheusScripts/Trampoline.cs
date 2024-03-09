using UnityEngine;

namespace PrometheusPlayerController
{
    public class Trampoline : MonoBehaviour
    {
        public float bounceForce = 10f; // The force with which the player will be bounced upwards

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Check if the colliding object has a Rigidbody2D component (i.e., is the player)
            Rigidbody2D rb = collision.collider.GetComponent<Rigidbody2D>();

            // If the Rigidbody2D is not null, then the colliding object can be bounced
            if (rb != null)
            {
                // Apply an upward force to the Rigidbody2D
                Vector2 bounceDirection = new Vector2(0, bounceForce);
                rb.velocity = bounceDirection; // This directly sets the velocity for an immediate bounce effect
            }
        }
    }
}