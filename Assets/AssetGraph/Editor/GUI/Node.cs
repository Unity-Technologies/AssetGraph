using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;


namespace AssetGraph {
	public class Node {
		private Action<OnNodeEvent> Emit;

		private List<ConnectionPoint> connectionPoints = new List<ConnectionPoint>();

		private readonly int nodeWindowId;

		public string name;
		public string id;
		public AssetGraphSettings.NodeKind kind;
		public string scriptPath;
		public string loadPath;
		public string exportPath;

		private readonly string nodeLabel;

		private string nodeInterfaceTypeStr;


		public NodeInspector nodeInsp;


		public Rect baseRect;
		private Rect closeButtonRect;
		
		public static Node LoaderNode (Action<OnNodeEvent> emit, int index, string name, string id, AssetGraphSettings.NodeKind kind, string loadPath, float x, float y) {
			return new Node(
				emit,
				index,
				name,
				id,
				kind,
				null,
				loadPath, 
				null, 
				x,
				y
			);
		}

		public static Node ExporterNode (Action<OnNodeEvent> emit, int index, string name, string id, AssetGraphSettings.NodeKind kind, string exportPath, float x, float y) {
			return new Node(
				emit,
				index,
				name,
				id,
				kind,
				null,
				null, 
				exportPath, 
				x,
				y
			);
		}

		public static Node ScriptNode (Action<OnNodeEvent> emit, int index, string name, string id, AssetGraphSettings.NodeKind kind, string scriptPath, float x, float y) {
			return new Node(
				emit,
				index,
				name,
				id,
				kind,
				scriptPath,
				null, 
				null, 
				x,
				y
			);
		}

		public static Node GUINode (Action<OnNodeEvent> emit, int index, string name, string id, AssetGraphSettings.NodeKind kind, float x, float y) {
			return new Node(
				emit,
				index,
				name,
				id,
				kind,
				null,
				null, 
				null, 
				x,
				y
			);
		}

		

		/**
			Inspector GUI
		*/
		[CustomEditor(typeof(NodeInspector))]
		public class NodeObj : Editor {
			
			public override void OnInspectorGUI () {
				var node = ((NodeInspector)target).node;
				if (node == null) return;

				EditorGUILayout.LabelField("name", node.name);
				EditorGUILayout.LabelField("kind", node.kind.ToString());
				
				switch (node.kind) {
					case AssetGraphSettings.NodeKind.LOADER_SCRIPT:
					case AssetGraphSettings.NodeKind.LOADER_GUI: {
						var newLoadPath = EditorGUILayout.TextArea(node.loadPath, GUILayout.MaxHeight(75));
						if (newLoadPath != node.loadPath) {
							Debug.LogWarning("本当は打ち込み単位の更新ではなくて、Finderからパス、、とかがいいんだと思うけど、今はパス。");
							node.loadPath = newLoadPath;
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

		public void Save () {
			Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_SAVE, this, Vector2.zero, null));
		}
		
		private Node (Action<OnNodeEvent> emit, int index, string name, string id, AssetGraphSettings.NodeKind kind, string scriptPath, string loadPath, string exportPath, float x, float y) {
			nodeInsp = ScriptableObject.CreateInstance<NodeInspector>();
			this.Emit = emit;
			this.nodeWindowId = index;
			this.name = name;
			this.id = id;
			this.kind = kind;
			this.scriptPath = scriptPath;
			this.loadPath = loadPath;
			this.exportPath = exportPath;
			
			this.baseRect = new Rect(x, y, NodeEditorSettings.NODE_BASE_WIDTH, NodeEditorSettings.NODE_BASE_HEIGHT);
			this.closeButtonRect = new Rect(0f, 0f, 18f, 18f);
			
			this.nodeLabel = string.Empty;
			
			switch (this.kind) {
				case AssetGraphSettings.NodeKind.LOADER_SCRIPT: {
					this.nodeInterfaceTypeStr = "flow node 0";
					var height = loadPath.Split(AssetGraphSettings.UNITY_FOLDER_SEPARATOR).Length;
					this.nodeLabel = this.loadPath.Replace("/", "\n/");
					this.baseRect = new Rect(baseRect.x, baseRect.y, baseRect.width, height * 16f);
					break;
				}
				case AssetGraphSettings.NodeKind.EXPORTER_SCRIPT: {
					this.nodeInterfaceTypeStr = "flow node 0";
					var height = exportPath.Split(AssetGraphSettings.UNITY_FOLDER_SEPARATOR).Length;
					this.nodeLabel = this.exportPath.Replace("/", "\n/");
					this.baseRect = new Rect(baseRect.x, baseRect.y, baseRect.width, height * 16f);
					break;
				}
				
				case AssetGraphSettings.NodeKind.LOADER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 0";
					this.nodeLabel = name;
					break;
				}
				case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 0";
					this.nodeLabel = name;
					break;
				}

				case AssetGraphSettings.NodeKind.FILTER_GUI:
				case AssetGraphSettings.NodeKind.IMPORTER_GUI:
				case AssetGraphSettings.NodeKind.GROUPING_GUI:
				case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI:
				case AssetGraphSettings.NodeKind.BUNDLIZER_GUI:
				case AssetGraphSettings.NodeKind.FILTER_SCRIPT:
				case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT:
				case AssetGraphSettings.NodeKind.GROUPING_SCRIPT:
				case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
				case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT: {
					this.nodeInterfaceTypeStr = "flow node 1";
					this.nodeLabel = name;
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
				case AssetGraphSettings.NodeKind.LOADER_SCRIPT: {
					this.nodeInterfaceTypeStr = "flow node 0 on";
					break;
				}
				case AssetGraphSettings.NodeKind.EXPORTER_SCRIPT: {
					this.nodeInterfaceTypeStr = "flow node 0 on";
					break;
				}

				case AssetGraphSettings.NodeKind.LOADER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 0 on";
					break;
				}
				case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 0 on";
					break;
				}

				case AssetGraphSettings.NodeKind.FILTER_GUI:
				case AssetGraphSettings.NodeKind.IMPORTER_GUI:
				case AssetGraphSettings.NodeKind.GROUPING_GUI:
				case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI:
				case AssetGraphSettings.NodeKind.BUNDLIZER_GUI:
				case AssetGraphSettings.NodeKind.FILTER_SCRIPT:
				case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT:
				case AssetGraphSettings.NodeKind.GROUPING_SCRIPT:
				case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
				case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT: {
					this.nodeInterfaceTypeStr = "flow node 1 on";
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
				case AssetGraphSettings.NodeKind.LOADER_GUI:
				case AssetGraphSettings.NodeKind.LOADER_SCRIPT: {
					this.nodeInterfaceTypeStr = "flow node 0";
					break;
				}
				case AssetGraphSettings.NodeKind.EXPORTER_GUI:
				case AssetGraphSettings.NodeKind.EXPORTER_SCRIPT: {
					this.nodeInterfaceTypeStr = "flow node 0";
					break;
				}

				case AssetGraphSettings.NodeKind.FILTER_GUI:
				case AssetGraphSettings.NodeKind.IMPORTER_GUI:
				case AssetGraphSettings.NodeKind.GROUPING_GUI:
				case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI:
				case AssetGraphSettings.NodeKind.BUNDLIZER_GUI:
				case AssetGraphSettings.NodeKind.FILTER_SCRIPT:
				case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT:
				case AssetGraphSettings.NodeKind.GROUPING_SCRIPT:
				case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
				case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT: {
					this.nodeInterfaceTypeStr = "flow node 1";
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
				this.baseRect = new Rect(baseRect.x, baseRect.y, baseRect.width, NodeEditorSettings.NODE_BASE_HEIGHT + (20 * (connectionPoints.Count - 3)));
			}

			// update all connection point's index.

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
			if (!targetPoints.Any()) throw new Exception("no connection label:" + label + " exists in node name:" + name);
			return targetPoints[0];
		}

		public void UpdateNodeRect () {
			baseRect = GUI.Window(nodeWindowId, baseRect, UpdateNodeEvent, string.Empty, nodeInterfaceTypeStr);
		}

		/**
			retrieve GUI events for this node.
		*/
		void UpdateNodeEvent (int id) {
			var currentEvent = Event.current.type;
			switch (currentEvent) {

				/*
					handling release of mouse drag from this node to another node.
					this node doesn't know about where the other node is. the master only knows.
					only emit event.
				*/
				case EventType.Ignore: {
					Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_CONNECTIONPOINT_DROPPED, this, Event.current.mousePosition, null));
					break;
				}
				/*
					handling release of mouse drag on this node.
					cancel connecting event.
				*/
				case EventType.MouseUp: {
					Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_CONNECTIONPOINT_RELEASED, this, Event.current.mousePosition, null));
					break;
				}

				/*
					handling drag.
				*/
				case EventType.MouseDrag: {
					Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_CONNECTIONPOINT_HANDLING, this, Event.current.mousePosition, null));
					break;
				}

				/*
					check if the mouse-down point is over one of the connectionPoint in this node.
					then emit event.
				*/
				case EventType.MouseDown: {
					var result = IsOverConnectionPoint(connectionPoints, Event.current.mousePosition);

					if (result != null) Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_CONNECTIONPOINT_HANDLE_STARTED, this, Event.current.mousePosition, result));
					else Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_TAPPED, this, Event.current.mousePosition, null));
					
					break;
				}
				
				default: {
					if (currentEvent == EventType.Layout) break;
					if (currentEvent == EventType.Repaint) break;
					if (currentEvent == EventType.mouseMove) break;
					// Debug.Log("other currentEvent:" + currentEvent);
					break;
				}
			}


			// draw & update connectionPoint button interface.
			foreach (var point in connectionPoints) {
				/*
					detect button-up event.
				*/
				var upInButtonRect = GUI.Button(point.buttonRect, string.Empty, point.buttonStyle);
				if (upInButtonRect) Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_CONNECTIONPOINT_RECEIVE_TAPPED, this, Event.current.mousePosition, point));
			}

			// draw & update close button interface.
			if (GUI.Button(closeButtonRect, string.Empty, "OL Minus")) {
				Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_CLOSE_TAPPED, this, Event.current.mousePosition, null));
			}


			OnGUI();
			GUI.DragWindow();
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



		/**
			GUI update for a Node.
		*/
		void OnGUI () {
			var style = EditorStyles.label;
			var defaultAlignment = style.alignment;
			style.alignment = TextAnchor.MiddleCenter;

			var nodeTitleRect = new Rect(0, 0, baseRect.width, baseRect.height);
			GUI.Label(nodeTitleRect, nodeLabel, style);

			style.alignment = defaultAlignment;
		}

		public bool ConitainsGlobalPos (Vector2 globalPos) {
			if (baseRect.x <= globalPos.x && 
				globalPos.x <= baseRect.x + baseRect.width &&
				baseRect.y <= globalPos.y && 
				globalPos.y <= baseRect.y + baseRect.height) {
				return true;
			}
			return false;
		}

		public Vector2 GlobalConnectionPointPosition(ConnectionPoint p) {
			var x = baseRect.x + p.buttonRect.x;
			if (p.isInput) x = baseRect.x + p.buttonRect.x;
			if (p.isOutput) x = baseRect.x + p.buttonRect.x + NodeEditorSettings.POINT_SIZE;

			var y = baseRect.y + p.buttonRect.y + NodeEditorSettings.POINT_SIZE/2;

			return new Vector2(x, y);
		}

		public List<ConnectionPoint> ConnectionPointUnderGlobalPos (Vector2 globalPos) {
			var localPos = globalPos - new Vector2(baseRect.x, baseRect.y);
			return connectionPoints.Where(conPos => conPos.ContainsPosition(localPos)).ToList();
		}

		public void ResetConnectionPointsViews () {
			connectionPoints.ForEach(c => c.ResetView());
		}
	}
}