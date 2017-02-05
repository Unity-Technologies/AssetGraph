using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;

namespace UnityEngine.AssetBundles.GraphTool {

	/*
		string:Vector2 pseudo dictionary to support Undo
	*/
	[Serializable] public class SerializableVector2Dictionary {
		[SerializeField] private List<string> keys = new List<string>();
		[SerializeField] private List<Vector2> values = new List<Vector2>();

		public SerializableVector2Dictionary (Dictionary<string, Vector2> baseDict) {
			var dict = new Dictionary<string, Vector2>(baseDict);

			keys = dict.Keys.ToList();
			values = dict.Values.ToList();
		}

		public void Add (string key, Vector2 val) {
			var dict = new Dictionary<string, Vector2>();
			
			for (var i = 0; i < keys.Count; i++) {
				var currentKey = keys[i];
				var currentVal = values[i];
				dict[currentKey] = currentVal;
			}

			// add or update parameter.
			dict[key] = val;

			keys = new List<string>(dict.Keys);
			values = new List<Vector2>(dict.Values);
		}

		public bool ContainsKey (string key) {
			var dict = new Dictionary<string, Vector2>();
			
			for (var i = 0; i < keys.Count; i++) {
				var currentKey = keys[i];
				var currentVal = values[i];
				dict[currentKey] = currentVal;
			}

			return dict.ContainsKey(key);
		}

		public void Remove (string key) {
			var dict = new Dictionary<string, Vector2>();
			
			for (var i = 0; i < keys.Count; i++) {
				var currentKey = keys[i];
				var currentVal = values[i];
				dict[currentKey] = currentVal;
			}

			dict.Remove(key);
			keys = new List<string>(dict.Keys);
			values = new List<Vector2>(dict.Values);
		}

		public Dictionary<string, Vector2> ReadonlyDict () {
			var dict = new Dictionary<string, Vector2>();
			if (keys == null) return dict;

			for (var i = 0; i < keys.Count; i++) {
				var key = keys[i];
				var val = values[i];
				dict[key] = val;
			}

			return dict;
		}
	}
}
