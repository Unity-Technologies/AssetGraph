using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;

namespace AssetBundleGraph {
	/*
		string key & string value only.
		because generic dictionary class cannot undo.

		write -> Add(k,v) -> new dict -> keys, values
		read <- ReadonlyDict() <- new dict <- keys, values
	*/
	[Serializable] public class SerializablePseudoDictionary {
		[SerializeField] private List<string> keys = new List<string>();
		[SerializeField] private List<string> values = new List<string>();

		public List<string> Keys {
			get {
				return keys;
			}
		}

		public List<string> Values {
			get {
				return values;
			}
		}

		public SerializablePseudoDictionary (Dictionary<string, string> baseDict) {
			var dict = new Dictionary<string, string>(baseDict);

			keys = dict.Keys.ToList();
			values = dict.Values.ToList();
		}

		public void Add (string key, string val) {
			var dict = new Dictionary<string, string>();
			
			for (var i = 0; i < keys.Count; i++) {
				var currentKey = keys[i];
				var currentVal = values[i];
				dict[currentKey] = currentVal;
			}

			// add or update parameter.
			dict[key] = val;

			keys = new List<string>(dict.Keys);
			values = new List<string>(dict.Values);
		}

		public bool ContainsKey (string key) {
			var dict = new Dictionary<string, string>();
			
			for (var i = 0; i < keys.Count; i++) {
				var currentKey = keys[i];
				var currentVal = values[i];
				dict[currentKey] = currentVal;
			}

			return dict.ContainsKey(key);
		}

		public void Remove (string key) {
			var dict = new Dictionary<string, string>();
			
			for (var i = 0; i < keys.Count; i++) {
				var currentKey = keys[i];
				var currentVal = values[i];
				dict[currentKey] = currentVal;
			}

			dict.Remove(key);
			keys = new List<string>(dict.Keys);
			values = new List<string>(dict.Values);
		}

		public Dictionary<string, string> ReadonlyDict () {
			var dict = new Dictionary<string, string>();
			if (keys == null) return dict;

			for (var i = 0; i < keys.Count; i++) {
				var key = keys[i];
				var val = values[i];
				dict[key] = val;
			}

			return dict;
		}
	}


	/*
		string key & List<string> value only.
		because generic dictionary class cannot undo.

		write -> Add(k,v) -> new dict -> keys, values
		read <- ReadonlyDict() <- new dict <- keys, values
	*/
	[Serializable] public class SerializablePseudoDictionary2 {
		[SerializeField] private List<string> keys = new List<string>();
		[SerializeField] private List<SerializablePseudoDictionary2Value> values = new List<SerializablePseudoDictionary2Value>();

		public SerializablePseudoDictionary2 (Dictionary<string, List<string>> baseDict) {
			var dict = new Dictionary<string,List<string>>(baseDict);

			keys = dict.Keys.ToList();
			values = new List<SerializablePseudoDictionary2Value>();
			foreach (var val in dict.Values.ToList()) {
				values.Add(new SerializablePseudoDictionary2Value(val));
			}
		}

		public void Add (string key, List<string> val) {
			var dict = new Dictionary<string, List<string>>();
			
			for (var i = 0; i < keys.Count; i++) {
				var currentKey = keys[i];
				var currentVal = values[i];
				dict[currentKey] = currentVal.ReadonlyList();
			}

			// add or update parameter.
			dict[key] = val;

			keys = dict.Keys.ToList();
			values = new List<SerializablePseudoDictionary2Value>();
			foreach (var dictVal in dict.Values.ToList()) {
				values.Add(new SerializablePseudoDictionary2Value(dictVal));
			}
		}

		public bool ContainsKey (string key) {
			var dict = new Dictionary<string, List<string>>();
			
			for (var i = 0; i < keys.Count; i++) {
				var currentKey = keys[i];
				var currentVal = values[i];
				dict[currentKey] = currentVal.ReadonlyList();
			}

			return dict.ContainsKey(key);
		}

		public Dictionary<string, List<string>> ReadonlyDict () {
			var dict = new Dictionary<string, List<string>>();
			if (keys == null) return dict;

			for (var i = 0; i < keys.Count; i++) {
				var key = keys[i];
				var val = values[i].ReadonlyList();
				dict[key] = val;
			}

			return dict;
		}

		public void Remove (string key) {
			var dict = new Dictionary<string, List<string>>();

			for (var i = 0; i < keys.Count; i++) {
				var currentKey = keys[i];
				var currentVal = values[i].ReadonlyList();
				dict[currentKey] = currentVal;
			}

			dict.Remove(key);
			keys = dict.Keys.ToList();
			values = new List<SerializablePseudoDictionary2Value>();
			foreach (var dictVal in dict.Values.ToList()) {
				values.Add(new SerializablePseudoDictionary2Value(dictVal));
			}
		}

		[Serializable] public class SerializablePseudoDictionary2Value {
			[SerializeField] private List<string> values = new List<string>();

			public SerializablePseudoDictionary2Value (List<string> sources) {
				foreach (var source in sources) values.Add(source);
			}

			public List<string> ReadonlyList () {
				return values;
			}

		}
	}


	/*
		string key & Vector2 value only.
		because generic dictionary class cannot undo.

		write -> Add(k,v) -> new dict -> keys, values
		read <- ReadonlyDict() <- new dict <- keys, values
	*/
	[Serializable] public class SerializablePseudoDictionary3 {
		[SerializeField] private List<string> keys = new List<string>();
		[SerializeField] private List<Vector2> values = new List<Vector2>();

		public SerializablePseudoDictionary3 (Dictionary<string, Vector2> baseDict) {
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