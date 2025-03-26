# Unity TouchSocket

TouchSocket是一款简单易用的基础网络通讯组件库，是由原作者`若汝棋茗`及其他贡献者开发，
所有版权归作者若汝棋茗所有，程序集源代码在遵循 Apache License 2.0 的开源协议以及附加协议下，
可免费供其他开发者二次开发或（商业）使用。

你可以从以下仓库访问到完整的TouchSocket源代码。  
[Gitee仓库(主库)](https://gitee.com/rrqm_home/touchsocket)  
[Github仓库](https://github.com/RRQM/TouchSocket)  
[Nuget仓库](https://www.nuget.org/profiles/rrqm)  
[入门指南](https://touchsocket.net/)

+ 本项目只是将其中适用于Unity部分单独提取出来，方便在Unity中一步应用。
+ 本项目提取自仓库中的TouchSocketAll-v3.0.19.unitypackage

## 安装提示

[//]: # (+ 本项目需要从Github中拉取依赖包，请保障Unity可以访问Github，具体可参考[Unity Package Manager无法下载github包的问题]&#40;https://blog.csdn.net/qq_39940718/article/details/133345656&#41;)

[//]: # (和[解决Unity网络问题]&#40;https://docs.unity.cn/cn/2020.3/Manual/upm-config-network.html&#41;)

[//]: # (。  )

+ 引用此包之前请手动安装以下依赖包。

#### 依赖包

+ InputSystem(Unity Registry) :Unity注册登记包，从Unity Package Manager中的`Unity Registry`选项卡中搜索`InputSystem`安装。
+ [UnityWebSocket](https://github.com/psygames/UnityWebSocket)

## 错误修复

+ 您可能会收到关于`TouchSocket.SerialPorts`的`3.0.19`和`3.0.10`程序集重复的错误。删除低版本即可。
