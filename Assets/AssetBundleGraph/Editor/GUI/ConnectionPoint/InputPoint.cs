using UnityEngine;

namespace AssetBundleGraph {
	public class InputPoint : ConnectionPoint {
		public InputPoint (string label) : base ("fixedId", label, true, false) {}
		
		public override void UpdatePos (int index, int max, float width, float height) {
			var y = (height - AssetBundleGraphGUISettings.INPUT_POINT_HEIGHT)/2f + 1f;
			buttonRect = new Rect(0,y, AssetBundleGraphGUISettings.INPUT_POINT_WIDTH, AssetBundleGraphGUISettings.INPUT_POINT_HEIGHT);
		}
	}
}