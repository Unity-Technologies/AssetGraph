using System;

using UnityEngine;
using UnityEditor;

namespace AssetGraph {
	[Serializable] public class ConnectionPoint {
		[SerializeField] public string label;
		[SerializeField] public bool isInput;
		[SerializeField] public bool isOutput;
		
		[SerializeField] public Rect buttonRect;
		[SerializeField] public string buttonStyle;

		public ConnectionPoint (string label, bool input, bool output) {
			this.label = label;
			this.isInput = input;
			this.isOutput = output;
		}

		public virtual void UpdatePos (int index, int max, float width, float height) {}

		public bool ContainsPosition (Vector2 localPos) {
			if (buttonRect.x < localPos.x && 
				localPos.x < buttonRect.x + buttonRect.width &&
				buttonRect.y < localPos.y && 
				localPos.y < buttonRect.y + buttonRect.height) {
				return true;
			}
			return false;
		}

		public virtual void ResetView () {
			buttonStyle = string.Empty;
		}
	}
}