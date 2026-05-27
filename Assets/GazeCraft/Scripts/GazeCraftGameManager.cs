using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace GazeCraft
{
    [RequireComponent(typeof(GazeCraftGazeProvider))]
    [RequireComponent(typeof(GazeCraftSpeechListener))]
    public sealed class GazeCraftGameManager : MonoBehaviour
    {
        [SerializeField] private Transform gazeCursor;
        [SerializeField] private Text statusText;
        [SerializeField] private float hitRadius = 0.35f;

        private GazeCraftGazeProvider gazeProvider;
        private GazeCraftSpeechListener speechListener;
        private PuzzlePiece gazedPiece;
        private PuzzleSlot gazedSlot;
        private PuzzlePiece heldPiece;
        private readonly List<PuzzlePiece> pieces = new();
        private readonly List<PuzzleSlot> slots = new();

        private void Awake()
        {
            gazeProvider = GetComponent<GazeCraftGazeProvider>();
            speechListener = GetComponent<GazeCraftSpeechListener>();
        }

        private void OnEnable()
        {
            speechListener.TakeRequested += TakeThat;
            speechListener.PutRequested += PutThat;
        }

        private void Start()
        {
            RefreshSceneReferences();
            SetStatus("Look at a puzzle piece and say \"Take that\". Say \"Put that\" over the matching slot.");
        }

        private void Update()
        {
            if (!gazeProvider.TryGetWorldPoint(out var gazePoint))
            {
                SetStatus("Waiting for gaze data...");
                return;
            }

            UpdateCursor(gazePoint);
            UpdateGazedObjects(gazePoint);

            if (heldPiece != null)
            {
                heldPiece.Follow(gazePoint);
            }

            if (AllPiecesPlaced())
            {
                SetStatus("Puzzle complete.");
            }
            else
            {
                UpdateLookStatus();
            }
        }

        private void OnDisable()
        {
            speechListener.TakeRequested -= TakeThat;
            speechListener.PutRequested -= PutThat;
        }

        public void RefreshSceneReferences()
        {
            pieces.Clear();
            pieces.AddRange(FindObjectsByType<PuzzlePiece>(FindObjectsInactive.Exclude));
            slots.Clear();
            slots.AddRange(FindObjectsByType<PuzzleSlot>(FindObjectsInactive.Exclude));
        }

        public void TakeThat()
        {
            if (heldPiece != null)
            {
                SetStatus("Already holding a piece. Look at a slot and say \"Put that\".");
                return;
            }

            if (gazedPiece == null)
            {
                SetStatus("No puzzle piece under gaze.");
                return;
            }

            heldPiece = gazedPiece;
            heldPiece.PickUp();
            SetStatus("Piece picked up. Look at the target slot and say \"Put that\".");
        }

        public void PutThat()
        {
            if (heldPiece == null)
            {
                SetStatus("No piece is held. Look at a piece and say \"Take that\".");
                return;
            }

            if (gazedSlot == null)
            {
                heldPiece.ReturnHome();
                heldPiece = null;
                SetStatus("No slot under gaze. Piece returned.");
                return;
            }

            var correct = heldPiece.TryDropOn(gazedSlot);
            gazedSlot.MarkResult(correct);
            heldPiece = null;
            SetStatus(correct ? "Correct." : "Wrong slot. Try another piece.");
        }

        private void UpdateCursor(Vector3 gazePoint)
        {
            if (gazeCursor == null)
            {
                return;
            }

            gazeCursor.position = gazePoint;
        }

        private void UpdateGazedObjects(Vector3 gazePoint)
        {
            var newPiece = FindClosestPiece(gazePoint);
            var newSlot = FindClosestSlot(gazePoint);

            if (newPiece != gazedPiece)
            {
                if (gazedPiece != null)
                {
                    gazedPiece.SetGazed(false);
                }

                gazedPiece = newPiece;
            }

            if (newSlot != gazedSlot)
            {
                if (gazedSlot != null)
                {
                    gazedSlot.SetGazed(false);
                }

                gazedSlot = newSlot;
            }

            if (gazedPiece != null)
            {
                gazedPiece.SetGazed(true);
            }

            if (gazedSlot != null)
            {
                gazedSlot.SetGazed(true);
            }
        }

        private PuzzlePiece FindClosestPiece(Vector3 gazePoint)
        {
            return pieces
                .Where(piece => piece != null && !piece.IsHeld && !piece.IsPlaced)
                .Where(piece => IsGazeInside(piece.GetComponent<Collider2D>(), gazePoint))
                .OrderBy(piece => Vector2.Distance(gazePoint, piece.transform.position))
                .FirstOrDefault();
        }

        private PuzzleSlot FindClosestSlot(Vector3 gazePoint)
        {
            return slots
                .Where(slot => slot != null && !slot.Occupied)
                .Where(slot => IsGazeInside(slot.GetComponent<Collider2D>(), gazePoint))
                .OrderBy(slot => Vector2.Distance(gazePoint, slot.transform.position))
                .FirstOrDefault();
        }

        private bool IsGazeInside(Collider2D targetCollider, Vector3 gazePoint)
        {
            if (targetCollider == null)
            {
                return false;
            }

            if (targetCollider.OverlapPoint(gazePoint))
            {
                return true;
            }

            return Vector2.Distance(targetCollider.ClosestPoint(gazePoint), gazePoint) <= hitRadius;
        }

        private bool AllPiecesPlaced()
        {
            return pieces.Count > 0 && pieces.All(piece => piece != null && piece.IsPlaced);
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private void UpdateLookStatus()
        {
            var diagnostic = " [" + gazeProvider.LastSource + " events:" + gazeProvider.TobiiEventCount + " xy:" + gazeProvider.LastDisplayPoint.x.ToString("0.00") + "," + gazeProvider.LastDisplayPoint.y.ToString("0.00") + " offset:" + gazeProvider.DisplayPointOffset.x.ToString("0.00") + "," + gazeProvider.DisplayPointOffset.y.ToString("0.00") + "]\n" + gazeProvider.LastTobiiStatus + "\n" + speechListener.Status;

            if (heldPiece != null)
            {
                SetStatus((gazedSlot != null ? "Looking at " + gazedSlot.name + ". Say \"Put that\"." : "Holding piece. Look at a slot.") + diagnostic);
                return;
            }

            if (gazedPiece != null)
            {
                SetStatus("Looking at " + gazedPiece.name + ". Say \"Take that\"." + diagnostic);
                return;
            }

            if (gazedSlot != null)
            {
                SetStatus("Looking at " + gazedSlot.name + "." + diagnostic);
                return;
            }

            SetStatus("Look at a puzzle piece." + diagnostic);
        }
    }
}
