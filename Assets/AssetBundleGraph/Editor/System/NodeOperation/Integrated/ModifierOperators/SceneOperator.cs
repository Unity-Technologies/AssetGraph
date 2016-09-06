using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.ModifierOperators {
	
	[Serializable] public class SceneOperator : OperatorBase {
		
		public SceneOperator () {}
		
		private SceneOperator (
			string operatorType
		) {
			this.operatorType = operatorType;
		}

		/*
			constructor for default data setting.
		*/
		public override OperatorBase DefaultSetting () {
			return new SceneOperator(
				"UnityEngine.SceneManagement.Scene"
			);
		}

		public override bool IsChanged<T> (T asset) {
			//var scene = asset as SceneAsset;
			
			var changed = false;
			
			return changed; 
		}

		public override void Modify<T> (T asset) {
			//var scene = asset as SceneAsset;
			
		}
		
		public override void DrawInspector (Action changed) {
			GUILayout.Label("SceneOperator inspector.");
		}
	}
	
}