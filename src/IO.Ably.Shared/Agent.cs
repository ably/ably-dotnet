using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#if NETSTANDARD2_0 && UNITY_PACKAGE
using UnityEngine; // lib/UnityEngine.dll - 2019.4.40 LTS compile time, at runtime unity player version will be used.
#endif

namespace IO.Ably
{
    internal static class Agent
    {
        public enum PlatformRuntime
        {
            Framework,
            Netstandard20,
            Net6,
            Net7,
            XamarinAndroid,
            XamarinIos,
            Other,
        }

        public static class OS
        {
            public const string Windows = "dotnet-windows";
            public const string MacOS = "dotnet-macOS";
            public const string Linux = "dotnet-linux";
            public const string Android = "dotnet-android";
            public const string IOS = "dotnet-iOS";
            public const string TvOS = "dotnet-tvOS";
            public const string WatchOS = "dotnet-watchOS";
            public const string Browser = "dotnet-browser";
        }

        internal const string AblyAgentHeader = "Ably-Agent";
        private static readonly string AblySdkIdentifier = $"ably-dotnet/{Defaults.LibraryVersion}"; // RSC7d1

        /// <summary>
        /// This returns dotnet platform as per ably-lib mappings defined in agents.json.
        /// https://github.com/ably/ably-common/blob/main/protocol/agents.json.
        /// This is required since we are migrating from 'X-Ably-Lib' header (RSC7b) to agent headers (RSC7d).
        /// Please note that uwp platform is Deprecated and removed as a part of https://github.com/ably/ably-dotnet/pull/1101.
        /// </summary>
        /// <returns> Clean Platform Identifier. </returns>
        internal static string DotnetRuntimeIdentifier()
        {
            string DotnetRuntimeName()
            {
                switch (IoC.PlatformId)
                {
                    case PlatformRuntime.Framework:
                        return "dotnet-framework";
                    case PlatformRuntime.Netstandard20:
                        return "dotnet-standard";
                    case PlatformRuntime.Net6:
                        return "dotnet6";
                    case PlatformRuntime.Net7:
                        return "dotnet7";
                    case PlatformRuntime.XamarinAndroid:
                        return "xamarin";
                    case PlatformRuntime.XamarinIos:
                        return "xamarin";
                }

                return string.Empty;
            }

            var dotnetRuntimeName = DotnetRuntimeName();

            string DotnetRuntimeVersion()
            {
                try
                {
                    return Environment.Version.ToString();
                }
                catch
                {
                    return string.Empty;
                }
            }

            var dotnetRuntimeVersion = DotnetRuntimeVersion();

            return dotnetRuntimeVersion.IsEmpty() ?
                dotnetRuntimeName : $"{dotnetRuntimeName}/{dotnetRuntimeVersion}";
        }

#if NETSTANDARD2_0 && UNITY_PACKAGE
        internal static string UnityPlayerIdentifier()
        {
            return Application.unityVersion.IsEmpty() ?
                "unity" : $"unity/{Application.unityVersion}";
        }

        public static class UnityOS
        {
            public const string Windows = "unity-windows";
            public const string MacOS = "unity-macOS";
            public const string Linux = "unity-linux";
            public const string Android = "unity-android";
            public const string IOS = "unity-iOS";
            public const string TvOS = "unity-tvOS";
            public const string WebGL = "unity-webGL";
            public const string Switch = "unity-nintendo-switch";
            public const string PS4 = "unity-PS4";
            public const string PS5 = "unity-PS5";
            public const string Xbox = "unity-xbox";
        }

        internal static string UnityOsIdentifier()
        {
            // lib/UnityEngine.dll - 2019.4.40 LTS compile time.
            // Added cases for consistent platforms for future versions of unity.
            // At runtime unity player version >= 2019.x.x will be used.
            switch (Application.platform)
            {
                case RuntimePlatform.OSXEditor:
                    return UnityOS.MacOS;
                case RuntimePlatform.OSXPlayer:
                    return UnityOS.MacOS;
                case RuntimePlatform.WindowsPlayer:
                    return UnityOS.Windows;
                case RuntimePlatform.WindowsEditor:
                    return UnityOS.Windows;
                case RuntimePlatform.IPhonePlayer:
                    return UnityOS.IOS;
                case RuntimePlatform.Android:
                    return UnityOS.Android;
                case RuntimePlatform.LinuxPlayer:
                    return UnityOS.Linux;
                case RuntimePlatform.LinuxEditor:
                    return UnityOS.Linux;
                case RuntimePlatform.WebGLPlayer:
                    return UnityOS.WebGL;
                case RuntimePlatform.PS4:
                    return UnityOS.PS4;
                case RuntimePlatform.XboxOne:
                    return UnityOS.Xbox;
                case RuntimePlatform.tvOS:
                    return UnityOS.TvOS;
                case RuntimePlatform.Switch:
                    return UnityOS.Switch;
                case RuntimePlatform.PS5:
                    return UnityOS.PS5;
            }

            return string.Empty;
        }
#endif

        internal static string OsIdentifier()
        {
            switch (IoC.PlatformId)
            {
                case PlatformRuntime.XamarinAndroid:
                    return OS.Android;
                case PlatformRuntime.XamarinIos:
                    return OS.IOS;
            }

            // Preprocessors defined as per https://learn.microsoft.com/en-us/dotnet/standard/frameworks#preprocessor-symbols
            // Get operating system as per https://stackoverflow.com/a/66618677 for .NET5 and greater
#if NET5_0_OR_GREATER
            if (OperatingSystem.IsWindows())
            {
                return OS.Windows;
            }

            if (OperatingSystem.IsLinux())
            {
                return OS.Linux;
            }

            if (OperatingSystem.IsMacOS())
            {
                return OS.MacOS;
            }

            if (OperatingSystem.IsAndroid())
            {
                return OS.Android;
            }

            if (OperatingSystem.IsIOS())
            {
                return OS.IOS;
            }

            if (OperatingSystem.IsTvOS())
            {
                return OS.TvOS;
            }

            if (OperatingSystem.IsWatchOS())
            {
                return OS.WatchOS;
            }

            if (OperatingSystem.IsBrowser())
            {
                return OS.Browser;
            }
#endif

            // If netstandard target is used by .Net Core App, https://mariusschulz.com/blog/detecting-the-operating-system-in-net-core
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return OS.Linux;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return OS.MacOS;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return OS.Windows;
                }
            }
            catch
            {
                // ignored
            }

#if NETSTANDARD2_0 && UNITY_PACKAGE
            return UnityOsIdentifier();
#endif

            // If netframework/netstandard target is used by .Net Mono App, http://docs.go-mono.com/?link=P%3aSystem.Environment.OSVersion
            // https://stackoverflow.com/questions/9129491/c-sharp-compiled-in-mono-detect-os
#pragma warning disable CS0162 // Disable code unreachable warning
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                    return OS.Windows;
                case PlatformID.Unix:
                    return OS.Linux;
                case PlatformID.MacOSX:
                    return OS.MacOS;
            }

            return string.Empty;
        }

        internal static string AblyAgentIdentifier(Dictionary<string, string> additionalAgents)
        {
            string GetAgentComponentString(string product, string version)
            {
                return string.IsNullOrEmpty(version) ? product : $"{product}/{version}";
            }

            void AddAgentIdentifier(ICollection<string> currentAgentComponents, string product, string version = null)
            {
                if (!string.IsNullOrEmpty(product))
                {
                    currentAgentComponents.Add(GetAgentComponentString(product, version));
                }
            }

            var agentComponents = new List<string>();
            AddAgentIdentifier(agentComponents, AblySdkIdentifier);
            AddAgentIdentifier(agentComponents, DotnetRuntimeIdentifier());

#if NETSTANDARD2_0 && UNITY_PACKAGE
            AddAgentIdentifier(agentComponents, UnityPlayerIdentifier());
#endif

            AddAgentIdentifier(agentComponents, OsIdentifier());

            if (additionalAgents == null)
            {
                return string.Join(" ", agentComponents);
            }

            foreach (var agent in additionalAgents)
            {
                AddAgentIdentifier(agentComponents, agent.Key, agent.Value);
            }

            return string.Join(" ", agentComponents);
        }
    }
}
