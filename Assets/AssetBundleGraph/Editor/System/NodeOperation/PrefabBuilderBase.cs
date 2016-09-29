using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

namespace AssetBundleGraph {

	[AttributeUsage(AttributeTargets.Class)] 
	public class MenuItemName : Attribute {
		public string Name;
		public MenuItemName (string name) {
			Name = name;
		}
	}

	public class PrefabBuilderBase : INodeOperationBase {

		private static  Dictionary<string, string> s_attributeClassNameMap;
		
		public static Dictionary<string, string> GetAttributeClassNameMap () {

			if(s_attributeClassNameMap == null) {
				// attribute name or class name : class name
				s_attributeClassNameMap = new Dictionary<string, string>(); 

				var builders = Assembly
					.GetExecutingAssembly()
					.GetTypes()
					.Where(t => t != typeof(PrefabBuilderBase))
					.Where(t => typeof(PrefabBuilderBase).IsAssignableFrom(t));
				
				foreach (var type in builders) {
					// set attribute-name as key of dict if atribute is exist.
					MenuItemName attr = 
						type.GetCustomAttributes(typeof(MenuItemName), true).FirstOrDefault() as MenuItemName;

					var typename = type.ToString();


					if (attr != null) {
						if (!s_attributeClassNameMap.ContainsKey(attr.Name)) {
							s_attributeClassNameMap[attr.Name] = typename;
						}
					} else {
						s_attributeClassNameMap[typename] = typename;
					}

				}
			}
			return s_attributeClassNameMap;
		}

		public void Setup (BuildTarget target, 
			NodeData node, 
			ConnectionPointData inputPoint,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<Asset>> inputGroupAssets, 
			List<string> alreadyCached, 
			Action<ConnectionData, Dictionary<string, List<Asset>>, List<string>> Output) 
		{
			var invalids = new List<string>();
			foreach (var sources in inputGroupAssets.Values) {
				foreach (var source in sources) {
					if (string.IsNullOrEmpty(source.importFrom)) {
						invalids.Add(source.absoluteAssetPath);
					}
				}
			}

			if (invalids.Any()) {
				throw new NodeException(string.Join(", ", invalids.ToArray()) + " are not imported yet. These assets need to be imported before used to build prefab.", node.Id);
			}

			var prefabOutputDir = FileUtility.EnsurePrefabBuilderCacheDirExists(target, node);				
			var outputDict = new Dictionary<string, List<Asset>>();
			
			foreach (var groupKey in inputGroupAssets.Keys) {
				var inputSources = inputGroupAssets[groupKey];

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
					BuildPrefab(string prefabName) method.
				*/
				Func<string, string> BuildPrefab = (string prefabName) => {
					var newPrefabOutputPath = Path.Combine(prefabOutputDir, prefabName);
					generated.Add(newPrefabOutputPath);
					isBuildPrefabFunctionCalled = true;

					return newPrefabOutputPath;
				};

				ValidateCanCreatePrefab(target, node, groupKey, assets, prefabOutputDir, BuildPrefab);

				if (!isBuildPrefabFunctionCalled) {
					Debug.LogWarning(node.Name +": BuildPrefab delegate was not called. Prefab might not be created properly.");
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
			var cachedItems = new List<string>();
			
			var invalids = new List<string>();
			foreach (var sources in inputGroupAssets.Values) {
				foreach (var source in sources) {
					if (string.IsNullOrEmpty(source.importFrom)) {
						invalids.Add(source.absoluteAssetPath);
					}
				}
			}
			
			if (invalids.Any()) {
				throw new NodeException(string.Join(", ", invalids.ToArray()) + " are not imported yet. These assets need to be imported before used to build prefab.", node.Id);
			}

			var prefabOutputDir = FileUtility.EnsurePrefabBuilderCacheDirExists(target, node);
			var outputDict = new Dictionary<string, List<Asset>>();
			var cachedOrGenerated = new List<string>();

			foreach (var groupKey in inputGroupAssets.Keys) {
				var inputSources = inputGroupAssets[groupKey];

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
					BuildPrefab(GameObject baseObject, string prefabName, bool forceGenerate) method.
				*/
				Func<GameObject, string, bool, string> BuildPrefab = (GameObject baseObject, string prefabName, bool forceGenerate) => {
					var newPrefabOutputPath = Path.Combine(prefabOutputDir, prefabName);
					
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
						Debug.Log(node.Name + " created new prefab: " + newPrefabOutputPath );
					} else {
						// cached.
						cachedItems.Add(newPrefabOutputPath);
						cachedOrGenerated.Add(newPrefabOutputPath);
						Debug.Log(node.Name + " used cached prefab: " + newPrefabOutputPath );
					}

					isBuildPrefabFunctionCalled = true;

					return newPrefabOutputPath;
				};

				/*
					execute inheritee's input method.
				*/
				try {
					CreatePrefab(target, node, groupKey, assets, prefabOutputDir, BuildPrefab);
				} catch (Exception e) {
					Debug.LogError(node.Name + " Error:" + e);
					throw new NodeException(node.Name + " Error:" + e, node.Id);
				}

				if (!isBuildPrefabFunctionCalled) {
					Debug.LogWarning(node.Name +": BuildPrefab delegate was not called. Prefab might not be created properly.");
				}

				/*
					ready assets-output-data from this node to next output.
					it contains "cached" or "generated as prefab" or "else" assets.
					output all assets.
				*/
				var currentAssetsInThisNode = FileUtility.GetFilePathsInFolder(prefabOutputDir);
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
				FileUtility.DeleteFileThenDeleteFolderIfEmpty(unusedCachePath);
			}

			Output(connectionToOutput, outputDict, cachedItems);
		}

		private bool isBuildPrefabFunctionCalled = false;

		public virtual void ValidateCanCreatePrefab (BuildTarget target, NodeData node, string groupKey, List<DepreacatedAssetInfo> sources, string prefabOutputDir, Func<string, string> BuildPrefab) {
			Debug.LogError(node.Name + ":Subclass did not implement \"ValidateCanCreatePrefab ()\" method:" + this);
			throw new NodeException(node.Name + ":Subclass did not implement \"ValidateCanCreatePrefab ()\" method:" + this, node.Id);
		}

		public virtual void CreatePrefab (BuildTarget target, NodeData node, string groupKey, List<DepreacatedAssetInfo> sources, string prefabOutputDir, Func<GameObject, string, bool, string> BuildPrefab) {
			Debug.LogError(node.Name + ":Subclass did not implement \"CreatePrefab ()\" method:" + this);
			throw new NodeException(node.Name + ":Subclass did not implement \"ValidateCanCreatePrefab ()\" method:" + this, node.Id);
		}


		public static void ValidatePrefabScriptClassName (string prefabScriptClassName, Action NullOrEmpty, Action PrefabTypeIsNull) {
			if (string.IsNullOrEmpty(prefabScriptClassName)) NullOrEmpty();
			var loadedType = System.Reflection.Assembly.GetExecutingAssembly().CreateInstance(prefabScriptClassName);
			if (loadedType == null) PrefabTypeIsNull();
		}
	}
}