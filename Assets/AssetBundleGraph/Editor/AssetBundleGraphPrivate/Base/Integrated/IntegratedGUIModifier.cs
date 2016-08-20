using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace AssetBundleGraph {
    public class IntegratedGUIModifier : INodeBase {
		public void Setup (string nodeName, string nodeId, string connectionIdToNextNode, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			if (groupedSources.Keys.Count == 0) {
				return;
			}
			
			// ImportSetting merges multiple incoming groups into one. why -> see IntegratedGUIImporterSetting. same behaviour preferred.
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

			// modifierType is fixed.

			// generate modifier operation data if data is not exist yet.
			var modifierOperationDataFolderPath = AssetBundleGraphSettings.MODIFIER_OPERATION_DATAS_PLACE;
			if (!Directory.Exists(modifierOperationDataFolderPath)) {
				Directory.CreateDirectory(modifierOperationDataFolderPath);
			}

			var opDataFolderPath = FileController.PathCombine(modifierOperationDataFolderPath, nodeId);
			if (!Directory.Exists(opDataFolderPath)) {
				Directory.CreateDirectory(opDataFolderPath);
			} 

			var opDataPath = FileController.PathCombine(opDataFolderPath, AssetBundleGraphSettings.MODIFIER_OPERATION_DATA_NANE);
			if (!File.Exists(opDataPath)) {
				// type is already assumed.
				if (!TypeBinder.SupportedModifierOperationTarget.ContainsKey(modifierType)) {
					throw new NodeException("unsupported ModifierOperation Type:" + modifierType, nodeId);
				}

				var operatorType = TypeBinder.SupportedModifierOperationTarget[modifierType];

				var operatorInstance = Activator.CreateInstance(operatorType) as ModifierOperators.OperatorBase;

				var defaultRenderTextureOp = operatorInstance.DefaultSetting();

				/*
					generated json data is typed as supported ModifierOperation type.
				*/
				var jsonData = JsonUtility.ToJson(defaultRenderTextureOp);
				
				using (var sw = new StreamWriter(opDataPath, true)) {
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
			
			var outputSources = new List<InternalAssetData>();

			// /*
			// 	all assets types are same and do nothing to assets in setup.
			// */
			// foreach (var inputSource in inputSources) {
			// 	var modifyTargetAssetPath = inputSource.importedPath;
				
			// 	var newData = InternalAssetData.InternalAssetDataByImporterOrModifier(
			// 		inputSource.traceId,
			// 		inputSource.absoluteSourcePath,
			// 		inputSource.sourceBasePath,
			// 		inputSource.fileNameAndExtension,
			// 		inputSource.pathUnderSourceBase,
			// 		inputSource.importedPath,
			// 		null,
			// 		inputSource.assetType
			// 	);

			// 	outputSources.Add(newData);
			// }

			// 実行時のブロック
			// 実行時には、データ生成とかはしないが内容の有無のチェックとかはする。エラーで吹っ飛ばす専門。
			var loadedModifierOperationData = string.Empty;
			using (var sr = new StreamReader(opDataPath)) {
				loadedModifierOperationData = sr.ReadLine();
			}

			/*
				read saved modifierOperation type for detect data type.
			*/
			var deserializedDataObject = JsonUtility.FromJson<ModifierOperators.OperatorBase>(loadedModifierOperationData);
			var dataTypeString = deserializedDataObject.dataType;
			
			if (!TypeBinder.SupportedModifierOperationTarget.ContainsKey(dataTypeString)) {
				throw new NodeException("unsupported ModifierOperation Type:" + modifierType, nodeId);
			} 

			var modifyOperatorType = TypeBinder.SupportedModifierOperationTarget[dataTypeString];
			
			/*
				make generic method as desired typed.
			*/
			var modifyOperatorInstance = typeof(IntegratedGUIModifier)
				.GetMethod("FromJson")
				.MakeGenericMethod(modifyOperatorType)// set desired generic type here.
				.Invoke(this, new object[] { loadedModifierOperationData }) as ModifierOperators.OperatorBase;
			
			var isChanged = false;
			foreach (var inputSource in inputSources) {
				var modifyTargetAssetPath = inputSource.importedPath;

				var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(modifyTargetAssetPath);

				if (!modifyOperatorInstance.IsChanged(asset)) {
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

				// isChanged = true;
				// modifyOperatorInstance.Modify(modifyTargetAssetPath);
				
				// var newData = InternalAssetData.InternalAssetDataGeneratedByImporterOrModifierOrPrefabricator(
				// 	inputSource.importedPath,
				// 	AssetDatabase.AssetPathToGUID(inputSource.importedPath),
				// 	AssetBundleGraphInternalFunctions.GetAssetType(inputSource.importedPath),
				// 	true,// marked as changed.
				// 	false
				// );
				
				// outputSources.Add(newData);
			}

			if (isChanged) {
				AssetDatabase.Refresh();// 変更を加えたAssetの設定をデータベースに反映させないといけない。
			}

			var outputDict = new Dictionary<string, List<InternalAssetData>>();
			outputDict[groupMergeKey] = outputSources;

			Output(nodeId, connectionIdToNextNode, outputDict, new List<string>());
		}

		public T FromJson<T> (string source) {
			return JsonUtility.FromJson<T>(source);
		}
		
		public void Run (string nodeName, string nodeId, string connectionIdToNextNode, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			
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
			var opDataPath = FileController.PathCombine(AssetBundleGraphSettings.MODIFIER_OPERATION_DATAS_PLACE, modifierNodeId, AssetBundleGraphSettings.MODIFIER_OPERATION_DATA_NANE); 
			if (!File.Exists(opDataPath)) {
				noAssetOperationDataFound();
			}

			validAssetOperationDataFound();
		}
	}

    public class ModifierOperation<T> where T : UnityEngine.Object {
        public bool IsChanged (string modifyTargetAssetPath) {
            throw new NotImplementedException();
        }

        public void Modify (string modifyTargetAssetPath) {
            throw new NotImplementedException();
        }
    }
}
