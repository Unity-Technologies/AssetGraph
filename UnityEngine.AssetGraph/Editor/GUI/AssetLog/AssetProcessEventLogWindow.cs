#if UNITY_5_6_OR_NEWER
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
	public class AssetProcessEventLogWindow : EditorWindow {

        private AssetProcessEventLogViewController m_logViewController;

        private bool m_showError;
        private bool m_showInfo;
        private Texture2D m_errorIcon;
        private Texture2D m_infoIcon;

        [MenuItem(Model.Settings.GUI_TEXT_MENU_ASSETLOGWINDOW_OPEN, false, 2)]
		public static void Open () {
            GetWindow<AssetProcessEventLogWindow>();
		}

		private void Init() {
			this.titleContent = new GUIContent("Asset Log");
			this.minSize = new Vector2(150f, 100f);

            m_errorIcon = EditorGUIUtility.Load ("icons/console.erroricon.sml.png") as Texture2D;
            m_infoIcon = EditorGUIUtility.Load ("icons/console.infoicon.sml.png") as Texture2D;

            m_showError = true;
            m_showInfo = true;

            m_logViewController = new AssetProcessEventLogViewController ();
		}

		public void OnEnable () {
			Init();
            m_logViewController.OnEnabled ();
		}

		public void OnFocus() {
            RefreshView();
		}

		public void OnDisable() {
		}

        public void DrawToolBar() {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar)) {

                var r = AssetProcessEventRecord.GetRecord ();

                if (GUILayout.Button ("Clear", EditorStyles.toolbarButton, GUILayout.Height (Model.Settings.GUI.TOOLBAR_HEIGHT))) {
                    AssetProcessEventRecord.GetRecord ().Clear (true);
                    m_logViewController.EventSelectionChanged (null);
                    RefreshView ();
                }

                GUILayout.FlexibleSpace();

                var showInfo = GUILayout.Toggle(m_showInfo, new GUIContent(r.InfoEventCount.ToString(), m_infoIcon, "Toggle Show Info"), EditorStyles.toolbarButton, GUILayout.Height(Model.Settings.GUI.TOOLBAR_HEIGHT));
                var showError = GUILayout.Toggle(m_showError, new GUIContent(r.ErrorEventCount.ToString(), m_errorIcon, "Toggle Show Errors"), EditorStyles.toolbarButton, GUILayout.Height(Model.Settings.GUI.TOOLBAR_HEIGHT));

                if(showInfo != m_showInfo || showError != m_showError) {
                    m_showInfo = showInfo;
                    m_showError = showError;
                    r.SetFilterCondition(m_showInfo, m_showError);
                    m_logViewController.ReloadAndSelect ();
                }
            }
        }

		public void OnGUI () {

            DrawToolBar ();

            if (m_logViewController.OnLogViewGUI ()) {
                Repaint ();
            }
		}

        private void RefreshView() {
            m_logViewController.ReloadAndSelect ();
        }

        private void OnAssetsReimported(AssetPostprocessorContext ctx) {
            RefreshView ();
		}

        public static void NotifyAssetsReimportedToAllWindows(AssetPostprocessorContext ctx) {
            var windows = Resources.FindObjectsOfTypeAll<AssetProcessEventLogWindow>();
			foreach(var w in windows) {
				w.OnAssetsReimported(ctx);
			}
		}
	}
}
#endif

