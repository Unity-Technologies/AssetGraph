namespace AssetBundleGraph {
	public class OnConnectionEvent {
		public enum EventType : int {
			EVENT_NONE,

			EVENT_CONNECTION_TAPPED,
			EVENT_CONNECTION_DELETED,
		}

		public readonly EventType eventType;
		public readonly ConnectionGUI eventSourceCon;

		public OnConnectionEvent (EventType type, ConnectionGUI con) {
			this.eventType = type;
			this.eventSourceCon = con;
		}
	}
}