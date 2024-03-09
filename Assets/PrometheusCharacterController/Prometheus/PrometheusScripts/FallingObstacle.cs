using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PrometheusPlayerController
{
    public class FallingObstacle : MonoBehaviour
    {
        Rigidbody2D rb;

        // Start is called before the first frame update
        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (col.gameObject.tag.Equals("Player"))
                rb.isKinematic = false;
        }

    }
}