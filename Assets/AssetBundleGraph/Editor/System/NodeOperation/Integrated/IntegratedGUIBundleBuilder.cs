using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace AssetBundleGraph {

	public class IntegratedGUIBundleBuilder : INodeOperation {

		private static readonly string key = "0";

		public void Setup (BuildTarget target, 
			NodeData node, 
			ConnectionPointData inputPoint,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<Asset>> inputGroupAssets, 
			List<string> alreadyCached, 
			Action<ConnectionData, Dictionary<string, List<Asset>>, List<string>> Output) 
		{

			var outputDict = new Dictionary<string, List<Asset>>();
			outputDict[key] = new List<Asset>();

			var bundleNames = inputGroupAssets.Keys.ToList();

			// validate if all assets configured with the same variant name
			foreach (var name in bundleNames) {
				var assets = inputGroupAssets[name];
				if( assets.Count > 0 ) {
					var variantName = assets[0].variantName;
					foreach(var a in assets) {
						if(a.variantName != variantName) {
							throw new NodeException("Different variant name found on asset bundle group "+name + ":Expected=" + variantName + " Found=" + a.variantName, node.Id); 
						}
					}
				} else {
					//skip bundle if there is no asset assigned
					Debug.LogWarning(node.Name + ":" + name + " has no asset assigned.");
				}
			}

			// add manifest file
			bundleNames.Add( SystemDataUtility.GetPathSafeTargetName(target) );
			var bundleOutputDir = FileUtility.EnsureAssetBundleCacheDirExists(target, node);

			foreach (var name in bundleNames) {
				Asset bundle = Asset.CreateAssetWithImportPath( FileUtility.PathCombine(bundleOutputDir, name) );
				Asset manifest = Asset.CreateAssetWithImportPath( FileUtility.PathCombine(bundleOutputDir, name + AssetBundleGraphSettings.MANIFEST_FOOTER) );
				outputDict[key].Add(bundle);
				outputDict[key].Add(manifest);
			}

			Output(connectionToOutput, outputDict, new List<string>());
		}
		
		public void Run (BuildTarget target, 
			NodeData node, 
			ConnectionPointData inputPoint,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<Asset>> inputGroupAssets, 
			List<string> alreadyCached, 
			Action<ConnectionData, Dictionary<string, List<Asset>>, List<string>> Output) 
		{
			
			var bundleOutputDir = FileUtility.EnsureAssetBundleCacheDirExists(target, node);
			
			AssetBundleBuild[] bundleBuild = new AssetBundleBuild[inputGroupAssets.Keys.Count];

			var bundleNames = inputGroupAssets.Keys.ToList();

			for(int i=0; i<bundleNames.Count; ++i) {
				var bundleName = bundleNames[i];
				var assets = inputGroupAssets[bundleName];

				if(assets.Count == 0) {
					Debug.LogWarning(node.Name + ":" + bundleName + " build skipped. No asset assigned to this asset bundle.");
					continue;
				}

				bundleBuild[i].assetBundleName = bundleName;
				bundleBuild[i].assetBundleVariant = assets[0].variantName;
				bundleBuild[i].assetNames = assets.Select(a => a.importFrom).ToArray();
			}


			BuildPipeline.BuildAssetBundles(bundleOutputDir, bundleBuild, (BuildAssetBundleOptions)node.BundleBuilderBundleOptions[target], target);


			var output = new Dictionary<string, List<Asset>>();
			output[key] = new List<Asset>();


			var generatedFiles = FileUtility.GetAllFilePathsInFolder(bundleOutputDir);
			// add manifest file
			bundleNames.Add( SystemDataUtility.GetPathSafeTargetName(target) );
			foreach (var path in generatedFiles) {
				var fileName = Path.GetFileName(path);
				if( IsFileIntendedItem(fileName, bundleNames) ) {
					output[key].Add( Asset.CreateAssetWithImportPath(path) );
				} else {
					Debug.LogWarning(node.Name + ":Irrelevant file found in assetbundle cache folder:" + fileName);
				}
			}

			Output(connectionToOutput, output, alreadyCached);
		}

		// Check if given file is generated Item
		private bool IsFileIntendedItem(string filename, List<string> bundleNames) {
			foreach(var name in bundleNames) {
				// related files always start from bundle names, as variants and manifests
				// are only appended on treail
				if( filename.IndexOf(name) == 0 ) {
					return true;
				}
			}
			return false;
		}
	}
}