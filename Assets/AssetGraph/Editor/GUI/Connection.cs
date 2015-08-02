using UnityEngine;
using UnityEditor;

using System;
using System.Collections.Generic;

namespace AssetGraph {
	public class Connection {
		public readonly string label;
		public readonly string connectionId;

		public readonly string startPointInfo;
		public readonly string endPointInfo;

		public readonly Node startNode;
		public readonly ConnectionPoint outputPoint;

		public readonly Node endNode;
		public readonly ConnectionPoint inputPoint;

		public ConnectionInspector conInsp;

		public static Connection LoadConnection (string label, string connectionId, Node start, ConnectionPoint output, Node end, ConnectionPoint input) {
			return new Connection(
				label,
				connectionId,
				start,
				output,
				end,
				input
			);
		}

		public static Connection NewConnection (string label, Node start, ConnectionPoint output, Node end, ConnectionPoint input) {
			return new Connection(
				label,
				Guid.NewGuid().ToString(),
				start,
				output,
				end,
				input
			);
		}

		private Connection (string label, string connectionId, Node start, ConnectionPoint output, Node end, ConnectionPoint input) {
			conInsp = ScriptableObject.CreateInstance<ConnectionInspector>();

			this.label = label;
			this.connectionId = connectionId;

			this.startNode = start;
			this.outputPoint = output;
			this.endNode = end;
			this.inputPoint = input;

			this.startPointInfo = start.name + ":" + output.label;
			this.endPointInfo = end.name + ":" + input.label;
		}

		/**
			Inspector GUI for this connection.
		*/
		[CustomEditor(typeof(ConnectionInspector))]
		public class ConnectionObj : Editor {
			public override void OnInspectorGUI () {
				var con = ((ConnectionInspector)target).con;
				if (con == null) return;

				var throughputDataList = ((ConnectionInspector)target).throughputDataList;
				EditorGUILayout.LabelField("count", throughputDataList.Count.ToString());

				for (var i = 0; i < throughputDataList.Count; i++) {
					var data = throughputDataList[i];
					EditorGUILayout.LabelField((i + 1).ToString(), data);
				}
			}
		}

		public void DrawConnection (List<string> throughputDataList) {
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
			var throughputCount = throughputDataList.Count;
			var offsetSize = throughputCount.ToString().Length * 20f;
			if (GUI.Button(new Rect(centerPointV3.x - offsetSize/2f, centerPointV3.y - 10f, offsetSize, 20f), throughputCount.ToString(), "sv_label_0")) {
				conInsp.UpdateCon(this, throughputDataList);
				Selection.activeObject = conInsp;
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