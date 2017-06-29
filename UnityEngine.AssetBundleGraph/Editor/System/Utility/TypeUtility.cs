using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {
	public static class TypeUtility {
		public static readonly List<string> KeyTypes = new List<string>{
			// empty
			Model.Settings.DEFAULT_FILTER_KEYTYPE,
			
			// importers
			typeof(TextureImporter).ToString(),
			typeof(ModelImporter).ToString(),
			typeof(AudioImporter).ToString(),
			#if UNITY_5_6 || UNITY_5_6_OR_NEWER
			typeof(VideoClipImporter).ToString(),
			#endif
			
			// others(Assets)
			typeof(TextAsset).ToString(),
			typeof(Animation).ToString(),
			typeof(Animator).ToString(),
			typeof(AvatarMask).ToString(),
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
			typeof(Scene).ToString(),
            typeof(GameObject).ToString(),
            typeof(Audio.AudioMixer).ToString(),
		};
		
        private static readonly Dictionary<string, Type> FilterTypeBindingByExtension = new Dictionary<string, Type>{
			// others(Assets)
            {".png", typeof(TextureImporter)},
            {".jpg", typeof(TextureImporter)},
            {".exr", typeof(TextureImporter)},
            {".anim", typeof(Animation)},
			{".controller", typeof(Animator)},
			{".mask", typeof(AvatarMask)},
			{".cubemap", typeof(Cubemap)},
			{".flare", typeof(Flare)},
            {".fontsettings", typeof(Font)},
            {".ttf", typeof(Font)},
            {".otf", typeof(Font)},
            {".compute", typeof(ComputeShader)},
			{".guiskin", typeof(GUISkin)},
			// typeof(LightmapParameters).ToString(),
			{".mat", typeof(Material)},
			{".physicmaterial", typeof(PhysicMaterial)},
			{".physicsmaterial2d", typeof(PhysicsMaterial2D)},
			{".rendertexture", typeof(RenderTexture)},
			// typeof(SceneAsset).ToString(),
            {".shader", typeof(Shader)},
            {".cg", typeof(Shader)},
            {".cginc", typeof(Shader)},
            {".mixer", typeof(Audio.AudioMixer)},
			{".unity", typeof(Scene)},
			{".txt", typeof(TextAsset)},
			{".html", typeof(TextAsset)},
			{".htm", typeof(TextAsset)},
			{".xml", typeof(TextAsset)},
			{".bytes", typeof(TextAsset)},
			{".json", typeof(TextAsset)},
			{".csv", typeof(TextAsset)},
			{".yaml", typeof(TextAsset)},
			{".fnt", typeof(TextAsset)},
			{".asset", typeof(Object)},
			{".prefab", typeof(UnityEngine.GameObject)}

			// {"", typeof(Sprite)},
		};

        private static readonly List<string> IgnoredAssetTypeExtension = new List<string>{
			string.Empty,
			".manifest",
			".assetbundle",
			".sample",
			".unitypackage",
			".cs",
			".sh",
			".js",
			".zip",
			".tar",
			".tgz",
			#if UNITY_5_6 || UNITY_5_6_OR_NEWER
			#else
			".m4v",
			#endif
		};

		private static readonly List<Type> IgnoreTypes = new List<Type> {
			typeof(MonoScript),
			typeof(AssetBundleReference),
            typeof(Model.ConfigGraph)
		};

        private static readonly List<Type> GraphToolAssetType = new List<Type> {
            typeof(AssetBundleReference),
            typeof(Model.ConfigGraph),
            typeof(Model.ConnectionData),
            typeof(Model.ConnectionPointData),
            typeof(Model.NodeData),
            typeof(AssetReferenceDatabase),
            typeof(AssetBundleBuildMap)
        };

        public static bool IsGraphToolSystemAssetType(Type t) {
            if (t == null) {
                return  false;
            }
            return GraphToolAssetType.Contains (t);
        }

        public static bool IsGraphToolSystemAsset(string assetPath) {
            return 
                assetPath.Contains (Model.Settings.Path.BasePath) || 
                IsGraphToolSystemAssetType (GetTypeOfAsset(assetPath));
        }

		public static bool IsLoadingAsset (AssetReference r) {
			Type t = r.assetType;
			return t != null && !IgnoreTypes.Contains(t);
		}

		/**
		 * Get type of asset from give path.
		 */
		public static Type GetTypeOfAsset (string assetPath) {
			if (assetPath.EndsWith(Model.Settings.UNITY_METAFILE_EXTENSION)) {
				return typeof(string);
			}

			Type t = null;
			#if (UNITY_5_4_OR_NEWER && !UNITY_5_4_0 && !UNITY_5_4_1)

			t = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            if(t == typeof(MonoBehaviour)) {
                UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                t = asset.GetType();
                //Resources.UnloadAsset(asset);
            }

			#else

			UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);

			if (asset != null) {
				t = asset.GetType();
				if(asset is UnityEngine.GameObject || asset is UnityEngine.Component) {
					// do nothing.
					// NOTE: DestroyImmediate() will destroy persistant GameObject in prefab. Do not call it.
				} else {
					Resources.UnloadAsset(asset);
				}
			}
			#endif

			return t;
		}

		/**
		 * Get asset filter type from asset path.
		 */
		public static Type FindAssetFilterType (string assetPath) {
			// check by asset importer type.
//			if (importer == null) {
//				LogUtility.Logger.LogWarning(LogUtility.kTag, "Failed to assume assetType of asset. The asset will be ignored: " + assetPath);
//				return typeof(object);
//			}

            var importer = AssetImporter.GetAtPath(assetPath);
            if (importer != null) {
                var importerType = importer.GetType();
                var importerTypeStr = importerType.ToString();

                switch (importerTypeStr) {
                case "UnityEditor.TextureImporter":
                case "UnityEditor.ModelImporter":
                case "UnityEditor.AudioImporter": 
                    #if UNITY_5_6 || UNITY_5_6_OR_NEWER
                case "UnityEditor.VideoClipImporter": 
                    #endif
                    {
                        return importerType;
                    }
                }
            }
			
			// not specific type importer. should determine their type by extension.
			var extension = Path.GetExtension(assetPath).ToLower();
			if (FilterTypeBindingByExtension.ContainsKey(extension)) {
				return FilterTypeBindingByExtension[extension];
			}

			if (IgnoredAssetTypeExtension.Contains(extension)) {
				return null;
			}
			
			// unhandled.
			LogUtility.Logger.LogWarning(LogUtility.kTag, "Unknown file type found:" + extension + "\n. AssetReference:" + assetPath + "\n Assume 'object'.");
			return typeof(object);
		}			

		public static Type FindFirstIncomingAssetType(List<AssetReference> assets) {

			if(assets.Any()) {
				return assets.First().filterType;
			}

			return null;
		}

		public static Type FindFirstIncomingAssetType(AssetReferenceStreamManager mgr, Model.ConnectionPointData inputPoint) {
			var assetGroupEnum = mgr.EnumurateIncomingAssetGroups(inputPoint);
			if(assetGroupEnum == null) {
				return null;
			}

			if(assetGroupEnum.Any()) {
				var ag = assetGroupEnum.First();
				if(ag.Values.Any()) {
					var assets = ag.Values.First();
					if(assets.Count > 0) {
						return assets[0].filterType;
					}
				}
			}

			return null;
		}

		public static AssetReference GetFirstIncomingAsset(IEnumerable<PerformGraph.AssetGroups> incoming) {

			if( incoming == null ) {
				return null;
			}

			foreach(var ag in incoming) {
				foreach(var v in ag.assetGroups.Values) {
					if(v.Count > 0) {
						return v[0];
					}
				}
			}

			return null;
		}

		public static Type FindFirstIncomingAssetType(IEnumerable<PerformGraph.AssetGroups> incoming) {
			AssetReference r = GetFirstIncomingAsset(incoming);
			if(r != null) {
				return r.filterType;
			}
			return null;
		}


		public static MonoScript LoadMonoScript(string className) {
			if(className == null) {
				return null;
			}

			var t = Type.GetType(className);
			if(t == null) {
				return null;
			}

			string[] guids = AssetDatabase.FindAssets ("t:MonoScript " + className);

			MonoScript s = null;

			if(guids.Length > 0 ) {
				var path = AssetDatabase.GUIDToAssetPath(guids[0]);
				s = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
			}

			return s;
		}
	}

	public class AssetBundleReference {}
	public class AssetBundleManifestReference {}
}
