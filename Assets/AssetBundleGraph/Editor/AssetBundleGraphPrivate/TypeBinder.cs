using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

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
			typeof(Scene).ToString(),
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
			{".unity", typeof(Scene)},

			// {"", typeof(Sprite)},
		};

		public static List<string> IgnoreExtension = new List<string>{
			"",
			".manifest",
			".assetbundle",
			".sample",
			".cs",
			".sh",
			".json",
			".js",
		};

		public static Type AssumeTypeOfAsset (string assetPath) {
			// check by asset importer type.
			var importer = AssetImporter.GetAtPath(assetPath);
			if (importer == null) {
				Debug.LogError("failed to assume the assetType of assetPath:" + assetPath + " 's importer is null, this asset is ignored from Unity.");
				return typeof(object);
			}

			var assumedImporterType = importer.GetType();
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
			if (AssumeTypeBindingByExtension.ContainsKey(extension)) {
				return AssumeTypeBindingByExtension[extension];
			}

			if (IgnoreExtension.Contains(extension)) {
				return null;
			}
			
			// unhandled.
			Debug.LogWarning("Unknown file type found:" + extension + "\n. Asset:" + assetPath + "\n Assume 'object'.");
			return typeof(object);
		}
	}
}
