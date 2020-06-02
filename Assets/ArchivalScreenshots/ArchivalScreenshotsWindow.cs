
#if UNITY_EDITOR

namespace ArchivalScreenshots
{
    using System;
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    [InitializeOnLoad]
    class ArchivalScreenshotsWindow : EditorWindow
    {
        private static ArchivalScreenshotsSettings settings;
        private static bool screenshotTakenThisSession;
        private static float sessionStartTime;
        private static int captureFrame = -1;

        static ArchivalScreenshotsWindow()
        {
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
            EditorApplication.update += EditorApplicationUpdate;

            // Avoid screenshots after assembly reload
            screenshotTakenThisSession = true;
        }

        [MenuItem("Window/Archival screenshots")]
        private static void Init()
        {
            GetWindow<ArchivalScreenshotsWindow>("Archival screenshots").Show();
        }

        private static void PlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                screenshotTakenThisSession = false;
                sessionStartTime = Time.unscaledTime;
                captureFrame = -1;
            }
        }

        private static void EditorApplicationUpdate()
        {
            if (settings == null)
            {
                settings = Resources.Load<ArchivalScreenshotsSettings>("Archival Screenshots Settings");

                if (settings == null) return;
            }

            if (Application.isPlaying &&
                settings.enabled &&
                Time.unscaledTime > sessionStartTime + settings.delaySeconds &&
                !screenshotTakenThisSession)
            {
                captureFrame = 0;

                GameViewUtils.SetTempResolution(settings.resolution);

                var dir = EditorPrefs.GetString(GetDirectoryPrefsKey());

                string filename = Path.Combine(dir, $"Screen_{DateTime.UtcNow.ToString("yyyy-MM-dd_hh-mm-ss")}.png");
                ScreenCapture.CaptureScreenshot(filename, settings.supersampling);

                Debug.Log("Archive screenshot taken");

                screenshotTakenThisSession = true;
            }

            if (captureFrame == 1)
            {
                GameViewUtils.RestoreResolution();
            }

            if (captureFrame >= 0)
                captureFrame++;
        }

        private void OnGUI()
        {
            GUILayout.Label("Archival screenshots");

            if (settings == null)
            {
                settings = Resources.Load<ArchivalScreenshotsSettings>("Archival Screenshots Settings");

                if (settings == null) return;
            }

            Undo.RecordObject(settings, "Archival Screenshots Settings");

            settings.enabled = EditorGUILayout.Toggle("Enabled", settings.enabled);
            settings.delaySeconds = EditorGUILayout.FloatField("Delay (seconds)", settings.delaySeconds);
            settings.resolution = EditorGUILayout.Vector2IntField("Resolution", settings.resolution);
            settings.supersampling = EditorGUILayout.IntSlider("Supersampling", settings.supersampling, 1, 4);

            // A bit hacky, but I'm sure it'll be fine
            var defaultPath = Application.dataPath.Substring(0, Application.dataPath.Length - "/Assets".Length);
            var prefsKey = GetDirectoryPrefsKey();

            var dir = EditorPrefs.GetString(prefsKey, defaultPath);
            dir = EditorGUILayout.TextField("Output directory", dir);

            if (GUILayout.Button("Select"))
            {
                var result = EditorUtility.OpenFolderPanel("Archival screenshots directory", dir, "");

                if (!string.IsNullOrEmpty(result))
                    dir = result;
            }
            EditorPrefs.SetString(prefsKey, dir);

            if (GUILayout.Button("Open"))
            {
                if (Directory.Exists(dir))
                {
                    System.Diagnostics.Process.Start(dir);
                }
            }

            EditorUtility.SetDirty(settings);
        }

        private static string GetDirectoryPrefsKey()
        {
            // TODO Fix: Directory will change if project name changes
            return $"ArchivalScreenshotsOf_{Application.productName}";
        }

        private static void FindSettingsAsset()
        {
            if (settings == null)
            {
                settings = Resources.Load<ArchivalScreenshotsSettings>("Archival Screenshots Settings");
            }
        }
    }
}

#endif