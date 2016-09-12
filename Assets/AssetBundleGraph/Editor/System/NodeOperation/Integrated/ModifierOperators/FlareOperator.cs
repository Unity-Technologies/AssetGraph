using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.ModifierOperators {
	
	[Serializable] public class FlareOperator : ModifierBase {
		
		public FlareOperator () {}

		private FlareOperator (
			string operatorType
		) {
			this.operatorType = operatorType;
		}

		/*
			constructor for default data setting.
		*/
		public override ModifierBase DefaultSetting () {
			return new FlareOperator(
				"UnityEngine.Flare"
			);
		}

		public override bool IsChanged<T> (T asset) {
			var flare = asset as Flare;

			var changed = false;

			return changed; 
		}

		public override void Modify<T> (T asset) {
			var flare = asset as Flare;
			
		}
		
		public override void DrawInspector (Action changed) {
			GUILayout.Label("FlareOperator inspector.");
		}
	}

}