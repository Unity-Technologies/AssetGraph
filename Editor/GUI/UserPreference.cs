using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	public class UserPreference : MonoBehaviour {

        static readonly string kKEY_USERPREF_GRID = "UnityEngine.AssetGraph.UserPref.GridSize";
        static readonly string kKEY_USERPREF_DEFAULTVERBOSELOG = "UnityEngine.AssetGraph.UserPref.DefaultVerboseLog";
        static readonly string kKEY_USERPREF_DEFAULTASSETLOG = "UnityEngine.AssetGraph.UserPref.DefaultAssetLog";

		private static bool s_prefsLoaded = false;

        private static float s_editorWindowGridSize;
        private static bool s_defaultVerboseLog;
        private static bool s_clearAssetLogOnBuild;

		public static float EditorWindowGridSize {
			get {
				LoadAllPreferenceValues();
				return s_editorWindowGridSize;
			}
			set {
				s_editorWindowGridSize = value;
				SaveAllPreferenceValues();
			}
		}

        public static bool DefaultVerboseLog {
            get {
                LoadAllPreferenceValues ();
                return s_defaultVerboseLog;
            }
            set {
                s_defaultVerboseLog = value;
                SaveAllPreferenceValues ();
            }
        }

        public static bool ClearAssetLogOnBuild {
            get {
                LoadAllPreferenceValues ();
                return s_clearAssetLogOnBuild;
            }
            set {
                s_clearAssetLogOnBuild = value;
                SaveAllPreferenceValues ();
            }
        }


		private static void LoadAllPreferenceValues() {
			if (!s_prefsLoaded)
			{
                s_editorWindowGridSize = EditorPrefs.GetFloat(kKEY_USERPREF_GRID, 12f);
                s_defaultVerboseLog = EditorPrefs.GetBool(kKEY_USERPREF_DEFAULTVERBOSELOG, false);
                s_clearAssetLogOnBuild = EditorPrefs.GetBool(kKEY_USERPREF_DEFAULTASSETLOG, false);

				s_prefsLoaded = true;
			}
		}

		private static void SaveAllPreferenceValues() {
            EditorPrefs.SetFloat(kKEY_USERPREF_GRID, s_editorWindowGridSize);
            EditorPrefs.SetBool(kKEY_USERPREF_DEFAULTVERBOSELOG, s_defaultVerboseLog);
            EditorPrefs.SetBool(kKEY_USERPREF_DEFAULTASSETLOG, s_clearAssetLogOnBuild);
		}

		[PreferenceItem("AssetGraph")]
		public static void PreferencesGUI() {
			LoadAllPreferenceValues();

			s_editorWindowGridSize = EditorGUILayout.FloatField("Graph editor grid size", s_editorWindowGridSize);
            s_defaultVerboseLog = EditorGUILayout.ToggleLeft ("Default show verbose log", s_defaultVerboseLog);

			if (GUI.changed) {
				SaveAllPreferenceValues();
			}
		}
	}
}