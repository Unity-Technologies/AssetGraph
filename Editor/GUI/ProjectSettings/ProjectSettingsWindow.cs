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
	public class ProjectSettingsWindow : EditorWindow {

        private AssetBundlesSettingsTab     m_abTab;
        private ExecutionOrderSettingsTab   m_execTab;
        private Mode m_mode;

        enum Mode : int {
            AssetBundleSettings,
            ExecutionOrderSettings
        }

        [MenuItem(Model.Settings.GUI_TEXT_MENU_PROJECTWINDOW_OPEN, false, 41)]
		public static void Open () {
            GetWindow<ProjectSettingsWindow>();
		}

        private void Init() {
			this.titleContent = new GUIContent("Project Settings");
			this.minSize = new Vector2(300f, 100f);
            m_abTab = new AssetBundlesSettingsTab ();
            m_execTab = new ExecutionOrderSettingsTab ();
            m_mode = Mode.AssetBundleSettings;
		}

        public void OnEnable () {
			Init();
		}

		public void OnFocus() {
            m_abTab.Refresh ();
            m_execTab.Refresh ();
		}

		public void OnDisable() {
		}

		public void OnGUI () {

            DrawToolBar ();

            switch (m_mode) {
            case Mode.AssetBundleSettings:
                m_abTab.OnGUI ();
                break;
            case Mode.ExecutionOrderSettings:
                m_execTab.OnGUI ();
                break;
            }
            
		}

        private void DrawToolBar() {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            float toolbarWidth = position.width - (20f*2f);
            string[] labels = new string[] { "Asset Bundles", "Execution Order" };
            m_mode = (Mode)GUILayout.Toolbar((int)m_mode, labels, "LargeButton", GUILayout.Width(toolbarWidth) );
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(8f);
        }
	}
}
