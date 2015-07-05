using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;

using MiniJSONForAssetGraph;
 

namespace AssetGraph {


	public class AssetGraph : EditorWindow {
		[MenuItem("AssetGraph/Open...")]
		public static void Open() {
			var window = GetWindow<AssetGraph>();
			window.InitializeGraph();
		}

		List<Node> nodes = new List<Node>();
		List<Connection> connections = new List<Connection>();

		private OnNodeEvent currentEventSource;

		public ConnectionPoint modifingConnnectionPoint;

		public enum ModifyMode : int {
			CONNECT_STARTED,
			CONNECT_ENDED,
		}
		private ModifyMode modifyMode;

		/**
			node window initializer.
			setup nodes, points and connections from saved data.
		*/
		public void InitializeGraph () {
			minSize = new Vector2(600f, 300f);
			
			wantsMouseMove = true;
			modifyMode = ModifyMode.CONNECT_ENDED;

			Debug.LogError("グラフの初期化処理を行う。保存されている形式データを読み込むのと、あとはコンパイル済みのデータからその更新を漁る。データ形式は一応JSONでいいや。");
			// var dataSourceFilePath = "適当なAssetGraph以下のフォルダから読み込み";
			// var dataSourceStr = string.Empty;
			// using (var sr = new StreamReader(dataSourceFilePath)) {
			// 	dataSourceStr = sr.ReadToEnd();
			// }
			// var jsonData = Json.Deserialize(dataSourceStr) as Dictionary<string,object>;
			// んで、ここですべてのノードとその枝の情報が手に入るはず。
			// {
			// 	"nodes":[
			// 		{
			// 			"id": "ID0",
			// 			"kind": "source",
			// 			"sourcePath": "なんかフォルダの位置とか一ファイルのパスとか。"
			// 		},
			// 		{
			// 			"id": "ID1",
			// 			"kind": "filter",
			// 			"outputs":[
			// 				{
			// 					"rabel": "ラベル2",
			// 					"to": "ID3"
			// 				}
			// 			]
			// 		},
			// 		{
			// 			"id": "ID2",
			// 			"kind": "importer",
			// 			"": 途中
			// 		}
			// 	]
			// }

			nodes.Add(new Node(EmitEvent, nodes.Count, "node:" + nodes.Count, new Rect(10, 10, 200, 60)));
			nodes.Add(new Node(EmitEvent, nodes.Count, "node:" + nodes.Count, new Rect(310, 110, 210, 60)));
			nodes.Add(new Node(EmitEvent, nodes.Count, "node:" + nodes.Count, new Rect(310, 210, 210, 60)));
		}

		void OnGUI () {
			// update node window x N
			{
				BeginWindows();
				
				nodes.ForEach(node => node.UpdateNodeRect());

				EndWindows();
			}

			connections.ForEach(con => con.DrawConnection());

			/*
				draw line if modifing connection.
			*/
			switch (modifyMode) {
				case ModifyMode.CONNECT_STARTED: {
					// from start node to mouse.
					DrawStraightLineFromCurrentEventSourcePointTo(Event.current.mousePosition);
					break;
				}
				case ModifyMode.CONNECT_ENDED: {
					// do nothing
					break;
				}
			}
		}

		private void DrawStraightLineFromCurrentEventSourcePointTo (Vector2 to) {
			var p = currentEventSource.eventSourceNode.GlobalConnectionPointPosition(currentEventSource.eventSourceConnectionPoint);
			Handles.DrawLine(new Vector3(p.x, p.y, 0f), new Vector3(to.x, to.y, 0f));
		}

		public void EmitEvent (OnNodeEvent e) {
			switch (modifyMode) {
				case ModifyMode.CONNECT_STARTED: {
					switch (e.eventType) {
						/*
							handling
						*/
						case OnNodeEvent.EventType.EVENT_CONNECTIONPOINT_HANDLING: {

							/*
								animate connectionPoint under mouse if this connectionPoint is able to accept this kind of connection.
							*/
							if (false) {
								var candidateNodes = NodesUnderPosition(e.globalMousePosition);

								if (!candidateNodes.Any()) break;
								var nodeUnderMouse = candidateNodes.Last();

								// ignore if target node is source itself.
								if (nodeUnderMouse == e.eventSourceNode) break;
								
								var candidatePoints = nodeUnderMouse.ConnectionPointUnderGlobalPos(e.globalMousePosition);

								var sourcePoint = currentEventSource.eventSourceConnectionPoint;

								// limit by connectable or not.
								var connectableCandidates = candidatePoints.Where(point => IsConnectablePointFromTo(sourcePoint, point)).ToList();
								if (!connectableCandidates.Any()) break;

								// connectable point is exist. change line color. 

								// or, do something..
								Debug.Log("connectable!");
							}
							break;
						}

						/*
							drop detected.
						*/
						case OnNodeEvent.EventType.EVENT_CONNECTIONPOINT_DROPPED: {
							// finish connecting mode.
							modifyMode = ModifyMode.CONNECT_ENDED;
							
							/*
								connect when dropped target is connectable from start connectionPoint.
							*/
							{
								var candidateNodes = NodesUnderPosition(e.globalMousePosition);

								if (!candidateNodes.Any()) break;
								var nodeUnderMouse = candidateNodes.Last();

								// ignore if target node is source itself.
								if (nodeUnderMouse == e.eventSourceNode) break;
								
								var candidatePoints = nodeUnderMouse.ConnectionPointUnderGlobalPos(e.globalMousePosition);

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

								// create new connection if same relations are not exist yet.
								if (!connections.ContainsConnection(startNode, startConnectionPoint, endNode, endConnectionPoint)) {
									connections.Add(new Connection(Guid.NewGuid().ToString(), startNode, startConnectionPoint, endNode, endConnectionPoint));
								}
							}
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
							start connection handling.
						*/
						case OnNodeEvent.EventType.EVENT_CONNECTIONPOINT_HANDLE_STARTED: {
							modifyMode = ModifyMode.CONNECT_STARTED;
							currentEventSource = e;
							break;
						}

						/*
							connectionPoint tapped.
						*/
						case OnNodeEvent.EventType.EVENT_CONNECTIONPOINT_RECEIVE_TAPPED: {
							var sourcePoint = e.eventSourceConnectionPoint;

							var relatedConnections = connections.
								Where(
									con => con.IsStartAtConnectionPoint(sourcePoint) || 
									con.IsEndAtConnectionPoint(sourcePoint)
								).
								ToList();

							/*
								show menuContext for control these connections.
							*/
							var menu = new GenericMenu();
							foreach (var con in relatedConnections) {
								var message = string.Empty;
								if (sourcePoint.isInput) message = "from " + con.startPointInfo;
								if (sourcePoint.isOutput) message = "to " + con.endPointInfo;
								
								var conId = con.connectionId;

								menu.AddItem(
									new GUIContent("delete connection:" + con.label + " " + message), 
									false, 
									() => {
										DeleteConnection(conId);
									}
								);
							}
							menu.ShowAsContext();
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

		private List<Node> NodesUnderPosition (Vector2 pos) {
			return nodes.Where(n => n.ConitainsGlobalPos(pos)).ToList();
		}

		private bool IsConnectablePointFromTo (ConnectionPoint sourcePoint, ConnectionPoint destPoint) {
			if (sourcePoint.isOutput != destPoint.isOutput && sourcePoint.isInput != destPoint.isInput) {
				return true;
			}
			return false;
		}

		private void DeleteConnection (string connectionId) {
			for (var i = 0; i < connections.Count; i++) {
				var con = connections[i];
				if (con.connectionId == connectionId) connections.Remove(con);
			}
		}
	}

	public class NodeEditorSetting {
		public const float POINT_SIZE = 15f;
	}


	public class OnNodeEvent {
		public enum EventType : int {
			EVENT_NONE,
			EVENT_CONNECTIONPOINT_HANDLE_STARTED,
			EVENT_CONNECTIONPOINT_HANDLING,
			EVENT_CONNECTIONPOINT_DROPPED,
			EVENT_CONNECTIONPOINT_RELEASED,

			EVENT_CONNECTIONPOINT_RECEIVE_TAPPED,
		}

		public readonly EventType eventType;
		public readonly Node eventSourceNode;
		public readonly ConnectionPoint eventSourceConnectionPoint;
		public readonly Vector2 globalMousePosition;

		public OnNodeEvent (EventType type, Node node, Vector2 localMousePos, ConnectionPoint conPoint) {
			this.eventType = type;
			this.eventSourceNode = node;
			this.eventSourceConnectionPoint = conPoint;
			this.globalMousePosition = new Vector2(localMousePos.x + node.baseRect.x, localMousePos.y + node.baseRect.y);
		}
	}

	public class Node {
		private Action<OnNodeEvent> Emit;

		private List<ConnectionPoint> connectionPoints = new List<ConnectionPoint>();

		private readonly int nodeWindowId;
		public readonly string nodeNameText;
		
		public Rect baseRect;

		public Node (Action<OnNodeEvent> emit, int id, string nodeNameText, Rect rect) {
			this.nodeWindowId = id;
			this.nodeNameText = nodeNameText;
			this.baseRect = rect;

			this.Emit = emit;

			AddConnectionPoint();
		}

		public void AddConnectionPoint () {
			Debug.Log("適当に足す。整列はその場で判断してやってくれればそれでいい。ポイント増えるときはリコンパイル走る関係で、個別ではなく一発で実行することになりそう。");
			Debug.Log("ここでのpointのidは最終的には何になるんだろうねえ");
			connectionPoints.Add(new InputPoint("p0"));
			connectionPoints.Add(new InputPoint("p1"));
			connectionPoints.Add(new OutputPoint("p2"));
			connectionPoints.Add(new OutputPoint("p3"));

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
				if (p.buttonRect.x < touchedPoint.x && 
					touchedPoint.x < p.buttonRect.x + p.buttonRect.width && 
					p.buttonRect.y < touchedPoint.y && 
					touchedPoint.y < p.buttonRect.y + p.buttonRect.height
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
			GUI.Label(nodeTitleRect, nodeNameText, style);

			style.alignment = defaultAlignment;
		}


		public Vector2 GetAbsolutePosFromConnectionPoint (Vector2 p) {
			// the mousePosition data is based on this node's Rect pos. not grobal position.
			var globalMousePos = Event.current.mousePosition + new Vector2(baseRect.x, baseRect.y);

			// adjust with connectionPoint's pos(this param is also node-relative.)
			var onConnectionPointMousePos = globalMousePos + p;

			return onConnectionPointMousePos;
		}

		public bool ConitainsGlobalPos (Vector2 globalPos) {
			if (baseRect.x < globalPos.x && 
				globalPos.x < baseRect.x + baseRect.width &&
				baseRect.y < globalPos.y && 
				globalPos.y < baseRect.y + baseRect.height) {
				return true;
			}
			return false;
		}

		public Vector2 GlobalConnectionPointPosition(ConnectionPoint p) {
			var x = baseRect.x + p.buttonRect.x;
			if (p.isInput) x = baseRect.x + p.buttonRect.x;
			if (p.isOutput) x = baseRect.x + p.buttonRect.x + NodeEditorSetting.POINT_SIZE;

			var y = baseRect.y + p.buttonRect.y + NodeEditorSetting.POINT_SIZE/2;

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


	public class ConnectionPoint {
		public readonly string id;
		public readonly bool isInput;
		public readonly bool isOutput;
		public Rect buttonRect;
		public string buttonStyle;

		public ConnectionPoint (string id, bool input, bool output) {
			this.id = id;
			this.isInput = input;
			this.isOutput = output;
		}

		public virtual void UpdatePos (int index, int max, float width, float height) {}

		public bool ContainsPosition (Vector2 localPos) {
			if (buttonRect.x < localPos.x && 
				localPos.x < buttonRect.x + buttonRect.width &&
				buttonRect.y < localPos.y && 
				localPos.y < buttonRect.y + buttonRect.height) {
				return true;
			}
			return false;
		}

		public virtual void ResetView () {
			buttonStyle = string.Empty;
		}
	}

	public class InputPoint : ConnectionPoint {
		public InputPoint (string id) : base (id, true, false) {
			ResetView();
		}
		
		public override void UpdatePos (int index, int max, float width, float height) {
			var y = ((height/(max + 1)) * (index + 1)) - NodeEditorSetting.POINT_SIZE/2f;
			buttonRect = new Rect(0,y, NodeEditorSetting.POINT_SIZE, NodeEditorSetting.POINT_SIZE);
		}

		public override void ResetView () {
			buttonStyle = "flow shader in 0";
		}
	}

	public class OutputPoint : ConnectionPoint {
		public OutputPoint (string id) : base (id, false, true) {
			ResetView();
		}

		public override void UpdatePos (int index, int max, float width, float height) {
			var y = ((height/(max + 1)) * (index + 1)) - NodeEditorSetting.POINT_SIZE/2f;
			buttonRect = new Rect(width - NodeEditorSetting.POINT_SIZE,y, NodeEditorSetting.POINT_SIZE, NodeEditorSetting.POINT_SIZE);
		}

		public override void ResetView () {
			buttonStyle = "flow shader out 0";
		}
	}


	public class Connection {
		public readonly string label;
		public readonly string connectionId;

		public readonly string startPointInfo;
		public readonly string endPointInfo;

		public readonly Node startNode;
		public readonly ConnectionPoint outputPoint;

		public readonly Node endNode;
		public readonly ConnectionPoint inputPoint;

		public Connection (string label, Node start, ConnectionPoint output, Node end, ConnectionPoint input) {
			this.label = label;
			this.connectionId = Guid.NewGuid().ToString();

			this.startNode = start;
			this.outputPoint = output;
			this.endNode = end;
			this.inputPoint = input;

			this.startPointInfo = start.nodeNameText + ":" + output.id;
			this.endPointInfo = end.nodeNameText + ":" + input.id;
		}

		public void DrawConnection () {
			var start = startNode.GlobalConnectionPointPosition(outputPoint);
			var startV3 = new Vector3(start.x, start.y, 0f);
			
			var end = endNode.GlobalConnectionPointPosition(inputPoint);
			var endV3 = new Vector3(end.x, end.y, 0f);
			
			var pointDistance = (end.x - start.x) / 2f;
			if (pointDistance < 20f) pointDistance = 20f;

			var startTan = new Vector3(start.x + pointDistance, start.y, 0f);
			var endTan = new Vector3(end.x - pointDistance, end.y, 0f);

			Handles.DrawBezier(startV3, endV3, startTan, endTan, Color.gray, null, 4f);
		}

		public bool IsStartAtConnectionPoint (ConnectionPoint p) {
			return outputPoint == p;
		}

		public bool IsEndAtConnectionPoint (ConnectionPoint p) {
			return inputPoint == p;
		}

		public bool IsSameDetail (Node start, ConnectionPoint output, Node end, ConnectionPoint input) {
			if (
				startNode == start &&
				outputPoint == output && 
				endNode == end &&
				inputPoint == input
			) {
				return true;
			}
			return false;
		}
	}

	public static class NodeEditor_ConnectionListExtension {
		public static bool ContainsConnection(this List<Connection> connections, Node start, ConnectionPoint output, Node end, ConnectionPoint input) {
			foreach (var con in connections) {
				if (con.IsSameDetail(start, output, end, input)) return true;
			}
			return false;
		}
	}
}
