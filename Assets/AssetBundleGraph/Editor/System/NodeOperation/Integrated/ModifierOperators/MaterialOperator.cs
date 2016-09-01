using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.ModifierOperators {
	[Serializable] public class MaterialOperator : OperatorBase {
		[SerializeField] public Shader shader;
		
		public enum BlendMode {
			Opaque,
			Cutout,
			Fade,
			Transparent
		}

		[SerializeField] public BlendMode blendMode;

		public MaterialOperator () {}

		private MaterialOperator (
			string operatorType,
			Shader shader,
			BlendMode blendMode
		) {
			this.operatorType = operatorType;
			
			this.shader = shader;
			this.blendMode = blendMode;
		}

		/*
			constructor for default data setting.
		*/
		public override OperatorBase DefaultSetting () {
			return new MaterialOperator(
				"UnityEngine.Material",
				Shader.Find("Standard"),
				BlendMode.Opaque
			);
		}

		public override bool IsChanged<T> (T asset) {
			var mat = asset as Material;

			var changed = false;
			
			if ((int)mat.GetFloat("_Mode") != (int)this.blendMode) {
				changed = true;
			}

			return changed; 
		}

		private Material GenerateSettingMaterial () {
			var mat = new Material(this.shader);
			mat.SetFloat("_Mode", (int)this.blendMode); 
			
			// Debug.LogError("GenerateSettingMaterial いろいろ指定する。");

			return mat;
		}

		public override void Modify<T> (T asset) {
			var targetMat = asset as Material;
			var currentMaterial = GenerateSettingMaterial();
			
			// set blend mode.
			targetMat.SetFloat("_Mode", (int)currentMaterial.GetFloat("_Mode"));
		}

		public override void DrawInspector (Action changed) {
			var currentMaterial = GenerateSettingMaterial();
			
			GUILayout.Label("Shader シェーダ一覧を読み出すコードがinternalなので辛い");
			// var newShader = ShaderPopup();
			// if (newShader != this.material.shader) {
			// 	this.material.shader = newShader;
			// 	changed();
			// }

			// blend mode.
			var newBlendMode = (BlendMode)EditorGUILayout.Popup("Rendering Mode", (int)blendMode, Enum.GetNames(typeof(BlendMode)), new GUILayoutOption[0]);
			if (newBlendMode != blendMode) {
				this.blendMode = newBlendMode;
				changed();
			}

			GUILayout.Label("このへんの要素が軒並みそのままの形だと読み出せない。materialEditorが作り出せない + internalなメソッド使ってて取り出せない。");
			GUILayout.Label("該当するような機構をこのクラスに抜き出して実装するかな、、");

			GUILayout.Label("Main Maps", EditorStyles.boldLabel);
			
			DoAlbedoArea(currentMaterial);
			// this.DoSpecularMetallicArea();
			// this.m_MaterialEditor.TexturePropertySingleLine(StandardShaderGUI.Styles.normalMapText, this.bumpMap, (!(this.bumpMap.textureValue != null)) ? null : this.bumpScale);
			// this.m_MaterialEditor.TexturePropertySingleLine(StandardShaderGUI.Styles.heightMapText, this.heightMap, (!(this.heightMap.textureValue != null)) ? null : this.heigtMapScale);
			// this.m_MaterialEditor.TexturePropertySingleLine(StandardShaderGUI.Styles.occlusionText, this.occlusionMap, (!(this.occlusionMap.textureValue != null)) ? null : this.occlusionStrength);
			// this.DoEmissionArea(material);
			// this.m_MaterialEditor.TexturePropertySingleLine(StandardShaderGUI.Styles.detailMaskText, this.detailMask);
			
			// // EditorGUI.BeginChangeCheck();
			
			// this.m_MaterialEditor.TextureScaleOffsetProperty(this.albedoMap);
			
			// if (EditorGUI.EndChangeCheck()) {
			// 	this.emissionMap.textureScaleAndOffset = this.albedoMap.textureScaleAndOffset;
			// }

			EditorGUILayout.Space();
			
			GUILayout.Label("Secondary Maps", EditorStyles.boldLabel);
			
			// this.m_MaterialEditor.TexturePropertySingleLine(StandardShaderGUI.Styles.detailAlbedoText, this.detailAlbedoMap);
			// this.m_MaterialEditor.TexturePropertySingleLine(StandardShaderGUI.Styles.detailNormalMapText, this.detailNormalMap, this.detailNormalMapScale);
			// this.m_MaterialEditor.TextureScaleOffsetProperty(this.detailAlbedoMap);
			// this.m_MaterialEditor.ShaderProperty(this.uvSetSecondary, StandardShaderGUI.Styles.uvSetLabel.text);
		}

		/**
			エディタ内部でしか使えないリスト関数とか取得関数使ってて変更できないのが悲しい。
		*/
		private void ShaderPopup() {
			// bool enabled = GUI.enabled;
			Rect rect = EditorGUILayout.GetControlRect(new GUILayoutOption[0]);
			rect = EditorGUI.PrefixLabel(rect, 47385, new GUIContent("Shader"));

			// EditorGUI.showMixedValue = this.HasMultipleMixedShaderValues();
			// GUIContent content = EditorGUIUtility.TempContent((!(this.shader != null)) ? "No Shader Selected" : this.shader.name);
			
			// if (EditorGUI.ButtonMouseDown(rect, content, EditorGUIUtility.native)) {
			// 	// EditorGUI.showMixedValue = false;
			// 	Vector2 vector = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));
			// 	InternalEditorUtility.SetupShaderMenu(this.target as Material);
			// 	EditorUtility.Internal_DisplayPopupMenu(new Rect(vector.x, vector.y, rect.width, rect.height), "CONTEXT/ShaderPopup", this, 0);
			// 	Event.current.Use();
			// }

			// EditorGUI.showMixedValue = false;
			// GUI.enabled = enabled;
		}

		private void DoAlbedoArea(Material material) {
			// var materialEditor = new MaterialEditor();// 落ちる
			// materialEditor.target = material;// できない
			
			// ShaderGUI系のメソッドでパラメータ取得したいんだけど、このへんの関数がのきなみinternalなので呼べない。
			// var albedoMap = ShaderGUI.FindProperty("_MainTex", props);
			// materialEditor.TexturePropertySingleLine(new GUIContent("Albedo", "Albedo (RGB) and Transparency (A)"), this.albedoMap, this.albedoColor);
			// if ((int)material.GetFloat("_Mode") == 1) {
			// 	materialEditor.ShaderProperty(this.alphaCutoff, StandardShaderGUI.Styles.alphaCutoffText.text, 3);
			// }
		}

		private void DoEmissionArea(Material material) {
			// bool flag = this.emissionScaleUI.floatValue > 0f;
			// bool flag2 = this.emissionMap.textureValue != null;
			// this.m_MaterialEditor.TexturePropertySingleLine(StandardShaderGUI.Styles.emissionText, this.emissionMap, (!flag) ? null : this.emissionColorUI, this.emissionScaleUI);
			
			// if (this.emissionMap.textureValue != null && !flag2 && this.emissionScaleUI.floatValue <= 0f) {
			// 	this.emissionScaleUI.floatValue = 1f;
			// }
			
			// if (flag) {
			// 	bool flag3 = StandardShaderGUI.ShouldEmissionBeEnabled(StandardShaderGUI.EvalFinalEmissionColor(material));
			// 	EditorGUI.BeginDisabledGroup(!flag3);
			// 	this.m_MaterialEditor.LightmapEmissionProperty(3);
			// 	EditorGUI.EndDisabledGroup();
			// }

			// if (!this.HasValidEmissiveKeyword(material)) {
			// 	EditorGUILayout.HelpBox(StandardShaderGUI.Styles.emissiveWarning.text, MessageType.Warning);
			// }
		}

		private void DoSpecularMetallicArea() {
			// if (this.m_WorkflowMode == StandardShaderGUI.WorkflowMode.Specular) {
			// 	if (this.specularMap.textureValue == null) {
			// 		this.m_MaterialEditor.TexturePropertyTwoLines(StandardShaderGUI.Styles.specularMapText, this.specularMap, this.specularColor, StandardShaderGUI.Styles.smoothnessText, this.smoothness);
			// 	} else {
			// 		this.m_MaterialEditor.TexturePropertySingleLine(StandardShaderGUI.Styles.specularMapText, this.specularMap);
			// 	}
			// } else {
			// 	if (this.m_WorkflowMode == StandardShaderGUI.WorkflowMode.Metallic) {
			// 		if (this.metallicMap.textureValue == null) {
			// 			this.m_MaterialEditor.TexturePropertyTwoLines(StandardShaderGUI.Styles.metallicMapText, this.metallicMap, this.metallic, StandardShaderGUI.Styles.smoothnessText, this.smoothness);
			// 		} else {
			// 			this.m_MaterialEditor.TexturePropertySingleLine(StandardShaderGUI.Styles.metallicMapText, this.metallicMap);
			// 		}
			// 	}
			// }
		}

		public static void SetupMaterialWithBlendMode(Material material, BlendMode blendMode) {
			switch (blendMode) {
				case BlendMode.Opaque:
					material.SetInt("_SrcBlend", 1);
					material.SetInt("_DstBlend", 0);
					material.SetInt("_ZWrite", 1);
					material.DisableKeyword("_ALPHATEST_ON");
					material.DisableKeyword("_ALPHABLEND_ON");
					material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
					material.renderQueue = -1;
					break;
				case BlendMode.Cutout:
					material.SetInt("_SrcBlend", 1);
					material.SetInt("_DstBlend", 0);
					material.SetInt("_ZWrite", 1);
					material.EnableKeyword("_ALPHATEST_ON");
					material.DisableKeyword("_ALPHABLEND_ON");
					material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
					material.renderQueue = 2450;
					break;
				case BlendMode.Fade:
					material.SetInt("_SrcBlend", 5);
					material.SetInt("_DstBlend", 10);
					material.SetInt("_ZWrite", 0);
					material.DisableKeyword("_ALPHATEST_ON");
					material.EnableKeyword("_ALPHABLEND_ON");
					material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
					material.renderQueue = 3000;
					break;
				case BlendMode.Transparent:
					material.SetInt("_SrcBlend", 1);
					material.SetInt("_DstBlend", 10);
					material.SetInt("_ZWrite", 0);
					material.DisableKeyword("_ALPHATEST_ON");
					material.DisableKeyword("_ALPHABLEND_ON");
					material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
					material.renderQueue = 3000;
					break;
			}
		}
	}

}