using System;

using UnityEngine;

namespace AssetBundleGraph {

	/**
	 * ConnectionPointData GUI related field & operations
	 */
	public partial class ConnectionPointData {

		[SerializeField] private Rect buttonRect;
		[SerializeField] private string buttonStyle;

		public Rect Region {
			get {
				return buttonRect;
			}
		}

		// returns rect for outside marker
		public Rect GetGlobalRegion(NodeGUI node) {
			var baseRect = node.Region;
			return new Rect(
				baseRect.x + buttonRect.x,
				baseRect.y + buttonRect.y,
				buttonRect.width,
				buttonRect.height
			);
		}

		// returns rect for connection dot
		public Rect GetGlobalPointRegion(NodeGUI node) {
			if(IsInput) {
				return GetInputPointRect(node);
			} else {
				return GetOutputPointRect(node);
			}
		}

		public Vector2 GetGlobalPosition(NodeGUI node) {
			var x = 0f;
			var y = 0f;

			var baseRect = node.Region;

			if (IsInput) {
				x = baseRect.x;
				y = baseRect.y + buttonRect.y + (buttonRect.height / 2f) - 1f;
			}

			if (IsOutput) {
				x = baseRect.x + baseRect.width;
				y = baseRect.y + buttonRect.y + (buttonRect.height / 2f) - 1f;
			}

			return new Vector2(x, y);
		}

		public void UpdateRegion (NodeGUI node, int index, int max) {
			var parentRegion = node.Region;
			if(IsInput){
				buttonRect = new Rect(
					0,
					((parentRegion.height/(max + 1)) * (index + 1)) - AssetBundleGraphGUISettings.INPUT_POINT_HEIGHT/2f, 
					AssetBundleGraphGUISettings.INPUT_POINT_WIDTH, 
					AssetBundleGraphGUISettings.INPUT_POINT_HEIGHT);
			} else {
				var y = ((parentRegion.height/(max + 1)) * (index + 1)) - AssetBundleGraphGUISettings.OUTPUT_POINT_HEIGHT/2f;
				buttonRect = new Rect(
					parentRegion.width - AssetBundleGraphGUISettings.OUTPUT_POINT_WIDTH + 1f, 
					y + 1f, 
					AssetBundleGraphGUISettings.OUTPUT_POINT_WIDTH, 
					AssetBundleGraphGUISettings.OUTPUT_POINT_HEIGHT);
			}
		}

		private Rect GetOutputPointRect (NodeGUI node) {
			var baseRect = node.Region;
			return new Rect(
				baseRect.x + baseRect.width - 8f, 
				baseRect.y + buttonRect.y + 1f, 
				AssetBundleGraphGUISettings.CONNECTION_POINT_MARK_SIZE, 
				AssetBundleGraphGUISettings.CONNECTION_POINT_MARK_SIZE
			);
		}

		private Rect GetInputPointRect (NodeGUI node) {
			var baseRect = node.Region;
			return new Rect(
				baseRect.x - 2f, 
				baseRect.y + buttonRect.y + 3f, 
				AssetBundleGraphGUISettings.CONNECTION_POINT_MARK_SIZE, 
				AssetBundleGraphGUISettings.CONNECTION_POINT_MARK_SIZE
			);
		}
	}
}
