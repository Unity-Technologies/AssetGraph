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
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			ValidateModifier(node, target, incoming,
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
				(Type expectedType, Type incomingType) => {
					throw new NodeException(string.Format("{0} :Incoming asset type is does not match with this Modifier (Expected type:{1}, Incoming type:{2}).",
						node.Name, (expectedType != null)?expectedType.FullName:"null", (incomingType != null)?incomingType.FullName:"null"), node.Id);
				}
			);


			if(incoming != null && Output != null) {
				// Modifier does not add, filter or change structure of group, so just pass given group of assets
				var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
					null : connectionsToOutput.First();

				foreach(var ag in incoming) {
					Output(dst, ag.assetGroups);
				}
			}
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
			var modifier = ModifierUtility.CreateModifier(node, target);
			UnityEngine.Assertions.Assert.IsNotNull(modifier);
			bool isAnyAssetModified = false;

			foreach(var ag in incoming) {
				foreach(var assets in ag.assetGroups.Values) {
					foreach(var asset in assets) {
						if(modifier.IsModified(asset.allData)) {
							modifier.Modify(asset.allData);
							asset.SetDirty();
							isAnyAssetModified = true;
						}
					}
				}
			}

			if(isAnyAssetModified) {
				// apply asset setting changes to AssetDatabase.
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}

			foreach(var ag in incoming) {
				foreach(var assets in ag.assetGroups.Values) {
					foreach(var asset in assets) {
						asset.ReleaseData();
					}
				}
			}

			if(incoming != null && Output != null) {
				// Modifier does not add, filter or change structure of group, so just pass given group of assets
				var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
					null : connectionsToOutput.First();

				foreach(var ag in incoming) {
					Output(dst, ag.assetGroups);
				}
			}
		}
			
		public static void ValidateModifier (
			NodeData node,
			BuildTarget target,
			IEnumerable<PerformGraph.AssetGroups> incoming,
			Action<Type, Type, AssetReference> multipleAssetTypeFound,
			Action noModiferData,
			Action failedToCreateModifier,
			Action<Type, Type> incomingTypeMismatch
		) {
			Type expectedType = null;
			if(incoming != null) {
				expectedType = TypeUtility.FindFirstIncomingAssetType(incoming);
				if(expectedType != null) {
					foreach(var ag in incoming) {
						foreach(var assets in ag.assetGroups.Values) {
							foreach(var a in assets) {
								Type assetType = a.filterType;
								if(assetType != expectedType) {
									multipleAssetTypeFound(expectedType, assetType, a);
								}
							}
						}
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
			// expectedType is not null when there is at least one incoming asset
			if(incoming != null && expectedType != null) {
				var targetType = ModifierUtility.GetModifierTargetType(modifier);
				if( targetType != expectedType ) {
					incomingTypeMismatch(targetType, expectedType);
				}
			}
		}			
	}
}
