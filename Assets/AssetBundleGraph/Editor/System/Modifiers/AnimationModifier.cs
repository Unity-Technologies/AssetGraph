using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.Modifiers {
	
	[Serializable] 
	[CustomModifier("Default Modifier(Animation)", typeof(Animation))]
	public class AnimationModifier : IModifier {
		
		public AnimationModifier () {}

		public bool IsModified (UnityEngine.Object[] assets) {
//			var anim = assets[0] as Animation;

			// Do your work here

			var changed = false;
			return changed; 
		}

		public void Modify (UnityEngine.Object[] assets) {
//			var anim = assets[0] as Animation;

			// Do your work here
		}
		
		public void OnInspectorGUI (Action onValueChanged) {
			GUILayout.Label("Implement your modifier for this type.");
		}

		public string Serialize() {
			return JsonUtility.ToJson(this);
		}
	}
}