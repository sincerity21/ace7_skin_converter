using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SkinConverterCore.Models;

namespace SkinConverterCore.Services;

public static class SkinSlotDetector
{
    private static readonly Regex AircraftSlotRegex = new(
        @"[\\/](?:Nimbus[\\/]Content[\\/])?Vehicles[\\/]Aircraft[\\/](?<plane>[^\\/]+)[\\/](?<slot>\d{2})(?:[\\/]|$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static DetectedSkinInfo Detect(string rootDir, Action<string>? log = null)
    {
        if (!Directory.Exists(rootDir))
            throw new DirectoryNotFoundException($"Directory not found: {rootDir}");

        var allFiles = Directory.GetFiles(rootDir, "*.*", SearchOption.AllDirectories);
        if (allFiles.Length == 0)
            throw new InvalidOperationException($"No files found in directory: {rootDir}");

        string? detectedPlane = null;
        string? detectedSlotIdx = null;
        string? detectedSlotPath = null;
        var slotFiles = new List<string>();
        var externalFiles = new List<string>();

        foreach (var file in allFiles)
        {
            var match = AircraftSlotRegex.Match(file);
            if (match.Success)
            {
                var plane = match.Groups["plane"].Value.ToLowerInvariant();
                var slot = match.Groups["slot"].Value;

                if (detectedPlane == null)
                {
                    detectedPlane = plane;
                    detectedSlotIdx = slot;
                    detectedSlotPath = Path.GetDirectoryName(file) ?? string.Empty;
                }

                // If file is inside the primary slot folder
                if (detectedSlotPath != null && file.StartsWith(detectedSlotPath, StringComparison.OrdinalIgnoreCase))
                {
                    slotFiles.Add(file);
                }
                else
                {
                    externalFiles.Add(file);
                }
            }
            else
            {
                externalFiles.Add(file);
            }
        }

        if (string.IsNullOrEmpty(detectedPlane) || string.IsNullOrEmpty(detectedSlotIdx) || string.IsNullOrEmpty(detectedSlotPath))
        {
            throw new InvalidOperationException($"Could not detect Ace Combat 7 aircraft skin slot structure in {rootDir}. Expected 'Vehicles/Aircraft/<plane>/<slot_number>/'.");
        }

        int slotNumber = int.Parse(detectedSlotIdx) + 1;

        var info = new DetectedSkinInfo
        {
            PlaneStringId = detectedPlane,
            SourceSlotNumber = slotNumber,
            SourceSlotIndex = detectedSlotIdx,
            AbsoluteSlotDiskPath = detectedSlotPath,
            RelativeSlotPath = Path.GetRelativePath(rootDir, detectedSlotPath).Replace('\\', '/'),
            ExternalModFiles = externalFiles
        };

        foreach (var file in slotFiles)
        {
            var fileName = Path.GetFileName(file);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

            if (nameWithoutExt.EndsWith("_Decal_Inst", StringComparison.OrdinalIgnoreCase))
            {
                info.DecalInstanceFiles.Add(file);
            }
            else if (nameWithoutExt.EndsWith("_Inst", StringComparison.OrdinalIgnoreCase))
            {
                info.MaterialInstanceFiles.Add(file);
            }
            else if (nameWithoutExt.EndsWith("_D", StringComparison.OrdinalIgnoreCase) ||
                     nameWithoutExt.EndsWith("_MREC", StringComparison.OrdinalIgnoreCase) ||
                     nameWithoutExt.EndsWith("xMREC", StringComparison.OrdinalIgnoreCase) ||
                     nameWithoutExt.EndsWith("_N", StringComparison.OrdinalIgnoreCase))
            {
                info.TextureFiles.Add(file);
            }
            else
            {
                info.OtherSlotFiles.Add(file);
            }
        }

        log?.Invoke($"Detected: {info}");
        return info;
    }
}
