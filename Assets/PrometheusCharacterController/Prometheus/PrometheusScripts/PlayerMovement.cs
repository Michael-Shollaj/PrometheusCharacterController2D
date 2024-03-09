using UnityEngine;
using System.Collections;

namespace PrometheusCharacterController
{
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        private float horizontal;
        [SerializeField] private float speed = 8f;
        [SerializeField] private float sprintSpeed = 12f;
        [SerializeField] private float jumpingPower = 16f;
        [SerializeField] private float highJumpPower = 24f;
        private bool isFacingRight = true;

        [Header("Wall Jump Settings")]
        [SerializeField] private Vector2 wallJumpForce = new Vector2(10f, 15f); // X for horizontal force, Y for vertical force
        [SerializeField] private float wallJumpTimer;
        private bool isTouchingWall;
        private bool isWallJumping;

        [Header("Sliding Settings")]
        private bool isWallSliding;
        [SerializeField] private float wallSlidingSpeed = 2f;
        [SerializeField] private GameObject dustEffect;

        [Header("Crouch Settings")]
        [SerializeField] private float crouchSpeed = 4f;
        private bool isCrouching = false;
        [SerializeField] private Transform ceilingCheck;
        [SerializeField] private float ceilingCheckRadius = 0.2f;
        private bool isCeilingAbove = false;

        [Header("Ground Settings")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private Transform wallCheck;
        [SerializeField] private LayerMask wallLayer;
        [SerializeField] private float groundCheckRadius = 0.2f;
        private bool wasGrounded;

        [Header("Wall Climbing Settings")]
        private bool isWallClimbing;
        [SerializeField] private float wallClimbingSpeed = 3f;
        [SerializeField] private Transform wallClimbCheck;
        [SerializeField] private float wallClimbCheckRadius = 0.2f;

        [Header("Stamina Settings")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float stamina = 100f;
        [SerializeField] private float staminaDecreasePerSecond = 10f;
        [SerializeField] private float staminaRegenPerSecond = 5f;
        [SerializeField] private float sprintStaminaThreshold = 20f;

        [Header("Dash Settings")]
        [SerializeField] private float dashSpeed;
        [SerializeField] private float timeDash;
        private float gravity;
        private bool CanDoDash = true;
        private bool CanBeMoved = true;

        [Header("Ability Toggles")]
        [SerializeField] private bool enableDash = true;
        [SerializeField] private bool enableSprinting = true;
        [SerializeField] private bool enableCrouching = true;
        [SerializeField] private bool enableWallClimbing = true;
        [SerializeField] private bool enableDoubleJump = true;
        [SerializeField] private bool enableStamina = true;
        [SerializeField] private bool enableWallJump = true;

        private Animator anim;

        private bool isSprinting = false;
        private bool canDoubleJump = false;

        private void Start()
        {
            anim = GetComponent<Animator>();
            rb = GetComponent<Rigidbody2D>();
            gravity = rb.gravityScale;
        }

        private void Update()
        {
            horizontal = Input.GetAxisRaw("Horizontal");

            if (enableCrouching)
            {
                isCeilingAbove = Physics2D.OverlapCircle(ceilingCheck.position, ceilingCheckRadius, groundLayer);
                isCrouching = Input.GetKey(KeyCode.S) || isCeilingAbove;
            }

            if (enableSprinting)
            {
                isSprinting = Input.GetKey(KeyCode.LeftShift) && Mathf.Abs(horizontal) > 0 && !isCrouching && !isWallSliding;
                if (enableStamina)
                {
                    if (isSprinting && stamina > 0)
                    {
                        stamina -= staminaDecreasePerSecond * Time.deltaTime;
                        if (stamina < 0)
                        {
                            stamina = 0;
                            isSprinting = false;
                        }
                    }
                    else if (!isSprinting && stamina < maxStamina)
                    {
                        stamina += staminaRegenPerSecond * Time.deltaTime;
                        if (stamina > maxStamina)
                        {
                            stamina = maxStamina;
                        }
                    }
                    isSprinting = isSprinting && stamina > sprintStaminaThreshold && Mathf.Abs(horizontal) > 0;
                }
            }

            anim.SetBool("isWalking", Mathf.Abs(horizontal) > 0.1f);
            anim.SetBool("isSprinting", isSprinting && enableSprinting);
            anim.SetBool("isCrouching", isCrouching && enableCrouching);
            anim.SetBool("isJumping", !IsGrounded());
            anim.SetBool("isWallSliding", isWallSliding);

            if (Input.GetButtonDown("Jump"))
            {
                JumpLogic();
            }

            WallSlide();

            if (enableWallClimbing)
            {
                WallClimb();
            }

            if (!isWallClimbing)
            {
                Flip();
            }

            if (enableDash && Input.GetKeyDown(KeyCode.R) && CanDoDash)
            {
                StartCoroutine(Dash());
            }
            HandleWallJump();
        }

        private void HandleWallJump()
        {
            isTouchingWall = IsWalled() && !IsGrounded();

            if (enableWallJump && Input.GetButtonDown("Jump") && isTouchingWall)
            {
                isWallJumping = true;
                Invoke("SetWallJumpingToFalse", wallJumpTimer); // Delay to prevent immediate wall jump canceling
            }

            if (isWallJumping)
            {
                rb.velocity = new Vector2(wallJumpForce.x * -Mathf.Sign(transform.localScale.x), wallJumpForce.y);
            }
        }

        private void SetWallJumpingToFalse()
        {
            isWallJumping = false;
        }

        private IEnumerator Dash()
        {
            CanBeMoved = false;
            CanDoDash = false;
            rb.gravityScale = 0;
            rb.velocity = new Vector2(dashSpeed * transform.localScale.x, 0);
            anim.SetTrigger("Dash");

            yield return new WaitForSeconds(timeDash);

            CanBeMoved = true;
            CanDoDash = true;
            rb.gravityScale = gravity;
        }

        private void FixedUpdate()
        {
            if (!isWallClimbing && !isWallJumping)
            {
                MovementLogic();
            }

            CheckForLanding();
        }

        private void JumpLogic()
        {
            if (IsGrounded())
            {
                float jumpForce = isCrouching ? highJumpPower : jumpingPower;
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);
                anim.SetTrigger("Jump");
                canDoubleJump = true;
            }
            else if (canDoubleJump && enableDoubleJump)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpingPower);
                anim.SetTrigger("DoubleJump");
                canDoubleJump = false;
            }
        }

        private bool IsGrounded()
        {
            return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        private void WallSlide()
        {
            if (IsWalled() && !IsGrounded() && !isWallClimbing)
            {
                isWallSliding = true;
                rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlidingSpeed, float.MaxValue));
            }
            else
            {
                isWallSliding = false;
            }

            anim.SetBool("isWallSliding", isWallSliding);
        }

        private bool IsWalled()
        {
            return Physics2D.OverlapCircle(wallCheck.position, 0.2f, wallLayer);
        }

        private void WallClimb()
        {
            if (enableWallClimbing && IsNearWall() && Input.GetKey(KeyCode.W))
            {
                isWallClimbing = true;
                rb.velocity = new Vector2(rb.velocity.x, Input.GetAxisRaw("Vertical") * wallClimbingSpeed);
            }
            else
            {
                isWallClimbing = false;
            }

            anim.SetBool("isWallClimbing", isWallClimbing);
        }

        private bool IsNearWall()
        {
            return Physics2D.OverlapCircle(wallClimbCheck.position, wallClimbCheckRadius, wallLayer);
        }

        private void MovementLogic()
        {
            if (CanBeMoved)
            {
                float currentSpeed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : speed);
                rb.velocity = new Vector2(horizontal * currentSpeed, rb.velocity.y);
            }
        }

        private void Flip()
        {
            if (isFacingRight && horizontal < 0f || !isFacingRight && horizontal > 0f)
            {
                isFacingRight = !isFacingRight;
                Vector3 localScale = transform.localScale;
                localScale.x *= -1f;
                transform.localScale = localScale;
            }
        }

        private void CheckForLanding()
        {
            bool isGroundedNow = IsGrounded();
            if (isGroundedNow && !wasGrounded && rb.velocity.y <= 0f)
            {
                TriggerDustEffect();
            }
            wasGrounded = isGroundedNow; // Update the wasGrounded for the next frame
        }

        private void TriggerDustEffect()
        {
            if (dustEffect != null)
            {
                dustEffect.SetActive(true);
                StartCoroutine(DeactivateAfterDelay(dustEffect, dustEffect.GetComponent<ParticleSystem>().main.duration));
            }
        }

        private IEnumerator DeactivateAfterDelay(GameObject effect, float delay)
        {
            yield return new WaitForSeconds(delay);
            effect.SetActive(false);
        }

        private void OnDrawGizmosSelected()
        {
            if (ceilingCheck != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(ceilingCheck.position, ceilingCheckRadius);
            }
            if (groundCheck != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }
            if (wallClimbCheck != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(wallClimbCheck.position, wallClimbCheckRadius);
            }
        }
    }
}