using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetGraph {
	public static class TypeBinder {
		public static List<string> KeyTypes = new List<string>{
			// empty
			AssetGraphSettings.DEFAULT_FILTER_KEYTYPE,
			
			// importers
			typeof(TextureImporter).ToString(),
			typeof(ModelImporter).ToString(),
			typeof(AudioImporter).ToString(),
			
			// others(Assets)
			typeof(Animation).ToString(),
			typeof(Animator).ToString(),
			typeof(Avatar).ToString(),
			typeof(Cubemap).ToString(),
			typeof(Flare).ToString(),
			typeof(Font).ToString(),
			typeof(GUISkin).ToString(),
			// typeof(LightmapParameters).ToString(),
			typeof(Material).ToString(),
			typeof(PhysicMaterial).ToString(),
			typeof(PhysicsMaterial2D).ToString(),
			typeof(RenderTexture).ToString(),
			// typeof(SceneAsset).ToString(),
			typeof(Shader).ToString(),
			typeof(Sprite).ToString(),
		};
		
		public static Dictionary<string, Type> AssumeTypeBinding = new Dictionary<string, Type>{
			// importers
			{".png", typeof(TextureImporter)},// もっといっぱいあるよね -> このへんはimporterで見る
			{".fbx", typeof(ModelImporter)},// もっといっぱいあるよね
			{".mp3", typeof(AudioImporter)},// もっといっぱいあるよね
			
			// others(Assets)
			{".anim", typeof(Animation)},
			{".controller", typeof(Animator)},
			{".mask", typeof(Avatar)},
			{".cubemap", typeof(Cubemap)},
			{".flare", typeof(Flare)},
			{".fontsettings", typeof(Font)},
			{".guiskin", typeof(GUISkin)},
			// typeof(LightmapParameters).ToString(),
			{".mat", typeof(Material)},
			{".physicMaterial", typeof(PhysicMaterial)},
			{".physicsMaterial2D", typeof(PhysicsMaterial2D)},
			{".renderTexture", typeof(RenderTexture)},
			// typeof(SceneAsset).ToString(),
			{".shader", typeof(Shader)},
			// {"", typeof(Sprite)},
		};
		
		public static Type AssumeTypeFromExtension (string filePath) {
			var extension = Path.GetExtension(filePath);
			if (AssumeTypeBinding.ContainsKey(extension)) return AssumeTypeBinding[extension];
			return typeof(UnityEngine.Object);
		}
	}
}
