using System;
using UnityEngine;

namespace AssetBundleGraph.ModifierOperators {
	/*
		this class is base class of ModifierOperator.
		every operator should extend this class and implement these virtual methods.
	*/
	[Serializable] public class OperatorBase {
		[SerializeField] public string dataType;

		public OperatorBase () {}

        public virtual OperatorBase DefaultSetting () {
			throw new Exception("should override DefaultSetting method.");
		}

		public virtual bool IsChanged<T> (T asset) {
			throw new Exception("should override IsChanged method.");
		}

		public virtual void Modify<T> (T asset) {
			throw new Exception("should override Modify method.");
		}
    }
}