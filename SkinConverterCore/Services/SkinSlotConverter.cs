using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SkinConverterCore.Models;

namespace SkinConverterCore.Services;

public static class SkinSlotConverter
{
    public static void ConvertDirectory(string sourceDir, string targetDir, int targetSlotNumber, Action<string>? log = null)
    {
        var detected = SkinSlotDetector.Detect(sourceDir, log);
        var targetSlotIdx = (targetSlotNumber - 1).ToString("D2");

        log?.Invoke($"Converting {detected.PlaneStringId} from slot {detected.SourceSlotNumber} ({detected.SourceSlotIndex}) to slot {targetSlotNumber} ({targetSlotIdx})");

        if (Directory.Exists(targetDir))
            Directory.Delete(targetDir, recursive: true);
        Directory.CreateDirectory(targetDir);

        // 1. Copy any external mod files (e.g. shared textures, custom cooked dirs)
        foreach (var extFile in detected.ExternalModFiles)
        {
            var relPath = Path.GetRelativePath(sourceDir, extFile);
            var destPath = Path.Combine(targetDir, relPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            File.Copy(extFile, destPath, overwrite: true);
        }

        // 2. Build target slot folder path
        var relSlotParent = Path.GetDirectoryName(detected.RelativeSlotPath) ?? "";
        var targetRelativeSlotPath = Path.Combine(relSlotParent, targetSlotIdx).Replace('\\', '/');
        var targetAbsoluteSlotPath = Path.Combine(targetDir, targetRelativeSlotPath);
        Directory.CreateDirectory(targetAbsoluteSlotPath);

        var allSlotFiles = detected.MaterialInstanceFiles
            .Concat(detected.DecalInstanceFiles)
            .Concat(detected.TextureFiles)
            .Concat(detected.OtherSlotFiles)
            .Distinct()
            .ToList();

        foreach (var filePath in allSlotFiles)
        {
            var fileName = Path.GetFileName(filePath);
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

            var newBaseName = TransformFileName(nameWithoutExt, detected.PlaneStringId, detected.SourceSlotIndex, targetSlotIdx);
            var outPath = Path.Combine(targetAbsoluteSlotPath, newBaseName + Path.GetExtension(fileName));

            if (ext == ".json")
            {
                var content = File.ReadAllText(filePath);
                var transformed = TransformJsonAsset(content, detected.PlaneStringId, detected.SourceSlotIndex, targetSlotIdx, newBaseName, log);
                File.WriteAllText(outPath, transformed);
                log?.Invoke($"Converted JSON: {fileName} -> {Path.GetFileName(outPath)}");
            }
            else if (ext == ".uasset")
            {
                var json = AssetSerializationService.UAssetToJson(filePath, log);
                var transformed = TransformJsonAsset(json, detected.PlaneStringId, detected.SourceSlotIndex, targetSlotIdx, newBaseName, log);
                AssetSerializationService.JsonToUAsset(transformed, outPath, log);
                log?.Invoke($"Converted UAsset: {fileName} -> {Path.GetFileName(outPath)}");
            }
            else if (ext == ".uexp")
            {
                // .uexp is created automatically alongside .uasset during deserialization/serialization
            }
            else
            {
                File.Copy(filePath, outPath, overwrite: true);
                log?.Invoke($"Copied asset: {fileName} -> {Path.GetFileName(outPath)}");
            }
        }

        log?.Invoke($"Conversion completed successfully to: {targetDir}");
    }

    public static void ConvertPak(string unrealPakExe, string sourcePakPath, string outputPakPath, int targetSlotNumber, Action<string>? log = null)
    {
        var tempExtract = Path.Combine(Path.GetTempPath(), $"SkinConv_extract_{Guid.NewGuid():N}");
        var tempStaged = Path.Combine(Path.GetTempPath(), $"SkinConv_staged_{Guid.NewGuid():N}");

        try
        {
            PakService.ExtractPak(unrealPakExe, sourcePakPath, tempExtract, log);
            ConvertDirectory(tempExtract, tempStaged, targetSlotNumber, log);
            PakService.CreatePak(unrealPakExe, tempStaged, outputPakPath, log);
            log?.Invoke($"Packed output PAK at: {outputPakPath}");
        }
        finally
        {
            try { if (Directory.Exists(tempExtract)) Directory.Delete(tempExtract, true); } catch { }
            try { if (Directory.Exists(tempStaged)) Directory.Delete(tempStaged, true); } catch { }
        }
    }

    public static string TransformFileName(string nameWithoutExt, string plane, string srcSlot, string dstSlot)
    {
        // 1. Decal Instance
        if (nameWithoutExt.EndsWith("_Decal_Inst", StringComparison.OrdinalIgnoreCase))
            return $"{plane}_{dstSlot}_Decal_Inst";

        // 2. Base Material Instance
        if (nameWithoutExt.EndsWith("_Inst", StringComparison.OrdinalIgnoreCase))
            return $"{plane}_{dstSlot}_Inst";

        // 3. Diffuse texture
        if (nameWithoutExt.EndsWith("_D", StringComparison.OrdinalIgnoreCase))
            return $"{plane}_{dstSlot}_D";

        // 4. MREC texture (handles _MREC, x2_MREC, 02xMREC, etc.)
        if (nameWithoutExt.EndsWith("_MREC", StringComparison.OrdinalIgnoreCase) ||
            nameWithoutExt.EndsWith("xMREC", StringComparison.OrdinalIgnoreCase))
            return $"{plane}_{dstSlot}_MREC";

        // 5. Normal texture
        if (nameWithoutExt.EndsWith("_N", StringComparison.OrdinalIgnoreCase))
            return $"{plane}_{dstSlot}_N";

        // Fallback: replace any plane_srcSlot with plane_dstSlot
        return nameWithoutExt.Replace($"{plane}_{srcSlot}", $"{plane}_{dstSlot}", StringComparison.OrdinalIgnoreCase);
    }

    public static string TransformJsonAsset(string jsonText, string plane, string srcSlot, string dstSlot, string newObjectName, Action<string>? log = null)
    {
        var root = JObject.Parse(jsonText);

        // Update all string values across the entire JSON tree
        foreach (var token in root.DescendantsAndSelf().OfType<JValue>())
        {
            if (token.Type == JTokenType.String && token.Value is string strVal)
            {
                var updated = TransformSymbolString(strVal, plane, srcSlot, dstSlot);
                if (updated != strVal)
                {
                    token.Value = updated;
                }
            }
        }

        // Explicitly ensure Exports[0].ObjectName is set to the new asset base name
        if (root["Exports"] is JArray exports && exports.Count > 0 && exports[0] is JObject firstExport)
        {
            firstExport["ObjectName"] = newObjectName;
        }

        return root.ToString();
    }

    public static string TransformSymbolString(string val, string plane, string srcSlot, string dstSlot)
    {
        if (string.IsNullOrEmpty(val))
            return val;

        // 1. Path replacement: .../Vehicles/Aircraft/{plane}/{srcSlot}/ -> .../Vehicles/Aircraft/{plane}/{dstSlot}/
        // Note: Do not replace /00/ if it's the base normal map _00_N (unless target is slot 06 with slot-specific normal)
        if (!val.Contains($"{plane}_00_N", StringComparison.OrdinalIgnoreCase))
        {
            val = Regex.Replace(val,
                $@"(Aircraft[\\/]{Regex.Escape(plane)}[\\/]){srcSlot}([\\/]|$)",
                $"${{1}}{dstSlot}$2",
                RegexOptions.IgnoreCase);
        }

        // 2. Custom trigger path: /Trigger/{srcSlot}/ -> /Trigger/{dstSlot}/
        val = Regex.Replace(val,
            $@"([\\/]Trigger[\\/]){srcSlot}([\\/]|$)",
            $"${{1}}{dstSlot}$2",
            RegexOptions.IgnoreCase);

        // 3. /Game/{plane}_{srcSlot}_... -> /Game/{plane}_{dstSlot}_...
        // Note: Skip if it's base normal _00_N
        if (!val.EndsWith($"{plane}_00_N", StringComparison.OrdinalIgnoreCase))
        {
            val = Regex.Replace(val,
                $@"([\\/]Game[\\/]){Regex.Escape(plane)}_{srcSlot}_",
                $"${{1}}{plane}_{dstSlot}_",
                RegexOptions.IgnoreCase);
        }

        // 4. Custom MREC patterns (e.g. /Game/f16c_x2_MREC -> /Game/f16c_00_MREC, /Game/Trigger/02/mr2k_02xMREC -> .../06_MREC)
        val = Regex.Replace(val,
            $@"([\\/]Game[\\/]){Regex.Escape(plane)}_x\d+_MREC\b",
            $"${{1}}{plane}_{dstSlot}_MREC",
            RegexOptions.IgnoreCase);

        val = Regex.Replace(val,
            $@"{Regex.Escape(plane)}_x\d+_MREC\b",
            $"{plane}_{dstSlot}_MREC",
            RegexOptions.IgnoreCase);

        val = Regex.Replace(val,
            $@"{Regex.Escape(plane)}_{srcSlot}xMREC\b",
            $"{plane}_{dstSlot}_MREC",
            RegexOptions.IgnoreCase);

        // 5. Decal Instance: {plane}_{srcSlot}_Decal_Inst -> {plane}_{dstSlot}_Decal_Inst
        val = Regex.Replace(val,
            $@"{Regex.Escape(plane)}_{srcSlot}_Decal_Inst\b",
            $"{plane}_{dstSlot}_Decal_Inst",
            RegexOptions.IgnoreCase);

        // 6. Base Material Instance: {plane}_{srcSlot}_Inst -> {plane}_{dstSlot}_Inst
        val = Regex.Replace(val,
            $@"{Regex.Escape(plane)}_{srcSlot}_Inst\b",
            $"{plane}_{dstSlot}_Inst",
            RegexOptions.IgnoreCase);

        // 7. Diffuse Texture: {plane}_{srcSlot}_D -> {plane}_{dstSlot}_D
        val = Regex.Replace(val,
            $@"{Regex.Escape(plane)}_{srcSlot}_D\b",
            $"{plane}_{dstSlot}_D",
            RegexOptions.IgnoreCase);

        // 8. MREC Texture: {plane}_{srcSlot}_MREC -> {plane}_{dstSlot}_MREC
        val = Regex.Replace(val,
            $@"{Regex.Escape(plane)}_{srcSlot}_MREC\b",
            $"{plane}_{dstSlot}_MREC",
            RegexOptions.IgnoreCase);

        // 9. Normal Map handling
        if (dstSlot == "06" && (plane.Equals("mr2k", StringComparison.OrdinalIgnoreCase)))
        {
            // Slot 07 (06) on mr2k uses mr2k_06_N
            val = val.Replace($"/Vehicles/Aircraft/{plane}/00/{plane}_00_N", $"/Vehicles/Aircraft/{plane}/06/{plane}_06_N", StringComparison.OrdinalIgnoreCase);
            val = val.Replace($"/Vehicles/Aircraft/{plane}/{srcSlot}/{plane}_{srcSlot}_N", $"/Vehicles/Aircraft/{plane}/06/{plane}_06_N", StringComparison.OrdinalIgnoreCase);
            if (val.Equals($"{plane}_00_N", StringComparison.OrdinalIgnoreCase) || val.Equals($"{plane}_{srcSlot}_N", StringComparison.OrdinalIgnoreCase))
            {
                val = $"{plane}_06_N";
            }
        }
        else if (srcSlot != "00")
        {
            // Slot-specific non-base normal: {plane}_{srcSlot}_N -> {plane}_{dstSlot}_N
            val = Regex.Replace(val,
                $@"{Regex.Escape(plane)}_{srcSlot}_N\b",
                $"{plane}_{dstSlot}_N",
                RegexOptions.IgnoreCase);
        }

        return val;
    }
}
