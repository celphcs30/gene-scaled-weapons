using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GeneScaledWeapons
{
    internal static class ScaleFactor
    {
        // Return a factor to multiply vanilla scale by (1 = no change).
        public static float Factor(Pawn pawn, Thing eq)
        {
            // Defensive nulls
            if (pawn == null || eq?.def == null)
                return 1f;

            // Gate (null-safe + debug)
            if (!ScaleGate.ShouldScale(eq))
                return 1f;

            // Gene factor from existing calculator
            float geneFactor = GeneScaleUtil.WeaponScaleFor(pawn);

            // Per-def extra multiplier
            float defMult = ScaleGate.ExtraMult(eq);

            float final = geneFactor * defMult;

            // Optional sanity: clamp to reasonable range to avoid crazy visuals
            if (final < 0.25f) final = 0.25f;
            if (final > 4f) final = 4f;

            return final;
        }

        // Helper to multiply a Vector3 uniformly on X/Z, keep Y as-is
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

