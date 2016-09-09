using System;

using UnityEngine;

namespace AssetBundleGraph {
	[Serializable] public class ConnectionPointGUI {

		[SerializeField] public string pointId;
		[SerializeField] public string label;
		[SerializeField] public bool isInput;
		[SerializeField] public int orderPriority;
		[SerializeField] public bool showLabel;

		public bool isOutput {
			get {
				return !isInput;
			}
		}
		
		[SerializeField] public Rect buttonRect;
		[SerializeField] public string buttonStyle;

		public ConnectionPointGUI (string pointId, string label, bool input) {
			this.pointId = pointId;
			this.label = label;
			this.isInput = input;
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

        public static ConnectionPointGUI InputPoint (string label) {
			return new ConnectionPointGUI(AssetBundleGraphSettings.NODE_INPUTPOINT_FIXED_LABEL, label, true);
        }

		public static ConnectionPointGUI InputPoint (string pointId, string label) {
			return new ConnectionPointGUI(pointId, label, true);
		}

		public static ConnectionPointGUI OutputPoint (string pointId, string label) {
			return new ConnectionPointGUI(pointId, label, false);
		}

		public static ConnectionPointGUI InputPoint (ConnectionPointData data) {
			return new ConnectionPointGUI(data.Id, data.Label, true);
		}

		public static ConnectionPointGUI OutputPoint (ConnectionPointData data) {
			return new ConnectionPointGUI(data.Id, data.Label, false);
		}
    }
}