using UnityEngine;
using System.Collections;

namespace UnityEngine.AssetBundles.GraphTool {
	[System.Serializable]
	public class PrefabBuilderInstance : SerializedInstance<IPrefabBuilder> {
		
		public PrefabBuilderInstance() : base() {}
		public PrefabBuilderInstance(PrefabBuilderInstance instance): base(instance) {}
		public PrefabBuilderInstance(IPrefabBuilder obj) : base(obj) {}
	}
}
