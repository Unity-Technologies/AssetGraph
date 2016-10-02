using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.Modifiers {
	
	[Serializable] 
	[CustomModifier("Default Modifier", typeof(PhysicMaterial))]
	public class PhysicMaterialModifier : IModifier {
		
		public PhysicMaterialModifier () {}

		public bool IsModified (object asset) {
			//var animation = asset as Animation;

			//TODO: implement this
			var changed = false;

			return changed; 
		}

		public void Modify (object asset) {
			//var flare = asset as Shader;
			//TODO: implement this
		}

		public void OnInspectorGUI (Action onValueChanged) {
			//TODO: implement this
			GUILayout.Label("AnimationModifier inspector.");
		}

		public string Serialize() {
			//TODO: implement this
			return null;
		}
	}

}