using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PrometheusPlayerController
{
    public class FallingGround : MonoBehaviour
    {
        Rigidbody2D rb;
        Vector2 initialPosition;
        bool platformMovingBack;

        // Use this for initialization
        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            initialPosition = transform.position;
        }

        void Update()
        {
            if (platformMovingBack)
                transform.position = Vector2.MoveTowards(transform.position, initialPosition, 20f * Time.deltaTime);

            if (transform.position.y == initialPosition.y)
                platformMovingBack = false;
        }

        void OnCollisionEnter2D(Collision2D col)
        {
            // Use tag comparison instead of name
            if (col.gameObject.CompareTag("Player") && !platformMovingBack)
            {
                Invoke("DropPlatform", 0.1f);
            }
        }

        void DropPlatform()
        {
            rb.isKinematic = false;
            Invoke("GetPlatformBack", 1f);
        }

        void GetPlatformBack()
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
            platformMovingBack = true;
        }
    }
}