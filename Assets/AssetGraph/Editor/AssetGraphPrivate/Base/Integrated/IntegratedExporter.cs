using UnityEngine;

using System;
using System.IO;
using System.Collections.Generic;

namespace AssetGraph {
	public class IntegratedScriptExporter : INodeBase {
		public string exportFilePath;
		
		public void Setup (string nodeId, string labelToNext, List<InternalAssetData> inputSource, Action<string, string, List<InternalAssetData>> Output) {
			Debug.LogWarning("Exporter setup, まだ何もしない。フォルダに何かある場合それをリストアップ、くらいはしてもいいのかもしれない。");
			Debug.LogWarning("その素材がimported ~ prefabricatedの間のノードを通ってる場合のみ扱える。っていうの明示したほうがいい気がする。そこからしかつなげない、っていうのがあればいいよな。");
		}
		
		public void Run (string nodeId, string labelToNext, List<InternalAssetData> inputSource, Action<string, string, List<InternalAssetData>> Output) {
			foreach (var source in inputSource) {
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