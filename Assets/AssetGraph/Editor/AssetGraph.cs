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

			// ノードを増やす
			nodes.Add(new Node(EmitEvent, nodes.Count, "node:" + nodes.Count, new Rect(10, 10, 200, 60)));
			nodes.Add(new Node(EmitEvent, nodes.Count, "node:" + nodes.Count, new Rect(310, 110, 210, 60)));
			nodes.Add(new Node(EmitEvent, nodes.Count, "node:" + nodes.Count, new Rect(310, 210, 210, 60)));

			// ポイントを増やす
			nodes.ForEach(node => node.AddConnectionPoint(new InputPoint("in")));
			nodes.ForEach(node => node.AddConnectionPoint(new OutputPoint("out0")));
			nodes.ForEach(node => node.AddConnectionPoint(new OutputPoint("out1")));
			
			// コネクションを増やす(仮に書いただけなので、実際にindexで扱うわけでは無い。)
			AddConnection(nodes[0], nodes[0].ConnectionPointAtIndex(1), nodes[1], nodes[1].ConnectionPointAtIndex(0));

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

		/**
			emit event from node-GUI.
		*/
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

								
								AddConnection(startNode,startConnectionPoint, endNode, endConnectionPoint);
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
									() => DeleteConnectionById(conId)
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

		/**
			create new connection if same relationship is not exist yet.
		*/
		private void AddConnection (Node startNode, ConnectionPoint startPoint, Node endNode, ConnectionPoint endPoint) {
			if (!connections.ContainsConnection(startNode, startPoint, endNode, endPoint)) {
				connections.Add(new Connection(Guid.NewGuid().ToString(), startNode, startPoint, endNode, endPoint));
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
			for (var i = 0; i < connections.Count; i++) {
				var con = connections[i];
				if (con.connectionId == connectionId) connections.Remove(con);
			}
		}
	}
}
