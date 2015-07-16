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

		public readonly string name;
		public readonly string id;
		public readonly AssetGraphSettings.NodeKind kind;
		public readonly string scriptPath;
		public readonly string loadPath;
		public readonly string exportPath;

		
		public Rect baseRect;
		
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

		public static Node DefaultNode (Action<OnNodeEvent> emit, int index, string name, string id, AssetGraphSettings.NodeKind kind, string scriptPath, float x, float y) {
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

		private Node (Action<OnNodeEvent> emit, int index, string name, string id, AssetGraphSettings.NodeKind kind, string scriptPath, string loadPath, string exportPath, float x, float y) {
			this.Emit = emit;
			this.nodeWindowId = index;
			this.name = name;
			this.id = id;
			this.kind = kind;
			this.scriptPath = scriptPath;
			this.loadPath = loadPath;
			this.exportPath = exportPath;
			this.baseRect = new Rect(x, y, NodeEditorSettings.NODE_BASE_WIDTH, NodeEditorSettings.NODE_BASE_HEIGHT);
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
			baseRect = GUI.Window(nodeWindowId, baseRect, UpdateNodeEvent, string.Empty, "flow node 1");
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
			GUI.Label(nodeTitleRect, name, style);

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