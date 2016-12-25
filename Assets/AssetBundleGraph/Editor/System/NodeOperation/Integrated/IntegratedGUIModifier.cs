using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace AssetBundleGraph {
    public class IntegratedGUIModifier : INodeOperation {

		public void Setup (BuildTarget target, 
			NodeData node, 
			ConnectionData connectionFromInput,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<AssetReference>> inputGroupAssets, 
			PerformGraph.Output Output) 
		{
			Profiler.BeginSample("AssetBundleGraph.GUIModifier.Setup");
			var incomingAssets = inputGroupAssets.SelectMany(v => v.Value).ToList();

			ValidateModifier(node, target, incomingAssets,
				(Type expectedType, Type foundType, AssetReference foundAsset) => {
					throw new NodeException(string.Format("{3} :Modifier expect {0}, but different type of incoming asset is found({1} {2})", 
						expectedType.FullName, foundType.FullName, foundAsset.fileNameAndExtension, node.Name), node.Id);
				},
				() => {
					throw new NodeException(node.Name + " :Modifier is not configured. Please configure from Inspector.", node.Id);
				},
				() => {
					throw new NodeException(node.Name + " :Failed to create Modifier from settings. Please fix settings from Inspector.", node.Id);
				},
				(Type expected, Type incoming) => {
					throw new NodeException(string.Format("{0} :Incoming asset type is does not match with this Modifier (Expected type:{1}, Incoming type:{2}).",
						node.Name, (expected != null)?expected.FullName:"null", (incoming != null)?incoming.FullName:"null"), node.Id);
				}
			);

			// Modifier does not add, filter or change structure of group, so just pass given group of assets
			Output(inputGroupAssets);
			Profiler.EndSample();
		}

		
		public void Run (BuildTarget target, 
			NodeData node, 
			ConnectionData connectionFromInput,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<AssetReference>> inputGroupAssets, 
			PerformGraph.Output Output) 
		{
			Profiler.BeginSample("AssetBundleGraph.GUIModifier.Run");
			var incomingAssets = inputGroupAssets.SelectMany(v => v.Value).ToList();

			var modifier = ModifierUtility.CreateModifier(node, target);
			UnityEngine.Assertions.Assert.IsNotNull(modifier);
			bool isAnyAssetModified = false;

			foreach(var asset in incomingAssets) {
				if(modifier.IsModified(asset.data)) {
					modifier.Modify(asset.data);
					asset.ReleaseData();
					isAnyAssetModified = true;
				}
			}

			if(isAnyAssetModified) {
				// apply asset setting changes to AssetDatabase.
				AssetDatabase.Refresh();
			}

			// Modifier does not add, filter or change structure of group, so just pass given group of assets
			Output(inputGroupAssets);
			Profiler.EndSample();
		}
			
		public static void ValidateModifier (
			NodeData node,
			BuildTarget target,
			List<AssetReference> incomingAssets,
			Action<Type, Type, AssetReference> multipleAssetTypeFound,
			Action noModiferData,
			Action failedToCreateModifier,
			Action<Type, Type> incomingTypeMismatch
		) {
			Type expectedType = TypeUtility.FindFirstIncomingAssetType(incomingAssets);
			if(expectedType != null) {
				foreach(var a  in incomingAssets) {
					Type assetType = a.filterType;
					if(assetType != expectedType) {
						multipleAssetTypeFound(expectedType, assetType, a);
					}
				}
			}

			if(string.IsNullOrEmpty(node.InstanceData[target])) {
				noModiferData();
			}

			var modifier = ModifierUtility.CreateModifier(node, target);

			if(null == modifier ) {
				failedToCreateModifier();
			}

			// if there is no incoming assets, there is no way to check if 
			// right type of asset is coming in - so we'll just skip the test
			if(incomingAssets.Any()) {
				var targetType = ModifierUtility.GetModifierTargetType(modifier);
				if( targetType != expectedType ) {
					incomingTypeMismatch(targetType, expectedType);
				}
			}
		}			
	}
}
