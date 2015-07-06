using UnityEngine;

using System;
using System.Collections.Generic;

// Test on compile

public partial class Test {
	// private PlayerContext dummyContext;//なんか必要になりそう
	// private Dictionary<string, PlayerContext> dummyContexts;

	public void RunTests () {
		var tests = new List<Action>();

		// テストを追加する
		tests.Add(this._0_0_SetupFilter);
		tests.Add(this._0_1_RunFilter);
			
		Debug.LogError("test start date:" + DateTime.Now);
		foreach (var test in tests) {
			Setup();
			test();
			Teardown();
		}
	}


	public void Setup () {
		
	}

	public void Teardown () {
		
	}
}