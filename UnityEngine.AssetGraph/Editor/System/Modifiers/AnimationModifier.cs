using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using UnityEngine.AssetGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph.Modifiers {
	
	[Serializable] 
	[CustomModifier("Default Modifier(Animation)", typeof(Animation))]
	public class AnimationModifier : IModifier {
		
		public AnimationModifier () {}

        public bool IsModified (UnityEngine.Object[] assets, List<AssetReference> group) {
//			var anim = assets[0] as Animation;

			// Do your work here

			var changed = false;
			return changed; 
		}

        public void Modify (UnityEngine.Object[] assets, List<AssetReference> group) {
//			var anim = assets[0] as Animation;

			// Do your work here
		}
		
		public void OnInspectorGUI (Action onValueChanged) {
			GUILayout.Label("Implement your modifier for this type.");
		}
	}
}