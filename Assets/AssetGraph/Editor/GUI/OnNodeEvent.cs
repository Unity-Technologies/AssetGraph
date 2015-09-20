using UnityEngine;

namespace AssetGraph {
	public class OnNodeEvent {
		public enum EventType : int {
			EVENT_NONE,
			EVENT_NODE_HANDLE_STARTED,
			EVENT_NODE_HANDLING,
			EVENT_NODE_DROPPED,
			EVENT_NODE_CONNECTION_RAISED,
			EVENT_NODE_RELEASED,

			EVENT_NODE_TAPPED,

			EVENT_CONNECTIONPOINT_UPDATED,
			
			EVENT_DELETE_ALL_INPUT_CONNECTIONS,
			EVENT_DELETE_ALL_OUTPUT_CONNECTIONS,
			EVENT_DUPLICATE_TAPPED,
			EVENT_CLOSE_TAPPED,

			EVENT_SAVE,
			EVENT_RELOAD,
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