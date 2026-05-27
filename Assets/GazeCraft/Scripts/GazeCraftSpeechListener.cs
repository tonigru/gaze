using System;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using UnityEngine.Windows.Speech;
#endif

namespace GazeCraft
{
    public sealed class GazeCraftSpeechListener : MonoBehaviour
    {
        public event Action TakeRequested;
        public event Action PutRequested;

        [SerializeField] private string takePhrase = "take that";
        [SerializeField] private string putPhrase = "put that";
        [SerializeField] private bool keyboardFallback = true;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private PhraseRecognizer recognizer;
#endif
        public string Status { get; private set; } = "speech not started";

        private void OnEnable()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                recognizer = new KeywordRecognizer(new[] { takePhrase, putPhrase, "take", "put", "uzmi to", "stavi to", "uzmi", "stavi" }, ConfidenceLevel.Low);
                recognizer.OnPhraseRecognized += OnPhraseRecognized;
                recognizer.Start();
                Status = recognizer.IsRunning ? "speech running" : "speech not running";
                Debug.Log("GazeCraft speech recognizer: " + Status);
            }
            catch (Exception exception)
            {
                Status = "speech failed: " + exception.Message;
                Debug.LogWarning("GazeCraft speech recognizer failed: " + exception.Message);
            }
#endif
        }

        private void Update()
        {
            if (!keyboardFallback)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.tKey.wasPressedThisFrame)
            {
                TakeRequested?.Invoke();
            }

            if (keyboard.pKey.wasPressedThisFrame)
            {
                PutRequested?.Invoke();
            }
        }

        private void OnDisable()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (recognizer != null)
            {
                recognizer.OnPhraseRecognized -= OnPhraseRecognized;
                if (recognizer.IsRunning)
                {
                    recognizer.Stop();
                }

                recognizer.Dispose();
                recognizer = null;
            }
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
        {
            var phrase = args.text.Trim().ToLowerInvariant();
            if (phrase == takePhrase || phrase == "take" || phrase == "uzmi to" || phrase == "uzmi")
            {
                Status = "heard: " + phrase;
                TakeRequested?.Invoke();
            }
            else if (phrase == putPhrase || phrase == "put" || phrase == "stavi to" || phrase == "stavi")
            {
                Status = "heard: " + phrase;
                PutRequested?.Invoke();
            }
        }
#endif
    }
}
