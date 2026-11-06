using Forge.Core;
using UnityEngine;

namespace Forge.Enemies
{
    // Melee enemy: Patrol -> Chase -> Windup (glows orange, the "tell") -> Attack -> Recover.
    // Windup and Attack have super armor, so you can't just mash through them:
    // dodge through the swing or parry it. A parried enemy is Staggered (purple) and wide open.

    public class MeleeEnemy : EnemyBase
    {
        private enum State { Patrol, Chase, Windup, Attack, Recover, Hurt, Stagger }

        [Header("Movement")]
        [SerializeField] private float patrolSpeed = 1.5f;
        [SerializeField] private float chaseSpeed = 3f;
        [SerializeField] private float patrolDistance = 3f;

        [Header("Senses")]
        [SerializeField] private float detectRange = 7f;
        [SerializeField] private float attackRange = 1.6f;
        [SerializeField] private float loseSightTime = 2f;

        [Header("Attack")]
        [SerializeField] private int damage = 20;
        [SerializeField] private float windupTime = 0.6f;
        [SerializeField] private float attackTime = 0.15f;
        [SerializeField] private float recoverTime = 0.7f;
        [SerializeField] private float attackLunge = 3f;
        [SerializeField] private Vector2 hitboxOffset = new Vector2(1f, 0f);
        [SerializeField] private Vector2 hitboxSize = new Vector2(1.5f, 1.2f);
        [SerializeField] private SpriteRenderer slash;
        [SerializeField] private Color tellColor = new Color(1f, 0.5f, 0.15f);

        [Header("Reactions")]
        [SerializeField] private float hurtTime = 0.25f;
        [SerializeField] private float staggerTime = 1.5f;
        [SerializeField] private Color staggerColor = new Color(0.6f, 0.5f, 1f);

        private State state;
        private float timer;
        private float lostTimer;
        private int facing = -1;
        private bool hasHit;
        private float spawnX;

        protected override bool HasSuperArmor => state == State.Windup || state == State.Attack;

        // Used by the scene builder
        public void SetupAttack(int attackDamage, float windup, float speedMultiplier, float range,
                                Vector2 hitOffset, Vector2 hitSize, SpriteRenderer slashRenderer)
        {
            damage = attackDamage;
            windupTime = windup;
            patrolSpeed *= speedMultiplier;
            chaseSpeed *= speedMultiplier;
            attackRange = range;
            hitboxOffset = hitOffset;
            hitboxSize = hitSize;
            slash = slashRenderer;
        }

        protected override void Awake()
        {
            base.Awake();
            spawnX = transform.position.x;
            if (slash != null) slash.enabled = false;
            SetFacing(-1);
        }

        private void FixedUpdate()
        {
            if (IsDead) return;
            float dt = Time.fixedDeltaTime;
            timer -= dt;

            Vector2 toPlayer = player != null ? (Vector2)(player.position - transform.position) : Vector2.zero;
            bool canSee = player != null && playerHealth != null && !playerHealth.IsDead &&
                          Mathf.Abs(toPlayer.x) <= detectRange && Mathf.Abs(toPlayer.y) <= 2.5f;

            switch (state)
            {
                case State.Patrol:
                    if (canSee) { Enter(State.Chase); break; }
                    float fromSpawn = transform.position.x - spawnX;
                    bool tooFar = Mathf.Abs(fromSpawn) >= patrolDistance && (int)Mathf.Sign(fromSpawn) == facing;
                    if (tooFar || !GroundAhead() || WallAhead()) SetFacing(-facing);
                    SetSpeed(facing * patrolSpeed);
                    break;

                case State.Chase:
                    lostTimer = canSee ? 0f : lostTimer + dt;
                    if (lostTimer > loseSightTime) { Enter(State.Patrol); break; }
                    if (Mathf.Abs(toPlayer.x) > 0.1f) SetFacing(toPlayer.x > 0f ? 1 : -1);
                    if (canSee && Mathf.Abs(toPlayer.x) <= attackRange && Mathf.Abs(toPlayer.y) < 1.5f)
                    {
                        Enter(State.Windup);
                        break;
                    }
                    SetSpeed(GroundAhead() && !WallAhead() ? facing * chaseSpeed : 0f);
                    break;

                case State.Windup:
                    SetSpeed(0f);
                    if (timer <= 0f) Enter(State.Attack);
                    break;

                case State.Attack:
                    if (!hasHit) TryHitPlayer();
                    if (state == State.Attack && timer <= 0f) Enter(State.Recover);
                    break;

                case State.Recover:
                    Brake(dt);
                    if (timer <= 0f) Enter(canSee ? State.Chase : State.Patrol);
                    break;

                case State.Hurt:
                case State.Stagger:
                    Brake(dt);
                    if (timer <= 0f) Enter(State.Chase);
                    break;
            }
        }

        private void Enter(State next)
        {
            state = next;
            if (flash != null) flash.Tint = null;
            if (slash != null) slash.enabled = false;

            switch (next)
            {
                case State.Windup:
                    timer = windupTime;
                    if (flash != null) flash.Tint = tellColor;
                    break;
                case State.Attack:
                    timer = attackTime;
                    hasHit = false;
                    if (slash != null) slash.enabled = true;
                    SetSpeed(facing * attackLunge);
                    break;
                case State.Recover: timer = recoverTime; break;
                case State.Hurt: timer = hurtTime; break;
                case State.Stagger:
                    timer = staggerTime;
                    if (flash != null) flash.Tint = staggerColor;
                    break;
                case State.Chase: lostTimer = 0f; break;
            }
        }

        private void TryHitPlayer()
        {
            Vector2 center = (Vector2)transform.position + new Vector2(hitboxOffset.x * facing, hitboxOffset.y);
            Collider2D hit = Physics2D.OverlapBox(center, hitboxSize, 0f, playerLayer);
            if (hit == null) return;
            var target = hit.GetComponentInParent<IDamageable>();
            if (target == null) return;

            hasHit = true; // one hit per swing, even if dodged or parried
            target.TakeDamage(new DamageInfo(damage, transform.position, gameObject, parryable: true));
        }

        public override void OnParried()
        {
            if (IsDead) return;
            Enter(State.Stagger);
            SetSpeed(-facing * 3f);
        }

        protected override void OnHurt()
        {
            if (state == State.Windup || state == State.Attack || state == State.Stagger) return;
            Enter(State.Hurt);
        }

        protected override void OnRespawned()
        {
            SetFacing(-1);
            Enter(State.Patrol);
        }

        private bool GroundAhead()
        {
            Bounds b = colliders[0].bounds;
            Vector2 origin = new Vector2(b.center.x + facing * (b.extents.x + 0.1f), b.min.y + 0.1f);
            return Physics2D.Raycast(origin, Vector2.down, 0.6f, groundLayer).collider != null;
        }

        private bool WallAhead()
        {
            Bounds b = colliders[0].bounds;
            return Physics2D.Raycast(b.center, new Vector2(facing, 0f), b.extents.x + 0.15f, groundLayer).collider != null;
        }

        private void SetFacing(int direction)
        {
            facing = direction;
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * direction;
            transform.localScale = scale;
        }

        private void SetSpeed(float vx) => rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
        private void Brake(float dt) => SetSpeed(Mathf.MoveTowards(rb.linearVelocity.x, 0f, 20f * dt));

        private void OnDrawGizmosSelected()
        {
            float f = Mathf.Sign(transform.localScale.x);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + new Vector3(hitboxOffset.x * f, hitboxOffset.y), hitboxSize);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, new Vector3(detectRange * 2f, 5f, 0f));
        }
    }
}
