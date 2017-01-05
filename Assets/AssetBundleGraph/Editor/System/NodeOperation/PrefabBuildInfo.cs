using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;


namespace AssetBundleGraph {

	public class PrefabBuildInfo : ScriptableObject {

		[Serializable]
		class UsedAssets {
			public string importFrom;
			public string assetGuid;
			public string lastUpdated; // long is not supported by Text Serializer, so save it in string.

			public UsedAssets(string importFrom) {
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
		[SerializeField] private string m_prefabBuilderClass;
		[SerializeField] private string m_prefabBuilderData;
		[SerializeField] private string m_prefabBuilderVersion;
		[SerializeField] private int m_replacePrefabOptions = (int)UnityEditor.ReplacePrefabOptions.Default;
		[SerializeField] private List<UsedAssets> m_usedAssets;

		public PrefabBuildInfo() {}

		public void Initialize(string groupKey, string builderClass, string builderData, string version, ReplacePrefabOptions opt, List<AssetReference> assets) {
			m_groupKey = groupKey;
			m_prefabBuilderClass = builderClass;
			m_prefabBuilderData = builderData;
			m_prefabBuilderVersion = version;
			m_replacePrefabOptions = (int)opt;

			m_usedAssets = new List<UsedAssets> ();
			assets.ForEach(a => m_usedAssets.Add(new UsedAssets(a.importFrom)));
		}

		static private PrefabBuildInfo GetPrefabBuildInfo(NodeData node, BuildTarget target, string groupKey) {

			var prefabCacheDir = FileUtility.EnsurePrefabBuilderCacheDirExists(target, node);
			var buildInfoPath = FileUtility.PathCombine(prefabCacheDir, groupKey + ".asset");

			return AssetDatabase.LoadAssetAtPath<PrefabBuildInfo>(buildInfoPath);
		}

		static public bool DoesPrefabNeedRebuilding(NodeData node, BuildTarget target, string groupKey, List<AssetReference> assets) {
			var buildInfo = GetPrefabBuildInfo(node, target, groupKey);

			// need rebuilding if no buildInfo found
			if(buildInfo == null) {
				return true;
			}

			// need rebuilding if given builder is changed
			if(buildInfo.m_prefabBuilderClass != node.ScriptClassName) {
				return true;
			}

			// need rebuilding if given builder is changed
			if(buildInfo.m_prefabBuilderData != node.InstanceData[target]) {
				return true;
			}

			// need rebuilding if replace prefab option is changed
			if(buildInfo.m_replacePrefabOptions != (int)node.ReplacePrefabOptions) {
				return true;
			}

			var builderVersion = PrefabBuilderUtility.GetPrefabBuilderVersion(node.ScriptClassName);

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

		static public void SavePrefabBuildInfo(NodeData node, BuildTarget target, string groupKey, List<AssetReference> assets) {

			var prefabCacheDir = FileUtility.EnsurePrefabBuilderCacheDirExists(target, node);
			var buildInfoPath = FileUtility.PathCombine(prefabCacheDir, groupKey + ".asset");

			var version = PrefabBuilderUtility.GetPrefabBuilderVersion(node.ScriptClassName);

			var buildInfo = ScriptableObject.CreateInstance<PrefabBuildInfo>();
			buildInfo.Initialize(groupKey, node.ScriptClassName, node.InstanceData[target], version, node.ReplacePrefabOptions, assets);

			AssetDatabase.CreateAsset(buildInfo, buildInfoPath);		
		}
	}
}