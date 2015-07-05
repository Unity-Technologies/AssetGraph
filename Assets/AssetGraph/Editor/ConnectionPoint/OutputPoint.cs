using UnityEngine;
using UnityEditor;

namespace AssetGraph {
	public class OutputPoint : ConnectionPoint {
		public OutputPoint (string id) : base (id, false, true) {
			ResetView();
		}

		public override void UpdatePos (int index, int max, float width, float height) {
			var y = ((height/(max + 1)) * (index + 1)) - NodeEditorSettings.POINT_SIZE/2f;
			buttonRect = new Rect(width - NodeEditorSettings.POINT_SIZE,y, NodeEditorSettings.POINT_SIZE, NodeEditorSettings.POINT_SIZE);
		}

		public override void ResetView () {
			buttonStyle = "flow shader out 0";
		}
	}
}