using System;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.Modifiers {

	/*
	 * Code template for Material modifier.
	 * You can copy and create your CustomModifier.
	 */ 

	[Serializable] 
	[CustomModifier("Default Modifier(Material)", typeof(UnityEngine.Material))]
	public class MaterialModifier : IModifier {

		public enum BlendMode {
			Opaque,
			Cutout,
			Fade,
			Transparent
		}

		[SerializeField] public Shader shader;
		[SerializeField] public BlendMode blendMode;

		public MaterialModifier () {
			shader = Shader.Find("Standard");
			blendMode = BlendMode.Opaque;
		}

		public bool IsModified (UnityEngine.Object[] assets) {
			var mat = assets[0] as Material;

			var changed = false;

			if ((int)mat.GetFloat("_Mode") != (int)this.blendMode) {
				changed = true;
			}

			return changed; 
		}

		public void Modify (UnityEngine.Object[] assets) {
			var targetMat = assets[0] as Material;
			var currentMaterial = GenerateSettingMaterial();

			// set blend mode.
			targetMat.SetFloat("_Mode", (int)currentMaterial.GetFloat("_Mode"));
		}

		public void OnInspectorGUI (Action onValueChanged) {
			// blend mode.
			var newBlendMode = (BlendMode)EditorGUILayout.Popup("Rendering Mode", (int)blendMode, Enum.GetNames(typeof(BlendMode)), new GUILayoutOption[0]);
			if (newBlendMode != blendMode) {
				this.blendMode = newBlendMode;
				onValueChanged();
			}
		}

		public string Serialize() {
			return JsonUtility.ToJson(this);
		}

		private Material GenerateSettingMaterial () {
			var mat = new Material(this.shader);
			mat.SetFloat("_Mode", (int)this.blendMode); 
			
			return mat;
		}
	}

}