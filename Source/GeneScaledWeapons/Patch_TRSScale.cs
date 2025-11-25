using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GeneScaledWeapons
{
    internal static class Patch_TRSScale
    {
        // static caches (outside methods)
        static readonly MethodInfo MI_TRS = AccessTools.Method(typeof(Matrix4x4), nameof(Matrix4x4.TRS), new[] { typeof(Vector3), typeof(Quaternion), typeof(Vector3) });
        static readonly MethodInfo MI_SCALE = AccessTools.Method(typeof(Matrix4x4), nameof(Matrix4x4.Scale), new[] { typeof(Vector3) });
        static readonly MethodInfo MI_SETTRS = AccessTools.Method(typeof(Matrix4x4), "SetTRS", new[] { typeof(Vector3), typeof(Quaternion), typeof(Vector3) });
        static readonly MethodInfo MI_ADJUST = AccessTools.Method(typeof(Patch_TRSScale), nameof(AdjustScale));

        private static CodeInstruction LoadArg(int index)
        {
            return index switch
            {
                0 => new CodeInstruction(OpCodes.Ldarg_0),
                1 => new CodeInstruction(OpCodes.Ldarg_1),
                2 => new CodeInstruction(OpCodes.Ldarg_2),
                3 => new CodeInstruction(OpCodes.Ldarg_3),
                _ => new CodeInstruction(OpCodes.Ldarg_S, (byte)index)
            };
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var list = new List<CodeInstruction>(instructions);
            bool patchedAny = false;
            int countTRS = 0, countScale = 0, countSetTRS = 0;

            // If static, find a Pawn arg if present
            int pawnParamIndex = -1;
            if (original.IsStatic)
            {
                var pars = original.GetParameters();
                for (int i = 0; i < pars.Length; i++)
                    if (typeof(Pawn).IsAssignableFrom(pars[i].ParameterType)) { pawnParamIndex = i; break; }
            }

            for (int i = 0; i < list.Count; i++)
            {
                var ins = list[i];

                bool isTRS = ins.Calls(MI_TRS);
                bool isScale = ins.Calls(MI_SCALE);
                bool isSetTRS = ins.Calls(MI_SETTRS);

                if (isTRS || isScale || isSetTRS)
                {
                    if (isTRS) countTRS++;
                    if (isScale) countScale++;
                    if (isSetTRS) countSetTRS++;

                    // Stack right now: ..., [this?], pos, rot, scale  (for TRS/SetTRS)  OR ..., scale (for Scale)
                    // We want: ..., ..., scale, context -> AdjustScale -> ..., ..., scale'
                    if (original.IsStatic && pawnParamIndex >= 0)
                        yield return LoadArg(pawnParamIndex);
                    else
                        yield return new CodeInstruction(OpCodes.Ldarg_0); // 'this' OR first arg for statics without Pawn; AdjustScale will resolve

                    yield return new CodeInstruction(OpCodes.Call, MI_ADJUST);
                    patchedAny = true;
                }

                yield return ins;
            }

#if DEBUG
            if (patchedAny)
                Log.Message($"[GeneScaledWeapons] Transpiler on {original.DeclaringType?.FullName}.{original.Name}: TRS={countTRS}, SetTRS={countSetTRS}, Scale={countScale}");
#endif
            // iterator ends
        }

        static Pawn TryGetPawn(object ctx)
        {
            if (ctx == null) return null;
            if (ctx is Pawn p1) return p1;

            // PawnRenderer path
            if (ctx is PawnRenderer pr)
            {
                var pawnField = AccessTools.Field(typeof(PawnRenderer), "pawn");
                return pawnField?.GetValue(pr) as Pawn;
            }

            // Render node/context (reflect pawn field/property if present)
            try
            {
                var t = ctx.GetType();
                var pi = t.GetProperty("pawn", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pi != null && typeof(Pawn).IsAssignableFrom(pi.PropertyType))
                    return (Pawn)pi.GetValue(ctx);
                var fi = t.GetField("pawn", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fi != null && typeof(Pawn).IsAssignableFrom(fi.FieldType))
                    return (Pawn)fi.GetValue(ctx);
            }
            catch { }

            // Thing/weapon path
            if (ctx is ThingWithComps twc)
            {
                var comp = twc.TryGetComp<CompEquippable>();
                return comp?.PrimaryVerb?.CasterPawn;
            }
            if (ctx is Thing t2)
            {
                var comp = t2.TryGetComp<CompEquippable>();
                return comp?.PrimaryVerb?.CasterPawn;
            }

            return null;
        }

        public static Vector3 AdjustScale(Vector3 s, object context)
        {
            try
            {
                // Resolve pawn
                var pawn = TryGetPawn(context);
                if (pawn == null) return s;

                // Only scale actual held weapons
                if (!(context is ThingWithComps eq)) return s;
                var comp = eq.TryGetComp<CompEquippable>();
                if (comp == null) return s;

                // Must be drawn for this pawn (not some preview or other owner)
                if (comp.PrimaryVerb?.CasterPawn != pawn) return s;

                // Optional: allow defs to opt-out
                var skipExt = eq.def?.GetModExtension<ModExt_SkipGeneWeaponScale>();
                if (skipExt != null) return s;

                // Per-pawn multiplier
                float mult = GeneScaleUtil.WeaponScaleFor(pawn);
                if (mult == 1f) return s;

                // Scale only X/Z for RimWorld quads; keep Y as-is
                return new Vector3(s.x * mult, s.y, s.z * mult);
            }
            catch (Exception e)
            {
                Log.Error($"[GeneScaledWeapons] AdjustScale error: {e}");
                return s;
            }
        }
    }
}
