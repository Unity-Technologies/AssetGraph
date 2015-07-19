using UnityEngine;

using System;
using System.Collections.Generic;

public class SampleFilter : AssetGraph.FilterBase {

	/**
		実行時/Pre実行時に呼ばれ、オーバーライドすべき感じになる。
		中でOutメソッドを呼ぶと、アウトプット箇所が増える。
	*/
	public override void In (List<string> source, Action<string, List<string>> Out) {
		var images = new List<string>();
		var models = new List<string>();
		var bgms = new List<string>();
		var ses = new List<string>();
		
		Out("画像", images);
		Out("モデル", models);
		Out("BGM", bgms);
		Out("SE", ses);
	}
}