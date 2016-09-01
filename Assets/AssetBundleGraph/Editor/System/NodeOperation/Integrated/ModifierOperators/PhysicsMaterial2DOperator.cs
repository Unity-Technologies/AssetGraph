using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.ModifierOperators {
	
	[Serializable] public class PhysicsMaterial2DOperator : OperatorBase {
		
		public PhysicsMaterial2DOperator () {}

		private PhysicsMaterial2DOperator (
			string operatorType
		) {
			this.operatorType = operatorType;
		}

		/*
			constructor for default data setting.
		*/
		public override OperatorBase DefaultSetting () {
			return new PhysicsMaterial2DOperator(
				"UnityEngine.PhysicsMaterial2D"
			);
		}

		public override bool IsChanged<T> (T asset) {
			var physicsMaterial2D = asset as PhysicsMaterial2D;

			var changed = false;
			
			/*
Variables

bounciness	The degree of elasticity during collisions.
friction	Coefficient of friction.
			*/

			return changed; 
		}

		public override void Modify<T> (T asset) {
			var physicsMaterial2D = asset as PhysicsMaterial2D;
			
		}
		
		public override void DrawInspector (Action changed) {
			GUILayout.Label("PhysicsMaterial2DOperator inspector.");
		}
	}

}