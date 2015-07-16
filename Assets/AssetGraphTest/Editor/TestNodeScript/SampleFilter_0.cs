using UnityEngine;

using System;
using System.Collections.Generic;

public class SampleFilter_0 : AssetGraph.FilterBase {

	/**
		実行時/Pre実行時に呼ばれ、オーバーライドすべき感じになる。
		中でOutメソッドを呼ぶと、アウトプット箇所が増える。
	*/
	public override void In (List<string> source, Action<string, List<string>> Out) {
		var the1stList = new List<string>();
		var the2ndList = new List<string>();

		foreach (var src in source) {
			if (src.StartsWith("A/1st")) {
				the1stList.Add(src);
			}
			if (src.StartsWith("A/2nd")) {
				the2ndList.Add(src);
			}
		}

		Out("SampleFilter_0_LabelOf1st", the1stList);
		Out("SampleFilter_0_LabelOf2nd", the2ndList);
	}
}