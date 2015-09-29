using UnityEngine;
using UnityEditor;

namespace AssetGraph {
	public class NodeInspector : ScriptableObject {
		public Node node;

		// [SerializeField] public string[] filterKeywords;

		public void UpdateNode (Node node) {
			this.node = node;
			// switch (this.node.kind) {
			// 	case AssetGraphSettings.NodeKind.FILTER_SCRIPT:
			// 	case AssetGraphSettings.NodeKind.FILTER_GUI: {
					
			// 		this.filterKeywords = new string[this.node.filterContainsKeywords.Count];
			// 		for (var i = 0; i < filterKeywords.Length; i++) {
			// 			filterKeywords[i] = this.node.filterContainsKeywords[i];
			// 		}
					
			// 		break;	
			// 	}
			// }
		}
	}
}