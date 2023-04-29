using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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

        // Note - MAUI OS detection requires maui specific dependencies, https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/device/information?view=net-maui-7.0&tabs=windows
        internal static string OsIdentifier()
        {
            switch (IoC.PlatformId)
            {
                // For windows only dotnet-framework, return windows OS => https://dotnet.microsoft.com/en-us/download/dotnet-framework
                case PlatformRuntime.Framework:
                    return OS.Windows;
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

            // If netstandard target is used by .Net Mono App, http://docs.go-mono.com/?link=P%3aSystem.Environment.OSVersion
            // https://stackoverflow.com/questions/9129491/c-sharp-compiled-in-mono-detect-os
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
