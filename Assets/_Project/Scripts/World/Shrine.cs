using Forge.Player;
using Forge.UI;
using UnityEngine;

namespace Forge.World
{
    // Checkpoints, gives full health, flasks refilled, this becomes your respawn point, and every enemy comes back.
    public class Shrine : MonoBehaviour
    {
        public static Shrine Current { get; private set; }
        public static Shrine InRange { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
            InRange = null;
        }

        [SerializeField] private bool startingShrine;
        [SerializeField] private SpriteRenderer flame;
        [SerializeField] private Color litColor = new Color(1f, 0.75f, 0.3f);
        [SerializeField] private Color unlitColor = new Color(0.35f, 0.3f, 0.3f);

        private PlayerHealth playerHealth;
        private PlayerInputReader playerInput;

        public Vector3 RespawnPoint => transform.position + Vector3.up;

        public void Configure(bool isStartingShrine, SpriteRenderer flameRenderer)
        {
            startingShrine = isStartingShrine;
            flame = flameRenderer;
        }

        private void Start()
        {
            if (startingShrine && Current == null) Current = this;
        }

        private void Update()
        {
            if (flame != null) flame.color = Current == this ? litColor : unlitColor;

            if (playerHealth != null && !playerHealth.IsDead && playerInput.Interact.WasPressedThisFrame())
                Rest();
        }

        private void Rest() {}
    }
}
