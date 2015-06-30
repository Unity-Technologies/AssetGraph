using UnityEngine;
using UnityEditor;

using System.Collections;

[InitializeOnLoad]
public class EntryPoint {
	public EntryPoint () {
		Debug.Log("仮のエントリーポイント、適当にGUIを出さなくても動くテスト起動用として機能させる。");
		AssetGraphController a = new AssetGraphController();
	}

	
	
}
