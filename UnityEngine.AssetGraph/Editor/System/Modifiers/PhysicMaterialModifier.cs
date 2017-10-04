using System;
using System.Linq;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using UnityEngine.AssetGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph.Modifiers {

	/*
	 * Code template for PhysicMaterial modifier.
	 * You can copy and create your CustomModifier.
	 */ 

	[Serializable] 
	[CustomModifier("Default Modifier(PhysicMaterial)", typeof(PhysicMaterial))]
	public class PhysicMaterialModifier : IModifier {
		
		public PhysicMaterialModifier () {}

        public bool IsModified (UnityEngine.Object[] assets, List<AssetReference> group) {
			//var anim = assets[0] as PhysicMaterial;

			// Do your work here

			var changed = false;
			return changed; 
		}

        public void Modify (UnityEngine.Object[] assets, List<AssetReference> group) {
			//var anim = assets[0] as PhysicMaterial;

			// Do your work here
		}

		public void OnInspectorGUI (Action onValueChanged) {
			GUILayout.Label("Implement your modifier for this type.");
		}
	}

}