using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace AssetGraph {
	
	/**
		IntegratedGUIImportSetting is the class for apply specific setting to already imported files.
	*/
	public class IntegratedGUIImportSetting : INodeBase {
		private readonly string importerPackage;
		public IntegratedGUIImportSetting (string importerPackage) {
			this.importerPackage = importerPackage;
		}

		public void Setup (string nodeId, string labelToNext, string unusedPackageInfo, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			
			var outputDict = new Dictionary<string, List<InternalAssetData>>();

			var first = true;
			
			// shrink group to 1 group.
			if (1 < groupedSources.Keys.Count) Debug.LogWarning("importSetting shrinking group to \"" + groupedSources.Keys.ToList()[0] + "\" forcely.");

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

					EditorUtility.DisplayProgressBar("AssetGraph ImportSetting generating ImporterSetting...", targetFilePath, 0);
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
			

			// ready.
			AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
			

			// construct import path from package info. 
			// importer's package is complicated.
			// 1. importer uses their own package informatiom.
			// 2. but imported assets are located at platform-package combined path.(same as other node.)
			// this is comes from the spec: importer node contains platform settings in themselves.
			var nodeDirectoryPath = FileController.PathCombine(AssetGraphSettings.IMPORTER_CACHE_PLACE, nodeId, GraphStackController.Current_Platform_Package_Folder(package));
			
			// shrink group to 1 group.
			if (1 < groupedSources.Keys.Count) Debug.LogWarning("importSetting shrinking group to \"" + groupedSources.Keys.ToList()[0] + "\" forcely.");

			var inputSources = new List<InternalAssetData>();
			foreach (var groupKey in groupedSources.Keys) {
				inputSources.AddRange(groupedSources[groupKey]);
			}

			/*
				check file & setting.
				if need, apply importSetting to file.
			*/
			{
				var samplingAssetImporter = AssetImporter.GetAtPath(sampleAssetPath);
				var effector = new InternalSamplingImportEffector(samplingAssetImporter);
				{
					var alreadyImported = new List<string>();
					var ignoredResource = new List<string>();

					foreach (var inputSource in inputSources) {
						var importer = AssetImporter.GetAtPath(inputSource.importedPath);
						
						/*
							compare type of import setting effector.
						*/
						var importerTypeStr = importer.GetType().ToString();
						
						if (importerTypeStr != samplingAssetImporter.GetType().ToString()) {
							// mismatched target will be ignored. but already imported. 
							continue;
						}
						
						
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
									Debug.LogError("updated:" + inputSource.importedPath);
								}
								break;
							}
							case "UnityEditor.ModelImporter": {
								var modelImporter = importer as ModelImporter;
								var same = InternalSamplingImportAdopter.IsSameModelSetting(modelImporter, samplingAssetImporter as ModelImporter);
								
								if (!same) {
									effector.ForceOnPreprocessModel(modelImporter);
									Debug.LogError("updated:" + inputSource.importedPath);
								}
								break;
							}
							case "UnityEditor.AudioImporter": {
								var audioImporter = importer as AudioImporter;
								var same = InternalSamplingImportAdopter.IsSameAudioSetting(audioImporter, samplingAssetImporter as AudioImporter);
								
								if (!same) {
									effector.ForceOnPreprocessAudio(audioImporter);
									Debug.LogError("updated:" + inputSource.importedPath);
								}
								break;
							}
							
							default: {
								throw new Exception("unhandled importer type:" + importerTypeStr);
							}
						}
						
						// ここを通過したすべての素材が、どっちにしてもimportedとして扱われていいはず。
						// 比較チェックして、差がなければ変化なし、差があれば変化あり。
						
						// 同様のimporter種でなければスルーっていうのもある。
						// その場合でも、素材は使用される。
					}
					
					if (alreadyImported.Any()) Debug.LogError("importSetting:" + string.Join(", ", alreadyImported.ToArray()) + " are already imported.");
					if (ignoredResource.Any()) Debug.LogError("importSetting:" + string.Join(", ", ignoredResource.ToArray()) + " are ignored.");

				}
			}


			/*
				inputSetting sequence is over.
			*/
			
			var outputSources = new List<InternalAssetData>();
			
			// /*
			// 	treat all assets inside node.
			// */
			// foreach (var newAssetPath in localFilePathsWithoutNodeDirectoryPath) {
			// 	var basePathWithNewAssetPath = InternalAssetData.GetPathWithBasePath(newAssetPath, nodeDirectoryPath);
				
			// 	if (usedCache.Contains(basePathWithNewAssetPath)) {
			// 		// already cached, not new.
			// 		var newInternalAssetData = InternalAssetData.InternalAssetDataGeneratedByImporterOrPrefabricator(
			// 			basePathWithNewAssetPath,
			// 			AssetDatabase.AssetPathToGUID(basePathWithNewAssetPath),
			// 			AssetGraphInternalFunctions.GetAssetType(basePathWithNewAssetPath),
			// 			false,
			// 			false
			// 		);
			// 		outputSources.Add(newInternalAssetData);
			// 	} else {
			// 		// now cached. new resource.
			// 		var newInternalAssetData = InternalAssetData.InternalAssetDataGeneratedByImporterOrPrefabricator(
			// 			basePathWithNewAssetPath,
			// 			AssetDatabase.AssetPathToGUID(basePathWithNewAssetPath),
			// 			AssetGraphInternalFunctions.GetAssetType(basePathWithNewAssetPath),
			// 			true,
			// 			false
			// 		);
			// 		outputSources.Add(newInternalAssetData);
			// 	}
			// }

			outputDict[groupedSources.Keys.ToList()[0]] = outputSources;

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
