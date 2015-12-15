using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace AssetGraph {
	public class IntegratedGUIImporter : INodeBase {
		private readonly string importerPackage;
		public IntegratedGUIImporter (string importerPackage) {
			this.importerPackage = importerPackage;
		}

		public void Setup (string nodeId, string labelToNext, string unusedPackageInfo, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			var samplingDirectoryPath = FileController.PathCombine(AssetGraphSettings.IMPORTER_SAMPLING_PLACE, nodeId, importerPackage);
			var outputDict = new Dictionary<string, List<InternalAssetData>>();

			var first = true;
			
			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];
				
				var assumedImportedAssetDatas = new List<InternalAssetData>();
				
				// caution if file is exists already.
				if (Directory.Exists(samplingDirectoryPath)) {
					var filesInSampling = FileController.FilePathsInFolder(samplingDirectoryPath);
					switch (filesInSampling.Count) {
						case 0: {
							break;
						}
						case 1: {
							first = false;
							break;
						}
						default: {
							first = false;
							break;
						}
					}
				}

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

					var assumedImportedBasePath = inputSource.absoluteSourcePath.Replace(inputSource.sourceBasePath, AssetGraphSettings.IMPORTER_CACHE_PLACE);
					var assumedImportedPath = FileController.PathCombine(assumedImportedBasePath, nodeId);

					var assumedType = AssumeTypeFromExtension();

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

						EditorUtility.DisplayProgressBar("AssetGraph Importer generating ImporterSetting...", targetFilePath, 0);
						FileController.CopyFileFromGlobalToLocal(absoluteFilePath, targetFilePath);
						first = false;
						AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
						EditorUtility.ClearProgressBar();
					}
				}

				if (alreadyImported.Any()) Debug.LogError("importer:" + string.Join(", ", alreadyImported.ToArray()) + " are already imported.");
				if (ignoredResource.Any()) Debug.LogError("importer:" + string.Join(", ", ignoredResource.ToArray()) + " are ignored.");

				outputDict[groupKey] = assumedImportedAssetDatas;
			}

			Output(nodeId, labelToNext, outputDict, new List<string>());
		}
		
		public void Run (string nodeId, string labelToNext, string package, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			var usedCache = new List<string>();
			
			var samplingDirectoryPath = FileController.PathCombine(AssetGraphSettings.IMPORTER_SAMPLING_PLACE, nodeId, importerPackage);
			var outputDict = new Dictionary<string, List<InternalAssetData>>();

			// construct import path from package info. 
			// importer's package is complicated.
			// 1. importer uses their own package informatiom.
			// 2. but imported assets are located at platform-package combined path.(same as other node.)
			// this is comes from the spec: importer node contains platform settings in themselves.
			var nodeDirectoryPath = FileController.PathCombine(AssetGraphSettings.IMPORTER_CACHE_PLACE, nodeId, GraphStackController.Current_Platform_Package_Folder(package));
			
			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];
				
				// caution if file is exists already.
				var sampleAssetPath = string.Empty;
				if (Directory.Exists(samplingDirectoryPath)) {
					var filesInSampling = FileController.FilePathsInFolderOnly1Level(samplingDirectoryPath)
						.Where(path => !path.EndsWith(AssetGraphSettings.UNITY_METAFILE_EXTENSION))
						.ToList();

					switch (filesInSampling.Count) {
						case 0: {
							Debug.LogError("no importSetting file found in ImporterSetting directory:" + samplingDirectoryPath + ", please reload first.");
							return;
						}
						case 1: {
							Debug.Log("using sample:" + filesInSampling[0]);
							sampleAssetPath = filesInSampling[0];
							break;
						}
						default: {
							Debug.LogWarning("too many samples in ImporterSetting directory:" + samplingDirectoryPath);
							return;
						}
					}
				} else {
					Debug.LogWarning("no samples found in ImporterSetting directory:" + samplingDirectoryPath + ", applying default importer settings. If you want to set Importer seting, please Reload and set import setting from the inspector of Importer node.");
				}

				var samplingAssetImporter = AssetImporter.GetAtPath(sampleAssetPath);
				

				AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);


				/*
					copy all sources from outside to inside of Unity.
				*/
				InternalSamplingImportAdopter.Attach(samplingAssetImporter);

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

					var absoluteFilePath = inputSource.absoluteSourcePath;
					var pathUnderSourceBase = inputSource.pathUnderSourceBase;

					var targetFilePath = FileController.PathCombine(nodeDirectoryPath, pathUnderSourceBase);

					// skip if cached.
					if (GraphStackController.IsCached(inputSource, alreadyCached, targetFilePath)) {
						usedCache.Add(targetFilePath);
						continue;
					}

					/*
						copy files into local.
					*/
					FileController.CopyFileFromGlobalToLocal(absoluteFilePath, targetFilePath);					
				}

				if (alreadyImported.Any()) Debug.LogError("importer:" + string.Join(", ", alreadyImported.ToArray()) + " are already imported.");
				if (ignoredResource.Any()) Debug.LogError("importer:" + string.Join(", ", ignoredResource.ToArray()) + " are ignored.");

				AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
				InternalSamplingImportAdopter.Detach();


				// get files, which are imported or cached assets.
				var localFilePathsAfterImport = FileController.FilePathsInFolder(nodeDirectoryPath);

				// modify to local path.
				var localFilePathsWithoutnodeDirectoryPath = localFilePathsAfterImport.Select(path => InternalAssetData.GetPathWithoutBasePath(path, nodeDirectoryPath)).ToList();
				
				
				var outputSources = new List<InternalAssetData>();
				
				/*
					treat all assets inside node.
				*/
				foreach (var newAssetPath in localFilePathsWithoutnodeDirectoryPath) {
					var basePathWithNewAssetPath = InternalAssetData.GetPathWithBasePath(newAssetPath, nodeDirectoryPath);

					if (alreadyCached.Contains(basePathWithNewAssetPath)) {
						// already cached, not new.
						var newInternalAssetData = InternalAssetData.InternalAssetDataGeneratedByImporterOrPrefabricator(
							basePathWithNewAssetPath,
							AssetDatabase.AssetPathToGUID(basePathWithNewAssetPath),
							AssetGraphInternalFunctions.GetAssetType(basePathWithNewAssetPath),
							false
						);
						outputSources.Add(newInternalAssetData);
					} else {
						// now cached. new resource.
						var newInternalAssetData = InternalAssetData.InternalAssetDataGeneratedByImporterOrPrefabricator(
							basePathWithNewAssetPath,
							AssetDatabase.AssetPathToGUID(basePathWithNewAssetPath),
							AssetGraphInternalFunctions.GetAssetType(basePathWithNewAssetPath),
							true
						);
						outputSources.Add(newInternalAssetData);
					}
				}

				outputDict[groupKey] = outputSources;
			}

			Output(nodeId, labelToNext, outputDict, usedCache);
		}
		
		public Type AssumeTypeFromExtension () {
			// no mean. nobody can predict type of asset before import.
			return typeof(UnityEngine.Object);
		}
	}
}