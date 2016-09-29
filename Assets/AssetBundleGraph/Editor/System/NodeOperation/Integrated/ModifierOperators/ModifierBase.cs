using System;
using UnityEngine;

namespace AssetBundleGraph {
	/*
		this class is base class of ModifierOperator.
		every operator should extend this class and implement these virtual methods.

		2つの課題を解決したい。
		・内部向け実装、Extendを使ってデシリアライズ実装してるので、undoが効かない。
		・外部向け実装、まだハンドラとかが整ってない。Inspectorはだいたい何とかなってる感じ。

	*/
	[Serializable] public class ModifierBase {

		[AttributeUsage(AttributeTargets.Class)] 
		public class MenuItemName : Attribute {
			public string Name;

			public MenuItemName (string name) {
				Name = name;
			}
		}
		
		[SerializeField] public string operatorType;

		public ModifierBase () {}// this class is required for serialization. and reflection

        public virtual ModifierBase DefaultSetting () {
			throw new Exception("should override DefaultSetting method.");
		}

		public virtual bool IsChanged<T> (T asset) {
			throw new Exception("should override IsChanged method.");
		}

		public virtual void Modify<T> (T asset) {
			throw new Exception("should override Modify method.");
		}

        public virtual void DrawInspector (Action changed) {
            throw new Exception("should override DrawInspector method.");
        }
    }
}