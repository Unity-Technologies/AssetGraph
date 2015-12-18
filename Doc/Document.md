#AssetGraph Document
document version 0.8.0

##TOC
* はじめに
* 逆引きAssetGraph(how to)
* Node tips
* HookPoint tips
* Package tips


#はじめに
AssetGraphは、GUIでいろいろやって様々なファイルをUnityにインポートしたりプレファブを作成したり、AssetBundleを作ったりすることに特化しているツールです。


AssetGraphを使うと、面倒な処理を効率良く自動化できます。
いちいち手ですべてのAssetのインポート設定をいじったり、Prefabを作ったりする必要はありません。
特に大量のファイルから大量のAssetBundleを作ったり、大量のファイルに調整を施すのが得意です。

これ全部手作業でやるのしんどいな〜みたいな数までAssetが増えたら、是非試してみてください。

(アンチョコ)
手順：
ノードのつなぎ方、既存のREADMEを丸パク

**☆印のところはまだ書き終わってない。**

**desumasu調に統一する**

**英語化版も作る**

#逆引きAssetGraph(how to)


##複数の素材から一つのAssetBundleを作成する
☆
loader -> importして
Bundlizerを使う。
bundleNameTemplateにbundle名をセット
*マークは、複数のグループをそれぞれAssetBundleにするときに便利。


##大量の素材をAssetBundleにしたい
☆Loader -> Importer -> Grouping -> Bundlizer -> Exporter


##一気に大量の素材をimportしたい
importしたい素材をAssetGraphのプロジェクトフォルダに置き、そのパスをLoaderで指定、Importerへと繋ぐと一気にimportができます。

1. Loaderで素材が置いてあるパスを指定
1. Importerを繋いで、ImporterのインスペクタのModify Import Setting ボタンから、import設定をセット


複数の種類の素材(e.g. imageとmodelなど)が含まれている場合、Filterを使って素材の種類ごとにImporterを用意すると設定が楽でいいでしょう。

1. Loaderで素材が置いてあるパスを指定
1. Filterで、素材名やパスから、素材を仕分け
1. Importerを繋いで、ImporterのModify Import Setting ボタンから、import設定をセット



##複数の素材を複数のグループに分ける
たとえばゲームのキャラクターが複数いて、それらがテクスチャ + モデルで構成されている時、
複数の素材を、キャラ1の素材の集まり(テクスチャ + モデル)、 キャラ2の素材の集まり(テクスチャ + モデル)　などのようにグループ分けして扱いたい時があります。

具体的には、Prefabを作る際やAssetBundleをつくる際などです。
Groupingノードを通すと、複数の素材を、複数のグループとして扱うことができます。

GroupingノードのInspectorで、group Key に「グループ分けに使用するキーワード」を指定すると、
素材のパスから複数のグループが作られます。

group Keyでは、\* 記号をワイルドカードとして使用することができます。

たとえばフォルダ名に /ID_mainChara/、/ID_enemy/ などつけた場合、
group Key に /ID_\*/ とセットすると、作成されるグループは"mainChara", "enemy"の2つになります。


##素材からPrefabを作成する
AssetGraphでは、素材を読み込んでPrefabをつくることができます。
ただし、Assetを指定したりインスタンス化する必要があるため、それらの操作をC#Scriptとして記述する必要があります。
Scriptは次のようなものです。

```C#
using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Collections.Generic;

public class MyPrefabricator : AssetGraph.PrefabricatorBase {
	public override void In (string groupKey, List<AssetGraph.AssetInfo> source, string recommendedPrefabOutputDir, Func<GameObject, string, bool, string> Prefabricate) {
		// let's generate Prefab with "Prefabricate" method.
	}
}
```

AssetGraph.PrefabricatorBase クラスを継承し、In メソッドを持つScriptです。
このScriptは、Window > AssetGraph > Generate Script For Node > Prefabricator Script で自動的にひな形を作成できます。

Scriptを作成したら、その型名をAssetGraph内のPrefabricatorノードにセットすることで、ノードに流れてきたAssetが自動的にScriptを通過するようになります。

☆画像

PrefabricatorノードにどんなAssetがどのようなグループ名、順番で入ってくるかは、PrefabricatorノードにつながっているConnectionから想定することができます。

☆画像 

この場合、groupKey "0"で、dummy.png, kiosk001.mat, sample.fbx の3つが順にsourceに入った状態でInメソッドが呼ばれます。



Prefabの作成にPrefabricateメソッドを使うと、キャッシュが効いて便利です。

##適当なグループごとにPrefabを作成する
☆
スクリプト中で、グループが扱える。




##適当なグループ単位でAssetBundleにする
☆
groupingされた単位ごとにAssetBundleができる
bundleNameTemplateに*が含まれていると、グループごとにAssetBundleが作成される。
名前は、米にグループ名が入ったものになる。


##コマンドラインから実行する
☆コマンドラインから、platformとpackageを指定して実行する例


##Importしたファイルや、作成したPrefab、AssetBundleをAssets/外に吐き出したい
☆Exporterの例


##ビルド実行後に処理をしたい
☆Finally

##作成したAssetBundleのcrcやサイズ情報をjson形式で書き出す
☆Finally2の解説

##一つのプラットフォーム内で、特にHD向けなどの調整をしたい
☆packageに関するやつ


##variantsみたいなことがしたい
☆variantsそのものは扱いませんが、pacakgeを使って似たようなことができます。

##キャッシュを消したい
☆window > AssetGraph > Clear Cache

##グラフを消したい
☆AssetGraphのウインドウを開くとハングするようになってしまうなど、読み込もうとしたファイルによって具合が悪くなってしまった場合、Assets/AssetGraph/SettingFiles/AssetGraph.json ファイルを削除すると、グラフを消すことができます。

#Node tips
##Loader
☆AssetGraphにファイルを読みこむ。
loadPathはProjectフォルダのパスから下になっている
loadPathにはAssetsフォルダの内部も指定できる
指定したフォルダは事前につくっておかないといけない。

##Filter
☆キーワードは被るとまずい
パスにキーワードを含むファイルを振り分けることができる。

##Importer
☆一つ~複数のファイルをImportする。
1つのImporterでImport設定がセットできるのは１種類のみ
できるだけ一種類のものを読み込むのを推奨

##Grouping
☆複数のファイルを適当なグループに分割する
キーワードとして米を使う。例

##Prefabricator
☆プレファブを作成する
コードを書く必要がある

##Bundlizer
☆AssetBundleを作成する
Scriptから作る場合は、AssetBundleに限らずzipとかを作るコードも扱える。

##BundleBuilder
☆すべてのAssetBundleを一気に作成する
AssetBundleの設定に関していろいろできる
Bundlizer以外のoutputを受けつけない


##Exporter
☆AssetGraphからファイルを吐き出す
指定できる出力先PathはProjectフォルダの中
吐き出し先のフォルダは事前につくっておかないといけない。


#HookPoint: Finally tips
AssetGraphのビルド処理・リロード処理が終わったタイミングで実行する処理を、Scriptで記述することができます。

##FinallyBase クラスをextendsする
FinallyBase クラスを拡張したコードは、ビルド処理・リロード処理が終わったタイミングで自動的に呼ばれます。

public override void Run (Dictionary<string, Dictionary<string, List<string>>> throughputs, bool isBuild) メソッドで、すべてのNode、すべてのグループの実行結果を受け取ることができます。

Dictionary<string, Dictionary<string, List<string>>> throughputs
NodeName, groupKey, AssetPath が格納されています。

bool isBuild
ビルド処理時はtrue、それ以外ではfalse


##すべてのNodeの実行結果をログに出すサンプル
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
public class SampleFinally : AssetGraph.FinallyBase {
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
[SampleFinally](https://github.com/unity3d-jp/AssetGraph/blob/master/Assets/AssetGraph/UserSpace/Examples/Editor/SampleFinally.cs)

##AssetBundleのmanifestからjsonでリストを作り出す
FinallyはAssetGraphのBuild後に呼ばれるため、AssetBundle作成後にmanifestファイルからAssetBundleの内容を読み込み、json形式に変換する、などといったことができます。

[SampleFinally2](https://github.com/unity3d-jp/AssetGraph/blob/master/Assets/AssetGraph/UserSpace/Examples/Editor/SampleFinally2.cs)


#Package tips
一つのプラットフォームの中に複数の解像度があって、それぞれに応じたサイズの素材を使ってBundleを作成したい、、そう思ったことはないですか。ありますよね。

Unityにはvariantsという機構があり、Assetごとに個別にInspectorから指定することで、「同じGUIDで別の内容を持ったAssetを作り出す」ということができます。
AssetGraphでは、variantsとは少々異なったアプローチで、「同じプラットフォームのフローで、ちょっと違う素材を作り出す」ということができます。
pacakgeを使えば、「HD向けにはこのサイズの素材」「それ以外にはこのサイズの素材」といった調整を、簡単な操作で様々な素材に対して適応させることができます。

##HD用の素材を新たにつくる
すでに「通常サイズの素材を使ってAssetBudnleをつくる」というフローがあり、新たにHD用のAssetBundleも作りたくなった場合、LoaderのInspectorから、新たに「HD」packageを追加してみましょう。

☆画像

これで、「特にHD版の素材を作る場合、専用の画像を使ってAssetBundleを作る」ということが可能になります。

ImporterやBundlizerにも同様に「特にこのpackageだったら」というような、特別なケースの処理を行うことができます。

##variantsとの違い
variantsでは差異のあるAssetを同じGUIDで生成しますが、packageでは、「packageが違うものはすべて別のAsset」として出力します。
出力されるAssetBundleの拡張子は、必ずBUNDLE_NAME.PLATFORM.PACKAGE となります。

名前が異なることからも分かる通り、packageが異なるAssetBundleの間に、crcなどの共通性はありません。

##pacakgeで作ったAssetBundleを使う
使用方法としてはvariantsと違いはなく、次のような手順になります。

1. 端末側で、自分がどのpackageに属するのか、判定を行う
1. HDを使用する端末の場合、末尾にhdとついたAssetBundleを取得する
1. 取得したAssetBundleを使用する

variantsと異なる点としては、packageが異なるAssetBundleはcrcなども全て異なるため、HD用の端末はHD用のAssetBundleのcrc情報などを特に指定して取得する必要があります。


