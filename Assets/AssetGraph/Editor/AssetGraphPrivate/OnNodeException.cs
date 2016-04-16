using System;

namespace AssetGraph {
	class OnNodeException : Exception {
		public readonly string reason;
		public readonly string nodeId;
		
		public OnNodeException (string reason, string nodeId) {
			this.reason = reason;
			this.nodeId = nodeId;
		}
	}
}