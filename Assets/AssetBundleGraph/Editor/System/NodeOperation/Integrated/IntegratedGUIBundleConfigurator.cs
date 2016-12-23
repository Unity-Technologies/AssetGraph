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
			ConnectionData connectionFromInput,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<AssetReference>> inputGroupAssets, 
			PerformGraph.Output Output) 
		{
			Profiler.BeginSample("AssetBundleGraph.GUIBundleConfigurator.Setup");
			ValidateBundleNameTemplate(
				node.BundleNameTemplate[target],
				node.BundleConfigUseGroupAsVariants,
				() => {
					throw new NodeException(node.Name + ":Bundle Name Template is empty.", node.Id);
				},
				() => {
					throw new NodeException(node.Name + ":Bundle Name Template can not contain '" + AssetBundleGraphSettings.KEYWORD_WILDCARD.ToString() 
						+ "' when group name is used for variants.", node.Id);
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

			/*
			 * Check if incoming asset has valid import path
			 */ 
			var invalids = new List<AssetReference>();
			foreach (var groupKey in inputGroupAssets.Keys) {
				inputGroupAssets[groupKey].ForEach( a => { if (string.IsNullOrEmpty(a.importFrom)) invalids.Add(a); } );
			}
			if (invalids.Any()) {
				throw new NodeException(node.Name + 
					": Invalid files are found. Following files need to be imported to put into asset bundle: " + 
					string.Join(", ", invalids.Select(a =>a.absolutePath).ToArray()), node.Id );
			}

			var output = new Dictionary<string, List<AssetReference>>();

			string variantName = null;
			if(!node.BundleConfigUseGroupAsVariants) {
				var currentVariant = node.Variants.Find( v => v.ConnectionPoint.Id == connectionFromInput.ToNodeConnectionPointId );
				variantName = (currentVariant == null) ? null : currentVariant.Name;
			}


			// set configured assets in bundle name
			foreach (var groupKey in inputGroupAssets.Keys) {
				if(node.BundleConfigUseGroupAsVariants) {
					variantName = groupKey;
				}
				var bundleName = GetBundleName(target, node, groupKey);
				var newBundleSetting = ConfigureAssetBundleSettings(variantName, inputGroupAssets[groupKey]);
				if(output.ContainsKey(bundleName)) {
					output[bundleName].AddRange(newBundleSetting);
				} else {
					output[bundleName] = newBundleSetting;
				}
			}
			
			Output(output);
			Profiler.EndSample();
		}
		
		public void Run (BuildTarget target, 
			NodeData node, 
			ConnectionData connectionFromInput,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<AssetReference>> inputGroupAssets, 
			PerformGraph.Output Output) 
		{
			Profiler.BeginSample("AssetBundleGraph.GUIBundleConfigurator.Run");

			var output = new Dictionary<string, List<AssetReference>>();

			string variantName = null;
			if(!node.BundleConfigUseGroupAsVariants) {
				var currentVariant = node.Variants.Find( v => v.ConnectionPoint.Id == connectionFromInput.ToNodeConnectionPointId );
				variantName = (currentVariant == null) ? null : currentVariant.Name;
			}

			// set configured assets in bundle name
			foreach (var groupKey in inputGroupAssets.Keys) {
				if(node.BundleConfigUseGroupAsVariants) {
					variantName = groupKey;
				}
				var bundleName = GetBundleName(target, node, groupKey);

				var newBundleSetting = ConfigureAssetBundleSettings(variantName, inputGroupAssets[groupKey]);
				if(output.ContainsKey(bundleName)) {
					output[bundleName].AddRange(newBundleSetting);
				} else {
					output[bundleName] = newBundleSetting;
				}
			}

			Output(output);
			Profiler.EndSample();
		}

		public List<AssetReference> ConfigureAssetBundleSettings (string variantName, List<AssetReference> assets) {		

			List<AssetReference> configuredAssets = new List<AssetReference>();

			foreach(var a in assets) {
				var lowerName = (string.IsNullOrEmpty(variantName))? variantName : variantName.ToLower();
				a.variantName = lowerName;
				configuredAssets.Add( a );
			}

			return configuredAssets;
		}

		public static void ValidateBundleNameTemplate (string bundleNameTemplate, bool useGroupAsVariants, Action NullOrEmpty, Action InvalidBundleNameTemplate) {
			if (string.IsNullOrEmpty(bundleNameTemplate)){
				NullOrEmpty();
			}
			if(useGroupAsVariants && bundleNameTemplate.IndexOf(AssetBundleGraphSettings.KEYWORD_WILDCARD) >= 0) {
				InvalidBundleNameTemplate();
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
				return bundleName;
			} else {
				return bundleName.Replace(AssetBundleGraphSettings.KEYWORD_WILDCARD.ToString(), groupKey);
			}
		}
	}
}