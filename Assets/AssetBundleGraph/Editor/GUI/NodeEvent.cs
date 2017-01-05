using UnityEngine;

namespace AssetBundleGraph {
	public class NodeEvent {
		public enum EventType : int {
			EVENT_NONE,

			EVENT_NODE_MOVING,

			EVENT_NODE_CONNECT_STARTED,
			EVENT_NODE_CONNECTION_OVERED,
			EVENT_NODE_CONNECTION_RAISED,

			EVENT_NODE_UPDATED,

			EVENT_NODE_TOUCHED,

			EVENT_CONNECTIONPOINT_DELETED,
			EVENT_CONNECTIONPOINT_LABELCHANGED,

			EVENT_DELETE_ALL_CONNECTIONS_TO_POINT,
			
			EVENT_CLOSE_TAPPED,

			EVENT_RECORDUNDO,
			EVENT_SAVE,
		}

		public readonly EventType eventType;
		public readonly NodeGUI eventSourceNode;
		public readonly ConnectionPointData point;
		public readonly Vector2 globalMousePosition;
		public readonly string message;

		public NodeEvent (EventType type, NodeGUI node, Vector2 localMousePos, ConnectionPointData point) {
			this.eventType = type;
			this.eventSourceNode = node;
			this.point = point;
			this.globalMousePosition = new Vector2(localMousePos.x + node.GetX(), localMousePos.y + node.GetY());
		}

		public NodeEvent (EventType type, NodeGUI node) {
			this.eventType = type;
			this.eventSourceNode = node;
		}

		public NodeEvent (EventType type, string message) {
			this.eventType = type;
			this.message = message;
		}

		public NodeEvent (EventType type) {
			this.eventType = type;
		}
	}
}