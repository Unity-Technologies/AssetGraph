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

			public void Clear(bool deactivate = false) {

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
		}

		public enum ScriptType : int {
			SCRIPT_MODIFIER,		
			SCRIPT_PREFABBUILDER,
			SCRIPT_POSTPROCESS
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
		private string lastLoaded;
		private Vector2 spacerRectRightBottom;
		private Vector2 scrollPos = new Vector2(1500,0);
		private Vector2 errorScrollPos = new Vector2(0,0);
		private Rect graphRegion = new Rect();
		private SelectPoint selectStartMousePosition;
		private GraphBackground background = new GraphBackground();
		private AssetBundleGraphController controller = new AssetBundleGraphController();

		private static AssetBundleGraphController s_currentController;
		private static BuildTarget s_selectedTarget;

		private Texture2D selectionTex {
			get{
				if(_selectionTex == null) {
					_selectionTex = LoadTextureFromFile(Model.Settings.GUI.RESOURCE_SELECTION);
				}
				return _selectionTex;
			}
		}

		private GUIContent reloadButtonTexture {
			get {
				if( _reloadButtonTexture == null ) {
					_reloadButtonTexture = EditorGUIUtility.IconContent("RotateTool");
				}
				return _reloadButtonTexture;
			}
		}

		public static void GenerateScript (ScriptType scriptType) {
			var destinationBasePath = Model.Settings.USERSPACE_PATH;

			var sourceFileName = string.Empty;
			var destinationFileName = string.Empty;

			switch (scriptType) {
			case ScriptType.SCRIPT_MODIFIER: 
				{
					sourceFileName = FileUtility.PathCombine(Model.Settings.SCRIPT_TEMPLATE_PATH, "MyModifier.cs.template");
					destinationFileName = "MyModifier{0}{1}";
					break;
				}
			case ScriptType.SCRIPT_PREFABBUILDER: {
					sourceFileName = FileUtility.PathCombine(Model.Settings.SCRIPT_TEMPLATE_PATH, "MyPrefabBuilder.cs.template");
					destinationFileName = "MyPrefabBuilder{0}{1}";
					break;
				}
			case ScriptType.SCRIPT_POSTPROCESS: {
					sourceFileName = FileUtility.PathCombine(Model.Settings.SCRIPT_TEMPLATE_PATH, "MyPostprocess.cs.template");
					destinationFileName = "MyPostprocess{0}{1}";
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

			FileUtility.CopyTemplateFile(sourceFileName, destinationPath, string.Format(destinationFileName, "", ""), string.Format(destinationFileName, count, ""));

			AssetDatabase.Refresh();

			//Highlight in ProjectView
			MonoScript s = AssetDatabase.LoadAssetAtPath<MonoScript>(destinationPath);
			UnityEngine.Assertions.Assert.IsNotNull(s);
			EditorGUIUtility.PingObject(s);
		}

		/*
			menu items
		*/
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
			
		[MenuItem(Model.Settings.GUI_TEXT_MENU_OPEN, false, 1)]
		public static void Open () {
			GetWindow<AssetBundleGraphEditorWindow>();
		}

		[MenuItem(Model.Settings.GUI_TEXT_MENU_BUILD, true, 1 + 11)]
		public static bool BuildFromMenuValidator () {
			// Calling GetWindow<>() will force open window
			// That's not what we want to do in validator function,
			// so just reference s_currentController directly
			return (s_currentController != null && !s_currentController.IsAnyIssueFound);
		}

		[MenuItem(Model.Settings.GUI_TEXT_MENU_BUILD, false, 1 + 11)]
		public static void BuildFromMenu () {
			var window = GetWindow<AssetBundleGraphEditorWindow>();
			window.SaveGraph();
			window.Run(ActiveBuildTarget);
		}

		[MenuItem(Model.Settings.GUI_TEXT_MENU_DELETE_CACHE)] public static void DeleteCache () {
			FileUtility.RemakeDirectory(Model.Settings.APPLICATIONDATAPATH_CACHE_PATH);

			AssetDatabase.Refresh();
		}

		[MenuItem(Model.Settings.GUI_TEXT_MENU_DELETE_IMPORTSETTING_SETTINGS)] public static void DeleteImportSettingSample () {
			FileUtility.RemakeDirectory(Model.Settings.IMPORTER_SETTINGS_PLACE);

			AssetDatabase.Refresh();
		}

		public static BuildTarget ActiveBuildTarget {
			get {
				return s_selectedTarget;
			}
		}

		public void OnFocus () {
			// update handlers. these static handlers are erase when window is full-screened and badk to normal window.
			modifyMode = ModifyMode.NONE;
			NodeGUIUtility.NodeEventHandler = HandleNodeEvent;
			ConnectionGUIUtility.ConnectionEventHandler = HandleConnectionEvent;
		}

		public void OnLostFocus() {
			modifyMode = ModifyMode.NONE;
		}

		public void OnProjectChange() {
			Repaint();
		}

		public void SelectNode(string nodeId) {
			var selectObject = nodes.Find(node => node.Id == nodeId);
			foreach (var node in nodes) {
				node.SetActive( node == selectObject );
			}
		}

		private void Init() {

			s_currentController = this.controller;
			s_selectedTarget    = EditorUserBuildSettings.activeBuildTarget;
			LogUtility.Logger.filterLogType = LogType.Warning;

			this.titleContent = new GUIContent("AssetBundle");

			Model.SaveData.Reload();

			Undo.undoRedoPerformed += () => {
				Setup(ActiveBuildTarget);
				Repaint();
			};

			modifyMode = ModifyMode.NONE;
			NodeGUIUtility.NodeEventHandler = HandleNodeEvent;
			ConnectionGUIUtility.ConnectionEventHandler = HandleConnectionEvent;

			InitializeGraph();
			Setup(ActiveBuildTarget);

			if (nodes.Any()) {
				UpdateSpacerRect();
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
			
		public static Texture2D LoadTextureFromFile(string path) {
            Texture2D texture = new Texture2D(1, 1);
            texture.LoadImage(File.ReadAllBytes(path));
            return texture;
        }

		/**
			node graph initializer.
			setup nodes, points and connections from saved data.
		*/
		public void InitializeGraph () {

			/*
				do nothing if json does not modified after first load.
			*/
			if (Model.SaveData.Data.LastModified == lastLoaded) {
				return;
			}
				
			lastLoaded = Model.SaveData.Data.LastModified;

			minSize = new Vector2(600f, 300f);
			
			wantsMouseMove = true;
			modifyMode = ModifyMode.NONE;
						
			
			/*
				load graph data from deserialized data.
			*/
			ConstructGraphFromSaveData(out this.nodes, out this.connections);
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
		private static void ConstructGraphFromSaveData (out List<NodeGUI> nodes, out List<ConnectionGUI> connections) {
			var saveData = Model.SaveData.Data;
			var currentNodes = new List<NodeGUI>();
			var currentConnections = new List<ConnectionGUI>();

			foreach (var node in saveData.Nodes) {
				var newNodeGUI = new NodeGUI(node);
				newNodeGUI.WindowId = GetSafeWindowId(currentNodes);
				currentNodes.Add(newNodeGUI);
			}

			// load connections
			foreach (var c in saveData.Connections) {
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
			Model.SaveData.Data.ApplyGraph(nodes, connections);
		}

		/**
		 * Save Graph and update all nodes & connections
		 */ 
		private void Setup (BuildTarget target, bool forceVisitAll = false) {

			EditorUtility.ClearProgressBar();

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

		private void Validate (BuildTarget target, NodeGUI node) {

			EditorUtility.ClearProgressBar();


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
		private void Run (BuildTarget target) {

			try {
				AssetDatabase.SaveAssets();

				List<NodeGUI> currentNodes = null;
				List<ConnectionGUI> currentConnections = null;

				ConstructGraphFromSaveData(out currentNodes, out currentConnections);

				float currentCount = 0f;
				float totalCount = (float)currentNodes.Count;
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

		public static IEnumerable<Dictionary<string, List<AssetReference>>> EnumurateIncomingAssetGroups(Model.ConnectionPointData inputPoint) {
			if(s_currentController != null) {
				return s_currentController.StreamManager.EnumurateIncomingAssetGroups(inputPoint);
			}
			return null;
		}

		public static void OnAssetsReimported(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
			if(s_currentController != null) {
				s_currentController.OnAssetsReimported(s_selectedTarget, importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
			}
		}

		private void DrawGUIToolBar() {
			bool performBuild = false;

			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar)) {
				if (GUILayout.Button(new GUIContent("Refresh", reloadButtonTexture.image, "Refresh and reload"), EditorStyles.toolbarButton, GUILayout.Width(80), GUILayout.Height(Model.Settings.GUI.TOOLBAR_HEIGHT))) {
					Setup(ActiveBuildTarget);
				}
				showErrors = GUILayout.Toggle(showErrors, "Show Error", EditorStyles.toolbarButton, GUILayout.Height(Model.Settings.GUI.TOOLBAR_HEIGHT));

				GUILayout.Space(4);

				showVerboseLog = GUILayout.Toggle(showVerboseLog, "Show Verbose Log", EditorStyles.toolbarButton, GUILayout.Height(Model.Settings.GUI.TOOLBAR_HEIGHT));
				LogUtility.Logger.filterLogType = (showVerboseLog)? LogType.Log : LogType.Warning;

				GUILayout.FlexibleSpace();

				if(controller.IsAnyIssueFound) {
					GUIStyle errorStyle = new GUIStyle("ErrorLabel");
					errorStyle.alignment = TextAnchor.MiddleCenter;
					GUILayout.Label("All errors needs to be fixed before building", errorStyle);
					GUILayout.FlexibleSpace();
				}

				GUIStyle tbLabel = new GUIStyle(EditorStyles.toolbar);

				tbLabel.alignment = TextAnchor.MiddleCenter;

				GUIStyle tbLabelTarget = new GUIStyle(tbLabel);
				tbLabelTarget.fontStyle = FontStyle.Bold;

				GUILayout.Label("Platform:", tbLabel, GUILayout.Height(Model.Settings.GUI.TOOLBAR_HEIGHT));

				var supportedTargets = NodeGUIUtility.SupportedBuildTargets;
				int currentIndex = Mathf.Max(0, supportedTargets.FindIndex(t => t == s_selectedTarget));

				int newIndex = EditorGUILayout.Popup(currentIndex, NodeGUIUtility.supportedBuildTargetNames, 
					EditorStyles.toolbarButton, GUILayout.Width(150), GUILayout.Height(Model.Settings.GUI.TOOLBAR_HEIGHT));

				if(newIndex != currentIndex) {
					s_selectedTarget = supportedTargets[newIndex];
					Setup(ActiveBuildTarget, true);
				}

				using(new EditorGUI.DisabledScope(controller.IsAnyIssueFound)) {
					performBuild = GUILayout.Button("Build", EditorStyles.toolbarButton, GUILayout.Height(Model.Settings.GUI.TOOLBAR_HEIGHT));
				}

			}		

			// Workaround:
			// Calling time taking procedure such as asset bundle build inside Scope object 
			// may throw Exception becuase object state is already invalid by the time to Dispose.
			if(performBuild) {
				EditorApplication.ExecuteMenuItem(Model.Settings.GUI_TEXT_MENU_BUILD);
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

				// draw node window x N.
				{
					BeginWindows();

					nodes.ForEach(node => node.DrawNode());

					EndWindows();
				}

				// draw connection input point marks.
				foreach (var node in nodes) {
					node.DrawConnectionInputPointMark(currentEventSource, modifyMode == ModifyMode.CONNECTING);
				}

				// draw connections.
				foreach (var con in connections) {
					con.DrawConnection(nodes, controller.StreamManager.FindAssetGroup(con.Id));
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
						GUI.DrawTexture(new Rect(selectStartMousePosition.x, selectStartMousePosition.y, Event.current.mousePosition.x - selectStartMousePosition.x, Event.current.mousePosition.y - selectStartMousePosition.y), selectionTex);
						break;
					}
				}

				// handle Graph GUI events
				HandleGraphGUIEvents();

				// set rect for scroll.
				if (nodes.Any()) {
					GUILayoutUtility.GetRect(new GUIContent(string.Empty), GUIStyle.none, GUILayout.Width(spacerRectRightBottom.x), GUILayout.Height(spacerRectRightBottom.y));
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
									if(graphRegion.Contains(Event.current.mousePosition)) {
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
								activeSelection.Clear();
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

		public void OnEnable () {
			Init();
		}

		public void OnDisable() {
			LogUtility.Logger.Log("OnDisable");
			Model.SaveData.SetSavedataDirty();
		}

		public void OnGUI () {
			DrawGUIToolBar();

			using (new EditorGUILayout.HorizontalScope()) {
				DrawGUINodeGraph();
				if(showErrors) {
					DrawGUINodeErrors();
				}
			}

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
					var rightClickPos = Event.current.mousePosition;
					var menu = new GenericMenu();
					var customNodes = NodeUtility.CustomNodeTypes;
					for(int i = 0; i < customNodes.Count; ++i) {
						// workaround: avoiding compilier closure bug
						var index = i;
						menu.AddItem(
							new GUIContent(string.Format("Create {0} Node", customNodes[i].node.Name)),
							false, 
							() => {
								AddNodeFromGUI(customNodes[index].CreateInstance(), customNodes[index].node.Name, rightClickPos.x, rightClickPos.y);
								Setup(ActiveBuildTarget);
								Repaint();
							}
						);
					}

					menu.ShowAsContext();
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

						activeSelection.Clear();
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
							Undo.RecordObject(this, "Delete Selection");

							foreach (var n in activeSelection.nodes) {
								DeleteNode(n.Id);
							}

							foreach (var c in activeSelection.connections) {
								DeleteConnection(c.Id);
							}

							activeSelection.Clear();
							UpdateActiveObjects(activeSelection);

							Setup(ActiveBuildTarget);

							Event.current.Use();
							break;
						}

						case "Copy": {
							if (!isValidSelection) {
								break;
							}

							Undo.RecordObject(this, "Copy Selection");

							copiedSelection = new SavedSelection(activeSelection);

							Event.current.Use();
							break;
						}

						case "Cut": {
							if (!isValidSelection) {
								break;
							}

							Undo.RecordObject(this, "Cut Selection");

							copiedSelection = new SavedSelection(activeSelection);

							foreach (var n in activeSelection.nodes) {
								DeleteNode(n.Id);
							}

							foreach (var c in activeSelection.connections) {
								DeleteConnection(c.Id);
							}
							activeSelection.Clear();
							UpdateActiveObjects(activeSelection);

							Setup(ActiveBuildTarget);
							InitializeGraph();

							Event.current.Use();
							break;
						}

						case "Paste": {
							if(!isValidCopy)  {
								break;
							}

							Undo.RecordObject(this, "Paste");
							foreach (var newNode in copiedSelection.nodes) {
								DuplicateNode(newNode, copiedSelection.PasteOffset);
							}
							copiedSelection.IncrementPasteOffset();

							Setup(ActiveBuildTarget);
							InitializeGraph();

							Event.current.Use();
							break;
						}

						case "SelectAll": {
							Undo.RecordObject(this, "Select All Objects");

							if(activeSelection == null) {
								activeSelection = new SavedSelection();
							}

							activeSelection.Clear();
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

		private void AddNodeFromGUI (Node n, string guiName, float x, float y) {

			string nodeName = string.Format("New {0} Node", guiName);
			NodeGUI newNode = new NodeGUI(new Model.NodeData(nodeName, n, x, y));

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
					case NodeEvent.EventType.EVENT_NODE_MOVING: {
						break;
					}

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
						Setup(ActiveBuildTarget);
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
						Setup(ActiveBuildTarget);
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
				case NodeEvent.EventType.EVENT_NODE_MOVING: 
					if (activeSelection != null && activeSelection.nodes.Contains(e.eventSourceNode)) 
					{
						var moveDistance = e.position;

						foreach(var n in activeSelection.nodes) {
							// skipping eventSourceNode because the movement is already done by GUI.DragWindow()
							if(n != e.eventSourceNode) {
								n.MoveBy(moveDistance);
							}
						}
					}
					break;

				/*
					start connection handling.
				*/
				case NodeEvent.EventType.EVENT_CONNECTING_BEGIN: 
					modifyMode = ModifyMode.CONNECTING;
					currentEventSource = e;
					break;

				case NodeEvent.EventType.EVENT_NODE_DELETE: 
					
					Undo.RecordObject(this, "Delete Node");
					
					var deletingNodeId = e.eventSourceNode.Id;
					DeleteNode(deletingNodeId);
					if(activeSelection != null) {
						activeSelection.Remove(e.eventSourceNode);
					}

					Setup(ActiveBuildTarget);
					InitializeGraph();
					break;

				/*
					node clicked.
				*/
				case NodeEvent.EventType.EVENT_NODE_CLICKED: {
					var clickedNode = e.eventSourceNode;

					Model.SaveData.SetSavedataDirty();

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
						activeSelection.Clear();
						activeSelection.Add(clickedNode);
					}
					
					UpdateActiveObjects(activeSelection);
					UpdateSpacerRect();
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
				Validate(ActiveBuildTarget, e.eventSourceNode);
				break;
			}

			case NodeEvent.EventType.EVENT_RECORDUNDO: {
				Undo.RecordObject(this, e.message);
				break;
			}
			case NodeEvent.EventType.EVENT_SAVE: 
				Setup(ActiveBuildTarget);
				Repaint();
				break;
			}
		}

		/**
			once expand, keep max size.
			it's convenience.
		*/
		private void UpdateSpacerRect () {
			var rightPoint = nodes.OrderByDescending(node => node.GetRightPos()).Select(node => node.GetRightPos()).ToList()[0] + Model.Settings.WINDOW_SPAN;
			if (rightPoint < spacerRectRightBottom.x) rightPoint = spacerRectRightBottom.x;

			var bottomPoint = nodes.OrderByDescending(node => node.GetBottomPos()).Select(node => node.GetBottomPos()).ToList()[0] + Model.Settings.WINDOW_SPAN;
			if (bottomPoint < spacerRectRightBottom.y) bottomPoint = spacerRectRightBottom.y;

			spacerRectRightBottom = new Vector2(rightPoint, bottomPoint);
		}
		
		public void DuplicateNode (NodeGUI node, float offset) {
			var newNode = node.Duplicate(
				node.GetX() + offset,
				node.GetY() + offset
			);
			AddNodeGUI(newNode);
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
								activeSelection.Clear();
								activeSelection.Add(e.eventSourceCon);
								UpdateActiveObjects(activeSelection);
								break;
							}
						}
						case ConnectionEvent.EventType.EVENT_CONNECTION_DELETED: {
							Undo.RecordObject(this, "Delete Connection");

							var deletedConnectionId = e.eventSourceCon.Id;

							DeleteConnection(deletedConnectionId);
							activeSelection.Clear();
							UpdateActiveObjects(activeSelection);

							Setup(ActiveBuildTarget);
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
