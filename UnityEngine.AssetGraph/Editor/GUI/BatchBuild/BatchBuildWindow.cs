using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
	public class BatchBuildWindow : EditorWindow {

        private enum Mode : int
        {
            Manage,
            Execute
        }

        private GraphCollectionManageTab m_manageTab;
        private GraphCollectionExecuteTab m_executeTab;
        private Mode m_mode;

		private static BatchBuildWindow s_window;

		[MenuItem(Model.Settings.GUI_TEXT_MENU_BATCHWINDOW_OPEN, false, 2)]
		public static void Open () {
			GetWindow<BatchBuildWindow>();
		}

		[MenuItem(Model.Settings.GUI_TEXT_MENU_BATCHBUILD, false, 2+101)]
		public static void BuildFromMenu() {
			var w = GetWindow<BatchBuildWindow>() as BatchBuildWindow;
			w.Build();
		}

		[MenuItem(Model.Settings.GUI_TEXT_MENU_BATCHBUILD, true, 2+101)]
		public static bool BuildFromMenuValidator() {
			var windows = Resources.FindObjectsOfTypeAll<BatchBuildWindow>();

			return windows.Length > 0;
		}


		private void Init() {
			this.titleContent = new GUIContent("Batch Build");
			this.minSize = new Vector2(350f, 300f);

            m_manageTab = new GraphCollectionManageTab ();
            m_executeTab = new GraphCollectionExecuteTab ();
            m_mode = Mode.Manage;
		}

		public void OnEnable () {
			Init();
            m_manageTab.OnEnable (position, this);
            m_executeTab.OnEnable (position, this);
		}

		public void OnFocus() {
            m_manageTab.Refresh ();
            m_executeTab.Refresh ();
		}

		public void OnDisable() {
		}

        public void OnGUI () {

            DrawToolBar ();

            var tabRect = GUILayoutUtility.GetRect (100f, 100f, GUILayout.ExpandHeight (true), GUILayout.ExpandWidth (true));
            var bound = new Rect (0f, 0f, tabRect.width, tabRect.height);

            GUI.BeginGroup (tabRect);

            switch (m_mode) {
            case Mode.Manage:
                m_manageTab.OnGUI (bound);
                break;
            case Mode.Execute:
                m_executeTab.OnGUI (bound);
                break;
            }

            GUI.EndGroup ();
        }

        private void DrawToolBar() {

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            float toolbarWidth = position.width - (20f*2f);
            string[] labels = new string[] { "Manage", "Execute" };
            m_mode = (Mode)GUILayout.Toolbar((int)m_mode, labels, "LargeButton", GUILayout.Width(toolbarWidth) );
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(8f);
        }

        private void Build() {
            m_executeTab.Build ();
        }
	}
}
