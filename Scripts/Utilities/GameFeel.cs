using UnityEngine;

namespace Forge.Utilities
{
    // Helpers for game "feel". Freeze frames and camera shakes

    public class GameFeel : MonoBehaviour {
        private static GameFeel instance;
        private float resumeAt;
        private static GameFeel Instance {
            get {
                if (instance == null) instance = new GameObject("GameFeel").AddComponent<GameFeel>();
                return instance;
            }
        }

        public static void HitStop(float duration) {
            GameFeel feel = Instance;
            feel.resumeAt = Mathf.Max(feel.resumeAt, Time.unscaledTime + duration);
            Time.timeScale = 0f;
        }

        public static void Shake(float amount, float duration) {
            Camera cam = Camera.main;
            if (cam != null && cam.TryGetComponent(out CameraFollow follow)) follow.Shake(amount, duration);
        }
        private void Update() {
            if (Time.timeScale == 0f && Time.unscaledTime >= resumeAt) Time.timeScale = 1f;
        }

        private void OnDestroy() {
            Time.timeScale = 1f;
        }
    }
}