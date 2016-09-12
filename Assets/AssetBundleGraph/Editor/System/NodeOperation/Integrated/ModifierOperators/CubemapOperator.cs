using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.ModifierOperators {
	
	[Serializable] public class CubemapOperator : ModifierBase {
		
		public CubemapOperator () {}

		private CubemapOperator (
			string operatorType
		) {
			this.operatorType = operatorType;
		}

		/*
			constructor for default data setting.
		*/
		public override ModifierBase DefaultSetting () {
			return new CubemapOperator(
				"UnityEngine.Cubemap"
			);
		}

		public override bool IsChanged<T> (T asset) {
			var cubemap = asset as Cubemap;

			var changed = false;

			return changed; 
		}

		public override void Modify<T> (T asset) {
			var cubemap = asset as Cubemap;

		}

		public override void DrawInspector (Action changed) {
			GUILayout.Label("CubemapOperator inspector.");
		}
	}

}