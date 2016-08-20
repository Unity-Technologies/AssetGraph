using System;
using UnityEngine;

namespace AssetBundleGraph.ModifierOperators {
	/*
		paramter definitions for RenderTexture.

		定数定義とかについて考えてあること：
		・パラメータ名はInspector準拠
			例えばRenderTextureの場合、Inspector上でColor Formatって書いてあるパラメータは、実際のAssetではformatって名前になってます。
			どっちに合わせようか悩んだんですが、Inspectorに合わせるようにしました。
			IsChangedやModifyメソッド内ではその辺を加味して突き合せる根性が必要です。

		・閾値のあるパラメータについて
			
	*/
	[Serializable] public class RenderTextureOperator : OperatorBase {
		[SerializeField] public Int32 width, height;

		public enum AntiAliasing : int {
			None = 1,
			_2 = 2,
			_4 = 4,
			_8 = 8
		}
		[SerializeField] public AntiAliasing antiAliasing;// 1, 2, 4, 8. 4type.
		
		[SerializeField] public UnityEngine.RenderTextureFormat colorFormat;

		public enum DepthBuffer : int {
			NoDepthBuffer = 0,
			_16bitDepth = 16,
			_24bitDepth = 24
		}
		[SerializeField] public DepthBuffer depthBuffer;// 0, 16, 24. 3type.

		[SerializeField] public UnityEngine.TextureWrapMode wrapMode;

		[SerializeField] public UnityEngine.FilterMode filterMode;

		[SerializeField] public int anisoLevel;// limit to 16.



		public RenderTextureOperator () {}

		private RenderTextureOperator (
			Int32 width, Int32 height,
			AntiAliasing antiAliasing,
			UnityEngine.RenderTextureFormat colorFormat,
			DepthBuffer depthBuffer,
			UnityEngine.TextureWrapMode wrapMode,
			UnityEngine.FilterMode filterMode,
			Int32 anisoLevel
		) {
			this.dataType = "UnityEngine.RenderTexture";
			this.width = width;
			this.height = height;
			this.antiAliasing = antiAliasing;
			this.colorFormat = colorFormat;
			this.depthBuffer = depthBuffer;
			this.wrapMode = wrapMode;
			this.filterMode = filterMode;
			this.anisoLevel = anisoLevel;
		}

		/*
			constructor for default data setting.
		*/
		public override OperatorBase DefaultSetting () {
			return new RenderTextureOperator(
				256, 256,
				AntiAliasing.None,
				UnityEngine.RenderTextureFormat.ARGB32,
				DepthBuffer._16bitDepth,
				UnityEngine.TextureWrapMode.Clamp,
				UnityEngine.FilterMode.Bilinear,
				0
			);
		}

		public override bool IsChanged<T> (T asset) {
			var t = asset as RenderTexture;

			// Inspector上の項目名 / 値(パラメータ名すらすれ違うがまあ、、)
			// Debug.LogError("t.width:" + t.width.GetType());
			// Debug.LogError("t.height:" + t.height);
			// Debug.LogError("t.antiAliasing:" + t.antiAliasing);// 1 = None, 2 = 2samples, 4 = 4samples, 8 = 8samples. GUIマッピングが露骨な例。
			// Debug.LogError("t.ColorFormat:" + t.format.GetType());//
			// Debug.LogError("t.DepthBuffer:" + t.depth);// 0, 16, 24,
			// Debug.LogError("t.WrapMode:" + t.wrapMode);// 
			// Debug.LogError("t.FilterMode:" + t.filterMode);// なんか定数が返ってくる。UnityEngine.FilterMode。

			var changed = false;
			if (t.width != this.width) changed = true;
			if (t.height != this.height) changed = true;
			if (t.antiAliasing != (int)this.antiAliasing) changed = true; 
			if (t.format != this.colorFormat) changed = true; 
			if (t.depth != (int)this.depthBuffer) changed = true; 
			if (t.wrapMode != this.wrapMode) changed = true; 
			if (t.depth != (int)this.depthBuffer) changed = true; 
			if (t.filterMode != this.filterMode) changed = true; 
			if (t.anisoLevel != this.anisoLevel) changed = true;
			return changed; 
		}

		public override void Modify<T> (T asset) {
			var t = asset as RenderTexture;

			

			t.anisoLevel = this.anisoLevel;
		}
	}

}