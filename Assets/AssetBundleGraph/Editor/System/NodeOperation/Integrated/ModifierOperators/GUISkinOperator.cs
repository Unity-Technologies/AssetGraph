using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.ModifierOperators {
	
	[Serializable] public class GUISkinOperator : OperatorBase {
		
		public GUISkinOperator () {}

		private GUISkinOperator (
			string operatorType
		) {
			this.operatorType = operatorType;
		}

		/*
			constructor for default data setting.
		*/
		public override OperatorBase DefaultSetting () {
			return new GUISkinOperator(
				"UnityEngine.GUISkin"
			);
		}

		public override bool IsChanged<T> (T asset) {
			var guiSkin = asset as GUISkin;

			var changed = false;
			
			return changed; 
		}

		public override void Modify<T> (T asset) {
			var guiSkin = asset as GUISkin;
			
		}

		public override void DrawInspector (Action changed) {
			GUILayout.Label("GUISkinOperator inspector.");
		}
	}

}