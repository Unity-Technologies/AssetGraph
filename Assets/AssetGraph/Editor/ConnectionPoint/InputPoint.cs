using UnityEngine;
using UnityEditor;

namespace AssetGraph {
	public class InputPoint : ConnectionPoint {
		public InputPoint (string id) : base (id, true, false) {
			ResetView();
		}
		
		public override void UpdatePos (int index, int max, float width, float height) {
			var y = ((height/(max + 1)) * (index + 1)) - NodeEditorSettings.POINT_SIZE/2f;
			buttonRect = new Rect(0,y, NodeEditorSettings.POINT_SIZE, NodeEditorSettings.POINT_SIZE);
		}

		public override void ResetView () {
			buttonStyle = "flow shader in 0";
		}
	}
}