using UnityEngine;

namespace Forge.Player {
    // Using some custom setting to achieve the soulslike feeling I'm going for
    // Slower run, committed jumps (spamming isn't ideal), ground dodging
    // getting knocked back when hit, etc

    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerInputReader))]

    public class PlayerController : MonoBehaviour {
        [Header("Run")]
        [SerializeField] private float maxSpeed = 5.5f;
        [SerializeField] private float acceleration = 60f;
        [SerializeField] private float deceleration = 80f;
        [SerializeField, Range(0f, 1f)] private float airControl = 0.6f;

        [Header("Jump")]
        [SerializeField] private float jumpVelocity = 13.5f;
        [SerializeField, Range(0f, 1f)] private float jumpCutMultiplier = 0.45f;
        [SerializeField] private float coyoteTime = 0.08f;
        [SerializeField] private float jumpBufferTime = 0.1f;

        [Header("Gravity")]
        [SerializeField] private float normalGravity = 3.2f;
        [SerializeField] private float fallGravity = 4.5f;
        [SerializeField] private float maxFallSpeed = 18f;

        [Header("Dodge (ground slide)")]
        [SerializeField] private float dodgeSpeed = 10f;
        [SerializeField] private float dodgeDuration = 0.35f;
        [SerializeField] private float dodgeInvulnerability = 0.3f;
        [SerializeField] private float dodgeCooldown = 0.45f;

        [Header("Hurt")]
        [SerializeField] private Vector2 knockback = new Vector2(6f, 5f);
        [SerializeField] private float hurtStunTime = 0.3f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.7f, 0.1f);
        [SerializeField] private LayerMask groundLayer;

        private Rigidbody2D rb;
        private PlayerInputReader input;
        private PlayerCombat combat;
        private PlayerHealth health;

        private float moveInput;
        private float coyoteTimer;
        private float jumpBufferTimer;
        private float dodgeTimer;
        private float dodgeCooldownTimer;
        private float stunTimer;
        private int dodgeDirection = 1;

        public bool IsGrounded { get; private set; }
        public bool IsDodging => dodgeTimer > 0f;
        public bool IsStunned => stunTimer > 0f;
        public int Facing { get; private set; } = 1;
        // Last place the player stood on solid ground, gonna use this for a guilt type system
        // like in Witchfire or other soulslikes (drop stuff on your death location)
        public Vector3 LastSafePosition { get; private set; }

        // Attacking, parrying or healing locks movement input
        private bool ActionLocked => (combat != null && combat.IsBusy) || (health != null && health.IsHealing);

        public void Configure(Transform groundCheckPoint, LayerMask ground)
        {
            groundCheck = groundCheckPoint;
            groundLayer = ground;
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            input = GetComponent<PlayerInputReader>();
            combat = GetComponent<PlayerCombat>();
            health = GetComponent<PlayerHealth>();

            rb.gravityScale = normalGravity;
            rb.freezeRotation = true;
            LastSafePosition = transform.position;

            // Player and enemies pass through each other, this way enemies only hurt you with attacks.
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1) Physics2D.IgnoreLayerCollision(gameObject.layer, enemyLayer, true);
        }

        private void Update()
        {
            if (health != null && health.IsDead)
            {
                moveInput = 0f;
                return;
            }

            moveInput = input.Move.ReadValue<float>();

            if (input.Jump.WasPressedThisFrame()) jumpBufferTimer = jumpBufferTime;
            else jumpBufferTimer -= Time.deltaTime;

            if (input.Jump.WasReleasedThisFrame() && rb.linearVelocity.y > 0f && !IsStunned)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);

            dodgeCooldownTimer -= Time.deltaTime;
            if (input.Dodge.WasPressedThisFrame()) TryDodge();

            // You can't turn around mid-attack, attacks are committed.
            if (!ActionLocked && !IsDodging && !IsStunned)
            {
                if (moveInput > 0.01f) Facing = 1;
                else if (moveInput < -0.01f) Facing = -1;
            }
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * Facing;
            transform.localScale = scale;
        }

        private void TryDodge()
        {
            if (!IsGrounded || IsDodging || IsStunned || dodgeCooldownTimer > 0f) return;
            if (health != null && health.IsHealing) return;
            if (combat != null && combat.IsBusy && !combat.CanCancelIntoDodge) return;

            if (combat != null) combat.CancelAction();
            if (moveInput > 0.01f) Facing = 1;
            else if (moveInput < -0.01f) Facing = -1;

            dodgeDirection = Facing;
            dodgeTimer = dodgeDuration;
            dodgeCooldownTimer = dodgeDuration + dodgeCooldown;
            if (health != null) health.GrantInvulnerability(dodgeInvulnerability, false);
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector2 velocity = rb.linearVelocity;

            IsGrounded = groundCheck != null && velocity.y <= 0.01f &&
                         Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer) != null;

            if (IsGrounded)
            {
                coyoteTimer = coyoteTime;
                LastSafePosition = transform.position;
            }
            else
            {
                coyoteTimer -= dt;
            }

            if (health != null && health.IsDead)
            {
                velocity.x = Mathf.MoveTowards(velocity.x, 0f, deceleration * dt);
            }
            else if (IsStunned)
            {
                stunTimer -= dt; // knockback velocity plays out
            }
            else if (IsDodging)
            {
                dodgeTimer -= dt;
                velocity.x = dodgeTimer > 0f ? dodgeDirection * dodgeSpeed : dodgeDirection * maxSpeed * 0.5f;
            }
            else
            {
                bool locked = ActionLocked;
                // Grounded attacks stop you in place, while aerial attacks keep your momentum
                if (!(locked && !IsGrounded))
                {
                    float targetSpeed = locked ? 0f : moveInput * maxSpeed;
                    float rate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
                    if (!IsGrounded) rate *= airControl;
                    velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, rate * dt);
                }

                if (!locked && jumpBufferTimer > 0f && coyoteTimer > 0f)
                {
                    velocity.y = jumpVelocity;
                    jumpBufferTimer = 0f;
                    coyoteTimer = 0f;
                }
            }

            rb.gravityScale = velocity.y < 0f ? fallGravity : normalGravity;
            velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
            rb.linearVelocity = velocity;
        }

        // Small forward step at the start of a grounded attack
        public void Lunge(float speed)
        {
            if (IsGrounded) rb.linearVelocity = new Vector2(Facing * speed, rb.linearVelocity.y);
        }

        // Pushes the player away from the hit and briefly takes away control
        public void Knockback(Vector2 sourcePosition)
        {
            float direction = Mathf.Sign(transform.position.x - sourcePosition.x);
            stunTimer = hurtStunTime;
            dodgeTimer = 0f;
            rb.linearVelocity = new Vector2(direction * knockback.x, knockback.y);
        }

        public void ResetMotion()
        {
            rb.linearVelocity = Vector2.zero;
            stunTimer = 0f;
            dodgeTimer = 0f;
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null) return;
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }
}