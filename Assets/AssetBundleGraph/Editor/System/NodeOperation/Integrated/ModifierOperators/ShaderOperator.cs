using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.ModifierOperators {
	
	[Serializable] public class ShaderOperator : ModifierBase {
		
		public ShaderOperator () {}
		
		private ShaderOperator (
			string operatorType
		) {
			this.operatorType = operatorType;
		}

		/*
			constructor for default data setting.
		*/
		public override ModifierBase DefaultSetting () {
			return new ShaderOperator(
				"UnityEngine.Shader"
			);
		}

		public override bool IsChanged<T> (T asset) {
			var shader = asset as Shader;
			
			var changed = false;

			/*
Variables

maximumLOD	Shader LOD level for this shader.
			*/

			return changed; 
		}

		public override void Modify<T> (T asset) {
			var shader = asset as Shader;
			
		}
		
		public override void DrawInspector (Action changed) {
			GUILayout.Label("ShaderOperator inspector.");
		}
	}
	
}