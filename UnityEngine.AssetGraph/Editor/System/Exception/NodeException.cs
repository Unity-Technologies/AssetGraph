using System;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
	public class NodeException : Exception {
        public readonly string Reason;
        public readonly string HowToFix;
        public readonly Model.NodeData Node;
        public readonly AssetReference Asset;

        public string NodeId {
            get {
                return Node.Id;
            }
        }

        public NodeException (string reason, Model.NodeData node) {
            this.Reason = reason;
            this.Node = node;
            this.Asset = null;
        }

        public NodeException (string reason, string howToFix, Model.NodeData node) {
            this.Reason = reason;
            this.HowToFix = howToFix;
            this.Node = node;
            this.Asset = null;
        }

        public NodeException (string reason, string howToFix, Model.NodeData node, AssetReference a) {
            this.Reason = reason;
            this.HowToFix = howToFix;
            this.Node = node;
            this.Asset = a;
        }
	}
}