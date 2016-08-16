using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using MiniJSONForAssetBundleGraph;
 

namespace AssetBundleGraph {
	public struct ThroughputAsset {
		public readonly string path;
		public readonly bool isBundled;
		
		public ThroughputAsset (string path, bool isBundled) {
			this.path = path;
			this.isBundled = isBundled;
		}
	}
	
	public class AssetBundleGraph : EditorWindow {
		/*
			exception pool for display node error.
		*/
		private static List<NodeException> nodeExceptionPool = new List<NodeException>();

		private bool initialized;

		public static void AddNodeException (NodeException nodeEx) {
			nodeExceptionPool.Add(nodeEx);
		}
		
		private static void ResetNodeExceptionPool () {
			nodeExceptionPool.Clear();
		}

		private static void ShowErrorOnNodes (List<Node> nodes) {
			foreach (var node in nodes) {
				node.RenewErrorSource();
				var errorsForeachNode = nodeExceptionPool.Where(e => e.nodeId == node.nodeId).Select(e => e.reason).ToList();
				if (errorsForeachNode.Any()) {
					node.AppendErrorSources(errorsForeachNode);
				}
			}
		}

		/*
			menu items
		*/
		[MenuItem(AssetBundleGraphSettings.GUI_TEXT_MENU_OPEN, false, 1)]
		public static void Open () {
			var window = GetWindow<AssetBundleGraph>();
		}

		[MenuItem(AssetBundleGraphSettings.GUI_TEXT_MENU_BUILD, false, 1 + 11)]
		public static void BuildFromMenu () {
			Run();
		}

		public enum ScriptType : int {
			SCRIPT_PREFABRICATOR,
			SCRIPT_FINALLY
		}

		[MenuItem(AssetBundleGraphSettings.GUI_TEXT_MENU_GENERATE_PREFABRICATOR)]
		public static void GeneratePrefabricator () {
			GenerateScript(ScriptType.SCRIPT_PREFABRICATOR);
		}
		[MenuItem(AssetBundleGraphSettings.GUI_TEXT_MENU_GENERATE_FINALLY)]
		public static void GenerateFinally () {
			GenerateScript(ScriptType.SCRIPT_FINALLY);
		}

		/**
			build from commandline.
		*/
		public static void Build () {
			var argumentSources = new List<string>(System.Environment.GetCommandLineArgs());

			var argumentStartIndex = argumentSources.FindIndex(arg => arg == "AssetBundleGraph.AssetBundleGraph.Build") + 1;
			var currentParams = argumentSources.GetRange(argumentStartIndex, argumentSources.Count - argumentStartIndex).ToList();

			if (0 < currentParams.Count) {
				/*
					change platform for execute.
				*/
				switch (currentParams[0]) {
					case "Web": 
					case "Standalone": 
					case "iOS": 
					case "Android": 
					case "BlackBerry": 
					case "Tizen": 
					case "XBox360": 
					case "XboxOne": 
					case "PS3": 
					case "PSP2": 
					case "PS4": 
					case "StandaloneGLESEmu": 
					case "Metro": 
					case "WP8": 
					case "WebGL": 
					case "SamsungTV": {
						// valid platform.
						EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetFromString(currentParams[0]));
						break;
					}
					default: {
						throw new Exception("AssetBundleGraph error:" + currentParams[0] + " is not valid platform. by default.");
					}
				}
			}
			Run();
		}

		public static BuildTarget BuildTargetFromString (string val) {
			return (BuildTarget)Enum.Parse(typeof(BuildTarget), val);
		}

		public static void GenerateScript (ScriptType scriptType) {
			var destinationBasePath = AssetBundleGraphSettings.USERSPACE_PATH;
			var destinationPath = string.Empty;

			var sourceFileName = string.Empty;

			switch (scriptType) {
				case ScriptType.SCRIPT_PREFABRICATOR: {
					sourceFileName = FileController.PathCombine(AssetBundleGraphSettings.SCRIPTSAMPLE_PATH, "MyPrefabricator.cs.sample");
					destinationPath = FileController.PathCombine(destinationBasePath, "MyPrefabricator.cs");
					break;
				}
				case ScriptType.SCRIPT_FINALLY: {
					sourceFileName = FileController.PathCombine(AssetBundleGraphSettings.SCRIPTSAMPLE_PATH, "MyFinally.cs.sample");
					destinationPath = FileController.PathCombine(destinationBasePath, "MyFinally.cs");
					break;
				}
				default: {
					Debug.LogError("undefined script type:" + scriptType);
					break;
				}
			}

			if (string.IsNullOrEmpty(sourceFileName)) return;
			
			FileController.CopyFileFromGlobalToLocal(sourceFileName, destinationPath);

			AssetDatabase.Refresh();
		}

		
		[MenuItem(AssetBundleGraphSettings.GUI_TEXT_MENU_DELETE_CACHE)] public static void DeleteCache () {
			FileController.RemakeDirectory(AssetBundleGraphSettings.APPLICATIONDATAPATH_CACHE_PATH);

			AssetDatabase.Refresh();
		}
		
		
		[MenuItem(AssetBundleGraphSettings.GUI_TEXT_MENU_DELETE_IMPORTSETTING_SETTINGS)] public static void DeleteImportSettingSample () {
			FileController.RemakeDirectory(AssetBundleGraphSettings.IMPORTER_SETTINGS_PLACE);

			AssetDatabase.Refresh();
		}


		public void OnFocus () {
			// update handlers. these static handlers are erase when window is full-screened and badk to normal window.
			Node.Emit = EmitNodeEvent;
			Connection.Emit = EmitConnectionEvent;
		}

		private void Init() {

			if(initialized) {
				return;
			}
			initialized = true;

			this.titleContent = new GUIContent("AssetBundle");

			Node.EnsureInitialized();

			Undo.undoRedoPerformed += () => {
				SaveGraphWithReload();
				Repaint();
			};

			Node.Emit = EmitNodeEvent;
			Connection.Emit = EmitConnectionEvent;

			InitializeGraph();
			Setup();

			// load other textures
			reloadButtonTexture = UnityEditor.EditorGUIUtility.IconContent("RotateTool");
			selectionTex = LoadTextureFromFile(AssetBundleGraphGUISettings.RESOURCE_SELECTION);

			if (nodes.Any()) UpdateSpacerRect();
		}
		
		[Serializable] public struct KeyObject {
			public string key;

			public KeyObject (string val) {
				key = val;
			}
		}

		[SerializeField] private List<Node> nodes = new List<Node>();
		[SerializeField] private List<Connection> connections = new List<Connection>();

		private OnNodeEvent currentEventSource;

		public ConnectionPoint modifingConnnectionPoint;

		private Texture2D selectionTex;

		public enum ModifyMode : int {
			CONNECT_STARTED,
			CONNECT_ENDED,
			SELECTION_STARTED,
			SCALING_STARTED,
		}
		private ModifyMode modifyMode;

		private DateTime lastLoaded = DateTime.MinValue;
		
		private Vector2 spacerRectRightBottom;
		private Vector2 scrollPos = new Vector2(1500,0);
		
		private GUIContent reloadButtonTexture;

		private static Dictionary<string,Dictionary<string, List<ThroughputAsset>>> connectionThroughputs = new Dictionary<string, Dictionary<string, List<ThroughputAsset>>>();


		[Serializable] public struct ActiveObject {
			[SerializeField] public SerializablePseudoDictionary3 idPosDict;
			
			public ActiveObject (Dictionary<string, Vector2> idPosDict) {
				this.idPosDict = new SerializablePseudoDictionary3(idPosDict);
			}
		}
		[SerializeField] private ActiveObject activeObject = new ActiveObject(new Dictionary<string, Vector2>());

		public enum CopyType : int {
			COPYTYPE_COPY,
			COPYTYPE_CUT
		}

		[Serializable] public struct CopyField {
			[SerializeField] public List<string> datas;
			[SerializeField] public CopyType type;

			public CopyField (List<string> datas, CopyType type) {
				this.datas = datas;
				this.type = type;
			}
		}
		[SerializeField] private CopyField copyField = new CopyField();
		
		// hold selection start data.
		public struct AssetBundleGraphSelection {
			public readonly float x;
			public readonly float y;

			public AssetBundleGraphSelection (Vector2 position) {
				this.x = position.x;
				this.y = position.y;
			}
		}
		private AssetBundleGraphSelection selection;

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
		private ScalePoint scalePoint;

		public static Texture2D LoadTextureFromFile(string path) {
            Texture2D texture = new Texture2D(1, 1);
            texture.LoadImage(File.ReadAllBytes(path));
            return texture;
        }

		private ActiveObject RenewActiveObject (List<string> ids) {
			var idPosDict = new Dictionary<string, Vector2>();
			foreach (var node in nodes) {
				if (ids.Contains(node.nodeId)) idPosDict[node.nodeId] = node.GetPos();
			}
			foreach (var connection in connections) {
				if (ids.Contains(connection.connectionId)) idPosDict[connection.connectionId] = Vector2.zero;
			}
			return new ActiveObject(idPosDict);
		}

		/**
			node graph initializer.
			setup nodes, points and connections from saved data.
		*/
		public void InitializeGraph () {
			var basePath = FileController.PathCombine(Application.dataPath, AssetBundleGraphSettings.ASSETNBUNDLEGRAPH_DATA_PATH);
			
			// create Temp folder under Assets/AssetBundleGraph
			if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);

			var graphDataPath = FileController.PathCombine(basePath, AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_NAME);


			var deserialized = new Dictionary<string, object>();
			var lastModified = DateTime.Now;

			if (File.Exists(graphDataPath)) {

				// load
				var dataStr = string.Empty;
				
				using (var sr = new StreamReader(graphDataPath)) {
					dataStr = sr.ReadToEnd();
				}

				try {
					deserialized = Json.Deserialize(dataStr) as Dictionary<string, object>;
				} catch (Exception e) {
					Debug.LogError("data load error:" + e + " at path:" + graphDataPath);
					return;
				}

				var lastModifiedStr = deserialized[AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_LASTMODIFIED] as string;
				lastModified = Convert.ToDateTime(lastModifiedStr);

				var validatedDataDict = GraphStackController.ValidateStackedGraph(deserialized);

				var validatedDate = validatedDataDict[AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_LASTMODIFIED] as string;
				if (lastModifiedStr != validatedDate) {
					// save validated graph data.
					UpdateGraphData(validatedDataDict);

					// reload
					var dataStr2 = string.Empty;
					using (var sr = new StreamReader(graphDataPath)) {
						dataStr2 = sr.ReadToEnd();
					}

					deserialized = Json.Deserialize(dataStr2) as Dictionary<string, object>;

					var lastModifiedStr2 = deserialized[AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_LASTMODIFIED] as string;
					lastModified = Convert.ToDateTime(lastModifiedStr2);
				}

			} else {
				var graphData = RenewData();

				// set new empty graph data.
				deserialized = graphData;
			}

			/*
				do nothing if json does not modified after first load.
			*/
			if (lastModified == lastLoaded) return;


			lastLoaded = lastModified;

			minSize = new Vector2(600f, 300f);
			
			wantsMouseMove = true;
			modifyMode = ModifyMode.CONNECT_ENDED;

			
			
			/*
				load graph data from deserialized data.
			*/
			var nodesAndConnections = ConstructGraphFromDeserializedData(deserialized);
			nodes = nodesAndConnections.currentNodes;
			connections = nodesAndConnections.currentConnections;
		}

		private static Dictionary<string, object> RenewData () {
			// renew
			var graphData = new Dictionary<string, object>{
				{AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_LASTMODIFIED, DateTime.Now.ToString()},
				{AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_NODES, new List<object>()},
				{AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_CONNECTIONS, new List<object>()}
			};

			// save new empty graph data.
			UpdateGraphData(graphData);

			return graphData;
		}

		private struct NodesAndConnections {
			public List<Node> currentNodes;
			public List<Connection> currentConnections;

			public NodesAndConnections (List<Node> currentNodes, List<Connection> currentConnections) {
				this.currentNodes = currentNodes;
				this.currentConnections = currentConnections;
			}
		}

		private static NodesAndConnections ConstructGraphFromDeserializedData (Dictionary<string, object> deserializedData) {
			var currentNodes = new List<Node>();
			var currentConnections = new List<Connection>();

			var nodesSource = deserializedData[AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_NODES] as List<object>;
			
			foreach (var nodeDictSource in nodesSource) {
				currentNodes.Add(NodeFromJsonDict(currentNodes.Count, nodeDictSource as Dictionary<string, object>));
			}

			// add default input if node is not NodeKind.SOURCE.
			foreach (var node in currentNodes) {
				if (node.kind == AssetBundleGraphSettings.NodeKind.LOADER_GUI) continue;
				node.AddConnectionPoint(new InputPoint(AssetBundleGraphSettings.DEFAULT_INPUTPOINT_LABEL));
			}

			// load connections
			var connectionsSource = deserializedData[AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_CONNECTIONS] as List<object>;
			foreach (var connectionSource in connectionsSource) {
				var connectionDict = connectionSource as Dictionary<string, object>;
				var label = connectionDict[AssetBundleGraphSettings.CONNECTION_LABEL] as string;
				var connectionId = connectionDict[AssetBundleGraphSettings.CONNECTION_ID] as string;
				var fromNodeId = connectionDict[AssetBundleGraphSettings.CONNECTION_FROMNODE] as string;
				var toNodeId = connectionDict[AssetBundleGraphSettings.CONNECTION_TONODE] as string;

				var startNodeCandidates = currentNodes.Where(node => node.nodeId == fromNodeId).ToList();
				if (!startNodeCandidates.Any()) continue;
				var startNode = startNodeCandidates[0];
				var startPoint = startNode.ConnectionPointFromLabel(label);

				var endNodeCandidates = currentNodes.Where(node => node.nodeId == toNodeId).ToList();
				if (!endNodeCandidates.Any()) continue;
				var endNode = endNodeCandidates[0];
				var endPoint = endNode.ConnectionPointFromLabel(AssetBundleGraphSettings.DEFAULT_INPUTPOINT_LABEL);

				currentConnections.Add(Connection.LoadConnection(label, connectionId, startNode.nodeId, startPoint, endNode.nodeId, endPoint));
			}

			return new NodesAndConnections(currentNodes, currentConnections);
		}

		private void SaveGraph () {
			var nodeList = new List<Dictionary<string, object>>();
			foreach (var node in nodes) {
				var jsonRepresentationSourceDict = JsonRepresentationDict(node);
				nodeList.Add(jsonRepresentationSourceDict);
			}

			var connectionList = new List<Dictionary<string, string>>();
			foreach (var connection in connections) {
				var connectionDict = new Dictionary<string, string>{
					{AssetBundleGraphSettings.CONNECTION_LABEL, connection.label},
					{AssetBundleGraphSettings.CONNECTION_ID, connection.connectionId},
					{AssetBundleGraphSettings.CONNECTION_FROMNODE, connection.startNodeId},
					{AssetBundleGraphSettings.CONNECTION_TONODE, connection.endNodeId}
				};
				connectionList.Add(connectionDict);
			}

			var graphData = new Dictionary<string, object>{
				{AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_LASTMODIFIED, DateTime.Now.ToString()},
				{AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_NODES, nodeList},
				{AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_CONNECTIONS, connectionList}
			};

			UpdateGraphData(graphData);
		}

		private void SaveGraphWithReloadSilent () {
			SaveGraph();
			try {
				Setup();
			} catch {
				// display nothing.d
			}
		}

		private void SaveGraphWithReload () {
			SaveGraph();
			try {
				Setup();
			} catch (Exception e) {
				Debug.LogError("reload error:" + e);
			}
		}

		
		private void Setup () {
			ResetNodeExceptionPool();

			EditorUtility.ClearProgressBar();

			var graphDataPath = FileController.PathCombine(Application.dataPath, AssetBundleGraphSettings.ASSETNBUNDLEGRAPH_DATA_PATH, AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_NAME);
			if (!File.Exists(graphDataPath)) {
				RenewData();
				Debug.LogError("no data found. new data is generated.");
				return;
			}

			foreach (var node in nodes) {
				node.HideProgress();
			}

			// reload data from file.
			var dataStr = string.Empty;
			using (var sr = new StreamReader(graphDataPath)) {
				dataStr = sr.ReadToEnd();
			}

			var reloadedData = Json.Deserialize(dataStr) as Dictionary<string, object>;

			// update static all node names.
			Node.allNodeNames = new List<string>(nodes.Select(node => node.name).ToList());

			// ready throughput datas.
			connectionThroughputs = GraphStackController.SetupStackedGraph(reloadedData);

			RefreshInspector(connectionThroughputs);

			ShowErrorOnNodes(nodes);

			Finally(nodes, connections, connectionThroughputs, false);
		}

		private static void Run () {
			ResetNodeExceptionPool();

			var graphDataPath = FileController.PathCombine(Application.dataPath, AssetBundleGraphSettings.ASSETNBUNDLEGRAPH_DATA_PATH, AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_NAME);
			if (!File.Exists(graphDataPath)) {
				RenewData();
				Debug.LogError("no data found. new data is generated.");
				return;
			}

			// reload data from file.
			var dataStr = string.Empty;
			using (var sr = new StreamReader(graphDataPath)) {
				dataStr = sr.ReadToEnd();
			}
			
			var loadedData = Json.Deserialize(dataStr) as Dictionary<string, object>;
			var nodesAndConnections = ConstructGraphFromDeserializedData(loadedData);
			var currentNodes = nodesAndConnections.currentNodes;
			var currentConnections = nodesAndConnections.currentConnections;

			var currentCount = 0.00f;
			var totalCount = currentNodes.Count * 1f;

			Action<string, float> updateHandler = (nodeId, progress) => {
				var targetNodes = currentNodes.Where(node => node.nodeId == nodeId).ToList();
				
				var progressPercentage = ((currentCount/totalCount) * 100).ToString();
				
				if (progressPercentage.Contains(".")) progressPercentage = progressPercentage.Split('.')[0];
				
				if (0 < progress) {
					currentCount = currentCount + 1f;
				}

				if (targetNodes.Any()) {
					targetNodes.ForEach(
						node => {
							EditorUtility.DisplayProgressBar("AssetBundleGraph Processing " + node.name + ".", progressPercentage + "%", currentCount/totalCount);
						}
					);
				}
			};


			// setup datas. fail if exception raise.
			GraphStackController.SetupStackedGraph(loadedData);


			/*
				remove bundlize setting names from unused Nodes.
			*/
			var endpointNodeIdsAndNodeDatasAndConnectionDatas = GraphStackController.SerializeNodeRoute(loadedData);
			var usedNodeIds = endpointNodeIdsAndNodeDatasAndConnectionDatas.nodeDatas.Select(usedNode => usedNode.nodeId).ToList();
			UnbundlizeUnusedNodeBundleSettings(usedNodeIds);

			
			// run datas.
			connectionThroughputs = GraphStackController.RunStackedGraph(loadedData, updateHandler);

			EditorUtility.ClearProgressBar();
			AssetDatabase.Refresh();

			RefreshInspector(connectionThroughputs);

			ShowErrorOnNodes(currentNodes);

			Finally(currentNodes, currentConnections, connectionThroughputs, true);
		}

		private static void RefreshInspector (Dictionary<string,Dictionary<string, List<ThroughputAsset>>> currentConnectionThroughputs) {
			if (Selection.activeObject == null) return; 
			switch (Selection.activeObject.GetType().ToString()) {
				case "AssetBundleGraph.ConnectionInspector": {
					var con = ((ConnectionInspector)Selection.activeObject).con;
					((ConnectionInspector)Selection.activeObject).UpdateThroughputs(currentConnectionThroughputs[con.connectionId]);
					break;
				}
				default: {
					// do nothing.
					break;
				}
			}
		} 

		public static void Finally (
			List<Node> currentNodes,
			List<Connection> currentConnections,
			Dictionary<string, Dictionary<string, List<ThroughputAsset>>> throughputsSource, 
			bool isRun
		) {
			var nodeThroughputs = NodeThroughputs(currentNodes, currentConnections, throughputsSource);

			var finallyBasedTypeRunner = Assembly.GetExecutingAssembly().GetTypes()
					.Where(currentType => currentType.BaseType == typeof(FinallyBase))
					.Select(type => type.ToString())
					.ToList();
			foreach (var typeStr in finallyBasedTypeRunner) {
				var finallyScriptInstance = Assembly.GetExecutingAssembly().CreateInstance(typeStr);
				if (finallyScriptInstance == null) throw new Exception("failed to generate class information of class:" + typeStr + " which is based on Type:" + typeof(FinallyBase));
				var finallyInstance = (FinallyBase)finallyScriptInstance;

				finallyInstance.Run(nodeThroughputs, isRun);
			}
		}

		
		private static void UnbundlizeUnusedNodeBundleSettings (List<string> usedNodeIds) {
			EditorUtility.DisplayProgressBar("unbundlize unused resources...", "ready", 0);
			
			var filePathsInFolder = FileController
				.FilePathsInFolder(AssetBundleGraphSettings.APPLICATIONDATAPATH_CACHE_PATH)
				.Where(path => !GraphStackController.IsMetaFile(path))
				.ToList();

			
			var unusedNodeResourcePaths = new List<string>();
			foreach (var filePath in filePathsInFolder) {
				// Assets/AssetBundleGraph/Cached/NodeKind/NodeId/platform-package/CachedResources
				var splitted = filePath.Split(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR);

				var nodeIdInCache = splitted[4];

				if (usedNodeIds.Contains(nodeIdInCache)) continue;
				unusedNodeResourcePaths.Add(filePath);
			}

			var max = unusedNodeResourcePaths.Count * 1.0f;
			var count = 0;
			foreach (var unusedNodeResourcePath in unusedNodeResourcePaths) {
				// Assets/AssetBundleGraph/Cached/NodeKind/NodeId/platform-package/CachedResources
				var splitted = unusedNodeResourcePath.Split(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR);
				var underNodeFilePathSource = splitted.Where((v,i) => 5 < i).ToArray();
				var underNodeFilePath = string.Join(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR.ToString(), underNodeFilePathSource);
				EditorUtility.DisplayProgressBar("unbundlize unused resources...", count + "/" + max + " " + splitted[3] + " : " + underNodeFilePath, count / max);
				
				var assetImporter = AssetImporter.GetAtPath(unusedNodeResourcePath);
				assetImporter.assetBundleName = string.Empty;

				count = count + 1;
			}

			EditorUtility.ClearProgressBar();
		}

		/**
			collect node's result with node name.
			structure is:

			nodeNames
				groups
					resources
		*/
		private static Dictionary<string, Dictionary<string, List<string>>> NodeThroughputs (
			List<Node> currentNodes,
			List<Connection> currentConnections,
			Dictionary<string, Dictionary<string, List<ThroughputAsset>>> throughputs
		) {
			var nodeDatas = new Dictionary<string, Dictionary<string, List<string>>>();

			var nodeIds = currentNodes.Select(node => node.nodeId).ToList();
			var connectionIds = currentConnections.Select(con => con.connectionId).ToList();

			foreach (var nodeOrConnectionId in throughputs.Keys) {
				// get endpoint node result.
				if (nodeIds.Contains(nodeOrConnectionId)) {
					var targetNodeName = currentNodes.Where(node => node.nodeId == nodeOrConnectionId).Select(node => node.name).FirstOrDefault();
					
					var nodeThroughput = throughputs[nodeOrConnectionId];

					if (!nodeDatas.ContainsKey(targetNodeName)) nodeDatas[targetNodeName] = new Dictionary<string, List<string>>();
					foreach (var groupKey in nodeThroughput.Keys) {
						if (!nodeDatas[targetNodeName].ContainsKey(groupKey)) nodeDatas[targetNodeName][groupKey] = new List<string>();
						var assetPaths = nodeThroughput[groupKey].Select(asset => asset.path).ToList();
						nodeDatas[targetNodeName][groupKey].AddRange(assetPaths);
					}
				}

				// get connection result.
				if (connectionIds.Contains(nodeOrConnectionId)) {
					var targetConnection = currentConnections.Where(con => con.connectionId == nodeOrConnectionId).FirstOrDefault();
					var targetNodeName = currentNodes.Where(node => node.nodeId == targetConnection.startNodeId).Select(node => node.name).FirstOrDefault();
					
					var nodeThroughput = throughputs[nodeOrConnectionId];

					if (!nodeDatas.ContainsKey(targetNodeName)) nodeDatas[targetNodeName] = new Dictionary<string, List<string>>();
					foreach (var groupKey in nodeThroughput.Keys) {
						if (!nodeDatas[targetNodeName].ContainsKey(groupKey)) nodeDatas[targetNodeName][groupKey] = new List<string>();
						var assetPaths = nodeThroughput[groupKey].Select(asset => asset.path).ToList();
						nodeDatas[targetNodeName][groupKey].AddRange(assetPaths);
					}
				}
			}

			return nodeDatas;
		}

		public void OnGUI () {

			Init();

			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar)) {
				if (GUILayout.Button(new GUIContent("Refresh", reloadButtonTexture.image, "Refresh and reload"), EditorStyles.toolbarButton, GUILayout.Width(80), GUILayout.Height(AssetBundleGraphGUISettings.TOOLBAR_HEIGHT))) {
					Setup();
				}

				GUILayout.FlexibleSpace();

				GUIStyle tbLabel = new GUIStyle(EditorStyles.toolbar);

				tbLabel.alignment = TextAnchor.MiddleCenter;

				GUIStyle tbLabelTarget = new GUIStyle(tbLabel);
				tbLabelTarget.fontStyle = FontStyle.Bold;

				GUILayout.Label("Platform:", tbLabel, GUILayout.Height(AssetBundleGraphGUISettings.TOOLBAR_HEIGHT));
				GUILayout.Label(AssetBundleGraphPlatformSettings.BuildTargetToHumaneString(EditorUserBuildSettings.activeBuildTarget), tbLabelTarget, GUILayout.Height(AssetBundleGraphGUISettings.TOOLBAR_HEIGHT));

				if (GUILayout.Button("Build", EditorStyles.toolbarButton, GUILayout.Height(AssetBundleGraphGUISettings.TOOLBAR_HEIGHT))) {
					Run();
				}
			}

			/*
				scroll view.
			*/
			// var scaledScrollPos = Node.ScaleEffect(scrollPos);
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
			// scrollPos = scrollPos + (movedScrollPos - scaledScrollPos);
			{
				
				// draw node window x N.
				{
					BeginWindows();
					
					nodes.ForEach(node => node.DrawNode());

					EndWindows();
				}

				// draw connection input point marks.
				foreach (var node in nodes) {
					node.DrawConnectionInputPointMark(currentEventSource, modifyMode == ModifyMode.CONNECT_STARTED);
				}
				
				
				// draw connections.
				foreach (var con in connections) {
					if (connectionThroughputs.ContainsKey(con.connectionId)) { 
						var throughputListDict = connectionThroughputs[con.connectionId];
						con.DrawConnection(nodes, throughputListDict);
					} else {
						con.DrawConnection(nodes, new Dictionary<string, List<ThroughputAsset>>());
					}
				}

				

				// draw connection output point marks.
				foreach (var node in nodes) {
					node.DrawConnectionOutputPointMark(currentEventSource, modifyMode == ModifyMode.CONNECT_STARTED, Event.current);
				}

				/*
					draw connecting line if modifing connection.
				*/
				switch (modifyMode) {
					case ModifyMode.CONNECT_STARTED: {
						// from start node to mouse.
						DrawStraightLineFromCurrentEventSourcePointTo(Event.current.mousePosition, currentEventSource);

						break;
					}
					case ModifyMode.CONNECT_ENDED: {
						// do nothing
						break;
					}
					case ModifyMode.SELECTION_STARTED: {
						GUI.DrawTexture(new Rect(selection.x, selection.y, Event.current.mousePosition.x - selection.x, Event.current.mousePosition.y - selection.y), selectionTex);
						break;
					}
				}

				/*
					mouse drag event handling.
				*/
				switch (Event.current.type) {

					// draw line while dragging.
					case EventType.MouseDrag: {
						switch (modifyMode) {
							case ModifyMode.CONNECT_ENDED: {
								switch (Event.current.button) {
									case 0:{// left click
										if (Event.current.command) {
											scalePoint = new ScalePoint(Event.current.mousePosition, Node.scaleFactor, 0);
											modifyMode = ModifyMode.SCALING_STARTED;
											break;
										}

										selection = new AssetBundleGraphSelection(Event.current.mousePosition);
										modifyMode = ModifyMode.SELECTION_STARTED;
										break;
									}
									case 2:{// middle click.
										scalePoint = new ScalePoint(Event.current.mousePosition, Node.scaleFactor, 0);
										modifyMode = ModifyMode.SCALING_STARTED;
										break;
									}
								}
								break;
							}
							case ModifyMode.SELECTION_STARTED: {
								// do nothing.
								break;
							}
							case ModifyMode.SCALING_STARTED: {
								var baseDistance = (int)Vector2.Distance(Event.current.mousePosition, new Vector2(scalePoint.x, scalePoint.y));
								var distance = baseDistance / Node.SCALE_WIDTH;
								var direction = (0 < Event.current.mousePosition.y - scalePoint.y);

								if (!direction) distance = -distance;

								// var before = Node.scaleFactor;
								Node.scaleFactor = scalePoint.startScale + (distance * Node.SCALE_RATIO);

								if (Node.scaleFactor < Node.SCALE_MIN) Node.scaleFactor = Node.SCALE_MIN;
								if (Node.SCALE_MAX < Node.scaleFactor) Node.scaleFactor = Node.SCALE_MAX;
								break;
							}
						}

						HandleUtility.Repaint();
						Event.current.Use();
						break;
					}
				}

				/*
					mouse up event handling.
					use rawType for detect for detectiong mouse-up which raises outside of window.
				*/
				switch (Event.current.rawType) {
					case EventType.MouseUp: {
						switch (modifyMode) {
							/*
								select contained nodes & connections.
							*/
							case ModifyMode.SELECTION_STARTED: {
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
									nodeRect.x = nodeRect.x * Node.scaleFactor;
									nodeRect.y = nodeRect.y * Node.scaleFactor;
									nodeRect.width = nodeRect.width * Node.scaleFactor;
									nodeRect.height = nodeRect.height * Node.scaleFactor;
									// get containd nodes,
									if (nodeRect.Overlaps(selectedRect)) {
										activeObjectIds.Add(node.nodeId);
									}
								}

								foreach (var connection in connections) {
									// get contained connection badge.
									if (connection.GetRect().Overlaps(selectedRect)) {
										activeObjectIds.Add(connection.connectionId);
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
								modifyMode = ModifyMode.CONNECT_ENDED;

								HandleUtility.Repaint();
								Event.current.Use();
								break;
							}

							case ModifyMode.SCALING_STARTED: {
								modifyMode = ModifyMode.CONNECT_ENDED;
								break;
							}
						}
						break;
					}
				}
				
				// set rect for scroll.
				if (nodes.Any()) {
					GUILayoutUtility.GetRect(new GUIContent(string.Empty), GUIStyle.none, GUILayout.Width(spacerRectRightBottom.x), GUILayout.Height(spacerRectRightBottom.y));
				}
			}
			EditorGUILayout.EndScrollView();


			/*
				detect 
					dragging some script into window.
					right click.
					connection end mouse up.
					command(Delete, Copy, and more)
			*/
			switch (Event.current.type) {
				// detect dragging script then change interface to "(+)" icon.
				case EventType.DragUpdated: {
					var refs = DragAndDrop.objectReferences;

					foreach (var refe in refs) {
						if (refe.GetType() == typeof(UnityEditor.MonoScript)) {
							var type = ((MonoScript)refe).GetClass();
							
							var inherited = IsAcceptableScriptType(type);

							if (inherited != null) {
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
						var path = item.Key;
						var refe = (MonoScript)item.Value;
						if (refe.GetType() == typeof(UnityEditor.MonoScript)) {
							var type = refe.GetClass();
							var inherited = IsAcceptableScriptType(type);

							if (inherited != null) {
								var dropPos = Event.current.mousePosition;
								var scriptName = refe.name;
								var scriptType = scriptName;// name = type.
								var scriptPath = path;
								AddNodeFromCode(scriptName, scriptType, scriptPath, inherited, Guid.NewGuid().ToString(), dropPos.x, dropPos.y);
								shouldSave = true;
							}
						}
					}

					if (shouldSave) SaveGraphWithReload();
					break;
				}

				// show context menu
				case EventType.ContextClick: {
					var rightClickPos = Event.current.mousePosition;
					var menu = new GenericMenu();
					foreach (var menuItemStr in AssetBundleGraphSettings.GUI_Menu_Item_TargetGUINodeDict.Keys) {
						var targetGUINodeNameStr = AssetBundleGraphSettings.GUI_Menu_Item_TargetGUINodeDict[menuItemStr];
						menu.AddItem(
							new GUIContent(menuItemStr),
							false, 
							() => {
								AddNodeFromGUI(string.Empty, targetGUINodeNameStr, Guid.NewGuid().ToString(), rightClickPos.x, rightClickPos.y);
								SaveGraphWithReload();
								Repaint();
							}
						);
					}
					menu.ShowAsContext();
					break;
				}

				/*
					handling mouse up
						 -> drag released -> release modifyMode.
				*/
				case EventType.MouseUp: {
					modifyMode = ModifyMode.CONNECT_ENDED;
					HandleUtility.Repaint();
					
					if (activeObject.idPosDict.ReadonlyDict().Any()) {
						Undo.RecordObject(this, "Unselect");

						foreach (var activeObjectId in activeObject.idPosDict.ReadonlyDict().Keys) {
							// unselect all.
							foreach (var node in nodes) {
								if (activeObjectId == node.nodeId) node.SetInactive();
							}
							foreach (var connection in connections) {
								if (activeObjectId == connection.connectionId) connection.SetInactive();
							}
						}

						activeObject = RenewActiveObject(new List<string>());
					}

					break;
				}

				/*
					scale up or down by command & + or command & -.
				*/
				case EventType.KeyDown: {
					if (Event.current.command) {
						if (Event.current.shift && Event.current.keyCode == KeyCode.Semicolon) {
							Node.scaleFactor = Node.scaleFactor + 0.1f;
							if (Node.scaleFactor < Node.SCALE_MIN) Node.scaleFactor = Node.SCALE_MIN;
							if (Node.SCALE_MAX < Node.scaleFactor) Node.scaleFactor = Node.SCALE_MAX;
							Event.current.Use();
							break;
						}

						if (Event.current.keyCode == KeyCode.Minus) {
							Node.scaleFactor = Node.scaleFactor - 0.1f;
							if (Node.scaleFactor < Node.SCALE_MIN) Node.scaleFactor = Node.SCALE_MIN;
							if (Node.SCALE_MAX < Node.scaleFactor) Node.scaleFactor = Node.SCALE_MAX;
							Event.current.Use();
							break;
						}
					}
					break;
				}

				case EventType.ValidateCommand: {
					switch (Event.current.commandName) {
						// Delete active node or connection.
						case "Delete": {

							if (!activeObject.idPosDict.ReadonlyDict().Any()) break;
							Undo.RecordObject(this, "Delete Selection");

							foreach (var targetId in activeObject.idPosDict.ReadonlyDict().Keys) {
								DeleteNode(targetId);
								DeleteConnectionById(targetId);
							}

							SaveGraphWithReload();
							InitializeGraph();

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

							SaveGraphWithReload();
							InitializeGraph();

							activeObject = RenewActiveObject(new List<string>());
							UpdateActivationOfObjects(activeObject);

							Event.current.Use();
							break;
						}

						case "Paste": {
							var nodeNames = nodes.Select(node => node.name).ToList();
							var duplicatingData = new List<Node>();

							if (copyField.datas.Any()) {
								var pasteType = copyField.type;
								foreach (var copyFieldData in copyField.datas) {
									var nodeJsonDict = Json.Deserialize(copyFieldData) as Dictionary<string, object>;
									var pastingNode = NodeFromJsonDict(nodes.Count, nodeJsonDict);
									var pastingNodeName = pastingNode.name;

									var nameOverlapping = nodeNames.Where(name => name == pastingNodeName).ToList();

  									switch (pasteType) {
  										case CopyType.COPYTYPE_COPY: {
  											if (2 <= nameOverlapping.Count) continue;
  											break;
  										}
  										case CopyType.COPYTYPE_CUT: {
  											if (1 <= nameOverlapping.Count) continue;
  											break;
  										}
  									}

  									duplicatingData.Add(pastingNode);
								}
							}

							if (!duplicatingData.Any()) break;

							Undo.RecordObject(this, "Paste");
							foreach (var newNode in duplicatingData) {
								DuplicateNode(newNode);
							}

							SaveGraphWithReload();
							InitializeGraph();

							Event.current.Use();
							break;
						}

						case "SelectAll": {
							Undo.RecordObject(this, "Select All Objects");

							var nodeIds = nodes.Select(node => node.nodeId).ToList();
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
			var jsonRepresentations = new List<string>();
			var jsonRepsSourceDictList = nodes.Where(node => nodeIds.Contains(node.nodeId)).Select(node => JsonRepresentationDict(node)).ToList();
			
			foreach (var jsonRepsSourceDict in jsonRepsSourceDictList) {
				var jsonString = Json.Serialize(jsonRepsSourceDict);
				jsonRepresentations.Add(jsonString);
			}
			
			return jsonRepresentations;
		}

		private static Dictionary<string, object> JsonRepresentationDict (Node node) {
			var nodeDict = new Dictionary<string, object>();

			nodeDict[AssetBundleGraphSettings.NODE_NAME] = node.name;
			nodeDict[AssetBundleGraphSettings.NODE_ID] = node.nodeId;
			nodeDict[AssetBundleGraphSettings.NODE_KIND] = node.kind.ToString();

			var outputLabels = node.OutputPointLabels();
			nodeDict[AssetBundleGraphSettings.NODE_OUTPUT_LABELS] = outputLabels;

			var posDict = new Dictionary<string, object>();
			posDict[AssetBundleGraphSettings.NODE_POS_X] = node.GetX();
			posDict[AssetBundleGraphSettings.NODE_POS_Y] = node.GetY();

			nodeDict[AssetBundleGraphSettings.NODE_POS] = posDict;

			switch (node.kind) {
				case AssetBundleGraphSettings.NodeKind.LOADER_GUI: {
					nodeDict[AssetBundleGraphSettings.NODE_LOADER_LOAD_PATH] = node.loadPath.ReadonlyDict();
					break;
				}
				case AssetBundleGraphSettings.NodeKind.EXPORTER_GUI: {
					nodeDict[AssetBundleGraphSettings.NODE_EXPORTER_EXPORT_PATH] = node.exportPath.ReadonlyDict();
					break;
				}
				
				case AssetBundleGraphSettings.NodeKind.FILTER_SCRIPT:
				case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_SCRIPT: {
					nodeDict[AssetBundleGraphSettings.NODE_SCRIPT_TYPE] = node.scriptType;
					nodeDict[AssetBundleGraphSettings.NODE_SCRIPT_PATH] = node.scriptPath;
					break;
				}

				case AssetBundleGraphSettings.NodeKind.FILTER_GUI: {
					nodeDict[AssetBundleGraphSettings.NODE_FILTER_CONTAINS_KEYWORDS] = node.filterContainsKeywords;
					nodeDict[AssetBundleGraphSettings.NODE_FILTER_CONTAINS_KEYTYPES] = node.filterContainsKeytypes;
					break;
				}

				case AssetBundleGraphSettings.NodeKind.IMPORTSETTING_GUI: {
					nodeDict[AssetBundleGraphSettings.NODE_IMPORTER_PACKAGES] = node.importerPackages.ReadonlyDict();
					break;
				}
				
				case AssetBundleGraphSettings.NodeKind.GROUPING_GUI: {
					nodeDict[AssetBundleGraphSettings.NODE_GROUPING_KEYWORD] = node.groupingKeyword.ReadonlyDict();
					break;
				}

				case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_GUI: {
					nodeDict[AssetBundleGraphSettings.NODE_SCRIPT_TYPE] = node.scriptType;
					nodeDict[AssetBundleGraphSettings.NODE_SCRIPT_PATH] = node.scriptPath;
					break;
				}

				case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: {
					nodeDict[AssetBundleGraphSettings.NODE_BUNDLIZER_BUNDLENAME_TEMPLATE] = node.bundleNameTemplate.ReadonlyDict();
					nodeDict[AssetBundleGraphSettings.NODE_BUNDLIZER_USE_OUTPUT] = node.bundleUseOutput.ReadonlyDict();
					break;
				}

				case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					nodeDict[AssetBundleGraphSettings.NODE_BUNDLEBUILDER_ENABLEDBUNDLEOPTIONS] = node.enabledBundleOptions.ReadonlyDict();
					break;
				}

				default: {
					Debug.LogError("failed to match:" + node.kind);
					break;
				}
			}
			return nodeDict;
		}

		private static Node NodeFromJsonDict (int currentNodesCount, Dictionary<string, object> nodeDict) {
			var name = nodeDict[AssetBundleGraphSettings.NODE_NAME] as string;
			var id = nodeDict[AssetBundleGraphSettings.NODE_ID] as string;
			var kindSource = nodeDict[AssetBundleGraphSettings.NODE_KIND] as string;

			var kind = AssetBundleGraphSettings.NodeKindFromString(kindSource);
			
			var posDict = nodeDict[AssetBundleGraphSettings.NODE_POS] as Dictionary<string, object>;
			var x = (float)Convert.ToInt32(posDict[AssetBundleGraphSettings.NODE_POS_X]);
			var y = (float)Convert.ToInt32(posDict[AssetBundleGraphSettings.NODE_POS_Y]);		

			switch (kind) {
				case AssetBundleGraphSettings.NodeKind.LOADER_GUI: {
					var loadPathSource = nodeDict[AssetBundleGraphSettings.NODE_LOADER_LOAD_PATH] as Dictionary<string, object>;
					var loadPath = new Dictionary<string, string>();
					foreach (var platform_package_key in loadPathSource.Keys) loadPath[platform_package_key] = loadPathSource[platform_package_key] as string;

					var newNode = Node.LoaderNode(currentNodesCount, name, id, kind, loadPath, x, y);
					
					var outputLabelsList = nodeDict[AssetBundleGraphSettings.NODE_OUTPUT_LABELS] as List<object>;
					foreach (var outputLabelSource in outputLabelsList) {
						var label = outputLabelSource as string;
						newNode.AddConnectionPoint(new OutputPoint(label));
					}
					return newNode;
				}
				case AssetBundleGraphSettings.NodeKind.FILTER_SCRIPT:

				case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
				case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_GUI: {
					var scriptType = nodeDict[AssetBundleGraphSettings.NODE_SCRIPT_TYPE] as string;
					var scriptPath = nodeDict[AssetBundleGraphSettings.NODE_SCRIPT_PATH] as string;

					var newNode = Node.ScriptNode(currentNodesCount, name, id, kind, scriptType, scriptPath, x, y);

					var outputLabelsList = nodeDict[AssetBundleGraphSettings.NODE_OUTPUT_LABELS] as List<object>;
					foreach (var outputLabelSource in outputLabelsList) {
						var label = outputLabelSource as string;
						newNode.AddConnectionPoint(new OutputPoint(label));
					}
					return newNode;
				}

				case AssetBundleGraphSettings.NodeKind.FILTER_GUI: {
					var filterContainsKeywordsSource = nodeDict[AssetBundleGraphSettings.NODE_FILTER_CONTAINS_KEYWORDS] as List<object>;
					var filterContainsKeywords = new List<string>();
					foreach (var filterContainsKeywordSource in filterContainsKeywordsSource) {
						filterContainsKeywords.Add(filterContainsKeywordSource.ToString());
					}
					
					var filterContainsKeytypesSource = nodeDict[AssetBundleGraphSettings.NODE_FILTER_CONTAINS_KEYTYPES] as List<object>;
					var filterContainsKeytypes = new List<string>();
					foreach (var filterContainsKeytypeSource in filterContainsKeytypesSource) {
						filterContainsKeytypes.Add(filterContainsKeytypeSource.ToString());
					}

					var newNode = Node.GUINodeForFilter(currentNodesCount, name, id, kind, filterContainsKeywords, filterContainsKeytypes, x, y);

					var outputLabelsList = nodeDict[AssetBundleGraphSettings.NODE_OUTPUT_LABELS] as List<object>;
					foreach (var outputLabelSource in outputLabelsList) {
						var label = outputLabelSource as string;
						newNode.AddConnectionPoint(new OutputPoint(label));
					}
					return newNode;
				}

				case AssetBundleGraphSettings.NodeKind.IMPORTSETTING_GUI: {
					var defaultPlatformAndPackagesSource = nodeDict[AssetBundleGraphSettings.NODE_IMPORTER_PACKAGES] as Dictionary<string, object>;
					var defaultPlatformAndPackages = new Dictionary<string, string>();
					foreach (var platform_package_key in defaultPlatformAndPackagesSource.Keys) defaultPlatformAndPackages[platform_package_key] = defaultPlatformAndPackagesSource[platform_package_key] as string;

					var newNode = Node.GUINodeForImport(currentNodesCount, name, id, kind, defaultPlatformAndPackages, x, y);
					
					var outputLabelsList = nodeDict[AssetBundleGraphSettings.NODE_OUTPUT_LABELS] as List<object>;
					foreach (var outputLabelSource in outputLabelsList) {
						var label = outputLabelSource as string;
						newNode.AddConnectionPoint(new OutputPoint(label));
					}
					return newNode;
				}

				case AssetBundleGraphSettings.NodeKind.GROUPING_GUI: {
					var groupingKeywordSource = nodeDict[AssetBundleGraphSettings.NODE_GROUPING_KEYWORD] as Dictionary<string, object>;
					var groupingKeyword = new Dictionary<string, string>();
					foreach (var platform_package_key in groupingKeywordSource.Keys) groupingKeyword[platform_package_key] = groupingKeywordSource[platform_package_key] as string;

					var newNode = Node.GUINodeForGrouping(currentNodesCount, name, id, kind, groupingKeyword, x, y);
					
					var outputLabelsList = nodeDict[AssetBundleGraphSettings.NODE_OUTPUT_LABELS] as List<object>;
					foreach (var outputLabelSource in outputLabelsList) {
						var label = outputLabelSource as string;
						newNode.AddConnectionPoint(new OutputPoint(label));
					}
					return newNode;
				}
				
				case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: {
					
					var bundleNameTemplateSource = nodeDict[AssetBundleGraphSettings.NODE_BUNDLIZER_BUNDLENAME_TEMPLATE] as Dictionary<string, object>;
					var bundleNameTemplate = new Dictionary<string, string>();
					foreach (var platform_package_key in bundleNameTemplateSource.Keys) bundleNameTemplate[platform_package_key] = bundleNameTemplateSource[platform_package_key] as string;
					
					var bundleUseOutputSource = nodeDict[AssetBundleGraphSettings.NODE_BUNDLIZER_USE_OUTPUT] as Dictionary<string, object>;
					var bundleUseOutput = new Dictionary<string, string>();
					foreach (var platform_package_key in bundleUseOutputSource.Keys) bundleUseOutput[platform_package_key] = bundleUseOutputSource[platform_package_key] as string; 
					
					var newNode = Node.GUINodeForBundlizer(currentNodesCount, name, id, kind, bundleNameTemplate, bundleUseOutput, x, y);
					
					var outputLabelsList = nodeDict[AssetBundleGraphSettings.NODE_OUTPUT_LABELS] as List<object>;
					foreach (var outputLabelSource in outputLabelsList) {
						var label = outputLabelSource as string;
						newNode.AddConnectionPoint(new OutputPoint(label));
					}
					return newNode;
				}

				case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					var bundleOptions = new Dictionary<string, List<string>>();

					var enabledBundleOptionsDict = nodeDict[AssetBundleGraphSettings.NODE_BUNDLEBUILDER_ENABLEDBUNDLEOPTIONS] as Dictionary<string, object>;
					foreach (var platform_package_key in enabledBundleOptionsDict.Keys) {
						var optionListSource = enabledBundleOptionsDict[platform_package_key] as List<object>;
						bundleOptions[platform_package_key] = new List<string>();

						foreach (var optionSource in optionListSource) bundleOptions[platform_package_key].Add(optionSource as string);
					}

					var newNode = Node.GUINodeForBundleBuilder(currentNodesCount, name, id, kind, bundleOptions, x, y);
					
					var outputLabelsList = nodeDict[AssetBundleGraphSettings.NODE_OUTPUT_LABELS] as List<object>;
					foreach (var outputLabelSource in outputLabelsList) {
						var label = outputLabelSource as string;
						newNode.AddConnectionPoint(new OutputPoint(label));
					}
					return newNode;
				}

				case AssetBundleGraphSettings.NodeKind.EXPORTER_GUI: {
					var exportPathSource = nodeDict[AssetBundleGraphSettings.NODE_EXPORTER_EXPORT_PATH] as Dictionary<string, object>;
					var exportPath = new Dictionary<string, string>();
					foreach (var platform_package_key in exportPathSource.Keys) exportPath[platform_package_key] = exportPathSource[platform_package_key] as string;

					var newNode = Node.ExporterNode(currentNodesCount, name, id, kind, exportPath, x, y);
					return newNode;
				}

				default: {
					Debug.LogError("kind not found:" + kind);
					break;
				}
			}

			Debug.LogError("failed to detect." + kindSource);
			// error. returns empty node.
			return new Node();
		}

		private Type IsAcceptableScriptType (Type type) {
			if (typeof(FilterBase).IsAssignableFrom(type)) return typeof(FilterBase);
			if (typeof(PrefabricatorBase).IsAssignableFrom(type)) return typeof(PrefabricatorBase);
			Debug.LogError("failed to accept:" + type);
			return null;
		}

		private void AddNodeFromCode (string scriptName, string scriptType, string scriptPath, Type scriptBaseType, string nodeId, float x, float y) {
			Node newNode = null;
			if (scriptBaseType == typeof(FilterBase)) {
				var kind = AssetBundleGraphSettings.NodeKind.FILTER_SCRIPT;
				newNode = Node.ScriptNode(nodes.Count, scriptName, nodeId, kind, scriptType, scriptPath, x, y);
				
				// add output point to this node.
				// setup this filter then add output point by result of setup.
				var outputPointLabels = GraphStackController.GetLabelsFromSetupFilter(scriptName);

				newNode.AddConnectionPoint(new InputPoint(AssetBundleGraphSettings.DEFAULT_INPUTPOINT_LABEL));
				foreach (var outputPointLabel in outputPointLabels) {
					newNode.AddConnectionPoint(new OutputPoint(outputPointLabel));
				}
			}
			
			if (scriptBaseType == typeof(PrefabricatorBase)) {
				var kind = AssetBundleGraphSettings.NodeKind.PREFABRICATOR_SCRIPT;
				newNode = Node.ScriptNode(nodes.Count, scriptName, nodeId, kind, scriptType, scriptPath, x, y);
				newNode.AddConnectionPoint(new InputPoint(AssetBundleGraphSettings.DEFAULT_INPUTPOINT_LABEL));
				newNode.AddConnectionPoint(new OutputPoint(AssetBundleGraphSettings.DEFAULT_OUTPUTPOINT_LABEL));
			}
			
			
			if (newNode == null) {
				Debug.LogError("failed to add node. no type found, scriptName:" + scriptName + " scriptPath:" + scriptPath + " scriptBaseType:" + scriptBaseType);
				return;
			}

			nodes.Add(newNode);
		}

		private void AddNodeFromGUI (string nodeName, AssetBundleGraphSettings.NodeKind kind, string nodeId, float x, float y) {
			Node newNode = null;

			if (string.IsNullOrEmpty(nodeName)) nodeName = AssetBundleGraphSettings.DEFAULT_NODE_NAME[kind] + nodes.Where(node => node.kind == kind).ToList().Count;
			
			switch (kind) {
				case AssetBundleGraphSettings.NodeKind.LOADER_GUI: {
					var default_platform_package_loadPath = new Dictionary<string, string> {
						{AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME, string.Empty}
					};

					newNode = Node.LoaderNode(nodes.Count, nodeName, nodeId, kind, default_platform_package_loadPath, x, y);
					newNode.AddConnectionPoint(new OutputPoint(AssetBundleGraphSettings.DEFAULT_OUTPUTPOINT_LABEL));
					break;
				}

				case AssetBundleGraphSettings.NodeKind.FILTER_GUI: {
					var newFilterKeywords = new List<string>();
					var newFilterKeytypes = new List<string>();
					newNode = Node.GUINodeForFilter(nodes.Count, nodeName, nodeId, kind, newFilterKeywords, newFilterKeytypes, x, y);
					newNode.AddConnectionPoint(new InputPoint(AssetBundleGraphSettings.DEFAULT_INPUTPOINT_LABEL));
					break;
				}
				
				case AssetBundleGraphSettings.NodeKind.IMPORTSETTING_GUI: {
					var importerPackages = new Dictionary<string, string> {
						{AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME, string.Empty}
					};

					newNode = Node.GUINodeForImport(nodes.Count, nodeName, nodeId, kind, importerPackages, x, y);
					newNode.AddConnectionPoint(new InputPoint(AssetBundleGraphSettings.DEFAULT_INPUTPOINT_LABEL));
					newNode.AddConnectionPoint(new OutputPoint(AssetBundleGraphSettings.DEFAULT_OUTPUTPOINT_LABEL));
					break;
				}
				
				case AssetBundleGraphSettings.NodeKind.GROUPING_GUI: {
					var newGroupingKeywords = new Dictionary<string, string> {
						{AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME, AssetBundleGraphSettings.GROUPING_KEYWORD_DEFAULT}
					};

					newNode = Node.GUINodeForGrouping(nodes.Count, nodeName, nodeId, kind, newGroupingKeywords, x, y);
					newNode.AddConnectionPoint(new InputPoint(AssetBundleGraphSettings.DEFAULT_INPUTPOINT_LABEL));
					newNode.AddConnectionPoint(new OutputPoint(AssetBundleGraphSettings.DEFAULT_OUTPUTPOINT_LABEL));
					break;
				}
				
				case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_GUI:{
					newNode = Node.GUINodeForPrefabricator(nodes.Count, nodeName, nodeId, kind, x, y);
					newNode.AddConnectionPoint(new InputPoint(AssetBundleGraphSettings.DEFAULT_INPUTPOINT_LABEL));
					newNode.AddConnectionPoint(new OutputPoint(AssetBundleGraphSettings.DEFAULT_OUTPUTPOINT_LABEL));
					break;
				}

				case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: {
					var newBundlizerKeyword = new Dictionary<string, string> {
						{AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME, AssetBundleGraphSettings.BUNDLIZER_BUNDLENAME_TEMPLATE_DEFAULT}
					};
					
					var newBundleUseOutput = new Dictionary<string, string> {
						{AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME, AssetBundleGraphSettings.BUNDLIZER_USEOUTPUT_DEFAULT}	
					};

					newNode = Node.GUINodeForBundlizer(nodes.Count, nodeName, nodeId, kind, newBundlizerKeyword, newBundleUseOutput, x, y);
					newNode.AddConnectionPoint(new InputPoint(AssetBundleGraphSettings.DEFAULT_INPUTPOINT_LABEL));
					newNode.AddConnectionPoint(new OutputPoint(AssetBundleGraphSettings.BUNDLIZER_BUNDLE_OUTPUTPOINT_LABEL));
					break;
				}

				case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					var bundleOptions = new Dictionary<string, List<string>>{
						{AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME, new List<string>()}
					};

					newNode = Node.GUINodeForBundleBuilder(nodes.Count, nodeName, nodeId, kind, bundleOptions, x, y);
					newNode.AddConnectionPoint(new InputPoint(AssetBundleGraphSettings.DEFAULT_INPUTPOINT_LABEL));
					newNode.AddConnectionPoint(new OutputPoint(AssetBundleGraphSettings.DEFAULT_OUTPUTPOINT_LABEL));
					break;
				}

				case AssetBundleGraphSettings.NodeKind.EXPORTER_GUI: {
					var default_platform_package_exportPath = new Dictionary<string, string> {
						{AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME, string.Empty}
					};

					newNode = Node.ExporterNode(nodes.Count, nodeName, nodeId, kind, default_platform_package_exportPath, x, y);
					newNode.AddConnectionPoint(new InputPoint(AssetBundleGraphSettings.DEFAULT_INPUTPOINT_LABEL));
					break;
				}
				default: {
					Debug.LogError("no kind match:" + kind);
					break;
				}
			}

			if (newNode == null) return;
			Undo.RecordObject(this, "Add Node");

			nodes.Add(newNode);
		}

		private void DrawStraightLineFromCurrentEventSourcePointTo (Vector2 to, OnNodeEvent eventSource) {
			if (eventSource == null) return;

			var p = eventSource.eventSourceNode.GlobalConnectionPointPosition(eventSource.eventSourceConnectionPoint);
			Handles.DrawLine(new Vector3(p.x, p.y, 0f), new Vector3(to.x, to.y, 0f));
		}

		private static string Prettify (string sourceJson) {
			var lines = sourceJson
				.Replace("{", "{\n").Replace("}", "\n}")
				.Replace("[", "[\n").Replace("]", "\n]")
				.Replace(",", ",\n")
				.Split('\n');

			Func<string, int, string> indents = (string baseLine, int indentDepth) => {
				var indentsStr = string.Empty;
				for (var i = 0; i < indentDepth; i++) indentsStr += "\t";
				return indentsStr + baseLine;
			};

			var indent = 0;
			for (var i = 0; i < lines.Length; i++) {
				var line = lines[i];

				// reduce indent for "}"
				if (line.Contains("}") || line.Contains("]")) {
					indent--;
				}

				/*
					adopt indent.
				*/
				lines[i] = indents(lines[i], indent);

				// indent continued all line after "{" 
				if (line.Contains("{") || line.Contains("[")) {
					indent++;
					continue;
				}

				
			}
			return string.Join("\n", lines);
		}

		private static void UpdateGraphData (Dictionary<string, object> data) {
			var dataStr = Json.Serialize(data);

			var prettified = Prettify(dataStr);
			
			var basePath = FileController.PathCombine(Application.dataPath, AssetBundleGraphSettings.ASSETNBUNDLEGRAPH_DATA_PATH);
			var graphDataPath = FileController.PathCombine(basePath, AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_NAME);
			using (var sw = new StreamWriter(graphDataPath)) {
				sw.Write(prettified);
			}
		}

		/**
			emit event from node-GUI.
		*/
		private void EmitNodeEvent (OnNodeEvent e) {
			switch (modifyMode) {
				case ModifyMode.CONNECT_STARTED: {
					switch (e.eventType) {
						/*
							handling
						*/
						case OnNodeEvent.EventType.EVENT_NODE_MOVING: {
							// do nothing.
							break;
						}

						/*
							connection drop detected from toward node.
						*/
						case OnNodeEvent.EventType.EVENT_NODE_CONNECTION_RAISED: {
							// finish connecting mode.
							modifyMode = ModifyMode.CONNECT_ENDED;
							
							if (currentEventSource == null) break;

							var sourceNode = currentEventSource.eventSourceNode;
							var sourceConnectionPoint = currentEventSource.eventSourceConnectionPoint;
							
							var targetNode = e.eventSourceNode;
							var targetConnectionPoint = e.eventSourceConnectionPoint;

							if (sourceNode.nodeId == targetNode.nodeId) break;

							if (!IsConnectablePointFromTo(sourceConnectionPoint, targetConnectionPoint)) break;

							var startNode = sourceNode;
							var startConnectionPoint = sourceConnectionPoint;
							var endNode = targetNode;
							var endConnectionPoint = targetConnectionPoint;

							// reverse if connected from input to output.
							if (sourceConnectionPoint.isInput) {
								startNode = targetNode;
								startConnectionPoint = targetConnectionPoint;
								endNode = sourceNode;
								endConnectionPoint = sourceConnectionPoint;
							}

							var label = startConnectionPoint.label;
							AddConnection(label, startNode, startConnectionPoint, endNode, endConnectionPoint);
							SaveGraphWithReload();
							break;
						}

						/*
							connection drop detected by started node.
						*/
						case OnNodeEvent.EventType.EVENT_NODE_CONNECTION_OVERED: {
							// finish connecting mode.
							modifyMode = ModifyMode.CONNECT_ENDED;
							
							/*
								connect when dropped target is connectable from start connectionPoint.
							*/
							var candidateNodes = NodesUnderPosition(e.globalMousePosition);

							if (!candidateNodes.Any()) break;
						
							var nodeUnderMouse = candidateNodes[0];

							// ignore if target node is source itself.
							if (nodeUnderMouse.nodeId == e.eventSourceNode.nodeId) break;

							var candidatePoints = nodeUnderMouse.ConnectionPointUnderGlobalPos(e.globalMousePosition);

							if (!candidatePoints.Any()) break;

							var sourcePoint = currentEventSource.eventSourceConnectionPoint;
							
							// limit by connectable or not.
							var connectableCandidates = candidatePoints.Where(point => IsConnectablePointFromTo(sourcePoint, point)).ToList();
							if (!connectableCandidates.Any()) break;

							// target point is determined.
							var connectablePoint = connectableCandidates.First();
							
							var startNode = e.eventSourceNode;
							var startConnectionPoint = currentEventSource.eventSourceConnectionPoint;
							var endNode = nodeUnderMouse;
							var endConnectionPoint = connectablePoint;

							// reverse if connected from input to output.
							if (startConnectionPoint.isInput) {
								startNode = nodeUnderMouse;
								startConnectionPoint = connectablePoint;
								endNode = e.eventSourceNode;
								endConnectionPoint = currentEventSource.eventSourceConnectionPoint;
							}

							var label = startConnectionPoint.label;
							AddConnection(label, startNode, startConnectionPoint, endNode, endConnectionPoint);
							SaveGraphWithReload();
							break;
						}

						default: {
							// Debug.Log("unconsumed or ignored event:" + e.eventType);
							modifyMode = ModifyMode.CONNECT_ENDED;
							break;
						}
					}
					break;
				}
				case ModifyMode.CONNECT_ENDED: {
					switch (e.eventType) {
						/*
							node move detected.
						*/
						case OnNodeEvent.EventType.EVENT_NODE_MOVING: {
							var tappedNode = e.eventSourceNode;
							var tappedNodeId = tappedNode.nodeId;
							
							if (activeObject.idPosDict.ContainsKey(tappedNodeId)) {
								// already active, do nothing for this node.
								var distancePos = tappedNode.GetPos() - activeObject.idPosDict.ReadonlyDict()[tappedNodeId];

								foreach (var node in nodes) {
									if (node.nodeId == tappedNodeId) continue;
									if (!activeObject.idPosDict.ContainsKey(node.nodeId)) continue;
									var relativePos = activeObject.idPosDict.ReadonlyDict()[node.nodeId] + distancePos;
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

							Undo.RecordObject(this, "Select Node");
							activeObject = RenewActiveObject(new List<string>{tappedNodeId});
							UpdateActivationOfObjects(activeObject);
							break;
						}

						/*
							start connection handling.
						*/
						case OnNodeEvent.EventType.EVENT_NODE_CONNECT_STARTED: {
							modifyMode = ModifyMode.CONNECT_STARTED;
							currentEventSource = e;
							break;
						}

						case OnNodeEvent.EventType.EVENT_CLOSE_TAPPED: {
							
							Undo.RecordObject(this, "Delete Node");
							
							var deletingNodeId = e.eventSourceNode.nodeId;
							DeleteNode(deletingNodeId);

							SaveGraphWithReload();
							InitializeGraph();
							break;
						}

						/*
							releasse detected.
								node move over.
								node tapped.
						*/
						case OnNodeEvent.EventType.EVENT_NODE_TOUCHED: {
							var movedNode = e.eventSourceNode;
							var movedNodeId = movedNode.nodeId;

							// already active, node(s) are just tapped or moved.
							if (activeObject.idPosDict.ContainsKey(movedNodeId)) {

								/*
									active nodes(contains tap released node) are possibly moved.
								*/
								var movedIdPosDict = new Dictionary<string, Vector2>();
								foreach (var node in nodes) {
									if (!activeObject.idPosDict.ContainsKey(node.nodeId)) continue;

									var startPos = activeObject.idPosDict.ReadonlyDict()[node.nodeId];
									if (node.GetPos() != startPos) {
										// moved.
										movedIdPosDict[node.nodeId] = node.GetPos();
									}
								}

								if (movedIdPosDict.Any()) {
									
									foreach (var node in nodes) {
										if (activeObject.idPosDict.ReadonlyDict().Keys.Contains(node.nodeId)) {
											var startPos = activeObject.idPosDict.ReadonlyDict()[node.nodeId];
											node.SetPos(startPos);
										}
									}

									Undo.RecordObject(this, "Move Node");

									foreach (var node in nodes) {
										if (movedIdPosDict.Keys.Contains(node.nodeId)) {
											var endPos = movedIdPosDict[node.nodeId];
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
								SaveGraph();
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
								SaveGraph();
								break;
							}
							
							Undo.RecordObject(this, "Select Node");

							activeObject = RenewActiveObject(new List<string>{movedNodeId});
							UpdateActivationOfObjects(activeObject);

							UpdateSpacerRect();
							SaveGraph();
							break;
						}

						default: {
							// Debug.Log("unconsumed or ignored event:" + e.eventType);
							break;
						}
					}
					break;
				}
			}

			switch (e.eventType) {
				case OnNodeEvent.EventType.EVENT_CONNECTIONPOINT_DELETED: {
					var deletedConnectionPoint = e.eventSourceConnectionPoint;
					var deletedOutputPointConnections = connections.Where(con => con.outputPoint.pointId == deletedConnectionPoint.pointId).ToList();
					
					if (!deletedOutputPointConnections.Any()) break;

					connections.Remove(deletedOutputPointConnections[0]);
					break;
				}
				case OnNodeEvent.EventType.EVENT_CONNECTIONPOINT_LABELCHANGED: {
					var labelChangedConnectionPoint = e.eventSourceConnectionPoint;
					var changedLabel = labelChangedConnectionPoint.label;

					var labelChangedOutputPointConnections = connections.Where(con => con.outputPoint.pointId == labelChangedConnectionPoint.pointId).ToList();

					if (!labelChangedOutputPointConnections.Any()) break;

					labelChangedOutputPointConnections[0].label = changedLabel;
					break;
				}
				case OnNodeEvent.EventType.EVENT_BEFORESAVE: {
					Undo.RecordObject(this, "Update Node Setting");
					break;
				}
				case OnNodeEvent.EventType.EVENT_SAVE: {
					SaveGraphWithReloadSilent();
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

		public void DuplicateNode (Node node) {
			var newNode = node.DuplicatedNode(
				nodes.Count,
				node.GetX() + 10f,
				node.GetY() + 10f
			);

			switch (newNode.kind) {
				case AssetBundleGraphSettings.NodeKind.LOADER_GUI: {
					newNode.AddConnectionPoint(new OutputPoint(AssetBundleGraphSettings.DEFAULT_OUTPUTPOINT_LABEL));
					break;
				}

				case AssetBundleGraphSettings.NodeKind.FILTER_GUI: {
					newNode.AddConnectionPoint(new InputPoint(AssetBundleGraphSettings.DEFAULT_INPUTPOINT_LABEL));
					foreach (var outputPointLabel in newNode.filterContainsKeywords) {
						newNode.AddConnectionPoint(new OutputPoint(outputPointLabel));
					}
					break;
				}
				
				case AssetBundleGraphSettings.NodeKind.IMPORTSETTING_GUI: {
					newNode.AddConnectionPoint(new InputPoint(AssetBundleGraphSettings.DEFAULT_INPUTPOINT_LABEL));
					newNode.AddConnectionPoint(new OutputPoint(AssetBundleGraphSettings.DEFAULT_OUTPUTPOINT_LABEL));
					break;
				}
				
				case AssetBundleGraphSettings.NodeKind.GROUPING_GUI: {
					newNode.AddConnectionPoint(new InputPoint(AssetBundleGraphSettings.DEFAULT_INPUTPOINT_LABEL));
					newNode.AddConnectionPoint(new OutputPoint(AssetBundleGraphSettings.DEFAULT_OUTPUTPOINT_LABEL));
					break;
				}
				
				case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_GUI:{
					newNode.AddConnectionPoint(new InputPoint(AssetBundleGraphSettings.DEFAULT_INPUTPOINT_LABEL));
					newNode.AddConnectionPoint(new OutputPoint(AssetBundleGraphSettings.DEFAULT_OUTPUTPOINT_LABEL));
					break;
				}

				case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: {
					newNode.AddConnectionPoint(new InputPoint(AssetBundleGraphSettings.DEFAULT_INPUTPOINT_LABEL));
					newNode.AddConnectionPoint(new OutputPoint(AssetBundleGraphSettings.DEFAULT_OUTPUTPOINT_LABEL));
					break;
				}

				case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					newNode.AddConnectionPoint(new InputPoint(AssetBundleGraphSettings.DEFAULT_INPUTPOINT_LABEL));
					newNode.AddConnectionPoint(new OutputPoint(AssetBundleGraphSettings.BUNDLIZER_BUNDLE_OUTPUTPOINT_LABEL));
					break;
				}

				case AssetBundleGraphSettings.NodeKind.EXPORTER_GUI: {
					newNode.AddConnectionPoint(new InputPoint(AssetBundleGraphSettings.DEFAULT_INPUTPOINT_LABEL));
					break;
				}
				default: {
					Debug.LogError("no kind match:" + newNode.kind);
					break;
				}
			}

			nodes.Add(newNode);
		}

		public void DeleteNode (string deletingNodeId) {
			var deletedNodeIndex = nodes.FindIndex(node => node.nodeId == deletingNodeId);
			if (0 <= deletedNodeIndex) {
				nodes[deletedNodeIndex].SetInactive();
				nodes.RemoveAt(deletedNodeIndex);
			}
		}

		public void EmitConnectionEvent (OnConnectionEvent e) {
			switch (modifyMode) {
				case ModifyMode.CONNECT_ENDED: {
					switch (e.eventType) {
						
						case OnConnectionEvent.EventType.EVENT_CONNECTION_TAPPED: {
							
							if (Event.current.shift) {
								Undo.RecordObject(this, "Select Objects");

								var objectId = string.Empty;

								if (e.eventSourceCon != null) {
									objectId = e.eventSourceCon.connectionId;
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

							var tappedConnectionId = e.eventSourceCon.connectionId;
							foreach (var con in connections) {
								if (con.connectionId == tappedConnectionId) {
									con.SetActive();
									activeObject = RenewActiveObject(new List<string>{con.connectionId});
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
						case OnConnectionEvent.EventType.EVENT_CONNECTION_DELETED: {
							
							Undo.RecordObject(this, "Delete Connection");

							var deletedConnectionId = e.eventSourceCon.connectionId;

							DeleteConnectionById(deletedConnectionId);

							SaveGraphWithReload();
							Repaint();
							break;
						}
						default: {
							// Debug.Log("unconsumed or ignored event:" + e.eventType);
							break;
						}
					}
					break;
				}
			}
		}

		private void UpdateActivationOfObjects (ActiveObject currentActiveObject) {
			foreach (var node in nodes) {
				if (currentActiveObject.idPosDict.ContainsKey(node.nodeId)) {
					node.SetActive();
					continue;
				}
				
				node.SetInactive();
			}

			foreach (var connection in connections) {
				if (currentActiveObject.idPosDict.ContainsKey(connection.connectionId)) {
					connection.SetActive();
					continue;
				}
				
				connection.SetInactive();
			}
		}

		/**
			create new connection if same relationship is not exist yet.
		*/
		private void AddConnection (string label, Node startNode, ConnectionPoint startPoint, Node endNode, ConnectionPoint endPoint) {
			
			Undo.RecordObject(this, "Add Connection");

			var connectionsFromThisNode = connections
				.Where(con => con.startNodeId == startNode.nodeId)
				.Where(con => con.outputPoint == startPoint)
				.ToList();
			if (connectionsFromThisNode.Any()) {
				var alreadyExistConnection = connectionsFromThisNode[0];
				DeleteConnectionById(alreadyExistConnection.connectionId);
			}

			if (!connections.ContainsConnection(startNode, startPoint, endNode, endPoint)) {
				connections.Add(Connection.NewConnection(label, startNode.nodeId, startPoint, endNode.nodeId, endPoint));
			}
		}

		private List<Node> NodesUnderPosition (Vector2 pos) {
			return nodes.Where(n => n.ConitainsGlobalPos(pos)).ToList();
		}

		private bool IsConnectablePointFromTo (ConnectionPoint sourcePoint, ConnectionPoint destPoint) {
			if (sourcePoint.isOutput != destPoint.isOutput && sourcePoint.isInput != destPoint.isInput) {
				return true;
			}
			return false;
		}

		private void DeleteConnectionById (string connectionId) {
			var deletedConnectionIndex = connections.FindIndex(con => con.connectionId == connectionId);
			if (0 <= deletedConnectionIndex) {
				connections[deletedConnectionIndex].SetInactive();
				connections.RemoveAt(deletedConnectionIndex);
			}
		}
	}
}
