using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.Modifiers {
	
	[Serializable] 
	[CustomModifier("Default Modifier(Animator)", typeof(Animator))]
	public class AnimatorModifier : IModifier {
		
		public AnimatorModifier () {}

		public bool IsModified (object asset) {
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
			GUILayout.Label("TODO: inspector.");
		}

		public string Serialize() {
			return JsonUtility.ToJson(this);
		}
	}
}