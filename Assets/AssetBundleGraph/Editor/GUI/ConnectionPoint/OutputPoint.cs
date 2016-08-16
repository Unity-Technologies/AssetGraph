using UnityEngine;

namespace AssetBundleGraph {
public class OutputPoint : ConnectionPoint {
		public OutputPoint (string pointId, string label) : base (pointId, label, false, true) {}

		public override void UpdatePos (int index, int max, float width, float height) {
			var y = ((height/(max + 1)) * (index + 1)) - AssetBundleGraphGUISettings.OUTPUT_POINT_HEIGHT/2f;
			buttonRect = new Rect(width - AssetBundleGraphGUISettings.OUTPUT_POINT_WIDTH + 1f, y + 1f, AssetBundleGraphGUISettings.OUTPUT_POINT_WIDTH, AssetBundleGraphGUISettings.OUTPUT_POINT_HEIGHT);
		}
	}
}