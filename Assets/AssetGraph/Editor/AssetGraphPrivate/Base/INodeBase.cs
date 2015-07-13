using System;
using System.Collections.Generic;

namespace AssetGraph {
	/**
		全てのNodeクラスのインターフェース
	*/
	public interface INodeBase {

		/**
			起動時に走るメソッド
		*/
		void Setup (string nodeId, string labelToNext, List<AssetData> source, Action<string, string, List<AssetData>> Output);

		/**
			実行時に走るメソッド
		*/
		void Run (string nodeId, string labelToNext, List<AssetData> source, Action<string, string, List<AssetData>> Output);
	}
}