using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Needed for reloading the scene
using System.Collections;

namespace PrometheusPlayerController
{
    public class HealthPrometheus : MonoBehaviour
    {
        public int health;
        public int numOfHearts;

        public Image[] hearts;
        public Sprite fullHeart;
        public Sprite emptyHeart;

        void Update()
        {
            if (health > numOfHearts)
            {
                health = numOfHearts;
            }

            for (int i = 0; i < hearts.Length; i++)
            {
                if (i < health)
                {
                    hearts[i].sprite = fullHeart;
                }
                else
                {
                    hearts[i].sprite = emptyHeart;
                }

                hearts[i].enabled = i < numOfHearts;
            }

            // The check if the player has lost all health might be better placed elsewhere 
            // to not constantly check every frame, but it's okay for demonstration purposes.
            if (health <= 0)
            {
                Respawn();
            }
        }

        // Public method to apply damage
        public void TakeDamage(int damage)
        {
            Debug.Log("Taking Damage");
            health -= damage;
            StartCoroutine(DamageAnimation());

            if (health <= 0)
            {
                Respawn();
            }
        }

        void Respawn()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        IEnumerator DamageAnimation()
        {
            SpriteRenderer[] srs = GetComponentsInChildren<SpriteRenderer>();

            for (int i = 0; i < 3; i++)
            {
                foreach (SpriteRenderer sr in srs)
                {
                    Color c = sr.color;
                    c.a = 0.5f; // Consider changing opacity to less than 1 but more than 0 for visibility
                    sr.color = c;
                }

                yield return new WaitForSeconds(.2f);

                foreach (SpriteRenderer sr in srs)
                {
                    Color c = sr.color;
                    c.a = 1;
                    sr.color = c;
                }

                yield return new WaitForSeconds(.2f);
            }
        }
    }
}
