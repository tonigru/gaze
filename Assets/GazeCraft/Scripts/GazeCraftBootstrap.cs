using UnityEngine;
using UnityEngine.UI;

namespace GazeCraft
{
    public sealed class GazeCraftBootstrap : MonoBehaviour
    {
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private Sprite puzzleSprite;

        private void Start()
        {
            if (buildOnStart && FindAnyObjectByType<GazeCraftGameManager>() == null)
            {
                BuildRuntimeScene();
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
            manager.RefreshSceneReferences();
        }

        private Camera EnsureCamera()
        {
            var existing = Camera.main;
            if (existing != null)
            {
                existing.orthographic = true;
                existing.orthographicSize = 5f;
                existing.transform.position = new Vector3(0f, 0f, -10f);
                return existing;
            }

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.1f, 0.12f);
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
            var slotPositions = new[]
            {
                new Vector3(-2.2f, 1.35f, 0f),
                new Vector3(0f, 1.35f, 0f),
                new Vector3(2.2f, 1.35f, 0f)
            };

            var piecePositions = new[]
            {
                new Vector3(2.2f, -2.25f, 0f),
                new Vector3(-2.2f, -2.25f, 0f),
                new Vector3(0f, -2.25f, 0f)
            };

            for (var i = 0; i < slotPositions.Length; i++)
            {
                var slot = CreateSquare("Slot " + (i + 1), slotPositions[i], new Vector2(1.45f, 1.45f), new Color(1f, 1f, 1f, 0.22f));
                slot.AddComponent<BoxCollider2D>();
                slot.AddComponent<PuzzleSlot>().Configure(i);
            }

            var colors = new[]
            {
                new Color(0.96f, 0.28f, 0.26f),
                new Color(0.17f, 0.66f, 0.95f),
                new Color(0.95f, 0.78f, 0.2f)
            };

            for (var i = 0; i < piecePositions.Length; i++)
            {
                var piece = CreateSquare("Puzzle Piece " + (i + 1), piecePositions[i], new Vector2(1.25f, 1.25f), colors[i]);
                piece.GetComponent<SpriteRenderer>().sortingOrder = 10;
                piece.AddComponent<BoxCollider2D>();
                piece.AddComponent<PuzzlePiece>().Configure(i, piecePositions[i]);
            }
        }

        private Text CreateStatusCanvas()
        {
            var canvasObject = new GameObject("GazeCraft UI");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            var textObject = new GameObject("Status");
            textObject.transform.SetParent(canvasObject.transform, false);
            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.alignment = TextAnchor.UpperCenter;
            text.color = Color.white;

            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -18f);
            rect.sizeDelta = new Vector2(0f, 80f);
            return text;
        }

        private GameObject CreateCursor()
        {
            var cursor = CreateSquare("Gaze Cursor", Vector3.zero, new Vector2(0.18f, 0.18f), new Color(0.4f, 1f, 0.9f, 0.9f));
            cursor.GetComponent<SpriteRenderer>().sortingOrder = 30;
            return cursor;
        }

        private GameObject CreateSquare(string name, Vector3 position, Vector2 scale, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.position = position;
            obj.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = puzzleSprite != null ? puzzleSprite : CreateWhiteSprite();
            renderer.color = color;
            return obj;
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
