using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;

namespace AssetGraph {
	public class InternalSamplingImportAdopter : AssetPostprocessor {
		
		static AssetImporter importerSourceObj = null;
		
		public static void Attach (AssetImporter newImporter) {
			importerSourceObj = newImporter;
		}
		public static void Detach () {
			importerSourceObj = null;
		}

		/*
			Unity's import handlers.
		*/
		// public void OnPostprocessGameObjectWithUserProperties (GameObject g, string[] propNames, object[] values) {}
		public void OnPreprocessTexture () {
			if (importerSourceObj == null) return;
			var importerSource = importerSourceObj as TextureImporter;

			var importer = assetImporter as TextureImporter;

			importer.anisoLevel = importerSource.anisoLevel;
			importer.borderMipmap = importerSource.borderMipmap;
			importer.compressionQuality = importerSource.compressionQuality;
			importer.convertToNormalmap = importerSource.convertToNormalmap;
			importer.fadeout = importerSource.fadeout;
			importer.filterMode = importerSource.filterMode;
			importer.generateCubemap = importerSource.generateCubemap;
			importer.generateMipsInLinearSpace = importerSource.generateMipsInLinearSpace;
			importer.grayscaleToAlpha = importerSource.grayscaleToAlpha;
			importer.heightmapScale = importerSource.heightmapScale;
			importer.isReadable = importerSource.isReadable;
			importer.lightmap = importerSource.lightmap;
			importer.linearTexture = importerSource.linearTexture;
			importer.maxTextureSize = importerSource.maxTextureSize;
			importer.mipMapBias = importerSource.mipMapBias;
			importer.mipmapEnabled = importerSource.mipmapEnabled;
			importer.mipmapFadeDistanceEnd = importerSource.mipmapFadeDistanceEnd;
			importer.mipmapFadeDistanceStart = importerSource.mipmapFadeDistanceStart;
			importer.mipmapFilter = importerSource.mipmapFilter;
			importer.normalmap = importerSource.normalmap;
			importer.normalmapFilter = importerSource.normalmapFilter;
			importer.npotScale = importerSource.npotScale;
			// importer.qualifiesForSpritePacking = importerSource.qualifiesForSpritePacking;
			importer.spriteBorder = importerSource.spriteBorder;
			importer.spriteImportMode = importerSource.spriteImportMode;
			importer.spritePackingTag = importerSource.spritePackingTag;
			importer.spritePivot = importerSource.spritePivot;
			importer.spritePixelsPerUnit = importerSource.spritePixelsPerUnit;
			importer.spritesheet = importerSource.spritesheet;
			importer.textureFormat = importerSource.textureFormat;
			importer.textureType = importerSource.textureType;
			importer.wrapMode = importerSource.wrapMode;
		}
		// public void OnPostprocessTexture (Texture2D texture) {}
		public void OnPreprocessAudio () {
			if (importerSourceObj == null) return;
			var importerSource = importerSourceObj as AudioImporter;

			var importer = assetImporter as UnityEditor.AudioImporter;

			importer.defaultSampleSettings = importerSource.defaultSampleSettings;
			importer.forceToMono = importerSource.forceToMono;
			importer.loadInBackground = importerSource.loadInBackground;
			importer.preloadAudioData = importerSource.preloadAudioData;
		}
		// public void OnPostprocessAudio (AudioClip clip) {}
		public void OnPreprocessModel () {
			if (importerSourceObj == null) return;
			var importerSource = importerSourceObj as ModelImporter;

			var importer = assetImporter as ModelImporter;

			importer.addCollider = importerSource.addCollider;
			importer.animationCompression = importerSource.animationCompression;
			importer.animationPositionError = importerSource.animationPositionError;
			importer.animationRotationError = importerSource.animationRotationError;
			importer.animationScaleError = importerSource.animationScaleError;
			importer.animationType = importerSource.animationType;
			importer.animationWrapMode = importerSource.animationWrapMode;
			importer.bakeIK = importerSource.bakeIK;
			importer.clipAnimations = importerSource.clipAnimations;
			// importer.defaultClipAnimations = importerSource.defaultClipAnimations;
			importer.extraExposedTransformPaths = importerSource.extraExposedTransformPaths;
			// importer.fileScale = importerSource.fileScale;
			importer.generateAnimations = importerSource.generateAnimations;
			importer.generateSecondaryUV = importerSource.generateSecondaryUV;
			importer.globalScale = importerSource.globalScale;
			importer.humanDescription = importerSource.humanDescription;
			importer.importAnimation = importerSource.importAnimation;
			importer.importBlendShapes = importerSource.importBlendShapes;
			// importer.importedTakeInfos = importerSource.importedTakeInfos;
			importer.importMaterials = importerSource.importMaterials;
			// importer.isBakeIKSupported = importerSource.isBakeIKSupported;
			// importer.isFileScaleUsed = importerSource.isFileScaleUsed;
			importer.isReadable = importerSource.isReadable;
			// importer.isTangentImportSupported = importerSource.isTangentImportSupported;
			// importer.isUseFileUnitsSupported = importerSource.isUseFileUnitsSupported;
			importer.materialName = importerSource.materialName;
			importer.materialSearch = importerSource.materialSearch;
			importer.meshCompression = importerSource.meshCompression;
			importer.motionNodeName = importerSource.motionNodeName;
			importer.normalImportMode = importerSource.normalImportMode;
			importer.normalSmoothingAngle = importerSource.normalSmoothingAngle;
			importer.optimizeGameObjects = importerSource.optimizeGameObjects;
			importer.optimizeMesh = importerSource.optimizeMesh;
			// importer.referencedClips = importerSource.referencedClips;
			importer.secondaryUVAngleDistortion = importerSource.secondaryUVAngleDistortion;
			importer.secondaryUVAreaDistortion = importerSource.secondaryUVAreaDistortion;
			importer.secondaryUVHardAngle = importerSource.secondaryUVHardAngle;
			importer.secondaryUVPackMargin = importerSource.secondaryUVPackMargin;
			importer.sourceAvatar = importerSource.sourceAvatar;
			importer.splitTangentsAcrossSeams = importerSource.splitTangentsAcrossSeams;
			importer.swapUVChannels = importerSource.swapUVChannels;
			importer.tangentImportMode = importerSource.tangentImportMode;
			// importer.transformPaths = importerSource.transformPaths;
			importer.useFileUnits = importerSource.useFileUnits;
		}
		// public void OnPostprocessModel (GameObject g) {}
		// public void OnAssignMaterialModel (Material material, Renderer renderer) {}
	}
}