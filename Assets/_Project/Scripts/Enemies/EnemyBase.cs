using System.Collections.Generic;
using System.Configuration.Assemblies;
using Forge.Core;
using Forge.Player;
using Forge.Utilities;
using UnityEngine;

namespace Forge.Enemies {
    // Shared enemy behavoir and info, health/knockback/armor/death
    // Each enemy registers itself into the gamespace 

    [RequireComponent(typeof(Rigidbody2D))]

    public abstract class EnemyBase : MonoBehaviour, IDamageable, IParryable
    {
        private static readonly List<EnemyBase> all = new List<EnemyBase>();
        public static IReadOnlyList<EnemyBase> All => all;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => all.Clear();
        public static void RespawnAll()
        {
            foreach (EnemyBase enemy in all) enemy.Respawn();
        }


        [Header("Identity")]
        [SerializeField] private string displayName = "Acolyte";
        [SerializeField] private bool isBoss;

        [Header("Stats")]
        [SerializeField] protected int maxHealth = 30;
        [SerializeField] private int ashReward = 20;
        [SerializeField] private Vector2 knockback = new Vector2(4f, 2f);

        [Header("References")]
        [SerializeField] protected SpriteFlash flash;
        [SerializeField] protected LayerMask groundLayer;
        [SerializeField] protected LayerMask playerLayer;

        protected Rigidbody2D rb;
        protected Transform player;
        protected PlayerHealth playerHealth;
        protected Collider2D[] colliders;
        private SpriteRenderer[] renderers;
        private Vector3 spawnPosition;

        public string DisplayName => displayName;
        public bool IsBoss => isBoss;
        public int Health { get; private set; }
        public int MaxHealth => maxHealth;
        public bool IsDead { get; private set; }
        public float TopY => colliders != null && colliders.Length > 0 ? colliders[0].bounds.max.y : transform.position.y + 1f;

        // While true, hits still do damage but don't knock back or interrupt
        // Would allow me to make something like a parry stamina meter, where as you parry more
        // you would start to still parry, but lose things like no knockback, or reduced damage 
        protected virtual bool HasSuperArmor => false;

        public void Configure(string enemyName, bool boss, int health, int ash, SpriteFlash bodyFlash, LayerMask ground, LayerMask playerMask)
        {
            displayName = enemyName;
            isBoss = boss;
            maxHealth = health;
            ashReward = ash;
            flash = bodyFlash;
            groundLayer = ground;
            playerLayer = playerMask;
        }

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            colliders = GetComponents<Collider2D>();
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            spawnPosition = transform.position;
            Health = maxHealth;
            all.Add(this);
        }

        protected virtual void OnDestroy() => all.Remove(this);

        protected virtual void Start()
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p == null) return;
            player = p.transform;
            playerHealth = p.GetComponent<PlayerHealth>();
        }

        public bool TakeDamage(DamageInfo info)
        {
            if (IsDead) return false;

            Health -= info.Amount;
            if (flash != null) flash.Flash(Color.white, 0.08f);

            if (Health <= 0)
            {
                Die();
                return true;
            }

            if (!HasSuperArmor)
            {
                float direction = Mathf.Sign(transform.position.x - info.SourcePosition.x);
                rb.linearVelocity = new Vector2(direction * knockback.x, knockback.y);
            }
            OnHurt();
            return true;
        }

        public abstract void OnParried();
        protected virtual void OnHurt() { }
        protected virtual void OnRespawned() { }

        private void Die()
        {
            IsDead = true;
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
            foreach (Collider2D c in colliders) c.enabled = false;
            foreach (SpriteRenderer r in renderers) r.enabled = false;

            if (player != null && player.TryGetComponent(out PlayerCurrency currency)) currency.AddAsh(ashReward);
            GameFeel.Shake(0.15f, 0.15f);
        }

        public void Respawn()
        {
            IsDead = false;
            Health = maxHealth;
            transform.position = spawnPosition;
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
            foreach (Collider2D c in colliders) c.enabled = true;
            foreach (SpriteRenderer r in renderers) r.enabled = true;
            if (flash != null) flash.Tint = null;
            OnRespawned();
        }
    }
}
