using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GeneScaledWeapons
{
    internal static class ScaleFactor
    {
        private static StatDef _vefCosmeticSize;
        private static StatDef VefCosmeticSize
        {
            get
            {
                if (_vefCosmeticSize == null)
                    _vefCosmeticSize = DefDatabase<StatDef>.GetNamedSilentFail("VEF_CosmeticBodySize_Multiplier");
                return _vefCosmeticSize;
            }
        }

        public static float Factor(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed) return 1f;

            // 1) Prefer VEF stat if present
            // VEF_CosmeticBodySize_Multiplier: lower values = bigger pawns (primarch 0.55 = biggest)
            // Only scale UP for bigger pawns (multiplier < 1.0)
            // Normal pawns (1.0) and smaller (>= 1.0) stay at 1.0x
            var stat = VefCosmeticSize;
            if (stat != null)
            {
                float fVEF = pawn.GetStatValue(stat, true);
                if (!float.IsNaN(fVEF) && !float.IsInfinity(fVEF) && Mathf.Abs(fVEF - 0f) > 0.0001f)
                {
                    // Only scale up for bigger pawns (multiplier < 1.0)
                    if (fVEF >= 1.0f)
                    {
                        // Normal or smaller pawns: no scaling
                        return 1.0f;
                    }
                    else
                    {
                        // Bigger pawns: scale up based on how much smaller the multiplier is
                        // Primarch (0.55) -> scale up
                        // Formula: 1.0 + (1.0 - multiplier) * factor
                        // Try: 1.0 + (1.0 - 0.55) * 1.2 = 1.0 + 0.54 = 1.54x (close to what user wants)
                        float scaleUp = 1.0f + (1.0f - fVEF) * 1.2f;
                        float clamped = Mathf.Clamp(scaleUp, 1.0f, 2.5f);
                        if (Prefs.DevMode && UnityEngine.Random.value < 0.01f) // Log 1% to avoid spam
                            Log.Message($"[GeneScaledWeapons] VEF stat: {fVEF:F2} -> scaleUp {scaleUp:F2} -> clamped {clamped:F2} for {pawn?.LabelShortCap}");
                        return clamped;
                    }
                }
            }

            // 2) Fallback: body graphic drawSize ratio (covers HAR big races)
            try
            {
                var renderer = pawn.Drawer?.renderer;
                if (renderer != null)
                {
                    var graphics = AccessTools.Field(typeof(PawnRenderer), "graphics")?.GetValue(renderer);
                    if (graphics != null)
                    {
                        var nakedGraphic = AccessTools.Property(graphics.GetType(), "nakedGraphic")?.GetValue(graphics);
                        if (nakedGraphic != null)
                        {
                            var drawSize = AccessTools.Property(nakedGraphic.GetType(), "drawSize")?.GetValue(nakedGraphic);
                            if (drawSize is Vector2 ds)
                            {
                                // Vanilla adult human naked body graphic is ~1.5 on the longest axis
                                float baseAxis = 1.5f;
                                float axis = Mathf.Max(ds.x, ds.y);
                                if (axis > 0.01f)
                                {
                                    float ratio = axis / baseAxis;
                                    float curved = Mathf.Pow(ratio, 0.75f);
                                    return Mathf.Clamp(curved, 0.25f, 2.5f);
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            // 3) Fallback: BodySize stat as a rough proxy
            try
            {
                float bodySize = pawn.BodySize; // humans ~1.0
                if (bodySize > 0.01f)
                {
                    float curved = Mathf.Pow(bodySize, 0.75f);
                    return Mathf.Clamp(curved, 0.25f, 2.5f);
                }
            }
            catch { }

            return 1f;
        }

        public static Matrix4x4 Apply(Matrix4x4 m, Pawn pawn)
        {
            float f = Factor(pawn);
            if (Mathf.Approximately(f, 1f)) return m;
            // Right-multiply to scale in local space (T * R * S) -> T * R * (S * scale)
            var s = Matrix4x4.Scale(new Vector3(f, 1f, f));
            return m * s;
        }

        // Legacy Vector3 multiply (for backward compat with old TRS patches)
        public static Vector3 MultiplyVec(Vector3 vanillaScale, object pawnOrRenderer, Thing eq)
        {
            Pawn pawn = null;
            if (pawnOrRenderer is Pawn p)
            {
                pawn = p;
            }
            else if (pawnOrRenderer is PawnRenderer pr)
            {
                var pawnField = AccessTools.Field(typeof(PawnRenderer), "pawn");
                pawn = pawnField?.GetValue(pr) as Pawn;
            }
            
            if (pawn == null) return vanillaScale;
            
            float f = Factor(pawn);
            if (f == 1f) return vanillaScale;
            
            return new Vector3(vanillaScale.x * f, vanillaScale.y, vanillaScale.z * f);
        }

        // Legacy alias for backward compat
        public static Matrix4x4 MultiplyMatrix(Matrix4x4 m, Pawn pawn) => Apply(m, pawn);
    }
}

