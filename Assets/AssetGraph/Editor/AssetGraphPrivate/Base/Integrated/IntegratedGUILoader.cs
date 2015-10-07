using UnityEngine;

using System;
using System.IO;
using System.Collections.Generic;

namespace AssetGraph {
	public class IntegratedGUILoader : INodeBase {
		private readonly string loadFilePath;
		
		public IntegratedGUILoader (string loadFilePath) {
			this.loadFilePath = loadFilePath;
		}

		public void Setup (string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> unused, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			if (string.IsNullOrEmpty(loadFilePath)) {
				Debug.LogWarning("no Load Path set.");
				return;
			}

			if (!Directory.Exists(loadFilePath)) {
				Debug.LogError("no Load Path found, loadFilePath:" + loadFilePath);
				return;
			}

			var outputSource = new List<InternalAssetData>();
			try {
				var targetFilePaths = FileController.FilePathsInFolder(loadFilePath);
				
				foreach (var targetFilePath in targetFilePaths) {
					outputSource.Add(
						InternalAssetData.InternalAssetDataByLoader(
							targetFilePath, 
							loadFilePath
						)
					);
				}

				var outputDir = new Dictionary<string, List<InternalAssetData>> {
					{"0", outputSource}
				};

				Output(nodeId, labelToNext, outputDir, new List<string>());
			} catch (Exception e) {
				Debug.LogError("Loader error:" + e);
			}
		}
		
		public void Run (string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> unused, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			if (string.IsNullOrEmpty(loadFilePath)) {
				Debug.LogWarning("no Load Path set.");
				return;
			}

			if (!Directory.Exists(loadFilePath)) {
				Debug.LogError("no Load Path found, loadFilePath:" + loadFilePath);
				return;
			}
			
			var outputSource = new List<InternalAssetData>();
			try {
				var targetFilePaths = FileController.FilePathsInFolder(loadFilePath);
				
				foreach (var targetFilePath in targetFilePaths) {
					outputSource.Add(
						InternalAssetData.InternalAssetDataByLoader(
							targetFilePath, 
							loadFilePath
						)
					);
				}
				
				var outputDir = new Dictionary<string, List<InternalAssetData>> {
					{"0", outputSource}
				};

				Output(nodeId, labelToNext, outputDir, new List<string>());
			} catch (Exception e) {
				Debug.LogError("Loader error:" + e);
			}
		}
	}
}