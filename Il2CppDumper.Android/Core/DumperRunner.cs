using System;
using System.IO;
using System.Linq;

namespace Il2CppDumper
{
    public sealed class DumpOptions
    {
        public string Il2CppPath { get; set; }
        public string MetadataPath { get; set; }
        public string OutputDir { get; set; }

        /// <summary>Base address for a memory-dumped .so (replaces the console prompt). Null/0 = force continue.</summary>
        public ulong? DumpAddress { get; set; }

        /// <summary>Manual mode values, used only when auto search fails.</summary>
        public ulong? CodeRegistration { get; set; }
        public ulong? MetadataRegistration { get; set; }
    }

    /// <summary>
    /// Non-interactive port of Program.Init / Program.Dump. All progress still goes to Console,
    /// so the UI just redirects Console.Out to capture it.
    /// </summary>
    public static class DumperRunner
    {
        public static bool Run(Config config, DumpOptions options)
        {
            if (!Init(config, options, out var metadata, out var il2Cpp))
            {
                return false;
            }
            Dump(config, metadata, il2Cpp, options.OutputDir);
            return true;
        }

        private static bool Init(Config config, DumpOptions options, out Metadata metadata, out Il2Cpp il2Cpp)
        {
            metadata = null;
            il2Cpp = null;

            Console.WriteLine("Initializing metadata...");
            var metadataBytes = File.ReadAllBytes(options.MetadataPath);
            if (metadataBytes.Length < 4 || BitConverter.ToUInt32(metadataBytes, 0) != 0xFAB11BAF)
            {
                Console.WriteLine("WARNING: Metadata magic not found. The file may be encrypted or not a global-metadata.dat.");
            }
            metadata = new Metadata(new MemoryStream(metadataBytes));
            Console.WriteLine($"Metadata Version: {metadata.Version}");

            Console.WriteLine("Initializing il2cpp file...");
            var il2cppBytes = File.ReadAllBytes(options.Il2CppPath);
            var il2cppMagic = BitConverter.ToUInt32(il2cppBytes, 0);
            var il2CppMemory = new MemoryStream(il2cppBytes);
            switch (il2cppMagic)
            {
                default:
                    throw new NotSupportedException("ERROR: il2cpp file not supported.");
                case 0x464c457f: //ELF
                    if (il2cppBytes[4] == 2) //ELF64
                    {
                        il2Cpp = new Elf64(il2CppMemory);
                    }
                    else
                    {
                        il2Cpp = new Elf(il2CppMemory);
                    }
                    break;
                case 0xCAFEBABE: //FAT Mach-O
                case 0xBEBAFECA:
                    {
                        var machofat = new MachoFat(new MemoryStream(il2cppBytes));
                        // No keyboard on Android: prefer the 64-bit slice automatically.
                        var index = Array.FindIndex(machofat.fats, f => f.magic == 0xFEEDFACF);
                        if (index < 0) index = 0;
                        Console.WriteLine($"FAT Mach-O: using slice {index + 1} ({(machofat.fats[index].magic == 0xFEEDFACF ? "64bit" : "32bit")})");
                        var magic = machofat.fats[index].magic;
                        il2cppBytes = machofat.GetMacho(index);
                        il2CppMemory = new MemoryStream(il2cppBytes);
                        if (magic == 0xFEEDFACF)
                            il2Cpp = new Macho64(il2CppMemory);
                        else
                            il2Cpp = new Macho(il2CppMemory);
                        break;
                    }
                case 0xFEEDFACF: // 64bit Mach-O
                    il2Cpp = new Macho64(il2CppMemory);
                    break;
                case 0xFEEDFACE: // 32bit Mach-O
                    il2Cpp = new Macho(il2CppMemory);
                    break;
            }

            var version = config.ForceIl2CppVersion ? config.ForceVersion : metadata.Version;
            il2Cpp.SetProperties(version, metadata.metadataUsagesCount);
            Console.WriteLine($"Il2Cpp Version: {il2Cpp.Version}");

            if (config.ForceDump || il2Cpp.CheckDump())
            {
                if (il2Cpp is ElfBase elf)
                {
                    Console.WriteLine("Detected this may be a dump file.");
                    var dumpAddr = options.DumpAddress ?? 0;
                    if (dumpAddr != 0)
                    {
                        Console.WriteLine($"Using dump address: {dumpAddr:X}");
                        il2Cpp.ImageBase = dumpAddr;
                        il2Cpp.IsDumped = true;
                        if (!config.NoRedirectedPointer)
                        {
                            elf.Reload();
                        }
                    }
                    else
                    {
                        Console.WriteLine("No dump address given, forcing continue (enter one in 'Dump address' if output is wrong).");
                    }
                }
                else
                {
                    il2Cpp.IsDumped = true;
                }
            }

            Console.WriteLine("Searching...");
            try
            {
                var flag = il2Cpp.PlusSearch(metadata.methodDefs.Count(x => x.methodIndex >= 0), metadata.typeDefs.Length, metadata.imageDefs.Length);
                if (!flag)
                {
                    flag = il2Cpp.Search();
                }
                if (!flag)
                {
                    flag = il2Cpp.SymbolSearch();
                }
                if (!flag)
                {
                    Console.WriteLine("ERROR: Can't use auto mode to process file, try manual mode.");
                    if (options.CodeRegistration is ulong codeRegistration && options.MetadataRegistration is ulong metadataRegistration)
                    {
                        Console.WriteLine($"Using manual CodeRegistration {codeRegistration:X}, MetadataRegistration {metadataRegistration:X}");
                        il2Cpp.Init(codeRegistration, metadataRegistration);
                    }
                    else
                    {
                        Console.WriteLine("Fill in CodeRegistration and MetadataRegistration (hex) under Advanced, then run again.");
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine("ERROR: An error occurred while processing.");
                return false;
            }
            return true;
        }

        private static void Dump(Config config, Metadata metadata, Il2Cpp il2Cpp, string outputDir)
        {
            if (!outputDir.EndsWith(Path.DirectorySeparatorChar))
            {
                outputDir += Path.DirectorySeparatorChar;
            }
            Directory.CreateDirectory(outputDir);

            Console.WriteLine("Dumping...");
            var executor = new Il2CppExecutor(metadata, il2Cpp);
            var decompiler = new Il2CppDecompiler(executor);
            decompiler.Decompile(config, outputDir);
            Console.WriteLine("Done!");
            if (config.GenerateStruct)
            {
                Console.WriteLine("Generate struct...");
                var scriptGenerator = new StructGenerator(executor);
                scriptGenerator.WriteScript(outputDir);
                Console.WriteLine("Done!");
            }
            if (config.GenerateDummyDll)
            {
                Console.WriteLine("Generate dummy dll...");
                DummyAssemblyExporter.Export(executor, outputDir, config.DummyDllAddToken);
                Console.WriteLine("Done!");
            }
        }
    }
}
