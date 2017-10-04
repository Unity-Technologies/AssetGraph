using System;
using System.Linq;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using UnityEngine.AssetGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph.Modifiers {
	
	/*
	 * Code template for Scene modifier.
	 * You can copy and create your CustomModifier.
	 */ 

	[Serializable] 
	[CustomModifier("Default Modifier(Scene)", typeof(UnityEngine.SceneManagement.Scene))]
	public class SceneModifier : IModifier {
		
		public SceneModifier () {}

        public bool IsModified (UnityEngine.Object[] assets, List<AssetReference> group) {
			//var anim = assets[0] as UnityEngine.SceneManagement.Scene;

			// Do your work here

			var changed = false;
			return changed; 
		}

        public void Modify (UnityEngine.Object[] assets, List<AssetReference> group) {
			//var anim = assets[0] as UnityEngine.SceneManagement.Scene;

			// Do your work here
		}

		public void OnInspectorGUI (Action onValueChanged) {
			GUILayout.Label("Implement your modifier for this type.");
		}
	}	
}