using UnityEngine;
using System.Collections;

namespace UnityEngine.AssetBundles.GraphTool {
	[System.Serializable]
	public class ModifierInstance : SerializedInstance<IModifier> {
		
		public ModifierInstance() : base() {}
		public ModifierInstance(ModifierInstance instance): base(instance) {}
		public ModifierInstance(IModifier obj) : base(obj) {}
	}
}
