using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;

namespace AssetGraph {
	[Serializable] public class Connection {
		public static Action<OnConnectionEvent> Emit;
		
		public static Texture2D connectionArrowTex;

		[SerializeField] public string label;
		[SerializeField] public string connectionId;

		[SerializeField] public string startNodeId;
		[SerializeField] public ConnectionPoint outputPoint;

		[SerializeField] public string endNodeId;
		[SerializeField] public ConnectionPoint inputPoint;

		[SerializeField] public ConnectionInspector conInsp;

		[SerializeField] private string connectionButtonStyle;

		private Rect buttonRect;

		public static Connection LoadConnection (string label, string connectionId, string startNodeId, ConnectionPoint output, string endNodeId, ConnectionPoint input) {
			return new Connection(
				label,
				connectionId,
				startNodeId,
				output,
				endNodeId,
				input
			);
		}

		public static Connection NewConnection (string label, string startNodeId, ConnectionPoint output, string endNodeId, ConnectionPoint input) {
			return new Connection(
				label,
				Guid.NewGuid().ToString(),
				startNodeId,
				output,
				endNodeId,
				input
			);
		}

		private Connection (string label, string connectionId, string startNodeId, ConnectionPoint output, string endNodeId, ConnectionPoint input) {
			conInsp = ScriptableObject.CreateInstance<ConnectionInspector>();
			conInsp.hideFlags = HideFlags.DontSave;

			this.label = label;
			this.connectionId = connectionId;

			this.startNodeId = startNodeId;
			this.outputPoint = output;
			this.endNodeId = endNodeId;
			this.inputPoint = input;

			connectionButtonStyle = "sv_label_0";
		}

		/**
			Inspector GUI for this connection.
		*/
		[CustomEditor(typeof(ConnectionInspector))]
		public class ConnectionObj : Editor {

			public override void OnInspectorGUI () {
				var con = ((ConnectionInspector)target).con;
				if (con == null) return;

				EditorGUILayout.LabelField("connectionId:", con.connectionId);

				var foldouts = ((ConnectionInspector)target).foldouts;
				

				var count = 0;
				var throughputListDict = ((ConnectionInspector)target).throughputListDict;
				if (throughputListDict == null)  return;

				foreach (var throughputList in throughputListDict.Values) {
					count += throughputList.Count;
				}

				EditorGUILayout.LabelField("Total", count.ToString());

				var index = 0;
				foreach (var groupKey in throughputListDict.Keys) {
					var throughputList = throughputListDict[groupKey];

					var foldout = foldouts[index];
					
					foldout = EditorGUILayout.Foldout(foldout, "Group Key:" + groupKey);
					if (foldout) {
						EditorGUI.indentLevel = 1;
						for (var i = 0; i < throughputList.Count; i++) {
							var sourceStr = throughputList[i];
							EditorGUILayout.LabelField(sourceStr);
						}
						EditorGUI.indentLevel = 0;
					}
					foldouts[index] = foldout;

					index++;
				}
			}
		}

		public Rect GetRect () {
			return buttonRect;
		}

		public void DrawConnection (List<Node> nodes, Dictionary<string, List<string>> throughputListDict) {
			var startNodes = nodes.Where(node => node.nodeId == startNodeId).ToList();
			if (!startNodes.Any()) return;

			var start = startNodes[0].GlobalConnectionPointPosition(outputPoint);
			var startV3 = new Vector3(start.x, start.y, 0f);

			var endNodes = nodes.Where(node => node.nodeId == endNodeId).ToList();
			if (!endNodes.Any()) return;

			var end = endNodes[0].GlobalConnectionPointPosition(inputPoint);
			var endV3 = new Vector3(end.x, end.y + 1f, 0f);
			
			var centerPoint = start + ((end - start) / 2);
			var centerPointV3 = new Vector3(centerPoint.x, centerPoint.y, 0f);

			var pointDistance = (end.x - start.x) / 3f;
			if (pointDistance < AssetGraphGUISettings.CONNECTION_CURVE_LENGTH) pointDistance = AssetGraphGUISettings.CONNECTION_CURVE_LENGTH;

			var startTan = new Vector3(start.x + pointDistance, start.y, 0f);
			var endTan = new Vector3(end.x - pointDistance, end.y, 0f);

			Handles.DrawBezier(startV3, endV3, startTan, endTan, Color.gray, null, 4f);

			// draw connection label.
			if (label != AssetGraphSettings.DEFAULT_OUTPUTPOINT_LABEL) {
				var labelPointV3 = new Vector3(centerPointV3.x - ((label.Length * 7f) / 2), centerPointV3.y - 24f, 0f) ;
				Handles.Label(labelPointV3, label);
			}

			// draw connection arrow.
			GUI.DrawTexture(
				new Rect(
					endV3.x - AssetGraphGUISettings.CONNECTION_ARROW_WIDTH + 4f, 
					endV3.y - (AssetGraphGUISettings.CONNECTION_ARROW_HEIGHT / 2f) - 1f, 
					AssetGraphGUISettings.CONNECTION_ARROW_WIDTH, 
					AssetGraphGUISettings.CONNECTION_ARROW_HEIGHT
				), 
				connectionArrowTex
			);

			/*
				draw throughtput badge.
			*/
			var throughputCount = 0;
			foreach (var list in throughputListDict.Values) {
				throughputCount += list.Count;
			}
			var offsetSize = throughputCount.ToString().Length * 20f;
			
			buttonRect = new Rect(centerPointV3.x - offsetSize/2f, centerPointV3.y - 7f, offsetSize, 20f);

			if (
				Event.current.type == EventType.ContextClick
				|| (Event.current.type == EventType.MouseUp && Event.current.button == 1)
			) {
				var rightClickPos = Event.current.mousePosition;
				if (buttonRect.Contains(rightClickPos)) {
					var menu = new GenericMenu();
					menu.AddItem(
						new GUIContent("Delete"),
						false, 
						() => {
							Delete();
						}
					);
					menu.ShowAsContext();
					Event.current.Use();
				}
			}

			if (GUI.Button(buttonRect, throughputCount.ToString(), connectionButtonStyle)) {
				conInsp.UpdateCon(this, throughputListDict);
				Emit(new OnConnectionEvent(OnConnectionEvent.EventType.EVENT_CONNECTION_TAPPED, this));
			}
		}

		public bool IsStartAtConnectionPoint (ConnectionPoint p) {
			return outputPoint == p;
		}

		public bool IsEndAtConnectionPoint (ConnectionPoint p) {
			return inputPoint == p;
		}

		public bool IsSameDetail (Node start, ConnectionPoint output, Node end, ConnectionPoint input) {
			if (
				startNodeId == start.nodeId &&
				outputPoint == output && 
				endNodeId == end.nodeId &&
				inputPoint == input
			) {
				return true;
			}
			return false;
		}

		public void SetActive () {
			Selection.activeObject = conInsp;
			connectionButtonStyle = "sv_label_1";
		}

		public void SetInactive () {
			connectionButtonStyle = "sv_label_0";
		}

		public void Delete () {
			Emit(new OnConnectionEvent(OnConnectionEvent.EventType.EVENT_CONNECTION_DELETED, this));
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