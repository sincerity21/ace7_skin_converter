# ace7_skin_converter

A fast, lightweight .NET CLI tool and core library for converting Ace Combat 7 aircraft skin mods between any aircraft skin slots (e.g. Slot 1–8).

## Features
- **Direct PAK & Folder Support**: Unpacks, transforms, and repacks skin mod `.pak` files automatically using `UnrealPak.exe` or converts raw loose directories.
- **Full UE4.18 Asset Conversion**: Serializes `.uasset` / `.uexp` binaries to JSON in-memory using [UAssetAPI](https://github.com/atenfyr/UAssetAPI), rewrites `NameMap`, `ObjectName`, `Imports`, and material `ParameterValue` texture references, and writes valid UE4 assets back to disk.
- **Decal & Material Relinking**: Automatically links `_Decal_Inst` parent references to the new `_Inst` and updates custom MREC/diffuse textures.
- **Bulk Data (`.ubulk`) Preservation**: Accurately moves and rebinds `.ubulk` streaming textures.

## Project Structure
```
ace7_skin_converter/
├── SkinConverterCore/     # .NET 8.0 core library & CLI entry point
│   ├── Models/            # Data models for detected skins & conversion requests
│   ├── Services/          # Detection, transformation engine & UnrealPak runner
│   └── Program.cs         # CLI interface & test runner
└── UAssetAPI/             # Submodule for Unreal Engine asset serialization
```

## Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- `UnrealPak.exe` (only required when packing/unpacking `.pak` archives)

## Cloning & Setup
```bash
git clone --recurse-submodules https://github.com/<your-username>/ace7_skin_converter.git
cd ace7_skin_converter
dotnet build SkinConverterCore
```

## Usage

### Convert a `.pak` mod to another slot
```powershell
dotnet run --project SkinConverterCore -- "path\to\MySkin_P.pak" <target_slot_number> "path\to\Output_Slot01_P.pak" "path\to\UnrealPak.exe"
```

### Convert an unpacked folder
```powershell
dotnet run --project SkinConverterCore -- "path\to\UnpackedSkinFolder" <target_slot_number> "path\to\OutputFolder"
```
