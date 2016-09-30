using UnityEngine;

using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;

namespace AssetBundleGraph {
	public class IntegratedGUILoader : INodeOperationBase {
		public void Setup (BuildTarget target, 
			NodeData node, 
			ConnectionPointData inputPoint,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<Asset>> inputGroupAssets, 
			List<string> alreadyCached, 
			Action<ConnectionData, Dictionary<string, List<Asset>>, List<string>> Output) 
		{
			ValidateLoadPath(
				node.LoaderLoadPath[target],
				node.GetLoaderFullLoadPath(target),
				() => {
					//can be empty
					//throw new NodeException(node.Name + ": Load Path is empty.", node.Id);
				}, 
				() => {
					throw new NodeException(node.Name + ": Directory not found: " + node.GetLoaderFullLoadPath(target), node.Id);
				}
			);

			// SOMEWHERE_FULLPATH/PROJECT_FOLDER/Assets/
			var assetsFolderPath = Application.dataPath + AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR;
			
			var outputSource = new List<Asset>();
			var targetFilePaths = FileUtility.GetFilePathsInFolder(node.GetLoaderFullLoadPath(target));

			foreach (var targetFilePath in targetFilePaths) {

				if(targetFilePath.Contains(AssetBundleGraphSettings.ASSETBUNDLEGRAPH_PATH)) {
					continue;
				}

				// already contained into Assets/ folder.
				// imported path is Assets/SOMEWHERE_FILE_EXISTS.
				if (targetFilePath.StartsWith(assetsFolderPath)) {
					var relativePath = targetFilePath.Replace(assetsFolderPath, AssetBundleGraphSettings.ASSETS_PATH);
					
					var assetType = TypeUtility.GetTypeOfAsset(relativePath);
					if (assetType == typeof(object)) {
						continue;
					}

					outputSource.Add(Asset.CreateNewAssetFromLoader(targetFilePath, relativePath));
					continue;
				}

				throw new NodeException(node.Name + ": Invalid Load Path. Path must start with Assets/", node.Name);
			}

			var outputDir = new Dictionary<string, List<Asset>> {
				{"0", outputSource}
			};

			Output(connectionToOutput, outputDir, null);
		}
		
		public void Run (BuildTarget target, 
			NodeData node, 
			ConnectionPointData inputPoint,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<Asset>> inputGroupAssets, 
			List<string> alreadyCached, 
			Action<ConnectionData, Dictionary<string, List<Asset>>, List<string>> Output) 
		{
			// SOMEWHERE_FULLPATH/PROJECT_FOLDER/Assets/
			var assetsFolderPath = Application.dataPath + AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR;
			
			var outputSource = new List<Asset>();
			try {
				var targetFilePaths = FileUtility.GetFilePathsInFolder(node.GetLoaderFullLoadPath(target));
				
				foreach (var targetFilePath in targetFilePaths) {

					if(targetFilePath.Contains(AssetBundleGraphSettings.ASSETBUNDLEGRAPH_PATH)) {
						continue;
					}

					// already contained into Assets/ folder.
					// imported path is Assets/SOMEWHERE_FILE_EXISTS.
					if (targetFilePath.StartsWith(assetsFolderPath)) {
						var importFrom = targetFilePath.Replace(assetsFolderPath, AssetBundleGraphSettings.ASSETS_PATH);
						
						outputSource.Add(Asset.CreateNewAssetFromLoader(targetFilePath, importFrom));
						continue;
					}
					
					throw new AssetBundleGraphSetupException(node.Name + ": Invalid target file path. Path needs to be set under Assets/ :" + targetFilePath);
				}
				
				var outputDir = new Dictionary<string, List<Asset>> {
					{"0", outputSource}
				};

				Output(connectionToOutput, outputDir, null);
			} catch (Exception e) {
				throw new NodeException(e.Message, node.Id);
			}
		}

		public static void ValidateLoadPath (string currentLoadPath, string combinedPath, Action NullOrEmpty, Action NotExist) {
			if (string.IsNullOrEmpty(currentLoadPath)) NullOrEmpty();
			if (!Directory.Exists(combinedPath)) NotExist();
		}
	}
}