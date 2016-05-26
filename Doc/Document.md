#AssetBundleGraph Document
document version 0.8.0

##TOC
* 使い方
* AssetBundleGraphが便利なワケ
* 逆引きAssetBundleGraph(how to)
* Nodeの詳細
* HookPoint tips


#使い方
AssetBundleGraphはUnityのAssetBundleを作るためのGUIツールです。
アセットへと設定や変更点を反映させるフローを作成し、AssetBundleの作成やその他のアセット生成/調整などあらゆる処理をだいたいコード無しで行うことができます。

###1.ノードを作成
AssetBundleGraphのウィンドウ内で右クリックして、ノードを追加します。
いろんなノードが作れるのですが、まずはLoaderを作成してみましょう。
Loaderは、AssetBundleGraph内へとUnityプロジェクト内部のアセットを読み込むノードです。
![SS](/Doc/images/1.png)

###2.繋ごう
２つ以上のノードを作ったら、それらを繋いでいきます。
ノードの左右両端にある丸い部分を、他のノードの丸い部分にドラッグ＆ドロップすると繋がります。

繋ぐとどうなるか？ 左のLoaderノードから、右のFilterノードへと素材が流れるようになります。
接続線の真ん中にある数字が流れる素材の個数です。数値が出ている部分を押すと、流れている素材一覧がInspectorに表示されます。

![SS](/Doc/images/2.png)

###3.ノードの設定を変更する
ノードを左クリックすると、Inspectorに細かいセッティングが表示されます。
ノードの種類ごとにいろいろな項目が設定できます。
例えばFilterノードでは、流れる素材を名前ごとに複数の流れに分ける、といったことができます。ImportSettingノードでは、アセットのインポート時のセッティングを自在にセットすることができます。

ノードの設定を変更すると、その設定はノードを通過するすべてのアセットに自動的にセットされます。

![SS](/Doc/images/3.png)

###4.ビルドしよう
AssetBundleGraphウィンドウ内のBuildボタンを押すと、繋がっているノードに素材が流れ処理が行われます。

BundlizerノードとBundleBuildノードがあれば、それらのノードに流れ込んだアセットがAssetBundleになります。

![SS](/Doc/images/4.png)  
![SS](/Doc/images/5.png)    
![SS](/Doc/images/6.png)

もちろんキャッシュが効くため、２度目以降は差分の素材を処理する時間しかかかりません。

GUIでノードの設定を変更するだけで、どんな量でも、どんな面倒な処理でも、何度でも実行することができます。

ね、簡単でしょう？




#AssetBundleGraphが便利な理由
AssetBundleGraphでは、素材の調整や設定がコードを一切書かずに実現できるため、プログラマーの助けになるだけではなく、アーティストやゲームデザイナーにもグッとくるものになっているハズです。特にAssetBundle周りについて、一切コードを書かなくても作れるようになるというのは素晴らしいことです。

ゲームを作っていく過程で素材が増えるのは避けられないことですが、それらの素材を手で調整しないでも、AssetBundleGraphで素材の調整を自動化してしまえば大丈夫。
新規に追加された素材も、いままで通りのフローに乗って処理されるため、余計な手間がいりません。追加した同じような素材100個を一つずつ手で、、みたいな地獄とはサヨナラできます。

おまけにAssetBundleGraphでは、AssetBundle以外にも、自分で作った圧縮/暗号化、Prefab作成(コードが必須)、インポートしたものをアセットのままどこかに出す、というようなことまでできるようになります。


#逆引きAssetBundleGraph(how to)


##複数の素材を一つのAssetBundleにする
一切コードを書かずに、フォルダに入っている素材をAssetBundleGraphに読み込み、一つのAssetBundleにすることができます。

1. Loaderで素材の入ったフォルダを指定
1. Importerで素材をインポート
1. Bundlizerで素材のAssetBundle化
1. BundleBuilderでAssetBundleの設定、生成
1. ExporterでAssetBundleの吐き出し

![SS](/Doc/images/howto_0.gif)


##複数の素材を複数のAssetBundleにする
Groupingノードを使って素材を複数のグループに分けることができます。

PrefabricatorやBundlizerは、PrefabやAssetBundleを作成する際に、グループ単位で作成を行うことが簡単にできるようになっています。

次のようなフローで、グループ単位でAssetBundleを作成することができます。

1. Loaderで素材の入ったフォルダを指定
1. Importerで素材をインポート
1. Groupimgで素材をグループ分け
1. Bundlizerでグループ分けされた素材をAssetBundle化
1. BundleBuilderでAssetBundleの設定、生成
1. ExporterでAssetBundleの吐き出し

![SS](/Doc/images/howto_1.gif)

ポイントは3,4で、Groupingで複数の素材からグループを作成、BundlizerでそのグループごとにAssetBundleを作成しています。


##一気に大量の素材のインポート設定を変更したい
プロジェクトへとインポートしたい素材をAssetBundleGraphのプロジェクトフォルダに置き、そのパスをLoaderで指定、Importerへと繋ぐと一気に素材のインポート処理ができます。

1. Loaderで素材の入ったフォルダを指定
1. Importerを繋いで、ImporterのインスペクタのModify Import Setting ボタンから、インポート設定をセットする

![SS](/Doc/images/howto_2.gif)

これだけで、ImportSettingノードを通過した素材すべてに変更した設定を反映することができます。

複数の種類の素材(e.g. imageとmodelなど)が含まれている場合、Filterを使って素材の種類ごとにImporterを用意すると設定が楽でいいでしょう。

1. Loaderで素材が置いてあるパスを指定
1. Filterで、素材名やパスから、素材を仕分け
1. Importerを繋いで、ImporterのModify Import Setting ボタンから、インポート設定をセット

![SS](/Doc/images/howto_3.gif)

##素材を複数のグループに分ける
たとえばゲームのキャラクターが複数いて、それらがテクスチャ + モデルで構成されている時、

Groupingノードを使うと、複数の素材を、キャラ1の素材の集まり(テクスチャ + モデル)、 キャラ2の素材の集まり(テクスチャ + モデル)　などのようにグループ分けすることができます。

![SS](/Doc/images/howto_4.gif)

[Grouping](https://github.com/unity3d-jp/AssetBundleGraph/blob/master/Doc/Document.md#grouping)

##素材からPrefabを作成する
AssetBundleGraphでは、素材を読み込んでPrefabをつくることができます。
ただし、Assetを指定したりインスタンス化する必要があるため、それらの操作をC#のスクリプトとして記述する必要があります。
スクリプトは次のようなものです。

```C#
using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Collections.Generic;

public class MyPrefabricator : AssetBundleGraph.PrefabricatorBase {
	public override void In (string groupKey, List<AssetBundleGraph.AssetInfo> source, string recommendedPrefabOutputDir, Func<GameObject, string, bool, string> Prefabricate) {
		// let's generate Prefab with "Prefabricate" method.
	}
}
```

AssetBundleGraph.PrefabricatorBase クラスを継承し、In メソッドを持つScriptです。
このScriptは、Window > AssetBundleGraph > Generate Script For Node > Prefabricator Script で自動的にひな形を作成できます。

スクリプトを作成したら、その型名をAssetBundleGraph内のPrefabricatorノードにセットすることで、ノードに流れてきたAssetが自動的にスクリプトを通過するようになります。

![SS](/Doc/images/howto_5.gif)


PrefabricatorノードにどんなAssetがどのようなグループ名、順番で入ってくるかは、PrefabricatorノードにつながっているConnectionから想定することができます。

![SS](/Doc/images/howto_6.png)

この場合、groupKey "0"で、dummy.png, kiosk001.mat, sample.fbx の3つが順にsourceに入った状態で、Prefabricatorを拡張したInメソッドが呼ばれます。

![SS](/Doc/images/howto_7.png)

Prefabの作成にPrefabricateメソッドを使うと、キャッシュが効いて便利です。

サンプルコードはこちら。
[SamplePrefabricator](https://github.com/unity3d-jp/AssetBundleGraph/blob/master/Assets/AssetBundleGraph/UserSpace/Examples/Editor/SamplePrefabricator.cs)


##素材のグループからPrefabを作成する
Groupingノードで複数のグループを作り出しPrefabricatorノードにつなぐと、複数のグルーピングされた素材をPrefab作成に使うことができます。
複数のグループは、PrefabricatorBaseを拡張したスクリプトの中で、groupKeyの値として使用できます。


##コマンドラインから実行する
AssetBundleGraphはコマンドラインから実行することができます。
UnityEditorにセットされているプラットフォームを使う場合、次のようなshellScript/batchで実行するといいでしょう。

```shellscript
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -quit\ 
	-projectPath $(pwd)\
	-executeMethod AssetBundleGraph.AssetBundleGraph.Build
```

また、次のようなshellScript/batchで、プラットフォームを指定してAssetBundleGraphを実行することができます。

```shellscript
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -quit\ 
	-projectPath $(pwd)\
	-executeMethod AssetBundleGraph.AssetBundleGraph.Build iOS
```

サンプルのshellScriptはこちら。
[build.sh](https://github.com/unity3d-jp/AssetBundleGraph/blob/master/Assets/AssetBundleGraph/UserSpace/build.sh)

##インポートしたファイルや、作成したPrefab、AssetBundleをAssets/外に吐き出したい
Exporterノードでは、インポート済みのファイルやPrefab、AssetBundleを出力することができます。


##ビルド実行後に処理をしたい
AssetBundleGraphでは、Finallyというフックポイントがビルド完了時に起動するFinallyという機構があります。

[Finally](https://github.com/unity3d-jp/AssetBundleGraph/blob/master/Doc/Document.md#hookpoint-finally-tips)


##作成したAssetBundleのcrcやサイズ情報を簡単に扱いたい
Unity5から、AssetBundleの情報は.manifestファイルで吐き出されるようになりました。Finally機構を利用してその情報を読み取る方法を紹介します。

[Finally](https://github.com/unity3d-jp/AssetBundleGraph/blob/master/Doc/Document.md#assetbundleのmanifestからjsonでリストを作り出す)


##キャッシュを消したい
AssetBundleGraphでは、一度実行したインポート処理やPrefab作成処理、AssetBundle作成処理をキャッシュし、同じ内容であればキャッシュを使うことで無駄な時間を削減しています。キャッシュの実態ファイルはAssets/AssetBundleGraph/Cache フォルダにあります。

UnityのメニューからClear Cacheを選択することで、AssetBundleGraph内にあるすべてのファイルのキャッシュを消すことができます。

Unity > Window > AssetBundleGraph > Clear Cache


##グラフのデータを消したい
AssetBundleGraphのウインドウを開くとハングするようになってしまうなど、読み込もうとしたファイルによって具合が悪くなってしまった場合、Assets/AssetBundleGraph/SettingFiles/AssetBundleGraph.json ファイルを削除すると、グラフのデータを消すことができます。


#Nodeの詳細
##Loader
- OUT: 指定したフォルダに含まれているすべての素材

Loader pathに指定したフォルダに入っているAssetをすべて読み込む。

![SS](/Doc/images/7_loader.png)

loadPathはProjectフォルダのパスから下を指定することができる。
ProjectフォルダにAssetBundleGraph用の素材を置くフォルダを作成するのがオススメ。

Assets/パスを使って、すでにプロジェクト内で使っているAssetを使用することもできる。


##Filter
- IN: 複数のAsset
- OUT: keywordにマッチした複数のAsset

パスにkeywordを含む素材を、複数の出力に振り分けることができます。

![SS](/Doc/images/7_filter.png)

keywordは複数設定することができます。


##ImportSetting
- IN: 複数のAsset
- OUT: 複数のAsset

一つ~複数のファイルの設定を変更します。

TextureImporter, ModelImporter, AudioImporterのいずれかのタイプのファイルを扱うことができます。

Inspectorからインポート設定を調整することができます。

![SS](/Doc/images/7_importer.png)

1つのImportSettingで設定ができるのは、画像/モデル/音声から１種類のみです。
例えば画像とモデルをまとめてこのノードに送り込むと、どちらかだけにしてくれ、という旨のエラーが出ます。


##Modifier
- IN: 複数のAsset
- OUT: グループ分けされた複数のAsset

一つ~複数のファイルの設定を変更します。

TextureImporter, ModelImporter, AudioImporter以外のタイプのファイルを扱うことができます。

Inspectorからインポート設定を調整することができます。今後。多分。



##Grouping
- IN: 複数のAsset
- OUT: グループ分けされた複数のAsset

キーワードを使って、素材を複数のグループに分けることができます。

![SS](/Doc/images/7_grouping_0.png)
![SS](/Doc/images/7_grouping_1.png)
![SS](/Doc/images/7_grouping_2.png)

Inspectorで、group Key に「グループ分けに使用するキーワード」を指定すると、素材のパスから複数のグループが作られます。

group Keyでは、\* 記号をワイルドカードとして使用することができます。

たとえばフォルダ名に /ID_mainChara/、/ID_enemy/ などがついている場合、
group Key に /ID_\*/ とセットすると、"mainChara", "enemy"の2つのグループが作成されます。

すでにグループ分けされたAssetをGroupingノードに通すと、一度グループ分けが解除され、再度グループ分けされます。

##Prefabricator
- IN: Prefabの素材にしたいAssetのグループ
- OUT: 作成されたPrefabを含むAssetのグループ

入力されたAssetから、スクリプトを介してPrefabを作成することができます。
出力されるAssetは、入力されたAssetと作成されたPrefabを合わせたものになります。

![SS](/Doc/images/7_prefabricator.png)

PrefabricatorBaseを拡張したスクリプトを書き、セットして使用します。
残念ながら、スクリプト無しでこのノードを使用することはできません。

Prefabricatorノードには、２通りの作成方法があります。

1. GUIで作成したものにスクリプトの型名を入力する
1. スクリプトをAssetBundleGraphウィンドウにD&Dする

スクリプトはAssetBundleGraph.PrefabricatorBaseクラスを拡張し、public override void In (string groupKey, List<AssetBundleGraph.AssetInfo> source, string recommendedPrefabOutputDir, Func<GameObject, string, bool, string> Prefabricate)メソッドをオーバーライドしている必要があります。

サンプルスクリプト[CreateCharaPrefab.cs](https://github.com/unity3d-jp/AssetBundleGraph/blob/master/Assets/AssetBundleGraph/UserSpace/Examples/Editor/CreateCharaPrefab.cs)



##Bundlizer
- In: AssetBundleの素材にしたいAssetのグループ
- Out: グループごとに生成されたAssetBundle

入力されたAssetから、AssetBundleを作成することができます。  
生成されるAssetBundleの名前は、BundleNameTemplateパラメータで指定することができます。

![SS](/Doc/images/7_bundlizer_0.png)
![SS](/Doc/images/7_bundlizer_1.png)
![SS](/Doc/images/7_bundlizer_2.png)

この際、BundleNameTemplateに\*が含まれていると、そこにはグループIDが自動的にセットされます。

\*が含まれていない場合、AssetBundleにはBundleNameTemplate通りの名前がつきます。

この機能は、例えばキャラクターのAssetBundleをそのキャラクターのIDを含んだ名前で作りたい、といった場合に、グループID = キャラクターIDとなるようなフローを組んでおくことで自動的にAssetBundleが命名されることになるので、とても効果的です。

また、このノードから出力されるAssetは、作成されたAssetBundleのみになります。このノードから接続できるノードは１種類、BundleBuilderノードのみです。

Bundlizerノードには、２通りの作成方法があります。

1. GUIで作成したものにAssetBundleの名前のテンプレートを入力する
1. BundlizerBaseを拡張したスクリプトをAssetBundleGraphウィンドウにD&Dする

2の方法では、自分で用意したスクリプトを実行することができます。
スクリプトはAssetBundleGraph.PrefabricatorBaseクラスを拡張し、public override void In (string groupKey, List<AssetBundleGraph.AssetInfo> source, string recommendedPrefabOutputDir, Func<GameObject, string, bool, string> Prefabricate)メソッドをオーバーライドしている必要があります。

サンプルスクリプト[CreateCharaBundle.cs](https://github.com/unity3d-jp/AssetBundleGraph/blob/master/Assets/AssetBundleGraph/UserSpace/Examples/Editor/CreateCharaBundle.cs)

この方法では、AssetBundleを作成するコードをこと細かく書いて実行できるほか、自分で考えた圧縮や暗号化などを行うこともできます。

ただし、BundlizerからはBundleBuilderノード以外に接続できないため、自分でAssetBundleを作成するコードを書いた場合であってもBundleBuilderへと繋ぐ必要があります。


##BundleBuilder
- In: AssetBundleなどのグループ
- Out: 実際に生成されたAssetBundleなどのグループ

Bundlizerで設定したAssetBundleを実際に生成します。様々なオプションをセットすることができます。

![SS](/Doc/images/7_bundlebuilder.png)

Bundlizer以外から接続することができません。
コードなしのBundlizerでAssetBundleの作成をした場合のみ、このノードでAssetBundleの設定を行うことができます。

##Exporter
- In: インポート済み or AssetBundleGraph内で作成したアセットのグループ

指定したパスにファイルを出力することができます。

![SS](/Doc/images/7_exporter.png)

指定できるパスは、プロジェクトのフォルダ以下であれば自由に指定できます。
ただし、指定したフォルダは実行前に作成しておかないといけません。


#HookPoint: Finally tips
AssetBundleGraphのビルド処理・リロード処理が終わったタイミングで実行する処理を、スクリプトで記述することができます。

##FinallyBase クラスをextendsする
FinallyBase クラスを拡張したコードは、ビルド処理・リロード処理が終わったタイミングで自動的に呼ばれます。

public override void Run (Dictionary\<string, Dictionary\<string, List<string>>> throughputs, bool isBuild) メソッドで、すべてのNode、すべてのグループの実行結果を受け取ることができます。

Dictionary\<string, Dictionary\<string, List\<string>>> throughputs
NodeName, groupKey, AssetPath が格納されています。

bool isBuild
ビルド処理時はtrue、それ以外ではfalse


##すべてのノードの生成物のパスをUnityのログに出すサンプル
Finallyのサンプルとして、ウィンドウ内にあるすべてのノードの生成物のファイルパスをログにだす、というものを作ってみましょう。

```C#
using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Collections.Generic;

/**
	sample class for finally hookPoint.

	show results of all nodes.
*/
public class SampleFinally : AssetBundleGraph.FinallyBase {
	public override void Run (Dictionary<string, Dictionary<string, List<string>>> throughputs, bool isBuild) {
		Debug.Log("flnally. isBuild:" + isBuild);

		if (!isBuild) return;
		
		foreach (var nodeName in throughputs.Keys) {
			Debug.Log("nodeName:" + nodeName);

			foreach (var groupKey in throughputs[nodeName].Keys) {
				Debug.Log("	groupKey:" + groupKey);

				foreach (var result in throughputs[nodeName][groupKey]) {
					Debug.Log("		result:" + result);
				}
			}
		}
	}
}
```
[SampleFinally](https://github.com/unity3d-jp/AssetBundleGraph/blob/master/Assets/AssetBundleGraph/UserSpace/Examples/Editor/SampleFinally.cs)

##AssetBundleのmanifestからjsonでリストを作り出す
Finallyの例のその２として、AssetBundle生成時に作られた.manifestファイルからAssetBundleの情報を読み出してjsonにしてみましょう。

Bundlizer、BundleBuilderでAssetBundleを作り、ExporterでAssetBundleをアウトプットしており、そのAssetBundleの内容をjson形式のリストにしたい、という前提です。

Finallyに書くべきコード(抜粋)は下記のようなものになります。

```C#
	public override void Run (Dictionary<string, Dictionary<string, List<string>>> throughputs, bool isBuild) {
		
		// run only build time.
		if (!isBuild) return;

		var bundleInfos = new List<Dictionary<string, object>>();

		// get exported .manifest files from "Exporter" node.

		string targetNodeName = "Exporter0";
		foreach (var groupKey in throughputs[targetNodeName].Keys) {
			foreach (var result in throughputs[targetNodeName][groupKey]) {
				// ignore SOMETHING.ASSET
				if (!result.EndsWith(".manifest")) continue;

				// ignore PLATFORM.manifest file.
				if (result.EndsWith(EditorUserBuildSettings.activeBuildTarget.ToString() + ".manifest")) continue;

				// get bundle info from .manifest file.
				var bundleInfo = GetBundleInfo(result);
				bundleInfos.Add(bundleInfo);
			}
		}

		var bundleListJson = Json.Serialize(bundleInfos);

		Debug.Log(bundleListJson);
```

Exporterノードの名前を指定することで、特にExporter0という名前のノードの結果だけに注目し、.manifestファイルからAssetBundleのデータを取り出しています。

最終的にはそれらを List\<Dictionary\<string, object>> bundleInfos に入れ、Json形式のstringにしています。

Jsonにすることで取り回しが楽になるケースなどで役にたつと思います。

サンプルコード全体はこちら[SampleFinally2](https://github.com/unity3d-jp/AssetBundleGraph/blob/master/Assets/AssetBundleGraph/UserSpace/Examples/Editor/SampleFinally2.cs)






