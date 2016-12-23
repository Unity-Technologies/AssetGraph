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
		public static bool IsCached (AssetReference relatedAsset, List<string> alreadyCachedPath, string localAssetPath) {
			if (alreadyCachedPath.Contains(localAssetPath)) {
				// if source is exists, check hash.
				var sourceHash = GetHash(relatedAsset.absolutePath);
				var destHash = GetHash(localAssetPath);

				// completely hit.
				if (sourceHash.SequenceEqual(destHash)) {
					return true;
				}
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

		public static T CreateNodeOperationInstance<T> (string typeStr, NodeData node) where T : INodeOperation {
			var nodeScriptInstance = Assembly.LoadFile("Library/ScriptAssemblies/Assembly-CSharp-Editor.dll").CreateInstance(typeStr);
			if (nodeScriptInstance == null) {
				throw new NodeException(node.Name + ": Failed to create instance:" + typeStr + " derived from:" + typeof(T), node.Id);
			}
			return ((T)nodeScriptInstance);
		}

		public static string GetPathSafeDefaultTargetName () {
			return GetPathSafeTargetGroupName(BuildTargetUtility.DefaultTarget);
		}

		public static string GetPathSafeTargetName (BuildTarget t) {
			return t.ToString();
		}

		public static string GetPathSafeTargetGroupName (BuildTargetGroup g) {
			return g.ToString();
		}

		public static string GetProjectName () {
			var assetsPath = Application.dataPath;
			var projectFolderNameArray = assetsPath.Split(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR);
			var projectFolderName = projectFolderNameArray[projectFolderNameArray.Length - 2] + AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR;
			return projectFolderName;
		}

	}
}

