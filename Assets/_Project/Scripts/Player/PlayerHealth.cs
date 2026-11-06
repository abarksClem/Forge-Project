using System.Collections;
using Forge.Core;
using Forge.Utilities;
using UnityEngine;
using Forge.World;
using Forge.Enemies;

namespace Forge.Player {
    // Health, health pots, i-frames, deaths, and respawning all handled here

    [RequireComponent(typeof(PlayerController))]

    public class PlayerHealth : MonoBehaviour, IDamageable {
        [Header("Health")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private float hurtInvulnerability = 0.8f;
        [SerializeField] private float respawnDelay = 1.8f;

        [Header("Flasks")]
        [SerializeField] private int maxFlasks = 2;
        [SerializeField] private int healAmount = 45;
        [SerializeField] private float healDuration = 0.7f; // you're vulnerable while drinking
        [SerializeField] private Color healColor = new Color(0.4f, 0.9f, 0.8f);

        [SerializeField] private SpriteFlash flash;

        private PlayerController controller;
        private PlayerCombat combat;
        private PlayerInputReader input;
        private PlayerCurrency currency;

        private float invulnerableTimer;
        private bool blinkWhileInvulnerable;
        private Coroutine healRoutine;
        private Vector3 startPosition;

        public int Health { get; private set; }
        public int MaxHealth => maxHealth;
        public int Flasks { get; private set; }
        public int MaxFlasks => maxFlasks;
        public bool IsDead { get; private set; }
        public bool IsHealing { get; private set; }
        public bool IsInvulnerable => invulnerableTimer > 0f;

        public void Configure(SpriteFlash bodyFlash) => flash = bodyFlash;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            combat = GetComponent<PlayerCombat>();
            input = GetComponent<PlayerInputReader>();
            currency = GetComponent<PlayerCurrency>();
            startPosition = transform.position;
            RestoreAll();
        }

        private void Update()
        {
            if (invulnerableTimer > 0f)
            {
                invulnerableTimer -= Time.deltaTime;
                if (flash != null) flash.Blink = blinkWhileInvulnerable && invulnerableTimer > 0f;
            }

            if (!IsDead && input.Heal.WasPressedThisFrame()) TryHeal();
        }

        public void GrantInvulnerability(float duration, bool blink)
        {
            if (duration <= invulnerableTimer) return;
            invulnerableTimer = duration;
            blinkWhileInvulnerable = blink;
        }

        private void TryHeal()
        {
            if (Flasks <= 0 || Health >= maxHealth || IsHealing) return;
            if (!controller.IsGrounded || controller.IsDodging || controller.IsStunned || combat.IsBusy) return;
            healRoutine = StartCoroutine(HealRoutine());
        }

        private IEnumerator HealRoutine()
        {
            IsHealing = true;
            Flasks--; // spent the moment you start drinking, even if you get hit
            if (flash != null) flash.Tint = healColor;
            yield return new WaitForSeconds(healDuration);

            Health = Mathf.Min(maxHealth, Health + healAmount);
            if (flash != null)
            {
                flash.Tint = null;
                flash.Flash(new Color(0.6f, 1f, 0.7f), 0.2f);
            }
            IsHealing = false;
            healRoutine = null;
        }

        private void CancelHeal()
        {
            if (healRoutine != null) StopCoroutine(healRoutine);
            healRoutine = null;
            if (IsHealing && flash != null) flash.Tint = null;
            IsHealing = false;
        }

        public bool TakeDamage(DamageInfo info)
        {
            if (IsDead || IsInvulnerable) return false;
            if (combat != null && combat.TryParry(info)) return false;

            CancelHeal();
            if (combat != null) combat.CancelAction();

            Health -= info.Amount;
            if (flash != null) flash.Flash(new Color(1f, 0.3f, 0.3f), 0.12f);
            GameFeel.HitStop(0.06f);
            GameFeel.Shake(0.2f, 0.15f);

            if (Health <= 0)
            {
                Die(false);
                return true;
            }

            controller.Knockback(info.SourcePosition);
            GrantInvulnerability(hurtInvulnerability, true);
            return true;
        }

        // Instant death (spikes, pits) ignores invulnerability
        public void Kill()
        {
            if (IsDead) return;
            CancelHeal();
            if (combat != null) combat.CancelAction();
            Die(true);
        }

        private void Die(bool byHazard)
        {
            IsDead = true;
            Health = 0;
            invulnerableTimer = 0f;
            if (flash != null)
            {
                flash.Blink = false;
                flash.Tint = new Color(0.25f, 0.25f, 0.3f);
            }

            // Falling into spikes/pit leaves the marker where you last stood safely
            if (currency != null) currency.PlaceGuilt(byHazard ? controller.LastSafePosition : transform.position);
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);

            transform.position = Shrine.Current != null ? Shrine.Current.RespawnPoint : startPosition;
            controller.ResetMotion();
            RestoreAll();
            IsDead = false;
            if (flash != null) flash.Tint = null;
            EnemyBase.RespawnAll();
            GrantInvulnerability(1f, true);
        }

        // Full health and flasks
        public void RestoreAll()
        {
            Health = maxHealth;
            Flasks = maxFlasks;
        }
    }
}
