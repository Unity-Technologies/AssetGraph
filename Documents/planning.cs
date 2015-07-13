// まだダミー。

// Source -> A(split) -> B(flow) -> C(merge) -> D(merge) とかある場合、
// 最後のmergeから逆を見て、処理を集計、集合させる必要がある。
// 関数群の集合として扱うことができると楽な感じだな。

// D -> C merge - mergeは即スタック
// C -> B -> A -> Sourceをまず辿ることができる。これでAを通る物事の内容が予約される
// Source - Aから、Aを通過するファイルが決定されて、Bでそのファイルに対しての処理が走る予約が構築される。
// あとは、BC(merge)へと伸びている全ての枝の処理が終わったらCが発生、という形にできればいい。

// 属性みたいなのを持って関数を渡すようにしておくと構築が超楽そう。

// Source -> A1(split) -> B(flow) -> C(merge) -> D(merge) とかある場合、
// 			\A2(split) --------------/

// D -> C merge - mergeは即スタック。
// C -> B -> A1 -> Sourceを辿り、A1の内容からBを決定。Bが終わったら結果のパスをCに積む。
// C -> A2 -> Sourceを辿り、Cに入ってくる内容を決定。
// Cにかかっている要素がすべて解消されたらC開始。

// 属性みたいなのを持って関数を渡すようにしておくと構築が超楽そう。
// 作られた各インスタンスは最終位置を元に逆さの木を作る。

// ☆一個もmergeがない場合どうなるの？　→　走らないとかだと楽だなあ。。。例えばimportしかない場合、それが走るだけみたいな感じなのか、、？せめてExport先を指定してほしいな？

// このへん楽しそうなので作り始めてしまおう。

// ドライラン扱いで一度走らせる、っていうのさえできれば、Filterの「関数を収集する」っていう問題はやらないで良い方向で回避できそう。
// もともと編集性がないので問題にはならないんだし。


// 内部的には、以下の順番で要素を構築すると良さそう。

// ・保存済みのデータを読み込む
// ・データを元に、ScriptをGUI上に置く(ここまでがロード工程)
// ・ロードが終わったら、Scriptのクラス情報を元に、各オブジェクトを初期化(idはノードのID)
// ・Filterの場合、メソッドの実行結果を収集(ここはRef使わずに実行するほうが良い感じだった。)
// ・Filter以外の場合、に関してはまたあとで考える。


// データフローとしては、

// 0.クラスがアタッチされる
// 1.アタッチされたクラスをインスタンス化して実行できるだけの情報を保持する
// 2.データを元にノードやコネクションをGUIに置く
// 3.この時点で特定のクラスに関しては実行して結果をプレ結果として収集する
// 4.再生を押したら、ノードデータをもとにクラスインスタンスを作って、実行させる


// このへんの、3から先を作り出せれば良さそう。
// 3.この時点で特定のクラスに関しては実行して結果をプレ結果として収集する
// 4.再生を押したら、ノードデータをもとにクラスインスタンスを作って、実行させる




internal Source
// N本のアウトプットを持つ。Scriptを足して云々する代物ではないので、GUIからプラスするのみ。データだけを持って、
// そのデータから独自でSourceクラスからインスタンスが作られる感じ。




/**
	フィルタのノードの元になるScript。
	SingleInput, MultiOutput.
*/
public class A : FilterBase {
	List<string> In (List<string> sources) {
		// これの個数だけ、コンパイル後に勝手にポイントが出来上がる。
		// まずはFilterBaseの子供のクラス名を列挙して、
		// そこからOutのRefを取得すれば、各クラスの各インスタンス(というかGUI上のID)の
		// このクラスの親クラスから、Out(string, List<string>)メソッドの参照箇所を抜き出せばよさげ。→参照漁るより実行ルートを2つ用意したほうが楽だしみやすかった。実行させる。
		// 実体化に関してはノードを生やすポイントをGUI上に出すときに利用する。
		// その際、ルーティングも全部網羅するような状態にする。つまりコンパイル後の解析情報を使うのはGUIの構築時の一回のみ。平和。
		AssetGraph.Out("ラベル0", sources.Where(path => path.StartsWith("/A/")).ToList());
	}
}

/**
	インポートを行う状況整理をするScript。
	SingleInput, SingleOutput(モデルの場合だけそうでもない。ここをどう収集するか。)

	複数のファイルが走る場合、import時にどれかのハンドラが走る。
	該当しない場合はスルーして通る。まあ通過するだけだな。
*/
public class B : ImporterBase {
	// baseにいろんなハンドラがあるよね。
	public virtual void InputAssetGraphOnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {}
	public virtual void InputAssetGraphOnPostprocessGameObjectWithUserProperties (GameObject g, string[] propNames, object[] values) {}
	public virtual void InputAssetGraphOnPreprocessTexture () {}
	public virtual void InputAssetGraphOnPostprocessTexture (Texture2D texture) {}
	public virtual void InputAssetGraphOnPreprocessAudio () {}
	public virtual void InputAssetGraphOnPostprocessAudio (AudioClip clip) {}
	public virtual void InputAssetGraphOnPreprocessModel () {}
	public virtual void InputAssetGraphOnPostprocessModel (GameObject g) {}
	public virtual void InputAssetGraphOnAssignMaterialModel (Material material, Renderer renderer) {}
	
	/*
		Texture読み込み直前に発生するやつ
		ここに Input input が入る感じ。
	*/
	void InputAssetGraphOnPreprocessTexture () {
		// ここで読み込み設定とかを書く、、んだけど、importはファイル個別で走る気がする。
		// filterでのカテゴリ分けとかは終わってるので、ここに来るのは個別のソースになる。
		// 個別に出るので、Inが全部終わって初めて次のnodeに行ける、みたいな感じになるな。リソースの分だけn回呼ばれる。
		// 実行計画みたいなのがmergeを起こす要素から逆算される必要がある。楽しそうだ。
		UnityEditor.TextureImporter importer = assetImporter as UnityEditor.TextureImporter;
		importer.textureType			= UnityEditor.TextureImporterType.Advanced;
		importer.npotScale				= TextureImporterNPOTScale.None;
		importer.isReadable				= true;
		importer.alphaIsTransparency 	= true;
		importer.mipmapEnabled			= false;
		importer.wrapMode				= TextureWrapMode.Repeat;
		importer.filterMode				= FilterMode.Bilinear;
		importer.textureFormat 			= TextureImporterFormat.ARGB16;
	}

	対象のファイルがすでに存在している場合、消す。っていう動作がある。
}

public class C : PrefabricatorBase {
	public void Inputs (Dictionary<AssetGraphLabel, List<string>> rabelsAndAssets) {
		// ここでラベルに対してのprefab作りの式を書く。
		// mergeなので、複数のファイルが複数のラベル付きリンクから来る。

		foreach (var rabel in rabelsAndAssets.Keys) {
			var values = rabelsAndAssets[rabel];

			// これで1チャンネル分のAssetsが来る感じになる。
			// valuesには、 "Assets/Textures/texture.jpg" とかが入ってるのか？
			var characterTexture = AssetDatabase.LoadAssetAtPath(values[0], Texture2D) as Texture2D;
			
			if (characterTexture) Debug.Log("Prefabricate:loaded:" + mainImageResourcePath);
			else Debug.LogError("Prefabricate:failed to load:" + mainImageResourcePath);

			var prefabBaseName = "SOMEWHERE/example";// valuesに入ってる要素から決め打ち

			// generate texture material
			var characterMaterial = new Material(Shader.Find ("Transparent/Diffuse"));
			AssetDatabase.CreateAsset(characterMaterial, prefabBaseName + "_material.mat");"SOMEWHERE/example";
			// ここで作ったやつを勝手に追跡する。差分で見ればいいんだと思う。
			// prefabの出力場所に関しては、なんらかヒントがいるんじゃないのかなあと思ったりしないでもない。
			// かならずAssets/以下に出すはめになった気がする。

			// then set loaded texture to that material.
			characterMaterial.mainTexture = characterTexture;


			// generate cube then set texture to it.
			var cubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);

			var meshRenderer = cubeObj.GetComponent<MeshRenderer>();
			meshRenderer.material = characterMaterial;


			// generate prefab in prefabBaseName folder."SOMEWHERE/example";
			var prefabOutputPath = prefabBaseName + "_prefab.prefab";"SOMEWHERE/example";
			UnityEngine.Object prefabFile = PrefabUtility.CreateEmptyPrefab(prefabOutputPath);
			

			// export prefab data.
			PrefabUtility.ReplacePrefab(cubeObj, prefabFile);


			//////// Outに出るのはprefab入りのListなので、例えば中身はこんな感じになるのが理想。
			////////// prefab(new)
			////////// sourceA(image)
			////////// sourceB(model)


			// ここで、例えばprefabを作り出すメソッドからprefabのフルパスを隠蔽、Outにprefabを自明で追加する仕掛けを作ることは可能。
			// やりすぎてる気がするのでここではコンセプトだけ示す。
			if (false) {
				AssetGraphPrefabricate(NAME, cubeObj);
				中身は {
					var prefabOutputPath = "AssetGraph下のTemp的なpath/このCクラスが起動した時の適当な起動ID/";
					PrefabUtility.ReplacePrefab(cubeObj, PrefabUtility.CreateEmptyPrefab(prefabOutputPath));
				}
			}
		}
	}
}

public class D : BundlizerBase {
	public AssetBundleSource Inputs (Dictionary<AssetGraphLabel, List<string>> rabelsAndAssets) {
		
		このへんまだAssetRailsのまんま。
		var mainResourceTexture = Resources.Load(resNameAndResourceLoadablePathsDict["texture"]);
		if (mainResourceTexture) Debug.Log("Bundlize:loaded:" + resNameAndResourceLoadablePathsDict["texture"]);
		else Debug.LogError("Bundlize:failed to load:" + resNameAndResourceLoadablePathsDict["texture"]);

		var otherResourcePaths = resNameAndResourceLoadablePathsDict.Keys
			.Where(key => key != "texture")
			.Select(key => resNameAndResourceLoadablePathsDict[key])
			.ToList();


		// load other resources.
		var subResources = new List<UnityEngine.Object>();

		foreach (var path in otherResourcePaths) {
			var subResource = Resources.Load(path);
			
			if (subResource) {
				subResources.Add(subResource);
				Debug.Log("Bundlize:loaded:" + path);
			} else {
				Debug.LogError("Bundlize:failed to load:" + path);
				return;
			}
		}

		return new AssetBundleSource(
			"バンドル名",
			mainResourceTexture,
			subResources.ToArray()
		);
	}
}

たぶん発生する既知の制約
	importしたのをimportするの無理なのでは


