using System;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.ModifierOperators {
	
	[Serializable] public class CubemapOperator : ModifierBase {
		
		[SerializeField] public int anisoLevel;// limit to 16.
		[SerializeField] public UnityEngine.FilterMode filterMode;
		[SerializeField] public float mipMapBias;
		[SerializeField] public UnityEngine.TextureWrapMode wrapMode;

		public CubemapOperator () {}

		private CubemapOperator (
			string operatorType,
			int anisoLevel,
			FilterMode filterMode,
			float mipMapBias,
			TextureWrapMode wrapMode
		) {
			this.operatorType = operatorType;
			this.anisoLevel = anisoLevel;
			this.filterMode = filterMode;
			this.mipMapBias = mipMapBias;
			this.wrapMode = wrapMode;
		}

		/*
			constructor for default data setting.
		*/
		public override ModifierBase DefaultSetting () {
			return new CubemapOperator(
				"UnityEngine.Cubemap",
				1,
				FilterMode.Bilinear,
				0,
				TextureWrapMode.Clamp
			);
		}

		public override bool IsChanged<T> (T asset) {
			var cubemap = asset as Cubemap;

			var changed = false;

			// cubemap.dimension //<- readonly
			// cubemap.height //<- readonly
			// cubemap.width //<- readonly

			if (cubemap.anisoLevel != this.anisoLevel) changed = true; 
			if (cubemap.filterMode != this.filterMode) changed = true;
			if (cubemap.mipMapBias != this.mipMapBias) changed = true;
			if (cubemap.wrapMode != this.wrapMode) changed = true;

			return changed; 
		}

		public override void Modify<T> (T asset) {
			var cubemap = asset as Cubemap;

			// cubemap.dimension //<- readonly
			// cubemap.height //<- readonly
			// cubemap.width //<- readonly
			
			cubemap.anisoLevel = this.anisoLevel;
			cubemap.filterMode = this.filterMode;
			cubemap.mipMapBias = this.mipMapBias;
			cubemap.wrapMode = this.wrapMode;
		}

		public override void DrawInspector (Action changed) {
			// anisoLevel
			using (new GUILayout.HorizontalScope()) {
				GUILayout.Label("Aniso Level");
				
				var changedVal = (int)EditorGUILayout.Slider(this.anisoLevel, 0, 16);// actually, max is not defined.
				if (changedVal != this.anisoLevel) {
					this.anisoLevel = changedVal;
					changed();
				}
			}

			// filterMode
			var newFilterMode = (UnityEngine.FilterMode)EditorGUILayout.Popup("Filter Mode", (int)this.filterMode, Enum.GetNames(typeof(UnityEngine.FilterMode)), new GUILayoutOption[0]);
			if (newFilterMode != this.filterMode) {
				this.filterMode = newFilterMode;
				changed();
			}

			// mipMapBias
			var newMipMapBias = EditorGUILayout.TextField("MipMap Bias", this.mipMapBias.ToString());
			if (newMipMapBias != this.mipMapBias.ToString()) {
				this.mipMapBias = float.Parse(newMipMapBias, CultureInfo.InvariantCulture.NumberFormat);
				changed();
			}

			// wrapMode
			var newWrapMode = (UnityEngine.TextureWrapMode)EditorGUILayout.Popup("Wrap Mode", (int)this.wrapMode, Enum.GetNames(typeof(UnityEngine.TextureWrapMode)), new GUILayoutOption[0]);
			if (newWrapMode != this.wrapMode) {
				this.wrapMode = newWrapMode;
				changed();
			}
		}
	}

}