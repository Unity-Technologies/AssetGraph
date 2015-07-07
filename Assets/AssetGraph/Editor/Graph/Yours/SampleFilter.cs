using System;
using System.Linq;
using System.Collections.Generic;

public class SampleFilter : AssetGraph.FilterBase {

	/**
		実行時/Pre実行時に呼ばれ、オーバーライドすべき感じになる。
		中でOutメソッドを呼ぶと、アウトプット箇所が増える。
	*/
	public override void In (List<string> source, Action<string, List<string>> Out) {

		Out("LabelOf1st", source.Where(src => src.StartsWith("1st")).ToList());
		Out("LabelOf2nd", source.Where(src => src.StartsWith("2nd")).ToList());

	}
}