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
	public class ProjectSettingsWindow : EditorWindow {

        [SerializeField] private string m_cacheDir;

        [MenuItem(Model.Settings.GUI_TEXT_MENU_PROJECTWINDOW_OPEN, false, 41)]
		public static void Open () {
            GetWindow<ProjectSettingsWindow>();
		}

        private void Init() {
			LogUtility.Logger.filterLogType = LogType.Warning;
			this.titleContent = new GUIContent("Project Settings");
			this.minSize = new Vector2(300f, 100f);
			this.maxSize = new Vector2(1000f, 400f);
		}

        public void OnEnable () {
			Init();
		}

		public void OnFocus() {
		}

		public void OnDisable() {
		}

		public void OnGUI () {

			using (new EditorGUILayout.VerticalScope()) {

				GUILayout.Label("Project Settings", new GUIStyle("BoldLabel"));
                GUILayout.Space(8f);

                string cacheDir = Model.Settings.UserSettings.AssetBundleBuildCacheDir;

                var newCacheDir = EditorGUILayout.TextField ("AB Cache Dir", cacheDir);
                if (newCacheDir != cacheDir) {
                    Model.Settings.UserSettings.AssetBundleBuildCacheDir = newCacheDir;
                }
			}
		}
	}
}
