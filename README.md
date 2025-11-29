# Gene Scaled Weapons

RimWorld mod that scales handheld weapons based on the pawn's Vanilla Expanded Framework cosmetic body size multiplier.

## Overview

This mod automatically adjusts weapon size to match the pawn's body size, making weapons look proportional to the pawn holding them. Bigger pawns get bigger weapons, smaller pawns get smaller weapons.

## How It Works

The mod uses the `VEF_CosmeticBodySize_Multiplier` stat from Vanilla Expanded Framework to determine weapon scaling:

- **Bigger pawns** (higher VEF value) → **Bigger weapons**
- **Normal pawns** (VEF = 1.0) → **Normal weapons** (no change)
- **Smaller pawns** (lower VEF value) → **Smaller weapons**

### Scaling Formula

The mod uses a softened power curve to prevent extreme scaling:

```
weaponScale = VEF^0.5
```

Where:
- `VEF` = `VEF_CosmeticBodySize_Multiplier` stat value
- `0.5` = power curve factor (k) that softens the scaling

### Example Scaling Values

With k = 0.5:
- **VEF 1.0** (normal human): `1.0^0.5 = 1.0x` (no change)
- **VEF 1.5** (bigger pawn): `1.5^0.5 ≈ 1.22x` (22% larger)
- **VEF 2.0** (huge pawn): `2.0^0.5 ≈ 1.41x` (41% larger)
- **VEF 0.5** (small pawn): `0.5^0.5 ≈ 0.71x` (29% smaller)

### Power Curve Factor (k)

The `k` value controls how strongly the scaling is applied:
- **k = 0.0**: No scaling (weapons always 1.0x)
- **k = 0.5**: Moderate scaling (current default, good balance)
- **k = 1.0**: Full proportional scaling (weapon scale = VEF value exactly)

You can adjust `k` in `Source/GeneScaledWeapons/ScaleFactor.cs` if you want stronger or weaker scaling.

## Technical Details

### Rendering System

The mod hooks into RimWorld's rendering pipeline at multiple points:

1. **Equipment Context Patcher**: Sets the current pawn context during equipment rendering
   - Patches `PawnRenderUtility.DrawEquipmentAndApparelExtras`
   - Patches `PawnRenderer.DrawEquipment` and `DrawEquipmentAiming`
   - Scans for RenderNode equipment render methods

2. **Graphics Draw Patcher**: Intercepts mesh drawing calls
   - Patches `GenDraw.DrawMeshNowOrLater` (Matrix4x4 overloads)
   - Patches `Graphics.DrawMesh` (Matrix4x4 overloads)
   - Scales the transformation matrix before rendering

3. **Thread-Safe Context**: Uses thread-static storage to track the current pawn during rendering

### Matrix Scaling

Weapons are scaled by modifying the transformation matrix:
- Scales X and Z axes (horizontal plane)
- Y axis (vertical) remains unchanged
- Uses right-multiply: `matrix = matrix * Scale(Vector3(scale, 1, scale))`

### Compatibility

- **RimWorld 1.6.9373+**: Full support via RenderNode system
- **Older versions**: Falls back to legacy `PawnRenderer` methods
- **No VEF stat**: If `VEF_CosmeticBodySize_Multiplier` is not present, weapons remain unchanged (1.0x)

## Requirements

- RimWorld 1.6+
- Vanilla Expanded Framework (for `VEF_CosmeticBodySize_Multiplier` stat)
- Harmony (included in RimWorld)

## Installation

1. Download the mod
2. Extract to RimWorld `Mods` directory
3. Enable in RimWorld mod list
4. Load order: Should load after Vanilla Expanded Framework

## Settings

The mod includes in-game settings (Options → Mod Settings → Gene Scaled Weapons):

- **Logging level**: Control verbosity (Off, Minimal, Verbose, Trace)
- **Scale ranged weapons**: Toggle scaling for ranged weapons (default: ON)
- **Scale melee weapons**: Toggle scaling for melee weapons (default: OFF)
- **Debug: Force scaling**: Ignore all blacklists and settings (for testing)

## Blacklisted Weapons

Certain weapons are automatically excluded from scaling because they already have correct scaling built-in:

- **RimDark 40k - Mankinds Finest weapons**: All weapons with the `BEWH_` prefix or specific weapon tags are blacklisted to prevent double-scaling. These weapons are already properly scaled by the RimDark mod itself.

## What Gets Scaled

- **All handheld weapons** (ranged and melee, if enabled)
- Only weapons held by pawns (not dropped items)
- Works in both aiming and idle/held states

## What Doesn't Get Scaled

- Dropped weapons on the ground
- Weapons in storage
- Apparel and other equipment (only weapons)
- **RimDark 40k weapons** (automatically blacklisted - these weapons are already correctly scaled by the RimDark mod)
  - All weapons with `BEWH_` prefix
  - Weapons with tags: `BEWH_AstartesRanged`, `BEWH_AstartesMelee`, `BEWH_ProtoAstartesRanged`, `BEWH_ProtoAstartesMelee`

## Troubleshooting

### Weapons aren't scaling

1. Check that the pawn has the `VEF_CosmeticBodySize_Multiplier` stat
2. Enable Dev Mode and check logs for `[GeneScaledWeapons]` messages
3. Verify the mod is enabled and loaded after VEF

### Weapons are too big/small

Adjust the `k` value in `ScaleFactor.cs`:
- Smaller k (e.g., 0.4) = softer scaling
- Larger k (e.g., 0.6) = stronger scaling

### Performance issues

The mod uses efficient matrix operations and only scales during equipment rendering. If you experience issues, try:
- Disabling scaling for melee weapons (if not needed)
- Reducing logging level to Minimal or Off

## Credits

- **Author**: celphcs30
- **Package ID**: `celphcs30.genescaledweapons`

## License

See LICENSE file for details.
