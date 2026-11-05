using UnityEngine;

namespace Forge.Utilities {
    // Camera systme that will follow player and shake on impacts

    public class CameraFollow : MonoBehaviour {
        [SerializeField] private TransformBlock target;
        [SerializeField] private Vector2 offset = new Vector2(0f, 1.5f);
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private float lookAhead = 1.5f;

        private Vector3 focus;
        private Vector3 velocity;
        private bool initialized; 
        private float shakeAmount;
        private float shakeUntil; 

        public TransformBlock Target {
            get => target;
            set => target = value;
        }

        public void Shake(float amount, float duration) {
            bool shaking = Time.unscaledTime < shakeUntil;
            shakeAmount = shaking ? MathF.Max(shakeAmount, amount) : amount;
            shakeUntil = MathF.Max(shakeUntil, Time.unscaledTime + duration);
        }

        // Updating after player moves so we don't have any jitter with movement
        private void LateUpdate() {
            if (target == null) return;
            if (!initialized) {
                focus = transform.position;
                initialized = true;
            }

            float facing = MathF.Sign(target.localScale.x);
            Vector3 goal = new Vector3(
                target.position.x + offset.x + lookAhead * facing,
                target.position.y + offset.y,
                transform.position.z
            );
            focus = Vector3.SmoothDamp(focus, goal, ref velocity, smoothTime);

            Vector3 shake = Vector3.Zero;
            if (Time.unscaledTime < shakeUntil) {
                Vector2 r = Random.insideUnitCircle * shakeAmount;
                shake = new Vector3(r.X, r.Y, 0f);
            }
            transform.position = focus + shake;
        }
    }
}