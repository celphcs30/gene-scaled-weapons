using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GeneScaledWeapons
{
    public static class GeneScaleUtil
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

        public static float GetPawnScaleFactor(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed) return 1f;

            // 1) Prefer VEF stat if present
            var stat = VefCosmeticSize;
            if (stat != null)
            {
                float fVEF = pawn.GetStatValue(stat, true);
                if (!float.IsNaN(fVEF) && !float.IsInfinity(fVEF) && Mathf.Abs(fVEF - 0f) > 0.0001f)
                    return Mathf.Clamp(fVEF, 0.5f, 3.0f);
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
                                    return Mathf.Clamp(axis / baseAxis, 0.5f, 3.0f);
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
                    return Mathf.Clamp(bodySize, 0.5f, 3.0f);
            }
            catch { }

            return 1f;
        }

        public static bool ShouldSkip(ThingDef def)
        {
            if (def == null) return true;

            // Only explicit opt-out
            if (def.HasModExtension<ModExt_SkipGeneWeaponScale>()) return true;

            return false;
        }
    }
}
