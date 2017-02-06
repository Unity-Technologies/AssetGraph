using UnityEngine;
using System.Collections;

namespace UnityEngine.AssetBundles.GraphTool {
	[System.Serializable]
	public class MultiTargetPrefabBuilderInstance : SerializableMultiTargetValue<PrefabBuilderInstance> {
		
		public MultiTargetPrefabBuilderInstance(MultiTargetPrefabBuilderInstance rhs) : base(rhs) {}
		public MultiTargetPrefabBuilderInstance(PrefabBuilderInstance value) : base(value) {}
		public MultiTargetPrefabBuilderInstance() : base() {}
	}
}
