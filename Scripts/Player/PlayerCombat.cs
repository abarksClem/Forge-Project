using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Forge.Core;
using Forge.Utilities;
using UnityEngine;

namespace Forge.Player {
    [System.Serializable]
    public class AttackStep {
        public int damage = 10;
        public float windup = 0.1f;
        public float active = 0.08f;
        public float recovery = 0.25f;
        public float lunge = 2.5f;
    }

    // 3-hit sword combo, kinda standard I feel for games of these nature. Attacks can be cancelled 
    // but only by dodging with proper timing.
    public class PlayerCombat : MonoBehaviour {
        [Header("Combo")]
        [SerializeField] private AttackStep[] combo = {
            new AttackStep { damage = 10, windup = 0.1f, active = 0.08f, recovery = 0.22f, lunge = 2.5f },
            new AttackStep { damage = 10, windup = 0.08f, active = 0.08f, recovery = 0.22f, lunge = 2.5f },
            new AttackStep { damage = 20, windup = 0.14f, active = 0.10f, recovery = 0.40f, lunge = 4f },
        };

        [SerializeField, Range(0f, 1f)] private float comboCancelPoint = 0.3f;
        [SerializeField] private Vector2 hitboxOffset = new Vector2(0.9f, 0.1f);
        [SerializeField] private Vector2 hitboxSize = new Vector2(1.5f, 1.1f);
        [SerializeField] private LayerMask hitLayers;
        [SerializeField] private SpriteRenderer slash;

        [Header("Parry")]
        [SerializeField] private float parryWindow = 0.25f;
        [SerializeField] private float parryRecovery = 0.35f;
        [SerializeField] private float riposteWindow = 1.2f;
        [SerializeField] private int riposteMultiplier = 3;
        [SerializeField] private Color parryColor = new Color(1f, 0.85f, 0.35f);

        [SerializeField] private SpriteFlash flash;

        private PlayerController controller;
        private PlayerInputReader input;
        private PlayerHealth health;

        private Coroutine action;
        private bool attackQueued;
        private bool parryActive;
        private bool inRecovery;
        private float riposteTimer;
        private readonly HashSet<IDamageable> hitThisSwing = new HashSet<IDamageable>();

        public bool IsAttacking { get; private set; }
        public bool IsParrying { get; private set; }
        public bool IsBusy => IsAttacking || IsParrying;
        public bool CanCancelIntoDodge => IsAttacking && inRecovery;
        public bool RiposteReady => riposteTimer > 0f;

        public void Configure(LayerMask enemies, SpriteRenderer slashRenderer, SpriteFlash bodyFlash)
        {
            hitLayers = enemies;
            slash = slashRenderer;
            flash = bodyFlash;
        }

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            input = GetComponent<PlayerInputReader>();
            health = GetComponent<PlayerHealth>();
            if (slash != null) slash.enabled = false;
        }

        private void Update()
        {
            riposteTimer -= Time.deltaTime;
            if (health.IsDead || health.IsHealing || controller.IsStunned || controller.IsDodging) return;

            if (input.Attack.WasPressedThisFrame())
            {
                if (IsAttacking) attackQueued = true;          // buffer the next combo hit
                else if (!IsParrying) action = StartCoroutine(AttackRoutine());
            }
            else if (input.Parry.WasPressedThisFrame() && !IsBusy)
            {
                action = StartCoroutine(ParryRoutine());
            }
        }

        private IEnumerator AttackRoutine()
        {
            IsAttacking = true;
            int step = 0;

            while (true)
            {
                AttackStep attack = combo[step];
                attackQueued = false;
                inRecovery = false;

                bool riposte = riposteTimer > 0f;
                if (riposte) riposteTimer = 0f;

                controller.Lunge(attack.lunge);
                yield return new WaitForSeconds(attack.windup);

                // Active frames, will check for hits every frame.
                hitThisSwing.Clear();
                if (slash != null)
                {
                    slash.enabled = true;
                    slash.color = riposte ? new Color(1f, 0.8f, 0.3f, 0.85f) : new Color(1f, 1f, 1f, 0.55f);
                }
                int damage = riposte ? attack.damage * riposteMultiplier : attack.damage;
                for (float t = 0f; t < attack.active; t += Time.deltaTime)
                {
                    DoHits(damage, riposte);
                    yield return null;
                }
                if (slash != null) slash.enabled = false;

                // Recovery = you're committed, unless you queued the next hit or dodge out.
                inRecovery = true;
                bool chain = false;
                for (float t = 0f; t < attack.recovery; t += Time.deltaTime)
                {
                    if (attackQueued && step < combo.Length - 1 && t >= attack.recovery * comboCancelPoint)
                    {
                        chain = true;
                        break;
                    }
                    yield return null;
                }

                if (!chain) break;
                step++;
            }

            EndAction();
        }

        private void DoHits(int damage, bool riposte)
        {
            Vector2 center = (Vector2)transform.position + new Vector2(hitboxOffset.x * controller.Facing, hitboxOffset.y);
            foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, hitboxSize, 0f, hitLayers))
            {
                var target = hit.GetComponentInParent<IDamageable>();
                if (target == null || !hitThisSwing.Add(target)) continue;

                if (target.TakeDamage(new DamageInfo(damage, transform.position, gameObject)))
                {
                    GameFeel.HitStop(riposte ? 0.15f : 0.05f);
                    GameFeel.Shake(riposte ? 0.25f : 0.08f, 0.12f);
                }
            }
        }

        private IEnumerator ParryRoutine()
        {
            IsParrying = true;
            parryActive = true;
            if (flash != null) flash.Tint = parryColor;
            yield return new WaitForSeconds(parryWindow);

            // Missed the timing: short window where you're open.
            parryActive = false;
            if (flash != null) flash.Tint = null;
            yield return new WaitForSeconds(parryRecovery);
            EndAction();
        }

        // Called by PlayerHealth before taking damage. Returns true if the hit was parried
        public bool TryParry(DamageInfo info)
        {
            if (!parryActive || !info.Parryable) return false;

            // Must be facing the attacker
            float side = Mathf.Sign(info.SourcePosition.x - transform.position.x);
            if ((int)side != controller.Facing) return false;

            CancelAction();
            riposteTimer = riposteWindow;
            if (info.Source != null) info.Source.GetComponent<IParryable>()?.OnParried();

            GameFeel.HitStop(0.12f);
            GameFeel.Shake(0.15f, 0.15f);
            if (flash != null) flash.Flash(Color.white, 0.15f);
            return true;
        }

        public void CancelAction()
        {
            if (action != null) StopCoroutine(action);
            EndAction();
        }

        private void EndAction()
        {
            action = null;
            IsAttacking = false;
            IsParrying = false;
            parryActive = false;
            inRecovery = false;
            attackQueued = false;
            if (slash != null) slash.enabled = false;
            if (flash != null) flash.Tint = null;
        }

        private void OnDrawGizmosSelected()
        {
            float facing = Mathf.Sign(transform.localScale.x);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position + new Vector3(hitboxOffset.x * facing, hitboxOffset.y), hitboxSize);
        }
    }
}
