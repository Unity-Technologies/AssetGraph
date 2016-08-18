using UnityEngine;

namespace AssetBundleGraph {
	public class OnNodeEvent {
		public enum EventType : int {
			EVENT_NONE,

			EVENT_NODE_MOVING,

			EVENT_NODE_CONNECT_STARTED,
			EVENT_NODE_CONNECTION_OVERED,
			EVENT_NODE_CONNECTION_RAISED,
			
			
			EVENT_NODE_TOUCHED,

			EVENT_CONNECTIONPOINT_DELETED,
			EVENT_CONNECTIONPOINT_LABELCHANGED,
			
			EVENT_CLOSE_TAPPED,

			EVENT_BEFORESAVE,
			EVENT_SAVE,
		}

		public readonly EventType eventType;
		public readonly Node eventSourceNode;
		public readonly string conPointId;
		public readonly Vector2 globalMousePosition;

		public OnNodeEvent (EventType type, Node node, Vector2 localMousePos, string conPointId) {
			this.eventType = type;
			this.eventSourceNode = node;
			this.conPointId = conPointId;
			this.globalMousePosition = new Vector2(localMousePos.x + node.GetX(), localMousePos.y + node.GetY());
		}
	}
}