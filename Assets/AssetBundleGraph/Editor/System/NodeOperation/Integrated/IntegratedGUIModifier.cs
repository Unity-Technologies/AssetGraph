using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace AssetBundleGraph {
    public class IntegratedGUIModifier : INodeOperationBase {
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

			// modifierType is fixed.

			// generate modifier operation data if data is not exist yet.
			var modifierOperationDataFolderPath = AssetBundleGraphSettings.MODIFIER_OPERATION_DATAS_PLACE;
			if (!Directory.Exists(modifierOperationDataFolderPath)) {
				Directory.CreateDirectory(modifierOperationDataFolderPath);
			}

			var opDataFolderPath = FileUtility.PathCombine(modifierOperationDataFolderPath, nodeId);
			if (!Directory.Exists(opDataFolderPath)) {
				Directory.CreateDirectory(opDataFolderPath);
			} 

			var opDataPath = FileUtility.PathCombine(opDataFolderPath, AssetBundleGraphSettings.MODIFIER_OPERATION_DATA_NANE);
			if (!File.Exists(opDataPath)) {
				// type is already assumed.
				if (!TypeUtility.SupportedModifierOperationDefinition.ContainsKey(modifierType)) {
					throw new NodeException("unsupported ModifierOperation Type:" + modifierType, nodeId);
				}

				var operatorType = TypeUtility.SupportedModifierOperationDefinition[modifierType];

				var operatorInstance = Activator.CreateInstance(operatorType) as ModifierOperators.OperatorBase;

				var defaultRenderTextureOp = operatorInstance.DefaultSetting();

				/*
					generated json data is typed as supported ModifierOperation type.
				*/
				var jsonData = JsonUtility.ToJson(defaultRenderTextureOp);
				using (var sw = new StreamWriter(opDataPath)) {
					sw.WriteLine(jsonData);
				}
			}
		

			// validate saved data.
			ValidateModifiyOperationData(
				nodeId,
				() => {
					throw new NodeException("このノードのOperationDataがないのでSetupしてね", nodeId);
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
				outputSources.Add(asset);
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

			var modifierOperationDataFolderPath = AssetBundleGraphSettings.MODIFIER_OPERATION_DATAS_PLACE;
			var opDataFolderPath = FileUtility.PathCombine(modifierOperationDataFolderPath, nodeId);
			var opDataPath = FileUtility.PathCombine(opDataFolderPath, AssetBundleGraphSettings.MODIFIER_OPERATION_DATA_NANE);
			
			// validate saved data.
			ValidateModifiyOperationData(
				nodeId,
				() => {
					throw new NodeException("このノードのOperationDataがないのでSetupしてね", nodeId);
				},
				() => {
					/*do nothing.*/
				}
			);
			
			var outputSources = new List<Asset>();

			var loadedModifierOperationData = string.Empty;
			using (var sr = new StreamReader(opDataPath)) {
				loadedModifierOperationData = sr.ReadLine();
			}

			/*
				read saved modifierOperation type for detect data type.
			*/
			var deserializedDataObject = JsonUtility.FromJson<ModifierOperators.OperatorBase>(loadedModifierOperationData);
			var dataTypeString = deserializedDataObject.dataType;
			
			if (!TypeUtility.SupportedModifierOperationDefinition.ContainsKey(dataTypeString)) {
				throw new NodeException("unsupported ModifierOperation Type:" + modifierType, nodeId);
			} 

			var modifyOperatorType = TypeUtility.SupportedModifierOperationDefinition[dataTypeString];
			
			/*
				make generic method for genearte desired typed ModifierOperator instance.
			*/
			var modifyOperatorInstance = typeof(IntegratedGUIModifier)
				.GetMethod("FromJson")
				.MakeGenericMethod(modifyOperatorType)// set desired generic type here.
				.Invoke(this, new object[] { loadedModifierOperationData }) as ModifierOperators.OperatorBase;
			
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
		
		/**
			限定的なチェックが出来る。
			・nodeに対応したファイルが存在するかどうか
			のみだ。

			それ以外の情報を得るには、
			・setupで流れ込んでくるデータの情報が必須。

			これはImporterと同じだね。
			ファイル存在チェック以上のチェックは、SetupとかRunで行おう。
		*/
		public static void ValidateModifiyOperationData (
			string modifierNodeId,
			Action noAssetOperationDataFound,
			Action validAssetOperationDataFound
		) {
			var opDataPath = FileUtility.PathCombine(AssetBundleGraphSettings.MODIFIER_OPERATION_DATAS_PLACE, modifierNodeId, AssetBundleGraphSettings.MODIFIER_OPERATION_DATA_NANE); 
			if (!File.Exists(opDataPath)) {
				noAssetOperationDataFound();
			}

			validAssetOperationDataFound();
		}
	}
}
