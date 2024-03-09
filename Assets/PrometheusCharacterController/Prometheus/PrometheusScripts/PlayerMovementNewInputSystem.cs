using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

namespace PrometheusPlayerController
{
    /// <summary>
    /// Controls the player's movement with the new Unity Input System, including advanced mechanics like sprinting, crouching, wall jumping, and dashing.
    /// </summary>
    public class PlayerMovementNewInputSystem : MonoBehaviour
    {
        [SerializeField] private HealthPrometheus healthScript;
        private bool isTakingOxygenDamage = false;

        [Header("Physics Components")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private Transform ceilingCheck;
        [SerializeField] private Transform wallCheck;

        [Header("Layers")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask ceilingLayer;
        [SerializeField] private LayerMask wallLayer;
        [SerializeField] private LayerMask waterLayer;



        [Header("Visuals")]
        [SerializeField] private Animator animator;
        [SerializeField] private ParticleSystem dustEffect;
        [SerializeField] private ParticleSystem wallSlideDustEffect;
        [SerializeField] private GameObject dashDustEffect;

        [Header("Movement Settings")]
        [Range(0f, 20f)] [SerializeField] private float speed = 8f;
        [Range(0f, 30f)] [SerializeField] private float sprintSpeed = 12f;
        [Range(0f, 20f)] [SerializeField] private float crouchSpeed = 4f;
        [Range(0f, 50f)] [SerializeField] private float jumpingPower = 16f;
        [SerializeField] private bool enableSprinting = true;
        [SerializeField] private bool enableCrouching = true;
        [SerializeField] private bool enableDoubleJump = true;
        [SerializeField] private bool enableWallJump = true;
        [SerializeField] private bool enableWallClimbing = true;
        [SerializeField] private bool enableDashing = true;

        [Header("Wall Interaction")]
        [SerializeField] private float wallSlidingSpeed = 2f;
        [SerializeField] private float wallClimbSpeed = 3f;
        [SerializeField] private Vector2 wallJumpingPower = new Vector2(8f, 16f);
        [SerializeField] private float wallJumpingDuration = 0.4f;

        [Header("Dash Settings")]
        [SerializeField] private float dashSpeed = 25f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private float dashCooldown = 1f;

        [Header("Underwater Settings")]
        [SerializeField] private float swimSpeed = 5f;
        [SerializeField] private Transform waterCheck;
        [SerializeField] private Slider oxygenSlider;
        [SerializeField] private float maxOxygen = 100f;
        [SerializeField] private float oxygenDepletionRate = 5f; // Oxygen depleted per second underwater
        [SerializeField] private float oxygenReplenishRate = 10f; // Oxygen replenished per second on land

        private float horizontal;
        private float vertical;
        private bool isFacingRight = true;
        private bool isCrouching = false;
        private bool isSprinting = false;
        private bool canDoubleJump = true;
        private bool isWallSliding;
        private bool isWallJumping;
        private bool isWallClimbing = false;
        private float wallJumpingDirection;
        private float wallJumpingCounter;
        private float dashTimeLeft;
        private float lastDash = -Mathf.Infinity;
        private bool isDashing = false;
        private bool isInWater = false;
        private float currentOxygen;

        private bool wasGrounded;

        private void Start()
        {
            currentOxygen = maxOxygen;
            oxygenSlider.maxValue = maxOxygen;
            oxygenSlider.value = currentOxygen;
        }

        void Update()
        {
            bool isWalking = Mathf.Abs(horizontal) > 0;
            bool isCrouchWalking = isCrouching && isWalking;

            animator.SetBool("IsWalking", isWalking && !isCrouching && !isSprinting);
            animator.SetBool("IsSprinting", isWalking && !isCrouching && isSprinting && enableSprinting);
            animator.SetBool("IsCrouchWalking", isCrouchWalking);
            bool isGrounded = IsGrounded();
            animator.SetBool("IsJumping", !isGrounded);

            if (!isFacingRight && horizontal > 0f)
            {
                Flip();
            }
            else if (isFacingRight && horizontal < 0f)
            {
                Flip();
            }

            if (IsCeilingAbove() || (isCrouching && enableCrouching))
            {
                isCrouching = true;
            }
            else
            {
                isCrouching = false; // Stand up automatically when there's no ceiling
            }
            animator.SetBool("IsCrouching", isCrouching);

            if (isGrounded)
            {
                canDoubleJump = enableDoubleJump;
            }

            WallSlide();

            if (isWallSliding)
            {
                canDoubleJump = enableDoubleJump;
            }

            if (enableWallClimbing && IsWalled() && Mathf.Abs(vertical) > 0f)
            {
                StartWallClimb();
            }
            else if (!IsWalled() || Mathf.Abs(vertical) == 0f)
            {
                StopWallClimb();
            }

            animator.SetBool("IsWallClimbing", isWallClimbing && enableWallClimbing);

            if (enableDashing && isDashing)
            {
                if (dashTimeLeft > 0)
                {
                    float dashDirection = isFacingRight ? 1 : -1;
                    rb.velocity = horizontal == 0 ? new Vector2(dashDirection * dashSpeed, rb.velocity.y) : new Vector2(horizontal * dashSpeed, rb.velocity.y);
                    dashTimeLeft -= Time.deltaTime;
                    animator.SetBool("IsDashing", true);
                }
                else
                {
                    isDashing = false;
                    animator.SetBool("IsDashing", false);
                }
            }
            else
            {
                animator.SetBool("IsDashing", false);
            }

            isInWater = Physics2D.OverlapCircle(waterCheck.position, 0.2f, waterLayer);
            bool isSwimmingMoving = isInWater && (Mathf.Abs(horizontal) > 0 || Mathf.Abs(vertical) > 0);
            bool isSwimmingIdle = isInWater && (Mathf.Abs(horizontal) == 0 && Mathf.Abs(vertical) == 0);

            animator.SetBool("IsSwimmingMoving", isSwimmingMoving);
            animator.SetBool("IsSwimmingIdle", isSwimmingIdle);

            if (isInWater)
            {
                currentOxygen -= oxygenDepletionRate * Time.deltaTime;
                if (currentOxygen <= 0)
                {
                    currentOxygen = 0;
                    if (!isTakingOxygenDamage)
                    {
                        isTakingOxygenDamage = true;
                        StartCoroutine(DamageDueToOxygenDepletion());
                    }
                }
            }
            else
            {
                if (currentOxygen < maxOxygen)
                {
                    currentOxygen += oxygenReplenishRate * Time.deltaTime;
                    if (currentOxygen > maxOxygen)
                    {
                        currentOxygen = maxOxygen;
                    }
                }
                if (isTakingOxygenDamage)
                {
                    StopCoroutine(DamageDueToOxygenDepletion());
                    isTakingOxygenDamage = false;
                }
            }

            CheckForLanding();

            oxygenSlider.value = currentOxygen;
            oxygenSlider.gameObject.SetActive(isInWater);
        }


        private IEnumerator DamageDueToOxygenDepletion()
        {
            while (currentOxygen <= 0)
            {
                healthScript.TakeDamage(1); // Damage the player by 1
                yield return new WaitForSeconds(2); // Wait for 2 seconds before applying damage again
            }
            isTakingOxygenDamage = false; // Reset flag when the loop exits
        }

        private void FixedUpdate()
        {
            if (!isDashing && !isWallJumping && !isInWater) // Normal movement
            {
                float currentSpeed = isCrouching ? crouchSpeed : (isSprinting && enableSprinting ? sprintSpeed : speed);
                rb.velocity = new Vector2(horizontal * currentSpeed, rb.velocity.y);
            }
            else if (isInWater) // Swimming movement
            {
                float currentSpeed = swimSpeed;
                rb.velocity = new Vector2(horizontal * currentSpeed, vertical * currentSpeed);
            }

            if (isWallClimbing && enableWallClimbing)
            {
                rb.velocity = new Vector2(rb.velocity.x, vertical * wallClimbSpeed);
            }
        }

        public void Jump(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                if (IsGrounded())
                {
                    rb.velocity = new Vector2(rb.velocity.x, jumpingPower);
                    canDoubleJump = enableDoubleJump;
                }
                else if (canDoubleJump && enableDoubleJump && !isWallSliding) // Double jump
                {
                    rb.velocity = new Vector2(rb.velocity.x, jumpingPower);
                    canDoubleJump = false;
                    PlayDustEffect();
                }
                else if (isWallSliding && enableWallJump) // Wall jump
                {
                    isWallJumping = true;
                    wallJumpingDirection = isFacingRight ? -1 : 1;
                    rb.velocity = new Vector2(wallJumpingPower.x * wallJumpingDirection, wallJumpingPower.y);
                    Invoke(nameof(ResetWallJump), wallJumpingDuration);
                }

                isCrouching = false;
                animator.SetBool("IsCrouching", isCrouching);
            }
        }

        public void Crouch(InputAction.CallbackContext context)
        {
            if (context.performed && enableCrouching)
            {
                isCrouching = true;
                animator.SetBool("IsCrouching", isCrouching);
            }
            else if (context.canceled)
            {
                // Modify this part to check if there's no ceiling above when uncrouching
                if (!IsCeilingAbove()) // Only stand up if there's no ceiling
                {
                    isCrouching = false;
                    animator.SetBool("IsWalking", true); 
                }
                animator.SetBool("IsCrouching", isCrouching);

            }
        }

        public void Sprint(InputAction.CallbackContext context)
        {
            if (enableSprinting)
            {
                isSprinting = context.performed;
            }
        }

        public void Dash(InputAction.CallbackContext context)
        {
            if (context.performed && Time.time >= lastDash + dashCooldown && !isDashing && enableDashing)
            {
                isDashing = true;
                dashTimeLeft = dashDuration;
                lastDash = Time.time;
                animator.SetBool("IsDashing", true);
                Instantiate(dashDustEffect, transform.position, Quaternion.identity);
            }
        }

        public void Move(InputAction.CallbackContext context)
        {
            Vector2 inputVector = context.ReadValue<Vector2>();
            horizontal = inputVector.x;
            vertical = inputVector.y; // Read vertical input for wall climbing
        }

        private void ResetWallJump()
        {
            isWallJumping = false;
        }

        private bool IsGrounded()
        {
            return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
        }

        private bool IsCeilingAbove()
        {
            return Physics2D.OverlapCircle(ceilingCheck.position, 0.2f, ceilingLayer);
        }

        private bool IsWalled()
        {
            return Physics2D.OverlapCircle(wallCheck.position, 0.2f, wallLayer);
        }

        private void WallSlide()
        {
            if (IsWalled() && !IsGrounded() && Mathf.Abs(horizontal) > 0f)
            {
                isWallSliding = true;
                rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlidingSpeed, float.MaxValue));

                // Play wall slide dust effect if not already playing
                if (wallSlideDustEffect != null && !wallSlideDustEffect.isPlaying)
                {
                    wallSlideDustEffect.Play();
                }
            }
            else
            {
                isWallSliding = false;

                // Stop wall slide dust effect if it was playing
                if (wallSlideDustEffect != null && wallSlideDustEffect.isPlaying)
                {
                    wallSlideDustEffect.Stop();
                }
            }

            animator.SetBool("IsWallSliding", isWallSliding);
        }


        private void StartWallClimb()
        {
            if (enableWallClimbing)
            {
                isWallClimbing = true;
                rb.gravityScale = 0; // Neutralize gravity while climbing
                animator.SetBool("IsWallSliding", false);
            }
        }

        private void StopWallClimb()
        {
            if (isWallClimbing && enableWallClimbing)
            {
                isWallClimbing = false;
                rb.gravityScale = 3; // Restore gravity effect after climbing
            }
        }

        private void CheckForLanding()
        {
            bool isGrounded = IsGrounded();
            if (isGrounded && !wasGrounded)
            {
                // Player just landed
                PlayDustEffect();
            }
            wasGrounded = isGrounded;
        }

        private void PlayDustEffect()
        {
            if (dustEffect != null)
            {
                dustEffect.Play();
            }
        }

        private void Flip()
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Water"))
            {
                isInWater = true;
                rb.gravityScale = 0; // Neutralize gravity in water
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Water"))
            {
                isInWater = false;
                rb.gravityScale = 3; // Restore gravity
            }
        }
    }
}
