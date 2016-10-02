using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.Modifiers {
	
	[Serializable] 
	[CustomModifier("Default Modifier(PhysicsMaterial2D)", typeof(PhysicsMaterial2D))]
	public class PhysicsMaterial2DModifier : IModifier {
		
		[SerializeField] public float friction;
		[SerializeField] public float bounciness;

		public PhysicsMaterial2DModifier () {
			
		}

		public bool IsModified (object asset) {
			var physicsMaterial2D = asset as PhysicsMaterial2D;

			var changed = false;

			if (physicsMaterial2D.friction != this.friction) changed = true;
			if (physicsMaterial2D.bounciness != this.bounciness) changed = true; 

			return changed; 
		}

		public void Modify (object asset) {
			var physicsMaterial2D = asset as PhysicsMaterial2D;

			physicsMaterial2D.friction = this.friction;
			physicsMaterial2D.bounciness = this.bounciness; 
		}

		public void OnInspectorGUI (Action onValueChanged) {
			//TODO: implement this
			GUILayout.Label(""+ friction);
			GUILayout.Label("bounciness");
		}

		public string Serialize() {
			return JsonUtility.ToJson(this);
		}
	}

}