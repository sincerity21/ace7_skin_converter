using System;
using System.IO;
using UAssetAPI;
using UAssetAPI.UnrealTypes;

namespace SkinConverterCore.Services;

public static class AssetSerializationService
{
    public const EngineVersion TargetEngineVersion = EngineVersion.VER_UE4_18;

    public static string UAssetToJson(string uassetPath, Action<string>? log = null)
    {
        var asset = new UAsset(uassetPath, TargetEngineVersion);
        return asset.SerializeJson(true);
    }

    public static void JsonToUAsset(string jsonContent, string outputUassetPath, Action<string>? log = null)
    {
        var outDir = Path.GetDirectoryName(outputUassetPath);
        if (!string.IsNullOrEmpty(outDir))
            Directory.CreateDirectory(outDir);

        var asset = UAsset.DeserializeJson(jsonContent);
        asset.Write(outputUassetPath);
    }
}
