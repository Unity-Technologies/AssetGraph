using UnityEngine;
using UnityEditor;

public class ImportCharaModel : AssetGraph.ImporterBase {
	public override void AssetGraphOnPreprocessModel () {
		Debug.Log("ImportCharaModel モデル読み込み:" + assetPath);
	}
}