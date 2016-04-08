using System;
using System.IO;
using System.Reflection;

namespace IO.Ably.AcceptanceTests
{
    public static class ResourceHelper
    {
        public static string GetResource(string localResName)
        {
            Assembly ass = typeof(TestsSetup).Assembly;
            string defaultNamespace = ass.GetName().Name;
            string resName = $"{defaultNamespace}.{localResName}";
            Stream resourceStream = ass.GetManifestResourceStream(resName);
            if (resourceStream == null)
                throw new Exception("Resource not found: " + resName);

            using (var reader = new StreamReader(resourceStream))
                return reader.ReadToEnd();
        }
    }
}