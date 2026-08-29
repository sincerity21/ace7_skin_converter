namespace SkinConverterCore.Models;

public class SkinConversionRequest
{
    public string SourcePath { get; set; } = string.Empty; // .pak file or unpacked root folder
    public int TargetSlotNumber { get; set; } = 1; // 1-based (e.g. 1 -> index "00", 7 -> index "06")
    public string? OutputPath { get; set; }
    public string? UnrealPakExe { get; set; }

    public string TargetSlotIndex => (TargetSlotNumber - 1).ToString("D2");
}
