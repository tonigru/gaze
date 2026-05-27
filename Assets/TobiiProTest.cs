using UnityEngine;
using Tobii.Research;

public class TobiiProTest : MonoBehaviour
{
    private IEyeTracker eyeTracker;

    void Start()
    {
        var trackers = EyeTrackingOperations.FindAllEyeTrackers();

        if (trackers.Count == 0)
        {
            Debug.LogError("Nema pronađenog Tobii eye trackera.");
            return;
        }

        eyeTracker = trackers[0];
        Debug.Log("Pronađen Tobii: " + eyeTracker.SerialNumber);

        eyeTracker.GazeDataReceived += OnGazeDataReceived;
    }

    private void OnGazeDataReceived(object sender, GazeDataEventArgs e)
    {
        Debug.Log("Gaze data dolazi!");
    }

    void OnDestroy()
    {
        if (eyeTracker != null)
        {
            eyeTracker.GazeDataReceived -= OnGazeDataReceived;
        }
    }
}