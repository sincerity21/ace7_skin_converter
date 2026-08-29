# ace7_skin_converter

A fast, lightweight .NET CLI tool and core library for converting Ace Combat 7 aircraft skin mods between any aircraft skin slots (e.g. Slot 1–8).

## Features
- **Direct PAK & Folder Support**: Unpacks, transforms, and repacks skin mod `.pak` files automatically using `UnrealPak.exe` or converts raw loose directories.
- **Full UE4.18 Asset Conversion**: Serializes `.uasset` / `.uexp` binaries to JSON in-memory using [UAssetAPI](https://github.com/atenfyr/UAssetAPI), rewrites `NameMap`, `ObjectName`, `Imports`, and material `ParameterValue` texture references, and writes valid UE4 assets back to disk.
- **Decal & Material Relinking**: Automatically links `_Decal_Inst` parent references to the new `_Inst` and updates custom MREC/diffuse textures.
- **Bulk Data (`.ubulk`) Preservation**: Accurately moves and rebinds `.ubulk` streaming textures.
- **Auto Patch Suffix**: Ensures repacked game mods always end with `_P.pak` for proper UE4 mod load order precedence.

---

## Skin Slot Mapping

The tool uses standard 1-based in-game slot numbers. The internal two-digit index (`00`–`07`) is handled automatically:

| Target In-Game Skin | What to Type in CLI | Internal Folder & Asset Index |
|:---|:---:|:---:|
| **Skin 01** (Osea / Default) | `1` | `00` |
| **Skin 02** (Erusea) | `2` | `01` |
| **Skin 03** (Special) | `3` | `02` |
| **Skin 04** (Mage / Trigger) | `4` | `03` |
| **Skin 05** (Spare / Trigger) | `5` | `04` |
| **Skin 06** (Strider / Trigger) | `6` | `05` |
| **Skin 07** (Special Skin / DLC 1) | `7` | `06` |
| **Skin 08** (Special Skin / DLC 2) | `8` | `07` |

---

## Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- `UnrealPak.exe` (only needed when extracting/packing `.pak` archives)

## Cloning & Setup
```bash
git clone --recurse-submodules https://github.com/<your-username>/ace7_skin_converter.git
cd ace7_skin_converter
dotnet build SkinConverterCore
```

---

## Usage

### General Syntax
```powershell
dotnet run --project SkinConverterCore -- "<source_pak_or_dir>" <target_slot_number> [output_path] [unrealpak_exe]
```

### 1. Converting a `.pak` Mod
To convert a `.pak` mod to **Skin 01** (type `1`):
```powershell
dotnet run --project SkinConverterCore -- "D:\Mods\F-16C_Skin3_Serdyukov_Blank_P.pak" 1 "D:\Mods\F-16C_Skin1_Serdyukov_Blank_P.pak" "D:\Tools\UnrealPak.exe"
```

### 2. Converting an Unpacked Folder
To convert an unpacked mod directory to **Skin 01** (type `1`):
```powershell
dotnet run --project SkinConverterCore -- "D:\Mods\CFA-44_Skin6_Elysia_P\" 1 "D:\Mods\CFA-44_Skin1_Elysia_P\"
```

---

## Verification Test Suite
To verify the transformation logic against all test dataset mod pairs:
```powershell
dotnet run --project SkinConverterCore -- --test-all
```
