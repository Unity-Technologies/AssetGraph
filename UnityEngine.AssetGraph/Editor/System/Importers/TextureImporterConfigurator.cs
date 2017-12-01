using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;

using Model = UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph
{
    [Serializable] 
    [CustomAssetImporterConfigurator(typeof(TextureImporter), "Texture", "setting.png")]
    public class TextureImportSettingsConfigurator : IAssetImporterConfigurator
    {
        [SerializeField] private bool m_overwritePackingTag;
        [SerializeField] private bool m_overwriteSpriteSheet;
        [SerializeField] private SerializableMultiTargetString m_customPackingTagTemplate;

        public void Initialize (ConfigurationOption option)
        {
            m_overwritePackingTag  = option.overwritePackingTag;
            m_overwriteSpriteSheet = option.overwriteSpriteSheet;
            m_customPackingTagTemplate = option.customPackingTagTemplate;
        }

        public bool IsModified (AssetImporter referenceImporter, AssetImporter importer, BuildTarget target, string group)
        {
            var r = referenceImporter as TextureImporter;
            var t = importer as TextureImporter;
            if (t == null || r == null) {
                throw new AssetGraphException (string.Format ("Invalid AssetImporter assigned for {0}", importer.assetPath));
            }
            return !IsEqual (t, r, GetTagName(target, group));
        }

        public void Configure (AssetImporter referenceImporter, AssetImporter importer, BuildTarget target, string group)
        {
            var r = referenceImporter as TextureImporter;
            var t = importer as TextureImporter;
            if (t == null || r == null) {
                throw new AssetGraphException (string.Format ("Invalid AssetImporter assigned for {0}", importer.assetPath));
            }
            OverwriteImportSettings (t, r, GetTagName(target, group));
        }

        public void OnInspectorGUI (AssetImporter referenceImporter, BuildTargetGroup target, Action onValueChanged)
        {
            var importer = referenceImporter as TextureImporter;
            if (importer == null) {
                return;
            }

            if (importer.textureType == TextureImporterType.Sprite) {
                using (new EditorGUILayout.VerticalScope (GUI.skin.box)) {
                    GUILayout.Label ("Sprite Settings");
                    GUILayout.Space (4f);
                    var bSpriteSheet = EditorGUILayout.ToggleLeft ("Configure Sprite Mode", m_overwriteSpriteSheet);
                    var bPackingTag  = EditorGUILayout.ToggleLeft ("Configure Sprite Packing Tag", m_overwritePackingTag);

                    if (bSpriteSheet != m_overwriteSpriteSheet ||
                       bPackingTag != m_overwritePackingTag) {

                        m_overwriteSpriteSheet = bSpriteSheet;
                        m_overwritePackingTag = bPackingTag;
                        onValueChanged ();
                    }

                    if (m_overwritePackingTag) {
                        if (m_customPackingTagTemplate == null) {
                            m_customPackingTagTemplate = new SerializableMultiTargetString ();
                        }

                        var val = m_customPackingTagTemplate.DefaultValue;

                        var newValue = EditorGUILayout.TextField ("Packing Tag", val);
                        if (newValue != val) {
                            m_customPackingTagTemplate.DefaultValue = newValue;
                            onValueChanged ();
                        }
                    }
                    EditorGUILayout.HelpBox (
                        "You can configure packing tag name with \"*\" to include group name in your sprite tag.", 
                        MessageType.Info);
                }
            }
        }

        private string GetTagName(BuildTarget target, string groupName) {
            return m_customPackingTagTemplate[target].Replace("*", groupName);
        }

        private void ApplySpriteTag(BuildTarget target, IEnumerable<PerformGraph.AssetGroups> incoming) {

            foreach(var ag in incoming) {
                foreach(var groupKey in ag.assetGroups.Keys) {
                    var assets = ag.assetGroups[groupKey];
                    foreach(var asset in assets) {

                        if(asset.importerType == typeof(UnityEditor.TextureImporter) ) {
                            var importer = AssetImporter.GetAtPath(asset.importFrom) as TextureImporter;

                            importer.spritePackingTag = GetTagName(target, groupKey);
                            importer.SaveAndReimport();
                            asset.TouchImportAsset();
                        }
                    }
                }
            }
        }



        private bool IsEqual (TextureImporter target, TextureImporter reference, string tagName)
        {
            // UnityEditor.TextureImporter.textureFormat' is obsolete: 
            // `textureFormat is not longer accessible at the TextureImporter level
            if (target.textureType != reference.textureType)
                return false;

            TextureImporterSettings targetSetting = new TextureImporterSettings ();
            TextureImporterSettings referenceSetting = new TextureImporterSettings ();

            target.ReadTextureSettings (targetSetting);
            reference.ReadTextureSettings (referenceSetting);

            // if m_overwriteSpriteSheet is false, following properties
            // should be ignored
            if (!m_overwriteSpriteSheet) {
                referenceSetting.spriteAlignment = targetSetting.spriteAlignment;
                referenceSetting.spriteBorder = targetSetting.spriteBorder;
                referenceSetting.spriteExtrude = targetSetting.spriteExtrude;
                referenceSetting.spriteMode = targetSetting.spriteMode;
                referenceSetting.spriteMeshType = targetSetting.spriteMeshType;
                referenceSetting.spritePivot = targetSetting.spritePivot;
                referenceSetting.spritePixelsPerUnit = targetSetting.spritePixelsPerUnit;
                referenceSetting.spriteTessellationDetail = targetSetting.spriteTessellationDetail;
            }

            if (!TextureImporterSettings.Equal (targetSetting, referenceSetting)) {
                return false;
            }

            if (target.textureType == TextureImporterType.Sprite) {
                if (m_overwritePackingTag) {
                    if (!string.IsNullOrEmpty (tagName)) {
                        if (target.spritePackingTag != tagName)
                            return false;
                    } else {
                        if (target.spritePackingTag != reference.spritePackingTag)
                            return false;
                    }
                }

                if (m_overwriteSpriteSheet) {
                    if (target.spriteBorder != reference.spriteBorder)
                        return false;
                    if (target.spriteImportMode != reference.spriteImportMode)
                        return false;
                    if (target.spritePivot != reference.spritePivot)
                        return false;
                    if (target.spritePixelsPerUnit != reference.spritePixelsPerUnit)
                        return false;

                    var s1 = target.spritesheet;
                    var s2 = reference.spritesheet;

                    if (s1.Length != s2.Length) {
                        return false;
                    }

                    for (int i = 0; i < s1.Length; ++i) {
                        if (s1 [i].alignment != s2 [i].alignment)
                            return false;
                        if (s1 [i].border != s2 [i].border)
                            return false;
                        if (s1 [i].name != s2 [i].name)
                            return false;
                        if (s1 [i].pivot != s2 [i].pivot)
                            return false;
                        if (s1 [i].rect != s2 [i].rect)
                            return false;
                    }
                }
            }

            if (target.wrapMode != reference.wrapMode)
                return false;
            if (target.anisoLevel != reference.anisoLevel)
                return false;
            if (target.borderMipmap != reference.borderMipmap)
                return false;
            if (target.compressionQuality != reference.compressionQuality)
                return false;
            if (target.convertToNormalmap != reference.convertToNormalmap)
                return false;
            if (target.fadeout != reference.fadeout)
                return false;
            if (target.filterMode != reference.filterMode)
                return false;
            if (target.generateCubemap != reference.generateCubemap)
                return false;
            if (target.heightmapScale != reference.heightmapScale)
                return false;
            if (target.isReadable != reference.isReadable)
                return false;
            if (target.maxTextureSize != reference.maxTextureSize)
                return false;
            if (target.mipMapBias != reference.mipMapBias)
                return false;
            if (target.mipmapEnabled != reference.mipmapEnabled)
                return false;
            if (target.mipmapFadeDistanceEnd != reference.mipmapFadeDistanceEnd)
                return false;
            if (target.mipmapFadeDistanceStart != reference.mipmapFadeDistanceStart)
                return false;
            if (target.mipmapFilter != reference.mipmapFilter)
                return false;
            if (target.normalmapFilter != reference.normalmapFilter)
                return false;
            if (target.npotScale != reference.npotScale)
                return false;

            /* read only properties */
            // target.qualifiesForSpritePacking

            #if !UNITY_5_5_OR_NEWER
            // obsolete features
            if (target.normalmap != reference.normalmap) return false;
            if (target.linearTexture != reference.linearTexture) return false;
            if (target.lightmap != reference.lightmap) return false;
            if (target.grayscaleToAlpha != reference.grayscaleToAlpha) return false;
            if (target.generateMipsInLinearSpace != reference.generateMipsInLinearSpace) return false;
            if (target.textureFormat != reference.textureFormat) return false;

            foreach (var g in NodeGUIUtility.SupportedBuildTargetGroups)
            {
            var platformName = BuildTargetUtility.TargetToAssetBundlePlatformName(g, BuildTargetUtility.PlatformNameType.TextureImporter);

            int srcMaxTextureSize;
            TextureImporterFormat srcFormat;
            int srcCompressionQuality;

            int dstMaxTextureSize;
            TextureImporterFormat dstFormat;
            int dstCompressionQuality;

            var srcHasSetting = target.GetPlatformTextureSettings(platformName, out srcMaxTextureSize, out srcFormat, out srcCompressionQuality);
            var dstHasSetting = reference.GetPlatformTextureSettings(platformName, out dstMaxTextureSize, out dstFormat, out dstCompressionQuality);

            if (srcHasSetting != dstHasSetting) return false;
            if (srcMaxTextureSize != dstMaxTextureSize) return false;
            if (srcFormat != dstFormat) return false;
            if (srcCompressionQuality != dstCompressionQuality) return false;
            }
            #else
            if (target.allowAlphaSplitting != reference.allowAlphaSplitting)
                return false;
            if (target.alphaIsTransparency != reference.alphaIsTransparency)
                return false;
            if (target.textureShape != reference.textureShape)
                return false;

            if (target.alphaSource != reference.alphaSource)
                return false;
            if (target.sRGBTexture != reference.sRGBTexture)
                return false;
            if (target.textureCompression != reference.textureCompression)
                return false;
            if (target.crunchedCompression != reference.crunchedCompression)
                return false;

            var refDefault = reference.GetDefaultPlatformTextureSettings ();
            var impDefault = target.GetDefaultPlatformTextureSettings ();
            if (!CompareImporterPlatformSettings (refDefault, impDefault))
                return false;

            foreach (var g in NodeGUIUtility.SupportedBuildTargetGroups) {
                var platformName = BuildTargetUtility.TargetToAssetBundlePlatformName (g, BuildTargetUtility.PlatformNameType.TextureImporter);

                var impSet = reference.GetPlatformTextureSettings (platformName);
                var targetImpSet = target.GetPlatformTextureSettings (platformName);
                if (!CompareImporterPlatformSettings (impSet, targetImpSet))
                    return false;
            }
            #endif

            #if UNITY_2017_1_OR_NEWER
            if (target.alphaTestReferenceValue != reference.alphaTestReferenceValue)
                return false;
            if (target.mipMapsPreserveCoverage != reference.mipMapsPreserveCoverage)
                return false;
            if (target.wrapModeU != reference.wrapModeU)
                return false;
            if (target.wrapModeV != reference.wrapModeV)
                return false;
            if (target.wrapModeW != reference.wrapModeW)
                return false;
            #endif
            return true;
        }

        private void OverwriteImportSettings (TextureImporter target, TextureImporter reference, string tagName)
        {
            target.textureType = reference.textureType;

            TextureImporterSettings dstSettings = new TextureImporterSettings ();
            TextureImporterSettings srcSettings = new TextureImporterSettings ();

            target.ReadTextureSettings (srcSettings);
            reference.ReadTextureSettings (dstSettings);

            if (!m_overwriteSpriteSheet) {
                dstSettings.spriteAlignment = srcSettings.spriteAlignment;
                dstSettings.spriteBorder = srcSettings.spriteBorder;
                dstSettings.spriteExtrude = srcSettings.spriteExtrude;
                dstSettings.spriteMode = srcSettings.spriteMode;
                dstSettings.spriteMeshType = srcSettings.spriteMeshType;
                dstSettings.spritePivot = srcSettings.spritePivot;
                dstSettings.spritePixelsPerUnit = srcSettings.spritePixelsPerUnit;
                dstSettings.spriteTessellationDetail = srcSettings.spriteTessellationDetail;
            }

            target.SetTextureSettings (dstSettings);

            if (m_overwriteSpriteSheet) {
                target.spritesheet = reference.spritesheet;
            }

            // some unity version do not properly copy properties via TextureSettings,
            // so also perform manual copy

            target.anisoLevel = reference.anisoLevel;
            target.borderMipmap = reference.borderMipmap;
            target.compressionQuality = reference.compressionQuality;
            target.convertToNormalmap = reference.convertToNormalmap;
            target.fadeout = reference.fadeout;
            target.filterMode = reference.filterMode;
            target.generateCubemap = reference.generateCubemap;
            target.heightmapScale = reference.heightmapScale;

            target.isReadable = reference.isReadable;
            target.maxTextureSize = reference.maxTextureSize;
            target.mipMapBias = reference.mipMapBias;
            target.mipmapEnabled = reference.mipmapEnabled;
            target.mipmapFadeDistanceEnd = reference.mipmapFadeDistanceEnd;
            target.mipmapFadeDistanceStart = reference.mipmapFadeDistanceStart;
            target.mipmapFilter = reference.mipmapFilter;

            target.normalmapFilter = reference.normalmapFilter;
            target.npotScale = reference.npotScale;

            if (m_overwritePackingTag) {
                if (!string.IsNullOrEmpty (tagName)) {
                    target.spritePackingTag = tagName;
                } else {
                    target.spritePackingTag = reference.spritePackingTag;
                }
            }

            target.wrapMode = reference.wrapMode;

            /* read only */
            // importer.qualifiesForSpritePacking

#if !UNITY_5_5_OR_NEWER
            // obsolete features
            importer.generateMipsInLinearSpace = reference.generateMipsInLinearSpace;
            importer.grayscaleToAlpha = reference.grayscaleToAlpha;
            importer.lightmap = reference.lightmap;
            importer.linearTexture = reference.linearTexture;
            importer.normalmap = reference.normalmap;
            importer.textureFormat = reference.textureFormat;

            foreach (var g in NodeGUIUtility.SupportedBuildTargetGroups)
            {
                var platformName = BuildTargetUtility.TargetToAssetBundlePlatformName(g, BuildTargetUtility.PlatformNameType.TextureImporter);

                int maxTextureSize;
                TextureImporterFormat format;
                int compressionQuality;

                if(reference.GetPlatformTextureSettings(platformName, out maxTextureSize, out format, out compressionQuality)) {
                    importer.SetPlatformTextureSettings(platformName, maxTextureSize, format, compressionQuality, false);
                } else {
                    importer.ClearPlatformTextureSettings(platformName);
                }
            }
#else
            target.allowAlphaSplitting = reference.allowAlphaSplitting;
            target.alphaIsTransparency = reference.alphaIsTransparency;
            target.textureShape = reference.textureShape;

            target.alphaSource = reference.alphaSource;
            target.sRGBTexture = reference.sRGBTexture;
            target.textureCompression = reference.textureCompression;
            target.crunchedCompression = reference.crunchedCompression;

            var defaultPlatformSetting = reference.GetDefaultPlatformTextureSettings ();
            target.SetPlatformTextureSettings (defaultPlatformSetting);

            foreach (var g in NodeGUIUtility.SupportedBuildTargetGroups) {
                var platformName = BuildTargetUtility.TargetToAssetBundlePlatformName (g, BuildTargetUtility.PlatformNameType.TextureImporter);
                var impSet = reference.GetPlatformTextureSettings (platformName);
                target.SetPlatformTextureSettings (impSet);
            }
#endif

#if UNITY_2017_1_OR_NEWER
            target.alphaTestReferenceValue = reference.alphaTestReferenceValue;
            target.mipMapsPreserveCoverage = reference.mipMapsPreserveCoverage;
            target.wrapModeU = reference.wrapModeU;
            target.wrapModeV = reference.wrapModeV;
            target.wrapModeW = reference.wrapModeW;
#endif
        }


        #if UNITY_5_5_OR_NEWER
        bool CompareImporterPlatformSettings (TextureImporterPlatformSettings c1, TextureImporterPlatformSettings c2)
        {
            if (c1.allowsAlphaSplitting != c2.allowsAlphaSplitting)
                return false;
            if (c1.compressionQuality != c2.compressionQuality)
                return false;
            if (c1.crunchedCompression != c2.crunchedCompression)
                return false;
            if (c1.format != c2.format)
                return false;
            if (c1.maxTextureSize != c2.maxTextureSize)
                return false;
            if (c1.name != c2.name)
                return false;
            if (c1.overridden != c2.overridden)
                return false;
            if (c1.textureCompression != c2.textureCompression)
                return false;
            #if UNITY_2017_2_OR_NEWER
            if (c1.resizeAlgorithm != c2.resizeAlgorithm)
                return false;
            #endif

            return true;
        }
        #endif
    }
}
