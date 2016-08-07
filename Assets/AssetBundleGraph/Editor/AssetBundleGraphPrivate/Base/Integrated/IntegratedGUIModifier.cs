using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace AssetBundleGraph {
	
	/**
		IntegratedGUIModifier is the class for apply specific setting to asset files.

		This node is under development.
	*/
	public class IntegratedGUIModifier : INodeBase {
		
		
		public void Setup (string nodeName, string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			
			var outputDict = new Dictionary<string, List<InternalAssetData>>();

			var first = true;
			
			if (groupedSources.Keys.Count == 0) return;
			
			// shrink group to 1 group.
			if (1 < groupedSources.Keys.Count) Debug.LogWarning("modifierSetting shrinking group to \"" + groupedSources.Keys.ToList()[0] + "\" forcely.");

			var inputSources = new List<InternalAssetData>();
			foreach (var groupKey in groupedSources.Keys) {
				inputSources.AddRange(groupedSources[groupKey]);
			}
				
			var assumedImportedAssetDatas = new List<InternalAssetData>();
			

			var samplingDirectoryPath = FileController.PathCombine(AssetBundleGraphSettings.MODIFIER_SETTINGS_PLACE, nodeId);
			ValidateModifierSample(samplingDirectoryPath,
				(string noSampleFolder) => {
					// do nothing. keep importing new asset for sampling.
				},
				(string noSampleFile) => {
					// do nothing. keep importing new asset for sampling.
				},
				(string samplePath) => {
					first = false;
				},
				(string tooManysample) => {
					first = false;
				}
			);

			var alreadyImported = new List<string>();
			var ignoredResource = new List<string>();

			foreach (var inputSource in inputSources) {
				if (string.IsNullOrEmpty(inputSource.absoluteSourcePath)) {
					if (!string.IsNullOrEmpty(inputSource.importedPath)) {
						alreadyImported.Add(inputSource.importedPath);
						continue;
					}

					ignoredResource.Add(inputSource.fileNameAndExtension);
					continue;
				}
				
				var assumedType = TypeBinder.AssumeTypeOfAsset(inputSource.importedPath);

				var newData = InternalAssetData.InternalAssetDataByImporter(
					inputSource.traceId,
					inputSource.absoluteSourcePath,
					inputSource.sourceBasePath,
					inputSource.fileNameAndExtension,
					inputSource.pathUnderSourceBase,
					inputSource.importedPath,
					null,
					assumedType
				);
				assumedImportedAssetDatas.Add(newData);

				if (first) {
					if (!Directory.Exists(samplingDirectoryPath)) Directory.CreateDirectory(samplingDirectoryPath);

					var absoluteFilePath = inputSource.absoluteSourcePath;
					var targetFilePath = FileController.PathCombine(samplingDirectoryPath, inputSource.fileNameAndExtension);

					EditorUtility.DisplayProgressBar("AssetBundleGraph Modifier generating ModifierSetting...", targetFilePath, 0);
					FileController.CopyFileFromGlobalToLocal(absoluteFilePath, targetFilePath);
					first = false;
					AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
					EditorUtility.ClearProgressBar();
				}
			

				if (alreadyImported.Any()) Debug.LogError("modifierSetting:" + string.Join(", ", alreadyImported.ToArray()) + " are already imported.");
				if (ignoredResource.Any()) Debug.LogError("modifierSetting:" + string.Join(", ", ignoredResource.ToArray()) + " are ignored.");

				outputDict[groupedSources.Keys.ToList()[0]] = assumedImportedAssetDatas;
			}

			Output(nodeId, labelToNext, outputDict, new List<string>());
		}
		
		public void Run (string nodeName, string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			var usedCache = new List<string>();
			
			var outputDict = new Dictionary<string, List<InternalAssetData>>();


			// caution if import setting file is exists already or not.
			var samplingDirectoryPath = FileController.PathCombine(AssetBundleGraphSettings.MODIFIER_SETTINGS_PLACE, nodeId);
			
			var sampleAssetPath = string.Empty;
			ValidateModifierSample(samplingDirectoryPath,
				(string noSampleFolder) => {
					Debug.LogWarning("modifierSetting:" + noSampleFolder);
				},
				(string noSampleFile) => {
					throw new Exception("modifierSetting error:" + noSampleFile);
				},
				(string samplePath) => {
					Debug.Log("using modifier setting:" + samplePath);
					sampleAssetPath = samplePath;
				},
				(string tooManysample) => {
					throw new Exception("modifierSetting error:" + tooManysample);
				}
			);
			
			if (groupedSources.Keys.Count == 0) return;
			
			var the1stGroupKey = groupedSources.Keys.ToList()[0];
			
			
			// shrink group to 1 group.
			if (1 < groupedSources.Keys.Count) Debug.LogWarning("modifierSetting shrinking group to \"" + the1stGroupKey + "\" forcely.");

			var inputSources = new List<InternalAssetData>();
			foreach (var groupKey in groupedSources.Keys) {
				inputSources.AddRange(groupedSources[groupKey]);
			}
			
			var importSetOveredAssetsAndUpdatedFlagDict = new Dictionary<InternalAssetData, bool>();
			
			/*
				check file & setting.
				if need, apply modifierSetting to file.
			*/
			{
				var samplingAssetImporter = AssetImporter.GetAtPath(sampleAssetPath);
//				var effector = new InternalSamplingImportEffector(samplingAssetImporter);
				{
					foreach (var inputSource in inputSources) {
						var importer = AssetImporter.GetAtPath(inputSource.importedPath);
						
						/*
							compare type of import setting effector.
						*/
						var importerTypeStr = importer.GetType().ToString();
						
						if (importerTypeStr != samplingAssetImporter.GetType().ToString()) {
							// mismatched target will be ignored. but already imported.
							importSetOveredAssetsAndUpdatedFlagDict[inputSource] = false; 
							continue;
						}
						importSetOveredAssetsAndUpdatedFlagDict[inputSource] = false;
						
						/*
							kind of importer is matched.
							check setting then apply setting or no changed.
						*/
						switch (importerTypeStr) {
							case "UnityEditor.AssetImporter": {// materials and others... assets which are generated in Unity.

								// Modifier is under development. do nothing in this node yet.

								
								 
								// importSetOveredAssetsAndUpdatedFlagDict[inputSource] = true;
								break;
							}
							default: {
								throw new Exception("unhandled modifier type:" + importerTypeStr);
							}
						}
					}
				}
			}


			/*
				inputSetting sequence is over.
			*/
			
			var outputSources = new List<InternalAssetData>();
			
			
			foreach (var inputAsset in inputSources) {
				var updated = importSetOveredAssetsAndUpdatedFlagDict[inputAsset];
				if (!updated) {
					// already set completed.
					var newInternalAssetData = InternalAssetData.InternalAssetDataGeneratedByImporterOrModifierOrPrefabricator(
						inputAsset.importedPath,
						AssetDatabase.AssetPathToGUID(inputAsset.importedPath),
						AssetBundleGraphInternalFunctions.GetAssetType(inputAsset.importedPath),
						false,// not changed.
						false
					);
					outputSources.Add(newInternalAssetData);
				} else {
					// updated asset.
					var newInternalAssetData = InternalAssetData.InternalAssetDataGeneratedByImporterOrModifierOrPrefabricator(
						inputAsset.importedPath,
						AssetDatabase.AssetPathToGUID(inputAsset.importedPath),
						AssetBundleGraphInternalFunctions.GetAssetType(inputAsset.importedPath),
						true,// changed.
						false
					);
					outputSources.Add(newInternalAssetData);
				}
			}
			
			outputDict[the1stGroupKey] = outputSources;

			Output(nodeId, labelToNext, outputDict, usedCache);
		}

		public static void ValidateModifierSample (string samplePath, 
			Action<string> NoSampleFolderFound, 
			Action<string> NoSampleFound, 
			Action<string> ValidSampleFound,
			Action<string> TooManySampleFound
		) {
			if (Directory.Exists(samplePath)) {
				var filesInSampling = FileController.FilePathsInFolderOnly1Level(samplePath)
					.Where(path => !path.EndsWith(AssetBundleGraphSettings.UNITY_METAFILE_EXTENSION))
					.ToList();

				switch (filesInSampling.Count) {
					case 0: {
						NoSampleFound("no importSetting file found in ImporterSetting directory:" + samplePath + ", please reload first.");
						return;
					}
					case 1: {
						ValidSampleFound(filesInSampling[0]);
						return;
					}
					default: {
						TooManySampleFound("too many samples in ImporterSetting directory:" + samplePath);
						return;
					}
				}
			}

			NoSampleFolderFound("no samples found in ImporterSetting directory:" + samplePath + ", applying default importer settings. If you want to set Importer seting, please Reload and set import setting from the inspector of Importer node.");
		}
		
		
		

	}
}
