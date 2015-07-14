using System;
using System.Collections.Generic;

public class SampleFilter_1 : AssetGraph.FilterBase {

	/**
		実行時/Pre実行時に呼ばれ、オーバーライドすべき感じになる。
		中でOutメソッドを呼ぶと、アウトプット箇所が増える。
	*/
	public override void In (List<string> source, Action<string, List<string>> Out) {
		var the2ndList = new List<string>();

		foreach (var src in source) {
			if (src.StartsWith("SampleFilter_2nd")) {
				the2ndList.Add(src);
			}
		}

		Out("SampleFilter_1_LabelOf1st", source);
		Out("SampleFilter_1_LabelOf2nd", the2ndList);
	}
}