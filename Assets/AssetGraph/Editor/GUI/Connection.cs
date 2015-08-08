using UnityEngine;
using UnityEditor;

using System;
using System.Collections.Generic;

namespace AssetGraph {
	public class Connection {
		public readonly string label;
		public readonly string connectionId;

		public readonly Node startNode;
		public readonly ConnectionPoint outputPoint;

		public readonly Node endNode;
		public readonly ConnectionPoint inputPoint;

		private readonly Action<OnConnectionEvent> Emit;

		public ConnectionInspector conInsp;

		private string connectionButtonStyle;

		public static Connection LoadConnection (Action<OnConnectionEvent> Emit, string label, string connectionId, Node start, ConnectionPoint output, Node end, ConnectionPoint input) {
			return new Connection(
				Emit,
				label,
				connectionId,
				start,
				output,
				end,
				input
			);
		}

		public static Connection NewConnection (Action<OnConnectionEvent> Emit, string label, Node start, ConnectionPoint output, Node end, ConnectionPoint input) {
			return new Connection(
				Emit,
				label,
				Guid.NewGuid().ToString(),
				start,
				output,
				end,
				input
			);
		}

		private Connection (Action<OnConnectionEvent> Emit, string label, string connectionId, Node start, ConnectionPoint output, Node end, ConnectionPoint input) {
			conInsp = ScriptableObject.CreateInstance<ConnectionInspector>();

			this.label = label;
			this.connectionId = connectionId;

			this.startNode = start;
			this.outputPoint = output;
			this.endNode = end;
			this.inputPoint = input;

			this.Emit = Emit;
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

				var foldouts = ((ConnectionInspector)target).foldouts;
				

				var count = 0;
				var throughputListDict = ((ConnectionInspector)target).throughputListDict;
				foreach (var list in throughputListDict.Values) {
					count += list.Count;
				}

				if (GUILayout.Button("Delete Connection")) {
					con.Delete();
				}
				EditorGUILayout.LabelField("Total", count.ToString());

				var index = 0;
				foreach (var groupKey in throughputListDict.Keys) {
					var list = throughputListDict[groupKey];

					var foldout = foldouts[index];
					
					foldout = EditorGUILayout.Foldout(foldout, "Group Key:" + groupKey);
					if (foldout) {
						EditorGUI.indentLevel = 1;
						for (var i = 0; i < list.Count; i++) {
							var sourceStr = list[i];
							EditorGUILayout.LabelField(sourceStr);
						}
						EditorGUI.indentLevel = 0;
					}
					foldouts[index] = foldout;

					index++;
				}
			}
		}

		public void DrawConnection (Dictionary<string, List<string>> throughputListDict) {
			var start = startNode.GlobalConnectionPointPosition(outputPoint);
			var startV3 = new Vector3(start.x, start.y, 0f);
			
			var end = endNode.GlobalConnectionPointPosition(inputPoint);
			var endV3 = new Vector3(end.x, end.y, 0f);
			
			var centerPoint = start + ((end - start) / 2);
			var centerPointV3 = new Vector3(centerPoint.x, centerPoint.y, 0f);

			var pointDistance = (end.x - start.x) / 2f;
			if (pointDistance < 20f) pointDistance = 20f;

			var startTan = new Vector3(start.x + pointDistance, start.y, 0f);
			var endTan = new Vector3(end.x - pointDistance, end.y, 0f);

			Handles.DrawBezier(startV3, endV3, startTan, endTan, Color.gray, null, 4f);

			// draw label.
			if (label != AssetGraphSettings.DEFAULT_OUTPUTPOINT_LABEL) {
				var labelPointV3 = new Vector3(centerPointV3.x - ((label.Length * 7f) / 2), centerPointV3.y - 24f, 0f) ;
				Handles.Label(labelPointV3, label);
			}

			/*
				draw throughtput badge.
			*/
			var throughputCount = 0;
			foreach (var list in throughputListDict.Values) {
				throughputCount += list.Count;
			}
			var offsetSize = throughputCount.ToString().Length * 20f;
			
			// var style = EditorStyles.boldLabel;

			// var defaultColor = style.normal.textColor;
			// var defaultAlignment = style.alignment;
			
			// if (throughputCount == 0) {
			// 	Debug.LogError("hahaa");
			// 	style.normal.textColor = Color.red;
			// }

			// style.alignment = TextAnchor.MiddleCenter;
			if (GUI.Button(new Rect(centerPointV3.x - offsetSize/2f, centerPointV3.y - 7f, offsetSize, 20f), throughputCount.ToString(), connectionButtonStyle)) {
				Emit(new OnConnectionEvent(OnConnectionEvent.EventType.EVENT_CONNECTION_TAPPED, this));

				conInsp.UpdateCon(this, throughputListDict);
				Selection.activeObject = conInsp;
			}
			
			// style.normal.textColor = defaultColor;
			// style.alignment = defaultAlignment;
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

		public void SetActive () {
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