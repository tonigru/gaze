using UnityEngine;

namespace GazeCraft
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PuzzlePiece : MonoBehaviour
    {
        [SerializeField] private int slotId;
        [SerializeField] private Vector3 carryOffset = new Vector3(0f, 0.35f, 0f);
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color gazedColor = new Color(0.1f, 1f, 0.25f, 1f);
        [SerializeField] private Color heldColor = new Color(0.55f, 1f, 0.75f, 1f);
        [SerializeField] private float gazedScaleMultiplier = 1.18f;

        private SpriteRenderer spriteRenderer;
        private SpriteRenderer highlightRenderer;
        private Vector3 homePosition;
        private Vector3 baseScale;

        public int SlotId => slotId;
        public bool IsHeld { get; private set; }
        public bool IsPlaced { get; private set; }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            var highlight = transform.Find("Highlight");
            if (highlight != null)
            {
                highlightRenderer = highlight.GetComponent<SpriteRenderer>();
                highlightRenderer.enabled = false;
            }

            homePosition = transform.position;
            baseScale = transform.localScale;
        }

        public void Configure(int id, Vector3 home)
        {
            slotId = id;
            homePosition = home;
        }

        public void SetGazed(bool isGazed)
        {
            if (IsHeld || IsPlaced)
            {
                return;
            }

            spriteRenderer.color = normalColor;
            SetHighlight(isGazed);
            transform.localScale = isGazed ? baseScale * gazedScaleMultiplier : baseScale;
        }

        public void PickUp()
        {
            if (IsPlaced)
            {
                return;
            }

            IsHeld = true;
            transform.localScale = baseScale * gazedScaleMultiplier;
            spriteRenderer.color = heldColor;
            SetHighlight(true);
            spriteRenderer.sortingOrder = 20;
        }

        public void Follow(Vector3 gazePoint)
        {
            if (!IsHeld)
            {
                return;
            }

            transform.position = Vector3.Lerp(transform.position, gazePoint + carryOffset, Time.deltaTime * 16f);
        }

        public bool TryDropOn(PuzzleSlot slot)
        {
            if (slot == null || slot.Occupied || slot.SlotId != slotId)
            {
                ReturnHome();
                return false;
            }

            IsHeld = false;
            IsPlaced = true;
            transform.localScale = baseScale;
            transform.position = slot.transform.position;
            spriteRenderer.color = normalColor;
            SetHighlight(false);
            spriteRenderer.sortingOrder = 5;
            return true;
        }

        public void ReturnHome()
        {
            IsHeld = false;
            transform.localScale = baseScale;
            transform.position = homePosition;
            spriteRenderer.color = normalColor;
            SetHighlight(false);
            spriteRenderer.sortingOrder = 10;
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
