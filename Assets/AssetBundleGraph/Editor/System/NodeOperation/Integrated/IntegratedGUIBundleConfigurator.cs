using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AssetBundleGraph {
    public class IntegratedGUIBundleConfigurator : INodeOperation {

		public void Setup (BuildTarget target, 
			NodeData node, 
			ConnectionPointData inputPoint,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<Asset>> inputGroupAssets, 
			List<string> alreadyCached, 
			Action<ConnectionData, Dictionary<string, List<Asset>>, List<string>> Output) 
		{
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

			/*
			 * Check if incoming asset has valid import path
			 */ 
			var invalids = new List<Asset>();
			foreach (var groupKey in inputGroupAssets.Keys) {
				inputGroupAssets[groupKey].ForEach( a => { if (string.IsNullOrEmpty(a.importFrom)) invalids.Add(a); } );
			}
			if (invalids.Any()) {
				throw new NodeException(node.Name + 
					": Invalid files are found. Following files need to be imported to put into asset bundle: " + 
					string.Join(", ", invalids.Select(a =>a.absoluteAssetPath).ToArray()), node.Id );
			}

			var output = new Dictionary<string, List<Asset>>();

			var currentVariant = node.Variants.Find( v => v.ConnectionPoint == inputPoint );
			var variantName = (currentVariant == null) ? null : currentVariant.Name;

			// set configured assets in bundle name
			foreach (var groupKey in inputGroupAssets.Keys) {
				var bundleName = GetBundleName(target, node, groupKey, variantName);
				output[bundleName] = ConfigureAssetBundleSettings(variantName, inputGroupAssets[groupKey]);
			}
			
			Output(connectionToOutput, output, null);
		}
		
		public void Run (BuildTarget target, 
			NodeData node, 
			ConnectionPointData inputPoint,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<Asset>> inputGroupAssets, 
			List<string> alreadyCached, 
			Action<ConnectionData, Dictionary<string, List<Asset>>, List<string>> Output) 
		{
			var output = new Dictionary<string, List<Asset>>();

			var currentVariant = node.Variants.Find( v => v.ConnectionPoint == inputPoint );
			var variantName = (currentVariant == null) ? null : currentVariant.Name;

			// set configured assets in bundle name
			foreach (var groupKey in inputGroupAssets.Keys) {
				var bundleName = GetBundleName(target, node, groupKey, variantName);
				output[bundleName] = ConfigureAssetBundleSettings(variantName, inputGroupAssets[groupKey]);
			}

			Output(connectionToOutput, output, null);
		}

		public List<Asset> ConfigureAssetBundleSettings (string variantName, List<Asset> assets) {		

			List<Asset> configuredAssets = new List<Asset>();

			foreach(var a in assets) {
				configuredAssets.Add( Asset.DuplicateAssetWithVariant(a, variantName) );
			}

			return configuredAssets;
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

		public static string GetBundleName(BuildTarget target, NodeData node, string groupKey, string variantName) {
			var bundleName = node.BundleNameTemplate[target];

			bundleName = bundleName.Replace(AssetBundleGraphSettings.KEYWORD_WILDCARD.ToString(), groupKey);
			if(variantName != null) {
				bundleName = bundleName + "." + variantName;
			}

			return bundleName;
		}
	}
}