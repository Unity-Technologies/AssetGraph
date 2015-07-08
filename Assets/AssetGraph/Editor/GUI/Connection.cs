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

		public Connection (string label, Node start, ConnectionPoint output, Node end, ConnectionPoint input) {
			this.label = label;
			this.connectionId = Guid.NewGuid().ToString();

			this.startNode = start;
			this.outputPoint = output;
			this.endNode = end;
			this.inputPoint = input;

			this.startPointInfo = start.name + ":" + output.id;
			this.endPointInfo = end.name + ":" + input.id;
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