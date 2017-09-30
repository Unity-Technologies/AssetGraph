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
	public class AssetBundleGraphEditorWindow : EditorWindow {
		[Serializable]
		public class SavedSelection {
			[SerializeField] public List<NodeGUI> nodes;
			[SerializeField] public List<ConnectionGUI> connections;
			[SerializeField] private float m_pasteOffset = kPasteOffset;

			static readonly float kPasteOffset = 20.0f;

			public SavedSelection (SavedSelection s) {
				nodes = new List<NodeGUI>(s.nodes);
				connections = new List<ConnectionGUI>(s.connections);
			}

			public SavedSelection () {
				nodes = new List<NodeGUI>();
				connections = new List<ConnectionGUI>();
			}

			public SavedSelection (IEnumerable<NodeGUI> n, IEnumerable<ConnectionGUI> c) {
				nodes = new List<NodeGUI>(n);
				connections = new List<ConnectionGUI>(c);
			}

			public bool IsSelected {
				get {
					return (nodes.Count + connections.Count) > 0;
				}
			}

			public float PasteOffset {
				get {
					return m_pasteOffset;
				}
			}

			public void IncrementPasteOffset() {
				m_pasteOffset += kPasteOffset;
			}

			public void Add(NodeGUI n) {
				nodes.Add(n);
			}

			public void Add(ConnectionGUI c) {
				connections.Add(c);
			}

			public void Remove(NodeGUI n) {
				nodes.Remove(n);
			}

			public void Remove(ConnectionGUI c) {
				connections.Remove(c);
			}

			public void Toggle(NodeGUI n) {
				if(nodes.Contains(n)) {
					nodes.Remove(n);
				} else {
					nodes.Add(n);
				}
			}

			public void Toggle(ConnectionGUI c) {
				if(connections.Contains(c)) {
					connections.Remove(c);
				} else {
					connections.Add(c);
				}
			}

			public void Clear(AssetBundleGraphController controller, bool deactivate = false) {

				if(deactivate) {
					foreach(var n in nodes) {
						n.SetActive(false);
					}

					foreach(var c in connections) {
						c.SetActive(false);
					}
				}

				nodes.Clear();
				connections.Clear();
			}
		}

		// hold selection start data.
		public class SelectPoint {
			public readonly float x;
			public readonly float y;

			public SelectPoint (Vector2 position) {
				this.x = position.x;
				this.y = position.y;
			}
		}

		public enum ModifyMode : int {
			NONE,
			CONNECTING,
			SELECTING,
			DRAGGING
		}

		public enum ScriptType : int {
			SCRIPT_MODIFIER,		
			SCRIPT_PREFABBUILDER,
			SCRIPT_POSTPROCESS,
			SCRIPT_NODE,
			SCRIPT_FILTER,
            SCRIPT_ASSETGENERATOR
		}
			
		[SerializeField] private List<NodeGUI> nodes = new List<NodeGUI>();
		[SerializeField] private List<ConnectionGUI> connections = new List<ConnectionGUI>();

		[SerializeField] private SavedSelection activeSelection = null;
		[SerializeField] private SavedSelection copiedSelection = null;

		private bool showErrors;
		private bool showVerboseLog;
		private NodeEvent currentEventSource;
		private Texture2D _selectionTex;
		private GUIContent _reloadButtonTexture;
		private ModifyMode modifyMode;
		private Vector2 spacerRectRightBottom;
		private Vector2 scrollPos = new Vector2(1500,0);
		private Vector2 errorScrollPos = new Vector2(0,0);
		private Rect graphRegion = new Rect();
		private SelectPoint selectStartMousePosition;
		private GraphBackground background = new GraphBackground();
		private string graphAssetPath;
		private string graphAssetName;

		private AssetBundleGraphController controller;
		private BuildTarget target;

		private Vector2 m_LastMousePosition;
		private Vector2 m_DragNodeDistance;
		private readonly Dictionary<NodeGUI, Vector2> m_InitialDragNodePositions = new Dictionary<NodeGUI, Vector2> ();

		private static readonly string kPREFKEY_LASTEDITEDGRAPH = "AssetBundles.GraphTool.LastEditedGraph";
		static readonly int kDragNodesControlID = "AssetBundleGraphTool.HandleDragNodes".GetHashCode();

		private GUIContent ReloadButtonTexture {
			get {
				if( _reloadButtonTexture == null ) {
					_reloadButtonTexture = EditorGUIUtility.IconContent("RotateTool");
				}
				return _reloadButtonTexture;
			}
		}

		private bool IsAnyIssueFound {
			get {
				if(controller == null) {
					return true;
				}
				return controller.IsAnyIssueFound;
			}
		}

		/*
		 * An alternative way to get Window, becuase
		 * GetWindow<AssetBundleGraphEditorWindow>() forces window to be active and present
		 */ 
		private static AssetBundleGraphEditorWindow Window {
			get {
				AssetBundleGraphEditorWindow[] windows = Resources.FindObjectsOfTypeAll<AssetBundleGraphEditorWindow>();
				if(windows.Length > 0) {
					return windows[0];
				}

				return null;
			}
		}

		public static void GenerateScript (ScriptType scriptType) {
            var destinationBasePath = Model.Settings.Path.UserSpacePath;

			var sourceFileName = string.Empty;
			var destinationFileName = string.Empty;

			switch (scriptType) {
			case ScriptType.SCRIPT_MODIFIER: 
				{
                    sourceFileName = FileUtility.PathCombine(Model.Settings.Path.ScriptTemplatePath, "MyModifier.cs.template");
					destinationFileName = "MyModifier{0}{1}";
					break;
				}
			case ScriptType.SCRIPT_PREFABBUILDER: {
                    sourceFileName = FileUtility.PathCombine(Model.Settings.Path.ScriptTemplatePath, "MyPrefabBuilder.cs.template");
					destinationFileName = "MyPrefabBuilder{0}{1}";
					break;
				}
			case ScriptType.SCRIPT_POSTPROCESS: {
                    sourceFileName = FileUtility.PathCombine(Model.Settings.Path.ScriptTemplatePath, "MyPostprocess.cs.template");
					destinationFileName = "MyPostprocess{0}{1}";
					break;
				}
			case ScriptType.SCRIPT_FILTER: {
                    sourceFileName = FileUtility.PathCombine(Model.Settings.Path.ScriptTemplatePath, "MyFilter.cs.template");
					destinationFileName = "MyFilter{0}{1}";
					break;
				}
            case ScriptType.SCRIPT_NODE: {
                    sourceFileName = FileUtility.PathCombine(Model.Settings.Path.ScriptTemplatePath, "MyNode.cs.template");
                    destinationFileName = "MyNode{0}{1}";
                    break;
                }
            case ScriptType.SCRIPT_ASSETGENERATOR: {
                    sourceFileName = FileUtility.PathCombine(Model.Settings.Path.ScriptTemplatePath, "MyGenerator.cs.template");
                    destinationFileName = "MyGenerator{0}{1}";
                    break;
                }
			default: {
					LogUtility.Logger.LogError(LogUtility.kTag, "Unknown script type found:" + scriptType);
					break;
				}
			}

			if (string.IsNullOrEmpty(sourceFileName) || string.IsNullOrEmpty(destinationFileName)) {
				return;
			}

			var destinationPath = FileUtility.PathCombine(destinationBasePath, string.Format(destinationFileName, "", ".cs"));
			int count = 0;
			while(File.Exists(destinationPath)) {
				destinationPath = FileUtility.PathCombine(destinationBasePath, string.Format(destinationFileName, ++count, ".cs"));
			}

            FileUtility.CopyTemplateFile(sourceFileName, destinationPath, string.Format(destinationFileName, "", ""), string.Format(destinationFileName, count==0?"":count.ToString(), ""));

			AssetDatabase.Refresh();

			//Highlight in ProjectView
			MonoScript s = AssetDatabase.LoadAssetAtPath<MonoScript>(destinationPath);
			UnityEngine.Assertions.Assert.IsNotNull(s);
			EditorGUIUtility.PingObject(s);
		}

		/*
			menu items
		*/
		[MenuItem(Model.Settings.GUI_TEXT_MENU_GENERATE_FILTER)]
		public static void GenerateCustomFilter () {
			GenerateScript(ScriptType.SCRIPT_FILTER);
		}
		[MenuItem(Model.Settings.GUI_TEXT_MENU_GENERATE_MODIFIER)]
		public static void GenerateModifier () {
			GenerateScript(ScriptType.SCRIPT_MODIFIER);
		}
		[MenuItem(Model.Settings.GUI_TEXT_MENU_GENERATE_PREFABBUILDER)]
		public static void GeneratePrefabBuilder () {
			GenerateScript(ScriptType.SCRIPT_PREFABBUILDER);
		}
		[MenuItem(Model.Settings.GUI_TEXT_MENU_GENERATE_POSTPROCESS)]
		public static void GeneratePostprocess () {
			GenerateScript(ScriptType.SCRIPT_POSTPROCESS);
		}
		[MenuItem(Model.Settings.GUI_TEXT_MENU_GENERATE_NODE)]
		public static void GenerateCustomNode () {
			GenerateScript(ScriptType.SCRIPT_NODE);
		}
        [MenuItem(Model.Settings.GUI_TEXT_MENU_GENERATE_ASSETGENERATOR)]
        public static void GenerateAssetGenerator () {
            GenerateScript(ScriptType.SCRIPT_ASSETGENERATOR);
        }
			
		[MenuItem(Model.Settings.GUI_TEXT_MENU_OPEN, false, 1)]
		public static void Open () {
			GetWindow<AssetBundleGraphEditorWindow>();
		}

		[MenuItem(Model.Settings.GUI_TEXT_MENU_DELETE_CACHE)] public static void DeleteCache () {
            FileUtility.RemakeDirectory(Model.Settings.Path.CachePath);

			AssetDatabase.Refresh();
		}

		[MenuItem(Model.Settings.GUI_TEXT_MENU_DELETE_IMPORTSETTING_SETTINGS)] public static void DeleteImportSettingSample () {

			var result = EditorUtility.DisplayDialog("Erase All Import Settings", "Do you want to erase settings for all ImportSetting node? " +
				"This operation is not undoable. It will affect all graphs in this project.", "Yes", "Cancel");

			if(result) {
                FileUtility.RemakeDirectory(Model.Settings.Path.ImporterSettingsPath);
				AssetDatabase.Refresh();
			}
		}

		[MenuItem(Model.Settings.GUI_TEXT_MENU_BUILD, true, 1 + 101)]
		public static bool BuildFromMenuValidator () {
			// Calling GetWindow<>() will force open window
			// That's not what we want to do in validator function,
			// so just reference s_currentController directly
			var w = Window;
			if(w == null) {
				return false;
			}
			return !w.IsAnyIssueFound;
		}

		[MenuItem(Model.Settings.GUI_TEXT_MENU_BUILD, false, 1 + 101)]
		public static void BuildFromMenu () {
			var window = GetWindow<AssetBundleGraphEditorWindow>();
			window.SaveGraph();
			window.Run();
		}


		public void OnFocus () {
			// update handlers. these static handlers are erase when window is full-screened and badk to normal window.
			modifyMode = ModifyMode.NONE;
			NodeGUIUtility.NodeEventHandler = HandleNodeEvent;
			ConnectionGUIUtility.ConnectionEventHandler = HandleConnectionEvent;

			HandleSelectionChange();
		}

		public void OnLostFocus() {
			modifyMode = ModifyMode.NONE;
		}

		public void OnProjectChange() {
			HandleSelectionChange ();
			Repaint();
		}

		public void OnSelectionChange ()
		{
			HandleSelectionChange();
			Repaint();
		}

		public void HandleSelectionChange ()
		{
			Model.ConfigGraph selectedGraph = null;

//			if (Selection.activeObject == null)
//			{
//				controller = null;
//			}

			if (Selection.activeObject is Model.ConfigGraph && EditorUtility.IsPersistent(Selection.activeObject))
			{
				selectedGraph = Selection.activeObject as Model.ConfigGraph;
			}

			if (selectedGraph != null && (controller == null || selectedGraph != controller.TargetGraph))
			{
				OpenGraph(selectedGraph);
			}
		}

		public void SelectNode(string nodeId) {
			var selectObject = nodes.Find(node => node.Id == nodeId);
			foreach (var node in nodes) {
				node.SetActive( node == selectObject );
			}
		}

		private void Init() {
			LogUtility.Logger.filterLogType = LogType.Warning;

			this.titleContent = new GUIContent("AssetBundle");
			this.minSize = new Vector2(600f, 300f);
			this.wantsMouseMove = true;

			target = EditorUserBuildSettings.activeBuildTarget;

			Undo.undoRedoPerformed += () => {
				Setup();
				Repaint();
			};

			modifyMode = ModifyMode.NONE;
			NodeGUIUtility.NodeEventHandler = HandleNodeEvent;
			ConnectionGUIUtility.ConnectionEventHandler = HandleConnectionEvent;

			string lastGraphAssetPath = EditorPrefs.GetString(kPREFKEY_LASTEDITEDGRAPH);

			if(!string.IsNullOrEmpty(lastGraphAssetPath)) {
				var graph = AssetDatabase.LoadAssetAtPath<Model.ConfigGraph>(lastGraphAssetPath);
				if(graph != null) {
					OpenGraph(graph);
				}
			}
		}

		private void ShowErrorOnNodes () {
			foreach (var node in nodes) {
				node.ResetErrorStatus();
				var errorsForeachNode = controller.Issues.Where(e => e.Id == node.Id).Select(e => e.reason).ToList();
				if (errorsForeachNode.Any()) {
					node.AppendErrorSources(errorsForeachNode);
				}
			}
		}
			
		private void SetGraphAssetPath(string newPath) {
			if(newPath == null) {
				graphAssetPath = null;
				graphAssetName = null;
			} else {
				graphAssetPath = newPath;
				graphAssetName = Path.GetFileNameWithoutExtension(graphAssetPath);
				if(graphAssetName.Length > Model.Settings.GUI.TOOLBAR_GRAPHNAMEMENU_CHAR_LENGTH) {
					graphAssetName = graphAssetName.Substring(0, Model.Settings.GUI.TOOLBAR_GRAPHNAMEMENU_CHAR_LENGTH) + "...";
				}

				EditorPrefs.SetString(kPREFKEY_LASTEDITEDGRAPH, graphAssetPath);
			}
		}

		[UnityEditor.Callbacks.OnOpenAsset()]
		public static bool OnOpenAsset( int instanceID, int line )
		{
			var graph = EditorUtility.InstanceIDToObject( instanceID ) as Model.ConfigGraph;
			if(graph != null) {
				var window = GetWindow<AssetBundleGraphEditorWindow>();
				window.OpenGraph(graph);
				return true;
			}
			return false;
		}

		public void OpenGraph (string path) {
			Model.ConfigGraph graph = AssetDatabase.LoadAssetAtPath<Model.ConfigGraph>(path);
			if(graph == null) {
				throw new AssetBundleGraphException("Could not open graph:" + path);
			}
			OpenGraph(graph);
		}

		public void OpenGraph (Model.ConfigGraph graph) {

			CloseGraph();

			SetGraphAssetPath(AssetDatabase.GetAssetPath(graph));

			modifyMode = ModifyMode.NONE;

			scrollPos = new Vector2(0,0);
			errorScrollPos = new Vector2(0,0);

			selectStartMousePosition = null;
			activeSelection = null;
			currentEventSource = null;

			controller = new AssetBundleGraphController(graph);
			ConstructGraphGUI();
			Setup();

			if (nodes.Any()) {
				UpdateSpacerRect();
			}

			Selection.activeObject = graph;
		}

		private void CloseGraph() {

			modifyMode = ModifyMode.NONE;
			SetGraphAssetPath(null);
			controller = null;
			nodes = null;
			connections = null;

			selectStartMousePosition = null;
			activeSelection = null;
			currentEventSource = null;
		}

		private void CreateNewGraphFromDialog() {
			string path =
			EditorUtility.SaveFilePanelInProject(
				"Create New AssetBundle Graph", 
				"AssetBundle Graph", "asset", 
				"Create a new asset bundle graph:");
			if(string.IsNullOrEmpty(path)) {
				return;
			}

			Model.ConfigGraph graph = Model.ConfigGraph.CreateNewGraph(path);
			OpenGraph(graph);
		}

		private void CreateNewGraphFromImport() {
			string path =
				EditorUtility.SaveFilePanelInProject(
					"Import AssetBundle Graph", 
					"AssetBundle Graph", "asset", 
					"Create a new asset bundle graph from previous version data:");
			if(string.IsNullOrEmpty(path)) {
				return;
			}

			Model.ConfigGraph graph = Model.ConfigGraph.CreateNewGraphFromImport(path);
			OpenGraph(graph);
		}

		/**
		 * Get WindowId does not collide with other nodeGUIs
		 */ 
		private static int GetSafeWindowId(List<NodeGUI> nodeGUIs) {
			int id = -1;

			foreach(var nodeGui in nodeGUIs) {
				if(nodeGui.WindowId > id) {
					id = nodeGui.WindowId;
				}
			}
			return id + 1;
		}

		/**
		 * Creates Graph structure with NodeGUI and ConnectionGUI from SaveData
		 */ 
		private void ConstructGraphGUI () {

			var activeGraph = controller.TargetGraph;

			var currentNodes = new List<NodeGUI>();
			var currentConnections = new List<ConnectionGUI>();

			foreach (var node in activeGraph.Nodes) {
				var newNodeGUI = new NodeGUI(controller, node);
				newNodeGUI.WindowId = GetSafeWindowId(currentNodes);
				currentNodes.Add(newNodeGUI);
			}

			// load connections
			foreach (var c in activeGraph.Connections) {
				var startNode = currentNodes.Find(node => node.Id == c.FromNodeId);
				if (startNode == null) {
					continue;
				}

				var endNode = currentNodes.Find(node => node.Id == c.ToNodeId);
				if (endNode == null) {
					continue;
				}
				var startPoint = startNode.Data.FindConnectionPoint (c.FromNodeConnectionPointId);
				var endPoint = endNode.Data.FindConnectionPoint (c.ToNodeConnectionPointId);

				currentConnections.Add(ConnectionGUI.LoadConnection(c, startPoint, endPoint));
			}

			nodes = currentNodes;
			connections = currentConnections;
		}

		private void SaveGraph () {
			Assertions.Assert.IsNotNull(controller);
			controller.TargetGraph.ApplyGraph(nodes, connections);
		}

		/**
		 * Save Graph and update all nodes & connections
		 */ 
		private void Setup (bool forceVisitAll = false) {

			EditorUtility.ClearProgressBar();
			if(controller == null) {
				return;
			}

			try {
				foreach (var node in nodes) {
					node.HideProgress();
				}

				SaveGraph();

				// update static all node names.
				NodeGUIUtility.allNodeNames = new List<string>(nodes.Select(node => node.Name).ToList());

				controller.Perform(target, false, forceVisitAll, null);

				RefreshInspector(controller.StreamManager);
				ShowErrorOnNodes();
			} catch(Exception e) {
				LogUtility.Logger.LogError(LogUtility.kTag, e);
			} finally {
				EditorUtility.ClearProgressBar();
			}
		}

		private void Validate (NodeGUI node) {

			EditorUtility.ClearProgressBar();
			if(controller == null) {
				return;
			}

			try {
				node.ResetErrorStatus();
				node.HideProgress();

				SaveGraph ();

				controller.Validate(node, target);

				RefreshInspector(controller.StreamManager);
				ShowErrorOnNodes();
			} catch(Exception e) {
				LogUtility.Logger.LogError(LogUtility.kTag, e);
			} finally {
				EditorUtility.ClearProgressBar();
				Repaint();
			}
		}

		/**
		 * Execute the build.
		 */
		private void Run () {

			if(controller == null) {
				return;
			}

			try {
				AssetDatabase.SaveAssets();
                AssetBundleBuildMap.GetBuildMap ().Clear ();

				float currentCount = 0f;
				float totalCount = (float)controller.TargetGraph.Nodes.Count;
				Model.NodeData lastNode = null;

				Action<Model.NodeData, string, float> updateHandler = (node, message, progress) => {

					if(lastNode != node) {
						// do not add count on first node visit to 
						// calcurate percantage correctly
						if(lastNode != null) {
							++currentCount;
						}
						lastNode = node;
					}

					float currentNodeProgress = progress * (1.0f / totalCount);
					float currentTotalProgress = (currentCount/totalCount) + currentNodeProgress;

					string title = string.Format("Processing AssetBundle Graph[{0}/{1}]", currentCount, totalCount);
					string info  = string.Format("{0}:{1}", node.Name, message);

					EditorUtility.DisplayProgressBar(title, "Processing " + info, currentTotalProgress);
				};

				// perform setup. Fails if any exception raises.
				controller.Perform(target, false, true,  null);				 

				// if there is not error reported, then run
				if(!controller.IsAnyIssueFound) {
					controller.Perform(target, true, true, updateHandler);
				}
				RefreshInspector(controller.StreamManager);
				AssetDatabase.Refresh();
				ShowErrorOnNodes();
			} catch(Exception e) {
				LogUtility.Logger.LogError(LogUtility.kTag, e);
			} finally {
				EditorUtility.ClearProgressBar();
			}
		}

		private static void RefreshInspector (AssetReferenceStreamManager streamManager) {
			if (Selection.activeObject == null) {
				return;
			}

			switch (Selection.activeObject.GetType().ToString()) {
				case "AssetBundleGraph.ConnectionGUIInspectorHelper": {
					var con = ((ConnectionGUIInspectorHelper)Selection.activeObject).connectionGUI;
					
					// null when multiple connection deleted.
					if (string.IsNullOrEmpty(con.Id)) {
						return; 
					}

					((ConnectionGUIInspectorHelper)Selection.activeObject).UpdateAssetGroups(streamManager.FindAssetGroup(con.Id));
					break;
				}
				default: {
					// do nothing.
					break;
				}
			}
		}

		private void OnAssetsReimported(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
			if(controller != null) {
				controller.OnAssetsReimported(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
			}

			if(!string.IsNullOrEmpty(graphAssetPath)) {
				if(deletedAssets.Contains(graphAssetPath)) {
					CloseGraph();
					return;
				}

				int moveIndex = Array.FindIndex(movedFromAssetPaths, p => p == graphAssetPath);
				if(moveIndex >= 0) {
					SetGraphAssetPath(movedAssets[moveIndex]);
				}
			}
		}

		public static void NotifyAssetsReimportedToAllWindows(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
			var w = Window;
			if(w != null) {
				w.OnAssetsReimported(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
			}
		}

		private void DrawGUIToolBar() {
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar)) {

				if (GUILayout.Button(new GUIContent(graphAssetName, "Select graph"), EditorStyles.toolbarPopup, GUILayout.Width(Model.Settings.GUI.TOOLBAR_GRAPHNAMEMENU_WIDTH), GUILayout.Height(Model.Settings.GUI.TOOLBAR_HEIGHT))) {
					GenericMenu menu = new GenericMenu();

					var guids = AssetDatabase.FindAssets(Model.Settings.GRAPH_SEARCH_CONDITION);
                    var nameList = new List<string> ();

					foreach(var guid in guids) {
						string path = AssetDatabase.GUIDToAssetPath(guid);
						string name = Path.GetFileNameWithoutExtension(path);

                        // GenericMenu can't have multiple menu item with the same name
                        // Avoid name overlap
                        string menuName = name;
                        int i = 1;
                        while (nameList.Contains (menuName)) {
                            menuName = string.Format ("{0} ({1})", name, i++);
                        }

                        menu.AddItem(new GUIContent(menuName), false, () => {
							if(path != graphAssetPath) {
								var graph = AssetDatabase.LoadAssetAtPath<Model.ConfigGraph>(path);
								OpenGraph(graph);
							}
						});
                        nameList.Add (menuName);
					}

					menu.AddSeparator("");
					menu.AddItem(new GUIContent("Create New..."), false, () => {
						CreateNewGraphFromDialog();
					});

                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Import/Import JSON Graph to current graph..."), false, () => {
                        var graph = JSONGraphUtility.ImportJSONToGraphFromDialog(controller.TargetGraph);
                        if(graph != null) {
                            OpenGraph(graph);
                        }
                    });
                    menu.AddSeparator("Import/");
                    menu.AddItem(new GUIContent("Import/Import JSON Graph and create new..."), false, () => {
                        var graph = JSONGraphUtility.ImportJSONToGraphFromDialog(null);
                        if(graph != null) {
                            OpenGraph(graph);
                        }
                    });
                    menu.AddItem(new GUIContent("Import/Import JSON Graphs in folder..."), false, () => {
                        JSONGraphUtility.ImportAllJSONInDirectoryToGraphFromDialog();
                    });
                    menu.AddItem (new GUIContent ("Export/Export current graph to JSON..."), false, () => {
                        JSONGraphUtility.ExportGraphToJSONFromDialog(controller.TargetGraph);
                    });
                    menu.AddItem(new GUIContent("Export/Export all graphs to JSON..."), false, () => {
                        JSONGraphUtility.ExportAllGraphsToJSONFromDialog();
                    });

					if(Model.ConfigGraph.IsImportableDataAvailableAtDisk()) {
						menu.AddSeparator("");
						menu.AddItem(new GUIContent("Import previous version..."), false, () => {
							CreateNewGraphFromImport();
						});
					}

					menu.DropDown(new Rect(4f, 8f, 0f, 0f));
				}

				GUILayout.Space(4);

				if (GUILayout.Button(new GUIContent("Refresh", ReloadButtonTexture.image, "Refresh and reload"), EditorStyles.toolbarButton, GUILayout.Width(80), GUILayout.Height(Model.Settings.GUI.TOOLBAR_HEIGHT))) {
					Setup();
				}
				showErrors = GUILayout.Toggle(showErrors, "Show Error", EditorStyles.toolbarButton, GUILayout.Height(Model.Settings.GUI.TOOLBAR_HEIGHT));

				GUILayout.Space(4);

				showVerboseLog = GUILayout.Toggle(showVerboseLog, "Show Verbose Log", EditorStyles.toolbarButton, GUILayout.Height(Model.Settings.GUI.TOOLBAR_HEIGHT));
				LogUtility.Logger.filterLogType = (showVerboseLog)? LogType.Log : LogType.Warning;

				controller.TargetGraph.UseAsAssetPostprocessor = GUILayout.Toggle(controller.TargetGraph.UseAsAssetPostprocessor, "Use As Postprocessor", EditorStyles.toolbarButton, GUILayout.Height(Model.Settings.GUI.TOOLBAR_HEIGHT));

				GUILayout.FlexibleSpace();

				GUIStyle tbLabel = new GUIStyle(EditorStyles.toolbar);

				tbLabel.alignment = TextAnchor.MiddleCenter;

				GUIStyle tbLabelTarget = new GUIStyle(tbLabel);
				tbLabelTarget.fontStyle = FontStyle.Bold;

				GUILayout.Label("Platform:", tbLabel, GUILayout.Height(Model.Settings.GUI.TOOLBAR_HEIGHT));

				var supportedTargets = NodeGUIUtility.SupportedBuildTargets;
				int currentIndex = Mathf.Max(0, supportedTargets.FindIndex(t => t == target));

				int newIndex = EditorGUILayout.Popup(currentIndex, NodeGUIUtility.supportedBuildTargetNames, 
					EditorStyles.toolbarPopup, GUILayout.Width(150), GUILayout.Height(Model.Settings.GUI.TOOLBAR_HEIGHT));

				if(newIndex != currentIndex) {
					target = supportedTargets[newIndex];
					Setup(true);
				}

				using(new EditorGUI.DisabledScope(controller.IsAnyIssueFound)) {
                    if (GUILayout.Button ("Build", EditorStyles.toolbarButton, GUILayout.Height (Model.Settings.GUI.TOOLBAR_HEIGHT))) {
                        EditorApplication.delayCall += BuildFromMenu;
                    }
				}
			}
		}

		static readonly string kGUIDELINETEXT = "To configure asset bundle workflow, create an AssetBundle Graph.";
		static readonly string kCREATEBUTTON  = "Create";
		static readonly string kIMPORTBUTTON  = "Import previous version";

		private void DrawNoGraphGUI() {
			using(new EditorGUILayout.HorizontalScope()) {
				GUILayout.FlexibleSpace();
				using(new EditorGUILayout.VerticalScope()) {
					GUILayout.FlexibleSpace();
					var guideline = new GUIContent(kGUIDELINETEXT);
					var size = GUI.skin.label.CalcSize(guideline);
					GUILayout.Label(kGUIDELINETEXT);

					using(new EditorGUILayout.HorizontalScope()) {

						bool showImport = Model.ConfigGraph.IsImportableDataAvailableAtDisk();
						float spaceWidth = (showImport) ? (size.x - 300f)/2f : (size.x - 100f)/2f;

						GUILayout.Space(spaceWidth);
						if(GUILayout.Button(kCREATEBUTTON, GUILayout.Width(100f), GUILayout.ExpandWidth(false))) {
							CreateNewGraphFromDialog();
						}

						if(showImport) {
							GUILayout.Space(20f);
							if(GUILayout.Button(kIMPORTBUTTON, GUILayout.Width(160f), GUILayout.ExpandWidth(false))) {
								CreateNewGraphFromImport();
							}
						}
					}
					GUILayout.FlexibleSpace();
				}
				GUILayout.FlexibleSpace();
			}
		}

		private void DrawGUINodeErrors() {

			errorScrollPos = EditorGUILayout.BeginScrollView(errorScrollPos, GUI.skin.box, GUILayout.Width(200));
			{
				using (new EditorGUILayout.VerticalScope()) {
					foreach(NodeException e in controller.Issues) {
						EditorGUILayout.HelpBox(e.reason, MessageType.Error);
						if( GUILayout.Button("Go to Node") ) {
							SelectNode(e.Id);
						}
					}
				}
			}
			EditorGUILayout.EndScrollView();
		}

		private void DrawGUINodeGraph() {

			background.Draw(graphRegion, scrollPos);

			using(var scrollScope = new EditorGUILayout.ScrollViewScope(scrollPos) ) {
				scrollPos = scrollScope.scrollPosition;

				// draw connections.
				foreach (var con in connections) {
					con.DrawConnection(nodes, controller.StreamManager.FindAssetGroup(con.Id));
				}

				// draw node window x N.
				{
					BeginWindows();

					nodes.ForEach(node => node.DrawNode());

					HandleDragNodes();

					EndWindows();
				}
					
				// draw connection input point marks.
				foreach (var node in nodes) {
					node.DrawConnectionInputPointMark(currentEventSource, modifyMode == ModifyMode.CONNECTING);
				}

				// draw connection output point marks.
				foreach (var node in nodes) {
					node.DrawConnectionOutputPointMark(currentEventSource, modifyMode == ModifyMode.CONNECTING, Event.current);
				}

				// draw connecting line if modifing connection.
				switch (modifyMode) {
				case ModifyMode.CONNECTING: {
						// from start node to mouse.
						DrawStraightLineFromCurrentEventSourcePointTo(Event.current.mousePosition, currentEventSource);
						break;
					}
				case ModifyMode.SELECTING: {
						float lx = Mathf.Max(selectStartMousePosition.x, Event.current.mousePosition.x);
						float ly = Mathf.Max(selectStartMousePosition.y, Event.current.mousePosition.y);
						float sx = Mathf.Min(selectStartMousePosition.x, Event.current.mousePosition.x);
						float sy = Mathf.Min(selectStartMousePosition.y, Event.current.mousePosition.y);

						Rect sel = new Rect(sx, sy, lx - sx, ly - sy);
						GUI.Label(sel, string.Empty, "SelectionRect");
						break;
					}
				}

				// handle Graph GUI events
				HandleGraphGUIEvents();
                HandleDragAndDropGUI (graphRegion);

				// set rect for scroll.
				if (nodes.Any()) {
					UpdateSpacerRect();
                    if (Event.current.type == EventType.Layout) {
                        GUILayoutUtility.GetRect(new GUIContent(string.Empty), GUIStyle.none, GUILayout.Width(spacerRectRightBottom.x), GUILayout.Height(spacerRectRightBottom.y));
                    }
				}
			}
			if(Event.current.type == EventType.Repaint) {
				var newRgn = GUILayoutUtility.GetLastRect();
				if(newRgn != graphRegion) {
					graphRegion = newRgn;
					Repaint();
				}
			}
		}

		private void HandleGraphGUIEvents() {
			
			//mouse drag event handling.
			switch (Event.current.type) {
			// draw line while dragging.
			case EventType.MouseDrag: {
					switch (modifyMode) {
					case ModifyMode.NONE: {
							switch (Event.current.button) {
							case 0:{// left click
									if(graphRegion.Contains(Event.current.mousePosition - scrollPos)) {
										selectStartMousePosition = new SelectPoint(Event.current.mousePosition);
										modifyMode = ModifyMode.SELECTING;
									}
									break;
								}
							}
							break;
						}
					case ModifyMode.SELECTING: {
							// do nothing.
							break;
						}
					}

					HandleUtility.Repaint();
					Event.current.Use();
					break;
				}
			}

			// mouse up event handling.
			// use rawType for detect for detectiong mouse-up which raises outside of window.
			switch (Event.current.rawType) {
			case EventType.MouseUp: {
					switch (modifyMode) {
					/*
						select contained nodes & connections.
					*/
					case ModifyMode.SELECTING: {

							if(selectStartMousePosition == null) {
								break;
							}

							var x = 0f;
							var y = 0f;
							var width = 0f;
							var height = 0f;

							if (Event.current.mousePosition.x < selectStartMousePosition.x) {
								x = Event.current.mousePosition.x;
								width = selectStartMousePosition.x - Event.current.mousePosition.x;
							}
							if (selectStartMousePosition.x < Event.current.mousePosition.x) {
								x = selectStartMousePosition.x;
								width = Event.current.mousePosition.x - selectStartMousePosition.x;
							}

							if (Event.current.mousePosition.y < selectStartMousePosition.y) {
								y = Event.current.mousePosition.y;
								height = selectStartMousePosition.y - Event.current.mousePosition.y;
							}
							if (selectStartMousePosition.y < Event.current.mousePosition.y) {
								y = selectStartMousePosition.y;
								height = Event.current.mousePosition.y - selectStartMousePosition.y;
							}

							Undo.RecordObject(this, "Select Objects");

							if(activeSelection == null) {
								activeSelection = new SavedSelection();
							}

							// if shift key is not pressed, clear current selection
							if(!Event.current.shift) {
								activeSelection.Clear(controller);
							}

							var selectedRect = new Rect(x, y, width, height);

							foreach (var node in nodes) {
								if (node.GetRect().Overlaps(selectedRect)) {
									activeSelection.Add(node);
								}
							}

							foreach (var connection in connections) {
								// get contained connection badge.
								if (connection.GetRect().Overlaps(selectedRect)) {
									activeSelection.Add(connection);
								}
							}

							UpdateActiveObjects(activeSelection);

							selectStartMousePosition = null;
							modifyMode = ModifyMode.NONE;

							HandleUtility.Repaint();
							Event.current.Use();
							break;
						}
					}
					break;
				}
			}
		}

        private void HandleDragAndDropGUI (Rect dragdropArea)
        {
            Event evt = Event.current;

            switch (evt.type) {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dragdropArea.Contains (evt.mousePosition))
                    return;

                foreach (Object obj in DragAndDrop.objectReferences) {
                    var path = AssetDatabase.GetAssetPath (obj);
                    if (!string.IsNullOrEmpty (path)) {
                        FileAttributes attr = File.GetAttributes (path);

                        if ((attr & FileAttributes.Directory) == FileAttributes.Directory) {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                            break;
                        } else {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                            break;
                        }
                    }
                }

                if (evt.type == EventType.DragPerform) {
                    DragAndDrop.AcceptDrag ();

                    foreach (Object obj in DragAndDrop.objectReferences) {
                        var path = AssetDatabase.GetAssetPath (obj);
                        FileAttributes attr = File.GetAttributes(path);

                        if ((attr & FileAttributes.Directory) == FileAttributes.Directory) {
                            AddNodeFromGUI(new Loader(path), 
                                string.Format("Load from {0}", Path.GetFileName(path)), 
                                evt.mousePosition.x, evt.mousePosition.y);
                            Setup();
                            Repaint();
                        }
                    }
                }
                break;
            }
        }

		public void OnEnable () {
			Init();
		}

		public void OnDisable() {
			LogUtility.Logger.Log("OnDisable");
			if(controller != null) {
				controller.TargetGraph.Save();
			}
		}

		public void OnGUI () {

			if(controller == null) {
				DrawNoGraphGUI();
			} else {
				DrawGUIToolBar();

				using (new EditorGUILayout.HorizontalScope()) {
					DrawGUINodeGraph();
					if(showErrors) {
						DrawGUINodeErrors();
					}
				}

				if(!string.IsNullOrEmpty(graphAssetPath)) {
					using(new EditorGUILayout.HorizontalScope()) {
						GUILayout.FlexibleSpace();
						GUILayout.Label(graphAssetPath, "MiniLabel");
					}
				}

				if(controller.IsAnyIssueFound) {
					Rect msgRgn = new Rect((graphRegion.width - 250f)/2f, graphRegion.y + 8f, 250f, 36f);
					EditorGUI.HelpBox(msgRgn, "All errors needs to be fixed before building.", MessageType.Error);
				}

				HandleGUIEvent();
			}
		}

		private void HandleGUIEvent() {
			var isValidSelection = activeSelection != null && activeSelection.IsSelected;
			var isValidCopy      = copiedSelection != null && copiedSelection.IsSelected;

			/*
				Event Handling:
				- Supporting dragging script into window to create node.
				- Context Menu	
				- NodeGUI connection.
				- Command(Delete, Copy, etc...)
			*/
			switch (Event.current.type) {
			// show context menu
			case EventType.ContextClick: {
					ShowNodeCreateContextMenu(Event.current.mousePosition);
					break;
				}

				/*
					Handling mouseUp at empty space. 
				*/
			case EventType.MouseUp: {
					modifyMode = ModifyMode.NONE;
					HandleUtility.Repaint();

					if (activeSelection != null && activeSelection.IsSelected) {
						Undo.RecordObject(this, "Unselect");

						activeSelection.Clear(controller);
						UpdateActiveObjects(activeSelection);
					}

					// clear inspector
					if( Selection.activeObject is NodeGUIInspectorHelper || Selection.activeObject is ConnectionGUIInspectorHelper) {
						Selection.activeObject = null;
					}
					break;
				}

			case EventType.ValidateCommand: 
				{
					switch (Event.current.commandName) {
					case "Delete": {
							if (isValidSelection) {
								Event.current.Use();
							}
							break;
						}

					case "Copy": {
							if (isValidSelection) {
								Event.current.Use();
							}
							break;
						}

					case "Cut": {
							if (isValidSelection) {
								Event.current.Use();
							}
							break;
						}

					case "Paste": {
							if(isValidCopy) {
								Event.current.Use();
							}
							break;
						}

					case "SelectAll": {
							Event.current.Use();
							break;
						}
					}
					break;
				}

			case EventType.ExecuteCommand: 
				{
					switch (Event.current.commandName) {
					// Delete active node or connection.
					case "Delete": {
							if (!isValidSelection) {
								break;
							}
							DeleteSelected();

							Event.current.Use();
							break;
						}

					case "Copy": {
							if (!isValidSelection) {
								break;
							}

							Undo.RecordObject(this, "Copy Selected");

							copiedSelection = new SavedSelection(activeSelection);

							Event.current.Use();
							break;
						}

					case "Cut": {
							if (!isValidSelection) {
								break;
							}

							Undo.RecordObject(this, "Cut Selected");

							copiedSelection = new SavedSelection(activeSelection);

							foreach (var n in activeSelection.nodes) {
								DeleteNode(n.Id);
							}

							foreach (var c in activeSelection.connections) {
								DeleteConnection(c.Id);
							}
							activeSelection.Clear(controller);
							UpdateActiveObjects(activeSelection);

							Setup();
							//InitializeGraph();

							Event.current.Use();
							break;
						}

					case "Paste": {
							if(!isValidCopy)  {
								break;
							}

							Undo.RecordObject(this, "Paste");

                            Dictionary<NodeGUI, NodeGUI> nodeLookup = new Dictionary<NodeGUI, NodeGUI> ();

							foreach (var copiedNode in copiedSelection.nodes) {
                                var newNode = DuplicateNode(copiedNode, copiedSelection.PasteOffset);
                                nodeLookup.Add (copiedNode, newNode);
							}

                            foreach (var copiedConnection in copiedSelection.connections) {
                                DuplicateConnection (copiedConnection, nodeLookup);
                            }


							copiedSelection.IncrementPasteOffset();

							Setup();
							//InitializeGraph();

							Event.current.Use();
							break;
						}

					case "SelectAll": {
							Undo.RecordObject(this, "Select All Objects");

							if(activeSelection == null) {
								activeSelection = new SavedSelection();
							}

							activeSelection.Clear(controller);
							nodes.ForEach(n => activeSelection.Add(n));
							connections.ForEach(c => activeSelection.Add(c));

							UpdateActiveObjects(activeSelection);

							Event.current.Use();
							break;
						}

					default: {
							break;
						}
					}
					break;
				}
			}
		}

		private void DeleteSelected() {
			Undo.RecordObject(this, "Delete Selected");

			foreach (var n in activeSelection.nodes) {
				DeleteNode(n.Id);
			}

			foreach (var c in activeSelection.connections) {
				DeleteConnection(c.Id);
			}

			activeSelection.Clear(controller);
			UpdateActiveObjects(activeSelection);

			Setup();
		}

		private void ShowNodeCreateContextMenu(Vector2 pos) {
			var menu = new GenericMenu();
			var customNodes = NodeUtility.CustomNodeTypes;
			for(int i = 0; i < customNodes.Count; ++i) {
				// workaround: avoiding compilier closure bug
				var index = i;
				var name = customNodes[index].node.Name;
				menu.AddItem(
					new GUIContent(name),
					false, 
					() => {
                        AddNodeFromGUI(customNodes[index].CreateInstance(), GetNodeNameFromMenu(name), pos.x + scrollPos.x, pos.y + scrollPos.y);
						Setup();
						Repaint();
					}
				);
			}

			menu.ShowAsContext();
		}

		private string GetNodeNameFromMenu(string nodeMenuName) {
			var slashIndex = nodeMenuName.LastIndexOf('/');
			return nodeMenuName.Substring(slashIndex+1);
		}

		private void AddNodeFromGUI (Node n, string guiName, float x, float y) {

			string nodeName = guiName;
			NodeGUI newNode = new NodeGUI(controller, new Model.NodeData(nodeName, n, x, y));

			Undo.RecordObject(this, "Add " + guiName + " Node");

			AddNodeGUI(newNode);
		}

		private void DrawStraightLineFromCurrentEventSourcePointTo (Vector2 to, NodeEvent eventSource) {
			if (eventSource == null) {
				return;
			}
			var p = eventSource.point.GetGlobalPosition(eventSource.eventSourceNode);
			Handles.DrawLine(new Vector3(p.x, p.y, 0f), new Vector3(to.x, to.y, 0f));
		}
			
		/**
		 * Handle Node Event
		*/
		private void HandleNodeEvent (NodeEvent e) {

			switch (modifyMode) {
			/*
			 * During Mouse-drag opration to connect to other node
			 */
			case ModifyMode.CONNECTING: 
				switch (e.eventType) {
					/*
						connection established between 2 nodes
					*/
					case NodeEvent.EventType.EVENT_CONNECTION_ESTABLISHED: {
						// finish connecting mode.
						modifyMode = ModifyMode.NONE;
						
						if (currentEventSource == null) {
							break;
						}

						var sourceNode = currentEventSource.eventSourceNode;
						var sourceConnectionPoint = currentEventSource.point;
						
						var targetNode = e.eventSourceNode;
						var targetConnectionPoint = e.point;

						if (sourceNode.Id == targetNode.Id) {
							break;
						}

						if (!IsConnectablePointFromTo(sourceConnectionPoint, targetConnectionPoint)) {
							break;
						}

						var startNode = sourceNode;
						var startConnectionPoint = sourceConnectionPoint;
						var endNode = targetNode;
						var endConnectionPoint = targetConnectionPoint;

						// reverse if connected from input to output.
						if (sourceConnectionPoint.IsInput) {
							startNode = targetNode;
							startConnectionPoint = targetConnectionPoint;
							endNode = sourceNode;
							endConnectionPoint = sourceConnectionPoint;
						}

						var outputPoint = startConnectionPoint;
						var inputPoint = endConnectionPoint;							
						var label = startConnectionPoint.Label;

						// if two nodes are not supposed to connect, dismiss
						if(!Model.ConnectionData.CanConnect(startNode.Data, endNode.Data)) {
							break;
						}

						AddConnection(label, startNode, outputPoint, endNode, inputPoint);
						Setup();
						break;
					}

					/*
						connecting operation ended.
					*/
					case NodeEvent.EventType.EVENT_CONNECTING_END: {
						// finish connecting mode.
						modifyMode = ModifyMode.NONE;
						
						/*
							connect when dropped target is connectable from start connectionPoint.
						*/
						var node = FindNodeByPosition(e.globalMousePosition);
						if (node == null) {
							break;
						}
					
						// ignore if target node is source itself.
						if (node == e.eventSourceNode) {
							break;
						}

						var pointAtPosition = node.FindConnectionPointByPosition(e.globalMousePosition);
						if (pointAtPosition == null) {
							break;
						}

						var sourcePoint = currentEventSource.point;
						
						// limit by connectable or not.
						if(!IsConnectablePointFromTo(sourcePoint, pointAtPosition)) {
							break;
						}

						var isInput = currentEventSource.point.IsInput;
						var startNode = (isInput)? node : e.eventSourceNode;
						var endNode   = (isInput)? e.eventSourceNode: node;
						var startConnectionPoint = (isInput)? pointAtPosition : currentEventSource.point;
						var endConnectionPoint   = (isInput)? currentEventSource.point: pointAtPosition;
						var outputPoint = startConnectionPoint;
						var inputPoint = endConnectionPoint;							
						var label = startConnectionPoint.Label;

						// if two nodes are not supposed to connect, dismiss
						if(!Model.ConnectionData.CanConnect(startNode.Data, endNode.Data)) {
							break;
						}

						AddConnection(label, startNode, outputPoint, endNode, inputPoint);
						Setup();
						break;
					}

					default: {
						modifyMode = ModifyMode.NONE;
						break;
					}
				}
				break;
			/*
			 * 
			 */ 
			case ModifyMode.NONE:
				switch (e.eventType) {
				/*
					start connection handling.
				*/
				case NodeEvent.EventType.EVENT_CONNECTING_BEGIN: 
					modifyMode = ModifyMode.CONNECTING;
					currentEventSource = e;
					break;

				case NodeEvent.EventType.EVENT_NODE_DELETE: 
					DeleteSelected();
					break;

				/*
					node clicked.
				*/
				case NodeEvent.EventType.EVENT_NODE_CLICKED: {
					var clickedNode = e.eventSourceNode;

					if(activeSelection != null && activeSelection.nodes.Contains(clickedNode)) {
						break;
					}

					if (Event.current.shift) {
						Undo.RecordObject(this, "Toggle " + clickedNode.Name + " Selection");
						if(activeSelection == null) {
							activeSelection = new SavedSelection();
						}
						activeSelection.Toggle(clickedNode);
					} else {
						Undo.RecordObject(this, "Select " + clickedNode.Name);
						if(activeSelection == null) {
							activeSelection = new SavedSelection();
						}
						activeSelection.Clear(controller);
						activeSelection.Add(clickedNode);
					}
					
					UpdateActiveObjects(activeSelection);
					break;
				}
				case NodeEvent.EventType.EVENT_NODE_UPDATED: {
					break;
				}

				default: 
					break;
				}
				break;
			}

			switch (e.eventType) {
			case NodeEvent.EventType.EVENT_DELETE_ALL_CONNECTIONS_TO_POINT: {
				// deleting all connections to this point
				connections.RemoveAll( c => (c.InputPoint == e.point || c.OutputPoint == e.point) );
				Repaint();
				break;
			}
			case NodeEvent.EventType.EVENT_CONNECTIONPOINT_DELETED: {
				// deleting point is handled by caller, so we are deleting connections associated with it.
				connections.RemoveAll( c => (c.InputPoint == e.point || c.OutputPoint == e.point) );
				Repaint();
				break;
			}
			case NodeEvent.EventType.EVENT_CONNECTIONPOINT_LABELCHANGED: {
				// point label change is handled by caller, so we are changing label of connection associated with it.
				var affectingConnections = connections.FindAll( c=> c.OutputPoint.Id == e.point.Id );
				affectingConnections.ForEach(c => c.Label = e.point.Label);
				Repaint();
				break;
			}
			case NodeEvent.EventType.EVENT_NODE_UPDATED: {
				Validate(e.eventSourceNode);
				break;
			}

			case NodeEvent.EventType.EVENT_RECORDUNDO: {
				Undo.RecordObject(this, e.message);
				break;
			}
			case NodeEvent.EventType.EVENT_SAVE: 
				Setup();
				Repaint();
				break;
			}
		}

		private void HandleDragNodes() {

			Event evt = Event.current;
			int id = GUIUtility.GetControlID(kDragNodesControlID, FocusType.Passive);

			switch (evt.GetTypeForControl (id))
			{
			case EventType.MouseDown:
				if(modifyMode == ModifyMode.NONE) {
					if (evt.button == 0)
					{
						if(activeSelection != null && activeSelection.nodes.Count > 0) {
							bool mouseInSelectedNode = false;
							foreach(var n in activeSelection.nodes) {
								if(n.GetRect().Contains(evt.mousePosition)) {
									mouseInSelectedNode = true;
									break;
								}
							}

							if(mouseInSelectedNode) {
								modifyMode = ModifyMode.DRAGGING;
								m_LastMousePosition = evt.mousePosition;
								m_DragNodeDistance = Vector2.zero;

								foreach(var n in activeSelection.nodes) {
									m_InitialDragNodePositions[n] = n.GetPos();
								}

								GUIUtility.hotControl = id;
								evt.Use();
							}
						}
					}
				}
				break;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == id)
				{
					m_InitialDragNodePositions.Clear ();
					GUIUtility.hotControl = 0;
					modifyMode = ModifyMode.NONE;
					evt.Use ();
				}
				break;
			case EventType.MouseDrag:
				if (GUIUtility.hotControl == id)
				{
					m_DragNodeDistance += evt.mousePosition - m_LastMousePosition;
					m_LastMousePosition = evt.mousePosition;

					foreach(var n in activeSelection.nodes) {
						Vector2 newPosition = n.GetPos();
						Vector2 initialPosition = m_InitialDragNodePositions[n];
						newPosition.x = initialPosition.x + m_DragNodeDistance.x;
						newPosition.y = initialPosition.y + m_DragNodeDistance.y;
						n.SetPos(SnapPositionToGrid (newPosition));
					}
					evt.Use ();
				}
				break;
			}
		}

		protected static Vector2 SnapPositionToGrid (Vector2 position)
		{
			float gridSize = UserPreference.EditorWindowGridSize;
			
			int xCell = Mathf.RoundToInt (position.x / gridSize);
			int yCell = Mathf.RoundToInt (position.y / gridSize);

			position.x = xCell * gridSize;
			position.y = yCell * gridSize;

			return position;
		}

		private void UpdateSpacerRect () {
			var rightPoint = nodes.OrderByDescending(node => node.GetRightPos()).First().GetRightPos() + Model.Settings.WINDOW_SPAN;
			var bottomPoint = nodes.OrderByDescending(node => node.GetBottomPos()).First().GetBottomPos() + Model.Settings.WINDOW_SPAN;

			spacerRectRightBottom = new Vector2(rightPoint, bottomPoint);
		}
		
		public NodeGUI DuplicateNode (NodeGUI node, float offset) {
			var newNode = node.Duplicate(
				controller,
				node.GetX() + offset,
				node.GetY() + offset
			);
			AddNodeGUI(newNode);
            return newNode;
		}

        public void DuplicateConnection(ConnectionGUI con, Dictionary<NodeGUI, NodeGUI> nodeLookup) {

            var srcNodes = nodeLookup.Keys;

            var srcFrom = srcNodes.Where (n => n.Id == con.Data.FromNodeId).FirstOrDefault();
            var srcTo = srcNodes.Where (n => n.Id == con.Data.ToNodeId).FirstOrDefault();

            if (srcFrom == null || srcTo == null) {
                return;
            }

            var fromPointIndex = srcFrom.Data.OutputPoints.FindIndex (p => p.Id == con.Data.FromNodeConnectionPointId);
            var inPointIndex   = srcTo.Data.InputPoints.FindIndex (p => p.Id == con.Data.ToNodeConnectionPointId);

            if (fromPointIndex < 0 || inPointIndex < 0) {
                return;
            }

            var dstFrom = nodeLookup [srcFrom];
            var dstTo   = nodeLookup [srcTo];
            var dstFromPoint = dstFrom.Data.OutputPoints [fromPointIndex];
            var dstToPoint   = dstTo.Data.InputPoints [inPointIndex];

            AddConnection (con.Label, dstFrom, dstFromPoint, dstTo, dstToPoint);
        }

		private void AddNodeGUI(NodeGUI newNode) {

			int id = -1;

			foreach(var node in nodes) {
				if(node.WindowId > id) {
					id = node.WindowId;
				}
			}

			newNode.WindowId = id + 1;
				
			nodes.Add(newNode);
		}

		public void DeleteNode (string deletingNodeId) {
			var deletedNodeIndex = nodes.FindIndex(node => node.Id == deletingNodeId);
			if (0 <= deletedNodeIndex) {
				var n = nodes[deletedNodeIndex];
				n.SetActive(false);
				nodes.RemoveAt(deletedNodeIndex);
			}

		}

		public void HandleConnectionEvent (ConnectionEvent e) {
			switch (modifyMode) {
				case ModifyMode.NONE: {
					switch (e.eventType) {
						
						case ConnectionEvent.EventType.EVENT_CONNECTION_TAPPED: {

							if(Event.current.shift) {
								Undo.RecordObject(this, "Toggle Select Connection");
								if(activeSelection == null) {
									activeSelection = new SavedSelection();
								}
								activeSelection.Toggle(e.eventSourceCon);
								UpdateActiveObjects(activeSelection);
								break;
							} else {
								Undo.RecordObject(this, "Select Connection");
								if(activeSelection == null) {
									activeSelection = new SavedSelection();
								}
								activeSelection.Clear(controller);
								activeSelection.Add(e.eventSourceCon);
								UpdateActiveObjects(activeSelection);
								break;
							}
						}
						case ConnectionEvent.EventType.EVENT_CONNECTION_DELETED: {
							Undo.RecordObject(this, "Delete Connection");

							var deletedConnectionId = e.eventSourceCon.Id;

							DeleteConnection(deletedConnectionId);
							activeSelection.Clear(controller);
							UpdateActiveObjects(activeSelection);

							Setup();
							Repaint();
							break;
						}
						default: {
							break;
						}
					}
					break;
				}
			}
		}

		private void UpdateActiveObjects (SavedSelection selection) {

			foreach(var n in nodes) {
				n.SetActive( selection.nodes.Contains(n) );
			}

			foreach(var c in connections) {
				c.SetActive( selection.connections.Contains(c) );
			}
		}

		/**
			create new connection if same relationship is not exist yet.
		*/
		private void AddConnection (string label, NodeGUI startNode, Model.ConnectionPointData startPoint, NodeGUI endNode, Model.ConnectionPointData endPoint) {
			Undo.RecordObject(this, "Add Connection");

			var connectionsFromThisNode = connections
				.Where(con => con.OutputNodeId == startNode.Id)
				.Where(con => con.OutputPoint == startPoint)
				.ToList();
			if (connectionsFromThisNode.Any()) {
				var alreadyExistConnection = connectionsFromThisNode[0];
				DeleteConnection(alreadyExistConnection.Id);
				if(activeSelection != null) {
					activeSelection.Remove(alreadyExistConnection);
				}
			}

			if (!connections.ContainsConnection(startPoint, endPoint)) {
				connections.Add(ConnectionGUI.CreateConnection(label, startPoint, endPoint));
			}
		}

		private NodeGUI FindNodeByPosition (Vector2 globalPos) {
			return nodes.Find(n => n.Conitains(globalPos));
		}

		private bool IsConnectablePointFromTo (Model.ConnectionPointData sourcePoint, Model.ConnectionPointData destPoint) {
			if( sourcePoint.IsInput ) {
				return destPoint.IsOutput;
			} else {
				return destPoint.IsInput;
			}
		}

		private void DeleteConnection (string id) {
			var deletedConnectionIndex = connections.FindIndex(con => con.Id == id);
			if (0 <= deletedConnectionIndex) {
				var c = connections[deletedConnectionIndex];
				c.SetActive(false);
				connections.RemoveAt(deletedConnectionIndex);
			}
		}

		public int GetUnusedWindowId() {
			int highest = 0;
			nodes.ForEach((NodeGUI n) => { if(n.WindowId > highest) highest = n.WindowId; });
			return highest + 1;
		}
	}
}
