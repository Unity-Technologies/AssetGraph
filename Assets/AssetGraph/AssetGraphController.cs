using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;
 

/**
	コントロールを行う本体。
	GUIとかからの通知も受けるのかな。
	試験中。GUIの中で動く形になるんだと思う。
*/
public class AssetGraphController : EditorWindow {

	List<Rect> nodeList;

	public AssetGraphController () {
		Debug.LogError("コントローラの初期化");
	}
	
	/**
		Menu item for AssetGraph.
	*/   
	[MenuItem("Window/AssetGraph")]
	static void ShowEditor() {
		NodeEditor editor = EditorWindow.GetWindow<NodeEditor>();
		editor.Init();
	}
   
	public void Init() {
		Debug.Log("特定のBaseを持ったClassのScriptが来たら着火するようにする。Class情報をILから引っ張って作り出せばいいのでは。");
		nodeList = new List<Rect>();

		{
			nodeList.Add(new Rect(10, 10, 100, 100));
			nodeList.Add(new Rect(210, 210, 100, 100));
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
		
		int i = 1;
		foreach (var node in nodeList) {
			GUI.Window(i, node, DrawNodeWindow, "node:" + i);
			i++;
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