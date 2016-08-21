using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace AssetBundleGraph {
    public class IntegratedGUIModifier : INodeBase {
		private readonly string currentPlatformStr;

		public IntegratedGUIModifier (string modifierTargetPlatform) {
			this.currentPlatformStr = modifierTargetPlatform;
		}

		public void Setup (string nodeName, string nodeId, string connectionIdToNextNode, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			if (groupedSources.Keys.Count == 0) {
				return;
			}
			
			// Modifier merges multiple incoming groups into one.
			if (1 < groupedSources.Keys.Count) {
				Debug.LogWarning(nodeName + " Modifier merges incoming group into \"" + groupedSources.Keys.ToList()[0]);
			}

			var groupMergeKey = groupedSources.Keys.ToList()[0];

			// merge all assets into single list.
			var inputSources = new List<InternalAssetData>();
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
				var modifyTargetAssetPath = inputSource.importedPath; 
				var assumedType = TypeBinder.AssumeTypeOfAsset(modifyTargetAssetPath);
				if (first) {
					first = false;
					modifierType = assumedType.ToString();
					continue;
				}

				if (modifierType != assumedType.ToString()) {
					Debug.LogError("type mismatch found:" + assumedType + " 実行時、流れ込んでくる素材の中に不純物がふくまれているエラー");
					return;
				}
			}
			
			// modifierType is fixed. check support.
			if (!TypeBinder.SupportedModifierOperatorDefinition.ContainsKey(modifierType)) {
				throw new NodeException("current incoming Asset Type:" + modifierType + " is unsupported.", nodeId);
			}

			// generate modifierOperatorData if data is not exist yet.
			{
				var modifierOperatorDataFolderPath = AssetBundleGraphSettings.MODIFIER_OPERATOR_DATAS_PLACE;
				if (!Directory.Exists(modifierOperatorDataFolderPath)) {
					Directory.CreateDirectory(modifierOperatorDataFolderPath);
				}

				var opDataFolderPath = FileController.PathCombine(modifierOperatorDataFolderPath, nodeId);
				if (!Directory.Exists(opDataFolderPath)) {
					Directory.CreateDirectory(opDataFolderPath);
				} 

				// ready default platform path.
				var modifierOperatorDataPathForDefaultPlatform = FileController.PathCombine(opDataFolderPath, ModifierOperatiorDataName(AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME));

				/*
					create default platform ModifierOperatorData if not exist.
					default ModifierOperatorData is the target platform for every platform by default.
				*/
				if (!File.Exists(modifierOperatorDataPathForDefaultPlatform)) {
					var operatorType = TypeBinder.SupportedModifierOperatorDefinition[modifierType];

					var operatorInstance = Activator.CreateInstance(operatorType) as ModifierOperators.OperatorBase;

					var defaultRenderTextureOp = operatorInstance.DefaultSetting();

					/*
						generated json data is typed as supported ModifierOperation type.
					*/
					var jsonData = JsonUtility.ToJson(defaultRenderTextureOp);
					var prettified = AssetBundleGraph.PrettifyJson(jsonData);
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
			
			var outputSources = new List<InternalAssetData>();

			/*
				all assets types are same and do nothing to assets in setup.
			*/
			foreach (var inputSource in inputSources) {
				var modifyTargetAssetPath = inputSource.importedPath;
				
				var newData = InternalAssetData.InternalAssetDataByImporterOrModifier(
					inputSource.traceId,
					inputSource.absoluteSourcePath,
					inputSource.sourceBasePath,
					inputSource.fileNameAndExtension,
					inputSource.pathUnderSourceBase,
					inputSource.importedPath,
					null,
					inputSource.assetType
				);

				outputSources.Add(newData);
			}

			var outputDict = new Dictionary<string, List<InternalAssetData>>();
			outputDict[groupMergeKey] = outputSources;

			Output(nodeId, connectionIdToNextNode, outputDict, new List<string>());
		}

		
		public void Run (string nodeName, string nodeId, string connectionIdToNextNode, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			if (groupedSources.Keys.Count == 0) {
				return;
			}
			
			// Modifier merges multiple incoming groups into one.
			if (1 < groupedSources.Keys.Count) {
				Debug.LogWarning(nodeName + " Modifier merges incoming group into \"" + groupedSources.Keys.ToList()[0]);
			}

			var groupMergeKey = groupedSources.Keys.ToList()[0];

			// merge all assets into single list.
			var inputSources = new List<InternalAssetData>();
			foreach (var groupKey in groupedSources.Keys) {
				inputSources.AddRange(groupedSources[groupKey]);
			}
			
			if (!inputSources.Any()) {
				return;
			} 

			// load type from 1st asset of flow.
			var modifierType = TypeBinder.AssumeTypeOfAsset(inputSources[0].importedPath).ToString();

			// modifierType is fixed. check support.
			if (!TypeBinder.SupportedModifierOperatorDefinition.ContainsKey(modifierType)) {
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
			
			var outputSources = new List<InternalAssetData>();

			var modifierOperatorDataPathForTargetPlatform = FileController.PathCombine(AssetBundleGraphSettings.MODIFIER_OPERATOR_DATAS_PLACE, nodeId, ModifierOperatiorDataName(currentPlatformStr));

			// if runtime platform specified modifierOperatorData is nof found, 
			// use default platform modifierOperatorData.
			if (!File.Exists(modifierOperatorDataPathForTargetPlatform)) {
				modifierOperatorDataPathForTargetPlatform = FileController.PathCombine(AssetBundleGraphSettings.MODIFIER_OPERATOR_DATAS_PLACE, nodeId, ModifierOperatiorDataName(AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME));
			} 

			var loadedModifierOperatorData = string.Empty;
			using (var sr = new StreamReader(modifierOperatorDataPathForTargetPlatform)) {
				loadedModifierOperatorData = sr.ReadToEnd();
			}

			/*
				read saved modifierOperator type for detect data type.
			*/
			var deserializedDataObject = JsonUtility.FromJson<ModifierOperators.OperatorBase>(loadedModifierOperatorData);
			var dataTypeString = deserializedDataObject.dataType;

			// sadly, if loaded assetType is no longer supported or not.
			if (!TypeBinder.SupportedModifierOperatorDefinition.ContainsKey(dataTypeString)) {
				throw new NodeException("unsupported ModifierOperator Type:" + modifierType, nodeId);
			} 

			var modifyOperatorType = TypeBinder.SupportedModifierOperatorDefinition[dataTypeString];
			
			/*
				make generic method for genearte desired typed ModifierOperator instance.
			*/
			var modifyOperatorInstance = typeof(IntegratedGUIModifier)
				.GetMethod("FromJson")
				.MakeGenericMethod(modifyOperatorType)// set desired generic type here.
				.Invoke(this, new object[] { loadedModifierOperatorData }) as ModifierOperators.OperatorBase;
			
			var isChanged = false;
			foreach (var inputSource in inputSources) {
				var modifyTargetAssetPath = inputSource.importedPath;

				var modifyOperationTargetAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(modifyTargetAssetPath);

				if (!modifyOperatorInstance.IsChanged(modifyOperationTargetAsset)) {
					var notChangedData = InternalAssetData.InternalAssetDataGeneratedByImporterOrModifierOrPrefabricator(
						inputSource.importedPath,
						AssetDatabase.AssetPathToGUID(inputSource.importedPath),
						AssetBundleGraphInternalFunctions.GetAssetType(inputSource.importedPath),
						false,// marked as not changed.
						false
					);
					outputSources.Add(notChangedData);
					continue;
				}

				isChanged = true;
				modifyOperatorInstance.Modify(modifyOperationTargetAsset);
				
				var newData = InternalAssetData.InternalAssetDataGeneratedByImporterOrModifierOrPrefabricator(
					inputSource.importedPath,
					AssetDatabase.AssetPathToGUID(inputSource.importedPath),
					AssetBundleGraphInternalFunctions.GetAssetType(inputSource.importedPath),
					true,// marked as changed.
					false
				);
				
				outputSources.Add(newData);
			}

			if (isChanged) {
				// apply asset setting changes to AssetDatabase.
				AssetDatabase.Refresh();
			}

			var outputDict = new Dictionary<string, List<InternalAssetData>>();
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
		
		/**
			限定的なチェックが出来る。
			・nodeに対応したファイルが存在するかどうか
			のみだ。

			それ以外の情報を得るには、
			・setupで流れ込んでくるデータの情報が必須。

			これはImporterと同じだね。
			ファイル存在チェック以上のチェックは、SetupとかRunで行おう。

			デフォルトプラットフォームとそれ以外とで、扱いが異ならないように、
			・まずその他のプラットフォームのデータの有無を確認 -> この処理は汎用化しないと。
			・なくても、デフォルトのものがあればそれを使う

			という感じ。
		*/
		public static void ValidateModifiyOperationData (
			string modifierNodeId,
			string targetPlatform,
			Action noAssetOperationDataFound,
			Action validAssetOperationDataFound
		) {
			var platformOpDataPath = FileController.PathCombine(AssetBundleGraphSettings.MODIFIER_OPERATOR_DATAS_PLACE, modifierNodeId, ModifierOperatiorDataName(targetPlatform));
			if (File.Exists(platformOpDataPath)) {
				validAssetOperationDataFound();
				return;
			}
			
			// if platform data is not exist, search default one.
			var defaultPlatformOpDataPath = FileController.PathCombine(AssetBundleGraphSettings.MODIFIER_OPERATOR_DATAS_PLACE, modifierNodeId, ModifierOperatiorDataName(AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME));
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

			var deserializedDataObject = JsonUtility.FromJson<ModifierOperators.OperatorBase>(dataStr);
			return deserializedDataObject.dataType;
		}

		public static string ModifierDataPathForeachPlatform (string nodeId, string platformStr) {
			return FileController.PathCombine(AssetBundleGraphSettings.MODIFIER_OPERATOR_DATAS_PLACE, nodeId, ModifierOperatiorDataName(platformStr));
		}

		public static string ModifierDataPathForDefaultPlatform (string nodeId) {
			return FileController.PathCombine(AssetBundleGraphSettings.MODIFIER_OPERATOR_DATAS_PLACE, nodeId, ModifierOperatiorDataName(AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME));
		}

        public static void DeletePlatformData(string nodeId, string platformStr) {
            var platformOpdataPath = FileController.PathCombine(AssetBundleGraphSettings.MODIFIER_OPERATOR_DATAS_PLACE, nodeId, ModifierOperatiorDataName(platformStr));
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
