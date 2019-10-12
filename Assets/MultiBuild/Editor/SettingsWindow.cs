using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/* Easiest way to check if we're running 5.4 or lower. */
#if UNITY_5_5_OR_NEWER
#else
namespace UnityEditor
{
	public struct BuildPlayerOptions
	{
		public string[] scenes { get; set; }
		public string locationPathName { get; set; }
		public string assetBundleManifestPath { get; set; }
		public BuildTargetGroup targetGroup { get; set; }
		public BuildTarget target { get; set; }
		public BuildOptions options { get; set; }
	}
}
#endif

namespace MultiBuild {
    public class SettingsWindow : EditorWindow {

        Settings _settings;
        Settings Settings {
            get {
                if (_settings == null) {
                    _settings = Storage.LoadOrCreateSettings();
                }
                return _settings;
            }
        }
        SerializedObject _serializedSettings;
        SerializedObject SerializedSettings {
            get {
                if (_serializedSettings == null) {
                    _serializedSettings = new SerializedObject(Settings);
                }
                return _serializedSettings;
            }
        }

        GUIStyle _actionButtonStyle;
        GUIStyle ActionButtonStyle {
            get {
                if (_actionButtonStyle == null) {
                    _actionButtonStyle = new GUIStyle(GUI.skin.button);
                    _actionButtonStyle.fontStyle = FontStyle.Bold;
                    _actionButtonStyle.normal.textColor = Color.white;
                }
                return _actionButtonStyle;
            }
        }
        GUIStyle _labelMarginStyle;
        GUIStyle LabelMarginStyle {
            get {
                if (_labelMarginStyle == null) {
                    _labelMarginStyle = new GUIStyle();
                    _labelMarginStyle.margin.left = GUI.skin.label.margin.left;
                }
                return _labelMarginStyle;
            }
        }
        GUIStyle _removeButtonContainerStyle;
        GUIStyle RemoveButtonContainerStyle {
            get {
                if (_removeButtonContainerStyle == null) {
                    _removeButtonContainerStyle = new GUIStyle();
                    _removeButtonContainerStyle.margin.left = 30;
                }
                return _removeButtonContainerStyle;
            }
        }
        List<string> _targetNamesNotAdded;
        bool _targetsDirty = true;
        int _targetToAddIndex;

        [MenuItem ("Tools/MultiBuild...")]
        public static void  ShowWindow () {
            EditorWindow.GetWindow(typeof(SettingsWindow), false, "MultiBuild");
        }

        void OnGUI () {

            // Use SerializedObject as a proxy to get dirty flags, undo & asset save
            GUILayout.Label ("Output Settings", EditorStyles.boldLabel);

            // Need to use nested verticals and flexible space to align text & button vertically
            EditorGUILayout.BeginHorizontal(GUILayout.MaxHeight(GUI.skin.button.CalcHeight(new GUIContent("..."), 30)));
            // Also need a style on label to make it line up on the left edge
            EditorGUILayout.BeginVertical(LabelMarginStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.PropertyField(SerializedSettings.FindProperty("outputFolder"));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("...", GUILayout.ExpandWidth(false))) {
                var prop = SerializedSettings.FindProperty("outputFolder");
                var fld = EditorUtility.OpenFolderPanel("Pick build folder", prop.stringValue, PlayerSettings.productName);
                if (!string.IsNullOrEmpty(fld)) {
                    prop.stringValue = fld;
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(SerializedSettings.FindProperty("useProductName"));
            if (!Settings.useProductName) {
                EditorGUILayout.PropertyField(SerializedSettings.FindProperty("overrideName"));
            }
            EditorGUILayout.Space();


            GUILayout.Label ("Platforms To Build", EditorStyles.boldLabel);

            bool removeTargetAtEnd = false;
            BuildTarget targetToRemove = BuildTarget.iOS;
            foreach (var target in Settings.targets) {
                EditorGUILayout.BeginHorizontal(RemoveButtonContainerStyle, GUILayout.MaxHeight(23));
                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(false), GUILayout.MinWidth(30));
                GUILayout.FlexibleSpace();
                GUI.backgroundColor = new Color(0.6f,0, 0,1);
                if (GUILayout.Button(" X ", ActionButtonStyle)) {
                    // Don't do this now, breaks iteration
                    targetToRemove = target;
                    removeTargetAtEnd = true;
                }
                GUI.backgroundColor = Color.white;
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(target.ToString());
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
            if (Settings.targets.Count == 0) {
                GUILayout.Label("No platforms selected! Add one below.", EditorStyles.centeredGreyMiniLabel);
            }
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal(GUILayout.MaxHeight(GUI.skin.button.CalcHeight(new GUIContent("..."), 30)));
            EditorGUILayout.BeginVertical(LabelMarginStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Add platform");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            BuildTarget[] targets = (BuildTarget[]) Enum.GetValues(typeof(BuildTarget));
            string[] values = new string[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                values[i] = targets[i].ToString();
            }
            _targetToAddIndex = EditorGUILayout.Popup(_targetToAddIndex, values);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add")) {
                var proplist = SerializedSettings.FindProperty("targets");
                proplist.arraySize++;
                SerializedProperty targetEnum = proplist.GetArrayElementAtIndex(proplist.arraySize - 1);
                int index = 0;
                foreach (string name in targetEnum.enumNames)
                {
                    if (name.ToLower().Equals(targets[_targetToAddIndex].ToString().ToLower()))
                    {
                        break;
                    }
                    index++;
                }
                proplist.GetArrayElementAtIndex(proplist.arraySize - 1).enumValueIndex = index;
                _targetsDirty = true;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            GUILayout.Label ("Additional options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(SerializedSettings.FindProperty("developmentBuild"));
            EditorGUILayout.PropertyField(SerializedSettings.FindProperty("compress"));

            if (removeTargetAtEnd) {
                int index = Settings.targets.IndexOf(targetToRemove);
                var proplist = SerializedSettings.FindProperty("targets");
                proplist.DeleteArrayElementAtIndex(index);
                _targetsDirty = true;
            }

            // This applies any changes to the underlying asset and marks dirty if needed
            // this is what ensures the asset gets saved
            SerializedSettings.ApplyModifiedProperties();
            if (_targetsDirty) {
                Repaint();
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            GUI.backgroundColor = new Color(0,0.6f,0,1);
            if (GUILayout.Button("Build Selected Platforms", ActionButtonStyle, GUILayout.MinHeight(30))) {
                // do eet
                Build();
            }
        }

        void Build() {

            var savedTarget = EditorUserBuildSettings.activeBuildTarget;

            bool ok = true;
            try {
                ok = Builder.Build(Settings, (opts, progress, done) => {
                    string message = done ?
                        string.Format("Building {0} Done", opts.target.ToString()) :
                        string.Format("Building {0}...", opts.target.ToString());
                    if (EditorUtility.DisplayCancelableProgressBar(
                        "Building project...",
                        message,
                        progress)) {
                            return false; // cancel
                    }
                    return true;
                });
            } catch (Exception e) {
                EditorUtility.DisplayDialog("Build error", e.Message, "Close");
                ok = false;
            }

            EditorUtility.ClearProgressBar();
            if (!ok) {
                EditorUtility.DisplayDialog("Cancelled", "Build cancelled before finishing.", "Close");
            }

            // Building can change the active target, can cause warnings or odd behaviour
            // Put it back to how it was
            if (EditorUserBuildSettings.activeBuildTarget != savedTarget) {
#if UNITY_5_6_OR_NEWER
                EditorUserBuildSettings.SwitchActiveBuildTargetAsync(Builder.GroupForTarget(savedTarget), savedTarget);
#else
                EditorUserBuildSettings.SwitchActiveBuildTarget(savedTarget);
#endif
            }
        }

    }

}
