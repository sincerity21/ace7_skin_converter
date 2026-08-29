namespace SkinConverterCore.Models;

public class DetectedSkinInfo
{
    public string PlaneStringId { get; set; } = string.Empty;
    public int SourceSlotNumber { get; set; }
    public string SourceSlotIndex { get; set; } = string.Empty; // e.g. "02"
    public string RelativeSlotPath { get; set; } = string.Empty; // e.g. "Nimbus/Content/Vehicles/Aircraft/f16c/02"
    public string AbsoluteSlotDiskPath { get; set; } = string.Empty;

    public List<string> MaterialInstanceFiles { get; set; } = new();
    public List<string> DecalInstanceFiles { get; set; } = new();
    public List<string> TextureFiles { get; set; } = new();
    public List<string> OtherSlotFiles { get; set; } = new();

    /// <summary>
    /// Custom textures or subfolders outside the main slot folder (e.g. fa44/PE/, mr2k/Vieux/, Common/LZ/Skin/...)
    /// </summary>
    public List<string> ExternalModFiles { get; set; } = new();

    public override string ToString()
    {
        return $"Plane: {PlaneStringId}, Slot: {SourceSlotNumber} (index {SourceSlotIndex}), Files: {MaterialInstanceFiles.Count + DecalInstanceFiles.Count + TextureFiles.Count + OtherSlotFiles.Count + ExternalModFiles.Count}";
    }
}
