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

		private static readonly string kPREFKEY_LASTSELECTEDCOLLECTION = "AssetBundles.GraphTool.LastSelectedCollection";

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

		private List<GraphEntry> m_graphsInProject;

		private static BatchBuildWindow s_window;

		[MenuItem(Model.Settings.GUI_TEXT_MENU_BATCHWINDOW_OPEN, false, 2)]
		public static void Open () {
			GetWindow<BatchBuildWindow>();
		}

		private void Init() {
			LogUtility.Logger.filterLogType = LogType.Warning;

			this.titleContent = new GUIContent("Batch Build");
			this.minSize = new Vector2(150f, 100f);
			this.maxSize = new Vector2(300f, 600f);
			m_graphsInProject = new List<GraphEntry>();

			m_activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;

			string lastCollection = EditorPrefs.GetString(kPREFKEY_LASTSELECTEDCOLLECTION);
			m_currentCollection = BatchBuildConfig.GetConfig().Find(lastCollection);

			scrollPos = new Vector2(0f,0f);
			UpdateGraphList();
		}

		private void UpdateGraphList() {
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
						var v = EditorGUILayout.ToggleLeft(c.Name, c.Selected);
						if(v != c.Selected) {
							c.Selected = v;
							UpdateCollection();
						}
					}
				}
			}
		}

		private void SelectGraphCollection(BatchBuildConfig.GraphCollection c) {
			UpdateGraphList();

			m_currentCollection = c;

			m_graphsInProject.ForEach(v => v.Selected = false);

			foreach(var guid in c.GraphGUIDs) {
				var entry = m_graphsInProject.Find(v => v.Guid == guid);
				entry.Selected = true;
			}
		}

		private BatchBuildConfig.GraphCollection CreateNewCollectionFromCurrent() {

			var collection = new BatchBuildConfig.GraphCollection("New Collection");

			foreach(var v in m_graphsInProject) {
				if(v.Selected) {
					collection.GraphGUIDs.Add(v.Guid);
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

			GUILayout.Label("Build Config Settings", new GUIStyle("BoldLabel"));
			GUILayout.Space(4f);
			using(new EditorGUI.DisabledScope(m_currentCollection == null)) {
				var newName = EditorGUILayout.TextField("Name", currentCollectionName);
				if(newName != currentCollectionName) {
					currentCollectionName = newName;
					m_currentCollection.Name = newName;
				}
			}
			using(new EditorGUILayout.HorizontalScope()) {
				if (GUILayout.Button(new GUIContent(currentCollectionName, "Select Collection"), EditorStyles.popup)) {
					GenericMenu menu = new GenericMenu();

					foreach(var c in BatchBuildConfig.GetConfig().GraphCollections) {

						menu.AddItem(new GUIContent(c.Name), false, () => {
							SelectGraphCollection(c);
						});
					}

					menu.AddSeparator("");
					menu.AddItem(new GUIContent("Create New..."), false, () => {

						var newCollection = CreateNewCollectionFromCurrent();
						BatchBuildConfig.GetConfig().GraphCollections.Add(newCollection);
						m_currentCollection = newCollection;
					});

					menu.DropDown(new Rect(4f, 90f, 0f, 0f));
				}
				if(GUILayout.Button("Delete", GUILayout.Width(50f))) {
					BatchBuildConfig.GetConfig().GraphCollections.Remove(m_currentCollection);
					m_currentCollection = null;
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

				if(GUILayout.Button("Build")) {
					var collection = m_currentCollection;
					if(collection == null) {
						collection = CreateNewCollectionFromCurrent();
					}

					AssetBundleGraphUtility.ExecuteGraphCollection(m_activeBuildTarget, collection);
				}
				GUILayout.Space(8f);
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
