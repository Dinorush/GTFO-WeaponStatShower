using BepInEx.Unity.IL2CPP;
using MTFO.Ext.PartialData;
using MTFO.Ext.PartialData.JsonConverters;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace WeaponStatShower.Dependencies
{
    internal static class PartialData
    {
        public const string PLUGIN_GUID = "MTFO.Extension.PartialBlocks";
        public static readonly bool HasPData;
        public static JsonConverter PDataIDConverter = null!;

        static PartialData()
        {
            HasPData = IL2CPPChainloader.Instance.Plugins.ContainsKey(PLUGIN_GUID);
            if (HasPData)
                SetConverter();
        }

        public static bool TryGetGUID(string text, out uint guid)
        {
            if (!HasPData)
            {
                guid = 0;
                return false;
            }
            return TryGetGUID_Internal(text, out guid);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool TryGetGUID_Internal(string text, out uint guid) => PersistentIDManager.TryGetId(text, out guid);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void SetConverter() => PDataIDConverter = new PersistentIDConverter();
    }
}
