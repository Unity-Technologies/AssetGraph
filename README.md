# Important
**AssetBundleGraphTool project has moved to Unity-Technologies BitBucket** ( https://bitbucket.org/Unity-Technologies/assetbundlegraphtool ). **This repository may be outdated. Also, please send any issues and/or pull request to the active repo in BitBucket.**

# AssetBundleGraphTool

![SS](/Doc/images/readme/graph.png)

AssetBundleGraphTool is a visual toolset that lets you configure and create Unity's AssetBundles. It is intended to create visual, rule-based configuration and manage efficient workflow around AssetBundle generation.

[Quick Q&A](https://bitbucket.org/Unity-Technologies/assetbundlegraphtool/wiki/Home)

##Usage
###1.Add Nodes
Right-clicking the AssetBundleGraphTool canvas will give you a list of nodes you can create. Select one of them to create nodes. First, create a Loader node to identify which assets should go into the AssetBundle.
![SS](/Doc/images/readme/1.png)

###2.Connect Them
When you create more than two nodes, connect them. Simply click the dot on a created node, drag and drop it to the dot of another node to connect them.

AssetBundleGraphTool gives you a live update preview of the asset list that will be passed through the link. By clicking links you will see the full list of assets.

![SS](/Doc/images/readme/2.png)

###3.Configure Settings
By selecting a node, you can configure settings for your AssetBundle building rules. I.e. Filter node lets you configure filtering rules, Importer node lets you configure different import settings you wish to apply to assets that go through that node. 
![SS](/Doc/images/readme/3.png)

###4.Build It!
By clicking Build on the AssetBundleGraphTool window, AssetBundles are built according to rules you have created.
Visual editor lets you build AssetBundles in the way you want while keeping everything easy, repeatable and scalable.
![SS](/Doc/images/readme/4.png)

###5.Born with multi-platform
AssetBundleGraphTool can configure all AssetBundle build settings ready for multi-platform. You can choose the platform to build, and overwrite settings where you want to customize per platform.

![SS](/Doc/images/readme/5.png)
![SS](/Doc/images/readme/6.png)


##Why Rule-based?
AssetBundleGraphTool handles AssetBundle build pipelines by rules, which enables programmers to safely build simple workflows with artists or game designers without making them worry about AssetBundle configuration. When they add new assets into a project, AssetBundleGraphTool automatically takes care of them and builds necessary AssetBundles using the rule(s) that you have made. 




##Nodes
There are several types of node you can use in this graph tool.

###Loader
Loader is your starting point of building AssetBundles. Loader finds and gives asset lists to following nodes.  
- OUT: list of assets under given directory

![SS](/Doc/images/readme/1000.png)

###Filter
Filter creates sub-list of assets coming from the previous node. You can add multiple filtering rules to create multiple sub-lists.
- IN: list of assets
- OUT: list of assets which matches given filter setting

![SS](/Doc/images/readme/600.png)  

###ImportSetting
ImportSetting overwrites import settings of assets passed by the previous node.
- IN: list of assets
- OUT: list of assets with given import settings applied

![SS](/Doc/images/readme/500.png)  

###Modifier
Modifier modifies asset configuration directly. You can also create your own modifier by implementing IModifier.
- IN: list of group of assets
- OUT: list of group of assets.

![SS](/Doc/images/readme/1100.png)


###Grouping
Grouping makes a group of resources from a list of assets using the configured pattern.
"Group" is very a useful approach in building AssetBundles.
- IN: list of assets
- OUT: list of group of assets

![SS](/Doc/images/readme/400.png)  

###PrefabBuilder
PrefabBuilder is a node that lets you create Prefabs according to your needs.
- IN: list of group of assets
- OUT: list of group of assets with your prefab

![SS](/Doc/images/readme/700.png)  

###BundleConfig
BundleConfig creates a catalog of the AssetBundle's content from a given group of assets.  Asterisks (*) in the template name will be replaced by the group identifier.  You can also create variants with BundleConfig.
- IN: list of group of assets
- OUT: list of group of assets in AssetBundle name.

![SS](/Doc/images/readme/800.png)


###BundleBuilder
BundleBuilder creates actual AssetBundle files from a given list of bundle configurations. By using BundleConfig and BundleBuilder(s), you can simultaneously create AssetBundles with different AssetBundle configuration (i.e. compressed & uncompressed)

- IN: list of bundles
- OUT: list of generated AssetBundle files (including manifests)

![SS](/Doc/images/readme/100.png)


###Exporter
Exporter saves the given assets into the given directory.  You can also select a directory outside /Assets/. 
- IN: list of assets (or AssetBundle files)

![SS](/Doc/images/readme/900.png)


#License

The MIT License (MIT)

Copyright (c) 2016 Unity Technologies

Copyright (c) 2013 Calvin Rien

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
