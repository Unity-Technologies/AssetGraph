using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.Modifiers {
	
	/*
	 * Code template for GUISkin modifier.
	 * You can copy and create your CustomModifier.
	 */ 

	[Serializable] 
	[CustomModifier("Default Modifier(GUISkin)", typeof(GUISkin))]
	public class GUISkinModifier : IModifier {
		
		public GUISkinModifier () {}

		public bool IsModified (object asset) {
			//var anim = asset as GUISkin;

			// Do your work here

			var changed = false;
			return changed; 
		}

		public void Modify (object asset) {
			//var anim = asset as GUISkin;

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