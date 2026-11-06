using Forge.Enemies;
using Forge.Player;
using Forge.World;
using UnityEngine;

namespace Forge.UI
{
    // Quick on-screen HUD for the demo (uses OnGUI, so it needs no Canvas setup)
    // health bar, flasks, Ash, guilt warning, enemy and boss health bars, prompts and messages.
    // Replace with a proper UI Toolkit / Canvas HUD later 
    public class GameHUD : MonoBehaviour
    {
        private static string message;
        private static float messageUntil;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            message = null;
            messageUntil = 0f;
        }

        public static void ShowMessage(string text, float duration = 3f)
        {
            message = text;
            messageUntil = Time.unscaledTime + duration;
        }

        [SerializeField] private float bossBarRange = 14f;

        private PlayerHealth health;
        private PlayerCurrency currency;
        private PlayerCombat combat;
        private GUIStyle textStyle, centerStyle, bigStyle, smallStyle;

        private static readonly Color Dark = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color HealthRed = new Color(0.75f, 0.12f, 0.12f);
        private static readonly Color FlaskFull = new Color(0.35f, 0.85f, 0.8f);
        private static readonly Color FlaskEmpty = new Color(0.2f, 0.25f, 0.25f);
        private static readonly Color GuiltPurple = new Color(0.75f, 0.55f, 1f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.35f);

        private void Start()
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p == null) return;
            health = p.GetComponent<PlayerHealth>();
            currency = p.GetComponent<PlayerCurrency>();
            combat = p.GetComponent<PlayerCombat>();
        }

        private void OnGUI()
        {
            if (health == null) return;
            CreateStyles();

            // Scale everything to a 720p-tall virtual screen.
            float scale = Screen.height / 720f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;

            // Player status (top left)
            Bar(new Rect(20, 20, 280, 18), health.Health / (float)health.MaxHealth, HealthRed);
            for (int i = 0; i < health.MaxFlasks; i++)
                Fill(new Rect(20 + i * 24, 46, 16, 24), i < health.Flasks ? FlaskFull : FlaskEmpty);

            int y = 76;
            if (currency != null)
            {
                Label(new Rect(20, y, 400, 24), $"Ash: {currency.Ash}", Color.white, textStyle);
                y += 24;
                if (currency.HasGuilt)
                    Label(new Rect(20, y, 600, 24), "Guilt: Ash gain halved. Reclaim your marker.", GuiltPurple, textStyle);
            }

            // Enemy health bars
            Camera cam = Camera.main;
            EnemyBase boss = null;
            foreach (EnemyBase enemy in EnemyBase.All)
            {
                if (enemy.IsDead) continue;
                if (enemy.IsBoss)
                {
                    if (Vector2.Distance(enemy.transform.position, health.transform.position) < bossBarRange) boss = enemy;
                    continue;
                }
                if (cam == null || enemy.Health >= enemy.MaxHealth) continue;
                Vector3 sp = cam.WorldToScreenPoint(new Vector3(enemy.transform.position.x, enemy.TopY + 0.3f, 0f));
                if (sp.z < 0f) continue;
                Bar(new Rect(sp.x / scale - 25, (Screen.height - sp.y) / scale, 50, 5), enemy.Health / (float)enemy.MaxHealth, HealthRed);
            }

            if (boss != null)
            {
                float bw = Mathf.Min(600, width - 80);
                Label(new Rect((width - bw) / 2, 640, bw, 24), boss.DisplayName, Color.white, centerStyle);
                Bar(new Rect((width - bw) / 2, 666, bw, 14), boss.Health / (float)boss.MaxHealth, HealthRed);
            }

            // Prompts & messages
            if (combat != null && combat.RiposteReady)
                Label(new Rect(0, 250, width, 40), "RIPOSTE!", Gold, bigStyle);

            if (Shrine.InRange != null && !health.IsDead)
                Label(new Rect(0, 560, width, 30), "[E] Rest at shrine", Gold, centerStyle);

            if (health.IsDead)
                Label(new Rect(0, 300, width, 80), "YOU DIED", new Color(0.7f, 0.05f, 0.05f), bigStyle);

            if (!string.IsNullOrEmpty(message) && Time.unscaledTime < messageUntil)
                Label(new Rect(0, 130, width, 30), message, Color.white, centerStyle);

            Label(new Rect(20, 690, width - 40, 22),
                "Move A/D   Jump Space   Attack J   Parry K   Dodge L/Shift   Heal F   Rest E",
                new Color(1f, 1f, 1f, 0.6f), smallStyle);
        }

        private void CreateStyles()
        {
            if (textStyle != null) return;
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            centerStyle = new GUIStyle(textStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 18 };
            bigStyle = new GUIStyle(centerStyle) { fontSize = 48, fontStyle = FontStyle.Bold };
            smallStyle = new GUIStyle(textStyle) { fontSize = 13 };
        }

        private static void Fill(Rect rect, Color color)
        {
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, 0f, 0f);
        }

        private static void Bar(Rect rect, float fraction, Color color)
        {
            Fill(rect, Dark);
            Rect filled = rect;
            filled.width *= Mathf.Clamp01(fraction);
            Fill(filled, color);
        }

        private static void Label(Rect rect, string text, Color color, GUIStyle style)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.Label(rect, text, style);
            GUI.color = old;
        }
    }
}