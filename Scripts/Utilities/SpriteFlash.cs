using UnityEngine;

namespace Forge.Utilities
{
    // Handles color effect so nothing fights 
    // Short Flash, longer Tint, Blinks all will have

    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFlash : MonoBehaviour
    {
        private SpriteRenderer sr;
        private Color baseColor;
        private Color flashColor;
        private float flashUntil;

        public Color? Tint { get; set; }
        public bool Blink { get; set; }

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            baseColor = sr.color;
        }

        public void Flash(Color color, float duration)
        {
            flashColor = color;
            flashUntil = Time.time + duration;
        }

        private void LateUpdate()
        {
            Color c = Time.time < flashUntil ? flashColor : (Tint ?? baseColor);
            if (Blink) c.a = Mathf.FloorToInt(Time.time * 20f) % 2 == 0 ? 0.35f : 1f;
            sr.color = c;
        }
    }
}
