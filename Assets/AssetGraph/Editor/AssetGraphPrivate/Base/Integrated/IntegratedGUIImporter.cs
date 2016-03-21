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
			
			var outputDict = new Dictionary<string, List<InternalAssetData>>();

			var first = true;
			
			// shrink group to 1 group.
			if (1 < groupedSources.Keys.Count) Debug.LogWarning("importer shrinking group to \"" + groupedSources.Keys.ToList()[0] + "\" forcely.");

			var inputSources = new List<InternalAssetData>();
			foreach (var groupKey in groupedSources.Keys) {
				inputSources.AddRange(groupedSources[groupKey]);
			}
				
			var assumedImportedAssetDatas = new List<InternalAssetData>();
			

			var samplingDirectoryPath = FileController.PathCombine(AssetGraphSettings.IMPORTER_SAMPLING_PLACE, nodeId, importerPackage);
			ValidateImportSample(samplingDirectoryPath,
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
			

				if (alreadyImported.Any()) Debug.LogError("importer:" + string.Join(", ", alreadyImported.ToArray()) + " are already imported.");
				if (ignoredResource.Any()) Debug.LogError("importer:" + string.Join(", ", ignoredResource.ToArray()) + " are ignored.");

				outputDict[groupedSources.Keys.ToList()[0]] = assumedImportedAssetDatas;
			}

			Output(nodeId, labelToNext, outputDict, new List<string>());
		}
		
		public void Run (string nodeId, string labelToNext, string package, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			var usedCache = new List<string>();
			
			
			var outputDict = new Dictionary<string, List<InternalAssetData>>();


			// caution if import setting file is exists already or not.
			var samplingDirectoryPath = FileController.PathCombine(AssetGraphSettings.IMPORTER_SAMPLING_PLACE, nodeId, importerPackage);
			
			var sampleAssetPath = string.Empty;
			ValidateImportSample(samplingDirectoryPath,
				(string noSampleFolder) => {
					Debug.LogWarning("importer:" + noSampleFolder);
				},
				(string noSampleFile) => {
					throw new Exception("importer error:" + noSampleFile);
				},
				(string samplePath) => {
					Debug.Log("using import setting:" + samplePath);
					sampleAssetPath = samplePath;
				},
				(string tooManysample) => {
					throw new Exception("importer error:" + tooManysample);
				}
			);
			

			// ready.
			AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
			

			// construct import path from package info. 
			// importer's package is complicated.
			// 1. importer uses their own package informatiom.
			// 2. but imported assets are located at platform-package combined path.(same as other node.)
			// this is comes from the spec: importer node contains platform settings in themselves.
			var nodeDirectoryPath = FileController.PathCombine(AssetGraphSettings.IMPORTER_CACHE_PLACE, nodeId, GraphStackController.Current_Platform_Package_Folder(package));
			
			// shrink group to 1 group.
			if (1 < groupedSources.Keys.Count) Debug.LogWarning("importer shrinking group to \"" + groupedSources.Keys.ToList()[0] + "\" forcely.");

			var inputSources = new List<InternalAssetData>();
			foreach (var groupKey in groupedSources.Keys) {
				inputSources.AddRange(groupedSources[groupKey]);
			}

			var oldGeneratedRecord = GraphStackController.LoadImporterRecord(nodeId, package);
			var oldRemainGeneratedAssetDict = new Dictionary<string, List<string>>();
			var newGeneratedAssetDict = new Dictionary<string, List<string>>();
			/**
				delete unnecessary cache from node.
			*/
			{
				var latestInputImportAssumePaths = inputSources.Select(latestImportAsset => FileController.PathCombine(nodeDirectoryPath, latestImportAsset.pathUnderSourceBase)).ToList();
				var oldGeneratedRecordPaths = oldGeneratedRecord.Keys.ToList();

				var notExistInLatestButRecordedPaths = oldGeneratedRecordPaths.Except(latestInputImportAssumePaths);
				foreach (var shouldDeleteCachePath in notExistInLatestButRecordedPaths) {
					var shouldDetelePaths = oldGeneratedRecord[shouldDeleteCachePath];
					foreach (var deletingCachePath in shouldDetelePaths) {
						// unbundlize unused imported cached asset.
						var assetImporter = AssetImporter.GetAtPath(deletingCachePath);
		  				assetImporter.assetBundleName = string.Empty;

						FileController.DeleteFileThenDeleteFolderIfEmpty(deletingCachePath);
					}
				}
			}

			/*
				copy all sources from outside to inside of Unity.
				apply importSetting to new file.
			*/
			{
				var samplingAssetImporter = AssetImporter.GetAtPath(sampleAssetPath);
				InternalSamplingImportAdopter.Attach(samplingAssetImporter);
				{
					var alreadyImported = new List<string>();
					var ignoredResource = new List<string>();

					foreach (var inputSource in inputSources) {
						// non absoluteSoucePath -> not imported. generated one. or else.
						if (string.IsNullOrEmpty(inputSource.absoluteSourcePath)) {

							if (!string.IsNullOrEmpty(inputSource.importedPath)) {
								alreadyImported.Add(inputSource.importedPath);
								continue;
							}

							// already imported. should ignore.
							ignoredResource.Add(inputSource.fileNameAndExtension);
							continue;
						}

						// construct imported path.
						var pathUnderSourceBase = inputSource.pathUnderSourceBase;
						var targetFilePath = FileController.PathCombine(nodeDirectoryPath, pathUnderSourceBase);

						// skip if cached.
						if (GraphStackController.IsCached(inputSource, alreadyCached, targetFilePath)) {
							var alreadyImportedPathAndGeneratedPaths = oldGeneratedRecord[targetFilePath];

							// continue using generated info.
							oldRemainGeneratedAssetDict[targetFilePath] = oldGeneratedRecord[targetFilePath];

							usedCache.AddRange(alreadyImportedPathAndGeneratedPaths);
							continue;
						}

						var before = FileController.FilePathsOfFile(targetFilePath);

						/*
							copy files into local.
						*/
						var absoluteFilePath = inputSource.absoluteSourcePath;
						FileController.CopyFileFromGlobalToLocal(absoluteFilePath, targetFilePath);					

						AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);

						var after = FileController.FilePathsOfFile(targetFilePath);

						/*
							record relationship of imported file & generated files.
						*/
						var diff = after.Except(before).Where(path => !GraphStackController.IsMetaFile(path)).ToList();
						newGeneratedAssetDict[targetFilePath] = diff;
					}

					if (alreadyImported.Any()) Debug.LogError("importer:" + string.Join(", ", alreadyImported.ToArray()) + " are already imported.");
					if (ignoredResource.Any()) Debug.LogError("importer:" + string.Join(", ", ignoredResource.ToArray()) + " are ignored.");

				}
				InternalSamplingImportAdopter.Detach();
			}


			/*
				input sequence is over.
			*/

			// get files, which are imported or cached assets.
			var localFilePathsAfterImport = FileController.FilePathsInFolder(nodeDirectoryPath);

			// modify to local path.
			var localFilePathsWithoutNodeDirectoryPath = localFilePathsAfterImport.Select(path => InternalAssetData.GetPathWithoutBasePath(path, nodeDirectoryPath)).ToList();
			
			
			var outputSources = new List<InternalAssetData>();
			
			/*
				treat all assets inside node.
			*/
			foreach (var newAssetPath in localFilePathsWithoutNodeDirectoryPath) {
				var basePathWithNewAssetPath = InternalAssetData.GetPathWithBasePath(newAssetPath, nodeDirectoryPath);
				
				if (usedCache.Contains(basePathWithNewAssetPath)) {
					// already cached, not new.
					var newInternalAssetData = InternalAssetData.InternalAssetDataGeneratedByImporterOrPrefabricator(
						basePathWithNewAssetPath,
						AssetDatabase.AssetPathToGUID(basePathWithNewAssetPath),
						AssetGraphInternalFunctions.GetAssetType(basePathWithNewAssetPath),
						false,
						false
					);
					outputSources.Add(newInternalAssetData);
				} else {
					// now cached. new resource.
					var newInternalAssetData = InternalAssetData.InternalAssetDataGeneratedByImporterOrPrefabricator(
						basePathWithNewAssetPath,
						AssetDatabase.AssetPathToGUID(basePathWithNewAssetPath),
						AssetGraphInternalFunctions.GetAssetType(basePathWithNewAssetPath),
						true,
						false
					);
					outputSources.Add(newInternalAssetData);
				}
			}

			outputDict[groupedSources.Keys.ToList()[0]] = outputSources;
			

			/*
				merge old remains record & new generated record.	
			*/
			{
				var newAndOldGeneratedAssetDict = new Dictionary<string, List<string>>();

				foreach (var oldRemainGeneratedAssetPath in oldRemainGeneratedAssetDict.Keys) {
					newAndOldGeneratedAssetDict[oldRemainGeneratedAssetPath] = oldRemainGeneratedAssetDict[oldRemainGeneratedAssetPath];
				}
				foreach (var newGeneratedAssetPath in newGeneratedAssetDict.Keys) {
					newAndOldGeneratedAssetDict[newGeneratedAssetPath] = newGeneratedAssetDict[newGeneratedAssetPath];
				}
				GraphStackController.UpdateImporterRecord(nodeId, package, newAndOldGeneratedAssetDict);
			}

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
					.Where(path => !path.EndsWith(AssetGraphSettings.UNITY_METAFILE_EXTENSION))
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
		
		public Type AssumeTypeFromExtension () {
			// no mean. nobody can predict type of asset before import.
			return typeof(UnityEngine.Object);
		}

	}
}