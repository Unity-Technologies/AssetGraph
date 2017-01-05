using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;

namespace AssetBundleGraph {
	public class ImportSettingsConfigurator {
		
		private readonly AssetImporter referenceImporter;

		public ImportSettingsConfigurator (AssetImporter referenceImporter) {
			this.referenceImporter = referenceImporter;
		}

		public bool IsEqual(AssetImporter importer) {

			if(importer.GetType() != referenceImporter.GetType()) {
				throw new AssetBundleGraphException("Importer type does not match.");
			}

			if(importer.GetType() == typeof(UnityEditor.TextureImporter)) {
				return IsEqual(importer as UnityEditor.TextureImporter);
			}
			else if(importer.GetType() == typeof(UnityEditor.AudioImporter)) {
				return IsEqual(importer as UnityEditor.AudioImporter);
			}
			else if(importer.GetType() == typeof(UnityEditor.ModelImporter)) {
				return IsEqual(importer as UnityEditor.ModelImporter);
			} else {
				throw new AssetBundleGraphException("Unknown importer type found:" + importer.GetType());
			}
		}

		public void OverwriteImportSettings(AssetImporter importer) {

			// avoid touching asset if there is no need to.
			if(IsEqual(importer)) {
				return;
			}

			if(importer.GetType() != referenceImporter.GetType()) {
				throw new AssetBundleGraphException("Importer type does not match.");
			}

			if(importer.GetType() == typeof(UnityEditor.TextureImporter)) {
				OverwriteImportSettings(importer as UnityEditor.TextureImporter);
			}
			else if(importer.GetType() == typeof(UnityEditor.AudioImporter)) {
				OverwriteImportSettings(importer as UnityEditor.AudioImporter);
			}
			else if(importer.GetType() == typeof(UnityEditor.ModelImporter)) {
				OverwriteImportSettings(importer as UnityEditor.ModelImporter);
			} else {
				throw new AssetBundleGraphException("Unknown importer type found:" + importer.GetType());
			}
		}

		private void OverwriteImportSettings (TextureImporter importer) {
			var reference = referenceImporter as TextureImporter;
			UnityEngine.Assertions.Assert.IsNotNull(reference);

			importer.anisoLevel = reference.anisoLevel;
			importer.borderMipmap = reference.borderMipmap;
			importer.compressionQuality = reference.compressionQuality;
			importer.convertToNormalmap = reference.convertToNormalmap;
			importer.fadeout = reference.fadeout;
			importer.filterMode = reference.filterMode;
			importer.generateCubemap = reference.generateCubemap;

			importer.heightmapScale = reference.heightmapScale;

			importer.isReadable = reference.isReadable;
			importer.maxTextureSize = reference.maxTextureSize;
			importer.mipMapBias = reference.mipMapBias;
			importer.mipmapEnabled = reference.mipmapEnabled;
			importer.mipmapFadeDistanceEnd = reference.mipmapFadeDistanceEnd;
			importer.mipmapFadeDistanceStart = reference.mipmapFadeDistanceStart;
			importer.mipmapFilter = reference.mipmapFilter;

			importer.normalmapFilter = reference.normalmapFilter;
			importer.npotScale = reference.npotScale;
			importer.spriteBorder = reference.spriteBorder;
			importer.spriteImportMode = reference.spriteImportMode;
			importer.spritePackingTag = reference.spritePackingTag;
			importer.spritePivot = reference.spritePivot;
			importer.spritePixelsPerUnit = reference.spritePixelsPerUnit;
			importer.spritesheet = reference.spritesheet;

			importer.textureType = reference.textureType;
			importer.wrapMode = reference.wrapMode;

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
			#endif

			#if UNITY_5_5_OR_NEWER
			importer.alphaSource = reference.alphaSource;
			importer.sRGBTexture = reference.sRGBTexture;
			#endif
		}

		private bool IsEqual (TextureImporter target) {
			TextureImporter reference = referenceImporter as TextureImporter;
			UnityEngine.Assertions.Assert.IsNotNull(reference);

			if (target.anisoLevel != reference.anisoLevel) return false;
			if (target.borderMipmap != reference.borderMipmap) return false;
			if (target.compressionQuality != reference.compressionQuality) return false;
			if (target.convertToNormalmap != reference.convertToNormalmap) return false;
			if (target.fadeout != reference.fadeout) return false;
			if (target.filterMode != reference.filterMode) return false;
			if (target.generateCubemap != reference.generateCubemap) return false;
			if (target.heightmapScale != reference.heightmapScale) return false;
			if (target.isReadable != reference.isReadable) return false;
			if (target.maxTextureSize != reference.maxTextureSize) return false;
			if (target.mipMapBias != reference.mipMapBias) return false;
			if (target.mipmapEnabled != reference.mipmapEnabled) return false;
			if (target.mipmapFadeDistanceEnd != reference.mipmapFadeDistanceEnd) return false;
			if (target.mipmapFadeDistanceStart != reference.mipmapFadeDistanceStart) return false;
			if (target.mipmapFilter != reference.mipmapFilter) return false;
			if (target.normalmapFilter != reference.normalmapFilter) return false;
			if (target.npotScale != reference.npotScale) return false;
			if (target.spriteBorder != reference.spriteBorder) return false;
			if (target.spriteImportMode != reference.spriteImportMode) return false;
			if (target.spritePackingTag != reference.spritePackingTag) return false;
			if (target.spritePivot != reference.spritePivot) return false;
			if (target.spritePixelsPerUnit != reference.spritePixelsPerUnit) return false;

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
			#endif

			#if UNITY_5_5_OR_NEWER
			if (target.alphaSource != reference.alphaSource) return false;
			if (target.sRGBTexture != reference.sRGBTexture) return false;
			#endif

			// spritesheet
			{
				if (target.spritesheet.Length != reference.spritesheet.Length) return false;
				for (int i = 0; i < target.spritesheet.Length; i++) {
					if (target.spritesheet[i].alignment != reference.spritesheet[i].alignment) return false;
					if (target.spritesheet[i].border != reference.spritesheet[i].border) return false;
					if (target.spritesheet[i].name != reference.spritesheet[i].name) return false;
					if (target.spritesheet[i].pivot != reference.spritesheet[i].pivot) return false;
					if (target.spritesheet[i].rect != reference.spritesheet[i].rect) return false;
				}
			}

      		// UnityEditor.TextureImporter.textureFormat' is obsolete: 
			// `textureFormat is not longer accessible at the TextureImporter level
			if (target.textureType != reference.textureType) return false;
			if (target.wrapMode != reference.wrapMode) return false;
			return true;
		}

		private void OverwriteImportSettings (AudioImporter importer) {
			var reference = referenceImporter as AudioImporter;
			UnityEngine.Assertions.Assert.IsNotNull(reference);

			importer.defaultSampleSettings = reference.defaultSampleSettings;
			importer.forceToMono = reference.forceToMono;
			importer.loadInBackground = reference.loadInBackground;
			importer.preloadAudioData = reference.preloadAudioData;
		}

		private bool IsEqual (AudioImporter target) {
			AudioImporter reference = referenceImporter as AudioImporter;
			UnityEngine.Assertions.Assert.IsNotNull(reference);

			// defaultSampleSettings
			if (target.defaultSampleSettings.compressionFormat != reference.defaultSampleSettings.compressionFormat) return false;
			if (target.defaultSampleSettings.loadType != reference.defaultSampleSettings.loadType) return false;
			if (target.defaultSampleSettings.quality != reference.defaultSampleSettings.quality) return false;
			if (target.defaultSampleSettings.sampleRateOverride != reference.defaultSampleSettings.sampleRateOverride) return false;
			if (target.defaultSampleSettings.sampleRateSetting != reference.defaultSampleSettings.sampleRateSetting) return false;

			if (target.forceToMono != reference.forceToMono) return false;
			if (target.loadInBackground != reference.loadInBackground) return false;
			if (target.preloadAudioData != reference.preloadAudioData) return false;

			return true;
		}

		private void OverwriteImportSettings (ModelImporter importer) {
			var reference = referenceImporter as ModelImporter;
			UnityEngine.Assertions.Assert.IsNotNull(reference);

			importer.addCollider = reference.addCollider;
			importer.animationCompression = reference.animationCompression;
			importer.animationPositionError = reference.animationPositionError;
			importer.animationRotationError = reference.animationRotationError;
			importer.animationScaleError = reference.animationScaleError;
			importer.animationType = reference.animationType;
			importer.animationWrapMode = reference.animationWrapMode;
			importer.bakeIK = reference.bakeIK;
			importer.clipAnimations = reference.clipAnimations;

			importer.extraExposedTransformPaths = reference.extraExposedTransformPaths;
			importer.generateAnimations = reference.generateAnimations;
			importer.generateSecondaryUV = reference.generateSecondaryUV;
			importer.globalScale = reference.globalScale;
			importer.humanDescription = reference.humanDescription;
			importer.importBlendShapes = reference.importBlendShapes;

			importer.isReadable = reference.isReadable;
			importer.materialName = reference.materialName;
			importer.materialSearch = reference.materialSearch;

			importer.normalSmoothingAngle = reference.normalSmoothingAngle;
			importer.optimizeMesh = reference.optimizeMesh;
			importer.secondaryUVAngleDistortion = reference.secondaryUVAngleDistortion;
			importer.secondaryUVAreaDistortion = reference.secondaryUVAreaDistortion;
			importer.secondaryUVHardAngle = reference.secondaryUVHardAngle;
			importer.secondaryUVPackMargin = reference.secondaryUVPackMargin;
			importer.sourceAvatar = reference.sourceAvatar;
			importer.swapUVChannels = reference.swapUVChannels;

			importer.importTangents = reference.importTangents;

			#if UNITY_5_6
			importer.keepQuads = reference.keepQuads;
			importer.weldVertices = reference.weldVertices;
			#endif

			/* read only */
			/* 
			importer.importedTakeInfos
			importer.defaultClipAnimations
			importer.importAnimation
			importer.isTangentImportSupported
			importer.meshCompression
			importer.importNormals
			importer.optimizeGameObjects
			importer.referencedClips
			importer.fileScale
			importer.importMaterials
			importer.isUseFileUnitsSupported
			importer.motionNodeName
			importer.isBakeIKSupported
			importer.isFileScaleUsed
			importer.useFileUnits
			importer.transformPaths
			*/

			/* Obsolete */
		}

		public bool IsEqual (ModelImporter target) {
			ModelImporter reference = referenceImporter as ModelImporter;
			UnityEngine.Assertions.Assert.IsNotNull(reference);

			if (target.addCollider != reference.addCollider) return false;
			if (target.animationCompression != reference.animationCompression) return false;
			if (target.animationPositionError != reference.animationPositionError) return false;
			if (target.animationRotationError != reference.animationRotationError) return false;
			if (target.animationScaleError != reference.animationScaleError) return false;
			if (target.animationType != reference.animationType) return false;
			if (target.animationWrapMode != reference.animationWrapMode) return false;
			if (target.bakeIK != reference.bakeIK) return false;

			// clipAnimations
			{
				if (target.clipAnimations.Length != reference.clipAnimations.Length) return false;
				for (int i = 0; i < target.clipAnimations.Length; i++) {
					if (target.clipAnimations[i].curves != reference.clipAnimations[i].curves) return false;
					if (target.clipAnimations[i].cycleOffset != reference.clipAnimations[i].cycleOffset) return false;
					if (target.clipAnimations[i].events != reference.clipAnimations[i].events) return false;
					if (target.clipAnimations[i].firstFrame != reference.clipAnimations[i].firstFrame) return false;
					if (target.clipAnimations[i].heightFromFeet != reference.clipAnimations[i].heightFromFeet) return false;
					if (target.clipAnimations[i].heightOffset != reference.clipAnimations[i].heightOffset) return false;
					if (target.clipAnimations[i].keepOriginalOrientation != reference.clipAnimations[i].keepOriginalOrientation) return false;
					if (target.clipAnimations[i].keepOriginalPositionXZ != reference.clipAnimations[i].keepOriginalPositionXZ) return false;
					if (target.clipAnimations[i].keepOriginalPositionY != reference.clipAnimations[i].keepOriginalPositionY) return false;
					if (target.clipAnimations[i].lastFrame != reference.clipAnimations[i].lastFrame) return false;
					if (target.clipAnimations[i].lockRootHeightY != reference.clipAnimations[i].lockRootHeightY) return false;
					if (target.clipAnimations[i].lockRootPositionXZ != reference.clipAnimations[i].lockRootPositionXZ) return false;
					if (target.clipAnimations[i].lockRootRotation != reference.clipAnimations[i].lockRootRotation) return false;
					if (target.clipAnimations[i].loop != reference.clipAnimations[i].loop) return false;
					if (target.clipAnimations[i].loopPose != reference.clipAnimations[i].loopPose) return false;
					if (target.clipAnimations[i].loopTime != reference.clipAnimations[i].loopTime) return false;
					if (target.clipAnimations[i].maskNeedsUpdating != reference.clipAnimations[i].maskNeedsUpdating) return false;
					if (target.clipAnimations[i].maskSource != reference.clipAnimations[i].maskSource) return false;
					if (target.clipAnimations[i].maskType != reference.clipAnimations[i].maskType) return false;
					if (target.clipAnimations[i].mirror != reference.clipAnimations[i].mirror) return false;
					if (target.clipAnimations[i].name != reference.clipAnimations[i].name) return false;
					if (target.clipAnimations[i].rotationOffset != reference.clipAnimations[i].rotationOffset) return false;
					if (target.clipAnimations[i].takeName != reference.clipAnimations[i].takeName) return false;
					if (target.clipAnimations[i].wrapMode != reference.clipAnimations[i].wrapMode) return false;
				}
			}

			if (target.defaultClipAnimations != reference.defaultClipAnimations) return false;

			// extraExposedTransformPaths
			{
				if (target.extraExposedTransformPaths.Length != reference.extraExposedTransformPaths.Length) return false;
				for (int i = 0; i < target.extraExposedTransformPaths.Length; i++) {
					if (target.extraExposedTransformPaths[i] != reference.extraExposedTransformPaths[i]) return false;
				}
			}

			if (target.fileScale != reference.fileScale) return false;
			if (target.generateAnimations != reference.generateAnimations) return false;
			if (target.generateSecondaryUV != reference.generateSecondaryUV) return false;
			if (target.globalScale != reference.globalScale) return false;

			// humanDescription
			{
				if (target.humanDescription.armStretch != reference.humanDescription.armStretch) return false;
				if (target.humanDescription.feetSpacing != reference.humanDescription.feetSpacing) return false;

				// human
				{
					if (target.humanDescription.human.Length != reference.humanDescription.human.Length) return false;
					for (int i = 0; i < target.humanDescription.human.Length; i++) {
						if (target.humanDescription.human[i].boneName != reference.humanDescription.human[i].boneName) return false;
						if (target.humanDescription.human[i].humanName != reference.humanDescription.human[i].humanName) return false;

						// limit
						if (target.humanDescription.human[i].limit.axisLength != reference.humanDescription.human[i].limit.axisLength) return false;
						if (target.humanDescription.human[i].limit.center != reference.humanDescription.human[i].limit.center) return false;
						if (target.humanDescription.human[i].limit.max != reference.humanDescription.human[i].limit.max) return false;
						if (target.humanDescription.human[i].limit.min != reference.humanDescription.human[i].limit.min) return false;
						if (target.humanDescription.human[i].limit.useDefaultValues != reference.humanDescription.human[i].limit.useDefaultValues) return false;
					}
				}

				if (target.humanDescription.legStretch != reference.humanDescription.legStretch) return false;
				if (target.humanDescription.lowerArmTwist != reference.humanDescription.lowerArmTwist) return false;
				if (target.humanDescription.lowerLegTwist != reference.humanDescription.lowerLegTwist) return false;

				// skeleton
				{
					if (target.humanDescription.skeleton.Length != reference.humanDescription.skeleton.Length) return false;
					for (int i = 0; i < target.humanDescription.skeleton.Length; i++) {
						if (target.humanDescription.skeleton[i].name != reference.humanDescription.skeleton[i].name) return false;
						if (target.humanDescription.skeleton[i].position != reference.humanDescription.skeleton[i].position) return false;
						if (target.humanDescription.skeleton[i].rotation != reference.humanDescription.skeleton[i].rotation) return false;
						if (target.humanDescription.skeleton[i].scale != reference.humanDescription.skeleton[i].scale) return false;
					}
				}

				if (target.humanDescription.upperArmTwist != reference.humanDescription.upperArmTwist) return false;
				if (target.humanDescription.upperLegTwist != reference.humanDescription.upperLegTwist) return false;
			}

			if (target.importAnimation != reference.importAnimation) return false;
			if (target.importBlendShapes != reference.importBlendShapes) return false;
			if (target.importedTakeInfos != reference.importedTakeInfos) return false;
			if (target.importMaterials != reference.importMaterials) return false;
			if (target.isBakeIKSupported != reference.isBakeIKSupported) return false;
			if (target.isFileScaleUsed != reference.isFileScaleUsed) return false;
			if (target.isReadable != reference.isReadable) return false;
			if (target.isTangentImportSupported != reference.isTangentImportSupported) return false;
			if (target.isUseFileUnitsSupported != reference.isUseFileUnitsSupported) return false;
			if (target.materialName != reference.materialName) return false;
			if (target.materialSearch != reference.materialSearch) return false;
			if (target.meshCompression != reference.meshCompression) return false;
			if (target.motionNodeName != reference.motionNodeName) return false;
			if (target.importNormals != reference.importNormals) return false;
			if (target.normalSmoothingAngle != reference.normalSmoothingAngle) return false;
			if (target.optimizeGameObjects != reference.optimizeGameObjects) return false;
			if (target.optimizeMesh != reference.optimizeMesh) return false;

			if (target.referencedClips != reference.referencedClips) return false;
			if (target.secondaryUVAngleDistortion != reference.secondaryUVAngleDistortion) return false;
			if (target.secondaryUVAreaDistortion != reference.secondaryUVAreaDistortion) return false;
			if (target.secondaryUVHardAngle != reference.secondaryUVHardAngle) return false;
			if (target.secondaryUVPackMargin != reference.secondaryUVPackMargin) return false;
			if (target.sourceAvatar != reference.sourceAvatar) return false;
			if (target.swapUVChannels != reference.swapUVChannels) return false;
			if (target.importTangents != reference.importTangents) return false;
			if (target.transformPaths != reference.transformPaths) return false;
			if (target.useFileUnits != reference.useFileUnits) return false;

			#if UNITY_5_6
			if (target.keepQuads != reference.keepQuads) return false;
			if (target.weldVertices != reference.weldVertices) return false;
			#endif

			return true;
		}
	}
}
