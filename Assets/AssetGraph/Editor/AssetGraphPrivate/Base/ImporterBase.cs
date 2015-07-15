using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace AssetGraph {
	public class ImporterBase : INodeBase {
		public UnityEditor.AssetPostprocessor assetPostprocessor;
		public UnityEditor.AssetImporter assetImporter;
		public string assetPath;
		
		public void Setup (string nodeId, string labelToNext, List<AssetData> inputSources, Action<string, string, List<AssetData>> Output) {
			Debug.LogWarning("importerのsetup、読み込んだファイルのパスをimported扱いにするくらいはすべきかもしれない。");
			Output(nodeId, labelToNext, inputSources);
		}
		
		public void Run (string nodeId, string labelToNext, List<AssetData> inputSources, Action<string, string, List<AssetData>> Output) {
			// import specific place / node's id folder.
			var targetDirectoryPath = Path.Combine(AssetGraphSettings.IMPORTER_TEMP_PLACE, nodeId);
			FileController.RemakeDirectory(targetDirectoryPath);
			AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);

			/*
				ready import resources from outside of Unity to inside of Unity.
			*/
			InternalImporter.Attach(this);
			foreach (var inputSource in inputSources) {
				var absoluteFilePath = inputSource.absoluteSourcePath;
				var pathUnderSourceBase = inputSource.pathUnderSourceBase;

				var targetFilePath = Path.Combine(targetDirectoryPath, pathUnderSourceBase);

				if (File.Exists(targetFilePath)) {
					Debug.LogError("この時点でファイルがダブってる場合どうしようかな、、事前のエラーでここまで見ても意味はないな。");
					throw new Exception("すでに同じファイルがある:" + targetFilePath);
				}

				// copy files into local.
				FileController.CopyFileFromGlobalToLocal(absoluteFilePath, targetFilePath);
			}
			AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
			InternalImporter.Detach(this);
			
			// get files, which are already assets.
			var localFilePathsAfterImport = FileController.FilePathsInFolderWithoutMeta(targetDirectoryPath);

			var localFilePathsWithoutTargetDirectoryPath = localFilePathsAfterImport.Select(path => AssetData.GetPathWithoutBasePath(path, targetDirectoryPath)).ToList();
			
			var outputSources = new List<AssetData>();

			// generate matching between source and imported assets.
			foreach (var localFilePathWithoutTargetDirectoryPath in localFilePathsWithoutTargetDirectoryPath) {
				foreach (var inputtedSourceCandidate in inputSources) {
					var pathsUnderSourceBase = inputtedSourceCandidate.pathUnderSourceBase;

					if (localFilePathWithoutTargetDirectoryPath == pathsUnderSourceBase) {
						var localFilePathWithTargetDirectoryPath = AssetData.GetPathWithBasePath(localFilePathWithoutTargetDirectoryPath, targetDirectoryPath);

						var newAssetData = AssetData.AssetDataByImporter(
							inputtedSourceCandidate.traceId,
							inputtedSourceCandidate.absoluteSourcePath,// /SOMEWHERE_OUTSIDE_OF_UNITY/~
							inputtedSourceCandidate.sourceBasePath,// /SOMEWHERE_OUTSIDE_OF_UNITY/
							inputtedSourceCandidate.fileNameAndExtension,// A.png
							inputtedSourceCandidate.pathUnderSourceBase,// (Temp/nodeId/)~
							localFilePathWithTargetDirectoryPath,// Assets/~
							AssetDatabase.AssetPathToGUID(localFilePathWithTargetDirectoryPath),
							AssetGraphInternalFunctions.GetAssetType(localFilePathWithTargetDirectoryPath)
						);
						outputSources.Add(newAssetData);
					}
				}
			}

			/*
				check if new Assets are generated, trace it.
			*/
			var assetPathsWhichAreAlreadyTraced = outputSources.Select(path => path.pathUnderSourceBase).ToList();
			var assetPathsWhichAreNotTraced = localFilePathsWithoutTargetDirectoryPath.Except(assetPathsWhichAreAlreadyTraced);
			foreach (var newAssetPath in assetPathsWhichAreNotTraced) {
				var basePathWithNewAssetPath = AssetData.GetPathWithBasePath(newAssetPath, targetDirectoryPath);
				var newAssetData = AssetData.AssetDataGeneratedByImporterOrPrefabricatorOrBundlizer(
					basePathWithNewAssetPath,
					AssetDatabase.AssetPathToGUID(basePathWithNewAssetPath),
					AssetGraphInternalFunctions.GetAssetType(basePathWithNewAssetPath)
				);
				outputSources.Add(newAssetData);
			}

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
	}
}