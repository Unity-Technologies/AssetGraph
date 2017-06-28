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
	public class BatchBuildWindow : EditorWindow {

		private class GraphEntry {
			private string name;
			private string guid;
			private bool selected;
			public GraphEntry(string guid) {
				this.guid = guid;
				this.name = Path.GetFileNameWithoutExtension( AssetDatabase.GUIDToAssetPath(guid) );
				selected = false;
			}

			public void Refresh() {
				this.name = Path.GetFileNameWithoutExtension( AssetPath );
			}

			public string Name {
				get {
					return name;
				}
			}
			public string Guid {
				get {
					return guid;
				}
			}
			public string AssetPath {
				get {
					return AssetDatabase.GUIDToAssetPath(guid);
				}
			}
			public bool Selected {
				get {
					return selected;
				}
				set {
					selected = value;
				}
			}
		}

		private BatchBuildConfig.GraphCollection m_currentCollection;
		private BuildTarget m_activeBuildTarget;
		private Vector2 scrollPos;
		private bool m_useGraphCollection;

		private List<GraphEntry> m_graphsInProject;

		private List<ExecuteGraphResult> m_result;

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
			LogUtility.Logger.filterLogType = LogType.Warning;

			this.titleContent = new GUIContent("Batch Build");
			this.minSize = new Vector2(150f, 100f);
			this.maxSize = new Vector2(300f, 600f);
			m_graphsInProject = new List<GraphEntry>();

			m_activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;

            m_useGraphCollection = Model.Settings.UserSettings.BatchBuildUseCollectionState;
            string lastCollection = Model.Settings.UserSettings.BatchBuildLastSelectedCollection;
			var newCollection = BatchBuildConfig.GetConfig().Find(lastCollection);

			SelectGraphCollection(newCollection);

			scrollPos = new Vector2(0f,0f);
			UpdateGraphList();
		}

		private void UpdateGraphList() {

			if(m_graphsInProject == null) {
				return;
			}

			var guids = AssetDatabase.FindAssets(Model.Settings.GRAPH_SEARCH_CONDITION);
			var newList = new List<GraphEntry>(guids.Length);

			foreach(var guid in guids) {
				var e = m_graphsInProject.Find(v => v.Guid == guid);
				if(e == null) {
					e = new GraphEntry(guid);
				} else {
					e.Refresh();
				}
				newList.Add(e);
			}

			m_graphsInProject = newList;
		}

		private void DrawGraphList() {
			GUILayout.Label("Graphs", new GUIStyle("BoldLabel"));
			using(new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				using(var scrollScope = new EditorGUILayout.ScrollViewScope(scrollPos) ) {
					scrollPos = scrollScope.scrollPosition;

					for(int i = 0; i < m_graphsInProject.Count; ++i) {
						var c = m_graphsInProject[i];

						using(new EditorGUILayout.HorizontalScope()) {
							var v = EditorGUILayout.ToggleLeft(c.Name, c.Selected);
							if(v != c.Selected) {
								c.Selected = v;
								UpdateCollection();
							}
							GUILayout.FlexibleSpace();

							if(m_result != null) {
								var r = m_result.Find(x => x.GraphAssetPath == c.AssetPath);
								if(r != null && r.IsAnyIssueFound) {
									GUILayout.Label("Failed", "ErrorLabel");
								}
							}

							if(GUILayout.Button("Edit", GUILayout.Width(40f))) {
								var w = GetWindow<AssetBundleGraphEditorWindow>();
								w.OpenGraph(c.AssetPath);
							}
						}
					}
				}
			}
		}

		private void SelectGraphCollection(BatchBuildConfig.GraphCollection c) {
			UpdateGraphList();

			m_currentCollection = c;

			m_graphsInProject.ForEach(v => v.Selected = false);

			if(c != null) {
				foreach(var guid in c.GraphGUIDs) {
					var entry = m_graphsInProject.Find(v => v.Guid == guid);
					if(entry != null) {
						entry.Selected = true;
					}
				}
                Model.Settings.UserSettings.BatchBuildLastSelectedCollection = m_currentCollection.Name;
			}
		}

		private BatchBuildConfig.GraphCollection CreateNewCollection(bool applyCurrent) {

			string newConfigName = null;
			bool goodNameFound = false;
			BatchBuildConfig.GraphCollection collection = null;
			int count = 0;
			while(!goodNameFound) {
				string space = count == 0 ? "" : " ";
				string countName = count == 0 ? "" : count.ToString();

				newConfigName = string.Format("New Collection{0}{1}", space, countName);
				collection = BatchBuildConfig.GetConfig().Find(newConfigName);
				goodNameFound = collection == null;
				++count;
			}

			collection = new BatchBuildConfig.GraphCollection(newConfigName);


			if(applyCurrent) {
				foreach(var v in m_graphsInProject) {
					if(v.Selected) {
						collection.GraphGUIDs.Add(v.Guid);
					}
				}
			}

			return collection;
		}

		private void UnselectAllGraphs() {
			m_graphsInProject.ForEach(v => v.Selected = false);
		}

		private void UpdateCollection() {

			if(m_currentCollection != null) {
				m_currentCollection.GraphGUIDs.Clear();
				foreach(var v in m_graphsInProject) {
					if(v.Selected) {
						m_currentCollection.GraphGUIDs.Add(v.Guid);
					}
				}
			}
		}

		private void DrawCollectionSelector() {

			string currentCollectionName = (m_currentCollection == null)? "" : m_currentCollection.Name;

			m_useGraphCollection = EditorGUILayout.ToggleLeft("Use Graph Collection", m_useGraphCollection, new GUIStyle("BoldLabel"));

			if(m_useGraphCollection && m_currentCollection == null) {
                string lastCollection = Model.Settings.UserSettings.BatchBuildLastSelectedCollection;
				m_currentCollection = BatchBuildConfig.GetConfig().Find(lastCollection);
			} 
			if(GUI.changed) {
                Model.Settings.UserSettings.BatchBuildUseCollectionState = m_useGraphCollection;
			}

			using(new EditorGUI.DisabledScope(!m_useGraphCollection)) {
				GUILayout.Space(4f);
				using(new EditorGUILayout.HorizontalScope()) {
					if (GUILayout.Button(new GUIContent(currentCollectionName, "Select Collection"), EditorStyles.popup)) {
						GenericMenu menu = new GenericMenu();

						foreach(var c in BatchBuildConfig.GetConfig().GraphCollections) {
							var collection = c;
							menu.AddItem(new GUIContent(collection.Name), false, () => {
								SelectGraphCollection(collection);
							});
						}

						menu.AddSeparator("");
						menu.AddItem(new GUIContent("Create New..."), false, () => {

							var newCollection = CreateNewCollection(false);
							BatchBuildConfig.GetConfig().GraphCollections.Add(newCollection);
							BatchBuildConfig.SetConfigDirty();
							m_currentCollection = newCollection;
						});

						menu.DropDown(new Rect(4f, 80f, 0f, 0f));
					}
					if(GUILayout.Button("Delete", GUILayout.Width(50f))) {
						BatchBuildConfig.GetConfig().GraphCollections.Remove(m_currentCollection);
						BatchBuildConfig.SetConfigDirty();
						m_currentCollection = null;
					}
				}
				using(new EditorGUI.DisabledScope(m_currentCollection == null)) {
					var newName = EditorGUILayout.TextField("Name", currentCollectionName);
					if(newName != currentCollectionName) {
						currentCollectionName = newName;
						m_currentCollection.Name = newName;
						BatchBuildConfig.SetConfigDirty();
					}
				}
			}
		}

		public void OnEnable () {
			Init();
		}

		public void OnFocus() {
			UpdateGraphList();
		}

		public void OnDisable() {
		}

		public void OnGUI () {

			using (new EditorGUILayout.VerticalScope()) {

				GUILayout.Label("Build Target", new GUIStyle("BoldLabel"));

				var supportedTargets = NodeGUIUtility.SupportedBuildTargets;
				int currentIndex = Mathf.Max(0, supportedTargets.FindIndex(t => t == m_activeBuildTarget));

				int newIndex = EditorGUILayout.Popup(currentIndex, NodeGUIUtility.supportedBuildTargetNames, EditorStyles.popup);

				if(newIndex != currentIndex) {
					m_activeBuildTarget = supportedTargets[newIndex];
				}

				GUILayout.Space(8f);

				DrawCollectionSelector();

				GUILayout.Space(8f);

				DrawGraphList();

				GUILayout.Space(10f);

				if(DidLastBuildFailed) {
					EditorGUILayout.HelpBox("Last batch build failed.", MessageType.Error);
				}

				if(GUILayout.Button("Build")) {
                    EditorApplication.delayCall += Build;
				}
				GUILayout.Space(8f);
			}
		}

		private void Build() {
			m_result = null;
			var collection = m_currentCollection;
			if(!m_useGraphCollection || collection == null) {
				collection = CreateNewCollection(true);
			}

			m_result = AssetBundleGraphUtility.ExecuteGraphCollection(m_activeBuildTarget, collection);

			foreach(var r in m_result) {
				if(r.IsAnyIssueFound) {
					foreach(var e in r.Issues) {
						
						LogUtility.Logger.LogError(LogUtility.kTag, r.Graph.name + ":" + e.reason);
					}
				}
			}
		}

		private bool DidLastBuildFailed {
			get {
				if(m_result == null) {
					return false;
				}

				foreach(var r in m_result) {
					if(r.IsAnyIssueFound) {
						return true;
					}
				}
				return false;
			}
		}

		private void OnAssetsReimported(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
			UpdateGraphList();
		}

		public static void NotifyAssetsReimportedToAllWindows(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
			var windows = Resources.FindObjectsOfTypeAll<BatchBuildWindow>();
			foreach(var w in windows) {
				w.OnAssetsReimported(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
			}
		}
	}
}
