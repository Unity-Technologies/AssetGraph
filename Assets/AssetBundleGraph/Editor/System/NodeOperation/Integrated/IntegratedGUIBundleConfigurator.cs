using UnityEditor;
using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AssetBundleGraph {
    public class IntegratedGUIBundleConfigurator : INodeOperation {

		public void Setup (BuildTarget target, 
			NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			int groupCount = 0;

			if(incoming != null) {
				var groupNames = new List<string>();
				foreach(var ag in incoming) {
					foreach (var groupKey in ag.assetGroups.Keys) {
						if(!groupNames.Contains(groupKey)) {
							groupNames.Add(groupKey);
						}
					}
				}
				groupCount = groupNames.Count;
			}

			ValidateBundleNameTemplate(
				node.BundleNameTemplate[target],
				node.BundleConfigUseGroupAsVariants,
				groupCount,
				() => {
					throw new NodeException(node.Name + ":Bundle Name Template is empty.", node.Id);
				},
				() => {
					throw new NodeException(node.Name + ":Bundle Name Template can not contain '" + AssetBundleGraphSettings.KEYWORD_WILDCARD.ToString() 
						+ "' when group name is used for variants.", node.Id);
				},
				() => {
					throw new NodeException(node.Name + ":Bundle Name Template must contain '" + AssetBundleGraphSettings.KEYWORD_WILDCARD.ToString() 
						+ "' when group name is not used for variants and expecting multiple incoming groups.", node.Id);
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


			if(incoming != null) {
				/**
				 * Check if incoming asset has valid import path
				 */
				var invalids = new List<AssetReference>();
				foreach(var ag in incoming) {
					foreach (var groupKey in ag.assetGroups.Keys) {
						ag.assetGroups[groupKey].ForEach( a => { if (string.IsNullOrEmpty(a.importFrom)) invalids.Add(a); } );
					}
				}
				if (invalids.Any()) {
					throw new NodeException(node.Name + 
						": Invalid files are found. Following files need to be imported to put into asset bundle: " + 
						string.Join(", ", invalids.Select(a =>a.absolutePath).ToArray()), node.Id );
				}
			}

			Dictionary<string, List<AssetReference>> output = null;
			if(Output != null) {
				output = new Dictionary<string, List<AssetReference>>();
			}

			if(incoming != null) {
				foreach(var ag in incoming) {
					string variantName = null;
					if(!node.BundleConfigUseGroupAsVariants) {
						var currentVariant = node.Variants.Find( v => v.ConnectionPointId == ag.connection.ToNodeConnectionPointId );
						variantName = (currentVariant == null) ? null : currentVariant.Name;
					}

					// set configured assets in bundle name
					foreach (var groupKey in ag.assetGroups.Keys) {
						if(node.BundleConfigUseGroupAsVariants) {
							variantName = groupKey;
						}
						var bundleName = GetBundleName(target, node, groupKey);
						var assets = ag.assetGroups[groupKey];
						ConfigureAssetBundleSettings(variantName, assets);
						if(output != null) {
							if(!output.ContainsKey(bundleName)) {
								output[bundleName] = new List<AssetReference>();
							} 
							output[bundleName].AddRange(assets);
						}
					}
				}
			}

			if(Output != null) {
				var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
					null : connectionsToOutput.First();
				Output(dst, output);
			}
		}
		
		public void Run (BuildTarget target, 
			NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output,
			Action<NodeData, string, float> progressFunc) 
		{
			Dictionary<string, List<AssetReference>> output = null;
			if(Output != null) {
				output = new Dictionary<string, List<AssetReference>>();
			}

			if(incoming != null) {
				foreach(var ag in incoming) {
					string variantName = null;
					if(!node.BundleConfigUseGroupAsVariants) {
						var currentVariant = node.Variants.Find( v => v.ConnectionPointId == ag.connection.ToNodeConnectionPointId );
						variantName = (currentVariant == null) ? null : currentVariant.Name;
					}

					// set configured assets in bundle name
					foreach (var groupKey in ag.assetGroups.Keys) {
						if(node.BundleConfigUseGroupAsVariants) {
							variantName = groupKey;
						}
						var bundleName = GetBundleName(target, node, groupKey);

						if(progressFunc != null) progressFunc(node, string.Format("Configuring {0}", bundleName), 0.5f);

						var assets = ag.assetGroups[groupKey];
						ConfigureAssetBundleSettings(variantName, assets);
						if(output != null) {
							if(!output.ContainsKey(bundleName)) {
								output[bundleName] = new List<AssetReference>();
							} 
							output[bundleName].AddRange(assets);
						}
					}
				}
			}

			if(Output != null) {
				var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
					null : connectionsToOutput.First();
				Output(dst, output);
			}
		}

		public void ConfigureAssetBundleSettings (string variantName, List<AssetReference> assets) {		

			foreach(var a in assets) {
				a.variantName = (string.IsNullOrEmpty(variantName))? null : variantName;
			}
		}

		public static void ValidateBundleNameTemplate (string bundleNameTemplate, bool useGroupAsVariants, int groupCount,
			Action NullOrEmpty, 
			Action InvalidBundleNameTemplateForVariants, 
			Action InvalidBundleNameTemplateForNotVariants
		) {
			if (string.IsNullOrEmpty(bundleNameTemplate)){
				NullOrEmpty();
			}
			if(useGroupAsVariants && bundleNameTemplate.IndexOf(AssetBundleGraphSettings.KEYWORD_WILDCARD) >= 0) {
				InvalidBundleNameTemplateForVariants();
			}
			if(!useGroupAsVariants && bundleNameTemplate.IndexOf(AssetBundleGraphSettings.KEYWORD_WILDCARD) < 0 &&
				groupCount > 1) {
				InvalidBundleNameTemplateForNotVariants();
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

		public static string GetBundleName(BuildTarget target, NodeData node, string groupKey) {
			var bundleName = node.BundleNameTemplate[target];

			if(node.BundleConfigUseGroupAsVariants) {
				return bundleName.ToLower();
			} else {
				return bundleName.Replace(AssetBundleGraphSettings.KEYWORD_WILDCARD.ToString(), groupKey).ToLower();
			}
		}
	}
}