using UnityEngine;

using System;
using System.IO;
using System.Collections.Generic;

namespace AssetGraph {
	public class IntegratedExporter : INodeBase {
		public string exportFilePath;
		
		public void Setup (string nodeId, string labelToNext, List<AssetData> inputSource, Action<string, string, List<AssetData>> Output) {
			Debug.LogWarning("Exporter setup, まだ何もしない。フォルダに何かある場合それをリストアップ、くらいはしてもいいのかもしれない。");
		}
		
		public void Run (string nodeId, string labelToNext, List<AssetData> inputSource, Action<string, string, List<AssetData>> Output) {
			foreach (var source in inputSource) {
				if (!Directory.Exists(exportFilePath)) Directory.CreateDirectory(exportFilePath);
				var destination = Path.Combine(exportFilePath, source.fileNameAndExtension);
				
				if (File.Exists(destination)) File.Delete(destination);
				File.Copy(source.importedPath, destination);
			}
		}
	}
}