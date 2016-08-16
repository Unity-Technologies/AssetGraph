using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;

/*
	this class is effector of import setting node.
	forcely effect settings from source obj to target obj.
*/
namespace AssetBundleGraph {
	public class InternalSamplingImportEffector {
		
		private readonly AssetImporter importerSourceObj;
		
		public InternalSamplingImportEffector (AssetImporter importerSourceObj) {
			this.importerSourceObj = importerSourceObj;
		}
		
		/*
			Psuade Unity's import handlers.
		*/
		// public void OnPostprocessGameObjectWithUserProperties (GameObject g, string[] propNames, object[] values) {}

		public void ForceOnPreprocessTexture (TextureImporter importer) {
			if (importerSourceObj == null) return;
			var importerSource = importerSourceObj as TextureImporter;
			if (importerSource == null) return;

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
		
		public static bool IsSameTextureSetting (TextureImporter target, TextureImporter compareBase) {
			if (target.anisoLevel != compareBase.anisoLevel) return false;
			if (target.borderMipmap != compareBase.borderMipmap) return false;
			if (target.compressionQuality != compareBase.compressionQuality) return false;
			if (target.convertToNormalmap != compareBase.convertToNormalmap) return false;
			if (target.fadeout != compareBase.fadeout) return false;
			if (target.filterMode != compareBase.filterMode) return false;
			if (target.generateCubemap != compareBase.generateCubemap) return false;
			if (target.generateMipsInLinearSpace != compareBase.generateMipsInLinearSpace) return false;
			if (target.grayscaleToAlpha != compareBase.grayscaleToAlpha) return false;
			if (target.heightmapScale != compareBase.heightmapScale) return false;
			if (target.isReadable != compareBase.isReadable) return false;
			if (target.lightmap != compareBase.lightmap) return false;
			if (target.linearTexture != compareBase.linearTexture) return false;
			if (target.maxTextureSize != compareBase.maxTextureSize) return false;
			if (target.mipMapBias != compareBase.mipMapBias) return false;
			if (target.mipmapEnabled != compareBase.mipmapEnabled) return false;
			if (target.mipmapFadeDistanceEnd != compareBase.mipmapFadeDistanceEnd) return false;
			if (target.mipmapFadeDistanceStart != compareBase.mipmapFadeDistanceStart) return false;
			if (target.mipmapFilter != compareBase.mipmapFilter) return false;
			if (target.normalmap != compareBase.normalmap) return false;
			if (target.normalmapFilter != compareBase.normalmapFilter) return false;
			if (target.npotScale != compareBase.npotScale) return false;
			// if (target.qualifiesForSpritePacking != compareBase.qualifiesForSpritePacking) return false;
			if (target.spriteBorder != compareBase.spriteBorder) return false;
			if (target.spriteImportMode != compareBase.spriteImportMode) return false;
			if (target.spritePackingTag != compareBase.spritePackingTag) return false;
			if (target.spritePivot != compareBase.spritePivot) return false;
			if (target.spritePixelsPerUnit != compareBase.spritePixelsPerUnit) return false;

			// spritesheet
			{
				if (target.spritesheet.Length != compareBase.spritesheet.Length) return false;
				for (int i = 0; i < target.spritesheet.Length; i++) {
					if (target.spritesheet[i].alignment != compareBase.spritesheet[i].alignment) return false;
					if (target.spritesheet[i].border != compareBase.spritesheet[i].border) return false;
					if (target.spritesheet[i].name != compareBase.spritesheet[i].name) return false;
					if (target.spritesheet[i].pivot != compareBase.spritesheet[i].pivot) return false;
					if (target.spritesheet[i].rect != compareBase.spritesheet[i].rect) return false;
				}
			}

			if (target.textureFormat != compareBase.textureFormat) return false;
			if (target.textureType != compareBase.textureType) return false;
			if (target.wrapMode != compareBase.wrapMode) return false;
			return true;
		}

		public void ForceOnPreprocessAudio (AudioImporter importer) {
			if (importerSourceObj == null) return;
			var importerSource = importerSourceObj as AudioImporter;

			importer.defaultSampleSettings = importerSource.defaultSampleSettings;
			importer.forceToMono = importerSource.forceToMono;
			importer.loadInBackground = importerSource.loadInBackground;
			importer.preloadAudioData = importerSource.preloadAudioData;
		}

		// public void OnPostprocessAudio (AudioClip clip) {}
		
		public static bool IsSameAudioSetting (AudioImporter target, AudioImporter compareBase) {
			// defaultSampleSettings
			if (target.defaultSampleSettings.compressionFormat != compareBase.defaultSampleSettings.compressionFormat) return false;
			if (target.defaultSampleSettings.loadType != compareBase.defaultSampleSettings.loadType) return false;
			if (target.defaultSampleSettings.quality != compareBase.defaultSampleSettings.quality) return false;
			if (target.defaultSampleSettings.sampleRateOverride != compareBase.defaultSampleSettings.sampleRateOverride) return false;
			if (target.defaultSampleSettings.sampleRateSetting != compareBase.defaultSampleSettings.sampleRateSetting) return false;

			if (target.forceToMono != compareBase.forceToMono) return false;
			if (target.loadInBackground != compareBase.loadInBackground) return false;
			if (target.preloadAudioData != compareBase.preloadAudioData) return false;

			return true;
		}
		
		public void ForceOnPreprocessModel (ModelImporter importer) {
			if (importerSourceObj == null) return;
			var importerSource = importerSourceObj as ModelImporter;
			if (importerSource == null) return;
			
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
			importer.importNormals = importerSource.importNormals;
			importer.normalSmoothingAngle = importerSource.normalSmoothingAngle;
			importer.optimizeGameObjects = importerSource.optimizeGameObjects;
			importer.optimizeMesh = importerSource.optimizeMesh;
			// importer.referencedClips = importerSource.referencedClips;
			importer.secondaryUVAngleDistortion = importerSource.secondaryUVAngleDistortion;
			importer.secondaryUVAreaDistortion = importerSource.secondaryUVAreaDistortion;
			importer.secondaryUVHardAngle = importerSource.secondaryUVHardAngle;
			importer.secondaryUVPackMargin = importerSource.secondaryUVPackMargin;
			importer.sourceAvatar = importerSource.sourceAvatar;
			importer.swapUVChannels = importerSource.swapUVChannels;
			importer.importTangents = importerSource.importTangents;
			// importer.transformPaths = importerSource.transformPaths;
			importer.useFileUnits = importerSource.useFileUnits;
		}

		// public void OnPostprocessModel (GameObject g) {}
		// public void OnAssignMaterialModel (Material material, Renderer renderer) {}
		
		public static bool IsSameModelSetting (ModelImporter target, ModelImporter compareBase) {
			if (target.addCollider != compareBase.addCollider) return false;
			if (target.animationCompression != compareBase.animationCompression) return false;
			if (target.animationPositionError != compareBase.animationPositionError) return false;
			if (target.animationRotationError != compareBase.animationRotationError) return false;
			if (target.animationScaleError != compareBase.animationScaleError) return false;
			if (target.animationType != compareBase.animationType) return false;
			if (target.animationWrapMode != compareBase.animationWrapMode) return false;
			if (target.bakeIK != compareBase.bakeIK) return false;
			
			// clipAnimations
			{
				if (target.clipAnimations.Length != compareBase.clipAnimations.Length) return false;
				for (int i = 0; i < target.clipAnimations.Length; i++) {
					if (target.clipAnimations[i].curves != compareBase.clipAnimations[i].curves) return false;
					if (target.clipAnimations[i].cycleOffset != compareBase.clipAnimations[i].cycleOffset) return false;
					if (target.clipAnimations[i].events != compareBase.clipAnimations[i].events) return false;
					if (target.clipAnimations[i].firstFrame != compareBase.clipAnimations[i].firstFrame) return false;
					if (target.clipAnimations[i].heightFromFeet != compareBase.clipAnimations[i].heightFromFeet) return false;
					if (target.clipAnimations[i].heightOffset != compareBase.clipAnimations[i].heightOffset) return false;
					if (target.clipAnimations[i].keepOriginalOrientation != compareBase.clipAnimations[i].keepOriginalOrientation) return false;
					if (target.clipAnimations[i].keepOriginalPositionXZ != compareBase.clipAnimations[i].keepOriginalPositionXZ) return false;
					if (target.clipAnimations[i].keepOriginalPositionY != compareBase.clipAnimations[i].keepOriginalPositionY) return false;
					if (target.clipAnimations[i].lastFrame != compareBase.clipAnimations[i].lastFrame) return false;
					if (target.clipAnimations[i].lockRootHeightY != compareBase.clipAnimations[i].lockRootHeightY) return false;
					if (target.clipAnimations[i].lockRootPositionXZ != compareBase.clipAnimations[i].lockRootPositionXZ) return false;
					if (target.clipAnimations[i].lockRootRotation != compareBase.clipAnimations[i].lockRootRotation) return false;
					if (target.clipAnimations[i].loop != compareBase.clipAnimations[i].loop) return false;
					if (target.clipAnimations[i].loopPose != compareBase.clipAnimations[i].loopPose) return false;
					if (target.clipAnimations[i].loopTime != compareBase.clipAnimations[i].loopTime) return false;
					if (target.clipAnimations[i].maskNeedsUpdating != compareBase.clipAnimations[i].maskNeedsUpdating) return false;
					if (target.clipAnimations[i].maskSource != compareBase.clipAnimations[i].maskSource) return false;
					if (target.clipAnimations[i].maskType != compareBase.clipAnimations[i].maskType) return false;
					if (target.clipAnimations[i].mirror != compareBase.clipAnimations[i].mirror) return false;
					if (target.clipAnimations[i].name != compareBase.clipAnimations[i].name) return false;
					if (target.clipAnimations[i].rotationOffset != compareBase.clipAnimations[i].rotationOffset) return false;
					if (target.clipAnimations[i].takeName != compareBase.clipAnimations[i].takeName) return false;
					if (target.clipAnimations[i].wrapMode != compareBase.clipAnimations[i].wrapMode) return false;
				}
			}
			
			// if (target.defaultClipAnimations != compareBase.defaultClipAnimations) return false;

			// extraExposedTransformPaths
			{
				if (target.extraExposedTransformPaths.Length != compareBase.extraExposedTransformPaths.Length) return false;
				for (int i = 0; i < target.extraExposedTransformPaths.Length; i++) {
					if (target.extraExposedTransformPaths[i] != compareBase.extraExposedTransformPaths[i]) return false;
				}
			}

			// if (target.fileScale != compareBase.fileScale) return false;
			if (target.generateAnimations != compareBase.generateAnimations) return false;
			if (target.generateSecondaryUV != compareBase.generateSecondaryUV) return false;
			if (target.globalScale != compareBase.globalScale) return false;
			
			// humanDescription
			{
				if (target.humanDescription.armStretch != compareBase.humanDescription.armStretch) return false;
				if (target.humanDescription.feetSpacing != compareBase.humanDescription.feetSpacing) return false;

				// human
				{
					if (target.humanDescription.human.Length != compareBase.humanDescription.human.Length) return false;
					for (int i = 0; i < target.humanDescription.human.Length; i++) {
						if (target.humanDescription.human[i].boneName != compareBase.humanDescription.human[i].boneName) return false;
						if (target.humanDescription.human[i].humanName != compareBase.humanDescription.human[i].humanName) return false;

						// limit
						if (target.humanDescription.human[i].limit.axisLength != compareBase.humanDescription.human[i].limit.axisLength) return false;
						if (target.humanDescription.human[i].limit.center != compareBase.humanDescription.human[i].limit.center) return false;
						if (target.humanDescription.human[i].limit.max != compareBase.humanDescription.human[i].limit.max) return false;
						if (target.humanDescription.human[i].limit.min != compareBase.humanDescription.human[i].limit.min) return false;
						if (target.humanDescription.human[i].limit.useDefaultValues != compareBase.humanDescription.human[i].limit.useDefaultValues) return false;
					}
				}

				if (target.humanDescription.legStretch != compareBase.humanDescription.legStretch) return false;
				if (target.humanDescription.lowerArmTwist != compareBase.humanDescription.lowerArmTwist) return false;
				if (target.humanDescription.lowerLegTwist != compareBase.humanDescription.lowerLegTwist) return false;
				
				// skeleton
				{
					if (target.humanDescription.skeleton.Length != compareBase.humanDescription.skeleton.Length) return false;
					for (int i = 0; i < target.humanDescription.skeleton.Length; i++) {
						if (target.humanDescription.skeleton[i].name != compareBase.humanDescription.skeleton[i].name) return false;
						if (target.humanDescription.skeleton[i].position != compareBase.humanDescription.skeleton[i].position) return false;
						if (target.humanDescription.skeleton[i].rotation != compareBase.humanDescription.skeleton[i].rotation) return false;
						if (target.humanDescription.skeleton[i].scale != compareBase.humanDescription.skeleton[i].scale) return false;
					}
				}

				if (target.humanDescription.upperArmTwist != compareBase.humanDescription.upperArmTwist) return false;
				if (target.humanDescription.upperLegTwist != compareBase.humanDescription.upperLegTwist) return false;
			}
			
			if (target.importAnimation != compareBase.importAnimation) return false;
			if (target.importBlendShapes != compareBase.importBlendShapes) return false;
			// if (target.importedTakeInfos != compareBase.importedTakeInfos) return false;
			if (target.importMaterials != compareBase.importMaterials) return false;
			// if (target.isBakeIKSupported != compareBase.isBakeIKSupported) return false;
			// if (target.isFileScaleUsed != compareBase.isFileScaleUsed) return false;
			if (target.isReadable != compareBase.isReadable) return false;
			// if (target.isTangentImportSupported != compareBase.isTangentImportSupported) return false;
			// if (target.isUseFileUnitsSupported != compareBase.isUseFileUnitsSupported) return false;
			if (target.materialName != compareBase.materialName) return false;
			if (target.materialSearch != compareBase.materialSearch) return false;
			if (target.meshCompression != compareBase.meshCompression) return false;
			if (target.motionNodeName != compareBase.motionNodeName) return false;
			if (target.importNormals != compareBase.importNormals) return false;
			if (target.normalSmoothingAngle != compareBase.normalSmoothingAngle) return false;
			if (target.optimizeGameObjects != compareBase.optimizeGameObjects) return false;
			if (target.optimizeMesh != compareBase.optimizeMesh) return false;

			// if (target.referencedClips != compareBase.referencedClips) return false;
			if (target.secondaryUVAngleDistortion != compareBase.secondaryUVAngleDistortion) return false;
			if (target.secondaryUVAreaDistortion != compareBase.secondaryUVAreaDistortion) return false;
			if (target.secondaryUVHardAngle != compareBase.secondaryUVHardAngle) return false;
			if (target.secondaryUVPackMargin != compareBase.secondaryUVPackMargin) return false;
			if (target.sourceAvatar != compareBase.sourceAvatar) return false;
			if (target.swapUVChannels != compareBase.swapUVChannels) return false;
			if (target.importTangents != compareBase.importTangents) return false;
			// if (target.transformPaths != compareBase.transformPaths) return false;
			if (target.useFileUnits != compareBase.useFileUnits) return false;

			return true;
		}
	}
}
