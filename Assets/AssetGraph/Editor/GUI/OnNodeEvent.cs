using UnityEngine;

namespace AssetGraph {
	public class OnNodeEvent {
		public enum EventType : int {
			EVENT_NONE,
			EVENT_CONNECTIONPOINT_HANDLE_STARTED,
			EVENT_CONNECTIONPOINT_HANDLING,
			EVENT_CONNECTIONPOINT_DROPPED,
			EVENT_CONNECTIONPOINT_RELEASED,

			EVENT_NODE_TAPPED,

			EVENT_CONNECTIONPOINT_RECEIVE_TAPPED,
			EVENT_CONNECTIONPOINT_UPDATED,
			
			EVENT_CLOSE_TAPPED,

			EVENT_SAVE,
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
}