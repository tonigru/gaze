using UnityEngine;

namespace GazeCraft
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PuzzleSlot : MonoBehaviour
    {
        [SerializeField] private int slotId;
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.22f);
        [SerializeField] private Color gazedColor = new Color(0f, 1f, 0.2f, 0.85f);
        [SerializeField] private Color correctColor = new Color(0.35f, 1f, 0.45f, 0.6f);
        [SerializeField] private Color wrongColor = new Color(1f, 0.38f, 0.25f, 0.55f);
        [SerializeField] private float gazedScaleMultiplier = 1.12f;

        private SpriteRenderer spriteRenderer;
        private SpriteRenderer highlightRenderer;
        private Vector3 baseScale;
        private float flashUntil;
        private Color flashColor;

        public int SlotId => slotId;
        public bool Occupied { get; private set; }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            var highlight = transform.Find("Highlight");
            if (highlight != null)
            {
                highlightRenderer = highlight.GetComponent<SpriteRenderer>();
                highlightRenderer.enabled = false;
            }

            baseScale = transform.localScale;
            SetGazed(false);
        }

        private void Update()
        {
            if (Time.time < flashUntil)
            {
                spriteRenderer.color = flashColor;
            }
        }

        public void Configure(int id)
        {
            slotId = id;
        }

        public void SetGazed(bool isGazed)
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (Time.time >= flashUntil)
            {
                spriteRenderer.color = isGazed ? gazedColor : normalColor;
                SetHighlight(isGazed);
                transform.localScale = isGazed ? baseScale * gazedScaleMultiplier : baseScale;
            }
        }

        public void MarkResult(bool correct)
        {
            Occupied = correct || Occupied;
            flashColor = correct ? correctColor : wrongColor;
            flashUntil = Time.time + 0.45f;
            spriteRenderer.color = flashColor;
            SetHighlight(correct);
        }

        public void Clear()
        {
            Occupied = false;
            transform.localScale = baseScale;
            flashUntil = 0f;
            SetGazed(false);
        }

        private void SetHighlight(bool enabled)
        {
            if (highlightRenderer != null)
            {
                highlightRenderer.enabled = enabled;
            }
        }
    }
}
