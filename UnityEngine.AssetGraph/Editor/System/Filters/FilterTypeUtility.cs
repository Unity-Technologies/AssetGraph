using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEditor.Animations;

using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_2017_1_OR_NEWER
using UnityEngine.U2D;
using UnityEngine.Playables;
using UnityEngine.Timeline;
#endif
#if UNITY_2017_2_OR_NEWER
using UnityEngine.Tilemaps;
#endif

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	public static class FilterTypeUtility {
        
//        private static readonly Dictionary<string, Type> FilterTypeBindingByExtension = new Dictionary<string, Type>{
//			// others(Assets)
//            {".png", typeof(TextureImporter)},
//            {".jpg", typeof(TextureImporter)},
//            {".tga", typeof(TextureImporter)},
//            {".exr", typeof(TextureImporter)},
//            {".anim", typeof(Animation)},
//			{".controller", typeof(Animator)},
//			{".mask", typeof(AvatarMask)},
//			{".cubemap", typeof(Cubemap)},
//			{".flare", typeof(Flare)},
//            {".fontsettings", typeof(Font)},
//            {".ttf", typeof(Font)},
//            {".otf", typeof(Font)},
//            {".compute", typeof(ComputeShader)},
//			{".guiskin", typeof(GUISkin)},
//			// typeof(LightmapParameters).ToString(),
//			{".mat", typeof(Material)},
//			{".physicmaterial", typeof(PhysicMaterial)},
//			{".physicsmaterial2d", typeof(PhysicsMaterial2D)},
//			{".rendertexture", typeof(RenderTexture)},
//            {".shader", typeof(Shader)},
//            {".cg", typeof(Shader)},
//            {".cginc", typeof(Shader)},
//            {".mixer", typeof(Audio.AudioMixer)},
//			{".unity", typeof(Scene)},
//			{".txt", typeof(TextAsset)},
//			{".html", typeof(TextAsset)},
//			{".htm", typeof(TextAsset)},
//			{".xml", typeof(TextAsset)},
//			{".bytes", typeof(TextAsset)},
//			{".json", typeof(TextAsset)},
//			{".csv", typeof(TextAsset)},
//			{".yaml", typeof(TextAsset)},
//			{".fnt", typeof(TextAsset)},
//			{".asset", typeof(Object)},
//            {".prefab", typeof(UnityEngine.GameObject)},
//            #if UNITY_2017_1_OR_NEWER
//            {".playable", typeof(PlayableAsset)},
//            {".spriteatlas", typeof(SpriteAtlas)},
//            #endif
//
//			// {"", typeof(Sprite)},
//		};

        private static readonly List<string> IgnoredAssetTypeExtension = new List<string>{
			string.Empty,
			".manifest",
			".assetbundle",
			".sample",
			".unitypackage",
			".cs",
			".sh",
            ".js",
            ".boo",
			".zip",
			".tar",
			".tgz",
			#if UNITY_5_6 || UNITY_5_6_OR_NEWER
			#else
			".m4v",
			#endif
		};

        private static readonly Dictionary<string, Type> s_defaultAssetFilterGUITypeMap = new Dictionary<string, Type> {

            {"Text or Binary", typeof(TextAsset)},
            {"Scene", typeof(UnityEditor.SceneAsset)},
            {"Prefab", typeof(GameObject)},
            {"Animation", typeof(AnimationClip)},
            {"Animator Controller", typeof(AnimatorController)},
            {"Animator Override Controller", typeof(AnimatorOverrideController)},
            {"Avatar Mask", typeof(AvatarMask)},
            {"Custom Font", typeof(Font)},
            {"Physic Material", typeof(PhysicMaterial)},
            {"Physic Material 2D", typeof(PhysicsMaterial2D)},
            {"Shader", typeof(Shader)},
            {"Material", typeof(Material)},
            {"Render Texture", typeof(RenderTexture)},
            {"Custom Render Texture", typeof(CustomRenderTexture)},
            {"Lightmap Parameter", typeof(LightmapParameters)},
            {"Lens Flare", typeof(Flare)},
//            {"Audio Mixer", typeof(AudioMixer)}, //AudioMixerController
            #if UNITY_2017_1_OR_NEWER
            {"Timeline", typeof(TimelineAsset)},
            #endif
            #if UNITY_2017_1_OR_NEWER
            {"Sprite Atlas", typeof(UnityEngine.U2D.SpriteAtlas)},
            #endif
            #if UNITY_2017_2_OR_NEWER
            {"Tilemap", typeof(UnityEngine.Tilemaps.Tile)},
            #endif
            {"GUI Skin", typeof(GUISkin)},
            {"Legacy/Cubemap", typeof(Cubemap)},
        };

        private static List<string> s_filterKeyTypeList;

        public static List<string> GetFilterGUINames() {
            if (s_filterKeyTypeList == null) {
                var typemap = ImporterConfiguratorUtility.GetImporterConfiguratorGuiNameTypeMap ();

                var keyList = new List<string> ();

                keyList.Add (Model.Settings.DEFAULT_FILTER_KEYTYPE);
                keyList.AddRange (typemap.Keys);
                keyList.AddRange (s_defaultAssetFilterGUITypeMap.Keys);

                s_filterKeyTypeList = keyList;
            }
            return s_filterKeyTypeList;
        }

        public static Type FindFilterTypeFromGUIName(string guiName) {
            if (guiName == Model.Settings.DEFAULT_FILTER_KEYTYPE) {
                return null; // or UnityEngine.Object ?
            }

            var typemap = ImporterConfiguratorUtility.GetImporterConfiguratorGuiNameTypeMap ();
            if (typemap.ContainsKey (guiName)) {
                return typemap [guiName];
            }
            if (s_defaultAssetFilterGUITypeMap.ContainsKey (guiName)) {
                return s_defaultAssetFilterGUITypeMap [guiName];
            }

            return null;
        }

        public static string FindGUINameFromType(Type t) {

            Assertions.Assert.IsNotNull (t);

            var typemap = ImporterConfiguratorUtility.GetImporterConfiguratorGuiNameTypeMap ();

            var elements = typemap.Where (v => v.Value == t);
            if (elements.Any ()) {
                return elements.First ().Key;
            }

            var elements2 = s_defaultAssetFilterGUITypeMap.Where (v => v.Value == t);
            if (elements2.Any ()) {
                return elements2.First ().Key;
            }

            return Model.Settings.DEFAULT_FILTER_KEYTYPE;
        }

		public static Type FindAssetFilterType (string assetPath) {
            var importerType = TypeUtility.GetAssetImporterTypeAtPath(assetPath);
            if (importerType != null) {
                return importerType;
            }

			// not specific type importer. should determine their type by extension.
			var extension = Path.GetExtension(assetPath).ToLower();

            if (IgnoredAssetTypeExtension.Contains(extension)) {
                return null;
            }

            return TypeUtility.GetMainAssetTypeAtPath (assetPath);

//			if (FilterTypeBindingByExtension.ContainsKey(extension)) {
//				return FilterTypeBindingByExtension[extension];
//			}
			
			// unhandled.
//			LogUtility.Logger.LogWarning(LogUtility.kTag, "Unknown file type found:" + extension + "\n. AssetReference:" + assetPath + "\n Assume 'object'.");
//			return typeof(object);
		}			
	}
}
