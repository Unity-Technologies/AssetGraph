using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.ModifierOperators {
	
	[Serializable] public class PhysicsMaterial2DOperator : ModifierBase {
		
		[SerializeField] public float friction;
		[SerializeField] public float bounciness;

		public PhysicsMaterial2DOperator () {}

		private PhysicsMaterial2DOperator (
			string operatorType
		) {
			this.operatorType = operatorType;
		}

		/*
			constructor for default data setting.
		*/
		public override ModifierBase DefaultSetting () {
			return new PhysicsMaterial2DOperator(
				"UnityEngine.PhysicsMaterial2D"
			);
		}

		public override bool IsChanged<T> (T asset) {
			var physicsMaterial2D = asset as PhysicsMaterial2D;

			var changed = false;
			
			if (physicsMaterial2D.friction != this.friction) changed = true;
			if (physicsMaterial2D.bounciness != this.bounciness) changed = true; 

			return changed; 
		}

		public override void Modify<T> (T asset) {
			var physicsMaterial2D = asset as PhysicsMaterial2D;
			
			physicsMaterial2D.friction = this.friction;
			physicsMaterial2D.bounciness = this.bounciness; 
		}
		
		public override void DrawInspector (Action changed) {
			GUILayout.Label(""+ friction);
			GUILayout.Label("bounciness");
		}
	}

}