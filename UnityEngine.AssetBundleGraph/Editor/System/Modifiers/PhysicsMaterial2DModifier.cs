using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

using UnityEngine.AssetBundles.GraphTool;
using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool.Modifiers {

	/*
	 * Code template for PhysicsMaterial2D modifier.
	 * You can copy and create your CustomModifier.
	 */ 

	[Serializable] 
	[CustomModifier("Default Modifier(PhysicsMaterial2D)", typeof(PhysicsMaterial2D))]
	public class PhysicsMaterial2DModifier : IModifier {
		
		[SerializeField] public float friction;
		[SerializeField] public float bounciness;

		public PhysicsMaterial2DModifier () {
			
		}

		public bool IsModified (UnityEngine.Object[] assets) {
			var physicsMaterial2D = assets[0] as PhysicsMaterial2D;

			var changed = false;

			if (physicsMaterial2D.friction != this.friction) changed = true;
			if (physicsMaterial2D.bounciness != this.bounciness) changed = true; 

			return changed; 
		}

		public void Modify (UnityEngine.Object[] assets) {
			var physicsMaterial2D = assets[0] as PhysicsMaterial2D;

			physicsMaterial2D.friction = this.friction;
			physicsMaterial2D.bounciness = this.bounciness; 
		}

		public void OnInspectorGUI (Action onValueChanged) {
			var newFriction = EditorGUILayout.FloatField("Friction", friction);
			var newBounciness = EditorGUILayout.FloatField("Bounciness", bounciness);

			if(newFriction != friction) {
				friction = newFriction;
				onValueChanged();
			}

			if(newBounciness != bounciness) {
				bounciness = newBounciness;
				onValueChanged();
			}
		}
	}

}