using Celeste.Mod;
using Celeste.Mod.Helpers;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Registry;

namespace vitmod {
    /// <summary>
    /// Provides utility functions for turning strings to types
    /// </summary>
    internal static class TypeHelper {
        private static Dictionary<string, HashSet<Type>> TypeListCache = new(StringComparer.Ordinal);
        private static Type[] AllEntityTypes;

        /// <summary>
        /// Parses a comma-separated list of c# type full names or short names. Cached.
        /// </summary>
        public static HashSet<Type> ParseTypeList(string list) {
            if (TypeListCache.TryGetValue(list, out var result))
                return result;

            var split = list.Split(',', StringSplitOptions.RemoveEmptyEntries);

            AllEntityTypes ??= FakeAssembly.GetFakeEntryAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(Entity))).ToArray();

            result = new(AllEntityTypes.Where(t =>
                EntityRegistry.GetKnownSidsFromType(t).Overlaps(split)
                || split.Contains(t.FullName, StringComparer.Ordinal)
                || split.Contains(t.Name, StringComparer.Ordinal)));
            TypeListCache[list] = result;

            return result;
        }

        #region Hooks

        internal static void Load() {
            Everest.Events.Everest.OnLoadMod += Event_Everest_OnLoadMod;
        }

        internal static void Unload() {
            Everest.Events.Everest.OnLoadMod -= Event_Everest_OnLoadMod;
        }

        // Clear the cache if a mod is loaded/hot reloaded
        private static void Event_Everest_OnLoadMod(EverestModuleMetadata meta) {
            AllEntityTypes = null;
            TypeListCache.Clear();
        }

        #endregion
    }
}
