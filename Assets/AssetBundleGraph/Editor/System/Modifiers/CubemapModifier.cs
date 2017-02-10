using System;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

using UnityEngine.AssetBundles.GraphTool;
using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool.Modifiers {

	/*
	 * Code template for Cubemap modifier.
	 * You can copy and create your CustomModifier.
	 */ 
	[Serializable] 
	[CustomModifier("Default Modifier(Cubemap)", typeof(Cubemap))]
	public class CubemapModifier : IModifier {
		
		[SerializeField] public int anisoLevel;// limit to 16.
		[SerializeField] public UnityEngine.FilterMode filterMode;
		[SerializeField] public float mipMapBias;
		[SerializeField] public UnityEngine.TextureWrapMode wrapMode;

		public CubemapModifier () {
			anisoLevel = 1;
			filterMode = FilterMode.Bilinear;
			mipMapBias = 0;
			wrapMode = TextureWrapMode.Clamp;
		}

		public bool IsModified (UnityEngine.Object[] assets) {
			var cubemap = assets[0] as Cubemap;

			var changed = false;

			if (cubemap.anisoLevel != this.anisoLevel) changed = true; 
			if (cubemap.filterMode != this.filterMode) changed = true;
			if (cubemap.mipMapBias != this.mipMapBias) changed = true;
			if (cubemap.wrapMode != this.wrapMode) changed = true;

			return changed; 
		}

		public void Modify (UnityEngine.Object[] assets) {
			var cubemap = assets[0] as Cubemap;

			cubemap.anisoLevel = this.anisoLevel;
			cubemap.filterMode = this.filterMode;
			cubemap.mipMapBias = this.mipMapBias;
			cubemap.wrapMode = this.wrapMode;
		}

		public void OnInspectorGUI (Action onValueChanged) {
			// anisoLevel
			using (new GUILayout.HorizontalScope()) {
				GUILayout.Label("Aniso Level");

				var changedVal = (int)EditorGUILayout.Slider(this.anisoLevel, 0, 16);// actually, max is not defined.
				if (changedVal != this.anisoLevel) {
					this.anisoLevel = changedVal;
					onValueChanged();
				}
			}

			// filterMode
			var newFilterMode = (UnityEngine.FilterMode)EditorGUILayout.Popup("Filter Mode", (int)this.filterMode, Enum.GetNames(typeof(UnityEngine.FilterMode)), new GUILayoutOption[0]);
			if (newFilterMode != this.filterMode) {
				this.filterMode = newFilterMode;
				onValueChanged();
			}

			// mipMapBias
			var newMipMapBias = EditorGUILayout.TextField("MipMap Bias", this.mipMapBias.ToString());
			if (newMipMapBias != this.mipMapBias.ToString()) {
				this.mipMapBias = float.Parse(newMipMapBias, CultureInfo.InvariantCulture.NumberFormat);
				onValueChanged();
			}

			// wrapMode
			var newWrapMode = (UnityEngine.TextureWrapMode)EditorGUILayout.Popup("Wrap Mode", (int)this.wrapMode, Enum.GetNames(typeof(UnityEngine.TextureWrapMode)), new GUILayoutOption[0]);
			if (newWrapMode != this.wrapMode) {
				this.wrapMode = newWrapMode;
				onValueChanged();
			}
		}
	}

}