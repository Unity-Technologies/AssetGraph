using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.Modifiers {
	
	[Serializable] 
	[CustomModifier("Default Modifier(Scene)", typeof(UnityEngine.SceneManagement.Scene))]
	public class SceneModifier : IModifier {
		
		public SceneModifier () {}

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
			GUILayout.Label("SceneModifier inspector.");
		}

		public string Serialize() {
			return JsonUtility.ToJson(this);
		}
	}	
}