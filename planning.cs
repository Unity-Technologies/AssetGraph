まだダミー。
public class A : FilterBase {
	A (List<string> sources) {
		out("ラベル0", sources);
	}
}

public class B : ImporterBase {
	List<string> AssetGraphPostImport (List<string> sources) {
		out("ラベル1", sources);
		out("ラベル2", sources);
	}
}

public class C :  PrefabricatorBase {
	public Prefabを返す型？ Input (List<string> sources) {
		return Prefabを返す型の素材一覧みたいなやつ。関数呼び出しにできるのがいいのか、多くの値を渡せるのがいいのか、、特定の型を要求するメソッドをラストに書かせるほうが優しい気はする。
	}
}

public class D : BundlizerBase {
	public AssetBundleを構成する要素を返す型？ GenerateAssetBundle (List<string> sources) {
		out("ラベル1", sources);
		out("ラベル2", sources);
	}
}