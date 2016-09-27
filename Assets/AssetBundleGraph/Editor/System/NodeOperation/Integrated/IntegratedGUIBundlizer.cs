using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AssetBundleGraph {
    public class IntegratedGUIBundlizer : INodeOperationBase {

		private readonly ConnectionData assetOutputConnection;

		public IntegratedGUIBundlizer(ConnectionData assetOutputConnection) {
			this.assetOutputConnection = assetOutputConnection;
		}

		public void Setup (BuildTarget target, NodeData node, string unused_connectionIdToNextNode, Dictionary<string, List<Asset>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {			

			try {
				ValidateBundleNameTemplate(
					node.BundleNameTemplate[target],
					() => {
						throw new NodeException(node.Name + ":Bundle Name Template is empty.", node.Id);
					}
				);

				var variantNames = node.Variants.Select(v=>v.Name).ToList();
				foreach(var variant in node.Variants) {
					ValidateVariantName(variant.Name, variantNames, 
						() => {
							throw new NodeException(node.Name + ":Variant name is empty.", node.Id);
						},
						() => {
							throw new NodeException(node.Name + ":Variant name cannot contain whitespace \"" + variant.Name + "\".", node.Id);
						},
						() => {
							throw new NodeException(node.Name + ":Variant name already exists \"" + variant.Name + "\".", node.Id);
						});
				}

			} catch (NodeException e) {
				AssetBundleGraphEditorWindow.AddNodeException(e);
				return;
			}
			

			var outputDict = new Dictionary<string, List<Asset>>();

			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];
				
				var bundleName = BundlizeAssets(target, node, groupKey, inputSources, false);
				var newAssetData = Asset.CreateAssetWithImportPath(bundleName);
				outputDict[groupKey] = new List<Asset>(){ newAssetData };
			}
			
			if (assetOutputConnection != null) {
				Output(node.Id, assetOutputConnection.Id, outputDict, new List<string>());
			}
			
		}
		
		public void Run (BuildTarget target, NodeData node, string unused_connectionIdToNextNode, Dictionary<string, List<Asset>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {
			ValidateBundleNameTemplate(
				node.BundleNameTemplate[target],
				() => {
					throw new AssetBundleGraphBuildException(node.Name + ": Bundle Name Template is empty.");
				}
			);

			var outputDict = new Dictionary<string, List<Asset>>();

			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];
				
				var bundleName = BundlizeAssets(target, node, groupKey, inputSources, true);
				var newAssetData = Asset.CreateAssetWithImportPath(bundleName);

				outputDict[groupKey] = new List<Asset>(){ newAssetData };
			}
			
			if (assetOutputConnection != null) {
				Output(node.Id, assetOutputConnection.Id, outputDict, new List<string>());
			}
			
		}

		public string BundlizeAssets (BuildTarget target, NodeData node, string groupkey, List<Asset> sources, bool isRun) {		
			var invalids = new List<string>();
			foreach (var source in sources) {
				if (string.IsNullOrEmpty(source.importFrom)) {
					invalids.Add(source.absoluteAssetPath);
				}
			}
			if (invalids.Any()) {
				throw new AssetBundleGraphBuildException(node.Name + ": Invalid files to bundle. Following files need to be imported before bundlize: " + string.Join(", ", invalids.ToArray()) );
			}

			var bundleName = node.BundleNameTemplate[target];

			/*
				if contains KEYWORD_WILDCARD, use group identifier to bundlize name.
			*/
			if (bundleName.Contains(AssetBundleGraphSettings.KEYWORD_WILDCARD)) {
				var templateHead = bundleName.Split(AssetBundleGraphSettings.KEYWORD_WILDCARD)[0];
				var templateTail = bundleName.Split(AssetBundleGraphSettings.KEYWORD_WILDCARD)[1];

				bundleName = (templateHead + groupkey + templateTail + "." + SystemDataUtility.GetPathSafeTargetName(target)).ToLower();
			}
			
			for (var i = 0; i < sources.Count; i++) {
				var source = sources[i];

				// if already bundled in this running, avoid changing that name.
				if (source.isBundled) {
					continue;
				}
				
				if (isRun) {
					if (FileUtility.IsMetaFile(source.importFrom)) continue;	
					var assetImporter = AssetImporter.GetAtPath(source.importFrom);
					if (assetImporter == null) continue; 
					assetImporter.assetBundleName = bundleName;
				}
				
				// set as this resource is already bundled.
				sources[i] = Asset.DuplicateAssetWithNewStatus(sources[i], sources[i].isNew, true);
			}

			return bundleName;
		}

		public static void ValidateBundleNameTemplate (string bundleNameTemplate, Action NullOrEmpty) {
			if (string.IsNullOrEmpty(bundleNameTemplate)){
				NullOrEmpty();
			}
		}

		public static void ValidateVariantName (string variantName, List<string> names, Action NullOrEmpty, Action ContainsSpace, Action NameAlreadyExists) {
			if (string.IsNullOrEmpty(variantName)) {
				NullOrEmpty();
			}
			if(Regex.IsMatch(variantName, "\\s")) {
				ContainsSpace();
			}
			var overlappings = names.GroupBy(x => x)
				.Where(group => 1 < group.Count())
				.Select(group => group.Key)
				.ToList();

			if (overlappings.Any()) {
				NameAlreadyExists();
			}
		}
	}
}