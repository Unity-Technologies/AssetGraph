using UnityEngine;
using UnityEditor;

/**
	if you want to import Assets manually,
	Drag & Drop this C# script to AssetGraph.
*/
public class ImportSE : AssetGraph.ImporterBase {
	public override void AssetGraphOnPreprocessAudio () {
		// タイプを受け付ける。Modifier
		// InspectorのSerializeField属性から値の参照を取得し、反映する。
		// receive type constraint
		// color Utility class.
		// 
	}
}