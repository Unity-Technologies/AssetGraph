using UnityEngine;
using System.Collections.Generic;

namespace UnityEngine.AssetBundles.GraphTool {
	[System.Serializable]
	public class MultiTargetModifierInstance : SerializableMultiTargetValue<ModifierInstance> {

		public MultiTargetModifierInstance(MultiTargetModifierInstance rhs) : base(rhs) {}
		public MultiTargetModifierInstance(ModifierInstance value) : base(value) {}
		public MultiTargetModifierInstance() : base() {}
	}
}
