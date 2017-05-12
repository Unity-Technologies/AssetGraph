using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {

	public class UserPreference : MonoBehaviour {

		static readonly string kKEY_USERPREF_GRID = "UnityEngine.AssetBundles.GraphTool.UserPref.GridSize";

		private static bool s_prefsLoaded = false;

		private static float s_editorWindowGridSize;

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

		private static void LoadAllPreferenceValues() {
			if (!s_prefsLoaded)
			{
				s_editorWindowGridSize = EditorPrefs.GetFloat(kKEY_USERPREF_GRID, 12f);

				s_prefsLoaded = true;
			}
		}

		private static void SaveAllPreferenceValues() {
			EditorPrefs.SetFloat(kKEY_USERPREF_GRID, s_editorWindowGridSize);
		}

		[PreferenceItem("AB GraphTool")]
		public static void PreferencesGUI() {
			LoadAllPreferenceValues();

			s_editorWindowGridSize = EditorGUILayout.FloatField("Graph Editor Grid Size", s_editorWindowGridSize);

			if (GUI.changed) {
				SaveAllPreferenceValues();
			}
		}
	}
}