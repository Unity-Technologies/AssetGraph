using UnityEngine;

using System;
using System.Collections.Generic;

namespace AssetGraph {
	public class IntegratedLoader : INodeBase {
		public string loadFilePath;
		
		public void Setup (string nodeId, string labelToNext, List<AssetData> inputSource, Action<string, string, List<AssetData>> Output) {
			var outputSource = new List<AssetData>();
			var targetFilePaths = FileController.FilePathsInFolderWithoutMeta(loadFilePath);
			
			foreach (var targetFilePath in targetFilePaths) {
				outputSource.Add(
					AssetData.AssetDataByLoader(
						targetFilePath, 
						loadFilePath
					)
				);
			}

			Output(nodeId, labelToNext, outputSource);
		}
		
		public void Run (string nodeId, string labelToNext, List<AssetData> inputSource, Action<string, string, List<AssetData>> Output) {
			var outputSource = new List<AssetData>();
			var targetFilePaths = FileController.FilePathsInFolderWithoutMeta(loadFilePath);
			
			foreach (var targetFilePath in targetFilePaths) {
				outputSource.Add(
					AssetData.AssetDataByLoader(
						targetFilePath, 
						loadFilePath
					)
				);
			}

			Output(nodeId, labelToNext, outputSource);
		}
	}
}