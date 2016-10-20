#QuickStart Guide
AssetBundle Graph Tool release 1.0

#AssetBundle Graph Toolとは
AssetBundle Graph ToolはUnityのAssetBundleを作るためのGUIツールです。
アセットへと設定や変更点を反映させるフローを作成し、AssetBundleの作成や、アセットバンドルに含めたいアセットの生成や調整などの処理を、だいたいコードを書かずに行うことができます。

ま、とりあえず使い方を簡単に見てみましょうか。こんな感じで使っていきます。

###1.ノードを作成する
WindowメニューのAssetBundleGraphから「Open Graph Editor」を選択して、AssetBundleウインドウを表示します。次にウィンドウ内で右クリックすると、作成したいノードを選ぶことが出来ます。まずはLoaderを作成してみましょう。Loaderは、AssetBundle Graph Tool内へとUnityプロジェクト内部のアセットを読み込むノードです。
![SS](/Doc/images/guide/1.png)

Loaderノードを作ったら、次に同じ要領でFilterノードを作ってみましょう。Filterは入力されたファイルをフィルタリングするノードです。

###2.ノードを接続する
２つ以上のノードを作成したら、ノードを繋いでいきます。ノードの左右両端にある丸い部分をクリックして、他のノードの丸い部分にドラッグ＆ドロップすると繋がります。

ノードを繋ぐと、左のLoaderノードから、右のFilterノードへとアセットが流れるようになります。接続線の真ん中にある数字が流れるアセットの数です。数値が出ている部分を押すと流れているアセット一覧がInspectorに表示されます。

![SS](/Doc/images/guide/2.png)

###3.ノードの設定を変更する
ノードを左クリックすると、Inspectorでノードの設定が出来ます。ノードの種類ごとに設定できることは異なります。
例えばFilterノードでは、流れるアセットを名前ごとに分ける、といったことができます。ImportSettingノードでは、入力されるアセットのインポート時の設定を変更することができます。

![SS](/Doc/images/guide/3.png)

###4.ビルドする
AssetBundle Graph Toolウィンドウ内のBuildボタンを押すと、繋がっているノードに素材が流れ処理が行われます。

BundleConfigノードとBundleBuilderノードを作ってアセットを流し込むことでアセットバンドル化することが出来ます。BundleConfigではアセットバンドルの設定を、BundleBuilderでは実際のアセットバンドルのビルド処理を行います。

![SS](/Doc/images/guide/4.png)  
![SS](/Doc/images/guide/5.png)    

##AssetBundle Graph Toolの開発背景
AssetBundle Graph Toolは、アセットの調整や設定をビジュアルに設定・確認できるようにすることで、アセットバンドルを誰でも使えるものにする事を目指して開発されました。
また、ファイル名などのルールから自動的に分類してアセットバンドルを作れるようにすることで、アーティストやゲームデザイナーがアセットバンドルの事に開発中に気を配らなくても、沢山のアセットバンドルを自動的に作れるようにするためのツールとして設計されています。

ゲームを作っていく過程でアセットが増えるのは避けられないことですが、AssetBundle Graph Toolを使うことで、テクスチャのサイズやマテリアルのプロパティなど、さまざまな設定をあらかじめ決めたフローで自動的に処理させた上でアセットバンドル化することができます。

また、フローの中で渡されたアセットからPrefabを作ったり、ビルド後のポストプロセスなどもスクリプトで書くことができるので、ビルドのフローの中で行いたいちょっとした特別な処理を簡単に含めることもできます。

#よくある使い方

## 複数のアセットから一つのアセットバンドルを作る
Assets以下にあるアセットをAssetBundle Graph Toolに読み込んでアセットバンドルにするには、以下のようにします。

1. Loaderでアセットを読み込みたいディレクトリを指定する
1. BundleConfigに繋いでアセットバンドルの名前を決めるImportSetting
1. BundleBuilderに繋いでアセットバンドルを出力する
1. Exporterで生成したアセットバンドルを所定の位置にコピーする

![SS](/Doc/images/guide/h1.gif)


##複数のアセットから、それぞれを別のアセットバンドルにする
Groupingノードを使うことで、アセットを複数のグループに分けて、それぞれアセットバンドルに出来ます。

1. Loaderでアセットを読み込みたいディレクトリを指定する
1. Groupingに繋いで、パターンを指定することでパス名からグループを作成する
1. BundleConfigに繋いでアセットバンドルの名前を決める
1. BundleBuilderに繋いでアセットバンドルを出力する
1. Exporterで生成したアセットバンドルを所定の位置にコピーする

![SS](/Doc/images/guide/h2.gif)


##アセットのインポート設定を自動的に変更したい
ImportSettingノードを使うことで、ノードを通るアセットのインポート設定を変更することが出来ます。

1. 他のノードからのアセットの出力をImportSettingを繋いで、インスペクタのModify Import Setting ボタンを押してインポート設定を調整する

![SS](/Doc/images/guide/h3.gif)

これだけで、ImportSettingノードを通過したアセットすべてに設定を反映することができます。

複数の種類のアセット(たとえばテクスチャやモデル)が含まれている場合、Filterを使ってアセットを種類ごとに分類し、それぞれ別のImportSettingノードに繋ぎます。インポーターの存在しないMaterialやRender Textureなどのアセットの場合は、Modifierノードを代わりに使うことが出来ます。

##アセットからPrefabを自動的に作成したい
アーティストの追加したモデルデータ等を使って、スクリプトを追加して敵キャラクター等のPrefabを作成したい、ということがあります。PrefabBuilderを使うことで、Prefabを作ることができます。Prefab化をするためにはスクリプトを書く必要があります。スクリプトは以下のようなものです：

```C#
public UnityEngine.GameObject CreatePrefab (string groupKey, List<UnityEngine.Object> objects) {
	GameObject go = new GameObject(string.Format("MyPrefab{0}", groupKey));
	GUITexture t = go.AddComponent<GUITexture>();

	Texture2D tex = (Texture2D)objects.Find(o => o.GetType() == typeof(UnityEngine.Texture2D));

	t.texture = tex;
	t.color = color;

	return go;
}
```
メニュー>AssetBundleGraph>Create Node Script>PrefabBuilder Scriptを選択してスクリプトを生成し、こんな感じのシンプルな関数を実装してGameObjectを返すと、Prefabとして保存してくれます。

![SS](/Doc/images/guide/h4.gif)

List<UnityEngine.Object> に渡されるオブジェクト群は、Groupingでグループ化したものが渡されてきます。PrefabBuilderは１つのグループに対して１つのGameObjectを返す事を前提にしています。

##コマンドラインから実行したい
AssetBundle Graph Toolはコマンドラインから実行することもできます。
メニュー>AssetBundleGraph>Create CUI Toolを選択すると、お使いのプラットフォームで有効なCUI用スクリプトを生成します。これを使うと、以下のようにコマンドラインから指定のプラットフォームのアセットバンドルをビルド出来ます。

```shellscript
$> sh -e buildassetbundle.sh -target WebGL
```

##ビルド実行後に処理を行いたい
Postprocessスクリプトを作成することで、ビルド完了時にスクリプトの処理を行うことが出来ます。
メニュー>AssetBundleGraph>Create Node Script>Postprocess Script を選択してポストプロセス用のスクリプトを作成しましょう。

#Node説明
##Loader
- OUT: 指定したフォルダに含まれているすべてのアセット（１グループ）

Loader pathに指定したフォルダに入っているAssetをすべて読み込む。

![SS](/Doc/images/guide/n_loader.png)

##Filter
- IN: アセット
- OUT: keywordにマッチしたアセット

フィルタ条件にマッチするアセットを抽出します。複数のフィルタを設定して、複数の出力を持たせることも出来ます。

![SS](/Doc/images/guide/n_filter.png)


##ImportSetting
- IN: アセットのグループ
- OUT: INから入ったアセットのグループ

テクスチャ、モデル、オーディオの3つのアセットについて、アセットのインポート設定を変更します。

![SS](/Doc/images/guide/n_importsetting.png)

1つのImportSettingで設定ができるのは１種類のアセットのみです。入力されたアセットに複数の種類がある場合、あるいは前に設定されたアセットとタイプが異なる場合はエラーになります。

##Modifier
- IN: アセットのグループ
- OUT: INから入ったアセットのグループ

インポーターのないアセットの設定を直接変更します。テクスチャ、モデル、オーディオ以外のRenderTextureやMaterialといったアセットの設定を変更することが出来ます。Modifierで行う変更はプロジェクトの要件によって多岐に渡るので、Modifierを使う場合は基本的にModifier Scriptを自分で書いて対応します。

![SS](/Doc/images/guide/n_modifier.png)

メニュー>AssetBundleGraph>Create Node Script>Modifier Script を選択することで、Modifier用のスクリプトを作成できます。
自分でModifierを定義するときには、AssetBundleGraph.CustomModifier アトリビュートを使ってどの型の変更を行うかを指定します。

```C#
[AssetBundleGraph.CustomModifier("MyModifier", typeof(RenderTexture))]
public class MyModifier : AssetBundleGraph.IModifier {

	[SerializeField] private bool doSomething;

	// Test if asset is different from intended configuration 
	public bool IsModified (object asset) {
		return false;
	}

	// Actually change asset configurations. 
	public void Modify (object asset) {
	}

	// Draw inspector gui 
	public void OnInspectorGUI (Action onValueChanged) {
		GUILayout.Label("MyModifier!");

		var newValue = GUILayout.Toggle(doSomething, "Do Something");
		if(newValue != doSomething) {
			doSomething = newValue;
			onValueChanged();
		}
	}

	// serialize this class to JSON 
	public string Serialize() {
		return JsonUtility.ToJson(this);
	}
}
```


##Grouping
- IN: アセットのグループ
- OUT: 設定によって分けられたアセットのグループのグループ

キーワードを使って、アセットを複数のグループに分けます。

![SS](/Doc/images/guide/n_grouping.png)

Inspectorでグループ分けに使用するパターンを指定すると、アセットのパスから複数のグループが作られます。*でマッチした部分がグループ名として使用されます。たとえば"Menu/English/GUI.prefab", "Menu/Danish/GUI.prefab" の２つのアセットが入力にある時に、"Menu/*/"をパターンとして指定すると、EnglishとDanishの２つのグループが作られます。

##PrefabBuilder
- IN: Prefabの素材にしたいAssetのグループ
- OUT: 作成されたPrefabを含むAssetのグループ

入力されたアセットから指定のスクリプトでPrefabを作成することができます。
出力されるアセットには、入力されたアセットに加えて新しくPrefabが追加されます。

![SS](/Doc/images/guide/n_prefabbuilder.png)

PrefabBuilderを使うには、簡単なスクリプトを作る必要があります。
メニュー>AssetBundleGraph>Create Node Script>PrefabBuilder Script を選択することで、Modifier用のスクリプトを作成できます。スクリプトの見た目はこんな感じです。

```C#
[AssetBundleGraph.CustomPrefabBuilder("MyBuilder")]
public class MyPrefabBuilder : IPrefabBuilder {

	[SerializeField] private Color color;

	public string CanCreatePrefab (string groupKey, List<UnityEngine.Object> objects) {
		var tex = objects.Find(o => o.GetType() == typeof(UnityEngine.Texture2D));

		if(tex != null) {
			return string.Format("MyPrefab{0}", groupKey);
		}

		return null;
	}

	public UnityEngine.GameObject CreatePrefab (string groupKey, List<UnityEngine.Object> objects) {
		GameObject go = new GameObject(string.Format("MyPrefab{0}", groupKey));
		GUITexture t = go.AddComponent<GUITexture>();
		Texture2D tex = (Texture2D)objects.Find(o => o.GetType() == typeof(UnityEngine.Texture2D));
		t.texture = tex;
		t.color = color;

		return go;
	}

	public void OnInspectorGUI (Action onValueChanged) {
		var newValue = EditorGUILayout.ColorField("Texture Color", color);
		if(newValue != color) {
			color = newValue;
			onValueChanged();
		}
	}

	public string Serialize() {
		return JsonUtility.ToJson(this);
	}
}
```

スクリプトを作成したら、インスペクターから使用したいPrefaBuilderを選んで設定します。


##BundleConfigurator
- In: アセットバンドルにしたいアセットのグループ
- Out: アセットバンドル化のための設定がなされたアセットのグループ

入力されたアセットのグループにアセットバンドル化のための設定を行います。
作成されるアセットバンドルの名前は、Bundle Name Templateで指定します。テンプレート名の"*"には、グループ名が入ります。

![SS](/Doc/images/guide/n_bundleconfig.png)

BundleConfiguratorを使うことで、バリアントの設定を行うことも出来ます。また、グループの入力をバリアントとして扱うことも出来ます。

##BundleBuilder
- In: アセットバンドル化のための設定がなされたアセットのグループ
- Out: 実際に生成されたアセットバンドルファイルとmanifest（１グループ）

BundleConfiguratorで設定したアセットバンドルをビルドします。アセットバンドルを圧縮するかどうかなど、ビルド時のオプションを設定できます。

![SS](/Doc/images/guide/n_bundlebuilder.png)

BundleBuilderはBundleConfigurator以外の入力を受け付けていません。BundleBuilderを使うためには、必ず
BundleConfiguratorを経由する必要があります。

##Exporter
- In: 出力したいアセットのグループ

指定したパスにファイルを出力することができます。出力先フォルダがないとエラーにするか、自動的に生成するかなどの出力オプションを設定できます。

![SS](/Doc/images/guide/n_exporter.png)


#ポストプロセス
ビルド処理が終わった時に追加で何かしたい場合は、Postprocessスクリプトを生成することで行えます。Postprocessを使った簡単なビルドレポートを生成するスクリプトは以下のようなものです。

```C#
public class MyPostprocess : AssetBundleGraph.IPostprocess {
	public void Run (Dictionary<AssetBundleGraph.NodeData, Dictionary<string, List<AssetBundleGraph.Asset>>> assetGroups, bool isRun) {

		if (!isRun) {
			return;
		}

		Debug.Log("BUILD REPORT:");

		foreach (var node in assetGroups.Keys) {
			var result = assetGroups[node];

			StringBuilder sb = new StringBuilder();

			foreach (var groupKey in result.Keys) {
				var assets = result[groupKey];

				sb.AppendFormat("In {0}:\n", groupKey);

				foreach (var a in assets) {
					sb.AppendFormat("\t {0} {1}\n", a.path, (a.isBundled)?"[in AssetBundle]":"");
				}
			}

			Debug.LogFormat("Node:{0}\n---\n{1}", node.Name, sb.ToString());
		}
	}
}
```

##ノードの接続
ノード同士は、繋がるものもあれば繋がらないものもあります。
![SS](/Doc/images/guide/nodeconnectivity.png)


