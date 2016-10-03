using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.Modifiers {
	
	[Serializable] 
	[CustomModifier("Default Modifier(Shader)", typeof(Shader))]
	public class ShaderModifier : IModifier {
		[SerializeField] int maximumLOD;

		public ShaderModifier () {}

		public bool IsModified (object asset) {
			var shader = asset as Shader;

			var changed = false;

			if (shader.maximumLOD != this.maximumLOD) changed = true; 
			// shader.isSupported // <- readonly
			// shader.renderQueue // <- readonly

			return changed; 
		}

		public void Modify (object asset) {
			var shader = asset as Shader;

			shader.maximumLOD = this.maximumLOD;
			// shader.isSupported // <- readonly
			// shader.renderQueue // <- readonly
		}

		public void OnInspectorGUI (Action onValueChanged) {
			using (new GUILayout.HorizontalScope()) {
				GUILayout.Label("Maximum LOD");

				var changedVal = (int)EditorGUILayout.Slider(this.maximumLOD, 0, 1000);
				if (changedVal != this.maximumLOD) {
					this.maximumLOD = changedVal;
					onValueChanged();
				}
			}
		}

		public string Serialize() {
			return JsonUtility.ToJson(this);
		}
	}
	
}