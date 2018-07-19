using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	public class PrefabBuildInfo : ScriptableObject {

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

		[SerializeField] private string m_groupKey;
		[SerializeField] private string m_builderClass;
		[SerializeField] private string m_instanceData;
		[SerializeField] private string m_prefabBuilderVersion;
		[SerializeField] private int m_replacePrefabOptions = (int)UnityEditor.ReplacePrefabOptions.Default;
		[SerializeField] private List<UsedAsset> m_usedAssets;
        [SerializeField] private string m_buildDir;

		public PrefabBuildInfo() {}

		public void Initialize(string buildDir, string groupKey, string className, string instanceData, string version, ReplacePrefabOptions opt, List<AssetReference> assets) {
			m_groupKey = groupKey;
			m_builderClass = className;
			m_instanceData = instanceData;
			m_prefabBuilderVersion = version;
			m_replacePrefabOptions = (int)opt;
            m_buildDir = buildDir;

			m_usedAssets = new List<UsedAsset> ();
			assets.ForEach(a => m_usedAssets.Add(new UsedAsset(a.importFrom)));
		}

		static private PrefabBuildInfo GetPrefabBuildInfo(PrefabBuilder builder, Model.NodeData node, BuildTarget target, string groupKey) {

            var prefabCacheDir = FileUtility.EnsureCacheDirExists(target, node, PrefabBuilder.kCacheDirName);
			var buildInfoPath = FileUtility.PathCombine(prefabCacheDir, groupKey + ".asset");

			return AssetDatabase.LoadAssetAtPath<PrefabBuildInfo>(buildInfoPath);
		}

		static public bool DoesPrefabNeedRebuilding(string buildPath, PrefabBuilder builder, Model.NodeData node, BuildTarget target, string groupKey, List<AssetReference> assets) {
			var buildInfo = GetPrefabBuildInfo(builder, node, target, groupKey);

			// need rebuilding if no buildInfo found
			if(buildInfo == null) {
				return true;
			}

            // need rebuilding if build path is changed
            if(buildInfo.m_buildDir != buildPath) {
                return true;
            }

            // need rebuilding if given builder is changed
			if(buildInfo.m_builderClass != builder.Builder.ClassName) {
				return true;
			}

			// need rebuilding if given builder is changed
			if(buildInfo.m_instanceData != builder.Builder[target]) {
				return true;
			}

			// need rebuilding if replace prefab option is changed
			if(buildInfo.m_replacePrefabOptions != (int)builder.Options) {
				return true;
			}

			var builderVersion = PrefabBuilderUtility.GetPrefabBuilderVersion(builder.Builder.ClassName);

			// need rebuilding if given builder version is changed
			if(buildInfo.m_prefabBuilderVersion != builderVersion) {
				return true;
			}

			// need rebuilding if given groupKey changed
			if(buildInfo.m_groupKey != groupKey) {
				return true;
			}

			if(!Enumerable.SequenceEqual( 
				buildInfo.m_usedAssets.Select(v=>v.importFrom).OrderBy(s=>s), 
				assets.Select(v=>v.importFrom).OrderBy(s=>s))) 
			{
				return true;
			}

			// If any asset is modified from last time, then need rebuilding
			foreach(var usedAsset in buildInfo.m_usedAssets) {
				if(usedAsset.IsAssetModifiedFromLastTime) {
					return true;
				}
			}

			return false;
		}

		static public void SavePrefabBuildInfo(string buildPath, PrefabBuilder builder, Model.NodeData node, BuildTarget target, string groupKey, List<AssetReference> assets) {

            var prefabCacheDir = FileUtility.EnsureCacheDirExists(target, node, PrefabBuilder.kCacheDirName);
			var buildInfoPath = FileUtility.PathCombine(prefabCacheDir, groupKey + ".asset");

			var version = PrefabBuilderUtility.GetPrefabBuilderVersion(builder.Builder.ClassName);

			var buildInfo = ScriptableObject.CreateInstance<PrefabBuildInfo>();
            buildInfo.Initialize(buildPath, groupKey, builder.Builder.ClassName, builder.Builder[target], version, builder.Options, assets);

			AssetDatabase.CreateAsset(buildInfo, buildInfoPath);		
		}
	}
}