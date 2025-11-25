using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace GeneScaledWeapons.Patches
{
    [HarmonyPatch(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawEquipmentAndApparelExtras))]
    internal static class Patch_DrawExtras
    {
        // Track current pawn for the wrapper
        static void Prefix(Pawn pawn) => RenderCtx.CurrentPawn = pawn;
        static void Finalizer() => RenderCtx.CurrentPawn = null;

        // Replace Graphics.DrawMesh calls with our wrapper overloads
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> ins, MethodBase __originalMethod)
        {
            var codes = new List<CodeInstruction>(ins);

            var mDraw4 = AccessTools.Method(typeof(Graphics), nameof(Graphics.DrawMesh),
                new[] { typeof(Mesh), typeof(Matrix4x4), typeof(Material), typeof(int) });

            var mDraw7 = AccessTools.Method(typeof(Graphics), nameof(Graphics.DrawMesh),
                new[] { typeof(Mesh), typeof(Matrix4x4), typeof(Material), typeof(int),
                        typeof(Camera), typeof(int), typeof(MaterialPropertyBlock) });

            var mWrap4 = AccessTools.Method(typeof(GSWGraphics), nameof(GSWGraphics.DrawMeshScaled),
                new[] { typeof(Mesh), typeof(Matrix4x4), typeof(Material), typeof(int) });

            var mWrap7 = AccessTools.Method(typeof(GSWGraphics), nameof(GSWGraphics.DrawMeshScaled),
                new[] { typeof(Mesh), typeof(Matrix4x4), typeof(Material), typeof(int),
                        typeof(Camera), typeof(int), typeof(MaterialPropertyBlock) });

            int replaced = 0;
            for (int i = 0; i < codes.Count; i++)
            {
                var ci = codes[i];
                if (ci.opcode == OpCodes.Call || ci.opcode == OpCodes.Callvirt)
                {
                    if (ci.operand is MethodInfo mi)
                    {
                        if (mi == mDraw4)
                        {
                            ci.operand = mWrap4;
                            replaced++;
                        }
                        else if (mi == mDraw7)
                        {
                            ci.operand = mWrap7;
                            replaced++;
                        }
                    }
                }
                yield return ci;
            }

            if (replaced == 0)
            {
                GSWLog.WarnOnce("GSW: DrawExtras transpiler didn't find Graphics.DrawMesh overloads to replace.", __originalMethod.GetHashCode());
            }
            else
            {
                GSWLog.Verb($"GSW: DrawExtras replaced DrawMesh calls: {replaced}");
            }
        }
    }
}

