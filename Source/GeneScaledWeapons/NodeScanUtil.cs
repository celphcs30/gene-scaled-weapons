using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace GeneScaledWeapons
{
    internal static class NodeScanUtil
    {
        static readonly MethodInfo TRS = AccessTools.Method(typeof(Matrix4x4), nameof(Matrix4x4.TRS), new[] { typeof(Vector3), typeof(Quaternion), typeof(Vector3) });
        static readonly MethodInfo ScaleM = AccessTools.Method(typeof(Matrix4x4), nameof(Matrix4x4.Scale), new[] { typeof(Vector3) });
        static readonly MethodInfo DrawMesh = AccessTools.Method(typeof(Graphics), nameof(Graphics.DrawMesh), new[] { typeof(Mesh), typeof(Matrix4x4), typeof(Material), typeof(int) });
        static readonly string[] NameHints = new[] { "equip", "weapon", "held", "carry", "primary", "offhand" };

        internal static void LogPotentialEquipmentNodes()
        {
            try
            {
                var asm = typeof(Pawn).Assembly;
                var nodeBase = AccessTools.TypeByName("Verse.PawnRenderNode");
                var workerBase = AccessTools.TypeByName("Verse.PawnRenderNodeWorker");

                if (nodeBase == null)
                {
                    Log.Message("[GeneScaledWeapons] Scan: PawnRenderNode base not found.");
                    return;
                }

                var allTypes = asm.GetTypes().Where(t => t != null && !t.IsAbstract).ToList();
                var nodes = allTypes.Where(t => nodeBase.IsAssignableFrom(t)).ToList();
                var workers = workerBase != null ? allTypes.Where(t => workerBase.IsAssignableFrom(t)).ToList() : new List<Type>();

                Log.Message($"[GeneScaledWeapons] Scan: Found {nodes.Count} nodes, {workers.Count} workers.");

                ScanSet("Node", nodes);
                ScanSet("Worker", workers);
            }
            catch (Exception e)
            {
                Log.Warning("[GeneScaledWeapons] Scan failed: " + e);
            }
        }

        static void ScanSet(string label, List<Type> types)
        {
            foreach (var t in types)
            {
                bool nameHit = NameHints.Any(h => t.Name.IndexOf(h, StringComparison.OrdinalIgnoreCase) >= 0);
                bool fieldHit = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                 .Any(f =>
                                 {
                                     var n = f.Name.ToLowerInvariant();
                                     return n.Contains("equip") || n.Contains("weapon") || n.Contains("primary") || n.Contains("carri") || f.FieldType == typeof(Thing) || f.FieldType == typeof(ThingWithComps);
                                 });

                if (!nameHit && !fieldHit) continue;

                var methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                               .Where(m => !m.IsSpecialName && (m.Name.IndexOf("render", StringComparison.OrdinalIgnoreCase) >= 0
                                                            || m.Name.IndexOf("draw", StringComparison.OrdinalIgnoreCase) >= 0))
                               .ToList();

                if (methods.Count == 0) continue;

                Log.Message($"[GeneScaledWeapons] Scan {label}: {t.FullName}");

                foreach (var m in methods)
                {
                    bool hasTRS = false, hasScale = false, hasDrawMesh = false;
                    try
                    {
                        // Try to read IL to detect TRS/Scale/DrawMesh calls
                        if (m.GetMethodBody() != null)
                        {
                            var ilBytes = m.GetMethodBody().GetILAsByteArray();
                            if (ilBytes != null && ilBytes.Length > 0)
                            {
                                // Simple heuristic: check method name hints and parameter types
                                // Full IL parsing is complex, so we'll rely on method names and try patching
                                var paramTypes = m.GetParameters().Select(p => p.ParameterType.FullName ?? "").ToList();
                                var returnType = m.ReturnType.FullName ?? "";
                                
                                // If method uses Vector3 or Matrix4x4, likely has scaling
                                if (paramTypes.Any(t => t.Contains("Vector3") || t.Contains("Matrix4x4")) || 
                                    returnType.Contains("Matrix4x4"))
                                {
                                    hasTRS = true; // Likely, will verify when patched
                                    hasScale = true;
                                }
                            }
                        }
                    }
                    catch { /* ignore methods we can't read */ }

                    Log.Message($"  - {m.Name} (params: {m.GetParameters().Length}, returns: {m.ReturnType.Name}) | TRS:{hasTRS} Scale:{hasScale} DrawMesh:{hasDrawMesh}");
                }
            }
        }
    }
}
