using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace AssetBundleGraph {
    public class IntegratedGUIModifier : INodeOperationBase {
		private readonly string currentPlatformStr;
		private readonly string specifiedScriptClass;

		public IntegratedGUIModifier (string specifiedScriptClass, string modifierTargetPlatform) {
			this.specifiedScriptClass = specifiedScriptClass;
			this.currentPlatformStr = modifierTargetPlatform;
		}

		public void Setup (string nodeName, string nodeId, string connectionIdToNextNode, Dictionary<string, List<Asset>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {
			if (groupedSources.Keys.Count == 0) {
				return;
			}
			
			// Modifier merges multiple incoming groups into one.
			if (1 < groupedSources.Keys.Count) {
				Debug.LogWarning(nodeName + " Modifier merges incoming group into \"" + groupedSources.Keys.ToList()[0]);
			}

			var groupMergeKey = groupedSources.Keys.ToList()[0];

			// merge all assets into single list.
			var inputSources = new List<Asset>();
			foreach (var groupKey in groupedSources.Keys) {
				inputSources.AddRange(groupedSources[groupKey]);
			}
			
			if (!inputSources.Any()) {
				return;
			} 

			// initialize as object.
			var modifierType = string.Empty;

			var first = true;
			foreach (var inputSource in inputSources) {
				var modifyTargetAssetPath = inputSource.importFrom; 
				var assumedType = TypeUtility.FindTypeOfAsset(modifyTargetAssetPath);

				if (assumedType == null || assumedType == typeof(object)) {
					continue;
				}

				if (first) {
					first = false;
					modifierType = assumedType.ToString();
					continue;
				}

				if (modifierType != assumedType.ToString()) {
					throw new NodeException("multiple Asset Type detected. consider reduce Asset Type number to only 1 by Filter. detected Asset Types is:" + modifierType + " , and " + assumedType.ToString(), nodeId);
				}
			}

			// modifierType is fixed. 
			
			if (!string.IsNullOrEmpty(specifiedScriptClass)) {
				Debug.LogError("modifierのScript版実装中。");
				return;
			}

			// check support.
			if (!TypeUtility.SupportedModifierOperatorDefinition.ContainsKey(modifierType)) {
				throw new NodeException("current incoming Asset Type:" + modifierType + " is unsupported.", nodeId);
			}

			// generate modifierOperatorData if data is not exist yet.
			{
				var modifierOperationDataFolderPath = AssetBundleGraphSettings.MODIFIER_OPERATOR_DATAS_PLACE;
				if (!Directory.Exists(modifierOperationDataFolderPath)) {
					Directory.CreateDirectory(modifierOperationDataFolderPath);
				}

				var opDataFolderPath = FileUtility.PathCombine(modifierOperationDataFolderPath, nodeId);
				if (!Directory.Exists(opDataFolderPath)) {
					Directory.CreateDirectory(opDataFolderPath);
				} 

				// ready default platform path.
				var modifierOperatorDataPathForDefaultPlatform = FileUtility.PathCombine(opDataFolderPath, ModifierOperatiorDataName(AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME));

				/*
					create default platform ModifierOperatorData if not exist.
					default ModifierOperatorData is the target platform for every platform by default.
				*/
				if (!File.Exists(modifierOperatorDataPathForDefaultPlatform)) {
					var operatorType = TypeUtility.SupportedModifierOperatorDefinition[modifierType];

					var operatorInstance = Activator.CreateInstance(operatorType) as ModifierBase;

					var defaultRenderTextureOp = operatorInstance.DefaultSetting();

					/*
						generated json data is typed as supported ModifierOperation type.
					*/
					var jsonData = JsonUtility.ToJson(defaultRenderTextureOp);
					var prettified = AssetBundleGraphEditorWindow.PrettifyJson(jsonData);
					using (var sw = new StreamWriter(modifierOperatorDataPathForDefaultPlatform)) {
						sw.WriteLine(prettified);
					}
				}
			}

			// validate saved data.
			ValidateModifiyOperationData(
				nodeId,
				currentPlatformStr,
				() => {
					throw new NodeException("No ModifierOperatorData found. please Setup first.", nodeId);
				},
				() => {
					/*do nothing.*/
				}
			);
			
			var outputSources = new List<Asset>();

			/*
				all assets types are same and do nothing to assets in setup.
			*/
			foreach (var asset in inputSources) {
				outputSources.Add(Asset.DuplicateAsset(asset));
			}

			var outputDict = new Dictionary<string, List<Asset>>();
			outputDict[groupMergeKey] = outputSources;

			Output(nodeId, connectionIdToNextNode, outputDict, new List<string>());
		}

		
		public void Run (string nodeName, string nodeId, string connectionIdToNextNode, Dictionary<string, List<Asset>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {
			if (groupedSources.Keys.Count == 0) {
				return;
			}
			
			// Modifier merges multiple incoming groups into one.
			if (1 < groupedSources.Keys.Count) {
				Debug.LogWarning(nodeName + " Modifier merges incoming group into \"" + groupedSources.Keys.ToList()[0]);
			}

			var groupMergeKey = groupedSources.Keys.ToList()[0];

			// merge all assets into single list.
			var inputSources = new List<Asset>();
			foreach (var groupKey in groupedSources.Keys) {
				inputSources.AddRange(groupedSources[groupKey]);
			}
			
			if (!inputSources.Any()) {
				return;
			} 

			// load type from 1st asset of flow.
			var modifierType = TypeUtility.FindTypeOfAsset(inputSources[0].importFrom).ToString();

			// modifierType is fixed. 
			
			if (!string.IsNullOrEmpty(specifiedScriptClass)) {
				Debug.LogError("modifierのScript版実装中。");
				return;
			}

			// check support.
			if (!TypeUtility.SupportedModifierOperatorDefinition.ContainsKey(modifierType)) {
				throw new NodeException("current incoming Asset Type:" + modifierType + " is unsupported.", nodeId);
			}


			// validate saved data.
			ValidateModifiyOperationData(
				nodeId,
				currentPlatformStr,
				() => {
					throw new NodeException("No ModifierOperatorData found. please Setup first.", nodeId);
				},
				() => {
					/*do nothing.*/
				}
			);
			
			var outputSources = new List<Asset>();

			var modifierOperatorDataPathForTargetPlatform = FileUtility.PathCombine(AssetBundleGraphSettings.MODIFIER_OPERATOR_DATAS_PLACE, nodeId, ModifierOperatiorDataName(currentPlatformStr));

			// if runtime platform specified modifierOperatorData is nof found, 
			// use default platform modifierOperatorData.
			if (!File.Exists(modifierOperatorDataPathForTargetPlatform)) {
				modifierOperatorDataPathForTargetPlatform = FileUtility.PathCombine(AssetBundleGraphSettings.MODIFIER_OPERATOR_DATAS_PLACE, nodeId, ModifierOperatiorDataName(AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME));
			} 

			var loadedModifierOperatorData = string.Empty;
			using (var sr = new StreamReader(modifierOperatorDataPathForTargetPlatform)) {
				loadedModifierOperatorData = sr.ReadToEnd();
			}

			/*
				read saved modifierOperation type for detect data type.
			*/
			var deserializedDataObject = JsonUtility.FromJson<ModifierBase>(loadedModifierOperatorData);
			var dataTypeString = deserializedDataObject.operatorType;
			
			// sadly, if loaded assetType is no longer supported or not.
			if (!TypeUtility.SupportedModifierOperatorDefinition.ContainsKey(dataTypeString)) {
				throw new NodeException("unsupported ModifierOperator Type:" + modifierType, nodeId);
			} 

			var modifyOperatorType = TypeUtility.SupportedModifierOperatorDefinition[dataTypeString];
			
			/*
				make generic method for genearte desired typed ModifierOperator instance.
			*/
			var modifyOperatorInstance = typeof(IntegratedGUIModifier)
				.GetMethod("FromJson")
				.MakeGenericMethod(modifyOperatorType)// set desired generic type here.
				.Invoke(this, new object[] { loadedModifierOperatorData }) as ModifierBase;
			
			var isChanged = false;
			foreach (var inputSource in inputSources) {
				var modifyTargetAssetPath = inputSource.importFrom;

				var modifyOperationTargetAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(modifyTargetAssetPath);

				if (!modifyOperatorInstance.IsChanged(modifyOperationTargetAsset)) {
					outputSources.Add(
						Asset.CreateNewAssetWithImportPathAndStatus(
							inputSource.importFrom,
							false,// marked as not changed.
							false
						)
					);
					continue;
				}

				isChanged = true;
				modifyOperatorInstance.Modify(modifyOperationTargetAsset);
				
				outputSources.Add(
					Asset.CreateNewAssetWithImportPathAndStatus(
						inputSource.importFrom,
						true,// marked as changed.
						false
					)				
				);
			}

			if (isChanged) {
				// apply asset setting changes to AssetDatabase.
				AssetDatabase.Refresh();
			}

			var outputDict = new Dictionary<string, List<Asset>>();
			outputDict[groupMergeKey] = outputSources;

			Output(nodeId, connectionIdToNextNode, outputDict, new List<string>());
		}

		/**
			caution.
			do not delete this method.
			this method is called through reflection for adopt Generic type in Runtime.
		*/
		public T FromJson<T> (string source) {
			return JsonUtility.FromJson<T>(source);
		}
		
		public static void ValidateModifiyOperationData (
			string modifierNodeId,
			string targetPlatform,
			Action noAssetOperationDataFound,
			Action validAssetOperationDataFound
		) {
			var platformOpDataPath = FileUtility.PathCombine(AssetBundleGraphSettings.MODIFIER_OPERATOR_DATAS_PLACE, modifierNodeId, ModifierOperatiorDataName(targetPlatform));
			if (File.Exists(platformOpDataPath)) {
				validAssetOperationDataFound();
				return;
			}
			
			// if platform data is not exist, search default one.
			var defaultPlatformOpDataPath = FileUtility.PathCombine(AssetBundleGraphSettings.MODIFIER_OPERATOR_DATAS_PLACE, modifierNodeId, ModifierOperatiorDataName(AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME));
			if (File.Exists(defaultPlatformOpDataPath)) {
				validAssetOperationDataFound();
				return;
			}

			noAssetOperationDataFound();
		}

		/**
			always returns Default platform's ModifierOperator's target asset type name.
		*/
		public static string ModifierOperationTargetTypeName (string nodeId) {
			var defaultModifierOperatorDataPath = ModifierDataPathForDefaultPlatform(nodeId);
			
			if (!File.Exists(defaultModifierOperatorDataPath)) {
				return string.Empty;
			}

			var dataStr = string.Empty;
			using (var sr = new StreamReader(defaultModifierOperatorDataPath)) {
				dataStr = sr.ReadToEnd();
			}

			if (string.IsNullOrEmpty(dataStr)) {
				return string.Empty;
			}

			var deserializedDataObject = JsonUtility.FromJson<ModifierBase>(dataStr);
			return deserializedDataObject.operatorType;
		}

		public static string ModifierDataPathForeachPlatform (string nodeId, string platformStr) {
			return FileUtility.PathCombine(AssetBundleGraphSettings.MODIFIER_OPERATOR_DATAS_PLACE, nodeId, ModifierOperatiorDataName(platformStr));
		}

		public static string ModifierDataPathForDefaultPlatform (string nodeId) {
			return FileUtility.PathCombine(AssetBundleGraphSettings.MODIFIER_OPERATOR_DATAS_PLACE, nodeId, ModifierOperatiorDataName(AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME));
		}

        public static void DeletePlatformData(string nodeId, string platformStr) {
			var platformOpdataPath = FileUtility.PathCombine(AssetBundleGraphSettings.MODIFIER_OPERATOR_DATAS_PLACE, nodeId, ModifierOperatiorDataName(platformStr));
			if (File.Exists(platformOpdataPath)) {
				File.Delete(platformOpdataPath);
			} 
        }

		public static string ModifierOperatiorDataName (string platformStr) {
			if (platformStr == AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME) {
				return AssetBundleGraphSettings.MODIFIER_OPERATOR_DATA_NANE_PREFIX + "." + AssetBundleGraphSettings.MODIFIER_OPERATOR_DATA_NANE_SUFFIX;
			}
			return AssetBundleGraphSettings.MODIFIER_OPERATOR_DATA_NANE_PREFIX + "." + platformStr + "." + AssetBundleGraphSettings.MODIFIER_OPERATOR_DATA_NANE_SUFFIX;
		}
	}
}
