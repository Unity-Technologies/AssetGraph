using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

using UnityEngine.AssetBundles.GraphTool;
using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool.Modifiers {

	/*
	 * Code template for RenderTexture modifier.
	 * You can copy and create your CustomModifier.
	 */ 

	[Serializable] 
	[CustomModifier("Default Modifier(RenderTexture)", typeof(RenderTexture))]
	public class RenderTextureModifier : IModifier {

		public enum DepthBuffer : int {
			NoDepthBuffer = 0,
			_16bitDepth = 16,
			_24bitDepth = 24
		}
		[SerializeField] public UnityEngine.TextureWrapMode wrapMode;
		[SerializeField] public UnityEngine.FilterMode filterMode;
		[SerializeField] public int anisoLevel;// limit to 16.



		public RenderTextureModifier () {
			wrapMode = TextureWrapMode.Clamp;
			filterMode = FilterMode.Bilinear;
			anisoLevel = 0;
		}

		public bool IsModified (UnityEngine.Object[] assets) {
			var renderTex = assets[0] as RenderTexture;

			var changed = false;

			if (renderTex.wrapMode != this.wrapMode) changed = true; 
			if (renderTex.filterMode != this.filterMode) changed = true; 
			if (renderTex.anisoLevel != this.anisoLevel) changed = true;

			return changed; 
		}

		public void Modify (UnityEngine.Object[] assets) {
			var renderTex = assets[0] as RenderTexture;

			renderTex.wrapMode = this.wrapMode;
			renderTex.filterMode = this.filterMode;

			/*
				depth parameter cannot change from code.
				and anisoLevel can be change if asset's depth is 0. 
			*/
			if (renderTex.depth == (int)DepthBuffer.NoDepthBuffer) {
				renderTex.anisoLevel = this.anisoLevel;
			}
		}

		public void OnInspectorGUI (Action onValueChanged) {
			// wrapMode
			var newWrapMode = (UnityEngine.TextureWrapMode)EditorGUILayout.Popup("Wrap Mode", (int)this.wrapMode, Enum.GetNames(typeof(UnityEngine.TextureWrapMode)), new GUILayoutOption[0]);
			if (newWrapMode != this.wrapMode) {
				this.wrapMode = newWrapMode;
				onValueChanged();
			}

			// filterMode
			var newFilterMode = (UnityEngine.FilterMode)EditorGUILayout.Popup("Filter Mode", (int)this.filterMode, Enum.GetNames(typeof(UnityEngine.FilterMode)), new GUILayoutOption[0]);
			if (newFilterMode != this.filterMode) {
				this.filterMode = newFilterMode;
				onValueChanged();
			}

			// anisoLevel
			using (new GUILayout.HorizontalScope()) {
				GUILayout.Label("Aniso Level");

				var changedVal = (int)EditorGUILayout.Slider(this.anisoLevel, 0, 16);
				if (changedVal != this.anisoLevel) {
					this.anisoLevel = changedVal;
					onValueChanged();
				}
			}
			EditorGUILayout.HelpBox("Aniso Level can be set only when RenderTexture does not have depth buffer.", MessageType.Info);
		}
	}
}