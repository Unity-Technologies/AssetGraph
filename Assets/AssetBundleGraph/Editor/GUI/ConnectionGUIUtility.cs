using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;


namespace AssetBundleGraph {
	public class ConnectionGUIUtility {

		public static Action<ConnectionEvent> ConnectionEventHandler {
			get {
				return ConnectionGUISingleton.s.emitAction;
			}
			set {
				ConnectionGUISingleton.s.emitAction = value;
			}
		}

		public static Texture2D connectionArrowTex {
			get {
				// load shared connection textures
				if( ConnectionGUISingleton.s.connectionArrowTex == null ) {
					ConnectionGUISingleton.s.connectionArrowTex = AssetBundleGraphEditorWindow.LoadTextureFromFile(AssetBundleGraphSettings.GUI.RESOURCE_ARROW);
				}
				return ConnectionGUISingleton.s.connectionArrowTex;
			}
		}

		private class ConnectionGUISingleton {
			public Action<ConnectionEvent> emitAction;

			public Texture2D connectionArrowTex;

			private static ConnectionGUISingleton s_singleton;

			public static ConnectionGUISingleton s {
				get {
					if( s_singleton == null ) {
						s_singleton = new ConnectionGUISingleton();
					}

					return s_singleton;
				}
			}
		}
	}
}
