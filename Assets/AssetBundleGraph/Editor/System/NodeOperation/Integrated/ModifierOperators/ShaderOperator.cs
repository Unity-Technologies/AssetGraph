using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.ModifierOperators {
	
	[Serializable] public class ShaderOperator : ModifierBase {
		[SerializeField] int maximumLOD;

		public ShaderOperator () {}
		
		private ShaderOperator (
			string operatorType,
			int maximumLOD
		) {
			this.operatorType = operatorType;
			this.maximumLOD = maximumLOD;
		}

		/*
			constructor for default data setting.
		*/
		public override ModifierBase DefaultSetting () {
			return new ShaderOperator(
				"UnityEngine.Shader",
				200// based on default shader LOD from shader's inspector.
			);
		}

		public override bool IsChanged<T> (T asset) {
			var shader = asset as Shader;
			
			var changed = false;

			if (shader.maximumLOD != this.maximumLOD) changed = true; 
			// shader.isSupported // <- readonly
			// shader.renderQueue // <- readonly

			return changed; 
		}

		public override void Modify<T> (T asset) {
			var shader = asset as Shader;

			shader.maximumLOD = this.maximumLOD;
			// shader.isSupported // <- readonly
			// shader.renderQueue // <- readonly
		}
		
		public override void DrawInspector (Action changed) {
			using (new GUILayout.HorizontalScope()) {
				GUILayout.Label("Maximum LOD");
				
				var changedVal = (int)EditorGUILayout.Slider(this.maximumLOD, 0, 1000);
				if (changedVal != this.maximumLOD) {
					this.maximumLOD = changedVal;
					changed();
				}
			}
		}
	}
	
}