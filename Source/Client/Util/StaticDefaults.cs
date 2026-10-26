using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace Multiplayer.Client.Util
{
    /// <summary>
    /// Restores a type's static fields to the values its static constructor assigned.
    /// Mono re-runs a static constructor called through reflection (TypeInitializer.Invoke), which
    /// Multiplayer relied on to reset game statics; the .NET runtime of RimWorld 1.7 silently ignores
    /// that call. The values are therefore captured once at startup, before the game changes them.
    /// </summary>
    public static class StaticDefaults
    {
        private static readonly Dictionary<Type, (FieldInfo field, object value)[]> snapshots = new();

        public static void Capture(Type type)
        {
            snapshots[type] = type
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(f => !f.IsLiteral && !f.IsInitOnly)
                .Select(f => (f, Copy(f.GetValue(null))))
                .ToArray();
        }

        public static void Restore(Type type)
        {
            if (!snapshots.TryGetValue(type, out var fields))
            {
                type.TypeInitializer?.Invoke(null, null);
                return;
            }

            foreach (var (field, value) in fields)
                field.SetValue(null, Copy(value));
        }

        // Arrays are the only mutable reference values in the captured types (e.g. a shuffled Rot4[]).
        private static object Copy(object value) => value is Array array ? array.Clone() : value;
    }
}
