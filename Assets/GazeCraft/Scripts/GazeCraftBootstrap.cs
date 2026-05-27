using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GazeCraft
{
    public sealed class GazeCraftBootstrap : MonoBehaviour
    {
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private Sprite fallbackSprite;

        private const string LayoutVersionObjectName = "GazeCraft Raised Layout v1";
        private const float ArtPixelsPerUnit = 512f;
        private static readonly Dictionary<string, Sprite> RuntimeSprites = new();

        private void Start()
        {
            if (buildOnStart && NeedsRuntimeBuild())
            {
                ClearGeneratedRuntimeScene();
                BuildRuntimeScene();
            }
        }

        private bool NeedsRuntimeBuild()
        {
            return FindAnyObjectByType<GazeCraftGameManager>() == null
                || FindObjectsByType<PuzzlePiece>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length != 9
                || FindObjectsByType<PuzzleSlot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length != 9
                || GameObject.Find("GazeCraft Neon Background") == null
                || GameObject.Find(LayoutVersionObjectName) == null;
        }

        private void ClearGeneratedRuntimeScene()
        {
            DestroyNamed("GazeCraft Game Manager");
            DestroyNamed("GazeCraft UI");
            DestroyNamed("Gaze Cursor");
            DestroyNamed("GazeCraft Neon Background");
            DestroyNamed("Reference Image");
            DestroyNamed(LayoutVersionObjectName);

            foreach (var piece in FindObjectsByType<PuzzlePiece>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                DestroySafe(piece.gameObject);
            }

            foreach (var slot in FindObjectsByType<PuzzleSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                DestroySafe(slot.gameObject);
            }
        }

        private void DestroyNamed(string objectName)
        {
            var obj = GameObject.Find(objectName);
            if (obj != null)
            {
                DestroySafe(obj);
            }
        }

        private void DestroySafe(GameObject obj)
        {
            if (Application.isPlaying)
            {
                obj.SetActive(false);
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }

        public void BuildRuntimeScene()
        {
            var camera = EnsureCamera();
            EnsureTobiiEyeTracker();

            var managerObject = new GameObject("GazeCraft Game Manager");
            var gazeProvider = managerObject.AddComponent<GazeCraftGazeProvider>();
            managerObject.AddComponent<GazeCraftSpeechListener>();
            var manager = managerObject.AddComponent<GazeCraftGameManager>();

            var statusText = CreateStatusCanvas();
            var cursor = CreateCursor();
            AssignPrivateReference(gazeProvider, "targetCamera", camera);
            AssignPrivateReference(manager, "statusText", statusText);
            AssignPrivateReference(manager, "gazeCursor", cursor.transform);

            CreateBoard();
            new GameObject(LayoutVersionObjectName);
            manager.RefreshSceneReferences();
        }

        private Camera EnsureCamera()
        {
            var existing = Camera.main;
            if (existing != null)
            {
                existing.orthographic = true;
                existing.orthographicSize = 6.7f;
                existing.transform.position = new Vector3(0f, 0f, -10f);
                existing.clearFlags = CameraClearFlags.SolidColor;
                existing.backgroundColor = new Color(0.015f, 0.028f, 0.045f);
                return existing;
            }

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6.7f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.028f, 0.045f);
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            return camera;
        }

        private void EnsureTobiiEyeTracker()
        {
            if (Tobii.Research.Unity.EyeTracker.Instance != null)
            {
                return;
            }

            var prefab = Resources.Load<GameObject>("[EyeTracker]");
            if (prefab != null)
            {
                Instantiate(prefab).name = "[EyeTracker]";
            }
        }

        private void CreateBoard()
        {
            CreateBackground();
            CreateReferenceImage();

            var slotPositions = new[]
            {
                new Vector3(-2.8f, 4.05f, 0f),
                new Vector3(0.2f, 4.05f, 0f),
                new Vector3(3.2f, 4.05f, 0f),
                new Vector3(-2.8f, 1.25f, 0f),
                new Vector3(0.2f, 1.25f, 0f),
                new Vector3(3.2f, 1.25f, 0f),
                new Vector3(-2.8f, -1.55f, 0f),
                new Vector3(0.2f, -1.55f, 0f),
                new Vector3(3.2f, -1.55f, 0f)
            };

            var piecePositions = new[]
            {
                new Vector3(-9.6f, -3.65f, 0f),
                new Vector3(-7.2f, -3.65f, 0f),
                new Vector3(-4.8f, -3.65f, 0f),
                new Vector3(-2.4f, -3.65f, 0f),
                new Vector3(0f, -3.65f, 0f),
                new Vector3(2.4f, -3.65f, 0f),
                new Vector3(4.8f, -3.65f, 0f),
                new Vector3(7.2f, -3.65f, 0f),
                new Vector3(9.6f, -3.65f, 0f)
            };

            var slotSprite = LoadArtSprite("empty_slot");
            var highlightSprite = LoadArtSprite("highlight_frame");

            for (var i = 0; i < slotPositions.Length; i++)
            {
                var slot = CreateSpriteObject("Slot " + (i + 1), slotPositions[i], new Vector2(2.62f, 2.62f), Color.white, slotSprite, 3);
                AttachHighlight(slot.transform, highlightSprite, 4, new Vector2(1.03f, 1.03f));
                slot.AddComponent<BoxCollider2D>();
                slot.AddComponent<PuzzleSlot>().Configure(i);
            }

            var pieceOrder = new[]
            {
                7, 2, 4, 0, 8, 3, 5, 1, 6
            };

            for (var i = 0; i < pieceOrder.Length; i++)
            {
                var pieceId = pieceOrder[i];
                var sprite = LoadArtSprite("puzzle_piece_" + (pieceId + 1));
                var piece = CreateSpriteObject("Puzzle Piece " + (pieceId + 1), piecePositions[i], new Vector2(2.24f, 2.24f), Color.white, sprite, 10);
                AttachDropShadow(piece.transform, 9);
                AttachHighlight(piece.transform, highlightSprite, 22, new Vector2(1.02f, 1.02f));
                piece.AddComponent<BoxCollider2D>();
                piece.AddComponent<PuzzlePiece>().Configure(pieceId, piecePositions[i]);
            }
        }

        private Text CreateStatusCanvas()
        {
            var canvasObject = new GameObject("GazeCraft UI");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            var panelObject = new GameObject("Status Panel");
            panelObject.transform.SetParent(canvasObject.transform, false);
            var panel = panelObject.AddComponent<Image>();
            panel.color = new Color(0.015f, 0.035f, 0.055f, 0.78f);

            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(18f, -18f);
            panelRect.sizeDelta = new Vector2(430f, 120f);

            var textObject = new GameObject("Status");
            textObject.transform.SetParent(panelObject.transform, false);
            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.alignment = TextAnchor.UpperLeft;
            text.color = new Color(0.86f, 0.98f, 1f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.offsetMin = new Vector2(14f, 10f);
            rect.offsetMax = new Vector2(-14f, -10f);
            return text;
        }

        private GameObject CreateCursor()
        {
            var cursor = CreateSpriteObject("Gaze Cursor", Vector3.zero, new Vector2(0.22f, 0.22f), Color.white, LoadArtSprite("gaze_cursor"), 30);
            cursor.GetComponent<SpriteRenderer>().sortingOrder = 30;
            return cursor;
        }

        private void CreateBackground()
        {
            var background = CreateSpriteObject("GazeCraft Neon Background", new Vector3(0f, 0f, 2f), new Vector2(14f, 8.2f), Color.white, LoadArtSprite("background_neon"), -20);
            background.transform.position = new Vector3(0f, 0f, 2f);
        }

        private void CreateReferenceImage()
        {
            var reference = CreateSpriteObject("Reference Image", new Vector3(-8.5f, 2.45f, 0f), new Vector2(1.18f, 1.18f), Color.white, LoadArtSprite("puzzle_complete"), 2);
            AttachDropShadow(reference.transform, 1);
            AttachFrame(reference.transform, LoadArtSprite("highlight_frame"), 3, new Vector2(1.01f, 1.01f));
        }

        private GameObject CreateSpriteObject(string name, Vector3 position, Vector2 scale, Color color, Sprite sprite, int sortingOrder)
        {
            var obj = new GameObject(name);
            obj.transform.position = position;
            obj.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite != null ? sprite : CreateWhiteSprite();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return obj;
        }

        private void AttachHighlight(Transform parent, Sprite highlightSprite, int sortingOrder, Vector2 scale)
        {
            var highlight = CreateSpriteObject("Highlight", Vector3.zero, scale, Color.white, highlightSprite, sortingOrder);
            highlight.transform.SetParent(parent, false);
            highlight.GetComponent<SpriteRenderer>().enabled = false;
        }

        private void AttachFrame(Transform parent, Sprite frameSprite, int sortingOrder, Vector2 scale)
        {
            var frame = CreateSpriteObject("Frame", Vector3.zero, scale, new Color(1f, 1f, 1f, 0.82f), frameSprite, sortingOrder);
            frame.transform.SetParent(parent, false);
        }

        private void AttachDropShadow(Transform parent, int sortingOrder)
        {
            var shadow = CreateSpriteObject("Soft Shadow", new Vector3(0.04f, -0.05f, 0.1f), new Vector2(0.98f, 0.98f), new Color(0f, 0f, 0f, 0.22f), LoadArtSprite("empty_slot"), sortingOrder);
            shadow.transform.SetParent(parent, false);
        }

        private Sprite LoadArtSprite(string spriteName)
        {
            var resourcePath = "GazeCraftArt/" + spriteName;
            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                return sprite;
            }

            if (RuntimeSprites.TryGetValue(resourcePath, out sprite))
            {
                return sprite;
            }

            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return fallbackSprite;
            }

            sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), ArtPixelsPerUnit);
            RuntimeSprites[resourcePath] = sprite;
            return sprite;
        }

        private static Sprite CreateWhiteSprite()
        {
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            var pixels = new Color[16 * 16];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 16f);
        }

        private static void AssignPrivateReference(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }
    }
}
