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
			ConnectionPointData inputPoint,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<Asset>> inputGroupAssets, 
			List<string> alreadyCached, 
			Action<ConnectionData, Dictionary<string, List<Asset>>, List<string>> Output) 
		{
			if (inputGroupAssets.Keys.Count == 0) {
				return;
			}
			
			// Modifier merges multiple incoming groups into one.
			if (1 < inputGroupAssets.Keys.Count) {
				Debug.LogWarning(node.Name + " Modifier merges incoming group into \"" + inputGroupAssets.Keys.ToList()[0]);
			}

			var groupMergeKey = inputGroupAssets.Keys.ToList()[0];

			// merge all assets into single list.
			var inputSources = new List<Asset>();
			foreach (var groupKey in inputGroupAssets.Keys) {
				inputSources.AddRange(inputGroupAssets[groupKey]);
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

			if (!string.IsNullOrEmpty(node.ScriptClassName)) {
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
				node,
				target,
				() => {
					throw new NodeException("No ModifierOperatorData found. please Setup first.", node.Id);
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

			Output(connectionToOutput, outputDict, null);
		}

		
		public void Run (BuildTarget target, 
			NodeData node, 
			ConnectionPointData inputPoint,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<Asset>> inputGroupAssets, 
			List<string> alreadyCached, 
			Action<ConnectionData, Dictionary<string, List<Asset>>, List<string>> Output) 
		{
			if (inputGroupAssets.Keys.Count == 0) {
				return;
			}
			
			// Modifier merges multiple incoming groups into one.
			if (1 < inputGroupAssets.Keys.Count) {
				Debug.LogWarning(node.Name + " Modifier merges incoming group into \"" + inputGroupAssets.Keys.ToList()[0]);
			}

			var groupMergeKey = inputGroupAssets.Keys.ToList()[0];

			// merge all assets into single list.
			var inputSources = new List<Asset>();
			foreach (var groupKey in inputGroupAssets.Keys) {
				inputSources.AddRange(inputGroupAssets[groupKey]);
			}
			
			if (!inputSources.Any()) {
				return;
			}

			Type assetType = TypeUtility.FindTypeOfAsset(inputSources[0].importFrom);

			// check support.
			if (!modifierOperatorTypeMap.ContainsKey(assetType)) {
				throw new NodeException("current incoming Asset Type:" + assetType + " is unsupported.", node.Id);
			}


			// validate saved data.
			ValidateModifiyOperationData(
				node,
				target,
				() => {
					throw new NodeException("No ModifierOperatorData found. please Setup first.", node.Id);
				}
			);
			
			var outputSources = new List<Asset>();

			var g = BuildTargetUtility.TargetToGroup(target);
			var modifyOperatorInstance = CreateModifierOperator(node, g);

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

			Output(connectionToOutput, outputDict, null);
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
			NodeData node,
			BuildTarget target,
			Action noAssetOperationDataFound
		) {
			if( !HasModifierDataFor(node, BuildTargetUtility.TargetToGroup(target), true) ) {
				noAssetOperationDataFound();
			}
		}
			
		private static Type GetTargetType (NodeData node) {
			var data = LoadModifierDataFromDisk(node.Id, BuildTargetUtility.DefaultTarget);
			if(data == string.Empty) {
				return null;
			}
			var deserializedDataObject = JsonUtility.FromJson<Modifier>(data);
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
			
		public static bool HasModifierDataFor (NodeData node, BuildTargetGroup targetGroup, bool checkDefault=false) {
			var dataPath = GetModifierDataPath(node.Id, targetGroup);
			if(File.Exists(dataPath)) {
				return true;
			}
			if(checkDefault) {
				dataPath = GetDefaultModifierDataPath(node.Id);
				if(File.Exists(dataPath)) {
					return true;
				}
			}
			return false;
		}

		public static void DeletePlatformData(NodeData node, BuildTargetGroup targetGroup) {
			var platformOpdataPath = GetModifierDataPath(node.Id, targetGroup);
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

				var operatorInstance = Activator.CreateInstance(operatorType) as Modifier;

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

		public static void SaveModifierOperatorToDisk(string nodeId, BuildTargetGroup targetPlatform, Modifier op) {

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

		public static Modifier CreateModifierOperator(NodeData node, BuildTargetGroup targetGroup) {

			string data = LoadModifierDataFromDisk(node.Id, targetGroup);
			Type targetType = GetTargetType(node);

			if(targetType != null) {
				Type operatorType = modifierOperatorTypeMap[GetTargetType(node)];
				return JsonUtility.FromJson(data, operatorType) as Modifier;
			} else {
				return null;
			}
		}
	}
}
