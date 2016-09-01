using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.ModifierOperators {
	
	[Serializable] public class PhysicMaterialOperator : OperatorBase {
		
		public PhysicMaterialOperator () {}

		private PhysicMaterialOperator (
			string operatorType
		) {
			this.operatorType = operatorType;
		}

		/*
			constructor for default data setting.
		*/
		public override OperatorBase DefaultSetting () {
			return new PhysicMaterialOperator(
				"UnityEngine.PhysicMaterial"
			);
		}

		public override bool IsChanged<T> (T asset) {
			var physicMaterial = asset as PhysicMaterial;

			var changed = false;

			/*
Variables

bounceCombine	Determines how the bounciness is combined.
bounciness	How bouncy is the surface? A value of 0 will not bounce. A value of 1 will bounce without any loss of energy.
dynamicFriction	The friction used when already moving. This value has to be between 0 and 1.
frictionCombine	Determines how the friction is combined.
staticFriction	The friction coefficient used when an object is lying on a surface.
			*/

			return changed; 
		}

		public override void Modify<T> (T asset) {
			var physicMaterial = asset as PhysicMaterial;
			
		}
		
		public override void DrawInspector (Action changed) {
			GUILayout.Label("PhysicMaterialOperator inspector.");
		}
	}

}