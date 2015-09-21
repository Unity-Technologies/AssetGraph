namespace AssetGraph {
	public class OnConnectionEvent {
		public enum EventType : int {
			EVENT_NONE,
			
			EVENT_CONNECTION_MULTIPLE_SELECTION,

			EVENT_CONNECTION_TAPPED,
			EVENT_CONNECTION_DELETED,
		}

		public readonly EventType eventType;
		public readonly Connection eventSourceCon;

		public OnConnectionEvent (EventType type, Connection con) {
			this.eventType = type;
			this.eventSourceCon = con;
		}
	}
}