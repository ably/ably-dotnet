using System;
using System.IO;
using System.Reflection;

namespace IO.Ably.Tests
{
    public static class ResourceHelper
    {
        public static string GetResource(string localResName)
        {
            Assembly ass = typeof(ResourceHelper).Assembly;
            string defaultNamespace = ass.GetName().Name;
            string resName = $"{defaultNamespace}.{localResName}";
            Stream resourceStream = ass.GetManifestResourceStream(resName);
            if (resourceStream == null)
            {
                throw new Exception("Resource not found: " + resName);
            }

            using var reader = new StreamReader(resourceStream);
            return reader.ReadToEnd();
        }

        public static byte[] GetBinaryResource(string localResName)
        {
            Assembly ass = typeof(ResourceHelper).Assembly;
            string defaultNamespace = ass.GetName().Name;
            string resName = $"{defaultNamespace}.{localResName}";
            using Stream resourceStream = ass.GetManifestResourceStream(resName);
            if (resourceStream == null)
            {
                throw new Exception("Resource not found: " + resName);
            }

            byte[] data = new byte[resourceStream.Length];
            resourceStream.Read(data, 0, data.Length);
            return data;
        }
    }
}
