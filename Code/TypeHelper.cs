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
    public static class TypeHelper {
        /// <summary>
        /// Wrapper class for checking if a given entity's type is in a set of types or entity SIDs.
        /// Types are found from SIDs each time when checking to ensure entities created through static generator methods always have their SIDs picked up.
        /// </summary>
        /// <param name="types">The list of types.</param>
        /// <param name="sids">The list of entity SIDs.</param>
        public class TypeAndSidList(HashSet<Type> types, HashSet<string> sids) {
            public bool IsEmpty => types.Count == 0 && sids.Count == 0;

            public bool Contains(Type type) {
                if (types.Contains(type))
                    return true;

                if (sids.Overlaps(EntityRegistry.GetKnownSidsFromType(type)))
                    return true;

                return false;
            }

            public bool Contains(Entity entity) {
                var type = entity.GetType();

                if (types.Contains(type))
                    return true;

                if (entity.SourceData?.Name is { } sourceSid
                    ? sids.Contains(sourceSid)
                    : sids.Overlaps(EntityRegistry.GetKnownSidsFromType(type)))
                    return true;

                return false;
            }
        }

        private static readonly Dictionary<string, TypeAndSidList> TypeListCache = new(StringComparer.Ordinal);
        private static Type[] allEntityTypes;

        /// <summary>
        /// Parses a comma-separated list of C# type full names, short names, or entity SIDs. Cached.
        /// </summary>
        public static TypeAndSidList ParseTypeList(string list) {
            if (TypeListCache.TryGetValue(list, out var result))
                return result;

            allEntityTypes ??= FakeAssembly.GetFakeEntryAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(Entity))).ToArray();

            var split = list.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var types = new HashSet<Type>();
            var sids = new HashSet<string>(split);
            foreach (var type in allEntityTypes) {
                if (split.Contains(type.FullName, StringComparer.Ordinal)) {
                    types.Add(type);
                    sids.Remove(type.FullName);
                }

                if (split.Contains(type.Name, StringComparer.Ordinal)) {
                    types.Add(type);
                    sids.Remove(type.Name);
                }
            }

            result = new(types, sids);
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
            allEntityTypes = null;
            TypeListCache.Clear();
        }

        #endregion
    }
}
