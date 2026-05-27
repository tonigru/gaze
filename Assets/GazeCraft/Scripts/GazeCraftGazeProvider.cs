using System;
using System.Collections.Generic;
using Tobii.Research;
using Tobii.Research.Unity;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GazeCraft
{
    public sealed class GazeCraftGazeProvider : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private bool useMouseFallback = true;
        [SerializeField] private bool preferRawTobiiSdk = true;

        private readonly object gazeLock = new();
        private Tobii.Research.IEyeTracker rawEyeTracker;
        private Tobii.StreamEngine.IEyeTracker streamEyeTracker;
        private Tobii.StreamEngine.tobii_gaze_point_callback_t streamGazePointCallback;
        private Tobii.StreamEngine.Interop.tobii_log_func_t streamLogCallback;
        private Vector2 latestRawDisplayPoint;
        private bool latestRawDisplayPointValid;
        private int rawEventCount;
        private Vector2 latestStreamDisplayPoint;
        private bool latestStreamDisplayPointValid;
        private int streamEventCount;

        public string LastSource { get; private set; } = "none";
        public string LastTobiiStatus { get; private set; } = "not started";
        public Vector2 LastDisplayPoint { get; private set; }
        public int TobiiEventCount => streamEventCount + rawEventCount;

        private void Start()
        {
            ConnectStreamEngine();
            ConnectRawTobiiSdk();
        }

        private void Update()
        {
            streamEyeTracker?.ProcessCallbacks();
        }

        private void OnDestroy()
        {
            if (streamEyeTracker != null)
            {
                if (streamGazePointCallback != null)
                {
                    streamEyeTracker.GazePoint -= streamGazePointCallback;
                }

                streamEyeTracker.Dispose();
                streamEyeTracker = null;
            }

            if (rawEyeTracker != null)
            {
                rawEyeTracker.GazeDataReceived -= OnRawGazeDataReceived;
                rawEyeTracker = null;
            }
        }

        public bool TryGetWorldPoint(out Vector3 worldPoint)
        {
            var cameraToUse = targetCamera != null ? targetCamera : Camera.main;
            if (cameraToUse == null)
            {
                worldPoint = Vector3.zero;
                return false;
            }

            if (TryGetStreamEngineWorldPoint(cameraToUse, out worldPoint))
            {
                LastSource = "Tobii stream";
                return true;
            }

            if (preferRawTobiiSdk && TryGetRawTobiiWorldPoint(cameraToUse, out worldPoint))
            {
                LastSource = "Tobii raw";
                return true;
            }

            var eyeTracker = EyeTracker.Instance;
            if (eyeTracker != null)
            {
                var gazeData = eyeTracker.LatestGazeData;
                if (gazeData != null && gazeData.CombinedGazeRayScreenValid)
                {
                    worldPoint = RayToWorldPoint(gazeData.CombinedGazeRayScreen);
                    LastSource = "Tobii prefab";
                    return true;
                }
            }

            if (!preferRawTobiiSdk && TryGetRawTobiiWorldPoint(cameraToUse, out worldPoint))
            {
                LastSource = "Tobii raw";
                return true;
            }

            if (useMouseFallback)
            {
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    var mousePosition = mouse.position.ReadValue();
                    worldPoint = cameraToUse.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, -cameraToUse.transform.position.z));
                    worldPoint.z = 0f;
                    LastSource = "mouse fallback";
                    return true;
                }
            }

            LastSource = rawEyeTracker == null ? "no Tobii tracker" : "Tobii waiting";
            worldPoint = Vector3.zero;
            return false;
        }

        private void ConnectStreamEngine()
        {
            try
            {
                streamLogCallback = OnStreamEngineLog;
                Tobii.StreamEngine.EyeTrackerFactory.Init(new Tobii.StreamEngine.tobii_custom_log_t
                {
                    log_context = IntPtr.Zero,
                    log_func = streamLogCallback
                });

                var urls = Tobii.StreamEngine.EyeTrackerFactory.ListEyeTrackers();
                if (urls == null || urls.Count == 0)
                {
                    LastTobiiStatus = "Stream Engine: no trackers";
                    Debug.LogWarning("GazeCraft: Stream Engine found no eye trackers.");
                    return;
                }

                streamEyeTracker = Tobii.StreamEngine.EyeTrackerFactory.Create(urls[0], null, new List<Tobii.StreamEngine.tobii_license_validation_result_t>());
                streamGazePointCallback = OnStreamGazePoint;
                streamEyeTracker.GazePoint += streamGazePointCallback;
                LastTobiiStatus = "Stream Engine connected";
                Debug.Log("GazeCraft Stream Engine connected: " + urls[0]);
            }
            catch (Exception exception)
            {
                LastTobiiStatus = "Stream Engine failed: " + exception.Message;
                Debug.LogWarning("GazeCraft Stream Engine connect failed: " + exception.Message);
            }
        }

        private void OnStreamEngineLog(IntPtr logContext, Tobii.StreamEngine.tobii_log_level_t level, string text)
        {
            if (level >= Tobii.StreamEngine.tobii_log_level_t.TOBII_LOG_LEVEL_ERROR)
            {
                Debug.LogWarning("Tobii Stream Engine: " + text);
            }
        }

        private void OnStreamGazePoint(ref Tobii.StreamEngine.tobii_gaze_point_t gazePoint, IntPtr userData)
        {
            lock (gazeLock)
            {
                streamEventCount++;
                if (gazePoint.validity == Tobii.StreamEngine.tobii_validity_t.TOBII_VALIDITY_VALID)
                {
                    latestStreamDisplayPoint = new Vector2(gazePoint.position.x, gazePoint.position.y);
                    latestStreamDisplayPointValid = true;
                }
            }
        }

        private void ConnectRawTobiiSdk()
        {
            try
            {
                var trackers = EyeTrackingOperations.FindAllEyeTrackers();
                if (trackers.Count == 0)
                {
                    LastTobiiStatus = LastTobiiStatus + " | Pro SDK: no trackers";
                    Debug.LogWarning("GazeCraft: no raw Tobii eye tracker found.");
                    return;
                }

                rawEyeTracker = trackers[0];
                rawEyeTracker.GazeDataReceived += OnRawGazeDataReceived;
                LastTobiiStatus = LastTobiiStatus + " | Pro SDK connected";
                Debug.Log("GazeCraft raw Tobii connected: " + rawEyeTracker.SerialNumber);
            }
            catch (Exception exception)
            {
                LastTobiiStatus = LastTobiiStatus + " | Pro SDK failed: " + exception.Message;
                Debug.LogWarning("GazeCraft raw Tobii connect failed: " + exception.Message);
            }
        }

        private void OnRawGazeDataReceived(object sender, GazeDataEventArgs eventArgs)
        {
            var sum = Vector2.zero;
            var count = 0;

            if (eventArgs.LeftEye.GazePoint.Validity == Validity.Valid)
            {
                sum += eventArgs.LeftEye.GazePoint.PositionOnDisplayArea.ToVector2();
                count++;
            }

            if (eventArgs.RightEye.GazePoint.Validity == Validity.Valid)
            {
                sum += eventArgs.RightEye.GazePoint.PositionOnDisplayArea.ToVector2();
                count++;
            }

            lock (gazeLock)
            {
                rawEventCount++;
                if (count > 0)
                {
                    latestRawDisplayPoint = sum / count;
                    latestRawDisplayPointValid = true;
                }
            }
        }

        private bool TryGetStreamEngineWorldPoint(Camera cameraToUse, out Vector3 worldPoint)
        {
            Vector2 displayPoint;
            lock (gazeLock)
            {
                if (!latestStreamDisplayPointValid)
                {
                    worldPoint = Vector3.zero;
                    return false;
                }

                displayPoint = latestStreamDisplayPoint;
            }

            LastDisplayPoint = displayPoint;
            var screenPoint = new Vector3(Screen.width * displayPoint.x, Screen.height * (1f - displayPoint.y), -cameraToUse.transform.position.z);
            worldPoint = cameraToUse.ScreenToWorldPoint(screenPoint);
            worldPoint.z = 0f;
            return true;
        }

        private bool TryGetRawTobiiWorldPoint(Camera cameraToUse, out Vector3 worldPoint)
        {
            Vector2 displayPoint;
            lock (gazeLock)
            {
                if (!latestRawDisplayPointValid)
                {
                    worldPoint = Vector3.zero;
                    return false;
                }

                displayPoint = latestRawDisplayPoint;
            }

            LastDisplayPoint = displayPoint;
            var screenPoint = new Vector3(Screen.width * displayPoint.x, Screen.height * (1f - displayPoint.y), -cameraToUse.transform.position.z);
            worldPoint = cameraToUse.ScreenToWorldPoint(screenPoint);
            worldPoint.z = 0f;
            return true;
        }

        private static Vector3 RayToWorldPoint(Ray ray)
        {
            if (Mathf.Abs(ray.direction.z) < 0.0001f)
            {
                var origin = ray.origin;
                origin.z = 0f;
                return origin;
            }

            var distanceToZZero = -ray.origin.z / ray.direction.z;
            var point = ray.GetPoint(distanceToZZero);
            point.z = 0f;
            return point;
        }
    }
}
