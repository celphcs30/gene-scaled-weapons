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
            var stat = VefCosmeticSize;
            if (stat != null)
            {
                float fVEF = pawn.GetStatValue(stat, true);
                if (!float.IsNaN(fVEF) && !float.IsInfinity(fVEF) && Mathf.Abs(fVEF - 0f) > 0.0001f)
                    return Mathf.Clamp(fVEF, 0.25f, 4.0f);
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
                                    return Mathf.Clamp(axis / baseAxis, 0.25f, 4.0f);
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
                    return Mathf.Clamp(bodySize, 0.25f, 4.0f);
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

