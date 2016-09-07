using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;

/**
	static executor for AssetBundleGraph's data.
*/
namespace AssetBundleGraph {
	public class SystemDataUtility {

		/**
			Return true if given asset is already Cached
		*/
		public static bool IsCached (Asset relatedAsset, List<string> alreadyCachedPath, string localAssetPath) {
			if (alreadyCachedPath.Contains(localAssetPath)) {
				// if source is exists, check hash.
				var sourceHash = GetHash(relatedAsset.absoluteAssetPath);
				var destHash = GetHash(localAssetPath);

				// completely hit.
				if (sourceHash.SequenceEqual(destHash)) {
					return true;
				}
			}

			return false;
		}

		/**
			Return true if all given assets are cached in most updated manner
		*/
		public static bool IsAllAssetsCachedAndUpdated (List<Asset> relatedAssets, List<string> alreadyCachedPath, string localAssetPath) {
			// check prefab-out file is exist or not.
			if (alreadyCachedPath.Contains(localAssetPath)) {
				
				// cached. check if 
				var changed = false;
				foreach (var relatedAsset in relatedAssets) {
					if (relatedAsset.isNew) {
						changed = true;
						break;
					}
				}
				
				if (changed) return false;
				return true;
			}
			return false;
		}

		public static byte[] GetHash (string filePath) {
			using (var md5 = MD5.Create()) {
				using (var stream = File.OpenRead(filePath)) {
					return md5.ComputeHash(stream);
				}
			}
		}

		public static List<string> CreateCustomFilterInstanceForScript (string scriptClassName) {
			var nodeScriptInstance = Assembly.LoadFile("Library/ScriptAssemblies/Assembly-CSharp-Editor.dll").CreateInstance(scriptClassName);
			if (nodeScriptInstance == null) {
				Debug.LogError("Failed to create instance for " + scriptClassName + ". No such class found in assembly.");
				return new List<string>();
			}

			var labels = new List<string>();
			Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output = (string dataSourceNodeId, string connectionLabel, Dictionary<string, List<Asset>> source, List<string> usedCache) => {
				labels.Add(connectionLabel);
			};

			((FilterBase)nodeScriptInstance).Setup(
				BuildTarget.StandaloneWindows64,
				new NodeData(),
				string.Empty,
				new Dictionary<string, List<Asset>>{
					{"0", new List<Asset>()}
				},
				new List<string>(),
				Output
			);
			return labels;

		}

		public static T CreateNodeOperationInstance<T> (string typeStr, NodeData node) where T : INodeOperationBase {
			var nodeScriptInstance = Assembly.LoadFile("Library/ScriptAssemblies/Assembly-CSharp-Editor.dll").CreateInstance(typeStr);
			if (nodeScriptInstance == null) {
				throw new NodeException("failed to generate class information of class:" + typeStr + " which is based on Type:" + typeof(T), node.Id);
			}
			return ((T)nodeScriptInstance);
		}

		public static string GetPathSafeDefaultTargetName () {
			return GetPathSafeTargetGroupName(BuildTargetUtility.DefaultTarget);
		}

		public static string GetPathSafeTargetName (BuildTarget t) {
			return t.ToString().Replace(" ", "_");
		}

		public static string GetPathSafeTargetGroupName (BuildTargetGroup g) {
			return g.ToString().Replace(" ", "_");
		}

		public static string GetProjectName () {
			var assetsPath = Application.dataPath;
			var projectFolderNameArray = assetsPath.Split(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR);
			var projectFolderName = projectFolderNameArray[projectFolderNameArray.Length - 2] + AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR;
			return projectFolderName;
		}

	}
}

