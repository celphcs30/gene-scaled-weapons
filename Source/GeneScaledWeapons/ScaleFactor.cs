using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GeneScaledWeapons
{
    internal static class ScaleFactor
    {
        public static float Factor(Pawn pawn, Thing eq)
        {
            if (pawn == null || eq?.def == null) return 1f;
            if (!ScaleGate.ShouldScale(eq)) return 1f;

            // Gene factor from existing calculator
            float geneFactor = GeneScaleUtil.WeaponScaleFor(pawn);
            
            // Per-def extra multiplier
            float defMult = ScaleGate.ExtraMult(eq);
            float f = geneFactor * defMult;
            
            if (f < 0.25f) f = 0.25f;
            if (f > 4f) f = 4f;
            return f;
        }

        public static Matrix4x4 MultiplyMatrix(Matrix4x4 m, Pawn pawn, Thing eq)
        {
            float f = Factor(pawn, eq);
            if (Mathf.Approximately(f, 1f)) return m;
            
            // Post-multiply by scale on X/Z: scale columns 0 and 2
            m.m00 *= f; m.m10 *= f; m.m20 *= f; m.m30 *= f;
            m.m02 *= f; m.m12 *= f; m.m22 *= f; m.m32 *= f;
            return m;
        }

        // Overload that fetches eq safely
        public static Matrix4x4 MultiplyMatrix(Matrix4x4 m, Pawn pawn)
        {
            Thing eq = pawn?.equipment?.Primary;
            return MultiplyMatrix(m, pawn, eq);
        }

        // Legacy Vector3 multiply (kept for backward compat)
        public static Vector3 MultiplyVec(Vector3 vanillaScale, object pawnOrRenderer, Thing eq)
        {
            // Resolve pawn from pawnOrRenderer (could be Pawn, PawnRenderer, or null)
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
            
            // Resolve eq if null
            if (eq == null && pawn != null)
            {
                eq = pawn.equipment?.Primary;
            }
            else if (pawn == null && eq != null)
            {
                // Try to get pawn from eq
                var comp = eq.TryGetComp<CompEquippable>();
                pawn = comp?.PrimaryVerb?.CasterPawn;
            }
            
            if (pawn == null || eq == null) return vanillaScale;
            
            float f = Factor(pawn, eq);
            if (f == 1f) return vanillaScale;
            
            return new Vector3(vanillaScale.x * f, vanillaScale.y, vanillaScale.z * f);
        }
    }
}

