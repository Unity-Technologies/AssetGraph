using System;

namespace AssetBundleGraph {
	public class NodeException : Exception {
		public readonly string reason;
		public readonly string nodeId;
		
		public NodeException (string reason, string nodeId) {
			this.reason = reason;
			this.nodeId = nodeId;
		}
	}
}