using UnityEngine;

using System;
using System.IO;
using System.Collections.Generic;

namespace AssetGraph {
	public class IntegratedScriptExporter : INodeBase {
		public readonly string exportFilePath;
		
		public IntegratedScriptExporter (string exportFilePath) {
			this.exportFilePath = exportFilePath;
		}

		public void Setup (string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, Action<string, string, Dictionary<string, List<InternalAssetData>>> Output) {
			Debug.LogWarning("Exporter setup, まだ何もしない。フォルダに何かある場合それをリストアップ、くらいはしてもいいのかもしれない。");
			Debug.LogWarning("その素材がimported ~ prefabricatedの間のノードを通ってる場合のみ扱える。っていうの明示したほうがいい気がする。そこからしかつなげない、っていうのがあればいいよな。");
		}
		
		public void Run (string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, Action<string, string, Dictionary<string, List<InternalAssetData>>> Output) {
			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];
				foreach (var source in inputSources) {
					if (!Directory.Exists(exportFilePath)) Directory.CreateDirectory(exportFilePath);
					var destination = Path.Combine(exportFilePath, source.pathUnderConnectionId);
					var parentDir = Directory.GetParent(destination).ToString();

					if (!Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir);

					if (File.Exists(destination)) File.Delete(destination);
					File.Copy(source.importedPath, destination);
				}
			}
		}
	}
}