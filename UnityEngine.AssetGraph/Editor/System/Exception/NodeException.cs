using System;

namespace UnityEngine.AssetGraph {
	public class NodeException : Exception {
		public readonly string reason;
		public readonly string Id;
		
		public NodeException (string reason, string nodeId) {
			this.reason = reason;
			this.Id = nodeId;
		}
	}
}