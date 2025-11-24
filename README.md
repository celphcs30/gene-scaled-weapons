# Gene Scaled Weapons

RimWorld mod that scales held weapon draw size by the pawn's Vanilla Expanded Framework cosmetic body size multiplier.

## Features

- **Dynamic weapon scaling**: Weapons held by pawns are scaled based on their `VEF_CosmeticBodySize_Multiplier` stat
- **No hard dependency**: Works without VEF installed (defaults to 1.0 scale factor)
- **Smart blacklist**: Automatically skips weapons from Rimdark mods
- **Per-weapon control**: Use `ModExt_SkipGeneWeaponScale` extension to exclude specific weapons
- **Safe scaling**: Clamps scale factor between 0.5x and 2.5x to prevent extreme sizes

## How It Works

The mod uses a Harmony transpiler to intercept `Graphics.DrawMesh` calls in `PawnRenderer.DrawEquipmentAiming`. When a weapon is drawn, the mod:

1. Checks if the weapon should be skipped (blacklist or extension flag)
2. Retrieves the pawn's `VEF_CosmeticBodySize_Multiplier` stat value
3. Applies the scale factor to the weapon's transformation matrix (X and Z axes only, Y remains 1.0)
4. Draws the weapon at the scaled size

## Compatibility

- **RimWorld**: 1.6
- **Vanilla Expanded Framework**: Optional (works without it)
- **Harmony**: Required (included in mod)

## Installation

1. Place the `GeneScaledWeapons` folder in your RimWorld `Mods` directory
2. Enable the mod in RimWorld's mod menu
3. Load order doesn't matter (no dependencies)

## Blacklist

The mod automatically skips scaling for:
- Any weapon with the `ModExt_SkipGeneWeaponScale` extension
- Any weapon from a mod whose `packageId` contains "rimdark" (case-insensitive)

### Adding Weapons to Blacklist

To exclude a specific weapon, add the extension to its `ThingDef`:

```xml
<ThingDef ParentName="BaseWeapon">
  <defName>ExampleWeapon</defName>
  <modExtensions>
    <li Class="GeneScaledWeapons.ModExt_SkipGeneWeaponScale"/>
  </modExtensions>
</ThingDef>
```

## Technical Details

- **Package ID**: `celphcs30.genescaledweapons`
- **Type**: C# Harmony mod
- **Method**: IL transpiler on `PawnRenderer.DrawEquipmentAiming`
- **Scale Range**: 0.5x to 2.5x (clamped)
- **Scale Axes**: X and Z only (Y axis remains 1.0 to preserve vertical positioning)

## Building from Source

1. Ensure you have .NET SDK installed
2. Place `0Harmony.dll` in the `Source` directory
3. Build using:
   ```bash
   dotnet build Source/GeneScaledWeapons.csproj
   ```
4. The compiled DLL will be in `Assemblies/GeneScaledWeapons.dll`

## Credits

- **Author**: celphcs30
- **Harmony**: https://github.com/pardeike/Harmony

## License

See LICENSE file for details.

