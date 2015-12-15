#AssetGraph Document
document version 0.8.0

##TOC
* はじめに
* 逆引きAssetGraph(how to)
* Node tips
* HookPoint tips


#はじめに
AssetGraphは、様々なファイルをUnityにインポート、プレファブ化したりしたあとAssetBundleにして出力することに特化しているツールです。

特に大量のファイルから大量のAssetBundleを作ったり、大量のファイルに調整を施すのに適しています。
これ手作業でやるのしんどいな〜みたいな数までAssetBundleが増えたら、是非試してみてください。

手順：
ノードのつなぎ方、既存のREADMEを丸パク

How to

NodeのTips
	いろんなところからここにつながれるような文字引き



#逆引きAssetGraph(how to)


##複数の素材から一つのAssetBundleを作成する
☆
importして
Bundlizerを使う。
bundleNameTemplateにbundle名をセット
*マークは、複数のグループをそれぞれAssetBundleにするときに便利。


##大量の素材をAssetBundleにしたい
☆


##一気に大量の素材をimportしたい
1. Loaderで素材が置いてあるパスを指定
2. Importerを繋いで、ImporterのModify Import Setting ボタンから、import設定をセット
3. Exporterを繋いで、importが済んだ素材を吐き出す

(metaファイルの扱いは未定です、みたいな話)(ここで作ったものを、metaごと外部に持ち出すのは満たしていない。)


##すでにImport済みの素材を使いたい
☆
UnityProjectの外部から、すでにimport済みの素材をImportする場合は、.metaファイルが必要になります。



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




##適当なグループでAssetBundleにする
☆
groupingされた単位ごとにAssetBundleができる
bundleNameTemplateに*が含まれていると、グループごとにAssetBundleが作成される。
名前は、米にグループ名が入ったものになる。


##コマンドラインから実行する
☆コマンドラインから、platformとpackageを指定して実行する例


##実行後に処理をしたい
☆Finally

##作成したAssetBundleの内容をjsonにする
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
セッティングできるのは一つだけ
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