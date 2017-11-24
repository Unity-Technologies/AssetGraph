using System;

namespace UnityEngine.AssetGraph {
	public class NodeException : Exception {
        public readonly string Reason;
        public readonly string HowToFix;
        public readonly string NodeId;
        public readonly AssetReference Asset;
		
		public NodeException (string reason, string nodeId) {
			this.Reason = reason;
			this.NodeId = nodeId;
            this.Asset = null;
		}

        public NodeException (string reason, string howToFix, string nodeId) {
            this.Reason = reason;
            this.HowToFix = howToFix;
            this.NodeId = nodeId;
            this.Asset = null;
        }

        public NodeException (string reason, string howToFix, string nodeId, AssetReference a) {
            this.Reason = reason;
            this.HowToFix = howToFix;
            this.NodeId = nodeId;
            this.Asset = a;
        }
	}
}