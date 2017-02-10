using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

using UnityEngine.AssetBundles.GraphTool;
using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool.Modifiers {

	/*
	 * Code template for Flare modifier.
	 * You can copy and create your CustomModifier.
	 */ 

	[Serializable] 
	[CustomModifier("Default Modifier(Flare)", typeof(Flare))]
	public class FlareModifier : IModifier {
		
		public FlareModifier () {}

		public bool IsModified (UnityEngine.Object[] assets) {
			//var anim = assets[0] as Flare;

			// Do your work here

			var changed = false;
			return changed; 
		}

		public void Modify (UnityEngine.Object[] assets) {
			//var anim = assets[0] as Flare;

			// Do your work here
		}

		public void OnInspectorGUI (Action onValueChanged) {
			GUILayout.Label("Implement your modifier for this type.");
		}
	}
}