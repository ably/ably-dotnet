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

        void start()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.OSXEditor:
                    break;
                case RuntimePlatform.OSXPlayer:
                    break;
                case RuntimePlatform.WindowsPlayer:
                    break;
                case RuntimePlatform.OSXWebPlayer:
                    break;
                case RuntimePlatform.OSXDashboardPlayer:
                    break;
                case RuntimePlatform.WindowsWebPlayer:
                    break;
                case RuntimePlatform.WindowsEditor:
                    break;
                case RuntimePlatform.IPhonePlayer:
                    break;
                case RuntimePlatform.XBOX360:
                    break;
                case RuntimePlatform.PS3:
                    break;
                case RuntimePlatform.Android:
                    break;
                case RuntimePlatform.NaCl:
                    break;
                case RuntimePlatform.FlashPlayer:
                    break;
                case RuntimePlatform.LinuxPlayer:
                    break;
                case RuntimePlatform.LinuxEditor:
                    break;
                case RuntimePlatform.WebGLPlayer:
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
                    break;
                case RuntimePlatform.PS4:
                    break;
                case RuntimePlatform.PSM:
                    break;
                case RuntimePlatform.XboxOne:
                    break;
                case RuntimePlatform.SamsungTVPlayer:
                    break;
                case RuntimePlatform.WiiU:
                    break;
                case RuntimePlatform.tvOS:
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

