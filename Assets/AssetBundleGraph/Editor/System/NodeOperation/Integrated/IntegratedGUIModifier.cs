using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace AssetBundleGraph {
    public class IntegratedGUIModifier : INodeOperationBase {

		private readonly string specifiedScriptClass;

		public IntegratedGUIModifier (string specifiedScriptClass) {
			this.specifiedScriptClass = specifiedScriptClass;
		}

		public void Setup (BuildTarget target, NodeData node, string connectionIdToNextNode, Dictionary<string, List<Asset>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {
			if (groupedSources.Keys.Count == 0) {
				return;
			}
			
			// Modifier merges multiple incoming groups into one.
			if (1 < groupedSources.Keys.Count) {
				Debug.LogWarning(node.Name + " Modifier merges incoming group into \"" + groupedSources.Keys.ToList()[0]);
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

			Type modifierType = null;

			var first = true;
			foreach (var inputSource in inputSources) {
				var modifyTargetAssetPath = inputSource.importFrom; 
				var givenAssetType = TypeUtility.FindTypeOfAsset(modifyTargetAssetPath);

				if (givenAssetType == null || givenAssetType == typeof(object)) {
					continue;
				}

				if (first) {
					first = false;
					modifierType = givenAssetType;
					continue;
				}

				if (modifierType != givenAssetType) {
					throw new NodeException("multiple Asset Type detected. consider reduce Asset Type number to only 1 by Filter. detected Asset Types is:" + modifierType + " , and " + givenAssetType, node.Id);
				}
			}

			if (!string.IsNullOrEmpty(specifiedScriptClass)) {
				Debug.LogError("modifierのScript版実装中。");
				return;
			}

			// check support.
			if (!modifierOperatorTypeMap.ContainsKey(modifierType)) {
				throw new NodeException("current incoming Asset Type:" + modifierType + " is not supported.", node.Id);
			}

			// make sure default data is present
			SaveDefaultDataToDisk(node.Id, modifierType);

			// validate saved data.
			ValidateModifiyOperationData(
				node.Id,
				target,
				() => {
					throw new NodeException("No ModifierOperatorData found. please Setup first.", node.Id);
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

			Output(node.Id, connectionIdToNextNode, outputDict, new List<string>());
		}

		
		public void Run (BuildTarget target, NodeData node, string connectionIdToNextNode, Dictionary<string, List<Asset>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {
			if (groupedSources.Keys.Count == 0) {
				return;
			}
			
			// Modifier merges multiple incoming groups into one.
			if (1 < groupedSources.Keys.Count) {
				Debug.LogWarning(node.Name + " Modifier merges incoming group into \"" + groupedSources.Keys.ToList()[0]);
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

			// modifierType is fixed. 			
			if (!string.IsNullOrEmpty(specifiedScriptClass)) {
				Debug.LogError("modifierのScript版実装中。");
				return;
			}

			Type assetType = TypeUtility.FindTypeOfAsset(inputSources[0].importFrom);

			// check support.
			if (!modifierOperatorTypeMap.ContainsKey(assetType)) {
				throw new NodeException("current incoming Asset Type:" + assetType + " is unsupported.", node.Id);
			}


			// validate saved data.
			ValidateModifiyOperationData(
				node.Id,
				target,
				() => {
					throw new NodeException("No ModifierOperatorData found. please Setup first.", node.Id);
				},
				() => {
					/*do nothing.*/
				}
			);
			
			var outputSources = new List<Asset>();

			var g = BuildTargetUtility.TargetToGroup(target);
			var modifyOperatorInstance = CreateModifierOperator(node.Id, g);

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

			Output(node.Id, connectionIdToNextNode, outputDict, new List<string>());
		}

//		/**
//			caution.
//			do not delete this method.
//			this method is called through reflection for adopt Generic type in Runtime.
//		*/
//		public T FromJson<T> (string source) {
//			return JsonUtility.FromJson<T>(source);
//		}
//		
		public static void ValidateModifiyOperationData (
			string nodeId,
			BuildTarget target,
			Action noAssetOperationDataFound,
			Action validAssetOperationDataFound
		) {
			if( HasModifierDataFor(nodeId, BuildTargetUtility.TargetToGroup(target), true) ) {
				validAssetOperationDataFound();
				return;
			}
			noAssetOperationDataFound();
		}
			
		private static Type GetTargetType (string nodeId) {
			var data = LoadModifierDataFromDisk(nodeId, BuildTargetUtility.DefaultTarget);
			if(data == string.Empty) {
				return null;
			}
			var deserializedDataObject = JsonUtility.FromJson<ModifierOperators.OperatorBase>(data);
			if(deserializedDataObject != null) {
				return Types.GetType (deserializedDataObject.operatorType, "Assembly-CSharp-Editor.dll");
			}
			return null;
		}

		private static string GetModifierDataPath (string nodeId, BuildTargetGroup targetGroup) {
			return FileUtility.PathCombine(AssetBundleGraphSettings.MODIFIER_OPERATOR_DATAS_PLACE, nodeId, GetModifierOperatiorDataFileName(targetGroup));
		}

		private static string GetDefaultModifierDataPath (string nodeId) {
			return GetModifierDataPath(nodeId, BuildTargetUtility.DefaultTarget);
		}
			
		public static bool HasModifierDataFor (string nodeId, BuildTargetGroup targetGroup, bool checkDefault=false) {
			var dataPath = GetModifierDataPath(nodeId, targetGroup);
			if(File.Exists(dataPath)) {
				return true;
			}
			if(checkDefault) {
				dataPath = GetDefaultModifierDataPath(nodeId);
				if(File.Exists(dataPath)) {
					return true;
				}
			}
			return false;
		}

		public static void DeletePlatformData(string nodeId, BuildTargetGroup targetGroup) {
			var platformOpdataPath = GetModifierDataPath(nodeId, targetGroup);
			if (File.Exists(platformOpdataPath)) {
				File.Delete(platformOpdataPath);
			}
        }

		private static string GetModifierOperatiorDataFileName (BuildTargetGroup targetGroup) {
			if (targetGroup == BuildTargetUtility.DefaultTarget) {
				return AssetBundleGraphSettings.MODIFIER_OPERATOR_DATA_NANE_PREFIX + "." + AssetBundleGraphSettings.MODIFIER_OPERATOR_DATA_NANE_SUFFIX;
			}
			return AssetBundleGraphSettings.MODIFIER_OPERATOR_DATA_NANE_PREFIX + "." + 
				SystemDataUtility.GetPathSafeTargetGroupName(targetGroup) + "." + 
				AssetBundleGraphSettings.MODIFIER_OPERATOR_DATA_NANE_SUFFIX;
		}

		private static void SaveDefaultDataToDisk(string nodeId, Type modifierType) {

			var dataPath = GetDefaultModifierDataPath(nodeId);
			var dataDir  = Directory.GetParent(dataPath);
			if (!dataDir.Exists) {
				dataDir.Create();
			}

			if (!File.Exists(dataPath)) {
				Type operatorType = modifierOperatorTypeMap[modifierType];

				var operatorInstance = Activator.CreateInstance(operatorType) as ModifierOperators.OperatorBase;

				var operatorWithDefaultSettings = operatorInstance.DefaultSetting();

				var jsonData = JsonUtility.ToJson(operatorWithDefaultSettings);
				var prettified = Json.Prettify(jsonData);
				using (var sw = new StreamWriter(dataPath)) {
					sw.WriteLine(prettified);
				}
			}
		}

		private static string LoadModifierDataFromDisk(string nodeId, BuildTargetGroup targetPlatform) {
			var modifierOperatorDataPath = GetModifierDataPath(nodeId, targetPlatform);

			// choose default modifierOperatorData if platform specified file is not exist.
			if (!File.Exists(modifierOperatorDataPath)) {
				modifierOperatorDataPath = GetDefaultModifierDataPath(nodeId);
			}

			var data = string.Empty;
			using (var sr = new StreamReader(modifierOperatorDataPath)) {
				data = sr.ReadToEnd();
			}

			return data;
		}

		public static void SaveModifierOperatorToDisk(string nodeId, BuildTargetGroup targetPlatform, ModifierOperators.OperatorBase op) {

			var dataPath = GetModifierDataPath(nodeId, targetPlatform);
			var dataDir  = Directory.GetParent(dataPath);
			if (!dataDir.Exists) {
				dataDir.Create();
			}

			var data = JsonUtility.ToJson(op);
			var prettified = Json.Prettify(data);

			using (var sw = new StreamWriter(dataPath)) {
				sw.Write(prettified);
			}
		}

		/**
		 * ModifierOperator map vs supported type
		 */
		private static Dictionary<Type, Type> modifierOperatorTypeMap = new Dictionary<Type, Type> {
			{typeof(UnityEngine.Animation), typeof(ModifierOperators.AnimationOperator)},
			{typeof(UnityEngine.Animator), typeof(ModifierOperators.AnimatorOperator)},
			{typeof(UnityEditor.Animations.AvatarMask), typeof(ModifierOperators.AvatarMaskOperator)},
			{typeof(UnityEngine.Cubemap), typeof(ModifierOperators.CubemapOperator)},
			{typeof(UnityEngine.Flare), typeof(ModifierOperators.FlareOperator)},
			{typeof(UnityEngine.Font), typeof(ModifierOperators.FontOperator)},
			{typeof(UnityEngine.GUISkin), typeof(ModifierOperators.GUISkinOperator)},
			// typeof(LightmapParameters).ToString(),// ファイルにならない
			{typeof(UnityEngine.Material), typeof(ModifierOperators.MaterialOperator)},
			{typeof(UnityEngine.PhysicMaterial), typeof(ModifierOperators.PhysicMaterialOperator)},
			{typeof(UnityEngine.PhysicsMaterial2D), typeof(ModifierOperators.PhysicsMaterial2DOperator)},
			{typeof(UnityEngine.RenderTexture), typeof(ModifierOperators.RenderTextureOperator)},
			// // typeof(SceneAsset).ToString(),// ファイルにならない
			{typeof(UnityEngine.Shader), typeof(ModifierOperators.ShaderOperator)},
			{typeof(UnityEngine.SceneManagement.Scene), typeof(ModifierOperators.SceneOperator)},
		};

		public static ModifierOperators.OperatorBase CreateModifierOperator(string nodeId, BuildTargetGroup targetGroup) {

			string data = LoadModifierDataFromDisk(nodeId, targetGroup);
			Type operatorType = modifierOperatorTypeMap[GetTargetType(nodeId)];

			return JsonUtility.FromJson(data, operatorType) as ModifierOperators.OperatorBase;
		}
	}
}
