using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {

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
		[SerializeField] private PrefabBuilderInstance m_builder;
		[SerializeField] private string m_prefabBuilderVersion;
		[SerializeField] private int m_replacePrefabOptions = (int)UnityEditor.ReplacePrefabOptions.Default;
		[SerializeField] private List<UsedAssets> m_usedAssets;

		public PrefabBuildInfo() {}

		public void Initialize(string groupKey, PrefabBuilderInstance builder, string version, ReplacePrefabOptions opt, List<AssetReference> assets) {
			m_groupKey = groupKey;
			m_builder = builder;
			m_prefabBuilderVersion = version;
			m_replacePrefabOptions = (int)opt;

			m_usedAssets = new List<UsedAssets> ();
			assets.ForEach(a => m_usedAssets.Add(new UsedAssets(a.importFrom)));
		}

		static private PrefabBuildInfo GetPrefabBuildInfo(PrefabBuilder builder, Model.NodeData node, BuildTarget target, string groupKey) {

			var prefabCacheDir = FileUtility.EnsurePrefabBuilderCacheDirExists(target, node);
			var buildInfoPath = FileUtility.PathCombine(prefabCacheDir, groupKey + ".asset");

			return AssetDatabase.LoadAssetAtPath<PrefabBuildInfo>(buildInfoPath);
		}

		static public bool DoesPrefabNeedRebuilding(PrefabBuilder builder, Model.NodeData node, BuildTarget target, string groupKey, List<AssetReference> assets) {
			var buildInfo = GetPrefabBuildInfo(builder, node, target, groupKey);

			// need rebuilding if no buildInfo found
			if(buildInfo == null) {
				return true;
			}

			// need rebuilding if given builder is changed
			if(buildInfo.m_builder != builder.Builder[target]) {
				return true;
			}

			// need rebuilding if replace prefab option is changed
			if(buildInfo.m_replacePrefabOptions != (int)builder.Options) {
				return true;
			}

			var builderVersion = PrefabBuilderUtility.GetPrefabBuilderVersion(builder.Builder[target].ClassName);

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

		static public void SavePrefabBuildInfo(PrefabBuilder builder, Model.NodeData node, BuildTarget target, string groupKey, List<AssetReference> assets) {

			var prefabCacheDir = FileUtility.EnsurePrefabBuilderCacheDirExists(target, node);
			var buildInfoPath = FileUtility.PathCombine(prefabCacheDir, groupKey + ".asset");

			var version = PrefabBuilderUtility.GetPrefabBuilderVersion(builder.Builder[target].ClassName);

			var buildInfo = ScriptableObject.CreateInstance<PrefabBuildInfo>();
			buildInfo.Initialize(groupKey, builder.Builder[target], version, builder.Options, assets);

			AssetDatabase.CreateAsset(buildInfo, buildInfoPath);		
		}
	}
}