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
        static readonly MethodInfo MI_MULTIPLY = AccessTools.Method(typeof(ScaleFactor), nameof(ScaleFactor.MultiplyVec));

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

            // Find Pawn and Thing/eq parameters
            int pawnParamIndex = -1;
            int eqParamIndex = -1;
            var parameters = original.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (typeof(Pawn).IsAssignableFrom(parameters[i].ParameterType) && pawnParamIndex < 0)
                    pawnParamIndex = i;
                if (typeof(Thing).IsAssignableFrom(parameters[i].ParameterType) && eqParamIndex < 0)
                    eqParamIndex = i;
            }

            // If instance method, check if 'this' is PawnRenderer (has pawn field)
            bool isInstanceMethod = !original.IsStatic;

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

                    // Stack before TRS/Scale: ..., pos, rot, scale (for TRS) OR ..., scale (for Scale)
                    // We need to multiply the scale Vector3: ScaleFactor.MultiplyVec(scale, pawn, eq)
                    // Stack after MultiplyVec: ..., pos, rot, scale' (modified scale)

                    // Before the call, scale is on stack. We need to:
                    // 1. Load pawn (from param or 'this')
                    // 2. Load eq (from param or pawn.equipment.Primary)
                    // 3. Call MultiplyVec(scale, pawn, eq)

                    // Load pawn (or PawnRenderer 'this')
                    if (pawnParamIndex >= 0)
                        yield return LoadArg(pawnParamIndex);
                    else if (isInstanceMethod && original.DeclaringType?.Name == "PawnRenderer")
                        yield return new CodeInstruction(OpCodes.Ldarg_0); // 'this' PawnRenderer, MultiplyVec will extract pawn
                    else if (isInstanceMethod)
                        yield return new CodeInstruction(OpCodes.Ldarg_0); // 'this' for instance methods, might be useful
                    else
                        yield return new CodeInstruction(OpCodes.Ldnull); // No pawn available

                    // Load eq
                    if (eqParamIndex >= 0)
                        yield return LoadArg(eqParamIndex);
                    else
                        yield return new CodeInstruction(OpCodes.Ldnull); // Will resolve from pawn in MultiplyVec

                    // Call MultiplyVec(scale, pawn, eq) -> returns modified scale
                    yield return new CodeInstruction(OpCodes.Call, MI_MULTIPLY);
                    patchedAny = true;
                }

                yield return ins;
            }

            if (patchedAny)
                GSWLog.Trace($"Transpiler on {original.DeclaringType?.FullName}.{original.Name}: TRS={countTRS}, SetTRS={countSetTRS}, Scale={countScale}");
            else
                GSWLog.WarnOnce($"Transpiler didn't find Matrix4x4.TRS/Scale/SetTRS in {original.DeclaringType?.FullName}.{original.Name}", original.GetHashCode());
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

        // Helper to resolve Pawn and Thing from context (backward compat)
        static void ResolveContext(object context, out Pawn pawn, out Thing eq)
        {
            pawn = TryGetPawn(context);
            
            // Try to get eq from context or pawn
            if (context is Thing ctxThing)
            {
                eq = ctxThing;
            }
            else if (pawn != null)
            {
                eq = pawn.equipment?.Primary;
            }
            else
            {
                eq = null;
            }
        }
    }
}
