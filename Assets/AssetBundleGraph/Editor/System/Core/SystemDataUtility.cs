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
				"GetLabelsFromSetupFilter_dummy_nodeName",
				"GetLabelsFromSetupFilter_dummy_nodeId", 
				string.Empty,
				new Dictionary<string, List<Asset>>{
					{"0", new List<Asset>()}
				},
				new List<string>(),
				Output
			);
			return labels;

		}

		public static string GetPlatformValue (Dictionary<string, string> packageDict, string platform) {
			var key = CreateKeyNameFromString(platform);
			if (packageDict.ContainsKey(key)) {
				return packageDict[key];
			}

			if (packageDict.ContainsKey(AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME)) {
				return packageDict[AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME];
			}

			throw new AssetBundleGraphException("Default setting not found.");
		}

		public static List<string> GetPlatformValue (Dictionary<string, List<string>> packageDict, string platform) {
			var key = CreateKeyNameFromString(platform);
			if (packageDict.ContainsKey(key)) {
				return packageDict[key];
			}

			if (packageDict.ContainsKey(AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME)) {
				return packageDict[AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME];
			}

			throw new AssetBundleGraphException("Default setting not found.");
		}

		public static List<string> GetCurrentPlatformValue (Dictionary<string, List<string>> packageDict) {
			var platformPackageKeyCandidate = GetCurrentPlatformKey();
			
			if (packageDict.ContainsKey(platformPackageKeyCandidate)) {
				return packageDict[platformPackageKeyCandidate];
			}
			
			if (packageDict.ContainsKey(AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME)) {
				return packageDict[AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME];
			}

			throw new AssetBundleGraphException("Default setting not found.");
		}

		public static string GetCurrentPlatformValue (Dictionary<string, string> packageDict) {
			var platformPackageKeyCandidate = GetCurrentPlatformKey();
			/*
				check best match for platform + pacakge.
			*/
			if (packageDict.ContainsKey(platformPackageKeyCandidate)) {
				return packageDict[platformPackageKeyCandidate];
			}
			
			/*
				check next match for defaultPlatform + package.
			*/
			var defaultPlatformAndCurrentPackageCandidate = GetDefaultPlatformKey();
			if (packageDict.ContainsKey(defaultPlatformAndCurrentPackageCandidate)) {
				return packageDict[defaultPlatformAndCurrentPackageCandidate];
			}

			/*
				check default platform.
			*/
			if (packageDict.ContainsKey(AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME)) {
				return packageDict[AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME];
			}
			
			throw new AssetBundleGraphException("Default setting not found.");
		}

		public static string GetCurrentPlatformShortName () {
			var currentPlatformCandidate = EditorUserBuildSettings.activeBuildTarget.ToString();
			return currentPlatformCandidate;
		}

		public static string GetCurrentPlatformKey () {
			var currentPlatformCandidate = GetCurrentPlatformShortName();

			return CreateKeyNameFromString(currentPlatformCandidate);
		}

		public static string GetDefaultPlatformKey () {
			return CreateKeyNameFromString(AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME);
		}

		public static string CreateKeyNameFromString (string keyname) {
			return keyname.Replace(" ", "_");
		}

		public static string GetProjectName () {
			var assetsPath = Application.dataPath;
			var projectFolderNameArray = assetsPath.Split(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR);
			var projectFolderName = projectFolderNameArray[projectFolderNameArray.Length - 2] + AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR;
			return projectFolderName;
		}

	}
}
