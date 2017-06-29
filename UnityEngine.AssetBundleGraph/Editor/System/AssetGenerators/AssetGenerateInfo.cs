using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {

	public class AssetGenerateInfo : ScriptableObject {

		[Serializable]
		class UsedAsset {
			public string importFrom;
			public string assetGuid;
			public string lastUpdated; // long is not supported by Text Serializer, so save it in string.

			public UsedAsset(string importFrom) {
				this.importFrom = importFrom;
				this.assetGuid = AssetDatabase.AssetPathToGUID(importFrom);
				this.lastUpdated = File.GetLastWriteTimeUtc(importFrom).ToFileTimeUtc().ToString();
			}

			public bool IsAssetModifiedFromLastTime {
				get {
					if(!File.Exists(importFrom)) {
						return true;
					}
					if(lastUpdated != File.GetLastWriteTimeUtc(importFrom).ToFileTimeUtc().ToString()) {
						return true;
					}
					if(assetGuid != AssetDatabase.AssetPathToGUID(importFrom)) {
						return true;
					}

					return false;
				}
			}
		}

        [SerializeField] private string m_generatorClass;
		[SerializeField] private string m_instanceData;
        [SerializeField] private string m_generatorVersion;
		[SerializeField] private UsedAsset m_usedAsset;

        public AssetGenerateInfo() {}

        public void Initialize(string className, string instanceData, string version, AssetReference asset) {
			m_generatorClass = className;
			m_instanceData = instanceData;
			m_generatorVersion = version;
            m_usedAsset = new UsedAsset (asset.importFrom);
		}

        static public bool DoesAssetNeedRegenerate(AssetGenerator.GeneratorEntry entry, Model.NodeData node, BuildTarget target, AssetReference asset) {
            var generateInfo = GetAssetGenerateInfo(entry, node, target, asset);

			// need rebuilding if no buildInfo found
			if(generateInfo == null) {
				return true;
			}

			// need rebuilding if given builder is changed
            if(generateInfo.m_generatorClass != entry.m_instance.ClassName) {
				return true;
			}

			// need rebuilding if given builder is changed
            if(generateInfo.m_instanceData != entry.m_instance[target]) {
				return true;
			}

            var version = AssetGeneratorUtility.GetVersion(entry.m_instance.ClassName);

			// need rebuilding if given builder version is changed
            if(generateInfo.m_generatorVersion != version) {
				return true;
			}

            if (generateInfo.m_usedAsset.importFrom != asset.importFrom) {
                return true;
            }

			// If asset is modified from last time, then need rebuilding
            if(generateInfo.m_usedAsset.IsAssetModifiedFromLastTime) {
                return true;
            }

			return false;
		}

        static private AssetGenerateInfo GetAssetGenerateInfo(AssetGenerator.GeneratorEntry entry, Model.NodeData node, BuildTarget target, AssetReference asset) {

            var cacheDir = FileUtility.EnsureAssetGeneratorCacheDirExists(target, node);
            var generatorInfoPath = FileUtility.PathCombine(cacheDir, entry.m_id + ".asset");

            return AssetDatabase.LoadAssetAtPath<AssetGenerateInfo>(generatorInfoPath);
        }

        static public void SaveAssetGenerateInfo(AssetGenerator.GeneratorEntry setting, Model.NodeData node, BuildTarget target, AssetReference asset) {

            var cacheDir = FileUtility.EnsureAssetGeneratorCacheDirExists(target, node);
            var generatorInfoPath = FileUtility.PathCombine(cacheDir, setting.m_id + ".asset");

            var version = PrefabBuilderUtility.GetPrefabBuilderVersion(setting.m_instance.ClassName);

            var info = ScriptableObject.CreateInstance<AssetGenerateInfo>();
            info.Initialize(setting.m_instance.ClassName, setting.m_instance[target], version, asset);

			AssetDatabase.CreateAsset(info, generatorInfoPath);		
		}
	}
}