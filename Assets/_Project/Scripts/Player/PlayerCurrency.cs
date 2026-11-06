using Forge.UI;
using UnityEngine;

namespace Forge.Player {
    // Ash is the currency name, and is dropped by enemies
    // Dying will leave markers, touching them removes them, giving 
    // back half of the lost Ash from the death

    public class PlayerCurrency : MonoBehaviour {
        [SerializeField, Range(0f, 1f)] private float guiltPenalty = 0.5f;
        [SerializeField] private Sprite markerSprite;
        [SerializeField] private Color markerColor = new Color(0.65f, 0.35f, 1f);

        private GuiltMarker marker;

        public int Ash { get; private set; }
        public bool HasGuilt => marker != null;

        public void Configure(Sprite sprite) => markerSprite = sprite;

        public void AddAsh(int amount)
        {
            Ash += HasGuilt ? Mathf.CeilToInt(amount * (1f - guiltPenalty)) : amount;
        }

        public void PlaceGuilt(Vector3 position)
        {
            if (marker != null) Destroy(marker.gameObject);

            var go = new GameObject("Guilt Marker");
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            go.transform.localScale = Vector3.one * 0.5f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = markerSprite;
            sr.color = markerColor;
            sr.sortingOrder = 5;

            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = Vector2.one * 2f;

            marker = go.AddComponent<GuiltMarker>();
            marker.Init(this);
        }

        public void ClearGuilt()
        {
            if (marker != null) Destroy(marker.gameObject);
            marker = null;
            GameHUD.ShowMessage("Guilt absolved");
        }
    }
}