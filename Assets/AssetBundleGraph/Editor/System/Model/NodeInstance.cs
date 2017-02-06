using UnityEngine;
using System.Collections;

namespace UnityEngine.AssetBundles.GraphTool {
	[System.Serializable]
	public class NodeInstance : SerializedInstance<Node> {
		
		public NodeInstance() : base() {}
		public NodeInstance(NodeInstance instance): base(instance) {}
		public NodeInstance(Node obj) : base(obj) {}
	}
}
