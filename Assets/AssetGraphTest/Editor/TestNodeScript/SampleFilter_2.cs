using UnityEngine;

using System;
using System.Collections.Generic;

public class SampleFilter_2 : AssetGraph.FilterBase {

	/**
		SampleFilter_1_LabelOf1stにすべてのsourcesが流れる。
		SampleFilter_1_LabelOf2ndには空のリストが流れる。
	*/
	public override void In (List<string> source, Action<string, List<string>> Out) {
		Out("SampleFilter_1_LabelOf1st", source);
		Out("SampleFilter_1_LabelOf2nd", new List<string>());
	}
}