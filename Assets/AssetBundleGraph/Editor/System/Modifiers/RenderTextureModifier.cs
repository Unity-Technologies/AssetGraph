using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.Modifiers {

	[Serializable] 
	[CustomModifier("Default Modifier(RenderTexture)", typeof(RenderTexture))]
	public class RenderTextureModifier : IModifier {
		// [SerializeField] public Int32 width, height;

		// public enum AntiAliasing : int {
		// 	None = 1,
		// 	_2_Samples = 2,
		// 	_4_Samples = 4,
		// 	_8_Samples = 8
		// }
		// [SerializeField] public AntiAliasing antiAliasing;// 1, 2, 4, 8. 4type.
		
		// [SerializeField] public UnityEngine.RenderTextureFormat colorFormat;

		public enum DepthBuffer : int {
			NoDepthBuffer = 0,
			_16bitDepth = 16,
			_24bitDepth = 24
		}
		// [SerializeField] public DepthBuffer depthBuffer;// 0, 16, 24. 3type.

		[SerializeField] public UnityEngine.TextureWrapMode wrapMode;

		[SerializeField] public UnityEngine.FilterMode filterMode;

		[SerializeField] public int anisoLevel;// limit to 16.



		public RenderTextureModifier () {
			wrapMode = TextureWrapMode.Clamp;
			filterMode = FilterMode.Bilinear;
			anisoLevel = 0;
		}

		public bool IsModified (object asset) {
			var renderTex = asset as RenderTexture;

			var changed = false;

			if (renderTex.wrapMode != this.wrapMode) changed = true; 
			if (renderTex.filterMode != this.filterMode) changed = true; 
			if (renderTex.anisoLevel != this.anisoLevel) changed = true;

			return changed; 
		}

		public void Modify (object asset) {
			var renderTex = asset as RenderTexture;

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
			EditorGUILayout.HelpBox("Aniso Level can be set if target Asset(RenderTexture)'s Depth Buffer setting is set to 'No depth buffer'. ", MessageType.Info);
		}

		public string Serialize() {
			return JsonUtility.ToJson(this);
		}
	}
}