using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace Unity.AI.Assistant.PlayModeTest
{
    [InitializeOnLoad]
    internal static class PlayModeTestRunner
    {
        private const string StateKey = "PlayModeTest.State";
        private const string ResultKey = "PlayModeTest.Result";
        private const string ScriptPathKey = "PlayModeTest.ScriptPath";
        private const string SentinelLog = "PLAY_MODE_TEST_COMPLETE";

        private static readonly int WaitFrames = SessionState.GetInt("PlayModeTest.WaitFrames", 5);
        private static readonly float TestTimeout = SessionState.GetFloat("PlayModeTest.TestTimeout", 8.0f);

        private static List<string> _capturedLogs = new List<string>();
        private const int MaxCapturedLogs = 100;

        static PlayModeTestRunner()
        {
            string state = SessionState.GetString(StateKey, "Idle");

            switch (state)
            {
                case "Idle":
                    break;

                case "WaitingForCompile":
                    Debug.Log("[PlayModeTest] Bootstrap compiled. Scheduling Play Mode entry.");
                    EditorApplication.delayCall += () =>
                    {
                        SessionState.SetString(StateKey, "EnteringPlayMode");
                        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                        EditorApplication.isPlaying = true;
                    };
                    break;

                case "EnteringPlayMode":
                    EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                    if (EditorApplication.isPlaying)
                    {
                        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                        SessionState.SetString(StateKey, "InPlayMode");
                        EditorApplication.update += WaitFramesThenRun;
                    }
                    break;

                case "InPlayMode":
                    if (EditorApplication.isPlaying)
                    {
                        EditorApplication.update += WaitFramesThenRun;
                    }
                    break;

                case "Done":
                    Debug.Log(SentinelLog);
                    EditorApplication.delayCall += SelfDestruct;
                    break;
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                SessionState.SetString(StateKey, "InPlayMode");
                EditorApplication.update += WaitFramesThenRun;
            }
        }

        private static int _frameCount = 0;
        private static bool _setupDone = false;
        private static bool _testDone = false;
        private static double _testStartTime = 0;

        private static void WaitFramesThenRun()
        {
            _frameCount++;
            if (_frameCount < WaitFrames) return;

            if (_testDone) return;

            if (!_setupDone)
            {
                _setupDone = true;
                Application.logMessageReceived += OnLogMessage;
                _testStartTime = EditorApplication.timeSinceStartup;
                try
                {
                    Setup();
                }
                catch (System.Exception e)
                {
                    Debug.LogError("[PlayModeTest] Setup threw exception: " + e);
                    FinishTest(true, e.Message);
                    return;
                }
                return;
            }

            float elapsed = (float)(EditorApplication.timeSinceStartup - _testStartTime);
            bool timedOut = elapsed >= TestTimeout;

            try
            {
                bool complete = Tick(elapsed);
                if (complete || timedOut)
                {
                    FinishTest(timedOut && !complete, timedOut ? "Test timed out after " + TestTimeout + "s" : null);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[PlayModeTest] Tick threw exception: " + e);
                FinishTest(true, e.Message);
            }
        }

        private static void FinishTest(bool isError, string errorMessage)
        {
            _testDone = true;
            EditorApplication.update -= WaitFramesThenRun;
            Application.logMessageReceived -= OnLogMessage;

            string resultJson;
            try
            {
                resultJson = GetResult();
            }
            catch (System.Exception e)
            {
                resultJson = JsonUtility.ToJson(new TestResult
                {
                    success = false,
                    error = "GetResult() threw: " + e.Message,
                    logs = _capturedLogs.ToArray()
                });
            }

            if (isError && errorMessage != null)
            {
                resultJson = JsonUtility.ToJson(new TestResult
                {
                    success = false,
                    error = errorMessage,
                    logs = _capturedLogs.ToArray()
                });
            }

            SessionState.SetString(ResultKey, resultJson);
            SessionState.SetString(StateKey, "Done");
            EditorApplication.isPlaying = false;
        }

        private static void OnLogMessage(string message, string stackTrace, LogType type)
        {
            if (_capturedLogs.Count >= MaxCapturedLogs) return;
            _capturedLogs.Add("[" + type + "] " + message);
        }

        private static void SelfDestruct()
        {
            string scriptPath = SessionState.GetString(ScriptPathKey, "");
            if (!string.IsNullOrEmpty(scriptPath) && AssetDatabase.AssetPathExists(scriptPath))
            {
                AssetDatabase.DeleteAsset(scriptPath);
            }
            SessionState.EraseString(StateKey);
            SessionState.EraseString(ScriptPathKey);
        }

        [System.Serializable]
        private class TestResult
        {
            public bool success;
            public string error;
            public string[] logs;
        }

        // ============================================================
        // TEST LOGIC
        // ============================================================
        private static GameObject _player;
        private static HubManager _hubManager;

        private static void Setup()
        {
            Debug.Log("[Test] Setup started");
            SceneManager.LoadScene(1); // Load 0.unity
        }

        private static bool Tick(float elapsed)
        {
            if (_player == null)
            {
                _player = GameObject.Find("Martial Hero");
                if (_player != null)
                {
                    Debug.Log("[Test] Found Martial Hero!");
                    _player.transform.position = new Vector3(0.0f, 4.6f, 0.0f);
                }
                else
                {
                    return false;
                }
            }

            if (_hubManager == null)
            {
                _hubManager = Object.FindAnyObjectByType<HubManager>();
                if (_hubManager != null)
                {
                    Debug.Log("[Test] Found HubManager!");
                }
            }

            if (Mathf.FloorToInt(elapsed * 2) % 2 == 0)
            {
                string sceneName = SceneManager.GetActiveScene().name;
                float dist = _player != null && _hubManager != null ? Vector2.Distance(_player.transform.position, _hubManager.dojoDoorPosition) : -1f;
                Debug.Log("[Test] Current scene: " + sceneName + ", Player Pos: " + _player.transform.position + ", Dist to door: " + dist);
            }

            if (SceneManager.GetActiveScene().buildIndex == 2)
            {
                Debug.Log("[Test] Successfully transitioned to TrainingRoom scene!");
                return true;
            }

            return false;
        }

        private static string GetResult()
        {
            var result = new TestResult
            {
                success = SceneManager.GetActiveScene().buildIndex == 2,
                logs = _capturedLogs.ToArray()
            };
            return JsonUtility.ToJson(result);
        }
    }
}