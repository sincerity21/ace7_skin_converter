using System;
using System.IO;
using SkinConverterCore.Models;
using SkinConverterCore.Services;

namespace SkinConverterCore;

internal class Program
{
    static int Main(string[] args)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine("  Ace Combat 7 Skin Slot Converter Core (CLI)");
        Console.WriteLine("==================================================");

        if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
        {
            PrintUsage();
            return 0;
        }

        if (args[0] == "--test-all")
        {
            return RunAllDatasetTests();
        }

        try
        {
            var sourcePath = args[0].Trim('"');
            if (!int.TryParse(args[1], out var targetSlot) || targetSlot < 1)
            {
                Console.Error.WriteLine("Error: Target slot number must be a positive integer (e.g. 1, 2, 3... 8).");
                return 1;
            }

            var outputPath = args.Length > 2 ? args[2].Trim('"') : null;
            var unrealPakExe = args.Length > 3 ? args[3].Trim('"') : null;

            if (File.Exists(sourcePath) && sourcePath.EndsWith(".pak", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(unrealPakExe) || !File.Exists(unrealPakExe))
                {
                    Console.Error.WriteLine("Error: UnrealPak.exe path is required when converting a .pak file.");
                    return 1;
                }

                if (string.IsNullOrEmpty(outputPath))
                {
                    var baseName = Path.GetFileNameWithoutExtension(sourcePath);
                    if (baseName.EndsWith("_P", StringComparison.OrdinalIgnoreCase))
                        baseName = baseName[..^2];

                    outputPath = Path.Combine(Path.GetDirectoryName(sourcePath) ?? ".", $"{baseName}_Slot{targetSlot:D2}_P.pak");
                }
                else
                {
                    // Ensure the custom output path ends with _P.pak for proper UE4 patch precedence
                    if (outputPath.EndsWith(".pak", StringComparison.OrdinalIgnoreCase) && !outputPath.EndsWith("_P.pak", StringComparison.OrdinalIgnoreCase))
                    {
                        var dir = Path.GetDirectoryName(outputPath) ?? ".";
                        var name = Path.GetFileNameWithoutExtension(outputPath);
                        if (name.EndsWith("_P", StringComparison.OrdinalIgnoreCase))
                            name = name[..^2];
                        outputPath = Path.Combine(dir, $"{name}_P.pak");
                    }
                }

                Console.WriteLine($"Converting PAK: {sourcePath} -> Target Slot {targetSlot}");
                SkinSlotConverter.ConvertPak(unrealPakExe, sourcePath, outputPath, targetSlot, Console.WriteLine);
            }
            else if (Directory.Exists(sourcePath))
            {
                if (string.IsNullOrEmpty(outputPath))
                {
                    outputPath = sourcePath.TrimEnd('\\', '/') + $"_Slot{targetSlot:D2}_Converted";
                }

                Console.WriteLine($"Converting Directory: {sourcePath} -> Target Slot {targetSlot}");
                SkinSlotConverter.ConvertDirectory(sourcePath, outputPath, targetSlot, Console.WriteLine);
            }
            else
            {
                Console.Error.WriteLine($"Error: Source path not found: {sourcePath}");
                return 1;
            }

            Console.WriteLine("[Success] Conversion completed successfully!");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Error] {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  SkinConverterCore <source_pak_or_dir> <target_slot_number> [output_path] [unrealpak_exe]");
        Console.WriteLine("  SkinConverterCore --test-all");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  SkinConverterCore \"D:\\mods\\MySkin_P.pak\" 1 \"D:\\mods\\MySkin_Slot01_P.pak\" \"C:\\tools\\UnrealPak.exe\"");
        Console.WriteLine("  SkinConverterCore \"D:\\mods\\F-16C_Skin3_P\" 1 \"D:\\mods\\F-16C_Skin1_P\"");
    }

    public static int RunAllDatasetTests()
    {
        Console.WriteLine("\nRunning verification test suite against all 6 dataset pairs...\n");

        var baseDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
        Console.WriteLine($"Workspace root: {baseDir}");

        var testCases = new (string sourceFolder, int targetSlot, string expectedFolder)[]
        {
            ("CFA-44_Skin6_Elysia_P", 1, "CFA-44_Skin1_Elysia_test_P"),
            ("F-16C_Skin3_Serdyukov_Blank_P", 1, "F-16C_Skin1_Serdyukov_Blank_test_P"),
            ("F-35C -RCAF Pine- Slot 03_P", 1, "F-35C -RCAF Pine- Slot 01_Test_P"),
            ("Mir2000-5_Glaucus_Blank(Slot03)_P", 7, "Mir2000-5_Glaucus_Blank(Slot07)_Test_P"),
            ("~~~~~~~ASF-X_Skin1_RAF_P", 3, "~~~~~~~ASF-X_Skin3_RAF_Test_P"),
            ("~~~~~~~Mirage 2000-5_Skin6_Vieux_P", 8, "~~~~~~~Mirage 2000-5_Skin8_Test_Vieux_P"),
        };

        int passed = 0;
        int failed = 0;

        foreach (var (src, targetSlot, expected) in testCases)
        {
            var srcPath = Path.Combine(baseDir, src);
            var expectedPath = Path.Combine(baseDir, expected);
            var actualPath = Path.Combine(Path.GetTempPath(), $"SkinConv_Test_{Guid.NewGuid():N}");

            Console.WriteLine($"--------------------------------------------------");
            Console.WriteLine($"Test Case: {src} -> Slot {targetSlot}");
            Console.WriteLine($"Expected:  {expected}");

            if (!Directory.Exists(srcPath) || !Directory.Exists(expectedPath))
            {
                Console.WriteLine($"[SKIPPED] Source or Expected folder not found on disk.");
                continue;
            }

            try
            {
                SkinSlotConverter.ConvertDirectory(srcPath, actualPath, targetSlot, Console.WriteLine);

                // Compare files in actual vs expected
                var expectedFiles = Directory.GetFiles(expectedPath, "*.json", SearchOption.AllDirectories)
                    .Select(f => Path.GetRelativePath(expectedPath, f))
                    .OrderBy(f => f)
                    .ToList();

                var actualFiles = Directory.GetFiles(actualPath, "*.json", SearchOption.AllDirectories)
                    .Select(f => Path.GetRelativePath(actualPath, f))
                    .OrderBy(f => f)
                    .ToList();

                bool mismatch = false;
                if (!expectedFiles.SequenceEqual(actualFiles, StringComparer.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[FAIL] Output file list mismatch!");
                    Console.WriteLine($" Expected: {string.Join(", ", expectedFiles)}");
                    Console.WriteLine($" Actual:   {string.Join(", ", actualFiles)}");
                    mismatch = true;
                }
                else
                {
                    foreach (var rel in expectedFiles)
                    {
                        var expJson = File.ReadAllText(Path.Combine(expectedPath, rel));
                        var actJson = File.ReadAllText(Path.Combine(actualPath, rel));

                        var expObj = Newtonsoft.Json.Linq.JObject.Parse(expJson);
                        var actObj = Newtonsoft.Json.Linq.JObject.Parse(actJson);

                        // Compare NameMap
                        var expNameMap = expObj["NameMap"]?.ToObject<List<string>>() ?? new();
                        var actNameMap = actObj["NameMap"]?.ToObject<List<string>>() ?? new();

                        if (!expNameMap.SequenceEqual(actNameMap))
                        {
                            Console.WriteLine($"[FAIL] NameMap difference in {rel}!");
                            Console.WriteLine($" Expected: {string.Join(", ", expNameMap.Take(10))}...");
                            Console.WriteLine($" Actual:   {string.Join(", ", actNameMap.Take(10))}...");
                            mismatch = true;
                            break;
                        }

                        // Compare ObjectName
                        var expObjName = expObj["Exports"]?[0]?["ObjectName"]?.ToString();
                        var actObjName = actObj["Exports"]?[0]?["ObjectName"]?.ToString();

                        if (expObjName != actObjName)
                        {
                            Console.WriteLine($"[FAIL] ObjectName difference in {rel}! Expected: {expObjName}, Actual: {actObjName}");
                            mismatch = true;
                            break;
                        }
                    }
                }

                if (!mismatch)
                {
                    Console.WriteLine($"[PASS] {src} successfully converted and verified against {expected}!");
                    passed++;
                }
                else
                {
                    failed++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FAIL] Exception during test: {ex.Message}");
                failed++;
            }
            finally
            {
                try { if (Directory.Exists(actualPath)) Directory.Delete(actualPath, true); } catch { }
            }
        }

        Console.WriteLine("\n==================================================");
        Console.WriteLine($"Test Results: {passed} PASSED, {failed} FAILED");
        Console.WriteLine("==================================================");

        return failed == 0 ? 0 : 1;
    }
}
