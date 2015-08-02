using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace AssetGraph {
	public class IntegreatedGUIImporter : INodeBase {
		private readonly Dictionary<string, object> importControlDict;
		
		public UnityEditor.AssetPostprocessor assetPostprocessor;
		public UnityEditor.AssetImporter assetImporter;
		public string assetPath;

		public IntegreatedGUIImporter (Dictionary<string, object> importControlDict) {
			this.importControlDict = importControlDict;
		}
		
		public void Setup (string nodeId, string labelToNext, List<InternalAssetData> inputSources, Action<string, string, List<InternalAssetData>> Output) {
			var samplingDirectoryPath = Path.Combine(AssetGraphSettings.IMPORTER_SAMPLING_PLACE, nodeId);

			var assumedImportedAssetDatas = new List<InternalAssetData>();
			
			var first = true;

			// caution if file is exists already.
			if (Directory.Exists(samplingDirectoryPath)) {
				var filesInSampling = FileController.FilePathsInFolderWithoutMeta(samplingDirectoryPath);
				switch (filesInSampling.Count) {
					case 0: {
						Debug.Log("sampling start.");
						break;
					}
					case 1: {
						Debug.Log("sample already exist:" + filesInSampling[0]);
						first = false;
						break;
					}
					default: {
						Debug.LogError("too many samples in samplingDirectoryPath:" + samplingDirectoryPath + ", make sure only 1 file in path:" + samplingDirectoryPath + ", or delete all files in path:" + samplingDirectoryPath + " and reload.");
						first = false;
						break;
					}
				}
			}

			foreach (var inputSource in inputSources) {
				var assumedImportedBasePath = inputSource.absoluteSourcePath.Replace(inputSource.sourceBasePath, AssetGraphSettings.IMPORTER_TEMP_PLACE);
				var assumedImportedPath = Path.Combine(assumedImportedBasePath, nodeId);

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
					var targetFilePath = Path.Combine(samplingDirectoryPath, inputSource.fileNameAndExtension);

					FileController.CopyFileFromGlobalToLocal(absoluteFilePath, targetFilePath);
					first = false;
					Debug.Log("succeeded to sampling:" + targetFilePath);
					AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
				}
			}

			Output(nodeId, labelToNext, assumedImportedAssetDatas);
		}
		
		public void Run (string nodeId, string labelToNext, List<InternalAssetData> inputSources, Action<string, string, List<InternalAssetData>> Output) {
			var samplingDirectoryPath = Path.Combine(AssetGraphSettings.IMPORTER_SAMPLING_PLACE, nodeId);

			// import specific place / node's id folder.
			var targetDirectoryPath = Path.Combine(AssetGraphSettings.IMPORTER_TEMP_PLACE, nodeId);
			FileController.RemakeDirectory(targetDirectoryPath);
			AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);

			
			// caution if file is exists already.
			var sampleAssetPath = string.Empty;
			if (Directory.Exists(samplingDirectoryPath)) {
				var filesInSampling = FileController.FilePathsInFolderWithoutMeta(samplingDirectoryPath);
				switch (filesInSampling.Count) {
					case 0: {
						throw new Exception("no samples found in samplingDirectoryPath:" + samplingDirectoryPath + ", please reload first.");
					}
					case 1: {
						Debug.Log("using sample:" + filesInSampling[0]);
						sampleAssetPath = filesInSampling[0];
						break;
					}
					default: {
						throw new Exception("too many samples in samplingDirectoryPath:" + samplingDirectoryPath);
					}
				}
			} else {
				throw new Exception("no samples found in samplingDirectoryPath:" + samplingDirectoryPath + ", please reload first.");
			}

			/*
				copy all sources from outside to inside of Unity.
			*/
			foreach (var inputSource in inputSources) {
				var absoluteFilePath = inputSource.absoluteSourcePath;
				var pathUnderSourceBase = inputSource.pathUnderSourceBase;

				var targetFilePath = Path.Combine(targetDirectoryPath, pathUnderSourceBase);

				if (File.Exists(targetFilePath)) {
					Debug.LogError("この時点でファイルがダブってる場合どうしようかな、、事前のエラーでここまで見ても意味はないな。2");
					throw new Exception("すでに同じファイルがある2:" + targetFilePath);
				}
				try {
					// copy files into local.
					FileController.CopyFileFromGlobalToLocal(absoluteFilePath, targetFilePath);
				} catch (Exception e) {
					Debug.LogError("IntegratedGUIImporter:" + this + " error:" + e);
				}
			}
			AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
			
			// get files, which are already assets.
			var localFilePathsAfterImport = FileController.FilePathsInFolderWithoutMeta(targetDirectoryPath);

			var localFilePathsWithoutTargetDirectoryPath = localFilePathsAfterImport.Select(path => InternalAssetData.GetPathWithoutBasePath(path, targetDirectoryPath)).ToList();
			
			var outputSources = new List<InternalAssetData>();

			// generate matching between source and imported assets.
			foreach (var localFilePathWithoutTargetDirectoryPath in localFilePathsWithoutTargetDirectoryPath) {
				foreach (var inputtedSourceCandidate in inputSources) {
					var pathsUnderSourceBase = inputtedSourceCandidate.pathUnderSourceBase;

					if (localFilePathWithoutTargetDirectoryPath == pathsUnderSourceBase) {
						var localFilePathWithTargetDirectoryPath = InternalAssetData.GetPathWithBasePath(localFilePathWithoutTargetDirectoryPath, targetDirectoryPath);

						var newInternalAssetData = InternalAssetData.InternalAssetDataByImporter(
							inputtedSourceCandidate.traceId,
							inputtedSourceCandidate.absoluteSourcePath,
							inputtedSourceCandidate.sourceBasePath,
							inputtedSourceCandidate.fileNameAndExtension,
							inputtedSourceCandidate.pathUnderSourceBase,
							localFilePathWithTargetDirectoryPath,
							AssetDatabase.AssetPathToGUID(localFilePathWithTargetDirectoryPath),
							AssetGraphInternalFunctions.GetAssetType(localFilePathWithTargetDirectoryPath)
						);
						outputSources.Add(newInternalAssetData);
					}
				}
			}

			/*
				check if new Assets are generated, trace it.
			*/
			var assetPathsWhichAreAlreadyTraced = outputSources.Select(path => path.pathUnderSourceBase).ToList();
			var assetPathsWhichAreNotTraced = localFilePathsWithoutTargetDirectoryPath.Except(assetPathsWhichAreAlreadyTraced);
			foreach (var newAssetPath in assetPathsWhichAreNotTraced) {
				var basePathWithNewAssetPath = InternalAssetData.GetPathWithBasePath(newAssetPath, targetDirectoryPath);
				var newInternalAssetData = InternalAssetData.InternalAssetDataGeneratedByImporterOrPrefabricatorOrBundlizer(
					basePathWithNewAssetPath,
					AssetDatabase.AssetPathToGUID(basePathWithNewAssetPath),
					AssetGraphInternalFunctions.GetAssetType(basePathWithNewAssetPath)
				);
				outputSources.Add(newInternalAssetData);
			}

			Debug.LogError("該当するSampleの内容をセットする。種類見てないので一個しかないはず、っていう前提。sampleAssetPath:" + sampleAssetPath);
			var samplingAssetImporter = AssetImporter.GetAtPath(sampleAssetPath);

			if (samplingAssetImporter) {
				Debug.Log("succeeded to get importer of Sampling Asset at path:" + sampleAssetPath);
			} else {
				throw new Exception("failed to get importer of Sampling asset at path:" + sampleAssetPath);
			}

			foreach (var asset in outputSources) {
				AdoptSettings(asset, samplingAssetImporter);
			}

			AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);

			Output(nodeId, labelToNext, outputSources);
		}

		/*
			handled import events.
		*/
		public virtual void AssetGraphOnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {}
		public virtual void AssetGraphOnPostprocessGameObjectWithUserProperties (GameObject g, string[] propNames, object[] values) {}
		public virtual void AssetGraphOnPreprocessTexture () {}
		public virtual void AssetGraphOnPostprocessTexture (Texture2D texture) {}
		public virtual void AssetGraphOnPreprocessAudio () {}
		public virtual void AssetGraphOnPostprocessAudio (AudioClip clip) {}
		public virtual void AssetGraphOnPreprocessModel () {}
		public virtual void AssetGraphOnPostprocessModel (GameObject g) {}
		public virtual void AssetGraphOnAssignMaterialModel (Material material, Renderer renderer) {}

		public Type AssumeTypeFromExtension () {
			Debug.LogWarning("もしもこれからimportする型の仮定が、拡張子とかからできれば、どのAssetPostprocessorが起動するのか特定できて、どのimporterがどのメソッドを積めばいいのかwarningとかで示せる。そういうUnityの関数ないっすかね、、2");
			return typeof(UnityEngine.Object);
		}

		private void AdoptSettings (InternalAssetData targetData, AssetImporter importerSourceObj) {
			var typeString = importerSourceObj.GetType().ToString();
			
			switch (typeString) {
				case "UnityEditor.TextureImporter": {
					var importerSource = importerSourceObj as TextureImporter;
					var importer = AssetImporter.GetAtPath(targetData.importedPath) as TextureImporter;

					importer.anisoLevel = importerSource.anisoLevel;
					importer.borderMipmap = importerSource.borderMipmap;
					importer.compressionQuality = importerSource.compressionQuality;
					importer.convertToNormalmap = importerSource.convertToNormalmap;
					importer.fadeout = importerSource.fadeout;
					importer.filterMode = importerSource.filterMode;
					importer.generateCubemap = importerSource.generateCubemap;
					importer.generateMipsInLinearSpace = importerSource.generateMipsInLinearSpace;
					importer.grayscaleToAlpha = importerSource.grayscaleToAlpha;
					importer.heightmapScale = importerSource.heightmapScale;
					importer.isReadable = importerSource.isReadable;
					importer.lightmap = importerSource.lightmap;
					importer.linearTexture = importerSource.linearTexture;
					importer.maxTextureSize = importerSource.maxTextureSize;
					importer.mipMapBias = importerSource.mipMapBias;
					importer.mipmapEnabled = importerSource.mipmapEnabled;
					importer.mipmapFadeDistanceEnd = importerSource.mipmapFadeDistanceEnd;
					importer.mipmapFadeDistanceStart = importerSource.mipmapFadeDistanceStart;
					importer.mipmapFilter = importerSource.mipmapFilter;
					importer.normalmap = importerSource.normalmap;
					importer.normalmapFilter = importerSource.normalmapFilter;
					importer.npotScale = importerSource.npotScale;
					// importer.qualifiesForSpritePacking = importerSource.qualifiesForSpritePacking;
					importer.spriteBorder = importerSource.spriteBorder;
					importer.spriteImportMode = importerSource.spriteImportMode;
					importer.spritePackingTag = importerSource.spritePackingTag;
					importer.spritePivot = importerSource.spritePivot;
					importer.spritePixelsPerUnit = importerSource.spritePixelsPerUnit;
					importer.spritesheet = importerSource.spritesheet;
					importer.textureFormat = importerSource.textureFormat;
					importer.textureType = importerSource.textureType;
					importer.wrapMode = importerSource.wrapMode;
					importer.textureType = importerSource.textureType;

					AssetDatabase.WriteImportSettingsIfDirty(targetData.importedPath);
					break;
				}
				default: {
					Debug.LogError("not yet supported. type:" + typeString);
					break;
				}
			}
		}
	}
}