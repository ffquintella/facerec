using System;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

namespace MLFaceLib.HaarCascadeDetection
{
    public static class XmlResourceLoader
    {
        /// <summary>
        /// Loads an XML document from an embedded resource in the current assembly.
        /// </summary>
        /// <param name="resourceName">The fully qualified resource name.</param>
        /// <returns>An XDocument loaded from the embedded resource.</returns>
        public static XDocument LoadXmlFromResource(string resourceName)
        {
            // Get the current assembly. You might also use typeof(SomeType).Assembly if needed.
            Assembly assembly = Assembly.GetExecutingAssembly();

            // For debugging, you can list all available resources:
            // var names = assembly.GetManifestResourceNames();
            // Console.WriteLine(string.Join(Environment.NewLine, names));

            using Stream stream = assembly.GetManifestResourceStream(resourceName)
                                  ?? throw new Exception($"Resource '{resourceName}' not found. Ensure the file is embedded and the resource name is correct.");
            return XDocument.Load(stream);
        }
    }
}
