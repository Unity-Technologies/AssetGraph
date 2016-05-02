using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace AssetBundleGraph {
	
	/**
		IntegratedGUIImportSetting is the class for apply specific setting to already imported files.
	*/
	public class IntegratedGUIImportSetting : INodeBase {
		private readonly string importerPackage;
		public IntegratedGUIImportSetting (string importerPackage) {
			this.importerPackage = importerPackage;
		}

		public void Setup (string nodeId, string labelToNext, string unusedPackageInfo, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			
			// reserve importSetting type for limit asset.
			var importSettingSampleType = string.Empty;
			
			
			var outputDict = new Dictionary<string, List<InternalAssetData>>();

			var first = true;
			
			if (groupedSources.Keys.Count == 0) return;
			
			// shrink group to 1 group.
			if (1 < groupedSources.Keys.Count) Debug.LogWarning("importSetting shrinking group to \"" + groupedSources.Keys.ToList()[0] + "\" forcely.");

			var inputSources = new List<InternalAssetData>();
			foreach (var groupKey in groupedSources.Keys) {
				inputSources.AddRange(groupedSources[groupKey]);
			}
				
			var assumedImportedAssetDatas = new List<InternalAssetData>();
			

			var samplingDirectoryPath = FileController.PathCombine(AssetBundleGraphSettings.IMPORTER_SAMPLING_PLACE, nodeId, importerPackage);
			ValidateImportSample(samplingDirectoryPath,
				(string noSampleFolder) => {
					// do nothing. keep importing new asset for sampling.
				},
				(string noSampleFile) => {
					// do nothing. keep importing new asset for sampling.
				},
				(string samplePath) => {
					importSettingSampleType = AssetImporter.GetAtPath(samplePath).GetType().ToString();
					first = false;
				},
				(string tooManysample) => {
					throw new OnNodeException("too many sampling file found. please clear ImportSettingSamples folder.", nodeId);
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
				
				var assumedImportedPath = inputSource.importedPath;
				
				var assumedType = AssetImporter.GetAtPath(assumedImportedPath).GetType();
				var importerTypeStr = assumedType.ToString();
				
				/*
					only texture, model and audio importer is acceptable.
				*/
				switch (importerTypeStr) {
					case "UnityEditor.TextureImporter":
					case "UnityEditor.ModelImporter":
					case "UnityEditor.AudioImporter": {
						break;
					}
					
					default: {
						throw new OnNodeException("unhandled importer type:" + importerTypeStr, nodeId);
					}
				}
				
				var newData = InternalAssetData.InternalAssetDataByImporter(
					inputSource.traceId,
					inputSource.absoluteSourcePath,
					inputSource.sourceBasePath,
					inputSource.fileNameAndExtension,
					inputSource.pathUnderSourceBase,
					assumedImportedPath,
					null,
					assumedType
				);
				assumedImportedAssetDatas.Add(newData);

				if (first) {
					if (!Directory.Exists(samplingDirectoryPath)) Directory.CreateDirectory(samplingDirectoryPath);

					var absoluteFilePath = inputSource.absoluteSourcePath;
					var targetFilePath = FileController.PathCombine(samplingDirectoryPath, inputSource.fileNameAndExtension);

					EditorUtility.DisplayProgressBar("AssetBundleGraph ImportSetting generating ImporterSetting...", targetFilePath, 0);
					FileController.CopyFileFromGlobalToLocal(absoluteFilePath, targetFilePath);
					first = false;
					AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
					EditorUtility.ClearProgressBar();
					
					importSettingSampleType = AssetImporter.GetAtPath(targetFilePath).GetType().ToString();
				} else {
					if (importerTypeStr != importSettingSampleType) {
						throw new OnNodeException("for each importerSetting should be only treat 1 import setting. current import setting type of this node is:" + importSettingSampleType + " inputted error file path:" + inputSource.importedPath, nodeId);
					}
				}
			

				if (alreadyImported.Any()) Debug.LogError("importSetting:" + string.Join(", ", alreadyImported.ToArray()) + " are already imported.");
				if (ignoredResource.Any()) Debug.LogError("importSetting:" + string.Join(", ", ignoredResource.ToArray()) + " are ignored.");

				outputDict[groupedSources.Keys.ToList()[0]] = assumedImportedAssetDatas;
			}

			Output(nodeId, labelToNext, outputDict, new List<string>());
		}
		
		public void Run (string nodeId, string labelToNext, string package, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			var usedCache = new List<string>();
			
			var outputDict = new Dictionary<string, List<InternalAssetData>>();


			// caution if import setting file is exists already or not.
			var samplingDirectoryPath = FileController.PathCombine(AssetBundleGraphSettings.IMPORTER_SAMPLING_PLACE, nodeId, importerPackage);
			
			var sampleAssetPath = string.Empty;
			ValidateImportSample(samplingDirectoryPath,
				(string noSampleFolder) => {
					Debug.LogWarning("importSetting:" + noSampleFolder);
				},
				(string noSampleFile) => {
					throw new Exception("importSetting error:" + noSampleFile);
				},
				(string samplePath) => {
					Debug.Log("using import setting:" + samplePath);
					sampleAssetPath = samplePath;
				},
				(string tooManysample) => {
					throw new Exception("importSetting error:" + tooManysample);
				}
			);
			
			if (groupedSources.Keys.Count == 0) return;
			
			var the1stGroupKey = groupedSources.Keys.ToList()[0];
			
			
			// shrink group to 1 group.
			if (1 < groupedSources.Keys.Count) Debug.LogWarning("importSetting shrinking group to \"" + the1stGroupKey + "\" forcely.");

			var inputSources = new List<InternalAssetData>();
			foreach (var groupKey in groupedSources.Keys) {
				inputSources.AddRange(groupedSources[groupKey]);
			}
			
			var importSetOveredAssetsAndUpdatedFlagDict = new Dictionary<InternalAssetData, bool>();
			
			/*
				check file & setting.
				if need, apply importSetting to file.
			*/
			var samplingAssetImporter = AssetImporter.GetAtPath(sampleAssetPath);
			var effector = new InternalSamplingImportEffector(samplingAssetImporter);
			var samplingAssetImporterTypeStr = samplingAssetImporter.GetType().ToString();
			
			foreach (var inputSource in inputSources) {
				var importer = AssetImporter.GetAtPath(inputSource.importedPath);
				
				/*
					compare type of import setting effector.
				*/
				var importerTypeStr = importer.GetType().ToString();
				
				
				if (importerTypeStr != samplingAssetImporterTypeStr) {
					throw new OnNodeException("for each importerSetting should be only treat 1 import setting. current import setting type of this node is:" + samplingAssetImporterTypeStr + " inputted error file path:" + inputSource.importedPath, nodeId);
				}
				
				importSetOveredAssetsAndUpdatedFlagDict[inputSource] = false;
				/*
					kind of importer is matched.
					check setting then apply setting or no changed.
				*/
				switch (importerTypeStr) {
					case "UnityEditor.TextureImporter": {
						var texImporter = importer as TextureImporter;
						var same = InternalSamplingImportAdopter.IsSameTextureSetting(texImporter, samplingAssetImporter as TextureImporter);
						
						if (!same) {
							effector.ForceOnPreprocessTexture(texImporter);
							importSetOveredAssetsAndUpdatedFlagDict[inputSource] = true;
						}
						break;
					}
					case "UnityEditor.ModelImporter": {
						var modelImporter = importer as ModelImporter;
						var same = InternalSamplingImportAdopter.IsSameModelSetting(modelImporter, samplingAssetImporter as ModelImporter);
						var data = AssetDatabase.LoadAssetAtPath(inputSource.importedPath, inputSource.assetType);
						
						if (!same) {
							effector.ForceOnPreprocessModel(modelImporter);
							importSetOveredAssetsAndUpdatedFlagDict[inputSource] = true;
						}
						break;
					}
					case "UnityEditor.AudioImporter": {
						var audioImporter = importer as AudioImporter;
						var same = InternalSamplingImportAdopter.IsSameAudioSetting(audioImporter, samplingAssetImporter as AudioImporter);
						
						if (!same) {
							effector.ForceOnPreprocessAudio(audioImporter);
							importSetOveredAssetsAndUpdatedFlagDict[inputSource] = true;
						}
						break;
					}
					
					default: {
						throw new OnNodeException("unhandled importer type:" + importerTypeStr, nodeId);
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

		public static void ValidateImportSample (string samplePath, 
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
