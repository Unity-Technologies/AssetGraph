using UnityEngine;
using UnityEditor;

using AssetGraph;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using MiniJSONForAssetGraph;

// Prefabricatorに関するテスト。Scriptの更新による変化も見てみたい。

public partial class Test {
	public void _2_0_PrefabricatorFromOutside () {
		// 外部からの新規なので、
		Debug.LogError("not yet");
	}

	public void _2_1_PrefabricatorFromOutsideWithMeta () {
		// メタが扱われない限りは無視していいと思うが、そうはいかんだろうな。
		Debug.LogError("not yet");
	}

}