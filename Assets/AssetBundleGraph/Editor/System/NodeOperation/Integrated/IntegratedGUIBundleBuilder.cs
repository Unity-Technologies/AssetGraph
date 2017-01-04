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
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			// BundleBuilder do nothing without incoming connections
			if(incoming == null) {
				return;
			}

			var outputDict = new Dictionary<string, List<AssetReference>>();
			outputDict[key] = new List<AssetReference>();

			var bundleNames = incoming.SelectMany(v => v.assetGroups.Keys).Distinct().ToList();
			var bundleVariants = new Dictionary<string, List<string>>();

			// get all variant name for bundles
			foreach(var ag in incoming) {
				foreach(var name in ag.assetGroups.Keys) {
					if(!bundleVariants.ContainsKey(name)) {
						bundleVariants[name] = new List<string>();
					}
					var assets = ag.assetGroups[name];
					foreach(var a in assets) {
						var variantName = a.variantName;
						if(!bundleVariants[name].Contains(variantName)) {
							bundleVariants[name].Add(variantName);
						}
					}
				}
			}

			// add manifest file
			var manifestName = BuildTargetUtility.TargetToAssetBundlePlatformName(target);
			bundleNames.Add( manifestName );
			bundleVariants[manifestName] = new List<string>() {""};

			var bundleOutputDir = FileUtility.EnsureAssetBundleCacheDirExists(target, node);

			foreach (var name in bundleNames) {
				foreach(var v in bundleVariants[name]) {
					string bundleName = (string.IsNullOrEmpty(v))? name : name + "." + v;
					AssetReference bundle = AssetReferenceDatabase.GetAssetBundleReference( FileUtility.PathCombine(bundleOutputDir, bundleName) );
					AssetReference manifest = AssetReferenceDatabase.GetAssetBundleReference( FileUtility.PathCombine(bundleOutputDir, bundleName + AssetBundleGraphSettings.MANIFEST_FOOTER) );
					outputDict[key].Add(bundle);
					outputDict[key].Add(manifest);
				}
			}

			var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
				null : connectionsToOutput.First();
			Output(dst, outputDict);
		}
		
		public void Run (BuildTarget target, 
			NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output,
			Action<NodeData, string, float> progressFunc) 
		{
			if(incoming == null) {
				return;
			}

			var aggregatedGroups = new Dictionary<string, List<AssetReference>>();
			aggregatedGroups[key] = new List<AssetReference>();

			if(progressFunc != null) progressFunc(node, "Collecting all inputs...", 0f);

			foreach(var ag in incoming) {
				foreach(var name in ag.assetGroups.Keys) {
					if(!aggregatedGroups.ContainsKey(name)) {
						aggregatedGroups[name] = new List<AssetReference>();
					}
					aggregatedGroups[name].AddRange(ag.assetGroups[name].AsEnumerable());
				}
			}

			var bundleOutputDir = FileUtility.EnsureAssetBundleCacheDirExists(target, node);

			var bundleNames = aggregatedGroups.Keys.ToList();
			var bundleVariants = new Dictionary<string, List<string>>();

			if(progressFunc != null) progressFunc(node, "Building bundle variants map...", 0.2f);

			// get all variant name for bundles
			foreach(var name in aggregatedGroups.Keys) {
				if(!bundleVariants.ContainsKey(name)) {
					bundleVariants[name] = new List<string>();
				}
				var assets = aggregatedGroups[name];
				foreach(var a in assets) {
					var variantName = a.variantName;
					if(!bundleVariants[name].Contains(variantName)) {
						bundleVariants[name].Add(variantName);
					}
				}
			}

			int validNames = 0;
			foreach (var name in bundleNames) {
				var assets = aggregatedGroups[name];
				// we do not build bundle without any asset
				if( assets.Count > 0 ) {
					validNames += bundleVariants[name].Count;
				}
			}

			AssetBundleBuild[] bundleBuild = new AssetBundleBuild[validNames];

			int bbIndex = 0;
			foreach(var name in bundleNames) {
				foreach(var v in bundleVariants[name]) {
					var assets = aggregatedGroups[name];

					if(assets.Count <= 0) {
						continue;
					}

					bundleBuild[bbIndex].assetBundleName = name;
					bundleBuild[bbIndex].assetBundleVariant = v;
					bundleBuild[bbIndex].assetNames = assets.Where(x => x.variantName == v).Select(x => x.importFrom).ToArray();
					++bbIndex;
				}
			}

			if(progressFunc != null) progressFunc(node, "Building Asset Bundles...", 0.7f);

			AssetBundleManifest m = BuildPipeline.BuildAssetBundles(bundleOutputDir, bundleBuild, (BuildAssetBundleOptions)node.BundleBuilderBundleOptions[target], target);

			var output = new Dictionary<string, List<AssetReference>>();
			output[key] = new List<AssetReference>();

			var generatedFiles = FileUtility.GetAllFilePathsInFolder(bundleOutputDir);
			// add manifest file
			bundleVariants.Add( BuildTargetUtility.TargetToAssetBundlePlatformName(target).ToLower(), new List<string> { null } );
			foreach (var path in generatedFiles) {
				var fileName = Path.GetFileName(path);
				if( IsFileIntendedItem(fileName, bundleVariants) ) {
					output[key].Add( AssetReferenceDatabase.GetAssetBundleReference(path) );
				}
			}

			var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
				null : connectionsToOutput.First();
			Output(dst, output);

			AssetBundleBuildReport.AddBuildReport(new AssetBundleBuildReport(node, m, bundleBuild, output[key], aggregatedGroups, bundleVariants));
		}

		// Check if given file is generated Item
		private bool IsFileIntendedItem(string filename, Dictionary<string, List<string>> bundleVariants) {
			filename = filename.ToLower();

			int lastDotManifestIndex = filename.LastIndexOf(".manifest");
			filename = (lastDotManifestIndex > 0)? filename.Substring(0, lastDotManifestIndex) : filename;

			int lastDotIndex = filename.LastIndexOf('.');
			var bundleNameFromFile  = (lastDotIndex > 0) ? filename.Substring(0, lastDotIndex): filename;
			var variantNameFromFile = (lastDotIndex > 0) ? filename.Substring(lastDotIndex+1): null;

			if(!bundleVariants.ContainsKey(bundleNameFromFile)) {
				return false;
			}

			var variants = bundleVariants[bundleNameFromFile];
			return variants.Contains(variantNameFromFile);
		}
	}
}