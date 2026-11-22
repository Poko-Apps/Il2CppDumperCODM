using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;

namespace Il2CppDumper.Mobile
{
    public class MainPage : ContentPage
    {
        private string il2cppPath;
        private string metadataPath;
        private string lastOutputDir;
        private bool running;

        private readonly Label il2cppLabel = new() { Text = "No file selected", FontSize = 12, TextColor = Colors.Gray };
        private readonly Label metadataLabel = new() { Text = "No file selected", FontSize = 12, TextColor = Colors.Gray };
        private readonly Label statusLabel = new() { FontSize = 13 };
        private readonly Button dumpButton = new() { Text = "Dump" };
        private readonly Button shareButton = new() { Text = "Share / save output (.zip)", IsEnabled = false };
        private readonly ActivityIndicator spinner = new() { IsRunning = false, IsVisible = false };
        private readonly Editor logView = new()
        {
            IsReadOnly = true,
            FontFamily = "monospace",
            FontSize = 11,
            HeightRequest = 320,
        };

        private readonly VerticalStackLayout optionsLayout = new() { Spacing = 2 };

        // Defaults mirror Il2CppDumper/config.json
        private Switch dumpMethod, dumpField, dumpProperty, dumpAttribute, dumpFieldOffset, dumpMethodOffset,
            dumpTypeDefIndex, generateDummyDll, generateStruct, dummyDllAddToken, forceIl2CppVersion, forceDump, noRedirectedPointer;

        private readonly Entry forceVersionEntry = new() { Text = "16", Keyboard = Keyboard.Numeric, Placeholder = "e.g. 24.3" };
        private readonly Entry dumpAddressEntry = new() { Placeholder = "Dump address (hex) – only for memory-dumped .so", Keyboard = Keyboard.Text };
        private readonly Entry codeRegEntry = new() { Placeholder = "CodeRegistration (hex) – manual mode", Keyboard = Keyboard.Text };
        private readonly Entry metaRegEntry = new() { Placeholder = "MetadataRegistration (hex) – manual mode", Keyboard = Keyboard.Text };

        private LogWriter logWriter;

        public MainPage()
        {
            Title = "Il2CppDumper";

            var pickIl2cpp = new Button { Text = "Pick libil2cpp.so" };
            pickIl2cpp.Clicked += async (_, _) => await PickAsync(isMetadata: false);
            var pickMetadata = new Button { Text = "Pick global-metadata.dat" };
            pickMetadata.Clicked += async (_, _) => await PickAsync(isMetadata: true);

            dumpButton.Clicked += async (_, _) => await RunDumpAsync();
            shareButton.Clicked += async (_, _) => await ShareOutputAsync();

            dumpMethod = AddToggle("Dump methods", true);
            dumpField = AddToggle("Dump fields", true);
            dumpProperty = AddToggle("Dump properties", true);
            dumpAttribute = AddToggle("Dump attributes", true);
            dumpFieldOffset = AddToggle("Dump field offsets", true);
            dumpMethodOffset = AddToggle("Dump method offsets", true);
            dumpTypeDefIndex = AddToggle("Dump TypeDefIndex", true);
            generateDummyDll = AddToggle("Generate dummy DLLs", true);
            dummyDllAddToken = AddToggle("Dummy DLL: add tokens", true);
            generateStruct = AddToggle("Generate struct / script.json", true);
            forceDump = AddToggle("Force dump mode", false);
            noRedirectedPointer = AddToggle("No redirected pointer", false);
            forceIl2CppVersion = AddToggle("Force il2cpp version", false);
            optionsLayout.Add(forceVersionEntry);

            var advanced = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = "Only needed if the log asks for them:", FontSize = 12, TextColor = Colors.Gray },
                    dumpAddressEntry,
                    codeRegEntry,
                    metaRegEntry,
                }
            };

            var optionsToggle = new Button { Text = "Show options ▾", BackgroundColor = Colors.Transparent, TextColor = Colors.DodgerBlue };
            var optionsContainer = new VerticalStackLayout
            {
                IsVisible = false,
                Spacing = 10,
                Children = { optionsLayout, Header("Advanced"), advanced }
            };
            optionsToggle.Clicked += (_, _) =>
            {
                optionsContainer.IsVisible = !optionsContainer.IsVisible;
                optionsToggle.Text = optionsContainer.IsVisible ? "Hide options ▴" : "Show options ▾";
            };

            Content = new ScrollView
            {
                Content = new VerticalStackLayout
                {
                    Padding = new Thickness(16),
                    Spacing = 10,
                    Children =
                    {
                        Header("1. Input files"),
                        pickIl2cpp, il2cppLabel,
                        pickMetadata, metadataLabel,
                        optionsToggle,
                        optionsContainer,
                        Header("2. Run"),
                        dumpButton,
                        spinner,
                        statusLabel,
                        shareButton,
                        Header("Log"),
                        logView,
                    }
                }
            };
        }

        private static Label Header(string text) =>
            new() { Text = text, FontAttributes = FontAttributes.Bold, FontSize = 16, Margin = new Thickness(0, 8, 0, 0) };

        private Switch AddToggle(string text, bool initial)
        {
            var sw = new Switch { IsToggled = initial, HorizontalOptions = LayoutOptions.End };
            var grid = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }
            };
            var label = new Label { Text = text, VerticalOptions = LayoutOptions.Center };
            grid.Add(label, 0, 0);
            grid.Add(sw, 1, 0);
            optionsLayout.Add(grid);
            return sw;
        }

        private async Task PickAsync(bool isMetadata)
        {
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = isMetadata ? "Select global-metadata.dat" : "Select libil2cpp.so"
                });
                if (result == null) return;

                // Copy into our cache so the dumper gets a normal file path.
                var dir = Path.Combine(FileSystem.CacheDirectory, isMetadata ? "input_metadata" : "input_il2cpp");
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
                Directory.CreateDirectory(dir);
                var dest = Path.Combine(dir, string.IsNullOrEmpty(result.FileName) ? "input.bin" : result.FileName);

                statusLabel.Text = "Copying file...";
                await using (var src = await result.OpenReadAsync())
                await using (var dst = File.Create(dest))
                {
                    await src.CopyToAsync(dst);
                }

                var size = new FileInfo(dest).Length;
                var text = $"{result.FileName}  ({size / 1024.0 / 1024.0:F1} MB)";
                if (isMetadata)
                {
                    metadataPath = dest;
                    metadataLabel.Text = text;
                }
                else
                {
                    il2cppPath = dest;
                    il2cppLabel.Text = text;
                }
                statusLabel.Text = "";
            }
            catch (Exception e)
            {
                statusLabel.Text = "Could not open file: " + e.Message;
            }
        }

        private async Task RunDumpAsync()
        {
            if (running) return;
            if (il2cppPath == null || metadataPath == null)
            {
                statusLabel.Text = "Pick both libil2cpp.so and global-metadata.dat first.";
                return;
            }

            var config = new Config
            {
                DumpMethod = dumpMethod.IsToggled,
                DumpField = dumpField.IsToggled,
                DumpProperty = dumpProperty.IsToggled,
                DumpAttribute = dumpAttribute.IsToggled,
                DumpFieldOffset = dumpFieldOffset.IsToggled,
                DumpMethodOffset = dumpMethodOffset.IsToggled,
                DumpTypeDefIndex = dumpTypeDefIndex.IsToggled,
                GenerateDummyDll = generateDummyDll.IsToggled,
                GenerateStruct = generateStruct.IsToggled,
                DummyDllAddToken = dummyDllAddToken.IsToggled,
                RequireAnyKey = false,
                ForceIl2CppVersion = forceIl2CppVersion.IsToggled,
                ForceDump = forceDump.IsToggled,
                NoRedirectedPointer = noRedirectedPointer.IsToggled,
            };
            if (config.ForceIl2CppVersion)
            {
                if (!double.TryParse(forceVersionEntry.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                {
                    statusLabel.Text = "Invalid forced il2cpp version.";
                    return;
                }
                config.ForceVersion = v;
            }

            if (!TryParseHex(dumpAddressEntry.Text, out var dumpAddr) ||
                !TryParseHex(codeRegEntry.Text, out var codeReg) ||
                !TryParseHex(metaRegEntry.Text, out var metaReg))
            {
                statusLabel.Text = "Advanced fields must be hex numbers (e.g. 0x7A1B2C00) or empty.";
                return;
            }

            var outputRoot = global::Android.App.Application.Context.GetExternalFilesDir(null)?.AbsolutePath
                             ?? FileSystem.AppDataDirectory;
            var outputDir = Path.Combine(outputRoot, "dumps", "dump_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));

            var options = new DumpOptions
            {
                Il2CppPath = il2cppPath,
                MetadataPath = metadataPath,
                OutputDir = outputDir,
                DumpAddress = dumpAddr,
                CodeRegistration = codeReg,
                MetadataRegistration = metaReg,
            };

            running = true;
            dumpButton.IsEnabled = false;
            shareButton.IsEnabled = false;
            spinner.IsVisible = spinner.IsRunning = true;
            statusLabel.Text = "Working... large games can take a few minutes.";

            logWriter = new LogWriter();
            var originalOut = Console.Out;
            Console.SetOut(logWriter);
            var lastLength = -1;
            Dispatcher.StartTimer(TimeSpan.FromMilliseconds(400), () =>
            {
                var writer = logWriter;
                if (writer != null && writer.Length != lastLength)
                {
                    lastLength = writer.Length;
                    logView.Text = writer.Snapshot();
                }
                return running;
            });

            bool ok = false;
            try
            {
                ok = await Task.Run(() => DumperRunner.Run(config, options));
            }
            catch (OutOfMemoryException)
            {
                Console.WriteLine("ERROR: Out of memory. This file is too large for this device.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                Console.SetOut(originalOut);
                running = false;
                logView.Text = logWriter.Snapshot();
                spinner.IsVisible = spinner.IsRunning = false;
                dumpButton.IsEnabled = true;
            }

            if (ok)
            {
                lastOutputDir = outputDir;
                shareButton.IsEnabled = true;
                statusLabel.Text = "Done. Output saved to:\n" + outputDir;
            }
            else
            {
                statusLabel.Text = "Dump failed – see the log below.";
            }
        }

        private async Task ShareOutputAsync()
        {
            if (lastOutputDir == null || !Directory.Exists(lastOutputDir)) return;
            try
            {
                shareButton.IsEnabled = false;
                statusLabel.Text = "Zipping output...";
                var shareDir = Path.Combine(FileSystem.CacheDirectory, "share");
                if (Directory.Exists(shareDir)) Directory.Delete(shareDir, true);
                Directory.CreateDirectory(shareDir);
                var zipPath = Path.Combine(shareDir, Path.GetFileName(lastOutputDir) + ".zip");
                await Task.Run(() => ZipFile.CreateFromDirectory(lastOutputDir, zipPath));

                statusLabel.Text = "Output saved to:\n" + lastOutputDir;
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Il2CppDumper output",
                    File = new ShareFile(zipPath, "application/zip"),
                });
            }
            catch (Exception e)
            {
                statusLabel.Text = "Share failed: " + e.Message;
            }
            finally
            {
                shareButton.IsEnabled = true;
            }
        }

        private static bool TryParseHex(string text, out ulong? value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(text)) return true;
            var s = text.Trim();
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
            if (ulong.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v))
            {
                value = v;
                return true;
            }
            return false;
        }

        /// <summary>Thread-safe Console sink so the dumper's Console.WriteLine output shows up in the app.</summary>
        private sealed class LogWriter : TextWriter
        {
            private const int MaxChars = 200_000;
            private readonly StringBuilder sb = new();
            private readonly object gate = new();

            public override Encoding Encoding => Encoding.UTF8;

            private int version;

            /// <summary>Changes on every write (even after trimming), so the UI knows when to refresh.</summary>
            public int Length { get { lock (gate) return version; } }

            public override void Write(char value)
            {
                lock (gate) { sb.Append(value); Trim(); }
            }

            public override void Write(string value)
            {
                lock (gate) { sb.Append(value); Trim(); }
            }

            public override void WriteLine(string value)
            {
                lock (gate) { sb.Append(value).Append('\n'); Trim(); }
            }

            public string Snapshot()
            {
                lock (gate) return sb.ToString();
            }

            private void Trim()
            {
                version++;
                if (sb.Length > MaxChars) sb.Remove(0, sb.Length - MaxChars);
            }
        }
    }
}
