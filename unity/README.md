# ably-unity

## Requirements
1. Unity - 2020.3.19f1 (LTS)
2. Unity script editor - Visual studio (Official IDE)/ Visual studio code (Editor)/ Rider (Intellij IDE)

## Installation
1. Unity - 
- Install unity hub (https://unity3d.com/get-unity/download) to manage unity projects and different versions of unity

2. Visual studio -
- Install official LTS visual studio from https://visualstudio.microsoft.com/downloads/

## Running tests

1. Run editmode tests
```bash
Unity.exe -batchmode -nographics -runTests -projectPath 'C:\Users\${UserName}\UnityProjects\ably-unity' -testResults editmode-results.xml -testPlatform editmode
```

2. Run playmode tests
```bash
Unity.exe -batchmode -nographics -runTests -projectPath 'C:\Users\${UserName}\UnityProjects\ably-unity' -testResults playmode-results.xml -testPlatform playmode
```

## Export unitypackage
```bash
Unity.exe -batchmode -nographics -quit -projectPath 'C:\Users\${UserName}\UnityProjects\ably-unity' -exportPackage 'Assets' 'ably.unitypackage'
```