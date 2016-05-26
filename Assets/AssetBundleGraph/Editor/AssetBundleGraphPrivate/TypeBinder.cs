using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph {
	public static class TypeBinder {
		public static List<string> KeyTypes = new List<string>{
			// empty
			AssetBundleGraphSettings.DEFAULT_FILTER_KEYTYPE,
			
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
		
		public static Dictionary<string, Type> AssumeTypeBindingByExtension = new Dictionary<string, Type>{
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
		
		public static Type AssumeTypeOfAsset (string assetPath) {
			// check by asset importer type.
			var assumedImporterType = AssetImporter.GetAtPath(assetPath).GetType();
			var importerTypeStr = assumedImporterType.ToString();
			
			switch (importerTypeStr) {
				case "UnityEditor.TextureImporter":
				case "UnityEditor.ModelImporter":
				case "UnityEditor.AudioImporter": {
					return assumedImporterType;
				}
			}
			
			// not specific type importer. should determine their type by extension.
			var extension = Path.GetExtension(assetPath);
			if (AssumeTypeBindingByExtension.ContainsKey(extension)) return AssumeTypeBindingByExtension[extension];
			
			
			// unhandled..
			Debug.LogError("failed to detect asset extension:" + extension);
			return typeof(UnityEngine.Object);
		}
	}
}
