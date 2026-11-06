using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Dataflow;
using UnityEngine;

namespace Forge.Player {
    // Floating marker left on death. Touching it removes it and gets rid of the death penalty

    public class GuiltMarker : MonoBehaviour {
        private PlayerCurrency owner;
        private Vector3 basePosition;
        
        public void Init(PlayerCurrency currency) {
            owner = currency;
            basePosition = transform.position;
        }

        private void Update() {
            transform.position = basePosition + Vector3.up * (MathF.Sin(Time.time * 2f) * 0.15f);
        }

        private void OnTriggerStay2D(Collider2D other) {
            if (owner == null) return;
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health == null || health.IsDead || health.GetComponent<PlayerCurrency>() != owner) return;
            owner.ClearGuilt();
        }
    }
}