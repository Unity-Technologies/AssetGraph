using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using MiniJSONForAssetGraph;
 

namespace AssetGraph {
	public class AssetGraph : EditorWindow {
		[MenuItem(AssetGraphSettings.GUI_TEXT_MENU_OPEN)]
		public static void Open () {
			GetWindow<AssetGraph>();
		}

		public void OnFocus () {
			// update handlers. these static handlers are erase when window is full-screened and badk to normal window.
			Node.Emit = EmitNodeEvent;
			Connection.Emit = EmitConnectionEvent;
		}

		public void OnEnable () {
			this.titleContent = new GUIContent("AssetGraph");

			Undo.undoRedoPerformed += () => {
				SaveGraphWithReload();
				Repaint();
			};

			Node.Emit = EmitNodeEvent;
			Connection.Emit = EmitConnectionEvent;

			LoadTextures();

			InitializeGraph();
			Setup(package);


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

		private Dictionary<string,Dictionary<string, List<string>>> connectionThroughputs = new Dictionary<string, Dictionary<string, List<string>>>();


		[Serializable] public struct ActiveObject {
			[SerializeField] public Dictionary<string, Vector2> idPosDict;
			
			public ActiveObject (Dictionary<string, Vector2> idPosDict) {
				this.idPosDict = new Dictionary<string, Vector2>(idPosDict);

			}
		}
		[SerializeField] private ActiveObject activeObject = new ActiveObject(new Dictionary<string, Vector2>());
		
		// hold selection start data.
		public struct Selection {
			public readonly float x;
			public readonly float y;

			public Selection (Vector2 position) {
				this.x = position.x;
				this.y = position.y;
			}
		}
		private Selection selection;

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
		

		private string package = string.Empty;

		private void LoadTextures () {
			// load shared node textures
			Node.inputPointTex = AssetDatabase.LoadAssetAtPath(AssetGraphGUISettings.RESOURCE_INPUT_BG, typeof(Texture2D)) as Texture2D;
			Node.outputPointTex = AssetDatabase.LoadAssetAtPath(AssetGraphGUISettings.RESOURCE_OUTPUT_BG, typeof(Texture2D)) as Texture2D;

			Node.enablePointMarkTex = AssetDatabase.LoadAssetAtPath(AssetGraphGUISettings.RESOURCE_CONNECTIONPOINT_ENABLE, typeof(Texture2D)) as Texture2D;

			Node.inputPointMarkTex = AssetDatabase.LoadAssetAtPath(AssetGraphGUISettings.RESOURCE_CONNECTIONPOINT_INPUT, typeof(Texture2D)) as Texture2D;
			Node.outputPointMarkTex = AssetDatabase.LoadAssetAtPath(AssetGraphGUISettings.RESOURCE_CONNECTIONPOINT_OUTPUT, typeof(Texture2D)) as Texture2D;
			Node.outputPointMarkConnectedTex = AssetDatabase.LoadAssetAtPath(AssetGraphGUISettings.RESOURCE_CONNECTIONPOINT_OUTPUT_CONNECTED, typeof(Texture2D)) as Texture2D;

			SetupPlatformIconsAndStrings(out Node.platformButtonTextures, out Node.platformStrings);

			// load shared connection textures
			Connection.connectionArrowTex = AssetDatabase.LoadAssetAtPath(AssetGraphGUISettings.RESOURCE_ARROW, typeof(Texture2D)) as Texture2D;

			// load other textures
			reloadButtonTexture = UnityEditor.EditorGUIUtility.IconContent("RotateTool");
			selectionTex = AssetDatabase.LoadAssetAtPath(AssetGraphGUISettings.RESOURCE_SELECTION, typeof(Texture2D)) as Texture2D;
		}

		private static void SetupPlatformIconsAndStrings (out Texture2D[] platformTextures, out string[] platformNames) {
			var assetGraphPlatformSettings = AssetGraphPlatformSettings.platforms;
			
			var platformStringList = new List<string>();
			var platformTexList = new List<Texture2D>();
			
			platformStringList.Add("Default");
			platformTexList.Add(GetPlatformIcon("BuildSettings.Web"));//dummy.

			if (assetGraphPlatformSettings.Contains("Web")) {
				platformStringList.Add("Web");
				platformTexList.Add(GetPlatformIcon("BuildSettings.Web"));
			}
			if (assetGraphPlatformSettings.Contains("Standalone")) {
				platformStringList.Add("Standalone");
				platformTexList.Add(GetPlatformIcon("BuildSettings.Standalone"));
			}
			if (assetGraphPlatformSettings.Contains("iPhone") || assetGraphPlatformSettings.Contains("iOS")) {// iPhone or iOS converted to iOS.
				platformStringList.Add("iOS");
				platformTexList.Add(GetPlatformIcon("BuildSettings.iPhone"));
			}
			if (assetGraphPlatformSettings.Contains("Android")) {
				platformStringList.Add("Android");
				platformTexList.Add(GetPlatformIcon("BuildSettings.Android"));
			}
			if (assetGraphPlatformSettings.Contains("BlackBerry")) {
				platformStringList.Add("BlackBerry");
				platformTexList.Add(GetPlatformIcon("BuildSettings.BlackBerry"));
			}
			if (assetGraphPlatformSettings.Contains("Tizen")) {
				platformStringList.Add("Tizen");
				platformTexList.Add(GetPlatformIcon("BuildSettings.Tizen"));
			}
			if (assetGraphPlatformSettings.Contains("XBox360")) {
				platformStringList.Add("XBox360");
				platformTexList.Add(GetPlatformIcon("BuildSettings.XBox360"));
			}
			if (assetGraphPlatformSettings.Contains("XboxOne")) {
				platformStringList.Add("XboxOne");
				platformTexList.Add(GetPlatformIcon("BuildSettings.XboxOne"));
			}
			if (assetGraphPlatformSettings.Contains("PS3")) {
				platformStringList.Add("PS3");
				platformTexList.Add(GetPlatformIcon("BuildSettings.PS3"));
			}
			if (assetGraphPlatformSettings.Contains("PSP2")) {
				platformStringList.Add("PSP2");
				platformTexList.Add(GetPlatformIcon("BuildSettings.PSP2"));
			}
			if (assetGraphPlatformSettings.Contains("PS4")) {
				platformStringList.Add("PS4");
				platformTexList.Add(GetPlatformIcon("BuildSettings.PS4"));
			}
			if (assetGraphPlatformSettings.Contains("StandaloneGLESEmu")) {
				platformStringList.Add("StandaloneGLESEmu");
				platformTexList.Add(GetPlatformIcon("BuildSettings.StandaloneGLESEmu"));
			}
			if (assetGraphPlatformSettings.Contains("Metro")) {
				platformStringList.Add("Metro");
				platformTexList.Add(GetPlatformIcon("BuildSettings.Metro"));
			}
			if (assetGraphPlatformSettings.Contains("WP8")) {
				platformStringList.Add("WP8");
				platformTexList.Add(GetPlatformIcon("BuildSettings.WP8"));
			}
			if (assetGraphPlatformSettings.Contains("WebGL")) {
				platformStringList.Add("WebGL");
				platformTexList.Add(GetPlatformIcon("BuildSettings.WebGL"));
			}
			if (assetGraphPlatformSettings.Contains("SamsungTV")) {
				platformStringList.Add("SamsungTV");
				platformTexList.Add(GetPlatformIcon("BuildSettings.SamsungTV"));
			}

			platformTextures = platformTexList.ToArray();
			platformNames = platformStringList.ToArray();
		}

		private static Texture2D GetPlatformIcon(string locTitle) {
			return EditorGUIUtility.IconContent(locTitle + ".Small").image as Texture2D;
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
			node window initializer.
			setup nodes, points and connections from saved data.
		*/
		public void InitializeGraph () {
			var basePath = FileController.PathCombine(Application.dataPath, AssetGraphSettings.ASSETGRAPH_DATA_PATH);
			
			// create Temp folder under Assets/AssetGraph
			if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);

			var graphDataPath = FileController.PathCombine(basePath, AssetGraphSettings.ASSETGRAPH_DATA_NAME);


			var deserialized = new Dictionary<string, object>();
			var lastModified = DateTime.Now;

			// default package is empty.
			package = string.Empty;

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

				var lastModifiedStr = deserialized[AssetGraphSettings.ASSETGRAPH_DATA_LASTMODIFIED] as string;
				lastModified = Convert.ToDateTime(lastModifiedStr);

				var lastPackageStr = deserialized[AssetGraphSettings.ASSETGRAPH_DATA_LASTPACKAGE] as string;
				if (string.IsNullOrEmpty(lastPackageStr)) lastPackageStr = string.Empty;
				package = lastPackageStr;

				var validatedDataDict = GraphStackController.ValidateStackedGraph(deserialized);

				var validatedDate = validatedDataDict[AssetGraphSettings.ASSETGRAPH_DATA_LASTMODIFIED] as string;
				if (lastModifiedStr != validatedDate) {
					// save validated graph data.
					UpdateGraphData(validatedDataDict);

					// reload
					var dataStr2 = string.Empty;
					using (var sr = new StreamReader(graphDataPath)) {
						dataStr2 = sr.ReadToEnd();
					}

					deserialized = Json.Deserialize(dataStr2) as Dictionary<string, object>;

					var lastModifiedStr2 = deserialized[AssetGraphSettings.ASSETGRAPH_DATA_LASTMODIFIED] as string;
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
			ConstructGraphFromDeserializedData(deserialized, package);
		}

		private Dictionary<string, object> RenewData () {
			// renew
			var graphData = new Dictionary<string, object>{
				{AssetGraphSettings.ASSETGRAPH_DATA_LASTMODIFIED, DateTime.Now.ToString()},
				{AssetGraphSettings.ASSETGRAPH_DATA_NODES, new List<object>()},
				{AssetGraphSettings.ASSETGRAPH_DATA_CONNECTIONS, new List<object>()},
				{AssetGraphSettings.ASSETGRAPH_DATA_LASTPACKAGE, string.Empty}
			};

			// save new empty graph data.
			UpdateGraphData(graphData);

			return graphData;
		}

		private void ConstructGraphFromDeserializedData (Dictionary<string, object> deserializedData, string currentPackage) {
			nodes = new List<Node>();
			connections = new List<Connection>();

			var nodesSource = deserializedData[AssetGraphSettings.ASSETGRAPH_DATA_NODES] as List<object>;
			
			foreach (var nodeDictSource in nodesSource) {
				var nodeDict = nodeDictSource as Dictionary<string, object>;
				var name = nodeDict[AssetGraphSettings.NODE_NAME] as string;
				var id = nodeDict[AssetGraphSettings.NODE_ID] as string;
				var kindSource = nodeDict[AssetGraphSettings.NODE_KIND] as string;

				var kind = AssetGraphSettings.NodeKindFromString(kindSource);
				
				var posDict = nodeDict[AssetGraphSettings.NODE_POS] as Dictionary<string, object>;
				var x = (float)Convert.ToInt32(posDict[AssetGraphSettings.NODE_POS_X]);
				var y = (float)Convert.ToInt32(posDict[AssetGraphSettings.NODE_POS_Y]);		

				switch (kind) {
					case AssetGraphSettings.NodeKind.LOADER_GUI: {
						var loadPathSource = nodeDict[AssetGraphSettings.NODE_LOADER_LOAD_PATH] as Dictionary<string, object>;
						var loadPath = new Dictionary<string, string>();
						foreach (var platform_package_key in loadPathSource.Keys) loadPath[platform_package_key] = loadPathSource[platform_package_key] as string;

						var newNode = Node.LoaderNode(nodes.Count, name, id, kind, loadPath, x, y);
						CollectPackage(loadPath.Keys.ToList());

						var outputLabelsList = nodeDict[AssetGraphSettings.NODE_OUTPUT_LABELS] as List<object>;
						foreach (var outputLabelSource in outputLabelsList) {
							var label = outputLabelSource as string;
							newNode.AddConnectionPoint(new OutputPoint(label));
						}

						nodes.Add(newNode);
						break;
					}
					case AssetGraphSettings.NodeKind.FILTER_SCRIPT:
					case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT:

					case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
					case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI:

					case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT: {
						var scriptType = nodeDict[AssetGraphSettings.NODE_SCRIPT_TYPE] as string;
						var scriptPath = nodeDict[AssetGraphSettings.NODE_SCRIPT_PATH] as string;

						var newNode = Node.ScriptNode(nodes.Count, name, id, kind, scriptType, scriptPath, x, y);

						var outputLabelsList = nodeDict[AssetGraphSettings.NODE_OUTPUT_LABELS] as List<object>;
						foreach (var outputLabelSource in outputLabelsList) {
							var label = outputLabelSource as string;
							newNode.AddConnectionPoint(new OutputPoint(label));
						}

						nodes.Add(newNode);
						break;
					}

					case AssetGraphSettings.NodeKind.FILTER_GUI: {
						var filterContainsKeywordsSource = nodeDict[AssetGraphSettings.NODE_FILTER_CONTAINS_KEYWORDS] as List<object>;
						var filterContainsKeywords = new List<string>();
						foreach (var filterContainsKeywordSource in filterContainsKeywordsSource) {
							filterContainsKeywords.Add(filterContainsKeywordSource.ToString());
						}

						var newNode = Node.GUINodeForFilter(nodes.Count, name, id, kind, filterContainsKeywords, x, y);

						var outputLabelsList = nodeDict[AssetGraphSettings.NODE_OUTPUT_LABELS] as List<object>;
						foreach (var outputLabelSource in outputLabelsList) {
							var label = outputLabelSource as string;
							newNode.AddConnectionPoint(new OutputPoint(label));
						}

						nodes.Add(newNode);
						break;
					}

					case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
						var defaultPlatformAndPackagesSource = nodeDict[AssetGraphSettings.NODE_IMPORTER_PACKAGES] as Dictionary<string, object>;
						var defaultPlatformAndPackages = new Dictionary<string, string>();
						foreach (var platform_package_key in defaultPlatformAndPackagesSource.Keys) defaultPlatformAndPackages[platform_package_key] = defaultPlatformAndPackagesSource[platform_package_key] as string;

						var newNode = Node.GUINodeForImport(nodes.Count, name, id, kind, defaultPlatformAndPackages, x, y);
						CollectPackage(defaultPlatformAndPackages.Keys.ToList());

						var outputLabelsList = nodeDict[AssetGraphSettings.NODE_OUTPUT_LABELS] as List<object>;
						foreach (var outputLabelSource in outputLabelsList) {
							var label = outputLabelSource as string;
							newNode.AddConnectionPoint(new OutputPoint(label));
						}

						nodes.Add(newNode);
						break;
					}

					case AssetGraphSettings.NodeKind.GROUPING_GUI: {
						var groupingKeywordSource = nodeDict[AssetGraphSettings.NODE_GROUPING_KEYWORD] as Dictionary<string, object>;
						var groupingKeyword = new Dictionary<string, string>();
						foreach (var platform_package_key in groupingKeywordSource.Keys) groupingKeyword[platform_package_key] = groupingKeywordSource[platform_package_key] as string;

						var newNode = Node.GUINodeForGrouping(nodes.Count, name, id, kind, groupingKeyword, x, y);
						CollectPackage(groupingKeyword.Keys.ToList());

						var outputLabelsList = nodeDict[AssetGraphSettings.NODE_OUTPUT_LABELS] as List<object>;
						foreach (var outputLabelSource in outputLabelsList) {
							var label = outputLabelSource as string;
							newNode.AddConnectionPoint(new OutputPoint(label));
						}

						nodes.Add(newNode);
						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
						var bundleNameTemplateSource = nodeDict[AssetGraphSettings.NODE_BUNDLIZER_BUNDLENAME_TEMPLATE] as Dictionary<string, object>;
						var bundleNameTemplate = new Dictionary<string, string>();
						foreach (var platform_package_key in bundleNameTemplateSource.Keys) bundleNameTemplate[platform_package_key] = bundleNameTemplateSource[platform_package_key] as string;

						var newNode = Node.GUINodeForBundlizer(nodes.Count, name, id, kind, bundleNameTemplate, x, y);
						CollectPackage(bundleNameTemplate.Keys.ToList());

						var outputLabelsList = nodeDict[AssetGraphSettings.NODE_OUTPUT_LABELS] as List<object>;
						foreach (var outputLabelSource in outputLabelsList) {
							var label = outputLabelSource as string;
							newNode.AddConnectionPoint(new OutputPoint(label));
						}

						nodes.Add(newNode);
						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
						var bundleOptions = new Dictionary<string, List<string>>();

						var enabledBundleOptionsDict = nodeDict[AssetGraphSettings.NODE_BUNDLEBUILDER_ENABLEDBUNDLEOPTIONS] as Dictionary<string, object>;
						foreach (var platform_package_key in enabledBundleOptionsDict.Keys) {
							var optionListSource = enabledBundleOptionsDict[platform_package_key] as List<object>;
							bundleOptions[platform_package_key] = new List<string>();

							foreach (var optionSource in optionListSource) bundleOptions[platform_package_key].Add(optionSource as string);
						}

						var newNode = Node.GUINodeForBundleBuilder(nodes.Count, name, id, kind, bundleOptions, x, y);
						CollectPackage(bundleOptions.Keys.ToList());

						var outputLabelsList = nodeDict[AssetGraphSettings.NODE_OUTPUT_LABELS] as List<object>;
						foreach (var outputLabelSource in outputLabelsList) {
							var label = outputLabelSource as string;
							newNode.AddConnectionPoint(new OutputPoint(label));
						}

						nodes.Add(newNode);
						break;
					}

					case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
						var exportPathSource = nodeDict[AssetGraphSettings.NODE_EXPORTER_EXPORT_PATH] as Dictionary<string, object>;
						var exportPath = new Dictionary<string, string>();
						foreach (var platform_package_key in exportPathSource.Keys) exportPath[platform_package_key] = exportPathSource[platform_package_key] as string;

						var newNode = Node.ExporterNode(nodes.Count, name, id, kind, exportPath, x, y);
						CollectPackage(exportPath.Keys.ToList());

						nodes.Add(newNode);
						break;
					}

					default: {
						Debug.LogError("kind not found:" + kind);
						break;
					}

				}
			}


			// add default input if node is not NodeKind.SOURCE.
			foreach (var node in nodes) {
				if (node.kind == AssetGraphSettings.NodeKind.LOADER_GUI) continue;
				node.AddConnectionPoint(new InputPoint(AssetGraphSettings.DEFAULT_INPUTPOINT_LABEL));
			}

			// load connections
			var connectionsSource = deserializedData[AssetGraphSettings.ASSETGRAPH_DATA_CONNECTIONS] as List<object>;
			foreach (var connectionSource in connectionsSource) {
				var connectionDict = connectionSource as Dictionary<string, object>;
				var label = connectionDict[AssetGraphSettings.CONNECTION_LABEL] as string;
				var connectionId = connectionDict[AssetGraphSettings.CONNECTION_ID] as string;
				var fromNodeId = connectionDict[AssetGraphSettings.CONNECTION_FROMNODE] as string;
				var toNodeId = connectionDict[AssetGraphSettings.CONNECTION_TONODE] as string;

				var startNodeCandidates = nodes.Where(node => node.nodeId == fromNodeId).ToList();
				if (!startNodeCandidates.Any()) continue;
				var startNode = startNodeCandidates[0];
				var startPoint = startNode.ConnectionPointFromLabel(label);

				var endNodeCandidates = nodes.Where(node => node.nodeId == toNodeId).ToList();
				if (!endNodeCandidates.Any()) continue;
				var endNode = endNodeCandidates[0];
				var endPoint = endNode.ConnectionPointFromLabel(AssetGraphSettings.DEFAULT_INPUTPOINT_LABEL);

				connections.Add(Connection.LoadConnection(label, connectionId, startNode.nodeId, startPoint, endNode.nodeId, endPoint));
			}
		}

		private void SaveGraph () {
			var nodeList = new List<Dictionary<string, object>>();
			foreach (var node in nodes) {
				var nodeDict = new Dictionary<string, object>();

				nodeDict[AssetGraphSettings.NODE_NAME] = node.name;
				nodeDict[AssetGraphSettings.NODE_ID] = node.nodeId;
				nodeDict[AssetGraphSettings.NODE_KIND] = node.kind;

				var outputLabels = node.OutputPointLabels();
				nodeDict[AssetGraphSettings.NODE_OUTPUT_LABELS] = outputLabels;

				var posDict = new Dictionary<string, int>();
				posDict[AssetGraphSettings.NODE_POS_X] = node.GetX();
				posDict[AssetGraphSettings.NODE_POS_Y] = node.GetY();

				nodeDict[AssetGraphSettings.NODE_POS] = posDict;

				switch (node.kind) {
					case AssetGraphSettings.NodeKind.LOADER_GUI: {
						nodeDict[AssetGraphSettings.NODE_LOADER_LOAD_PATH] = node.loadPath.ReadonlyDict();
						break;
					}
					case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
						nodeDict[AssetGraphSettings.NODE_EXPORTER_EXPORT_PATH] = node.exportPath.ReadonlyDict();
						break;
					}
					
					case AssetGraphSettings.NodeKind.FILTER_SCRIPT:
					case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT:
					case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
					case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT: {
						nodeDict[AssetGraphSettings.NODE_SCRIPT_TYPE] = node.scriptType;
						nodeDict[AssetGraphSettings.NODE_SCRIPT_PATH] = node.scriptPath;
						break;
					}

					case AssetGraphSettings.NodeKind.FILTER_GUI: {
						nodeDict[AssetGraphSettings.NODE_FILTER_CONTAINS_KEYWORDS] = node.filterContainsKeywords;
						break;
					}

					case AssetGraphSettings.NodeKind.IMPORTER_GUI:{
						nodeDict[AssetGraphSettings.NODE_IMPORTER_PACKAGES] = node.importerPackages.ReadonlyDict();
						break;
					}

					case AssetGraphSettings.NodeKind.GROUPING_GUI: {
						nodeDict[AssetGraphSettings.NODE_GROUPING_KEYWORD] = node.groupingKeyword.ReadonlyDict();
						break;
					}

					case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI: {
						nodeDict[AssetGraphSettings.NODE_SCRIPT_TYPE] = node.scriptType;
						nodeDict[AssetGraphSettings.NODE_SCRIPT_PATH] = node.scriptPath;
						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
						nodeDict[AssetGraphSettings.NODE_BUNDLIZER_BUNDLENAME_TEMPLATE] = node.bundleNameTemplate.ReadonlyDict();
						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
						nodeDict[AssetGraphSettings.NODE_BUNDLEBUILDER_ENABLEDBUNDLEOPTIONS] = node.enabledBundleOptions.ReadonlyDict();
						break;
					}

					default: {
						Debug.LogError("failed to match:" + node.kind);
						break;
					}
				}
				nodeList.Add(nodeDict);
			}

			var connectionList = new List<Dictionary<string, string>>();
			foreach (var connection in connections) {
				var connectionDict = new Dictionary<string, string>{
					{AssetGraphSettings.CONNECTION_LABEL, connection.label},
					{AssetGraphSettings.CONNECTION_ID, connection.connectionId},
					{AssetGraphSettings.CONNECTION_FROMNODE, connection.startNodeId},
					{AssetGraphSettings.CONNECTION_TONODE, connection.endNodeId}
				};
				connectionList.Add(connectionDict);
			}

			var graphData = new Dictionary<string, object>{
				{AssetGraphSettings.ASSETGRAPH_DATA_LASTMODIFIED, DateTime.Now.ToString()},
				{AssetGraphSettings.ASSETGRAPH_DATA_NODES, nodeList},
				{AssetGraphSettings.ASSETGRAPH_DATA_CONNECTIONS, connectionList},
				{AssetGraphSettings.ASSETGRAPH_DATA_LASTPACKAGE, package}
			};

			UpdateGraphData(graphData);
		}

		private void SaveGraphWithReload () {
			SaveGraph();
			Setup(package);
		}

		private void CollectPackage (List<string> platform_package_keys) {
			foreach (var platform_package_key in platform_package_keys) {
				var packageKey = GraphStackController.PackageKeyFromPlatform_Package_Key(platform_package_key);
				if (string.IsNullOrEmpty(packageKey)) continue;

				if (!Node.NodeSharedPackages.Contains(packageKey)) {
					Node.NodeSharedPackages.Add(packageKey);
				}
			}
		}

		private void Setup (string package) {
			var graphDataPath = FileController.PathCombine(Application.dataPath, AssetGraphSettings.ASSETGRAPH_DATA_PATH, AssetGraphSettings.ASSETGRAPH_DATA_NAME);
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
			connectionThroughputs = GraphStackController.SetupStackedGraph(reloadedData, package);

			Finally(connectionThroughputs, false);
		}

		private void Run (string package) {
			var graphDataPath = FileController.PathCombine(Application.dataPath, AssetGraphSettings.ASSETGRAPH_DATA_PATH, AssetGraphSettings.ASSETGRAPH_DATA_NAME);
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

			var currentCount = 0.00f;
			var totalCount = nodes.Count * 1f;

			Action<string, float> updateHandler = (nodeId, progress) => {
				var targetNodes = nodes.Where(node => node.nodeId == nodeId).ToList();
				
				var progressPercentage = ((currentCount/totalCount) * 100).ToString();
				
				if (progressPercentage.Contains(".")) progressPercentage = progressPercentage.Split('.')[0];
				
				if (0 < progress) {
					currentCount = currentCount + 1f;
				}

				if (targetNodes.Any()) {
					targetNodes.ForEach(
						node => {
							EditorUtility.DisplayProgressBar("AssetGraph Processing " + node.name + ".", progressPercentage + "%", currentCount/totalCount);
						}
					);
				}
			};

			var loadedData = Json.Deserialize(dataStr) as Dictionary<string, object>;
			
			// setup datas. fail if exception raise.
			GraphStackController.SetupStackedGraph(loadedData, package);
			

			// run datas.
			connectionThroughputs = GraphStackController.RunStackedGraph(loadedData, package, updateHandler);

			EditorUtility.ClearProgressBar();
			AssetDatabase.Refresh();

			Finally(connectionThroughputs, true);
		}


		public void Finally (Dictionary<string, Dictionary<string, List<string>>> throughputsSource, bool isRun) {
			var nodeThroughputs = NodeThroughputs(throughputsSource);

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

		/**
			collect node's result with node name.
			structure is:

			nodeNames
				groups
					resources
		*/
		private Dictionary<string, Dictionary<string, List<string>>> NodeThroughputs (Dictionary<string, Dictionary<string, List<string>>> throughputs) {
			var nodeDatas = new Dictionary<string, Dictionary<string, List<string>>>();

			var nodeIds = nodes.Select(node => node.nodeId).ToList();
			var connectionIds = connections.Select(con => con.connectionId).ToList();

			foreach (var nodeOrConnectionId in throughputs.Keys) {
				// get endpoint node result.
				if (nodeIds.Contains(nodeOrConnectionId)) {
					var targetNodeName = nodes.Where(node => node.nodeId == nodeOrConnectionId).Select(node => node.name).FirstOrDefault();
					
					var nodeThroughput = throughputs[nodeOrConnectionId];

					if (!nodeDatas.ContainsKey(targetNodeName)) nodeDatas[targetNodeName] = new Dictionary<string, List<string>>();
					foreach (var groupKey in nodeThroughput.Keys) {
						if (!nodeDatas[targetNodeName].ContainsKey(groupKey)) nodeDatas[targetNodeName][groupKey] = new List<string>();
						nodeDatas[targetNodeName][groupKey].AddRange(nodeThroughput[groupKey]);
					}
				}

				// get connection result.
				if (connectionIds.Contains(nodeOrConnectionId)) {
					var targetConnection = connections.Where(con => con.connectionId == nodeOrConnectionId).FirstOrDefault();
					var targetNodeName = nodes.Where(node => node.nodeId == targetConnection.startNodeId).Select(node => node.name).FirstOrDefault();
					
					var nodeThroughput = throughputs[nodeOrConnectionId];

					if (!nodeDatas.ContainsKey(targetNodeName)) nodeDatas[targetNodeName] = new Dictionary<string, List<string>>();
					foreach (var groupKey in nodeThroughput.Keys) {
						if (!nodeDatas[targetNodeName].ContainsKey(groupKey)) nodeDatas[targetNodeName][groupKey] = new List<string>();
						nodeDatas[targetNodeName][groupKey].AddRange(nodeThroughput[groupKey]);
					}
				}
			}

			return nodeDatas;
		}

		public void OnGUI () {
			using (new EditorGUILayout.HorizontalScope(GUI.skin.box)) {
				if (GUILayout.Button(reloadButtonTexture, GUILayout.Height(18))) {
					Setup(package);
				}

				if (GUILayout.Button("Build (active build target is " + EditorUserBuildSettings.activeBuildTarget + ")", GUILayout.Height(18))) {
					Run(package);
				}
				
				var packageStr = package;
				if (string.IsNullOrEmpty(packageStr)) packageStr = AssetGraphSettings.PLATFORM_NONE_PACKAGE;
				if (GUILayout.Button("Package:" + packageStr, "Popup", GUILayout.Width(200), GUILayout.Height(18))) {
					Action DefaultSelected = () => {
						package = string.Empty;
						SaveGraph();
						Setup(package);
					};

					Action<string> ExistSelected = (string newPackage) => {
						package = newPackage;
						SaveGraph();
						Setup(package);
					};
					
					Node.ShowPackageMenu(package, DefaultSelected, ExistSelected);
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
						con.DrawConnection(nodes, new Dictionary<string, List<string>>());
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

										selection = new Selection(Event.current.mousePosition);
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
									foreach (var alreadySelectedObjectId in activeObject.idPosDict.Keys) {
										if (!activeObjectIds.Contains(alreadySelectedObjectId)) activeObjectIds.Add(alreadySelectedObjectId);
									}
								} else {
									// do nothing, means cancel selections if nodes are not contained by selection.
								}


								Undo.RecordObject(this, "Select Objects");

								activeObject = RenewActiveObject(activeObjectIds);
								UpdateActivationOfObjects(activeObject);

								selection = new Selection(Vector2.zero);
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
					foreach (var menuItemStr in AssetGraphSettings.GUI_Menu_Item_TargetGUINodeDict.Keys) {
						var targetGUINodeNameStr = AssetGraphSettings.GUI_Menu_Item_TargetGUINodeDict[menuItemStr];
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
					
					if (activeObject.idPosDict.Any()) {
						Undo.RecordObject(this, "Unselect");

						foreach (var activeObjectId in activeObject.idPosDict.Keys) {
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

							if (!activeObject.idPosDict.Any()) break;

							Undo.RecordObject(this, "Delete Selection");

							foreach (var targetId in activeObject.idPosDict.Keys) {
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
							if (!activeObject.idPosDict.Any()) {
								break;
							}



							Debug.LogError("copy");
							Event.current.Use();
							break;
						}

						case "Cut": {
							if (!activeObject.idPosDict.Any()) {
								break;
							}

							Debug.LogError("cut");
							Event.current.Use();
							break;
						}

						case "Paste": {
							if (!activeObject.idPosDict.Any()) {
								break;
							}

							Debug.LogError("paste");
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

		private Type IsAcceptableScriptType (Type type) {
			if (typeof(FilterBase).IsAssignableFrom(type)) return typeof(FilterBase);
			if (typeof(ImporterBase).IsAssignableFrom(type)) return typeof(ImporterBase);
			if (typeof(PrefabricatorBase).IsAssignableFrom(type)) return typeof(PrefabricatorBase);
			if (typeof(BundlizerBase).IsAssignableFrom(type)) return typeof(BundlizerBase);
			Debug.LogError("failed to accept:" + type);
			return null;
		}

		private void AddNodeFromCode (string scriptName, string scriptType, string scriptPath, Type scriptBaseType, string nodeId, float x, float y) {
			Node newNode = null;
			if (scriptBaseType == typeof(FilterBase)) {
				var kind = AssetGraphSettings.NodeKind.FILTER_SCRIPT;
				newNode = Node.ScriptNode(nodes.Count, scriptName, nodeId, kind, scriptType, scriptPath, x, y);
				
				// add output point to this node.
				// setup this filter then add output point by result of setup.
				var outputPointLabels = GraphStackController.GetLabelsFromSetupFilter(scriptName);

				newNode.AddConnectionPoint(new InputPoint(AssetGraphSettings.DEFAULT_INPUTPOINT_LABEL));
				foreach (var outputPointLabel in outputPointLabels) {
					newNode.AddConnectionPoint(new OutputPoint(outputPointLabel));
				}
			}
			if (scriptBaseType == typeof(ImporterBase)) {
				var kind = AssetGraphSettings.NodeKind.IMPORTER_SCRIPT;
				newNode = Node.ScriptNode(nodes.Count, scriptName, nodeId, kind, scriptType, scriptPath, x, y);
				newNode.AddConnectionPoint(new InputPoint(AssetGraphSettings.DEFAULT_INPUTPOINT_LABEL));
				newNode.AddConnectionPoint(new OutputPoint(AssetGraphSettings.DEFAULT_OUTPUTPOINT_LABEL));
			}
			if (scriptBaseType == typeof(PrefabricatorBase)) {
				var kind = AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT;
				newNode = Node.ScriptNode(nodes.Count, scriptName, nodeId, kind, scriptType, scriptPath, x, y);
				newNode.AddConnectionPoint(new InputPoint(AssetGraphSettings.DEFAULT_INPUTPOINT_LABEL));
				newNode.AddConnectionPoint(new OutputPoint(AssetGraphSettings.DEFAULT_OUTPUTPOINT_LABEL));
			}
			if (scriptBaseType == typeof(BundlizerBase)) {
				var kind = AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT;
				newNode = Node.ScriptNode(nodes.Count, scriptName, nodeId, kind, scriptType, scriptPath, x, y);
				newNode.AddConnectionPoint(new InputPoint(AssetGraphSettings.DEFAULT_INPUTPOINT_LABEL));
				newNode.AddConnectionPoint(new OutputPoint(AssetGraphSettings.DEFAULT_OUTPUTPOINT_LABEL));
			}
			
			if (newNode == null) {
				Debug.LogError("failed to add node. no type found, scriptName:" + scriptName + " scriptPath:" + scriptPath + " scriptBaseType:" + scriptBaseType);
				return;
			}

			nodes.Add(newNode);
		}

		private void AddNodeFromGUI (string nodeName, AssetGraphSettings.NodeKind kind, string nodeId, float x, float y) {
			Node newNode = null;

			if (string.IsNullOrEmpty(nodeName)) nodeName = AssetGraphSettings.DEFAULT_NODE_NAME[kind] + nodes.Where(node => node.kind == kind).ToList().Count;
			
			switch (kind) {
				case AssetGraphSettings.NodeKind.LOADER_GUI: {
					var default_platform_package_loadPath = new Dictionary<string, string> {
						{AssetGraphSettings.PLATFORM_DEFAULT_NAME, RelativeProjectPath()}
					};

					newNode = Node.LoaderNode(nodes.Count, nodeName, nodeId, kind, default_platform_package_loadPath, x, y);
					newNode.AddConnectionPoint(new OutputPoint(AssetGraphSettings.DEFAULT_OUTPUTPOINT_LABEL));
					break;
				}

				case AssetGraphSettings.NodeKind.FILTER_GUI: {
					var newFilterKeywords = new List<string>();
					newNode = Node.GUINodeForFilter(nodes.Count, nodeName, nodeId, kind, newFilterKeywords, x, y);
					newNode.AddConnectionPoint(new InputPoint(AssetGraphSettings.DEFAULT_INPUTPOINT_LABEL));
					break;
				}
				
				case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
					var importerPackages = new Dictionary<string, string> {
						{AssetGraphSettings.PLATFORM_DEFAULT_NAME + AssetGraphSettings.package_SEPARATOR + AssetGraphSettings.PLATFORM_DEFAULT_PACKAGE, string.Empty}
					};

					newNode = Node.GUINodeForImport(nodes.Count, nodeName, nodeId, kind, importerPackages, x, y);
					newNode.AddConnectionPoint(new InputPoint(AssetGraphSettings.DEFAULT_INPUTPOINT_LABEL));
					newNode.AddConnectionPoint(new OutputPoint(AssetGraphSettings.DEFAULT_OUTPUTPOINT_LABEL));
					break;
				}

				case AssetGraphSettings.NodeKind.GROUPING_GUI: {
					var newGroupingKeywords = new Dictionary<string, string> {
						{AssetGraphSettings.PLATFORM_DEFAULT_NAME, AssetGraphSettings.GROUPING_KEYWORD_DEFAULT}
					};

					newNode = Node.GUINodeForGrouping(nodes.Count, nodeName, nodeId, kind, newGroupingKeywords, x, y);
					newNode.AddConnectionPoint(new InputPoint(AssetGraphSettings.DEFAULT_INPUTPOINT_LABEL));
					newNode.AddConnectionPoint(new OutputPoint(AssetGraphSettings.DEFAULT_OUTPUTPOINT_LABEL));
					break;
				}
				
				case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI:{
					newNode = Node.GUINodeForPrefabricator(nodes.Count, nodeName, nodeId, kind, x, y);
					newNode.AddConnectionPoint(new InputPoint(AssetGraphSettings.DEFAULT_INPUTPOINT_LABEL));
					newNode.AddConnectionPoint(new OutputPoint(AssetGraphSettings.DEFAULT_OUTPUTPOINT_LABEL));
					break;
				}

				case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
					var newBundlizerKeyword = new Dictionary<string, string> {
						{AssetGraphSettings.PLATFORM_DEFAULT_NAME, AssetGraphSettings.BUNDLIZER_BUNDLENAME_TEMPLATE_DEFAULT}
					};

					newNode = Node.GUINodeForBundlizer(nodes.Count, nodeName, nodeId, kind, newBundlizerKeyword, x, y);
					newNode.AddConnectionPoint(new InputPoint(AssetGraphSettings.DEFAULT_INPUTPOINT_LABEL));
					newNode.AddConnectionPoint(new OutputPoint(AssetGraphSettings.DEFAULT_OUTPUTPOINT_LABEL));
					break;
				}

				case AssetGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					var bundleOptions = new Dictionary<string, List<string>>{
						{AssetGraphSettings.PLATFORM_DEFAULT_NAME, new List<string>()}
					};

					newNode = Node.GUINodeForBundleBuilder(nodes.Count, nodeName, nodeId, kind, bundleOptions, x, y);
					newNode.AddConnectionPoint(new InputPoint(AssetGraphSettings.DEFAULT_INPUTPOINT_LABEL));
					newNode.AddConnectionPoint(new OutputPoint(AssetGraphSettings.DEFAULT_OUTPUTPOINT_LABEL));
					break;
				}

				case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
					var default_platform_package_exportPath = new Dictionary<string, string> {
						{AssetGraphSettings.PLATFORM_DEFAULT_NAME, RelativeProjectPath()}
					};

					newNode = Node.ExporterNode(nodes.Count, nodeName, nodeId, kind, default_platform_package_exportPath, x, y);
					newNode.AddConnectionPoint(new InputPoint(AssetGraphSettings.DEFAULT_INPUTPOINT_LABEL));
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

		private void UpdateGraphData (Dictionary<string, object> data) {
			var dataStr = Json.Serialize(data);
			var basePath = FileController.PathCombine(Application.dataPath, AssetGraphSettings.ASSETGRAPH_DATA_PATH);
			var graphDataPath = FileController.PathCombine(basePath, AssetGraphSettings.ASSETGRAPH_DATA_NAME);
			using (var sw = new StreamWriter(graphDataPath)) {
				sw.Write(dataStr);
			}
		}

		private string RelativeProjectPath () {
			var assetPath = Application.dataPath;
			return Directory.GetParent(assetPath).Name;
		}

		private void DuplicateNode (string sourceNodeId, float x, float y) {
			// add undo record.
			Undo.RecordObject(this, "Duplicate Node");

			Debug.LogError("パッケージ情報のコピーが必須、Nodeによっては実現できてる気がする。");
			var targetNodes = nodes.Where(node => node.nodeId == sourceNodeId).ToList();
			if (!targetNodes.Any()) return;

			foreach (var targetNode in targetNodes) {
				var id = Guid.NewGuid().ToString();
				var kind = targetNode.kind;
				var name = targetNode.name;
				Debug.LogWarning(".ReadonlyDict()つける旅");
				switch (kind) {
					case AssetGraphSettings.NodeKind.LOADER_GUI: {
						var loadPath = targetNode.loadPath.ReadonlyDict();

						var newNode = Node.LoaderNode(nodes.Count, name, id, kind, loadPath, x, y);

						var connectionPoints = targetNode.DuplicateConnectionPoints();
						foreach (var connectionPoint in connectionPoints) {
							newNode.AddConnectionPoint(connectionPoint);
						}
						
						nodes.Add(newNode);
						break;
					}
					case AssetGraphSettings.NodeKind.FILTER_SCRIPT:
					case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT:

					case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
					case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI:

					case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT: {
						var scriptType = targetNode.scriptType;
						var scriptPath = targetNode.scriptPath;

						var newNode = Node.ScriptNode(nodes.Count, name, id, kind, scriptType, scriptPath, x, y);

						var connectionPoints = targetNode.DuplicateConnectionPoints();
						foreach (var connectionPoint in connectionPoints) {
							newNode.AddConnectionPoint(connectionPoint);
						}
						
						nodes.Add(newNode);
						break;
					}

					case AssetGraphSettings.NodeKind.FILTER_GUI: {
						var filterContainsKeywords = targetNode.filterContainsKeywords;
						
						var newNode = Node.GUINodeForFilter(nodes.Count, name, id, kind, filterContainsKeywords.ToList(), x, y);

						var connectionPoints = targetNode.DuplicateConnectionPoints();
						foreach (var connectionPoint in connectionPoints) {
							newNode.AddConnectionPoint(connectionPoint);
						}
						
						nodes.Add(newNode);
						break;
					}

					case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
						var importerPackages = targetNode.importerPackages.ReadonlyDict();
						var newNode = Node.GUINodeForImport(nodes.Count, name, id, kind, importerPackages, x, y);

						var connectionPoints = targetNode.DuplicateConnectionPoints();
						foreach (var connectionPoint in connectionPoints) {
							newNode.AddConnectionPoint(connectionPoint);
						}
						
						nodes.Add(newNode);
						break;
					}

					case AssetGraphSettings.NodeKind.GROUPING_GUI: {
						var groupingKeyword = targetNode.groupingKeyword.ReadonlyDict();
						var newNode = Node.GUINodeForGrouping(nodes.Count, name, id, kind, groupingKeyword, x, y);

						var connectionPoints = targetNode.DuplicateConnectionPoints();
						foreach (var connectionPoint in connectionPoints) {
							newNode.AddConnectionPoint(connectionPoint);
						}
						
						nodes.Add(newNode);
						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
						var bundleNameTemplate = targetNode.bundleNameTemplate.ReadonlyDict();
						var newNode = Node.GUINodeForBundlizer(nodes.Count, name, id, kind, bundleNameTemplate, x, y);

						var connectionPoints = targetNode.DuplicateConnectionPoints();
						foreach (var connectionPoint in connectionPoints) {
							newNode.AddConnectionPoint(connectionPoint);
						}
						
						nodes.Add(newNode);
						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
						var bundleOptions = targetNode.enabledBundleOptions.ReadonlyDict();
						var newNode = Node.GUINodeForBundleBuilder(nodes.Count, name, id, kind, bundleOptions, x, y);

						var connectionPoints = targetNode.DuplicateConnectionPoints();
						foreach (var connectionPoint in connectionPoints) {
							newNode.AddConnectionPoint(connectionPoint);
						}
						
						nodes.Add(newNode);
						break;
					}

					case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
						var exportPath = targetNode.exportPath.ReadonlyDict();
						var newNode = Node.ExporterNode(nodes.Count, name, id, kind, exportPath, x, y);

						var connectionPoints = targetNode.DuplicateConnectionPoints();
						foreach (var connectionPoint in connectionPoints) {
							newNode.AddConnectionPoint(connectionPoint);
						}

						nodes.Add(newNode);
						break;
					}

					default: {
						Debug.LogError("kind not found:" + kind);
						return;
					}
				}

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
								var distancePos = tappedNode.GetPos() - activeObject.idPosDict[tappedNodeId];

								foreach (var node in nodes) {
									if (node.nodeId == tappedNodeId) continue;
									if (!activeObject.idPosDict.ContainsKey(node.nodeId)) continue;
									var relativePos = activeObject.idPosDict[node.nodeId] + distancePos;
									node.SetPos(relativePos);
								}
								break;
							}

							if (Event.current.shift) {
								Undo.RecordObject(this, "Select Objects");

								var additiveIds = new List<string>(activeObject.idPosDict.Keys);

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

						case OnNodeEvent.EventType.EVENT_DUPLICATE_TAPPED: {
							var duplicateNodeId = e.eventSourceNode.nodeId;
							var duplicatePoint = e.globalMousePosition;
							DuplicateNode(duplicateNodeId, duplicatePoint.x, duplicatePoint.y);
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

									var startPos = activeObject.idPosDict[node.nodeId];
									if (node.GetPos() != startPos) {
										// moved.
										movedIdPosDict[node.nodeId] = node.GetPos();
									}
								}

								if (movedIdPosDict.Any()) {
									
									foreach (var node in nodes) {
										if (activeObject.idPosDict.Keys.Contains(node.nodeId)) {
											var startPos = activeObject.idPosDict[node.nodeId];
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

									var activeObjectIds = activeObject.idPosDict.Keys.ToList();
									activeObject = RenewActiveObject(activeObjectIds);
								} else {
									// nothing moved, should cancel selecting this node.
									var cancelledActivatedIds = new List<string>(activeObject.idPosDict.Keys);
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

								var additiveIds = new List<string>(activeObject.idPosDict.Keys);

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
					SaveGraphWithReload();
					Repaint();
					break;
				}
				case OnNodeEvent.EventType.EVENT_SETUPWITHPACKAGE: {
					package = e.eventSourceNode.currentPackage;
					Setup(package);
					Repaint();
					break;
				}
				case OnNodeEvent.EventType.EVENT_UPDATEPACKAGE: {
					foreach (var node in nodes) {
						/*
							if package is deleted & node's current package is that, should change current package to None.
						*/
						if (!Node.NodeSharedPackages.Contains(node.currentPackage)) node.currentPackage = string.Empty;
					}
					break;
				}
			}
		}

		/**
			once expand, keep max size.
			it's convenience.
		*/
		private void UpdateSpacerRect () {
			var rightPoint = nodes.OrderByDescending(node => node.GetRightPos()).Select(node => node.GetRightPos()).ToList()[0] + AssetGraphSettings.WINDOW_SPAN;
			if (rightPoint < spacerRectRightBottom.x) rightPoint = spacerRectRightBottom.x;

			var bottomPoint = nodes.OrderByDescending(node => node.GetBottomPos()).Select(node => node.GetBottomPos()).ToList()[0] + AssetGraphSettings.WINDOW_SPAN;
			if (bottomPoint < spacerRectRightBottom.y) bottomPoint = spacerRectRightBottom.y;

			spacerRectRightBottom = new Vector2(rightPoint, bottomPoint);
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
									if (!activeObject.idPosDict.Any()) {
										activeObject = RenewActiveObject(new List<string>{objectId});
									} else {
										var additiveIds = new List<string>(activeObject.idPosDict.Keys);

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
