# ace7_skin_converter

A fast, lightweight CLI tool and core library for converting Ace Combat 7 aircraft skin mods between any aircraft skin slots (e.g. Slot 1–8, or extended ASS slots).

This program was vibe-coded using Cursor Pro.

## Features
- **Zero-Dependency Standalone Executable**: Can be run directly as a single `.exe` without requiring .NET SDK or external runtime installations.
- **Direct PAK & Folder Support**: Unpacks, transforms, and repacks skin mod `.pak` files automatically using `UnrealPak.exe` or converts raw loose directories.
- **Full UE4.18 Asset Conversion**: Serializes `.uasset` / `.uexp` binaries to JSON in-memory using [UAssetAPI](https://github.com/atenfyr/UAssetAPI), rewrites `NameMap`, `ObjectName`, `Imports`, and material `ParameterValue` texture references, and writes valid UE4 assets back to disk.
- **Decal & Material Relinking**: Automatically links `_Decal_Inst` parent references to the new `_Inst` and updates custom MREC/diffuse textures.
- **Bulk Data (`.ubulk`) Preservation**: Accurately moves and rebinds `.ubulk` streaming textures.
- **Auto Patch Suffix**: Ensures repacked game mods always end with `_P.pak` for proper UE4 mod load order precedence.

---

## Skin Slot Mapping

The tool uses standard 1-based in-game slot numbers. The internal two-digit index is handled automatically:

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
| **Skin 09+** (Extended / Custom Slots) | `9`, `10`, etc... | `08`, `09`, etc... |

---

## Prerequisites
- `UnrealPak.exe` from [UnrealPak Enhanced](https://www.moddb.com/downloads/unrealpak-enhanced) (only needed when extracting/packing `.pak` archives)
- *(Optional)* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (only if building from source code)

---

## Usage

Open a terminal (PowerShell or Command Prompt) in the folder containing `SkinConverterCore.exe` (or the repository root), then run the commands below.

### Method 1: Using Standalone Executable (Recommended)

#### Syntax
```powershell
.\SkinConverterCore.exe "<source_pak_or_dir>" <target_slot_number> [output_path] [unrealpak_exe]
```

#### Examples

**1. Converting a `.pak` Mod to Skin 01 (type `1`):**
```powershell
.\SkinConverterCore.exe "D:\Mods\F-16C_Skin3_Serdyukov_Blank_P.pak" 1 "D:\Mods\F-16C_Skin1_Serdyukov_Blank_P.pak" "D:\Tools\UnrealPak.exe"
```

**2. Converting a `.pak` Mod to an Extended / ASS Slot (e.g. Skin 09):**
```powershell
.\SkinConverterCore.exe "D:\Mods\ADF-01_Cotton_Slot02_P.pak" 9 "D:\Mods\ADF-01_Cotton_Slot09_P.pak" "D:\Tools\UnrealPak.exe"
```

**3. Converting an Unpacked Folder:**
```powershell
.\SkinConverterCore.exe "D:\Mods\CFA-44_Skin6_Elysia_P\" 1 "D:\Mods\CFA-44_Skin1_Elysia_P\"
```

---

### Method 2: Running from Source (.NET SDK)

If you have cloned the repository and installed [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0):

#### 1. Cloning & Building
```bash
git clone --recurse-submodules https://github.com/<your-username>/ace7_skin_converter.git
cd ace7_skin_converter
dotnet build SkinConverterCore
```

#### 2. Running via `dotnet run`
```powershell
dotnet run --project SkinConverterCore -- "<source_pak_or_dir>" <target_slot_number> [output_path] [unrealpak_exe]
```

#### 3. Building Your Own Standalone Single-File `.exe`
To compile a portable single-file executable that doesn't need .NET installed:
```powershell
dotnet publish SkinConverterCore -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o ./publish
```
The output `SkinConverterCore.exe` will be generated in the `./publish` directory.

---

## Verification Test Suite
To verify the transformation logic against all test dataset mod pairs:
```powershell
.\SkinConverterCore.exe --test-all
```
*(or `dotnet run --project SkinConverterCore -- --test-all`)*
