using UnityEngine;
using UnityEditor;

namespace AssetGraph {
	public class OutputPoint : ConnectionPoint {
		public OutputPoint (string id) : base (id, false, true) {}

		public override void UpdatePos (int index, int max, float width, float height) {
			var y = ((height/(max + 1)) * (index + 1)) - AssetGraphGUISettings.OUTPUT_POINT_HEIGHT/2f;
			buttonRect = new Rect(width - AssetGraphGUISettings.OUTPUT_POINT_WIDTH + 1f, y + 1f, AssetGraphGUISettings.OUTPUT_POINT_WIDTH, AssetGraphGUISettings.OUTPUT_POINT_HEIGHT);
		}
	}
}