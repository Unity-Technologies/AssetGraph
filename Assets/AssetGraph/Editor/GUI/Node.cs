using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;


namespace AssetGraph {
	[Serializable] public class Node {
		public static Action<OnNodeEvent> Emit;

		public static Texture2D inputPointTex;
		public static Texture2D outputPointTex;


		public static Texture2D enablePointMarkTex;

		public static Texture2D inputPointMarkTex;
		public static Texture2D outputPointMarkTex;
		public static Texture2D outputPointMarkConnectedTex;

		[SerializeField] private List<ConnectionPoint> connectionPoints = new List<ConnectionPoint>();

		[SerializeField] private int nodeWindowId;
		[SerializeField] private Rect baseRect;

		[SerializeField] public string name;
		[SerializeField] public string nodeId;
		[SerializeField] public AssetGraphSettings.NodeKind kind;

		[SerializeField] public string scriptType;
		[SerializeField] public string scriptPath;
		[SerializeField] public string loadPath;
		[SerializeField] public string exportPath;
		[SerializeField] public List<string> filterContainsKeywords;
		[SerializeField] public string groupingKeyword;
		[SerializeField] public string bundleNameTemplate;
		[SerializeField] public Dictionary<string, bool> bundleOptions;

		[SerializeField] private string nodeInterfaceTypeStr;

		[SerializeField] private NodeInspector nodeInsp;


		private float progress;
		private bool running;

		
		public static Node LoaderNode (int index, string name, string nodeId, AssetGraphSettings.NodeKind kind, string loadPath, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				loadPath: loadPath
			);
		}

		public static Node ExporterNode (int index, string name, string nodeId, AssetGraphSettings.NodeKind kind, string exportPath, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				exportPath: exportPath
			);
		}

		public static Node ScriptNode (int index, string name, string nodeId, AssetGraphSettings.NodeKind kind, string scriptType, string scriptPath, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				scriptType: scriptType,
				scriptPath: scriptPath
			);
		}

		public static Node GUINodeForFilter (int index, string name, string nodeId, AssetGraphSettings.NodeKind kind, List<string> filterContainsKeywords, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				filterContainsKeywords: filterContainsKeywords
			);
		}

		public static Node GUINodeForImport (int index, string name, string nodeId, AssetGraphSettings.NodeKind kind, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y
			);
		}

		public static Node GUINodeForGrouping (int index, string name, string nodeId, AssetGraphSettings.NodeKind kind, string groupingKeyword, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				groupingKeyword: groupingKeyword
			);
		}

		public static Node GUINodeForPrefabricator (int index, string name, string nodeId, AssetGraphSettings.NodeKind kind, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y
			);
		}

		public static Node GUINodeForBundlizer (int index, string name, string nodeId, AssetGraphSettings.NodeKind kind, string bundleNameTemplate, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				bundleNameTemplate: bundleNameTemplate
			);
		}

		public static Node GUINodeForBundleBuilder (int index, string name, string nodeId, AssetGraphSettings.NodeKind kind, Dictionary<string, bool> bundleOptions, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				bundleOptions: bundleOptions
			);
		}

		/**
			Inspector GUI for this node.
		*/
		[CustomEditor(typeof(NodeInspector))]
		public class NodeObj : Editor {
			
			public override void OnInspectorGUI () {
				var node = ((NodeInspector)target).node;
				if (node == null) return;

				EditorGUILayout.LabelField("nodeId:", node.nodeId);

				switch (node.kind) {
					case AssetGraphSettings.NodeKind.LOADER_SCRIPT:
					case AssetGraphSettings.NodeKind.LOADER_GUI: {
						EditorGUILayout.HelpBox("Loader: load files from path.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						var newLoadPath = EditorGUILayout.TextField("Load Path", node.loadPath);
						if (newLoadPath != node.loadPath) {
							Debug.LogWarning("本当は打ち込み単位の更新ではなくて、Finderからパス、、とかがいいんだと思うけど、今はパス。");
							node.loadPath = newLoadPath;
							node.Save();
						}
						break;
					}


					case AssetGraphSettings.NodeKind.FILTER_SCRIPT: {
						EditorGUILayout.HelpBox("Filter: filtering files by script.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						EditorGUILayout.LabelField("Script Path", node.scriptPath);

						var outputPointLabels = node.OutputPointLabels();
						EditorGUILayout.LabelField("connectionPoints Count", outputPointLabels.Count.ToString());
						
						foreach (var label in outputPointLabels) {
							EditorGUILayout.LabelField("label", label);
						}
						break;
					}
					case AssetGraphSettings.NodeKind.FILTER_GUI: {
						EditorGUILayout.HelpBox("Filter: filtering files by keywords.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						for (int i = 0; i < node.filterContainsKeywords.Count; i++) {
							GUILayout.BeginHorizontal();
							{
								if (GUILayout.Button("-")) {
									node.filterContainsKeywords.RemoveAt(i);
									node.UpdateOutputPoints();
									node.Save();
								} else {
									var newContainsKeyword = EditorGUILayout.TextField("Contains", node.filterContainsKeywords[i]);
									if (newContainsKeyword != node.filterContainsKeywords[i]) {
										node.filterContainsKeywords[i] = newContainsKeyword;
										node.UpdateOutputPoints();
										node.UpdateNodeRect();
										node.Save();
									}
								}
							}
							GUILayout.EndHorizontal();
						}

						// add contains keyword interface.
						if (GUILayout.Button("+")) node.filterContainsKeywords.Add(string.Empty);

						break;
					}


					case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT: {
						EditorGUILayout.HelpBox("Importer: import files by script.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						EditorGUILayout.LabelField("Script Path", node.scriptPath);
						break;
					}
					case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
						EditorGUILayout.HelpBox("Importer: import files with applying settings from SamplingAssets.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						var nodeId = node.nodeId;

						var noFilesFound = false;
						var tooManyFilesFound = false;

						var samplingPath = Path.Combine(AssetGraphSettings.IMPORTER_SAMPLING_PLACE, nodeId);
						if (Directory.Exists(samplingPath)) {
							var samplingFiles = FileController.FilePathsInFolderWithoutMetaOnly1Level(samplingPath);
							switch (samplingFiles.Count) {
								case 0: {
									noFilesFound = true;
									break;
								}
								case 1: {
									var samplingAssetPath = samplingFiles[0];
									EditorGUILayout.LabelField("Sampling Asset Path", samplingAssetPath);
									if (GUILayout.Button("Modify Import Setting")) {
										var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(samplingAssetPath);
										Selection.activeObject = obj;
									}
									if (GUILayout.Button("Reset Import Setting")) {
										var result = AssetDatabase.DeleteAsset(samplingAssetPath);
										if (!result) Debug.LogError("failed to delete samplingAsset:" + samplingAssetPath);
										node.Save();
									}
									break;
								}
								default: {
									tooManyFilesFound = true;
									break;
								}
							}
						} else {
							noFilesFound = true;
						}

						if (noFilesFound) {
							EditorGUILayout.LabelField("Sampling Asset", "no asset found. please Reload first.");
						}

						if (tooManyFilesFound) {
							EditorGUILayout.LabelField("Sampling Asset", "too many assets found. please delete file at:" + samplingPath);
						}

						break;
					}


					case AssetGraphSettings.NodeKind.GROUPING_SCRIPT: {
						Debug.LogError("まだ存在してない。");
						// EditorGUILayout.LabelField("kind", "Grouping:grouping files by script.");
						// EditorGUILayout.LabelField("Script Path", node.scriptPath);
						break;
					}
					case AssetGraphSettings.NodeKind.GROUPING_GUI: {
						EditorGUILayout.HelpBox("Grouping: grouping files by one keyword.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						var groupingKeyword = EditorGUILayout.TextField("Grouping Keyword", node.groupingKeyword);
						if (groupingKeyword != node.groupingKeyword) {
							node.groupingKeyword = groupingKeyword;
							node.Save();
						}
						break;
					}
					

					case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT: {
						EditorGUILayout.HelpBox("Prefabricator: generate prefab by PrefabricatorBase extended script.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						EditorGUILayout.LabelField("Script Path", node.scriptPath);
						Debug.LogError("うーんType指定のほうが楽だね");
						break;
					}
					case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI:{
						EditorGUILayout.HelpBox("Prefabricator: generate prefab by PrefabricatorBase extended script.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						var newScriptType = EditorGUILayout.TextField("Script Type", node.scriptType);
						if (newScriptType != node.scriptType) {
							Debug.LogWarning("Scriptなんで、 ScriptをAttachできて、勝手に決まった方が良い。");
							node.scriptType = newScriptType;
							node.Save();
						}
						break;
					}


					case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT: {
						EditorGUILayout.HelpBox("Bundlizer: generate AssetBundle by script.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						EditorGUILayout.LabelField("Script Path", node.scriptPath);
						break;
					}
					case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
						EditorGUILayout.HelpBox("Bundlizer: bundle resources to AssetBundle by template.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						var bundleNameTemplate = EditorGUILayout.TextField("Bundle Name Template", node.bundleNameTemplate);
						if (bundleNameTemplate != node.bundleNameTemplate) {
							node.bundleNameTemplate = bundleNameTemplate;
							node.Save();
						}
						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLEBUILDER_SCRIPT: {
						Debug.LogError("not yet.");
						// EditorGUILayout.HelpBox("Bundlizer: generate AssetBundle by script.", MessageType.Info);
						// var newName = EditorGUILayout.TextField("Node Name", node.name);
						// if (newName != node.name) {
						// 	node.name = newName;
						// 	node.UpdateNodeRect();
						// 	node.Save();
						// }

						// EditorGUILayout.LabelField("Script Path", node.scriptPath);
						break;
					}
					case AssetGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
						EditorGUILayout.HelpBox("BundleBuilder: generate AssetBundle.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						var bundleOptions = node.bundleOptions;
						var keys = bundleOptions.Keys.ToList();

						for (var i = 0; i < bundleOptions.Count; i++) {
							var key = keys[i];
							var val = bundleOptions[keys[i]];
							var result = EditorGUILayout.ToggleLeft(key, val);
							if (result != val) {
								node.bundleOptions[key] = result;

								/*
									Cannot use options DisableWriteTypeTree and IgnoreTypeTreeChanges at the same time.
								*/
								if (key == "Disable Write TypeTree" && result &&
									node.bundleOptions["Ignore TypeTree Changes"]) {
									node.bundleOptions["Ignore TypeTree Changes"] = false;
								}

								if (key == "Ignore TypeTree Changes" && result &&
									node.bundleOptions["Disable Write TypeTree"]) {
									node.bundleOptions["Disable Write TypeTree"] = false;
								}

								node.Save();
							}
						}
						break;
					}


					case AssetGraphSettings.NodeKind.EXPORTER_SCRIPT:
					case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
						EditorGUILayout.HelpBox("Exporter: export files to path.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						var newExportPath = EditorGUILayout.TextField("Export Path", node.exportPath);
						if (newExportPath != node.exportPath) {
							Debug.LogWarning("本当は打ち込み単位の更新ではなくて、Finderからパス、、とかがいいんだと思うけど、今はパス。");
							node.exportPath = newExportPath;
							node.Save();
						}
						break;
					}

					default: {
						Debug.LogError("failed to match:" + node.kind);
						break;
					}
				}
			}
		}

		public void UpdateOutputPoints () {
			connectionPoints = new List<ConnectionPoint>();

			foreach (var keyword in filterContainsKeywords) {
				var newPoint = new OutputPoint(keyword);
				AddConnectionPoint(newPoint);
			}

			// add input point
			AddConnectionPoint(new InputPoint(AssetGraphSettings.DEFAULT_INPUTPOINT_LABEL));

			Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_CONNECTIONPOINT_UPDATED, this, Vector2.zero, null));
		}

		public void Save () {
			Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_SAVE, this, Vector2.zero, null));
		}

		public Node () {}

		private Node (
			int index, 
			string name, 
			string nodeId, 
			AssetGraphSettings.NodeKind kind, 
			float x, 
			float y,
			string scriptType = null, 
			string scriptPath = null, 
			string loadPath = null, 
			string exportPath = null, 
			List<string> filterContainsKeywords = null, 
			string groupingKeyword = null,
			string bundleNameTemplate = null,
			Dictionary<string, bool> bundleOptions = null
		) {
			nodeInsp = ScriptableObject.CreateInstance<NodeInspector>();
			nodeInsp.hideFlags = HideFlags.DontSave;

			this.nodeWindowId = index;
			this.name = name;
			this.nodeId = nodeId;
			this.kind = kind;
			this.scriptType = scriptType;
			this.scriptPath = scriptPath;
			this.loadPath = loadPath;
			this.exportPath = exportPath;
			this.filterContainsKeywords = filterContainsKeywords;
			this.groupingKeyword = groupingKeyword;
			this.bundleNameTemplate = bundleNameTemplate;
			this.bundleOptions = bundleOptions;
			
			this.baseRect = new Rect(x, y, AssetGraphGUISettings.NODE_BASE_WIDTH, AssetGraphGUISettings.NODE_BASE_HEIGHT);
			
			switch (this.kind) {
				case AssetGraphSettings.NodeKind.LOADER_SCRIPT:
				case AssetGraphSettings.NodeKind.LOADER_GUI:

				case AssetGraphSettings.NodeKind.EXPORTER_GUI:
				case AssetGraphSettings.NodeKind.EXPORTER_SCRIPT: {
					this.nodeInterfaceTypeStr = "flow node 0";
					break;
				}

				case AssetGraphSettings.NodeKind.FILTER_SCRIPT:
				case AssetGraphSettings.NodeKind.FILTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 1";
					break;
				}
				
				case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT:
				case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 2";
					break;
				}

				case AssetGraphSettings.NodeKind.GROUPING_SCRIPT:
				case AssetGraphSettings.NodeKind.GROUPING_GUI: {
					this.nodeInterfaceTypeStr = "flow node 3";
					break;
				}

				case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
				case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI:
				 {
					this.nodeInterfaceTypeStr = "flow node 4";
					break;
				}

				case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT:
				case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 5";
					break;
				}

				case AssetGraphSettings.NodeKind.BUNDLEBUILDER_SCRIPT:
				case AssetGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 6";
					break;
				}

				default: {
					Debug.LogError("failed to match:" + this.kind);
					break;
				}
			}
		}

		public void SetActive () {
			nodeInsp.UpdateNode(this);
			Selection.activeObject = nodeInsp;

			switch (this.kind) {
				case AssetGraphSettings.NodeKind.LOADER_SCRIPT:
				case AssetGraphSettings.NodeKind.LOADER_GUI:

				case AssetGraphSettings.NodeKind.EXPORTER_GUI:
				case AssetGraphSettings.NodeKind.EXPORTER_SCRIPT: {
					this.nodeInterfaceTypeStr = "flow node 0 on";
					break;
				}

				case AssetGraphSettings.NodeKind.FILTER_SCRIPT:
				case AssetGraphSettings.NodeKind.FILTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 1 on";
					break;
				}
				
				case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT:
				case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 2 on";
					break;
				}

				case AssetGraphSettings.NodeKind.GROUPING_SCRIPT:
				case AssetGraphSettings.NodeKind.GROUPING_GUI: {
					this.nodeInterfaceTypeStr = "flow node 3 on";
					break;
				}

				case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
				case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI:
				 {
					this.nodeInterfaceTypeStr = "flow node 4 on";
					break;
				}

				case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT:
				case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 5 on";
					break;
				}

				case AssetGraphSettings.NodeKind.BUNDLEBUILDER_SCRIPT:
				case AssetGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 6 on";
					break;
				}

				default: {
					Debug.LogError("failed to match:" + this.kind);
					break;
				}
			}
		}

		public void SetInactive () {
			switch (this.kind) {
				case AssetGraphSettings.NodeKind.LOADER_SCRIPT:
				case AssetGraphSettings.NodeKind.LOADER_GUI:

				case AssetGraphSettings.NodeKind.EXPORTER_GUI:
				case AssetGraphSettings.NodeKind.EXPORTER_SCRIPT: {
					this.nodeInterfaceTypeStr = "flow node 0";
					break;
				}

				case AssetGraphSettings.NodeKind.FILTER_SCRIPT:
				case AssetGraphSettings.NodeKind.FILTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 1";
					break;
				}
				
				case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT:
				case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 2";
					break;
				}

				case AssetGraphSettings.NodeKind.GROUPING_SCRIPT:
				case AssetGraphSettings.NodeKind.GROUPING_GUI: {
					this.nodeInterfaceTypeStr = "flow node 3";
					break;
				}

				case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
				case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI:
				 {
					this.nodeInterfaceTypeStr = "flow node 4";
					break;
				}

				case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT:
				case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 5";
					break;
				}

				case AssetGraphSettings.NodeKind.BUNDLEBUILDER_SCRIPT:
				case AssetGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 6";
					break;
				}

				default: {
					Debug.LogError("failed to match:" + this.kind);
					break;
				}
			}
		}

		public void AddConnectionPoint (ConnectionPoint adding) {
			connectionPoints.Add(adding);
			
			// update node size by number of connectionPoint.
			if (3 < connectionPoints.Count) {
				this.baseRect = new Rect(baseRect.x, baseRect.y, baseRect.width, AssetGraphGUISettings.NODE_BASE_HEIGHT + (AssetGraphGUISettings.FILTER_OUTPUT_SPAN * (connectionPoints.Count - 3)));
			}

			UpdateNodeRect();
		}

		public List<ConnectionPoint> DuplicateConnectionPoints () {
			var copiedConnectionList = new List<ConnectionPoint>();
			foreach (var connectionPoint in connectionPoints) {
				if (connectionPoint.isOutput) copiedConnectionList.Add(new OutputPoint(connectionPoint.label));
				if (connectionPoint.isInput) copiedConnectionList.Add(new InputPoint(connectionPoint.label));
			}
			return copiedConnectionList;
		}

		private void RefreshConnectionPos () {
			var inputPoints = connectionPoints.Where(p => p.isInput).ToList();
			var outputPoints = connectionPoints.Where(p => p.isOutput).ToList();

			for (int i = 0; i < inputPoints.Count; i++) {
				var point = inputPoints[i];
				point.UpdatePos(i, inputPoints.Count, baseRect.width, baseRect.height);
			}

			for (int i = 0; i < outputPoints.Count; i++) {
				var point = outputPoints[i];
				point.UpdatePos(i, outputPoints.Count, baseRect.width, baseRect.height);
			}
		}

		public List<string> OutputPointLabels () {
			return connectionPoints
						.Where(p => p.isOutput)
						.Select(p => p.label)
						.ToList();
		}

		public ConnectionPoint ConnectionPointFromLabel (string label) {
			var targetPoints = connectionPoints.Where(con => con.label == label).ToList();
			if (!targetPoints.Any()) {
				Debug.LogError("no connection label:" + label + " exists in node name:" + name);
				return null;
			}
			return targetPoints[0];
		}

		public void DrawNode () {
			baseRect = GUI.Window(nodeWindowId, baseRect, UpdateNodeEvent, string.Empty, nodeInterfaceTypeStr);
		}

		/**
			retrieve GUI events for this node.
		*/
		private void UpdateNodeEvent (int id) {
			switch (Event.current.type) {

				/*
					handling release of mouse drag from this node to another node.
					this node doesn't know about where the other node is. the master only knows.
					only emit event.
				*/
				case EventType.Ignore: {
					Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_CONNECTION_OVERED, this, Event.current.mousePosition, null));
					break;
				}

				/*
					handling release of mouse drag on this node.
				*/
				case EventType.MouseUp: {
					// if mouse position is on the connection point, emit mouse raised event over thr connection.
					foreach (var connectionPoint in connectionPoints) {
						var globalConnectonPointRect = new Rect(connectionPoint.buttonRect.x, connectionPoint.buttonRect.y, connectionPoint.buttonRect.width, connectionPoint.buttonRect.height);
						if (globalConnectonPointRect.Contains(Event.current.mousePosition)) {
							Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_CONNECTION_RAISED, this, Event.current.mousePosition, connectionPoint));
							return;
						}
					}

					Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_TATCHED, this, Event.current.mousePosition, null));
					break;
				}

				/*
					handling drag.
				*/
				case EventType.MouseDrag: {
					Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_MOVING, this, Event.current.mousePosition, null));
					break;
				}

				/*
					check if the mouse-down point is over one of the connectionPoint in this node.
					then emit event.
				*/
				case EventType.MouseDown: {
					var result = IsOverConnectionPoint(connectionPoints, Event.current.mousePosition);

					if (result != null) {
						Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_CONNECT_STARTED, this, Event.current.mousePosition, result));
						break;
					}
					break;
				}
			}

			// draw & update connectionPoint button interface.
			foreach (var point in connectionPoints) {
				switch (this.kind) {
					case AssetGraphSettings.NodeKind.FILTER_SCRIPT:
					case AssetGraphSettings.NodeKind.FILTER_GUI: {
						var label = point.label;
						var labelRect = new Rect(point.buttonRect.x - baseRect.width, point.buttonRect.y - (point.buttonRect.height/2), baseRect.width, point.buttonRect.height*2);

						var style = EditorStyles.label;
						var defaultAlignment = style.alignment;
						style.alignment = TextAnchor.MiddleRight;
						GUI.Label(labelRect, label, style);
						style.alignment = defaultAlignment;
						break;
					}
				}


				if (point.isInput) {
					// var activeFrameLabel = new GUIStyle("AnimationKeyframeBackground");// そのうちやる。
					// activeFrameLabel.backgroundColor = Color.clear;
					// Debug.LogError("contentOffset"+ activeFrameLabel.contentOffset);
					// Debug.LogError("contentOffset"+ activeFrameLabel.Button);

					GUI.backgroundColor = Color.clear;
					GUI.Button(point.buttonRect, inputPointTex, "AnimationKeyframeBackground");
				}

				if (point.isOutput) {
					GUI.backgroundColor = Color.clear;
					GUI.Button(point.buttonRect, outputPointTex, "AnimationKeyframeBackground");
				}
			}

			/*
				right click.
			*/
			if (
				Event.current.type == EventType.ContextClick
				 || (Event.current.type == EventType.MouseUp && Event.current.button == 1)
			) {
				var rightClickPos = Event.current.mousePosition;
				var menu = new GenericMenu();
				menu.AddItem(
					new GUIContent("Delete All Input Connections"),
					false, 
					() => {
						Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_DELETE_ALL_INPUT_CONNECTIONS, this, rightClickPos, null));
					}
				);
				menu.AddItem(
					new GUIContent("Delete All Output Connections"),
					false, 
					() => {
						Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_DELETE_ALL_OUTPUT_CONNECTIONS, this, rightClickPos, null));
					}
				);
				menu.AddItem(
					new GUIContent("Duplicate"),
					false, 
					() => {
						Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_DUPLICATE_TAPPED, this, rightClickPos, null));
					}
				);
				menu.AddItem(
					new GUIContent("Delete"),
					false, 
					() => {
						Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_CLOSE_TAPPED, this, Vector2.zero, null));
					}
				);
				menu.ShowAsContext();
				Event.current.Use();
			}


			DrawNodeContents();

			GUI.DragWindow();
		}

		public void DrawConnectionInputPointMark (OnNodeEvent eventSource, bool justConnecting) {
			var defaultPointTex = inputPointMarkTex;

			if (justConnecting && eventSource != null) {
				if (eventSource.eventSourceNode.nodeId != this.nodeId) {
					if (eventSource.eventSourceConnectionPoint.isOutput) {
						defaultPointTex = enablePointMarkTex;
					}
				}
			}

			foreach (var point in connectionPoints) {
				if (point.isInput) {
					GUI.DrawTexture(
						new Rect(
							baseRect.x - 2f, 
							baseRect.y + (baseRect.height - AssetGraphGUISettings.CONNECTION_POINT_MARK_SIZE)/2f, 
							AssetGraphGUISettings.CONNECTION_POINT_MARK_SIZE, 
							AssetGraphGUISettings.CONNECTION_POINT_MARK_SIZE
						), 
						defaultPointTex
					);
				}
			}
		}

		public void DrawConnectionOutputPointMark (OnNodeEvent eventSource, bool justConnecting, Event current) {
			var defaultPointTex = outputPointMarkConnectedTex;
			
			if (justConnecting && eventSource != null) {
				if (eventSource.eventSourceNode.nodeId != this.nodeId) {
					if (eventSource.eventSourceConnectionPoint.isInput) {
						defaultPointTex = enablePointMarkTex;
					}
				}
			}

			var globalMousePosition = current.mousePosition;
			
			foreach (var point in connectionPoints) {
				if (point.isOutput) {
					var outputPointRect = OutputRect(point);

					GUI.DrawTexture(
						outputPointRect, 
						defaultPointTex
					);

					// eventPosition is contained by outputPointRect.
					if (outputPointRect.Contains(globalMousePosition)) {
						if (current.type == EventType.MouseDown) {
							Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_CONNECT_STARTED, this, current.mousePosition, point));
						}
					}
				}
			}
		}

		private Rect OutputRect (ConnectionPoint outputPoint) {
			return new Rect(
				baseRect.x + baseRect.width - 8f, 
				baseRect.y + outputPoint.buttonRect.y + 1f, 
				AssetGraphGUISettings.CONNECTION_POINT_MARK_SIZE, 
				AssetGraphGUISettings.CONNECTION_POINT_MARK_SIZE
			);
		}

		private void DrawNodeContents () {
			var style = EditorStyles.label;
			var defaultAlignment = style.alignment;
			style.alignment = TextAnchor.MiddleCenter;
			

			var nodeTitleRect = new Rect(0, 0, baseRect.width, baseRect.height);
			if (this.kind == AssetGraphSettings.NodeKind.PREFABRICATOR_GUI) GUI.contentColor = Color.black;
			if (this.kind == AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT) GUI.contentColor = Color.black; 
			GUI.Label(nodeTitleRect, name, style);

			if (running) EditorGUI.ProgressBar(new Rect(10f, baseRect.height - 20f, baseRect.width - 20f, 10f), progress, string.Empty);

			style.alignment = defaultAlignment;
		}

		public void UpdateNodeRect () {
			var contentWidth = this.name.Length;
			if (this.kind == AssetGraphSettings.NodeKind.FILTER_GUI) {
				var longestFilterLengths = connectionPoints.OrderByDescending(con => con.label.Length).Select(con => con.label.Length).ToList();
				if (longestFilterLengths.Any()) {
					contentWidth = contentWidth + longestFilterLengths[0];
				}
			}

			var newWidth = contentWidth * 12f;
			if (newWidth < AssetGraphGUISettings.NODE_BASE_WIDTH) newWidth = AssetGraphGUISettings.NODE_BASE_WIDTH;
			baseRect = new Rect(baseRect.x, baseRect.y, newWidth, baseRect.height);

			RefreshConnectionPos();
		}

		private ConnectionPoint IsOverConnectionPoint (List<ConnectionPoint> points, Vector2 touchedPoint) {
			foreach (var p in points) {
				if (p.buttonRect.x <= touchedPoint.x && 
					touchedPoint.x <= p.buttonRect.x + p.buttonRect.width && 
					p.buttonRect.y <= touchedPoint.y && 
					touchedPoint.y <= p.buttonRect.y + p.buttonRect.height
				) {
					return p;
				}
			}
			
			return null;
		}

		public Rect GetRect () {
			return baseRect;
		}

		public Vector2 GetPos () {
			return baseRect.position;
		}

		public int GetX () {
			return (int)baseRect.x;
		}

		public int GetY () {
			return (int)baseRect.y;
		}

		public int GetRightPos () {
			return (int)(baseRect.x + baseRect.width);
		}

		public int GetBottomPos () {
			return (int)(baseRect.y + baseRect.height);
		}

		public void SetPos (Vector2 position) {
			baseRect.position = position;
		}

		public void SetProgress (float val) {
			progress = val;
		}

		public void MoveRelative (Vector2 diff) {
			baseRect.position = baseRect.position - diff;
		}

		public void ShowProgress () {
			running = true;
		}

		public void HideProgress () {
			running = false;
		}

		public bool ConitainsGlobalPos (Vector2 globalPos) {
			if (baseRect.Contains(globalPos)) {
				return true;
			}

			foreach (var connectionPoint in connectionPoints) {
				if (connectionPoint.isOutput) {
					var outputRect = OutputRect(connectionPoint);
					if (outputRect.Contains(globalPos)) {
						return true;
					}
				}
			}

			return false;
		}

		public Vector2 GlobalConnectionPointPosition(ConnectionPoint p) {
			var x = 0f;
			var y = 0f;

			if (p.isInput) {
				x = baseRect.x;
				y = baseRect.y + p.buttonRect.y + (p.buttonRect.height / 2f) - 1f;
			}

			if (p.isOutput) {
				x = baseRect.x + baseRect.width;
				y = baseRect.y + p.buttonRect.y + (p.buttonRect.height / 2f) - 1f;
			}

			return new Vector2(x, y);
		}

		public List<ConnectionPoint> ConnectionPointUnderGlobalPos (Vector2 globalPos) {
			var containedPoints = new List<ConnectionPoint>();

			foreach (var connectionPoint in connectionPoints) {
				var grobalConnectionPointRect = new Rect(
					baseRect.x + connectionPoint.buttonRect.x,
					baseRect.y + connectionPoint.buttonRect.y,
					connectionPoint.buttonRect.width,
					connectionPoint.buttonRect.height
				);

				if (grobalConnectionPointRect.Contains(globalPos)) containedPoints.Add(connectionPoint);
				if (connectionPoint.isOutput) {
					var outputRect = OutputRect(connectionPoint);
					if (outputRect.Contains(globalPos)) containedPoints.Add(connectionPoint);
				}
			}
			
			return containedPoints;
		}
	}
}