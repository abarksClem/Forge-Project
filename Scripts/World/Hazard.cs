using Forge.Player;
using UnityEngine;

namespace Forge.World
{
    // Spikes and pits, touching it is instant death, invulnerability or not
    public class Hazard : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other) => Touch(other);
        private void OnTriggerStay2D(Collider2D other) => Touch(other);

        private static void Touch(Collider2D other)
        {
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health != null) health.Kill();
        }
    }
}
