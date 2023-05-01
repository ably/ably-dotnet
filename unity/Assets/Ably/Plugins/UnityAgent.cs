using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IO.Ably
{
    class UnityAgent
    {
        public static class OS
        {
            public const string Windows = "unity-windows";
            public const string MacOS = "unity-macOS";
            public const string Linux = "unity-linux";
            public const string Android = "unity-android";
            public const string IOS = "unity-iOS";
            public const string TvOS = "unity-tvOS";
            public const string WatchOS = "unity-watchOS";
            public const string WebGL = "unity-webGL";
            public const string PS2 = "unity-PS2";
            public const string PS3 = "unity-PS3";
            public const string PS4 = "unity-PS4";
            public const string PS5 = "unity-PS5";
            public const string xbox = "unity-xbox";
        }

        string start()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.OSXEditor:
                    break;
                case RuntimePlatform.OSXPlayer:
                    return OS.MacOS;
                    break;
                case RuntimePlatform.WindowsPlayer:
                    return OS.Windows;
                    break;
                case RuntimePlatform.OSXWebPlayer:
                    break;
                case RuntimePlatform.OSXDashboardPlayer:
                    break;
                case RuntimePlatform.WindowsWebPlayer:
                    return OS.WebGL;
                    break;
                case RuntimePlatform.WindowsEditor:
                    return OS.Windows;
                    break;
                case RuntimePlatform.IPhonePlayer:
                    return OS.IOS;
                    break;
                case RuntimePlatform.XBOX360:
                    break;
                case RuntimePlatform.PS3:
                    return OS.PS3;
                    break;
                case RuntimePlatform.Android:
                    return OS.Android;
                    break;
                case RuntimePlatform.NaCl:
                    break;
                case RuntimePlatform.FlashPlayer:
                    break;
                case RuntimePlatform.LinuxPlayer:
                    return OS.Linux;
                    break;
                case RuntimePlatform.LinuxEditor:
                    break;
                case RuntimePlatform.WebGLPlayer:
                    return OS.WebGL;
                    break;
                case RuntimePlatform.MetroPlayerX86:
                    break;
                case RuntimePlatform.MetroPlayerX64:
                    break;
                case RuntimePlatform.MetroPlayerARM:
                    break;
                case RuntimePlatform.WP8Player:
                    break;
                case RuntimePlatform.BB10Player:
                    break;
                case RuntimePlatform.TizenPlayer:
                    break;
                case RuntimePlatform.PSP2:
                    return OS.PS2;
                    break;
                case RuntimePlatform.PS4:
                    return OS.PS4;
                    break;
                case RuntimePlatform.PSM:
                    break;
                case RuntimePlatform.XboxOne:
                    return OS.xbox;
                    break;
                case RuntimePlatform.SamsungTVPlayer:
                    break;
                case RuntimePlatform.WiiU:
                    break;
                case RuntimePlatform.tvOS:
                    return OS.TvOS;
                    break;
                case RuntimePlatform.Switch:
                    break;
                case RuntimePlatform.Lumin:
                    break;
                case RuntimePlatform.Stadia:
                    break;
                case RuntimePlatform.CloudRendering:
                    break;
                case RuntimePlatform.GameCoreScarlett:
                    break;
                case RuntimePlatform.GameCoreXboxOne:
                    break;
                case RuntimePlatform.PS5:
                    return OS.PS5;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void stop()
        {

        }
    }
}

