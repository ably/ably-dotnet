# Ably Unity SDK
- Supports both [Mono](https://docs.unity3d.com/Manual/Mono.html) and [IL2CPP](https://docs.unity3d.com/Manual/IL2CPP.html) builds.
- Supports **Windows**, **MacOS**, **Linux**, **Android** and **iOS**.

### System Requirements
* Unity 2019.x.x or newer
* The following Unity Player settings must be applied:
  * Scripting Runtime Version should be '.NET 4.x Equivalent'
  * API Compatibility Level should be '.NET Standard 2.0'

### Downloading Unity Package
- Please download the latest Unity package from the [GitHub releases page](https://github.com/ably/ably-dotnet/releases/latest). All releases from 1.2.4 have `.unitypackage` included.

### Importing Unity Package
- You can import the package by going to Assets -> Import Package -> Custom Package in the Unity UI. For more detailed information on importing packages, visit https://docs.unity3d.com/Manual/AssetPackagesImport.html.
- Make sure to [disable assembly validation](CONTRIBUTING.md#disable-assembly-validation-error) if your project fails to build due to conflicts with Unity's internal newtonsoft json library.
- Please set `ClientOptions.AutomaticNetworkStateMonitoring` to `false` when instantiating the Ably Client Library, since this feature is not supported and will throw a runtime exception.
- Custom [NewtonSoft JSON DLLs](https://github.com/jilleJr/Newtonsoft.Json-for-Unity) under `Plugins` can be removed, in the case of conflict with other NewtonSoft DLLs in the project, or if the use of [inbuilt Newtonsoft](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.0/manual/index.html) is preferred.
- [Configure SynchronizationContext](../README.md#executing-callbacks-on-mainui-thread) to execute callbacks on Main/UI thread.
- Sample code :

```dotnet
using System;
using System.Threading;
using IO.Ably;
using IO.Ably.Realtime;
using UnityEngine;
using UnityEngine.UI;

namespace Example.ChatApp
{
    public class AblyConsole : MonoBehaviour
    {
        private AblyRealtime _ably;
        private ClientOptions _clientOptions;

        // It's recommended to use other forms of authentication. E.g. JWT, Token Auth 
        // This is to avoid exposing root api key to a client
        private static string _apiKey = "ROOT_API_KEY_COPIED_FROM_ABLY_WEB_DASHBOARD";

        void Start()
        {
            InitializeAbly();
        }

        private void InitializeAbly()
        {
            _clientOptions = new ClientOptions
            {
                Key = _apiKey,
                // this will disable the library trying to subscribe to network state notifications
                AutomaticNetworkStateMonitoring = false,
                AutoConnect = false,
                // this will make sure to post callbacks on UnitySynchronization Context Main Thread
                CustomContext = SynchronizationContext.Current
            };

            _ably = new AblyRealtime(_clientOptions);
            _ably.Connection.On(args =>
            {
                Debug.Log($"Connection State is <b>{args.Current}</b>");
            });
        }
    }
```
- Please take a look at [Unity Demo Chat App](./Assets/Ably/Examples/Chat/) to see a functioning Ably SDK setup.

### Unsupported Platforms
- It doesn't support **WebGL** due to incompatibility with WebSockets. Read the [Direct Socket Access](https://docs.unity3d.com/2019.3/Documentation/Manual/webgl-networking.html) section under WebGL Networking. We have active issue to add support for the same https://github.com/ably/ably-dotnet/issues/1211.
- To support **WebGL**, you should refer to [interation with browser javascript from WebGL](https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html). You can import [ably-js](https://github.com/ably/ably-js) as a browser javascript and call it from WebGL. For more information, refer to the project [Ably Tower Defence](https://github.com/ably-labs/ably-tower-defense/tree/js-branch/).


### Contributing
- Please take a look at the [contributing doc](CONTRIBUTING.md) for information relating to local development setup, writing and running tests.
- Detailed information related to updating Ably DLLs using [Unity Plug-ins](https://docs.unity3d.com/Manual/Plugins.html) is available in [updating-ably-unitypackage](CONTRIBUTING.md#updating-the-ably-unity-package).
