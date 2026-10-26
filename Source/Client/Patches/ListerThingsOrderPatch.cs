using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Multiplayer.Client.Patches
{
    /// <summary>
    /// RimWorld 1.7 removes things from ListerThings lists by moving the last element into the hole.
    /// Motes with a GUI overlay (damage text, ...) are listed too, and real-time motes end in
    /// <c>Mote.RealtimeUpdate</c>, once per rendered frame: each player removed them at a different
    /// moment, so the swap reordered the remaining things differently. A join point then saved and
    /// reloaded every map in a different order (cells come back in that order), and code iterating
    /// the lists picked other things. In Multiplayer the removal keeps the order, as in 1.6.
    /// </summary>
    [HarmonyPatch]
    static class ListerThingsOrderedRemoval
    {
        static MethodBase TargetMethod() => AccessTools.Method(typeof(ListerThings), "RemoveSwap");

        static bool Prepare() => TargetMethod() != null;

        static bool Prefix(List<Thing> list, Thing t)
        {
            if (Multiplayer.Client == null) return true;

            int index = list.IndexOf(t);
            if (index >= 0)
                list.RemoveAt(index);
            return false;
        }
    }
}
