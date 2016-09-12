using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

namespace AssetBundleGraph {
	
	public class PrefabricatorBase : INodeOperationBase {
		[AttributeUsage(AttributeTargets.Class)] public class DropdownMenuName : Attribute {
			public string Name;

			public DropdownMenuName () {}
		}
		
		public static Dictionary<string, string> GetPrefabricatorAttrName_ClassNameDict () {
			var prefabricatorCandidateTypes = Assembly
				.GetExecutingAssembly()
				.GetTypes()
				.Where(t => t != typeof(PrefabricatorBase))
				.Where(t => typeof(PrefabricatorBase).IsAssignableFrom(t));
			
			var prefabricatorCandidateAttrName_ClassNameDict = new Dictionary<string, string>();

			foreach (var type in prefabricatorCandidateTypes) {
				var typeNameStr = type.ToString();

				// set attribute-name as key of dict if atribute is exist.
				PrefabricatorBase.DropdownMenuName nameFromAttributeSource = type.GetCustomAttributes(typeof(PrefabricatorBase.DropdownMenuName), true).FirstOrDefault() as PrefabricatorBase.DropdownMenuName;
				if (nameFromAttributeSource != null) {
					var candidateName = nameFromAttributeSource.Name;
					if (!prefabricatorCandidateAttrName_ClassNameDict.ContainsKey(candidateName)) {
						prefabricatorCandidateAttrName_ClassNameDict[candidateName] = typeNameStr;
						continue;
					}
				}

				// if no attribute exist or same attr name is already exist, use type name for key.(they are automatically unique.)
				prefabricatorCandidateAttrName_ClassNameDict[typeNameStr] = typeNameStr;
			}
			return prefabricatorCandidateAttrName_ClassNameDict;
		}

		public static T CreatePrefabricatorNodeOperationInstance<T> (string attrNameOrClassName, string nodeId) where T : INodeOperationBase {
			var attrNameOrClassName_classNameDict = GetPrefabricatorAttrName_ClassNameDict();

			// if user changed their own Prefabricator className or attrName, attrNameOrClassName is already changed and that shoud be change from Inspector of Prefabricator.
			if (!attrNameOrClassName_classNameDict.ContainsKey(attrNameOrClassName)) {
				throw new NodeException("no match className or attribute name found. failed to generate class information of class:" + attrNameOrClassName + " which is based on Type:" + typeof(T), nodeId);
			}

			var nodeScriptInstance = Assembly.GetExecutingAssembly().CreateInstance(attrNameOrClassName_classNameDict[attrNameOrClassName]);
			if (nodeScriptInstance == null) {
				throw new NodeException("failed to generate class information of class:" + attrNameOrClassName + " which is based on Type:" + typeof(T), nodeId);
			}
			return ((T)nodeScriptInstance);
		}

		public void Setup (string nodeName, string nodeId, string connectionIdToNextNode, Dictionary<string, List<Asset>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {			
			var invalids = new List<string>();
			foreach (var sources in groupedSources.Values) {
				foreach (var source in sources) {
					if (string.IsNullOrEmpty(source.importFrom)) {
						invalids.Add(source.absoluteAssetPath);
					}
				}
			}

			if (invalids.Any()) {
				throw new NodeException(string.Join(", ", invalids.ToArray()) + " are not imported yet. These assets need to be imported before prefabricated.", nodeId);
			}
				
			var recommendedPrefabOutputDirectoryPath = FileUtility.PathCombine(AssetBundleGraphSettings.PREFABRICATOR_CACHE_PLACE, nodeId, SystemDataUtility.GetCurrentPlatformKey());				
			var outputDict = new Dictionary<string, List<Asset>>();
			
			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];

				var recommendedPrefabPath = FileUtility.PathCombine(recommendedPrefabOutputDirectoryPath, groupKey);
				if (!recommendedPrefabPath.EndsWith(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR.ToString())) recommendedPrefabPath = recommendedPrefabPath + AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR.ToString();
				
				/*
					ready input resource info for execute. not contains cache in this node.
				*/
				var assets = new List<DepreacatedAssetInfo>();
				foreach (var assetData in inputSources) {
					var assetName = assetData.fileNameAndExtension;
					var assetType = assetData.assetType;
					var assetPath = assetData.importFrom;
					var assetDatabaseId = assetData.assetDatabaseId;
					assets.Add(new DepreacatedAssetInfo(assetName, assetType, assetPath, assetDatabaseId));
				}

				// collect generated prefab path.
				var generated = new List<string>();
				
				/*
					Prefabricate(string prefabName) method.
				*/
				Func<string, string> Prefabricate = (string prefabName) => {
					var newPrefabOutputPath = Path.Combine(recommendedPrefabPath, prefabName);
					generated.Add(newPrefabOutputPath);
					isPrefabricateFunctionCalled = true;

					return newPrefabOutputPath;
				};

				ValidateCanCreatePrefab(nodeName, nodeId, groupKey, assets, recommendedPrefabPath, Prefabricate);

				if (!isPrefabricateFunctionCalled) {
					Debug.LogWarning(nodeName +": Prefabricate delegate was not called. Prefab might not be created properly.");
				}

				foreach (var generatedPrefabPath in generated) {
					var newAsset = Asset.CreateNewAssetWithImportPathAndStatus(
						generatedPrefabPath,
						true,// absolutely new in setup.
						false
					);

					if (!outputDict.ContainsKey(groupKey)) outputDict[groupKey] = new List<Asset>();
					outputDict[groupKey].Add(newAsset);
				}

				/*
					add input sources for next node.
				*/
				if (!outputDict.ContainsKey(groupKey)) outputDict[groupKey] = new List<Asset>();
				outputDict[groupKey].AddRange(inputSources);
			} 				

			Output(nodeId, connectionIdToNextNode, outputDict, new List<string>());

		}

		public void Run (string nodeName, string nodeId, string connectionIdToNextNode, Dictionary<string, List<Asset>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {
			var usedCache = new List<string>();
			
			var invalids = new List<string>();
			foreach (var sources in groupedSources.Values) {
				foreach (var source in sources) {
					if (string.IsNullOrEmpty(source.importFrom)) {
						invalids.Add(source.absoluteAssetPath);
					}
				}
			}
			
			if (invalids.Any()) {
				throw new NodeException(string.Join(", ", invalids.ToArray()) + " are not imported yet. These assets need to be imported before prefabricated.", nodeId);
			}
			
			var recommendedPrefabOutputDirectoryPath = FileUtility.PathCombine(AssetBundleGraphSettings.PREFABRICATOR_CACHE_PLACE, nodeId, SystemDataUtility.GetCurrentPlatformKey());
			
			var outputDict = new Dictionary<string, List<Asset>>();
			var cachedOrGenerated = new List<string>();

			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];

				var recommendedPrefabPath = FileUtility.PathCombine(recommendedPrefabOutputDirectoryPath, groupKey);
				if (!recommendedPrefabPath.EndsWith(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR.ToString())) recommendedPrefabPath = recommendedPrefabPath + AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR.ToString();
				
				/*
					ready input resource info for execute. not contains cache in this node.
				*/
				var assets = new List<DepreacatedAssetInfo>();
				foreach (var assetData in inputSources) {
					var assetName = assetData.fileNameAndExtension;
					var assetType = assetData.assetType;
					var assetPath = assetData.importFrom;
					var assetDatabaseId = assetData.assetDatabaseId;
					assets.Add(new DepreacatedAssetInfo(assetName, assetType, assetPath, assetDatabaseId));
				}

				// collect generated prefab path.
				var generated = new List<string>();
				var outputSources = new List<Asset>();
				
				
				/*
					Prefabricate(GameObject baseObject, string prefabName, bool forceGenerate) method.
				*/
				Func<GameObject, string, bool, string> Prefabricate = (GameObject baseObject, string prefabName, bool forceGenerate) => {
					var newPrefabOutputPath = Path.Combine(recommendedPrefabPath, prefabName);
					
					if (forceGenerate || !SystemDataUtility.IsAllAssetsCachedAndUpdated(inputSources, alreadyCached, newPrefabOutputPath)) {
						// not cached, create new.
						UnityEngine.Object prefabFile = PrefabUtility.CreateEmptyPrefab(newPrefabOutputPath);
					
						// export prefab data.
						PrefabUtility.ReplacePrefab(baseObject, prefabFile);

						// save prefab.
						AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
						AssetDatabase.SaveAssets();
						generated.Add(newPrefabOutputPath);
						cachedOrGenerated.Add(newPrefabOutputPath);
						Debug.Log(nodeName + " created new prefab: " + newPrefabOutputPath );
					} else {
						// cached.
						usedCache.Add(newPrefabOutputPath);
						cachedOrGenerated.Add(newPrefabOutputPath);
						Debug.Log(nodeName + " used cached prefab: " + newPrefabOutputPath );
					}

					isPrefabricateFunctionCalled = true;

					return newPrefabOutputPath;
				};

				if (!Directory.Exists(recommendedPrefabPath)) {
					// create recommended directory.
					Directory.CreateDirectory(recommendedPrefabPath);
				}

				/*
					execute inheritee's input method.
				*/
				try {
					CreatePrefab(nodeName, nodeId, groupKey, assets, recommendedPrefabPath, Prefabricate);
				} catch (Exception e) {
					Debug.LogError(nodeName + " Error:" + e);
					throw new NodeException(nodeName + " Error:" + e, nodeId);
				}

				if (!isPrefabricateFunctionCalled) {
					Debug.LogWarning(nodeName +": Prefabricate delegate was not called. Prefab might not be created properly.");
				}

				/*
					ready assets-output-data from this node to next output.
					it contains "cached" or "generated as prefab" or "else" assets.
					output all assets.
				*/
				var currentAssetsInThisNode = FileUtility.FilePathsInFolder(recommendedPrefabPath);
				foreach (var generatedCandidateAssetPath in currentAssetsInThisNode) {
					
					/*
						candidate is new, regenerated prefab.
					*/
					if (generated.Contains(generatedCandidateAssetPath)) {
						var newAsset = Asset.CreateNewAssetWithImportPathAndStatus(
							generatedCandidateAssetPath,
							true,
							false
						);
						outputSources.Add(newAsset);
						continue;
					}
					
					/*
						candidate is not new prefab.
					*/
					var cachedPrefabAsset = Asset.CreateNewAssetWithImportPathAndStatus(
						generatedCandidateAssetPath,
						false,
						false
					);
					outputSources.Add(cachedPrefabAsset);
				}


				/*
					add current resources to next node's resources.
				*/
				outputSources.AddRange(inputSources);

				outputDict[groupKey] = outputSources;
			}

			// delete unused cached prefabs.
			var unusedCachePaths = alreadyCached.Except(cachedOrGenerated).Where(path => !FileUtility.IsMetaFile(path)).ToList();
			foreach (var unusedCachePath in unusedCachePaths) {
				// unbundlize unused prefabricated cached asset.
				var assetImporter = AssetImporter.GetAtPath(unusedCachePath);
  				assetImporter.assetBundleName = string.Empty;

				FileUtility.DeleteFileThenDeleteFolderIfEmpty(unusedCachePath);
			}


			Output(nodeId, connectionIdToNextNode, outputDict, usedCache);
		}

		private bool isPrefabricateFunctionCalled = false;

		public virtual void ValidateCanCreatePrefab (string nodeName, string nodeId, string groupKey, List<DepreacatedAssetInfo> sources, string recommendedPrefabOutputDir, Func<string, string> Prefabricate) {
			Debug.LogError(nodeName + ":Subclass did not implement \"ValidateCanCreatePrefab ()\" method:" + this);
			throw new NodeException(nodeName + ":Subclass did not implement \"ValidateCanCreatePrefab ()\" method:" + this, nodeId);
		}

		public virtual void CreatePrefab (string nodeName, string nodeId, string groupKey, List<DepreacatedAssetInfo> sources, string recommendedPrefabOutputDir, Func<GameObject, string, bool, string> Prefabricate) {
			Debug.LogError(nodeName + ":Subclass did not implement \"CreatePrefab ()\" method:" + this);
			throw new NodeException(nodeName + ":Subclass did not implement \"ValidateCanCreatePrefab ()\" method:" + this, nodeId);
		}


		public static void ValidatePrefabScriptClassName (string prefabScriptClassName, Action NullOrEmpty, Action PrefabTypeIsNull) {
			if (string.IsNullOrEmpty(prefabScriptClassName)) NullOrEmpty();
			var loadedType = System.Reflection.Assembly.GetExecutingAssembly().CreateInstance(prefabScriptClassName);
			if (loadedType == null) PrefabTypeIsNull();
		}
	}
}