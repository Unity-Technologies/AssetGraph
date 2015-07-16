using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;

public class SampleFilter_3 : AssetGraph.FilterBase {

	/**
		適当な分配を行う
	*/
	public override void In (List<string> source, Action<string, List<string>> Out) {
		var s1 = source.Where(s => s.Contains("/images/")).ToList();
		var s2 = source.Where(s => s.Contains("/models/")).ToList();
		var s3 = source.Where(s => s.Contains("/bgms/")).ToList();
		var s4 = source.Where(s => s.Contains("/ses/")).ToList();

		Out("画像", s1);
		Out("モデル", s2);
		Out("BGM", s3);
		Out("SE", s4);
	}
}