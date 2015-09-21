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
		public static void Open() {
			GetWindow<AssetGraph>();
		}

		public void OnEnable () {
			Debug.LogError("OnEnable");
			this.title = "AssetGraph";

			Undo.undoRedoPerformed += () => {
				SaveGraphWithReload();
				Repaint();
			};

			Node.Emit = EmitNodeEvent;
			Connection.Emit = EmitConnectionEvent;

			LoadTextures();

			InitializeGraph();
			Reload();


			if (nodes.Any()) UpdateSpacerRect();
		}

		private void LoadTextures () {
			Node.inputPointTex = AssetDatabase.LoadAssetAtPath(AssetGraphGUISettings.RESOURCE_INPUT_BG, typeof(Texture2D)) as Texture2D;
			Node.outputPointTex = AssetDatabase.LoadAssetAtPath(AssetGraphGUISettings.RESOURCE_OUTPUT_BG, typeof(Texture2D)) as Texture2D;

			Node.enablePointMarkTex = AssetDatabase.LoadAssetAtPath(AssetGraphGUISettings.RESOURCE_CONNECTIONPOINT_ENABLE, typeof(Texture2D)) as Texture2D;

			Node.inputPointMarkTex = AssetDatabase.LoadAssetAtPath(AssetGraphGUISettings.RESOURCE_CONNECTIONPOINT_INPUT, typeof(Texture2D)) as Texture2D;
			Node.outputPointMarkTex = AssetDatabase.LoadAssetAtPath(AssetGraphGUISettings.RESOURCE_CONNECTIONPOINT_OUTPUT, typeof(Texture2D)) as Texture2D;
			Node.outputPointMarkConnectedTex = AssetDatabase.LoadAssetAtPath(AssetGraphGUISettings.RESOURCE_CONNECTIONPOINT_OUTPUT_CONNECTED, typeof(Texture2D)) as Texture2D;

			Connection.connectionArrowTex = AssetDatabase.LoadAssetAtPath(AssetGraphGUISettings.RESOURCE_ARROW, typeof(Texture2D)) as Texture2D;

			selectionTex = AssetDatabase.LoadAssetAtPath(AssetGraphGUISettings.RESOURCE_SELECTION, typeof(Texture2D)) as Texture2D;
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
		}
		private ModifyMode modifyMode;



		private DateTime lastLoaded = DateTime.MinValue;
		
		private Vector2 spacerRectRightBottom;
		private Vector2 scrollPos = new Vector2(1500,0);
		
		private Texture2D reloadButtonTexture;

		private Dictionary<string,Dictionary<string, List<string>>> connectionThroughputs = new Dictionary<string, Dictionary<string, List<string>>>();


		[Serializable] public struct ActiveObject {
			[SerializeField] public List<string> ids;
			[SerializeField] public Vector2 pos;
			
			public ActiveObject (List<string> ids, Vector2 pos) {
				this.ids = new List<string>(ids);
				this.pos = pos;
			}
		}
		[SerializeField] private ActiveObject activeObject = new ActiveObject(new List<string>(), Vector2.zero);
		
		public struct Selection {
			public readonly float x;
			public readonly float y;

			public Selection (Vector2 position) {
				this.x = position.x;
				this.y = position.y;
			}
		}
		private Selection selection;

		private Dictionary<string, Vector2> movingObjects = new Dictionary<string, Vector2>();

		/**
			node window initializer.
			setup nodes, points and connections from saved data.
		*/
		public void InitializeGraph () {
			if (reloadButtonTexture == null) {
				Debug.LogError("ローディング画像、ひどい読み方。");
				var reloadTextureSources = Resources
					.FindObjectsOfTypeAll(typeof(Texture2D))
					.Where(data => data.ToString().Contains("d_RotateTool"))
					.ToList();
				if (0 < reloadTextureSources.Count) reloadButtonTexture = reloadTextureSources[0] as Texture2D;
			}


			var basePath = Path.Combine(Application.dataPath, AssetGraphSettings.ASSETGRAPH_TEMP_PATH);
			
			// create Temp folder under Assets/AssetGraph
			if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);

			var graphDataPath = Path.Combine(basePath, AssetGraphSettings.ASSETGRAPH_DATA_NAME);


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

				var lastModifiedStr = deserialized[AssetGraphSettings.ASSETGRAPH_DATA_LASTMODIFIED] as string;
				lastModified = Convert.ToDateTime(lastModifiedStr);

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
				// renew
				var graphData = new Dictionary<string, object>{
					{
						AssetGraphSettings.ASSETGRAPH_DATA_LASTMODIFIED, lastModified.ToString()
					},
					{
						AssetGraphSettings.ASSETGRAPH_DATA_NODES, new List<object>()
					},
					{
						AssetGraphSettings.ASSETGRAPH_DATA_CONNECTIONS, new List<object>()
					}
				};

				// save new empty graph data.
				UpdateGraphData(graphData);

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
			ConstructGraphFromDeserializedData(deserialized);
		}

		private void ConstructGraphFromDeserializedData (Dictionary<string, object> deserializedData) {
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
					case AssetGraphSettings.NodeKind.LOADER_SCRIPT:
					case AssetGraphSettings.NodeKind.LOADER_GUI: {
						var loadPath = nodeDict[AssetGraphSettings.LOADERNODE_LOAD_PATH] as string;

						var newNode = Node.LoaderNode(nodes.Count, name, id, kind, loadPath, x, y);

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
						var newNode = Node.GUINodeForImport(nodes.Count, name, id, kind, x, y);

						var outputLabelsList = nodeDict[AssetGraphSettings.NODE_OUTPUT_LABELS] as List<object>;
						foreach (var outputLabelSource in outputLabelsList) {
							var label = outputLabelSource as string;
							newNode.AddConnectionPoint(new OutputPoint(label));
						}

						nodes.Add(newNode);
						break;
					}

					case AssetGraphSettings.NodeKind.GROUPING_GUI: {
						var groupingKeyword = nodeDict[AssetGraphSettings.NODE_GROUPING_KEYWORD] as string;
						var newNode = Node.GUINodeForGrouping(nodes.Count, name, id, kind, groupingKeyword, x, y);

						var outputLabelsList = nodeDict[AssetGraphSettings.NODE_OUTPUT_LABELS] as List<object>;
						foreach (var outputLabelSource in outputLabelsList) {
							var label = outputLabelSource as string;
							newNode.AddConnectionPoint(new OutputPoint(label));
						}

						nodes.Add(newNode);
						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
						var bundleNameTemplate = nodeDict[AssetGraphSettings.NODE_BUNDLIZER_BUNDLENAME_TEMPLATE] as string;
						var newNode = Node.GUINodeForBundlizer(nodes.Count, name, id, kind, bundleNameTemplate, x, y);

						var outputLabelsList = nodeDict[AssetGraphSettings.NODE_OUTPUT_LABELS] as List<object>;
						foreach (var outputLabelSource in outputLabelsList) {
							var label = outputLabelSource as string;
							newNode.AddConnectionPoint(new OutputPoint(label));
						}

						nodes.Add(newNode);
						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
						var bundleOptionsSource = nodeDict[AssetGraphSettings.NODE_BUNDLEBUILDER_BUNDLEOPTIONS] as Dictionary<string, object>;
						var bundleOptions = new Dictionary<string, bool>();

						foreach (var key in bundleOptionsSource.Keys) {
							var val = (bool)bundleOptionsSource[key];
							bundleOptions[key] = val;
						}

						var newNode = Node.GUINodeForBundleBuilder(nodes.Count, name, id, kind, bundleOptions, x, y);

						var outputLabelsList = nodeDict[AssetGraphSettings.NODE_OUTPUT_LABELS] as List<object>;
						foreach (var outputLabelSource in outputLabelsList) {
							var label = outputLabelSource as string;
							newNode.AddConnectionPoint(new OutputPoint(label));
						}

						nodes.Add(newNode);
						break;
					}

					case AssetGraphSettings.NodeKind.EXPORTER_SCRIPT:
					case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
						var exportPath = nodeDict[AssetGraphSettings.EXPORTERNODE_EXPORT_PATH] as string;
						var newNode = Node.ExporterNode(nodes.Count, name, id, kind, exportPath, x, y);

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
				if (node.kind == AssetGraphSettings.NodeKind.LOADER_SCRIPT) continue;
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
					case AssetGraphSettings.NodeKind.LOADER_SCRIPT:
					case AssetGraphSettings.NodeKind.LOADER_GUI: {
						nodeDict[AssetGraphSettings.LOADERNODE_LOAD_PATH] = node.loadPath;
						break;
					}
					case AssetGraphSettings.NodeKind.EXPORTER_SCRIPT:
					case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
						nodeDict[AssetGraphSettings.EXPORTERNODE_EXPORT_PATH] = node.exportPath;
						break;
					}
					
					case AssetGraphSettings.NodeKind.FILTER_SCRIPT:
					case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT:
					case AssetGraphSettings.NodeKind.GROUPING_SCRIPT:
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
						break;
					}

					case AssetGraphSettings.NodeKind.GROUPING_GUI: {
						nodeDict[AssetGraphSettings.NODE_GROUPING_KEYWORD] = node.groupingKeyword;
						break;
					}

					case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI: {
						nodeDict[AssetGraphSettings.NODE_SCRIPT_TYPE] = node.scriptType;
						nodeDict[AssetGraphSettings.NODE_SCRIPT_PATH] = node.scriptPath;
						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
						nodeDict[AssetGraphSettings.NODE_BUNDLIZER_BUNDLENAME_TEMPLATE] = node.bundleNameTemplate;
						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
						nodeDict[AssetGraphSettings.NODE_BUNDLEBUILDER_BUNDLEOPTIONS] = node.bundleOptions;
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
				{AssetGraphSettings.ASSETGRAPH_DATA_CONNECTIONS, connectionList}
			};

			UpdateGraphData(graphData);
		}

		private void SaveGraphWithReload () {
			SaveGraph();
			Reload();
		}

		private void Reload () {
			var basePath = Path.Combine(Application.dataPath, AssetGraphSettings.ASSETGRAPH_TEMP_PATH);
			var graphDataPath = Path.Combine(basePath, AssetGraphSettings.ASSETGRAPH_DATA_NAME);
			if (!File.Exists(graphDataPath)) {
				Debug.LogError("no data found、初期化してもいいかもしれない。");
				return;
			}

			foreach (var node in nodes) {
				node.HideProgress();
			}

			// reload
			var dataStr = string.Empty;
			using (var sr = new StreamReader(graphDataPath)) {
				dataStr = sr.ReadToEnd();
			}

			var reloadedData = Json.Deserialize(dataStr) as Dictionary<string, object>;

			// ready throughput datas.
			connectionThroughputs = GraphStackController.SetupStackedGraph(reloadedData);
		}

		private void Run () {
			var basePath = Path.Combine(Application.dataPath, AssetGraphSettings.ASSETGRAPH_TEMP_PATH);
			var graphDataPath = Path.Combine(basePath, AssetGraphSettings.ASSETGRAPH_DATA_NAME);
			if (!File.Exists(graphDataPath)) {
				Debug.LogError("no data found、初期化してもいいかもしれない。");
				return;
			}


			var dataStr = string.Empty;
			using (var sr = new StreamReader(graphDataPath)) {
				dataStr = sr.ReadToEnd();
			}

			var currentCount = 0.00f;
			var totalCount = nodes.Count * 1f;

			Action<string, float>  updateHandler = (nodeId, progress) => {
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

			// run datas.
			connectionThroughputs = GraphStackController.RunStackedGraph(loadedData, updateHandler);

			EditorUtility.ClearProgressBar();
		}


		public void OnGUI () {
			EditorGUILayout.BeginHorizontal(GUI.skin.box);
			{
				if (!reloadButtonTexture) {
					if (GUILayout.Button("Reload")) {
						Reload();
					}
				} else {
					if (GUILayout.Button(reloadButtonTexture)) {
						Reload();
					}
				}
				
				if (GUILayout.Button("Build")) {
					Run();
				}
			}
			EditorGUILayout.EndHorizontal();

			/*
				scroll view.
			*/
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
			{
				ShowEventType(-2, Event.current.type);
				// draw node window x N.
				{
					BeginWindows();
					
					nodes.ForEach(node => node.DrawNode());

					EndWindows();
				}

				ShowEventType(-1, Event.current.type);

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

				ShowEventType(0, Event.current.type);

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

				switch (Event.current.type) {

					// draw line while dragging.
					case EventType.MouseDrag: {
						switch (modifyMode) {
							case ModifyMode.CONNECT_ENDED: {
								selection = new Selection(Event.current.mousePosition);
								modifyMode = ModifyMode.SELECTION_STARTED;
								break;
							}
							case ModifyMode.SELECTION_STARTED: {
								// do nothing.
								foreach (var node in nodes) {
									GUI.DrawTexture(node.GetRect(), selectionTex);
								}
								break;
							}
						}

						HandleUtility.Repaint();
						Event.current.Use();
						break;
					}

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


								var selectedRect = new Rect(x, y, width, height);
								var activeObjectIds = new List<string>();
								
								foreach (var node in nodes) {
									// get containd nodes,
									if (node.GetRect().Overlaps(selectedRect)) {
										activeObjectIds.Add(node.nodeId);
									}
								}

								// add connections if connected nodes are contained.
								foreach (var connection in connections) {
									var startNodeId = connection.startNodeId;
									var endNodeId = connection.endNodeId;

									if (activeObjectIds.Contains(startNodeId) && activeObjectIds.Contains(endNodeId)) {
										activeObjectIds.Add(connection.connectionId);
									}
								}

								// add active object if already selected anything.
								if (activeObject.ids.Any()) activeObjectIds.AddRange(activeObject.ids);

								activeObject = new ActiveObject(activeObjectIds, Vector2.zero);
								foreach (var node in nodes) {
									if (activeObject.ids.Contains(node.nodeId)) {
										node.SetActive();
										continue;
									}
									
									node.SetInactive();
								}

								foreach (var connection in connections) {
									if (activeObject.ids.Contains(connection.connectionId)) {
										connection.SetActive();
										continue;
									}
									
									connection.SetInactive();
								}

								selection = new Selection(Vector2.zero);
								modifyMode = ModifyMode.CONNECT_ENDED;

								HandleUtility.Repaint();
								Event.current.Use();
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
					
					if (activeObject.ids.Any()) {
						Undo.RecordObject(this, "Unselect");

						foreach (var activeObjectId in activeObject.ids) {
							// unselect all.
							foreach (var node in nodes) {
								if (activeObjectId == node.nodeId) node.SetInactive();
							}
							foreach (var connection in connections) {
								if (activeObjectId == connection.connectionId) connection.SetInactive();
							}
						}

						activeObject = new ActiveObject(new List<string>(), Vector2.zero);
					}

					break;
				}

				case EventType.ValidateCommand: {
					switch (Event.current.commandName) {
						// Delete active node or connection.
						case "Delete": {
							if (!activeObject.ids.Any()) {
								break;
							}

							Undo.RecordObject(this, "Delete Selection");

							foreach (var targetId in activeObject.ids) {
								DeleteNode(targetId);
								DeleteConnectionById(targetId);
							}

							SaveGraphWithReload();
							InitializeGraph();

							Repaint();

							activeObject = new ActiveObject(new List<string>(), Vector2.zero);

							Event.current.Use();
							break;
						}

						case "Copy": {
							if (!activeObject.ids.Any()) {
								break;
							}



							Debug.LogError("copy");
							Event.current.Use();
							break;
						}

						case "Cut": {
							if (!activeObject.ids.Any()) {
								break;
							}

							Debug.LogError("cut");
							Event.current.Use();
							break;
						}

						case "Paste": {
							if (!activeObject.ids.Any()) {
								break;
							}

							Debug.LogError("paste");
							Event.current.Use();
							break;
						}

						case "SelectAll": {
							Undo.RecordObject(this, "Select All Objects");

							var nodeIds = nodes.Select(node => node.nodeId).ToList();
							activeObject = new ActiveObject(new List<string>(nodeIds), Vector2.zero);

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
							Debug.LogError("Event.current.commandName:" + Event.current.commandName);
							break;
						}
					}
					break;
				}
			}
		}

		private void ShowEventType (int index, EventType type) {
			if (type == EventType.Repaint) return;
			if (type == EventType.Layout) return;
			if (type == EventType.mouseMove) return;

			// Debug.LogError(index + ":" + type);
		}

		private Type IsAcceptableScriptType (Type type) {
			if (typeof(IntegratedScriptLoader).IsAssignableFrom(type)) return typeof(IntegratedScriptLoader);
			if (typeof(FilterBase).IsAssignableFrom(type)) return typeof(FilterBase);
			if (typeof(ImporterBase).IsAssignableFrom(type)) return typeof(ImporterBase);
			if (typeof(PrefabricatorBase).IsAssignableFrom(type)) return typeof(PrefabricatorBase);
			if (typeof(BundlizerBase).IsAssignableFrom(type)) return typeof(BundlizerBase);
			if (typeof(IntegratedScriptExporter).IsAssignableFrom(type)) return typeof(IntegratedScriptExporter);
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

			if (string.IsNullOrEmpty(nodeName)) nodeName = AssetGraphSettings.DEFAULT_NODE_NAME[kind];
			
			switch (kind) {
				case AssetGraphSettings.NodeKind.LOADER_GUI: {
					newNode = Node.LoaderNode(nodes.Count, nodeName, nodeId, kind, RelativeProjectPath(), x, y);
					newNode.AddConnectionPoint(new OutputPoint(AssetGraphSettings.DEFAULT_OUTPUTPOINT_LABEL));
					break;
				}

				case AssetGraphSettings.NodeKind.FILTER_GUI: {
					newNode = Node.GUINodeForFilter(nodes.Count, nodeName, nodeId, kind, new List<string>(), x, y);
					newNode.AddConnectionPoint(new InputPoint(AssetGraphSettings.DEFAULT_INPUTPOINT_LABEL));
					break;
				}
				
				case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
					newNode = Node.GUINodeForImport(nodes.Count, nodeName, nodeId, kind, x, y);
					newNode.AddConnectionPoint(new InputPoint(AssetGraphSettings.DEFAULT_INPUTPOINT_LABEL));
					newNode.AddConnectionPoint(new OutputPoint(AssetGraphSettings.DEFAULT_OUTPUTPOINT_LABEL));
					break;
				}

				case AssetGraphSettings.NodeKind.GROUPING_GUI: {
					newNode = Node.GUINodeForGrouping(nodes.Count, nodeName, nodeId, kind, string.Empty, x, y);
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
					newNode = Node.GUINodeForBundlizer(nodes.Count, nodeName, nodeId, kind, string.Empty, x, y);
					newNode.AddConnectionPoint(new InputPoint(AssetGraphSettings.DEFAULT_INPUTPOINT_LABEL));
					newNode.AddConnectionPoint(new OutputPoint(AssetGraphSettings.DEFAULT_OUTPUTPOINT_LABEL));
					break;
				}

				case AssetGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					var bundleOptions = AssetGraphSettings.DefaultBundleOptionSettings;
					newNode = Node.GUINodeForBundleBuilder(nodes.Count, nodeName, nodeId, kind, bundleOptions, x, y);
					newNode.AddConnectionPoint(new InputPoint(AssetGraphSettings.DEFAULT_INPUTPOINT_LABEL));
					newNode.AddConnectionPoint(new OutputPoint(AssetGraphSettings.DEFAULT_OUTPUTPOINT_LABEL));
					break;
				}

				case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
					newNode = Node.ExporterNode(nodes.Count, nodeName, nodeId, kind, RelativeProjectPath(), x, y);
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
			var basePath = Path.Combine(Application.dataPath, AssetGraphSettings.ASSETGRAPH_TEMP_PATH);
			var graphDataPath = Path.Combine(basePath, AssetGraphSettings.ASSETGRAPH_DATA_NAME);
			using (var sw = new StreamWriter(graphDataPath)) {
				sw.Write(dataStr);
			}
		}

		private string RelativeProjectPath () {
			Debug.LogError("winだとへんなの返している気がする");
			var assetPath = Application.dataPath;
			return Directory.GetParent(assetPath).Name;
		}

		private void DuplicateNode (string sourceNodeId, float x, float y) {
			// add undo record.
			Undo.RecordObject(this, "Duplicate Node");

						
			var targetNodes = nodes.Where(node => node.nodeId == sourceNodeId).ToList();
			if (!targetNodes.Any()) return;

			foreach (var targetNode in targetNodes) {
				var id = Guid.NewGuid().ToString();
				var kind = targetNode.kind;
				var name = targetNode.name;

				switch (kind) {
					case AssetGraphSettings.NodeKind.LOADER_SCRIPT:
					case AssetGraphSettings.NodeKind.LOADER_GUI: {
						var loadPath = targetNode.loadPath;

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
						
						var newNode = Node.GUINodeForFilter(nodes.Count, name, id, kind, filterContainsKeywords, x, y);

						var connectionPoints = targetNode.DuplicateConnectionPoints();
						foreach (var connectionPoint in connectionPoints) {
							newNode.AddConnectionPoint(connectionPoint);
						}
						
						nodes.Add(newNode);
						break;
					}

					case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
						var newNode = Node.GUINodeForImport(nodes.Count, name, id, kind, x, y);

						var connectionPoints = targetNode.DuplicateConnectionPoints();
						foreach (var connectionPoint in connectionPoints) {
							newNode.AddConnectionPoint(connectionPoint);
						}
						
						nodes.Add(newNode);
						break;
					}

					case AssetGraphSettings.NodeKind.GROUPING_GUI: {
						var groupingKeyword = targetNode.groupingKeyword;
						var newNode = Node.GUINodeForGrouping(nodes.Count, name, id, kind, groupingKeyword, x, y);

						var connectionPoints = targetNode.DuplicateConnectionPoints();
						foreach (var connectionPoint in connectionPoints) {
							newNode.AddConnectionPoint(connectionPoint);
						}
						
						nodes.Add(newNode);
						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
						var bundleNameTemplate = targetNode.bundleNameTemplate;
						var newNode = Node.GUINodeForBundlizer(nodes.Count, name, id, kind, bundleNameTemplate, x, y);

						var connectionPoints = targetNode.DuplicateConnectionPoints();
						foreach (var connectionPoint in connectionPoints) {
							newNode.AddConnectionPoint(connectionPoint);
						}
						
						nodes.Add(newNode);
						break;
					}

					case AssetGraphSettings.NodeKind.EXPORTER_SCRIPT:
					case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
						var exportPath = targetNode.exportPath;
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
						case OnNodeEvent.EventType.EVENT_NODE_HANDLING: {
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
							connection drop from started node detected.
						*/
						case OnNodeEvent.EventType.EVENT_NODE_DROPPED: {
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
						case OnNodeEvent.EventType.EVENT_NODE_TOUCHDOWN: {
							var tappedNodeId = e.eventSourceNode.nodeId;

							if (activeObject.ids.Contains(tappedNodeId)) {
								if (1 < activeObject.ids.Count) {
									movingObjects = new Dictionary<string, Vector2>();
									foreach (var node in nodes) {
										if (activeObject.ids.Contains(node.nodeId)) {
											movingObjects[node.nodeId] = node.GetPos();
										}
									}
								}
								break;
							}
							break;
						}

						case OnNodeEvent.EventType.EVENT_NODE_HANDLING: {
							var tappedNodeId = e.eventSourceNode.nodeId;
							
							if (activeObject.ids.Contains(tappedNodeId)) {
								if (1 < activeObject.ids.Count) {
									if (movingObjects.Any()) {
										var before = movingObjects[tappedNodeId];
										var after = e.eventSourceNode.GetPos();
										Debug.LogError("位置関係怪しい。");

										// var distance = before - after;
										
										// foreach (var node in nodes) {
										// 	if (!activeObject.ids.Contains(node.nodeId)) continue;
										// 	if (node.nodeId == tappedNodeId) continue;

										// 	node.MoveRelative(distance);
										// }
									}
								}
								break;
							}

							Undo.RecordObject(this, "Select Node");
							
							foreach (var node in nodes) {
								if (node.nodeId == tappedNodeId) {
									node.SetActive();
									activeObject = new ActiveObject(new List<string>{node.nodeId}, node.GetPos());
								}
								else node.SetInactive();
							}

							foreach (var con in connections) {
								con.SetInactive();
							}
							break;
						}

						case OnNodeEvent.EventType.EVENT_NODE_MULTIPLE_SELECTION: {
							
							Undo.RecordObject(this, "Select Objects");

							var objectId = string.Empty;

							if (e.eventSourceNode != null) {
								objectId = e.eventSourceNode.nodeId;
								if (!activeObject.ids.Any()) {
									var objectPos = e.eventSourceNode.GetPos();
									activeObject = new ActiveObject(new List<string>{objectId}, objectPos);
								} else {
									var additiveIds = new List<string>(activeObject.ids);

									// already contained, cancel.
									if (!activeObject.ids.Contains(objectId)) {
										additiveIds.Add(objectId);	
									} else {
										additiveIds.Remove(objectId);
									}
									
									Debug.LogError("位置は、選択オブジェクトの集まりから、一番左上を選びたいところ。Paste時に全部反映させたものを乗っけたい。消えてるものもコピー、、したいよな。。。ということはコピー用に領域持つ必要がある。うん、異論ない。");
									
									activeObject = new ActiveObject(additiveIds, activeObject.pos);
								}
							}

							foreach (var node in nodes) {
								if (activeObject.ids.Contains(node.nodeId)) {
									node.SetActive();
									continue;
								}
								
								node.SetInactive();
							}

							foreach (var connection in connections) {
								if (activeObject.ids.Contains(connection.connectionId)) {
									connection.SetActive();
									continue;
								}
								
								connection.SetInactive();
							}

							break;
						}

						/*
							start connection handling.
						*/
						case OnNodeEvent.EventType.EVENT_NODE_HANDLE_STARTED: {
							modifyMode = ModifyMode.CONNECT_STARTED;
							currentEventSource = e;
							break;
						}

						/*
							connectionPoint tapped.
						*/
						case OnNodeEvent.EventType.EVENT_DELETE_ALL_INPUT_CONNECTIONS: 
						case OnNodeEvent.EventType.EVENT_DELETE_ALL_OUTPUT_CONNECTIONS: {
							Debug.LogError("あとでなんとかする");
							// var sourcePoint = e.eventSourceNode;

							// var relatedConnections = connections
							// 	.Where(
							// 		con => con.IsStartAtConnectionPoint(sourcePoint) || 
							// 		con.IsEndAtConnectionPoint(sourcePoint)
							// 	)
							// 	.ToList();

							// /*
							// 	show menuContext for control these connections.
							// */
							// var menu = new GenericMenu();
							// menu.AddItem(
							// 	new GUIContent("delete all connections"), 
							// 	false, 
							// 	() => {
							// 		Undo.RecordObject(this, "Delete All Connections");

							// 		foreach (var con in relatedConnections) {
							// 			var conId = con.connectionId;
							// 			DeleteConnectionById(conId);
							// 		}

							// 		SaveGraphWithReload();
							// 	}
							// );
							// menu.ShowAsContext();
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
								node moved.
								node tapped.
						*/
						case OnNodeEvent.EventType.EVENT_NODE_RELEASED: {
							var movedNode = e.eventSourceNode;
							var movedNodeId = movedNode.nodeId;
							
							if (activeObject.ids.Contains(movedNodeId)) {
								var beforePosition = activeObject.pos;
								var afterPosition = movedNode.GetPos();

								movedNode.SetPos(beforePosition);

								Undo.RecordObject(this, "Move Node");

								movedNode.SetPos(afterPosition);

								activeObject = new ActiveObject(new List<string>{movedNodeId}, movedNode.GetPos());
							} else {
								Undo.RecordObject(this, "Select Node");

								activeObject = new ActiveObject(new List<string>{movedNodeId}, movedNode.GetPos());

								foreach (var node in nodes) {
									if (node.nodeId == movedNodeId) node.SetActive();
									else node.SetInactive();
								}

								foreach (var con in connections) {
									con.SetInactive();
								}
							}

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
				case OnNodeEvent.EventType.EVENT_CONNECTIONPOINT_UPDATED: {
					Debug.LogError("あーポイント追加時に線が消えちゃうのが困るよな");
					var targetNode = e.eventSourceNode;

					var connectionsFromThisNode = connections.Where(con => con.startNodeId == targetNode.nodeId).ToList();
					var connectionsToThisNode = connections.Where(con => con.endNodeId == targetNode.nodeId).ToList();
					
					// remove connections from this node.
					foreach (var con in connectionsFromThisNode) {
						connections.Remove(con);						
					}

					// remove connections to this node.
					foreach (var con in connectionsToThisNode) {
						connections.Remove(con);						
					}
					break;
				}
				case OnNodeEvent.EventType.EVENT_SAVE: {
					SaveGraphWithReload();
					Repaint();
					break;
				}
			}
		}

		private void UpdateSpacerRect () {
			var rightPoint = nodes.OrderByDescending(node => node.GetRightPos()).Select(node => node.GetRightPos()).ToList()[0] + AssetGraphSettings.WINDOW_SPAN;
			var bottomPoint = nodes.OrderByDescending(node => node.GetBottomPos()).Select(node => node.GetBottomPos()).ToList()[0] + AssetGraphSettings.WINDOW_SPAN;
			spacerRectRightBottom = new Vector2(rightPoint, bottomPoint);
		}

		public void DeleteNode (string deletingNodeId) {
			var deletedNodeIndex = nodes.FindIndex(node => node.nodeId == deletingNodeId);
			if (0 <= deletedNodeIndex) {
				nodes.RemoveAt(deletedNodeIndex);
			}
		}

		public void EmitConnectionEvent (OnConnectionEvent e) {
			switch (modifyMode) {
				case ModifyMode.CONNECT_ENDED: {
					switch (e.eventType) {
						case OnConnectionEvent.EventType.EVENT_CONNECTION_MULTIPLE_SELECTION: {
							Undo.RecordObject(this, "Select Objects");

							var objectId = string.Empty;

							if (e.eventSourceCon != null) {
								objectId = e.eventSourceCon.connectionId;
								if (!activeObject.ids.Any()) {
									var objectPos = new Vector2(-1, -1);
									activeObject = new ActiveObject(new List<string>{objectId}, objectPos);
								} else {
									var additiveIds = new List<string>(activeObject.ids);

									// already contained, cancel.
									if (!activeObject.ids.Contains(objectId)) {
										additiveIds.Add(objectId);	
									} else {
										activeObject.ids.Remove(objectId);
									}
									
									Debug.LogError("位置は、選択オブジェクトの集まりから、一番左上を選びたいところ。connectionのものは含まない。");
									
									activeObject = new ActiveObject(additiveIds, activeObject.pos);
								}
							}

							foreach (var node in nodes) {
								if (activeObject.ids.Contains(node.nodeId)) {
									node.SetActive();
									continue;
								}
								
								node.SetInactive();
							}

							foreach (var connection in connections) {
								if (activeObject.ids.Contains(connection.connectionId)) {
									connection.SetActive();
									continue;
								}
								
								connection.SetInactive();
							}
							break;
						}
						case OnConnectionEvent.EventType.EVENT_CONNECTION_TAPPED: {
							
							Undo.RecordObject(this, "Select Connection");

							var tappedConnectionId = e.eventSourceCon.connectionId;
							foreach (var con in connections) {
								if (con.connectionId == tappedConnectionId) {
									con.SetActive();
									activeObject = new ActiveObject(new List<string>{con.connectionId}, Vector2.zero);
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

		private void DeleteConnectionByRelation (Node startNode, ConnectionPoint startPoint, Node endNode, ConnectionPoint endPoint) {
			connections.Where(con => con.IsSameDetail(startNode, startPoint, endNode, endPoint)).
				Select(con => connections.Remove(con));
		}

		private void DeleteConnectionById (string connectionId) {
			var deletedConnectionIndex = connections.FindIndex(con => con.connectionId == connectionId);
			if (0 <= deletedConnectionIndex) {
				connections.RemoveAt(deletedConnectionIndex);
			}
		}
	}
}
