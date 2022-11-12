# Unity PlayableGraph 监控工具

PlayableGraph监控工具，参考了[PlayableGraph Visualizer](https://github.com/Unity-Technologies/graph-visualizer)，使用UIElements实现。

![PlayableGraph Monitor](./Documents~/imgs/img_sample_playablegraph_monitor.png)

[English](./README.md)

## 功能

- 监控所有有效的PlayableGraph
- 查看PlayableGraph中节点的详细数据
- 支持拖拽视图
- 支持缩放视图

## 支持的Unity版本

Unity 2019.4或更新版本。

## 安装

[![openupm](https://img.shields.io/npm/v/com.greenbamboogames.playablegraphmonitor?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.cn/packages/com.greenbamboogames.playablegraphmonitor/) 

从 [OpenUPM](https://openupm.cn/packages/com.greenbamboogames.playablegraphmonitor/) 安装。

## 如何使用

从 **菜单** “Window/Analysis/PlayableGraph Monitor” 打开PlayableGraph监控窗口，
在窗口左上角的 **弹出选框** 中选择一个PlayableGraph，即可查看此PlayableGraph的结构。
选中视图中的任意节点，可在窗口右侧面板中查看节点详细数据。
