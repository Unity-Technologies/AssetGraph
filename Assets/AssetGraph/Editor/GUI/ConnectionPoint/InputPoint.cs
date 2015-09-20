using UnityEngine;
using UnityEditor;

namespace AssetGraph {
	public class InputPoint : ConnectionPoint {
		public InputPoint (string id) : base (id, true, false) {}
		
		public override void UpdatePos (int index, int max, float width, float height) {
			var y = (height - AssetGraphGUISettings.INPUT_POINT_HEIGHT)/2f + 1f;
			buttonRect = new Rect(0,y, AssetGraphGUISettings.INPUT_POINT_WIDTH, AssetGraphGUISettings.INPUT_POINT_HEIGHT);
		}
	}
}