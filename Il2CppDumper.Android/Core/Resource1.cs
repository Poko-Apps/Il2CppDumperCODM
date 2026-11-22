using System.IO;

namespace Il2CppDumper
{
    // Replaces the WinForms-style Resource1.Designer.cs: reads the dummy dll straight from the embedded resource.
    internal static class Resource1
    {
        private static byte[] il2CppDummyDll;

        internal static byte[] Il2CppDummyDll
        {
            get
            {
                if (il2CppDummyDll == null)
                {
                    using var stream = typeof(Resource1).Assembly.GetManifestResourceStream("Il2CppDummyDll.dll")
                        ?? throw new FileNotFoundException("Embedded resource Il2CppDummyDll.dll not found.");
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    il2CppDummyDll = ms.ToArray();
                }
                return il2CppDummyDll;
            }
        }
    }
}
