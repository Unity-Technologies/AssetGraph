using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace AssetBundleGraph {
	public class AssetBundleGraphEditorWindow : EditorWindow {

		[Serializable] 
		public struct KeyObject {
			public string key;

			public KeyObject (string val) {
				key = val;
			}
		}

		[Serializable] 
		public struct ActiveObject {
			[SerializeField] public SerializableVector2Dictionary idPosDict;

			public ActiveObject (Dictionary<string, Vector2> idPosDict) {
				this.idPosDict = new SerializableVector2Dictionary(idPosDict);
			}
		}

		[Serializable] 
		public struct CopyField {
			[SerializeField] public List<string> datas;
			[SerializeField] public CopyType type;

			public CopyField (List<string> datas, CopyType type) {
				this.datas = datas;
				this.type = type;
			}
		}

		// hold selection start data.
		public struct AssetBundleGraphSelection {
			public readonly float x;
			public readonly float y;

			public AssetBundleGraphSelection (Vector2 position) {
				this.x = position.x;
				this.y = position.y;
			}
		}

		// hold scale start data.
		public struct ScalePoint {
			public readonly float x;
			public readonly float y;
			public readonly float startScale;
			public readonly int scaledDistance;

			public ScalePoint (Vector2 point, float scaleFactor, int scaledDistance) {
				this.x = point.x;
				this.y = point.y;
				this.startScale = scaleFactor;
				this.scaledDistance = scaledDistance;
			}
		}

		public enum ModifyMode : int {
			NONE,
			CONNECTING,
			SELECTING,
			SCALING,
		}

		public enum CopyType : int {
			COPYTYPE_COPY,
			COPYTYPE_CUT
		}

		public enum ScriptType : int {
			SCRIPT_MODIFIER,		
			SCRIPT_PREFABBUILDER,
			SCRIPT_POSTPROCESS
		}


		[SerializeField] private List<NodeGUI> nodes = new List<NodeGUI>();
		[SerializeField] private List<ConnectionGUI> connections = new List<ConnectionGUI>();
		[SerializeField] private ActiveObject activeObject = new ActiveObject(new Dictionary<string, Vector2>());

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
		private CopyField copyField = new CopyField();		
		private AssetBundleGraphSelection selection;
		private ScalePoint scalePoint;
		private GraphBackground background = new GraphBackground();
		private AssetBundleGraphController controller = new AssetBundleGraphController();

		private static AssetBundleGraphController s_currentController;
		private static BuildTarget s_selectedTarget;

		private Texture2D selectionTex {
			get{
				if(_selectionTex == null) {
					_selectionTex = LoadTextureFromFile(AssetBundleGraphSettings.GUI.RESOURCE_SELECTION);
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
			var destinationBasePath = AssetBundleGraphSettings.USERSPACE_PATH;

			var sourceFileName = string.Empty;
			var destinationFileName = string.Empty;

			switch (scriptType) {
			case ScriptType.SCRIPT_MODIFIER: 
				{
					sourceFileName = FileUtility.PathCombine(AssetBundleGraphSettings.SCRIPT_TEMPLATE_PATH, "MyModifier.cs.template");
					destinationFileName = "MyModifier{0}{1}";
					break;
				}
			case ScriptType.SCRIPT_PREFABBUILDER: {
					sourceFileName = FileUtility.PathCombine(AssetBundleGraphSettings.SCRIPT_TEMPLATE_PATH, "MyPrefabBuilder.cs.template");
					destinationFileName = "MyPrefabBuilder{0}{1}";
					break;
				}
			case ScriptType.SCRIPT_POSTPROCESS: {
					sourceFileName = FileUtility.PathCombine(AssetBundleGraphSettings.SCRIPT_TEMPLATE_PATH, "MyPostprocess.cs.template");
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
		[MenuItem(AssetBundleGraphSettings.GUI_TEXT_MENU_GENERATE_MODIFIER)]
		public static void GenerateModifier () {
			GenerateScript(ScriptType.SCRIPT_MODIFIER);
		}
		[MenuItem(AssetBundleGraphSettings.GUI_TEXT_MENU_GENERATE_PREFABBUILDER)]
		public static void GeneratePrefabBuilder () {
			GenerateScript(ScriptType.SCRIPT_PREFABBUILDER);
		}
		[MenuItem(AssetBundleGraphSettings.GUI_TEXT_MENU_GENERATE_POSTPROCESS)]
		public static void GeneratePostprocess () {
			GenerateScript(ScriptType.SCRIPT_POSTPROCESS);
		}
			
		[MenuItem(AssetBundleGraphSettings.GUI_TEXT_MENU_OPEN, false, 1)]
		public static void Open () {
			GetWindow<AssetBundleGraphEditorWindow>();
		}

		[MenuItem(AssetBundleGraphSettings.GUI_TEXT_MENU_BUILD, true, 1 + 11)]
		public static bool BuildFromMenuValidator () {
			// Calling GetWindow<>() will force open window
			// That's not what we want to do in validator function,
			// so just reference s_currentController directly
			return (s_currentController != null && !s_currentController.IsAnyIssueFound);
		}

		[MenuItem(AssetBundleGraphSettings.GUI_TEXT_MENU_BUILD, false, 1 + 11)]
		public static void BuildFromMenu () {
			var window = GetWindow<AssetBundleGraphEditorWindow>();
			window.SaveGraph();
			window.Run(ActiveBuildTarget);
		}

		[MenuItem(AssetBundleGraphSettings.GUI_TEXT_MENU_DELETE_CACHE)] public static void DeleteCache () {
			FileUtility.RemakeDirectory(AssetBundleGraphSettings.APPLICATIONDATAPATH_CACHE_PATH);

			AssetDatabase.Refresh();
		}

		[MenuItem(AssetBundleGraphSettings.GUI_TEXT_MENU_DELETE_IMPORTSETTING_SETTINGS)] public static void DeleteImportSettingSample () {
			FileUtility.RemakeDirectory(AssetBundleGraphSettings.IMPORTER_SETTINGS_PLACE);

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
			// set deactive for all nodes.
			foreach (var node in nodes) {
				node.SetInactive();
			}
			if(selectObject != null) {
				selectObject.SetActive();
			}
		}

		private void Init() {

			s_currentController = this.controller;
			s_selectedTarget    = EditorUserBuildSettings.activeBuildTarget;
			LogUtility.Logger.filterLogType = LogType.Warning;

			this.titleContent = new GUIContent("AssetBundle");

			SaveData.Reload();

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

		private ActiveObject RenewActiveObject (List<string> ids) {
			var idPosDict = new Dictionary<string, Vector2>();
			foreach (var node in nodes) {
				if (ids.Contains(node.Id)) idPosDict[node.Id] = node.GetPos();
			}
			foreach (var connection in connections) {
				if (ids.Contains(connection.Id)) idPosDict[connection.Id] = Vector2.zero;
			}
			return new ActiveObject(idPosDict);
		}

		/**
			node graph initializer.
			setup nodes, points and connections from saved data.
		*/
		public void InitializeGraph () {

			/*
				do nothing if json does not modified after first load.
			*/
			if (SaveData.Data.LastModified == lastLoaded) {
				return;
			}
				
			lastLoaded = SaveData.Data.LastModified;

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
			var saveData = SaveData.Data;
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
			SaveData.Data.ApplyGraph(nodes, connections);
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
				NodeData lastNode = null;

				Action<NodeData, string, float> updateHandler = (node, message, progress) => {

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

		public static IEnumerable<Dictionary<string, List<AssetReference>>> EnumurateIncomingAssetGroups(ConnectionPointData inputPoint) {
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
				if (GUILayout.Button(new GUIContent("Refresh", reloadButtonTexture.image, "Refresh and reload"), EditorStyles.toolbarButton, GUILayout.Width(80), GUILayout.Height(AssetBundleGraphSettings.GUI.TOOLBAR_HEIGHT))) {
					Setup(ActiveBuildTarget);
				}
				showErrors = GUILayout.Toggle(showErrors, "Show Error", EditorStyles.toolbarButton, GUILayout.Height(AssetBundleGraphSettings.GUI.TOOLBAR_HEIGHT));

				GUILayout.Space(4);

				showVerboseLog = GUILayout.Toggle(showVerboseLog, "Show Verbose Log", EditorStyles.toolbarButton, GUILayout.Height(AssetBundleGraphSettings.GUI.TOOLBAR_HEIGHT));
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

				GUILayout.Label("Platform:", tbLabel, GUILayout.Height(AssetBundleGraphSettings.GUI.TOOLBAR_HEIGHT));

				var supportedTargets = NodeGUIUtility.SupportedBuildTargets;
				int currentIndex = Mathf.Max(0, supportedTargets.FindIndex(t => t == s_selectedTarget));

				int newIndex = EditorGUILayout.Popup(currentIndex, NodeGUIUtility.supportedBuildTargetNames, 
					EditorStyles.toolbarButton, GUILayout.Width(150), GUILayout.Height(AssetBundleGraphSettings.GUI.TOOLBAR_HEIGHT));

				if(newIndex != currentIndex) {
					s_selectedTarget = supportedTargets[newIndex];
					Setup(ActiveBuildTarget, true);
				}

				using(new EditorGUI.DisabledScope(controller.IsAnyIssueFound)) {
					performBuild = GUILayout.Button("Build", EditorStyles.toolbarButton, GUILayout.Height(AssetBundleGraphSettings.GUI.TOOLBAR_HEIGHT));
				}

			}		

			// Workaround:
			// Calling time taking procedure such as asset bundle build inside Scope object 
			// may throw Exception becuase object state is already invalid by the time to Dispose.
			if(performBuild) {
				EditorApplication.ExecuteMenuItem(AssetBundleGraphSettings.GUI_TEXT_MENU_BUILD);
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
						GUI.DrawTexture(new Rect(selection.x, selection.y, Event.current.mousePosition.x - selection.x, Event.current.mousePosition.y - selection.y), selectionTex);
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
									if (Event.current.command) {
										scalePoint = new ScalePoint(Event.current.mousePosition, NodeGUI.scaleFactor, 0);
										modifyMode = ModifyMode.SCALING;
										break;
									}

									selection = new AssetBundleGraphSelection(Event.current.mousePosition);
									modifyMode = ModifyMode.SELECTING;
									break;
								}
							case 2:{// middle click.
									scalePoint = new ScalePoint(Event.current.mousePosition, NodeGUI.scaleFactor, 0);
									modifyMode = ModifyMode.SCALING;
									break;
								}
							}
							break;
						}
					case ModifyMode.SELECTING: {
							// do nothing.
							break;
						}
					case ModifyMode.SCALING: {
							var baseDistance = (int)Vector2.Distance(Event.current.mousePosition, new Vector2(scalePoint.x, scalePoint.y));
							var distance = baseDistance / NodeGUI.SCALE_WIDTH;
							var direction = (0 < Event.current.mousePosition.y - scalePoint.y);

							if (!direction) distance = -distance;

							// var before = NodeGUI.scaleFactor;
							NodeGUI.scaleFactor = scalePoint.startScale + (distance * NodeGUI.SCALE_RATIO);

							if (NodeGUI.scaleFactor < NodeGUI.SCALE_MIN) NodeGUI.scaleFactor = NodeGUI.SCALE_MIN;
							if (NodeGUI.SCALE_MAX < NodeGUI.scaleFactor) NodeGUI.scaleFactor = NodeGUI.SCALE_MAX;
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
							var x = 0f;
							var y = 0f;
							var width = 0f;
							var height = 0f;

							if (Event.current.mousePosition.x < selection.x) {
								x = Event.current.mousePosition.x;
								width = selection.x - Event.current.mousePosition.x;
							}
							if (selection.x < Event.current.mousePosition.x) {
								x = selection.x;
								width = Event.current.mousePosition.x - selection.x;
							}

							if (Event.current.mousePosition.y < selection.y) {
								y = Event.current.mousePosition.y;
								height = selection.y - Event.current.mousePosition.y;
							}
							if (selection.y < Event.current.mousePosition.y) {
								y = selection.y;
								height = Event.current.mousePosition.y - selection.y;
							}


							var activeObjectIds = new List<string>();

							var selectedRect = new Rect(x, y, width, height);


							foreach (var node in nodes) {
								var nodeRect = new Rect(node.GetRect());
								nodeRect.x = nodeRect.x * NodeGUI.scaleFactor;
								nodeRect.y = nodeRect.y * NodeGUI.scaleFactor;
								nodeRect.width = nodeRect.width * NodeGUI.scaleFactor;
								nodeRect.height = nodeRect.height * NodeGUI.scaleFactor;
								// get containd nodes,
								if (nodeRect.Overlaps(selectedRect)) {
									activeObjectIds.Add(node.Id);
								}
							}

							foreach (var connection in connections) {
								// get contained connection badge.
								if (connection.GetRect().Overlaps(selectedRect)) {
									activeObjectIds.Add(connection.Id);
								}
							}

							if (Event.current.shift) {
								// add current active object ids to new list.
								foreach (var alreadySelectedObjectId in activeObject.idPosDict.ReadonlyDict().Keys) {
									if (!activeObjectIds.Contains(alreadySelectedObjectId)) activeObjectIds.Add(alreadySelectedObjectId);
								}
							} else {
								// do nothing, means cancel selections if nodes are not contained by selection.
							}


							Undo.RecordObject(this, "Select Objects");

							activeObject = RenewActiveObject(activeObjectIds);
							UpdateActivationOfObjects(activeObject);

							selection = new AssetBundleGraphSelection(Vector2.zero);
							modifyMode = ModifyMode.NONE;

							HandleUtility.Repaint();
							Event.current.Use();
							break;
						}

					case ModifyMode.SCALING: {
							modifyMode = ModifyMode.NONE;
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
			SaveData.SetSavedataDirty();
		}

		public void OnGUI () {
			DrawGUIToolBar();

			using (new EditorGUILayout.HorizontalScope()) {
				DrawGUINodeGraph();
				if(showErrors) {
					DrawGUINodeErrors();
				}
			}

			/*
				Event Handling:
				- Supporting dragging script into window to create node.
				- Context Menu	
				- NodeGUI connection.
				- Command(Delete, Copy, etc...)
			*/
			switch (Event.current.type) {
				// detect dragging script then change interface to "(+)" icon.
				case EventType.DragUpdated: {
					var refs = DragAndDrop.objectReferences;

					foreach (var refe in refs) {
						if (refe.GetType() == typeof(UnityEditor.MonoScript)) {
							Type scriptTypeInfo = ((MonoScript)refe).GetClass();							
							Type inheritedTypeInfo = GetDragAndDropAcceptableScriptType(scriptTypeInfo);

							if (inheritedTypeInfo != null) {
								// at least one asset is script. change interface.
								DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
								break;
							}
						}
					}
					break;
				}

				// script drop on editor.
				case EventType.DragPerform: {
					var pathAndRefs = new Dictionary<string, object>();
					for (var i = 0; i < DragAndDrop.paths.Length; i++) {
						var path = DragAndDrop.paths[i];
						var refe = DragAndDrop.objectReferences[i];
						pathAndRefs[path] = refe;
					}
					var shouldSave = false;
					foreach (var item in pathAndRefs) {
						var refe = (MonoScript)item.Value;
						if (refe.GetType() == typeof(UnityEditor.MonoScript)) {
							Type scriptTypeInfo = refe.GetClass();
							Type inheritedTypeInfo = GetDragAndDropAcceptableScriptType(scriptTypeInfo);

							if (inheritedTypeInfo != null) {
								var dropPos = Event.current.mousePosition;
								var scriptName = refe.name;
								var scriptClassName = scriptName;
								AddNodeFromCode(scriptName, scriptClassName, inheritedTypeInfo, dropPos.x, dropPos.y);
								shouldSave = true;
							}
						}
					}

					if (shouldSave) {
						Setup(ActiveBuildTarget);
					}
					break;
				}

				// show context menu
				case EventType.ContextClick: {
					var rightClickPos = Event.current.mousePosition;
					var menu = new GenericMenu();
					foreach (var menuItemStr in AssetBundleGraphSettings.GUI_Menu_Item_TargetGUINodeDict.Keys) {
						var kind = AssetBundleGraphSettings.GUI_Menu_Item_TargetGUINodeDict[menuItemStr];
						menu.AddItem(
							new GUIContent(menuItemStr),
							false, 
							() => {
								AddNodeFromGUI(kind, rightClickPos.x, rightClickPos.y);
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
					
					if (activeObject.idPosDict.ReadonlyDict().Any()) {
						Undo.RecordObject(this, "Unselect");

						foreach (var activeObjectId in activeObject.idPosDict.ReadonlyDict().Keys) {
							// unselect all.
							foreach (var node in nodes) {
								if (activeObjectId == node.Id) {
									node.SetInactive();
								}
							}
							foreach (var connection in connections) {
								if (activeObjectId == connection.Id) {
									connection.SetInactive();
								}
							}
						}

						activeObject = RenewActiveObject(new List<string>());

					}

					// clear inspector
					if( Selection.activeObject is NodeGUIInspectorHelper || Selection.activeObject is ConnectionGUIInspectorHelper) {
						Selection.activeObject = null;
					}

					break;
				}

				/*
					scale up or down by command & + or command & -.
				*/
				case EventType.KeyDown: {
					if (Event.current.command) {
						if (Event.current.shift && Event.current.keyCode == KeyCode.Semicolon) {
							NodeGUI.scaleFactor = NodeGUI.scaleFactor + 0.1f;
							if (NodeGUI.scaleFactor < NodeGUI.SCALE_MIN) NodeGUI.scaleFactor = NodeGUI.SCALE_MIN;
							if (NodeGUI.SCALE_MAX < NodeGUI.scaleFactor) NodeGUI.scaleFactor = NodeGUI.SCALE_MAX;
							Event.current.Use();
							break;
						}

						if (Event.current.keyCode == KeyCode.Minus) {
							NodeGUI.scaleFactor = NodeGUI.scaleFactor - 0.1f;
							if (NodeGUI.scaleFactor < NodeGUI.SCALE_MIN) NodeGUI.scaleFactor = NodeGUI.SCALE_MIN;
							if (NodeGUI.SCALE_MAX < NodeGUI.scaleFactor) NodeGUI.scaleFactor = NodeGUI.SCALE_MAX;
							Event.current.Use();
							break;
						}
					}
					break;
				}

				case EventType.ValidateCommand: 
				{
					switch (Event.current.commandName) {
					// Delete active node or connection.
					case "Delete": {
							if (activeObject.idPosDict.ReadonlyDict().Any()) {
								Event.current.Use();
							}
							break;
						}

					case "Copy": {
							if (activeObject.idPosDict.ReadonlyDict().Any()) {
								Event.current.Use();
							}
							break;
						}

					case "Cut": {
							if (activeObject.idPosDict.ReadonlyDict().Any()) {
								Event.current.Use();
							}
							break;
						}

					case "Paste": {
							if(copyField.datas == null)  {
								break;
							}

							if (copyField.datas.Any()) {
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

							if (!activeObject.idPosDict.ReadonlyDict().Any()) break;
							Undo.RecordObject(this, "Delete Selection");

							foreach (var targetId in activeObject.idPosDict.ReadonlyDict().Keys) {
								DeleteNode(targetId);
								DeleteConnectionById(targetId);
							}

							Setup(ActiveBuildTarget);

							activeObject = RenewActiveObject(new List<string>());
							UpdateActivationOfObjects(activeObject);

							Event.current.Use();
							break;
						}

						case "Copy": {
							if (!activeObject.idPosDict.ReadonlyDict().Any()) {
								break;
							}

							Undo.RecordObject(this, "Copy Selection");

							var targetNodeIds = activeObject.idPosDict.ReadonlyDict().Keys.ToList();
							var targetNodeJsonRepresentations = JsonRepresentations(targetNodeIds);
							copyField = new CopyField(targetNodeJsonRepresentations, CopyType.COPYTYPE_COPY);

							Event.current.Use();
							break;
						}

						case "Cut": {
							if (!activeObject.idPosDict.ReadonlyDict().Any()) {
								break;
							}

							Undo.RecordObject(this, "Cut Selection");
							var targetNodeIds = activeObject.idPosDict.ReadonlyDict().Keys.ToList();
							var targetNodeJsonRepresentations = JsonRepresentations(targetNodeIds);
							copyField = new CopyField(targetNodeJsonRepresentations, CopyType.COPYTYPE_CUT);

							foreach (var targetId in activeObject.idPosDict.ReadonlyDict().Keys) {
								DeleteNode(targetId);
								DeleteConnectionById(targetId);
							}

							Setup(ActiveBuildTarget);
							InitializeGraph();

							activeObject = RenewActiveObject(new List<string>());
							UpdateActivationOfObjects(activeObject);

							Event.current.Use();
							break;
						}

						case "Paste": {

							if(copyField.datas == null)  {
								break;
							}

							var nodeNames = nodes.Select(node => node.Name).ToList();
							var duplicatingData = new List<NodeGUI>();

							if (copyField.datas.Any()) {
								var pasteType = copyField.type;
								foreach (var copyFieldData in copyField.datas) {
									var nodeJsonDict = AssetBundleGraph.Json.Deserialize(copyFieldData) as Dictionary<string, object>;
									var pastingNode = new NodeGUI(new NodeData(nodeJsonDict));
									var pastingNodeName = pastingNode.Name;

									var nameOverlapping = nodeNames.Where(name => name == pastingNodeName).ToList();

  									switch (pasteType) {
  										case CopyType.COPYTYPE_COPY: {
											if (2 <= nameOverlapping.Count) {
												continue;
											}
  											break;
  										}
  										case CopyType.COPYTYPE_CUT: {
											if (1 <= nameOverlapping.Count) {
												continue;
											}
  											break;
  										}
  									}

  									duplicatingData.Add(pastingNode);
								}
							}
							// consume copyField
							copyField.datas = null;

							if (!duplicatingData.Any()) {
								break;
							}

							Undo.RecordObject(this, "Paste");
							foreach (var newNode in duplicatingData) {
								DuplicateNode(newNode);
							}

							Setup(ActiveBuildTarget);
							InitializeGraph();

							Event.current.Use();
							break;
						}

						case "SelectAll": {
							Undo.RecordObject(this, "Select All Objects");

							var nodeIds = nodes.Select(node => node.Id).ToList();
							activeObject = RenewActiveObject(nodeIds);

							// select all.
							foreach (var node in nodes) {
								node.SetActive();
							}
							foreach (var connection in connections) {
								connection.SetActive();
							}
							
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

		private List<string> JsonRepresentations (List<string> nodeIds) {
			return nodes.Where(nodeGui => nodeIds.Contains(nodeGui.Id)).Select(nodeGui => nodeGui.Data.ToJsonString()).ToList();
		}

		private Type GetDragAndDropAcceptableScriptType (Type type) {
			if (typeof(IPrefabBuilder).IsAssignableFrom(type) && !type.IsInterface && PrefabBuilderUtility.HasValidCustomPrefabBuilderAttribute(type)) {
				return typeof(IPrefabBuilder);
			}
			if (typeof(IModifier).IsAssignableFrom(type) && !type.IsInterface && ModifierUtility.HasValidCustomModifierAttribute(type)) {
				return typeof(IModifier);
			}

			return null;
		}

		private void AddNodeFromCode (string name, string scriptClassName, Type scriptBaseType, float x, float y) {
			NodeGUI newNode = null;

			if (scriptBaseType == typeof(IModifier)) {
				var modifier = ModifierUtility.CreateModifier(scriptClassName);
				UnityEngine.Assertions.Assert.IsNotNull(modifier);

				newNode = new NodeGUI(new NodeData(name, NodeKind.MODIFIER_GUI, x, y));
				newNode.Data.ScriptClassName = scriptClassName;
				newNode.Data.InstanceData.DefaultValue = modifier.Serialize();
			}
			if (scriptBaseType == typeof(IPrefabBuilder)) {
				var builder = PrefabBuilderUtility.CreatePrefabBuilderByClassName(scriptClassName);
				UnityEngine.Assertions.Assert.IsNotNull(builder);

				newNode = new NodeGUI(new NodeData(name, NodeKind.PREFABBUILDER_GUI, x, y));
				newNode.Data.ScriptClassName = scriptClassName;
				newNode.Data.InstanceData.DefaultValue = builder.Serialize();
			}

			if (newNode == null) {
				LogUtility.Logger.LogError(LogUtility.kTag, "Could not add node from code. " + scriptClassName + "(base:" + scriptBaseType + 
					") is not supported to create from code.");
				return;
			}

			AddNodeGUI(newNode);
		}

		private void AddNodeFromGUI (NodeKind kind, float x, float y) {

			string nodeName = AssetBundleGraphSettings.DEFAULT_NODE_NAME[kind] + nodes.Where(node => node.Kind == kind).ToList().Count;
			NodeGUI newNode = new NodeGUI(new NodeData(nodeName, kind, x, y));

			Undo.RecordObject(this, "Add " + AssetBundleGraphSettings.DEFAULT_NODE_NAME[kind] + " Node");

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
			case ModifyMode.CONNECTING: {
				switch (e.eventType) {
					/*
						handling
					*/
					case NodeEvent.EventType.EVENT_NODE_MOVING: {
						// do nothing.
						break;
					}

					/*
						connection drop detected from toward node.
					*/
					case NodeEvent.EventType.EVENT_NODE_CONNECTION_RAISED: {
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
						if(!ConnectionData.CanConnect(startNode.Data, endNode.Data)) {
							break;
						}

						AddConnection(label, startNode, outputPoint, endNode, inputPoint);
						Setup(ActiveBuildTarget);
						break;
					}

					/*
						connection drop detected by started node.
					*/
					case NodeEvent.EventType.EVENT_NODE_CONNECTION_OVERED: {
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
						if(!ConnectionData.CanConnect(startNode.Data, endNode.Data)) {
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
			}
			case ModifyMode.NONE: {
				switch (e.eventType) {
					/*
						node move detected.
					*/
					case NodeEvent.EventType.EVENT_NODE_MOVING: {
						var tappedNode = e.eventSourceNode;
						var tappedNodeId = tappedNode.Id;
						
						if (activeObject.idPosDict.ContainsKey(tappedNodeId)) {
							// already active, do nothing for this node.
							var distancePos = tappedNode.GetPos() - activeObject.idPosDict.ReadonlyDict()[tappedNodeId];

							foreach (var node in nodes) {
								if (node.Id == tappedNodeId) continue;
								if (!activeObject.idPosDict.ContainsKey(node.Id)) continue;
								var relativePos = activeObject.idPosDict.ReadonlyDict()[node.Id] + distancePos;
								node.SetPos(relativePos);
							}
							break;
						}

						if (Event.current.shift) {
							Undo.RecordObject(this, "Select Objects");

							var additiveIds = new List<string>(activeObject.idPosDict.ReadonlyDict().Keys);

							additiveIds.Add(tappedNodeId);
							
							activeObject = RenewActiveObject(additiveIds);

							UpdateActivationOfObjects(activeObject);
							UpdateSpacerRect();
							break;
						}

						Undo.RecordObject(this, "Select " + tappedNode.Name);
						activeObject = RenewActiveObject(new List<string>{tappedNodeId});
						UpdateActivationOfObjects(activeObject);
						break;
					}

					/*
						start connection handling.
					*/
					case NodeEvent.EventType.EVENT_NODE_CONNECT_STARTED: {
						modifyMode = ModifyMode.CONNECTING;
						currentEventSource = e;
						break;
					}

					case NodeEvent.EventType.EVENT_CLOSE_TAPPED: {
						
						Undo.RecordObject(this, "Delete Node");
						
						var deletingNodeId = e.eventSourceNode.Id;
						DeleteNode(deletingNodeId);

						Setup(ActiveBuildTarget);
						InitializeGraph();
						break;
					}

					/*
						releasse detected.
							node move over.
							node tapped.
					*/
					case NodeEvent.EventType.EVENT_NODE_TOUCHED: {
						var movedNode = e.eventSourceNode;
						var movedNodeId = movedNode.Id;

						// already active, node(s) are just tapped or moved.
						if (activeObject.idPosDict.ContainsKey(movedNodeId)) {

							/*
								active nodes(contains tap released node) are possibly moved.
							*/
							var movedIdPosDict = new Dictionary<string, Vector2>();
							foreach (var node in nodes) {
								if (!activeObject.idPosDict.ContainsKey(node.Id)) continue;

								var startPos = activeObject.idPosDict.ReadonlyDict()[node.Id];
								if (node.GetPos() != startPos) {
									// moved.
									movedIdPosDict[node.Id] = node.GetPos();
								}
							}

							if (movedIdPosDict.Any()) {
								
								foreach (var node in nodes) {
									if (activeObject.idPosDict.ReadonlyDict().Keys.Contains(node.Id)) {
										var startPos = activeObject.idPosDict.ReadonlyDict()[node.Id];
										node.SetPos(startPos);
									}
								}

								Undo.RecordObject(this, "Move " + movedNode.Name);

								foreach (var node in nodes) {
									if (movedIdPosDict.Keys.Contains(node.Id)) {
										var endPos = movedIdPosDict[node.Id];
										node.SetPos(endPos);
									}
								}

								var activeObjectIds = activeObject.idPosDict.ReadonlyDict().Keys.ToList();
								activeObject = RenewActiveObject(activeObjectIds);
							} else {
								// nothing moved, should cancel selecting this node.
								var cancelledActivatedIds = new List<string>(activeObject.idPosDict.ReadonlyDict().Keys);
								cancelledActivatedIds.Remove(movedNodeId);

								Undo.RecordObject(this, "Select Objects");

								activeObject = RenewActiveObject(cancelledActivatedIds);
							}
							
							UpdateActivationOfObjects(activeObject);

							UpdateSpacerRect();
							//SaveGraph();
							break;
						}

						if (Event.current.shift) {
							Undo.RecordObject(this, "Select Objects");

							var additiveIds = new List<string>(activeObject.idPosDict.ReadonlyDict().Keys);

							// already contained, cancel.
							if (additiveIds.Contains(movedNodeId)) {
								additiveIds.Remove(movedNodeId);
							} else {
								additiveIds.Add(movedNodeId);
							}

							activeObject = RenewActiveObject(additiveIds);
							UpdateActivationOfObjects(activeObject);

							UpdateSpacerRect();
							//SaveGraph();
							break;
						}
						
						Undo.RecordObject(this, "Select " + movedNode.Name);

						activeObject = RenewActiveObject(new List<string>{movedNodeId});
						UpdateActivationOfObjects(activeObject);

						UpdateSpacerRect();
						//SaveGraph();
						break;
					}
					case NodeEvent.EventType.EVENT_NODE_UPDATED: {
						break;
					}

					default: {
						break;
					}
				}
				break;
				}
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
				case NodeEvent.EventType.EVENT_SAVE: {
					Setup(ActiveBuildTarget);
					Repaint();
					break;
				}
			}
		}

		/**
			once expand, keep max size.
			it's convenience.
		*/
		private void UpdateSpacerRect () {
			var rightPoint = nodes.OrderByDescending(node => node.GetRightPos()).Select(node => node.GetRightPos()).ToList()[0] + AssetBundleGraphSettings.WINDOW_SPAN;
			if (rightPoint < spacerRectRightBottom.x) rightPoint = spacerRectRightBottom.x;

			var bottomPoint = nodes.OrderByDescending(node => node.GetBottomPos()).Select(node => node.GetBottomPos()).ToList()[0] + AssetBundleGraphSettings.WINDOW_SPAN;
			if (bottomPoint < spacerRectRightBottom.y) bottomPoint = spacerRectRightBottom.y;

			spacerRectRightBottom = new Vector2(rightPoint, bottomPoint);
		}
		
		public void DuplicateNode (NodeGUI node) {
			var newNode = node.Duplicate(
				node.GetX() + 10f,
				node.GetY() + 10f
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
				nodes[deletedNodeIndex].SetInactive();
				nodes.RemoveAt(deletedNodeIndex);
			}
		}

		public void HandleConnectionEvent (ConnectionEvent e) {
			switch (modifyMode) {
				case ModifyMode.NONE: {
					switch (e.eventType) {
						
						case ConnectionEvent.EventType.EVENT_CONNECTION_TAPPED: {
							
							if (Event.current.shift) {
								Undo.RecordObject(this, "Select Objects");

								var objectId = string.Empty;

								if (e.eventSourceCon != null) {
									objectId = e.eventSourceCon.Id;
									if (!activeObject.idPosDict.ReadonlyDict().Any()) {
										activeObject = RenewActiveObject(new List<string>{objectId});
									} else {
										var additiveIds = new List<string>(activeObject.idPosDict.ReadonlyDict().Keys);

										// already contained, cancel.
										if (additiveIds.Contains(objectId)) {
											additiveIds.Remove(objectId);
										} else {
											additiveIds.Add(objectId);
										}
										
										activeObject = RenewActiveObject(additiveIds);
									}
								}

								UpdateActivationOfObjects(activeObject);
								break;
							}


							Undo.RecordObject(this, "Select Connection");

							var tappedConnectionId = e.eventSourceCon.Id;
							foreach (var con in connections) {
								if (con.Id == tappedConnectionId) {
									con.SetActive();
									activeObject = RenewActiveObject(new List<string>{con.Id});
								} else {
									con.SetInactive();
								}
							}

							// set deactive for all nodes.
							foreach (var node in nodes) {
								node.SetInactive();
							}
							break;
						}
						case ConnectionEvent.EventType.EVENT_CONNECTION_DELETED: {
							Undo.RecordObject(this, "Delete Connection");

							var deletedConnectionId = e.eventSourceCon.Id;

							DeleteConnectionById(deletedConnectionId);

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

		private void UpdateActivationOfObjects (ActiveObject currentActiveObject) {
			foreach (var node in nodes) {
				if (currentActiveObject.idPosDict.ContainsKey(node.Id)) {
					node.SetActive();
					continue;
				}
				
				node.SetInactive();
			}

			foreach (var connection in connections) {
				if (currentActiveObject.idPosDict.ContainsKey(connection.Id)) {
					connection.SetActive();
					continue;
				}
				
				connection.SetInactive();
			}
		}

		/**
			create new connection if same relationship is not exist yet.
		*/
		private void AddConnection (string label, NodeGUI startNode, ConnectionPointData startPoint, NodeGUI endNode, ConnectionPointData endPoint) {
			Undo.RecordObject(this, "Add Connection");

			var connectionsFromThisNode = connections
				.Where(con => con.OutputNodeId == startNode.Id)
				.Where(con => con.OutputPoint == startPoint)
				.ToList();
			if (connectionsFromThisNode.Any()) {
				var alreadyExistConnection = connectionsFromThisNode[0];
				DeleteConnectionById(alreadyExistConnection.Id);
			}

			if (!connections.ContainsConnection(startPoint, endPoint)) {
				connections.Add(ConnectionGUI.CreateConnection(label, startPoint, endPoint));
			}
		}

		private NodeGUI FindNodeByPosition (Vector2 globalPos) {
			return nodes.Find(n => n.Conitains(globalPos));
		}

		private bool IsConnectablePointFromTo (ConnectionPointData sourcePoint, ConnectionPointData destPoint) {
			if( sourcePoint.IsInput ) {
				return destPoint.IsOutput;
			} else {
				return destPoint.IsInput;
			}
		}

		private void DeleteConnectionById (string id) {
			var deletedConnectionIndex = connections.FindIndex(con => con.Id == id);
			if (0 <= deletedConnectionIndex) {
				connections[deletedConnectionIndex].SetInactive();
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
