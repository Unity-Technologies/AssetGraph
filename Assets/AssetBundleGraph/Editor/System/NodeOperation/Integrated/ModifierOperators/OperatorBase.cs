using System;
using UnityEngine;

namespace AssetBundleGraph.ModifierOperators {
	/*
		this class is base class of ModifierOperator.
		every operator should extend this class and implement these virtual methods.
	*/
	[Serializable] public class OperatorBase {
		[SerializeField] public string operatorType;

		public OperatorBase () {}// this class is required for serialization. and reflextion

        public virtual OperatorBase DefaultSetting () {
			throw new Exception("DefaultSetting not implemented by subclass.");
		}

		public virtual bool IsChanged<T> (T asset) {
			throw new Exception("IsChanged not implemented by subclass.");
		}

		public virtual void Modify<T> (T asset) {
			throw new Exception("Modify not implemented by subclass.");
		}

        public virtual void DrawInspector (Action changed) {
			throw new Exception("DrawInspector not implemented by subclass.");
        }
    }
}