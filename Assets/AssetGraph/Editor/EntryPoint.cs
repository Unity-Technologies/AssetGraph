using UnityEngine;
using UnityEditor;

using System;

[InitializeOnLoad]
public class EntryPoint {
	static EntryPoint () {
		Debug.Log("仮のエントリーポイント、適当にGUIを出さなくても動くテスト起動用として機能させる。");
		// var a = new AssetGraph.AssetGraph();
		// a.InitializeGraph();
		var runner = new TestRunner();
		runner.RunTests();
		Debug.Log("test done");

	}
}



public class TestRunner {

	public void RunTests () {
		
	}
}