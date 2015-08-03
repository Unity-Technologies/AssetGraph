using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;

namespace AssetGraph {
	public class IntegreatedGUIGrouping : INodeBase {
		private readonly string groupingKeyword;

		public IntegreatedGUIGrouping (string groupingKeyword) {
			this.groupingKeyword = groupingKeyword;
		}

		public void Setup (string nodeId, string noUseLabel, Dictionary<string, List<InternalAssetData>> groupedSources, Action<string, string, Dictionary<string, List<InternalAssetData>>> MultiOutputs) {
			Debug.LogError("辞書を渡して実行する。セットアップの場合は切り分けオンリーかな。 groupingKeyword:" + groupingKeyword);

			// @があったら、とかかな。パスの@部分をワイルドカードにして正規表現。
			// 先にコード側書いたほうがいいかもね。

			// var groupedDict = new Dictionary<string, List<InternalAssetData>>();
			// foreach (var source in inputSources) {
			// 	Debug.LogError("source:" + source.importedPath);
			// 	if (source.importedPath.Contains()) 
			// }
		}

		public void Run (string nodeId, string noUseLabel, Dictionary<string, List<InternalAssetData>> groupedSources, Action<string, string, Dictionary<string, List<InternalAssetData>>> MultiOutputs) {
			Debug.LogError("辞書を渡して実行する。セットアップの場合は切り分けオンリーかな。2 groupingKeyword:" + groupingKeyword);
			// Outputsを呼ぶ
		}
	}
}