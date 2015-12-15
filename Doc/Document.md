#ドキュメント 草案みたいなところまで書いたら見せる

##TOC

document version 1.0.0


##はじめに
AssetGraphは、Unityの外側に用意した素材をいろいろ調整、プレファブ化したりしたあとAssetBundleにして出力することに特化しているツールです。

特に大量の素材から大量のAssetBundleを作るのに適しています。
これ手作業でやるのしんどいな〜みたいな数までAssetBundleが増えたら、是非試してみてください。

手順：
ノードのつなぎ方、既存のREADMEを丸パク

How to

NodeのTips
	いろんなところからここにつながれるような文字引き



##how to = 逆引き


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
☆

##実行後に処理をしたい
☆Finally

##作成したAssetBundleの内容をjsonにする
☆Finally2の解説



#Node Tips
##Loader
##Filter
##Importer
##Grouping
##Prefabricator
##Bundlizer
##BundleBuilder
##Exporter

#HookPoint Tips


