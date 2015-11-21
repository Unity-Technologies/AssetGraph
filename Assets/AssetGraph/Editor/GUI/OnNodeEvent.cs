using UnityEngine;

namespace AssetGraph {
	public class OnNodeEvent {
		public enum EventType : int {
			EVENT_NONE,

			EVENT_NODE_MOVING,

			EVENT_NODE_CONNECT_STARTED,
			EVENT_NODE_CONNECTION_OVERED,
			EVENT_NODE_CONNECTION_RAISED,
			

			EVENT_NODE_TOUCHED,

			EVENT_CONNECTIONPOINT_UPDATED,
			
			EVENT_DELETE_ALL_INPUT_CONNECTIONS,
			EVENT_DELETE_ALL_OUTPUT_CONNECTIONS,
			EVENT_DUPLICATE_TAPPED,
			EVENT_CLOSE_TAPPED,

			EVENT_SAVE,
			EVENT_SETUPWITHPACKAGE
		}

		public readonly EventType eventType;
		public readonly Node eventSourceNode;
		public readonly ConnectionPoint eventSourceConnectionPoint;
		public readonly Vector2 globalMousePosition;

		public OnNodeEvent (EventType type, Node node, Vector2 localMousePos, ConnectionPoint conPoint) {
			this.eventType = type;
			this.eventSourceNode = node;
			this.eventSourceConnectionPoint = conPoint;
			this.globalMousePosition = new Vector2(localMousePos.x + node.GetX(), localMousePos.y + node.GetY());
		}
	}
}