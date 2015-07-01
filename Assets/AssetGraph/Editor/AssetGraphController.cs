using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;

using MiniJSONForAssetGraph;
 

/**
	コントロールを行う本体。
	GUIとかからの通知も受けるのかな。
	試験中。GUIの中で動く形になるんだと思う。
*/
public class AssetGraph : EditorWindow {

	List<Rect> nodeList;

	/**
		Menu item for AssetGraph.
	*/   
	[MenuItem("Window/AssetGraph")]
	static void ShowEditor() {
		AssetGraph editor = EditorWindow.GetWindow<AssetGraph>();
		editor.ReloadGraph();
	}


	public void ReloadGraph() {
		Debug.LogError("グラフの初期化処理を行う。保存されている形式データを読み込むのと、あとはコンパイル済みのデータからその更新を漁る。データ形式は一応JSONでいいや。");
		// var dataSourceFilePath = "適当なAssetGraph以下のフォルダから読み込み";
		// var dataSourceStr = string.Empty;
		// using (var sr = new StreamReader(dataSourceFilePath)) {
		// 	dataSourceStr = sr.ReadToEnd();
		// }
		// var jsonData = Json.Deserialize(dataSourceStr) as Dictionary<string,object>;
		// んで、ここですべてのノードとその枝の情報が手に入るはず。
		// {
		// 	"nodes":[
		// 		{
		// 			"id": "ID0",
		// 			"kind": "source",
		// 			"sourcePath": "なんかフォルダの位置とか一ファイルのパスとか。"
		// 		},
		// 		{
		// 			"id": "ID1",
		// 			"kind": "filter",
		// 			"outputs":[
		// 				{
		// 					"rabel": "ラベル2",
		// 					"to": "ID3"
		// 				}
		// 			]
		// 		},
		// 		{
		// 			"id": "ID2",
		// 			"kind": "importer",
		// 			"": 途中
		// 		}
		// 	]
		// }

		Debug.LogError("この辺で、コンパイル済みのコードからノードのアタッチポイントなどを漁る。");

		Debug.Log("特定のBaseを持ったClassのScriptが来たら着火するようにする。Class情報をILから引っ張って作り出せばいいのでは。");
		nodeList = new List<Rect>();

		{
			nodeList.Add(new Rect(10, 10, 100, 100));
			nodeList.Add(new Rect(10 + 200, 10, 100, 100));
		}
	}


   	/**
		draw GUI
   	*/
	void OnGUI() {
		if (!nodeList.Any()) return;

		// 仮に、1->2を引いてみる

		var startNode = nodeList[0];
		var endNode = nodeList[1];

		Vector3 startPos = new Vector3(startNode.x + startNode.width, startNode.y + startNode.height / 2, 0);
		Vector3 endPos = new Vector3(endNode.x, endNode.y + endNode.height / 2, 0);

		Vector3 startTan = startPos + Vector3.right * 50;
		Vector3 endTan = endPos + Vector3.left * 50;
		Color shadowCol = new Color(0, 0, 0, 0.06f);

		// draw shadow.
		for (int i = 0; i < 3; i++) Handles.DrawBezier(startPos, endPos, startTan, endTan, shadowCol, null, (i + 1) * 5);
		
		Handles.DrawBezier(startPos, endPos, startTan, endTan, Color.black, null, 1);



		BeginWindows();
		
		for (int i = 0; i < nodeList.Count; i++) {
			var node = nodeList[i];
			nodeList[i] = GUI.Window(i, node, DrawNodeWindow, "node_" + i);
		}

		EndWindows();
	}

	/**
		GUI.Windowに渡している、Window描画用の関数
   	*/
	void DrawNodeWindow(int id) {
		GUI.DragWindow();
	}
}