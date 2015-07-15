using UnityEngine;

using System;
using System.Collections.Generic;

namespace AssetGraph {
	public class IntegratedLoader : INodeBase {
		public string loadFilePath;
		
		public void Setup (string nodeId, string labelToNext, List<InternalAssetData> inputSource, Action<string, string, List<InternalAssetData>> Output) {
			var outputSource = new List<InternalAssetData>();
			var targetFilePaths = FileController.FilePathsInFolderWithoutMeta(loadFilePath);
			
			foreach (var targetFilePath in targetFilePaths) {
				outputSource.Add(
					InternalAssetData.InternalAssetDataByLoader(
						targetFilePath, 
						loadFilePath
					)
				);
			}

			Output(nodeId, labelToNext, outputSource);
		}
		
		public void Run (string nodeId, string labelToNext, List<InternalAssetData> inputSource, Action<string, string, List<InternalAssetData>> Output) {
			var outputSource = new List<InternalAssetData>();
			var targetFilePaths = FileController.FilePathsInFolderWithoutMeta(loadFilePath);
			
			foreach (var targetFilePath in targetFilePaths) {
				outputSource.Add(
					InternalAssetData.InternalAssetDataByLoader(
						targetFilePath, 
						loadFilePath
					)
				);
			}

			Output(nodeId, labelToNext, outputSource);
		}
	}
}