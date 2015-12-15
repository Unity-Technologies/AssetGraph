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

いちいち手でインポート設定をいじったり、Prefabを作ったりする必要がないので、特に大量のファイルから大量のAssetBundleを作ったり、大量のファイルに調整を施すのに適しています。
これ手作業でやるのしんどいな〜みたいな数までAssetBundleが増えたら、是非試してみてください。

(アンチョコ)
手順：
ノードのつなぎ方、既存のREADMEを丸パク

How to

NodeのTips
	いろんなところからここにつながれるような文字引き

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
☆

1. Loaderで素材が置いてあるパスを指定
2. Importerを繋いで、ImporterのModify Import Setting ボタンから、import設定をセット


##すでにImport済みの素材を使いたい
☆
Assetsフォルダの外部から、すでにimport済みの素材をImportする場合は、.metaファイルが必要になります。
.metaファイルと一緒に素材ファイルをimportした場合、たぶんそれが優先されます



##複数の素材を複数のグループに分ける
☆
Groupingを使おう。
素材のPathを使って、複数の素材を一つのグループとして扱える。
groupKeyに米を使うと、


##素材からPrefabを作成する
☆
スクリプトを書こう、みたいな。
WindowからのPrefabricatorを案内する。
入ってくる素材とグループIdについての説明、流入するConnectionの順番の解説が必要
サンプルコードを示唆

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

##HD向けなどの調整をしたい
☆packageに関するやつ




#Node tips
##Loader
☆loadPathは内部でも外部でも使える

##Filter
☆キーワードは被るとまずい
パスにキーワードを含むファイルを吐き出す

##Importer
☆一つ~複数のファイルをImportする。
1つのImporterでImport設定がセットできるのは１種類のみ
できるだけ一種類のものを読み込むのを推奨
フォルダは事前につくっておかないといけない。

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
☆生成したファイルやimportしたファイルをAssets内・外にコピーする。
フォルダは事前につくっておかないといけない。


#HookPoint tips
##Finally
☆AssetGraphのビルド処理・リロード処理が終わったタイミングで実行する処理を、Scriptで記述できる。
FinallyBase クラスを拡張したコードで可能。
すべてのNodeの実行結果をログに出すサンプルはこんな感じ
SampleFinally

作成したAssetBundleのmanifestからjsonリストを作り出すサンプルはこんな感じ
SampleFinally2


#Package tips
☆一つのプラットフォームの中に複数の解像度があって、それぞれに応じたサイズの素材を使ったりしたい、、そう思ったことはないですか。ありますよね。
pacakgeを使えば、「HD向けにはこのサイズの素材」「それ以外にはこのサイズの素材」といった調整をGUIから行うことができます。

いろんなNodeにpackageってパラメータがあるんで使ってねみたいな話
