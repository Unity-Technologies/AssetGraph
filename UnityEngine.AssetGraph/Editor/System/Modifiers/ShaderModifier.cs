using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

using UnityEngine.AssetBundles.GraphTool;
using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool.Modifiers {

	/*
	 * Code template for Shader modifier.
	 * You can copy and create your CustomModifier.
	 */ 

	[Serializable] 
	[CustomModifier("Default Modifier(Shader)", typeof(Shader))]
	public class ShaderModifier : IModifier {
		[SerializeField] int maximumLOD;

		public ShaderModifier () {}

		public bool IsModified (UnityEngine.Object[] assets) {
			var shader = assets[0] as Shader;

			var changed = false;

			if (shader.maximumLOD != this.maximumLOD) {
				changed = true; 
			}

			return changed; 
		}

		public void Modify (UnityEngine.Object[] assets) {
			var shader = assets[0] as Shader;

			shader.maximumLOD = this.maximumLOD;
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
	}
	
}