using UnityEngine;
using UnityEditor;


namespace AssetGraph {
	public class NodeInspector : ScriptableObject {
		public Node node;
		public void UpdateNode (Node node) {
			this.node = node;
		}

		public void Save () {
			Debug.LogError("保存ができるといいですね、更新のたびに発生すれば、それがNodeまで伝達されればいいわけだし。");
		}
	}
}