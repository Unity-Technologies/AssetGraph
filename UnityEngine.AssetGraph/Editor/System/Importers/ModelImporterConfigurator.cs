using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;

using Model = UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph
{
    [Serializable] 
    [CustomAssetImporterConfigurator(typeof(ModelImporter), "Model", "setting.fbx")]
    public class ModelImportSettingsConfigurator : IAssetImporterConfigurator
    {
        public void Initialize (ConfigurationOption option)
        {
        }

        public bool IsModified (AssetImporter referenceImporter, AssetImporter importer, BuildTarget target, string group)
        {
            var r = referenceImporter as ModelImporter;
            var t = importer as ModelImporter;
            if (r == null || t == null) {
                throw new AssetGraphException (string.Format ("Invalid AssetImporter assigned for {0}", importer.assetPath));
            }
            return !IsEqual (t, r);
        }

        public void Configure (AssetImporter referenceImporter, AssetImporter importer, BuildTarget target, string group)
        {
            var r = referenceImporter as ModelImporter;
            var t = importer as ModelImporter;
            if (r == null || t == null) {
                throw new AssetGraphException (string.Format ("Invalid AssetImporter assigned for {0}", importer.assetPath));
            }
            OverwriteImportSettings (t, r);
        }

        public void OnInspectorGUI (AssetImporter referenceImporter, BuildTargetGroup target, Action onValueChanged)
        {
        }

        /// <summary>
        /// Test if reference importer setting has the equal setting as given target.
        /// ImportSettingsConfigurator will not test read only properties.
        /// 
        /// </summary>
        /// <returns><c>true</c>, if both settings are the equal, <c>false</c> otherwise.</returns>
        /// <param name="target">Target importer to test equality.</param>
        public bool IsEqual (ModelImporter target, ModelImporter reference)
        {
            if (target.importMaterials != reference.importMaterials)
                return false;
            if (target.importAnimation != reference.importAnimation)
                return false;
            if (target.meshCompression != reference.meshCompression)
                return false;
            if (target.importNormals != reference.importNormals)
                return false;
            if (target.optimizeGameObjects != reference.optimizeGameObjects)
                return false;
            if (target.motionNodeName != reference.motionNodeName)
                return false;
            if (target.useFileUnits != reference.useFileUnits)
                return false;

            if (target.addCollider != reference.addCollider)
                return false;
            if (target.animationCompression != reference.animationCompression)
                return false;
            if (target.animationPositionError != reference.animationPositionError)
                return false;
            if (target.animationRotationError != reference.animationRotationError)
                return false;
            if (target.animationScaleError != reference.animationScaleError)
                return false;
            if (target.animationType != reference.animationType)
                return false;
            if (target.animationWrapMode != reference.animationWrapMode)
                return false;
            if (target.bakeIK != reference.bakeIK)
                return false;

            // clipAnimations
            {
                if (target.clipAnimations.Length != reference.clipAnimations.Length)
                    return false;
                for (int i = 0; i < target.clipAnimations.Length; i++) {
                    if (target.clipAnimations [i].additiveReferencePoseFrame != reference.clipAnimations [i].additiveReferencePoseFrame)
                        return false;
                    if (target.clipAnimations [i].curves != reference.clipAnimations [i].curves)
                        return false;
                    if (target.clipAnimations [i].cycleOffset != reference.clipAnimations [i].cycleOffset)
                        return false;
                    if (target.clipAnimations [i].events != reference.clipAnimations [i].events)
                        return false;
                    if (target.clipAnimations [i].firstFrame != reference.clipAnimations [i].firstFrame)
                        return false;
                    if (target.clipAnimations [i].hasAdditiveReferencePose != reference.clipAnimations [i].hasAdditiveReferencePose)
                        return false;
                    if (target.clipAnimations [i].heightFromFeet != reference.clipAnimations [i].heightFromFeet)
                        return false;
                    if (target.clipAnimations [i].heightOffset != reference.clipAnimations [i].heightOffset)
                        return false;
                    if (target.clipAnimations [i].keepOriginalOrientation != reference.clipAnimations [i].keepOriginalOrientation)
                        return false;
                    if (target.clipAnimations [i].keepOriginalPositionXZ != reference.clipAnimations [i].keepOriginalPositionXZ)
                        return false;
                    if (target.clipAnimations [i].keepOriginalPositionY != reference.clipAnimations [i].keepOriginalPositionY)
                        return false;
                    if (target.clipAnimations [i].lastFrame != reference.clipAnimations [i].lastFrame)
                        return false;
                    if (target.clipAnimations [i].lockRootHeightY != reference.clipAnimations [i].lockRootHeightY)
                        return false;
                    if (target.clipAnimations [i].lockRootPositionXZ != reference.clipAnimations [i].lockRootPositionXZ)
                        return false;
                    if (target.clipAnimations [i].lockRootRotation != reference.clipAnimations [i].lockRootRotation)
                        return false;
                    if (target.clipAnimations [i].loop != reference.clipAnimations [i].loop)
                        return false;
                    if (target.clipAnimations [i].loopPose != reference.clipAnimations [i].loopPose)
                        return false;
                    if (target.clipAnimations [i].loopTime != reference.clipAnimations [i].loopTime)
                        return false;
                    if (target.clipAnimations [i].maskNeedsUpdating != reference.clipAnimations [i].maskNeedsUpdating)
                        return false;
                    if (target.clipAnimations [i].maskSource != reference.clipAnimations [i].maskSource)
                        return false;
                    if (target.clipAnimations [i].maskType != reference.clipAnimations [i].maskType)
                        return false;
                    if (target.clipAnimations [i].mirror != reference.clipAnimations [i].mirror)
                        return false;
                    if (target.clipAnimations [i].name != reference.clipAnimations [i].name)
                        return false;
                    if (target.clipAnimations [i].rotationOffset != reference.clipAnimations [i].rotationOffset)
                        return false;
                    if (target.clipAnimations [i].takeName != reference.clipAnimations [i].takeName)
                        return false;
                    if (target.clipAnimations [i].wrapMode != reference.clipAnimations [i].wrapMode)
                        return false;
                }
            }

            // extraExposedTransformPaths
            {
                if (target.extraExposedTransformPaths.Length != reference.extraExposedTransformPaths.Length)
                    return false;
                for (int i = 0; i < target.extraExposedTransformPaths.Length; i++) {
                    if (target.extraExposedTransformPaths [i] != reference.extraExposedTransformPaths [i])
                        return false;
                }
            }

            if (target.generateAnimations != reference.generateAnimations)
                return false;
            if (target.generateSecondaryUV != reference.generateSecondaryUV)
                return false;
            if (target.globalScale != reference.globalScale)
                return false;

            // humanDescription
            {
                if (target.humanDescription.armStretch != reference.humanDescription.armStretch)
                    return false;
                if (target.humanDescription.feetSpacing != reference.humanDescription.feetSpacing)
                    return false;

                // human
                {
                    if (target.humanDescription.human.Length != reference.humanDescription.human.Length)
                        return false;
                    for (int i = 0; i < target.humanDescription.human.Length; i++) {
                        if (target.humanDescription.human [i].boneName != reference.humanDescription.human [i].boneName)
                            return false;
                        if (target.humanDescription.human [i].humanName != reference.humanDescription.human [i].humanName)
                            return false;

                        // limit
                        if (target.humanDescription.human [i].limit.axisLength != reference.humanDescription.human [i].limit.axisLength)
                            return false;
                        if (target.humanDescription.human [i].limit.center != reference.humanDescription.human [i].limit.center)
                            return false;
                        if (target.humanDescription.human [i].limit.max != reference.humanDescription.human [i].limit.max)
                            return false;
                        if (target.humanDescription.human [i].limit.min != reference.humanDescription.human [i].limit.min)
                            return false;
                        if (target.humanDescription.human [i].limit.useDefaultValues != reference.humanDescription.human [i].limit.useDefaultValues)
                            return false;
                    }
                }

                if (target.humanDescription.legStretch != reference.humanDescription.legStretch)
                    return false;
                if (target.humanDescription.lowerArmTwist != reference.humanDescription.lowerArmTwist)
                    return false;
                if (target.humanDescription.lowerLegTwist != reference.humanDescription.lowerLegTwist)
                    return false;

                // skeleton
                {
                    if (target.humanDescription.skeleton.Length != reference.humanDescription.skeleton.Length)
                        return false;
                    for (int i = 0; i < target.humanDescription.skeleton.Length; i++) {
                        if (target.humanDescription.skeleton [i].name != reference.humanDescription.skeleton [i].name)
                            return false;
                        if (target.humanDescription.skeleton [i].position != reference.humanDescription.skeleton [i].position)
                            return false;
                        if (target.humanDescription.skeleton [i].rotation != reference.humanDescription.skeleton [i].rotation)
                            return false;
                        if (target.humanDescription.skeleton [i].scale != reference.humanDescription.skeleton [i].scale)
                            return false;
                    }
                }

                if (target.humanDescription.upperArmTwist != reference.humanDescription.upperArmTwist)
                    return false;
                if (target.humanDescription.upperLegTwist != reference.humanDescription.upperLegTwist)
                    return false;
            }

            if (target.importBlendShapes != reference.importBlendShapes)
                return false;
            if (target.isReadable != reference.isReadable)
                return false;
            if (target.materialName != reference.materialName)
                return false;
            if (target.materialSearch != reference.materialSearch)
                return false;
            if (target.normalSmoothingAngle != reference.normalSmoothingAngle)
                return false;
            if (target.optimizeMesh != reference.optimizeMesh)
                return false;

            if (target.secondaryUVAngleDistortion != reference.secondaryUVAngleDistortion)
                return false;
            if (target.secondaryUVAreaDistortion != reference.secondaryUVAreaDistortion)
                return false;
            if (target.secondaryUVHardAngle != reference.secondaryUVHardAngle)
                return false;
            if (target.secondaryUVPackMargin != reference.secondaryUVPackMargin)
                return false;
            if (target.sourceAvatar != reference.sourceAvatar)
                return false;
            if (target.swapUVChannels != reference.swapUVChannels)
                return false;
            if (target.importTangents != reference.importTangents)
                return false;

            #if UNITY_5_6 || UNITY_5_6_OR_NEWER
            if (target.keepQuads != reference.keepQuads)
                return false;
            if (target.weldVertices != reference.weldVertices)
                return false;
            #endif

            #if UNITY_2017_1_OR_NEWER
            if (target.importCameras != reference.importCameras)
                return false;
            if (target.importLights != reference.importLights)
                return false;
            if (target.normalCalculationMode != reference.normalCalculationMode)
                return false;
            if (target.importVisibility != reference.importVisibility)
                return false;
            if (target.useFileScale != reference.useFileScale)
                return false;

            if (target.extraUserProperties.Length != reference.extraUserProperties.Length)
                return false;
            for (int i = 0; i < target.extraUserProperties.Length; ++i) {
                if (target.extraUserProperties [i] != reference.extraUserProperties [i])
                    return false;
            }

            #else
            if (target.fileScale != reference.fileScale) return false;
            #endif

            #if UNITY_2017_2_OR_NEWER
            if (target.importAnimatedCustomProperties != reference.importAnimatedCustomProperties) {
                return false;
            }
            #endif

            return true;
        }

        private void OverwriteImportSettings (ModelImporter target, ModelImporter reference)
        {
            target.importMaterials = reference.importMaterials;
            target.importAnimation = reference.importAnimation;
            target.meshCompression = reference.meshCompression;
            target.importNormals = reference.importNormals;
            target.optimizeGameObjects = reference.optimizeGameObjects;
            target.motionNodeName = reference.motionNodeName;
            target.useFileUnits = reference.useFileUnits;

            target.addCollider = reference.addCollider;
            target.animationCompression = reference.animationCompression;
            target.animationPositionError = reference.animationPositionError;
            target.animationRotationError = reference.animationRotationError;
            target.animationScaleError = reference.animationScaleError;
            target.animationType = reference.animationType;
            target.animationWrapMode = reference.animationWrapMode;
            target.bakeIK = reference.bakeIK;
            target.clipAnimations = reference.clipAnimations;

            target.extraExposedTransformPaths = reference.extraExposedTransformPaths;
            target.generateAnimations = reference.generateAnimations;
            target.generateSecondaryUV = reference.generateSecondaryUV;
            target.globalScale = reference.globalScale;
            target.humanDescription = reference.humanDescription;
            target.importBlendShapes = reference.importBlendShapes;

            target.isReadable = reference.isReadable;
            target.materialName = reference.materialName;
            target.materialSearch = reference.materialSearch;

            target.normalSmoothingAngle = reference.normalSmoothingAngle;
            target.optimizeMesh = reference.optimizeMesh;
            target.secondaryUVAngleDistortion = reference.secondaryUVAngleDistortion;
            target.secondaryUVAreaDistortion = reference.secondaryUVAreaDistortion;
            target.secondaryUVHardAngle = reference.secondaryUVHardAngle;
            target.secondaryUVPackMargin = reference.secondaryUVPackMargin;
            target.sourceAvatar = reference.sourceAvatar;
            target.swapUVChannels = reference.swapUVChannels;

            target.importTangents = reference.importTangents;

            #if UNITY_5_6 || UNITY_5_6_OR_NEWER
            target.keepQuads = reference.keepQuads;
            target.weldVertices = reference.weldVertices;
            #endif

            #if UNITY_2017_1_OR_NEWER
            target.importCameras = reference.importCameras;
            target.importLights = reference.importLights;
            target.importVisibility = reference.importVisibility;
            target.normalCalculationMode = reference.normalCalculationMode;
            target.extraUserProperties = reference.extraUserProperties;
            target.useFileScale = reference.useFileScale;
            #endif

            #if UNITY_2017_2_OR_NEWER
            target.importAnimatedCustomProperties = reference.importAnimatedCustomProperties;
            #endif

            /* 
             read only properties.

            target.importedTakeInfos
            target.defaultClipAnimations
            target.isTangentImportSupported
            target.referencedClips
            target.fileScale
            target.isUseFileUnitsSupported
            target.motionNodeName
            target.isBakeIKSupported
            target.isFileScaleUsed
            target.transformPaths
            */

            /* Obsolete */
        }
    }
}