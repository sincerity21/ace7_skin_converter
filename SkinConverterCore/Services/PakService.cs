using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SkinConverterCore.Services;

public static class PakService
{
    public static void ExtractPak(string unrealPakExe, string pakPath, string extractDir, Action<string>? log = null)
    {
        Directory.CreateDirectory(extractDir);

        var args = $"\"{pakPath}\" -extract \"{extractDir}\"";
        log?.Invoke($"Running UnrealPak extract: {unrealPakExe} {args}");

        var psi = new ProcessStartInfo
        {
            FileName = unrealPakExe,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start UnrealPak process.");
        var output = proc.StandardOutput.ReadToEnd();
        var error = proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        if (!string.IsNullOrWhiteSpace(output))
            log?.Invoke(output.Trim());
        if (!string.IsNullOrWhiteSpace(error))
            log?.Invoke(error.Trim());

        log?.Invoke($"UnrealPak exited with code {proc.ExitCode}");

        if (proc.ExitCode != 0)
        {
            throw new InvalidOperationException($"UnrealPak extract failed with exit code {proc.ExitCode}.");
        }
    }

    public static void CreatePak(string unrealPakExe, string stagedRootDir, string outputPakPath, Action<string>? log = null)
    {
        var unrealPakDir = Path.GetDirectoryName(unrealPakExe) ?? string.Empty;
        if (string.IsNullOrEmpty(unrealPakDir))
            throw new InvalidOperationException("Could not determine UnrealPak.exe directory.");

        var fileListFileName = $"filelist_skin_conv_{Guid.NewGuid():N}.txt";
        var fileListPath = Path.Combine(unrealPakDir, fileListFileName);

        var allFiles = Directory.GetFiles(stagedRootDir, "*.*", SearchOption.AllDirectories);
        if (allFiles.Length == 0)
            throw new InvalidOperationException($"No files found to pack in {stagedRootDir}");

        var sb = new StringBuilder();
        foreach (var fullPath in allFiles)
        {
            // Do not pack loose .json files or temp files into game pak
            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            if (ext == ".json" || ext == ".tmp" || ext == ".bak" || ext == ".txt")
                continue;

            var rel = Path.GetRelativePath(stagedRootDir, fullPath).Replace('\\', '/');
            // Internal mount path in AC7 is ../../../Nimbus/Content/...
            var mountPath = rel.StartsWith("Nimbus/", StringComparison.OrdinalIgnoreCase)
                ? $"../../../{rel}"
                : $"../../../Nimbus/Content/{rel}";

            sb.AppendLine($"\"{fullPath}\" \"{mountPath}\"");
        }

        File.WriteAllText(fileListPath, sb.ToString(), Encoding.UTF8);
        log?.Invoke($"Generated UnrealPak filelist with {allFiles.Length} entries at {fileListPath}");

        var outDir = Path.GetDirectoryName(outputPakPath);
        if (!string.IsNullOrEmpty(outDir))
            Directory.CreateDirectory(outDir);

        var args = $"\"{outputPakPath}\" -create={fileListFileName} -compress";
        log?.Invoke($"Running UnrealPak create: {unrealPakExe} {args}");

        var psi = new ProcessStartInfo
        {
            FileName = unrealPakExe,
            Arguments = args,
            WorkingDirectory = unrealPakDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start UnrealPak process.");
            var output = proc.StandardOutput.ReadToEnd();
            var error = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            if (!string.IsNullOrWhiteSpace(output))
                log?.Invoke(output.Trim());
            if (!string.IsNullOrWhiteSpace(error))
                log?.Invoke(error.Trim());

            log?.Invoke($"UnrealPak exited with code {proc.ExitCode}");

            if (proc.ExitCode != 0)
                throw new InvalidOperationException($"UnrealPak create failed with exit code {proc.ExitCode}.");
        }
        finally
        {
            if (File.Exists(fileListPath))
            {
                try { File.Delete(fileListPath); } catch { }
            }
        }
    }
}
