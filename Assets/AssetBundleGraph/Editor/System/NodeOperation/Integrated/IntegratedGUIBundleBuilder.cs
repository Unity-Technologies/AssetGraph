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

			var bundleVariants = new Dictionary<string, List<string>>();

			// get all variant name for bundles
			foreach (var name in bundleNames) {
				bundleVariants[name] = new List<string>();
				var assets = inputGroupAssets[name];
				foreach(var a in assets) {
					var variantName = a.variantName;
					if(!bundleVariants[name].Contains(variantName)) {
						bundleVariants[name].Add(variantName);
					}
				}
			}

			// add manifest file
			var manifestName = BuildTargetUtility.TargetToAssetBundlePlatformName(target);
			bundleNames.Add( manifestName );
			bundleVariants[manifestName] = new List<string>();

			var bundleOutputDir = FileUtility.EnsureAssetBundleCacheDirExists(target, node);

			foreach (var name in bundleNames) {
				foreach(var v in bundleVariants[name]) {
					string bundleName = (string.IsNullOrEmpty(v))? name : name + "." + v;
					Asset bundle = Asset.CreateAssetWithImportPath( FileUtility.PathCombine(bundleOutputDir, bundleName) );
					Asset manifest = Asset.CreateAssetWithImportPath( FileUtility.PathCombine(bundleOutputDir, bundleName + AssetBundleGraphSettings.MANIFEST_FOOTER) );
					outputDict[key].Add(bundle);
					outputDict[key].Add(manifest);
				}
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

			var bundleNames = inputGroupAssets.Keys.ToList();
			var bundleVariants = new Dictionary<string, List<string>>();

			// get all variant name for bundles
			foreach (var name in bundleNames) {
				bundleVariants[name] = new List<string>();
				var assets = inputGroupAssets[name];
				foreach(var a in assets) {
					var variantName = a.variantName;
					if(!bundleVariants[name].Contains(variantName)) {
						bundleVariants[name].Add(variantName);
					}
				}
			}

			int validNames = 0;
			foreach (var name in bundleNames) {
				var assets = inputGroupAssets[name];
				// we do not build bundle without any asset
				if( assets.Count > 0 ) {
					validNames += bundleVariants[name].Count;
				}
			}

			AssetBundleBuild[] bundleBuild = new AssetBundleBuild[validNames];

			int bbIndex = 0;
			foreach(var name in bundleNames) {
				foreach(var v in bundleVariants[name]) {
					var bundleName = name;
					var assets = inputGroupAssets[name];

					if(assets.Count <= 0) {
						continue;
					}

					bundleBuild[bbIndex].assetBundleName = bundleName;
					bundleBuild[bbIndex].assetBundleVariant = v;
					bundleBuild[bbIndex].assetNames = assets.Where(x => x.variantName == v).Select(x => x.importFrom).ToArray();
					++bbIndex;
				}
			}


			BuildPipeline.BuildAssetBundles(bundleOutputDir, bundleBuild, (BuildAssetBundleOptions)node.BundleBuilderBundleOptions[target], target);


			var output = new Dictionary<string, List<Asset>>();
			output[key] = new List<Asset>();

			var generatedFiles = FileUtility.GetAllFilePathsInFolder(bundleOutputDir);
			// add manifest file
			bundleNames.Add( BuildTargetUtility.TargetToAssetBundlePlatformName(target) );
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
			filename = filename.ToLower();
			foreach(var name in bundleNames) {
				var compName = name.ToLower();
				// bundle identifier may have "/"
				if(compName.IndexOf(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR) != -1) {
					var items = compName.Split(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR);
					compName  = items[items.Length-1];
				}
				// related files always start from bundle names, as variants and manifests
				// are only appended on treail
				if( filename.IndexOf(compName.ToLower()) == 0 ) {
					return true;
				}
			}
			return false;
		}
	}
}