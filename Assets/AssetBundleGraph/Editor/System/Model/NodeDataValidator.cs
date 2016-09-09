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
	public partial class NodeData {

		/*
		 * Checks deserialized NodeData, and make some changes if necessary
		 * return false if any changes are perfomed.
		 */
		public bool Validate () {

			return true;

//			var nodeJson = n as Dictionary<string, object>;				
//
//			// copy all key and value to new Node data dictionary.
//			var sanitizedNodeJson = new Dictionary<string, object>();
//			foreach (var key in nodeJson.Keys) {
//				sanitizedNodeJson[key] = nodeJson[key];
//			}
//
//			var kind = AssetBundleGraphSettings.NodeKindFromString(nodeJson[AssetBundleGraphSettings.NODE_KIND] as string);
//
//			switch (kind) {	
//			case NodeKind.FILTER_SCRIPT:
//				if(!ValidateNodeJsonDataForFilterScript(ref nodeJson, ref sanitizedNodeJson, ref changed)) {
//					changed = true;
//					continue;
//				}
//				break;			
//			case NodeKind.PREFABRICATOR_GUI: 
//			case NodeKind.PREFABRICATOR_SCRIPT: 
//				if(!ValidateNodeJsonDataForPrefabricator(ref nodeJson, kind == NodeKind.PREFABRICATOR_SCRIPT)) 
//				{
//					changed = true;
//					continue;
//				}
//				break;
//			case NodeKind.BUNDLIZER_GUI: 
//				if(!ValidateNodeJsonDataForBundlizer(ref nodeJson)) {
//					changed = true;
//					continue;
//				}
//				break;
//			case NodeKind.LOADER_GUI:
//			case NodeKind.FILTER_GUI:
//			case NodeKind.IMPORTSETTING_GUI:
//			case NodeKind.MODIFIER_GUI:
//			case NodeKind.GROUPING_GUI:
//			case NodeKind.EXPORTER_GUI: 
//			case NodeKind.BUNDLEBUILDER_GUI: 
//				break;
//			default:
//				{
//					var nodeName = nodeJson[AssetBundleGraphSettings.NODE_NAME] as string;
//					Debug.LogError(node.Name + " is defined as unknown kind of node. value:" + kind);
//					break;
//				}
//			}
		}
			
		private bool ValidateForPrefabricator() {

//			var nodeName = nodeJson[AssetBundleGraphSettings.NODE_NAME] as string;
//			var scriptClassName = nodeJson[AssetBundleGraphSettings.NODE_SCRIPT_CLASSNAME] as string;
//
//			if (string.IsNullOrEmpty(scriptClassName)) {
//				Debug.LogWarning(nodeName  + ": No script name assigned.");
//				// Node should not be removed if not pure script node
//				return !pureScriptNode;
//			}
//
//			var nodeScriptInstance = Assembly.GetExecutingAssembly().CreateInstance(scriptClassName);
//
//			if (nodeScriptInstance == null) {
//				Debug.LogError(nodeName  + ": Node could not be created properly because AssetBundleGraph failed to create script instance for " + 
//					scriptClassName + ". No such class found in assembly.");
//
//				// Node should not be removed if not pure script node
//				return !pureScriptNode;
//			}

			return true;
		}

		private static bool ValidateForFilterScript(ref bool isChanged) {

//			var nodeId 		= nodeJson[AssetBundleGraphSettings.NODE_ID] as string;
//			var nodeName 	= nodeJson[AssetBundleGraphSettings.NODE_NAME] as string;
//			var scriptClassName = nodeJson[AssetBundleGraphSettings.NODE_SCRIPT_CLASSNAME] as string;
//
//			if (string.IsNullOrEmpty(scriptClassName)) {
//				Debug.LogWarning(nodeName  + ": No script name assigned.");
//				return false;
//			}
//
//			var nodeScriptInstance = Assembly.GetExecutingAssembly().CreateInstance(scriptClassName);
//			if (nodeScriptInstance == null) {
//				Debug.LogError(nodeName  + ": Node could not be created properly because AssetBundleGraph failed to create script instance for " + 
//					scriptClassName + ". No such class found in assembly.");
//				return false;
//			}
//
//			var outputLabelsJson = nodeJson[AssetBundleGraphSettings.NODE_OUTPUTPOINT_LABELS] as List<object>;
//			var outputLabelsSet = new HashSet<string>();
//			foreach (var label in outputLabelsJson) {
//				outputLabelsSet.Add(label.ToString());
//			}
//
//			var labelsFromScript = new HashSet<string>();
//			Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output = 
//				(string dataSourceNodeId, string connectionLabel, Dictionary<string, List<Asset>> source, List<string> usedCache) => 
//			{
//				labelsFromScript.Add(connectionLabel);
//			};
//
//			// Setup() executed with dummy data to collect labels from derived script
//			((FilterBase)nodeScriptInstance).Setup(
//				nodeName,
//				nodeId, 
//				string.Empty,
//				new Dictionary<string, List<Asset>>{
//					{"0", new List<Asset>()}
//				},
//				new List<string>(),
//				Output
//			);
//
//			if (!outputLabelsSet.SetEquals(labelsFromScript)) {
//				sanitizedNodeJson[AssetBundleGraphSettings.NODE_OUTPUTPOINT_LABELS] = labelsFromScript.ToList();
//				isChanged = true;
//			}
			return true;
		}

		private static bool ValidateForBundlizer() {

//			var nodeName 	= nodeJson[AssetBundleGraphSettings.NODE_NAME] as string;
//
//			var bundleNameTemplateSource = nodeJson[AssetBundleGraphSettings.NODE_BUNDLIZER_BUNDLENAME_TEMPLATE] as Dictionary<string, object>;
//			if (bundleNameTemplateSource == null) {
//				Debug.LogWarning(node.Name + " bundleNameTemplateSource is null. This could be caused because of deserialization error.");
//				bundleNameTemplateSource = new Dictionary<string, object>();
//			}
//			var variantsSource = nodeJson[AssetBundleGraphSettings.NODE_BUNDLIZER_VARIANTS] as Dictionary<string, object>;
//			if (variantsSource == null) {
//				Debug.LogWarning(node.Name + " variantsSource is null. This could be caused because of deserialization error.");
//				variantsSource = new Dictionary<string, object>();
//			}
//			foreach (var platform_package_key in bundleNameTemplateSource.Keys) {
//				var platform_package_bundleNameTemplate = bundleNameTemplateSource[platform_package_key] as string;
//				if (string.IsNullOrEmpty(platform_package_bundleNameTemplate)) {
//					Debug.LogWarning(node.Name + " Bundle Name Template is empty. Configure this from editor.");
//					break;
//				}
//			}

			return true;
		}
	}
}
