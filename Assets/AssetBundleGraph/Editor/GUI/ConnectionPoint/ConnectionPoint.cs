using System;

using UnityEngine;

namespace AssetBundleGraph {
	[Serializable] public class ConnectionPoint {
		[SerializeField] public string pointId;
		[SerializeField] public string label;
		[SerializeField] public bool isInput;
		[SerializeField] public bool isOutput;
		
		[SerializeField] public Rect buttonRect;
		[SerializeField] public string buttonStyle;

		public ConnectionPoint (string pointId, string label, bool input, bool output) {
			this.pointId = pointId;
			this.label = label;
			this.isInput = input;
			this.isOutput = output;
		}

		public void UpdatePos (int index, int max, float width, float height) {
			if (isInput) {
				var y = ((height/(max + 1)) * (index + 1)) - AssetBundleGraphGUISettings.INPUT_POINT_HEIGHT/2f;
				buttonRect = new Rect(0,y, AssetBundleGraphGUISettings.INPUT_POINT_WIDTH, AssetBundleGraphGUISettings.INPUT_POINT_HEIGHT);
			}

			if (isOutput) {
				var y = ((height/(max + 1)) * (index + 1)) - AssetBundleGraphGUISettings.OUTPUT_POINT_HEIGHT/2f;
				buttonRect = new Rect(width - AssetBundleGraphGUISettings.OUTPUT_POINT_WIDTH + 1f, y + 1f, AssetBundleGraphGUISettings.OUTPUT_POINT_WIDTH, AssetBundleGraphGUISettings.OUTPUT_POINT_HEIGHT);
			} 
		}

        public static ConnectionPoint InputPoint (string label) {
			return new ConnectionPoint(AssetBundleGraphSettings.NODE_INPUTPOINT_FIXED_LABEL, label, true, false);
        }

		public static ConnectionPoint InputPoint (string pointId, string label) {
			return new ConnectionPoint(pointId, label, true, false);
		}

		public static ConnectionPoint OutputPoint (string pointId, string label) {
			return new ConnectionPoint(pointId, label, false, true);
        }
    }
}