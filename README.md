# Unity PlayableGraph Monitor Tool

PlayableGraph monitor tool inspired by [PlayableGraph Visualizer](https://github.com/Unity-Technologies/graph-visualizer) and implemented in UIElements.

![PlayableGraph Monitor](./Documents~/imgs/img_sample_playablegraph_monitor.png)

[中文](./README_CN.md)

## Features

- Monitor all valid PlayableGraphs
- Inspect node details in PlayableGraph
- Draggable graph view
- Zoomable graph view
- Show extra label on Playable node

## Supported Unity Version

Unity 2019.4 and later.

## Installation

[![openupm](https://img.shields.io/npm/v/com.greenbamboogames.playablegraphmonitor?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.greenbamboogames.playablegraphmonitor/) 

Install this package via [OpenUPM](https://openupm.com/packages/com.greenbamboogames.playablegraphmonitor/).

## How to use

Open PlayableGraph Monitor window from **menu** "Window/Analysis/PlayableGraph Monitor"，
then select a PlayableGraph from the **popup menu** on the top left of the window.
Select any node in the graph to inspect its details.

Use `PlayableGraphMonitorWindow.Open(IReadOnlyDictionary<PlayableHandle, string>)` or `PlayableGraphMonitorWindow.SetNodeExtraLabelTable(IReadOnlyDictionary<PlayableHandle, string>)` to add extra labels to Playable node.
