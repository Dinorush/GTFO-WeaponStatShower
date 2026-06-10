using BepInEx.Unity.IL2CPP;
using MTFO.API;
using System.Runtime.CompilerServices;

namespace WeaponStatShower.Dependencies
{
    internal static class MTFOWrapper
    {
        public const string PLUGIN_GUID = "com.dak.MTFO";
        public static readonly bool HasMTFO;

        static MTFOWrapper()
        {
            HasMTFO = IL2CPPChainloader.Instance.Plugins.ContainsKey(PLUGIN_GUID);
        }

        public static string GameDataPath => MTFOPathAPI.RundownPath;
        public static string CustomPath =>
        MTFOPathAPI.CustomPath;
        public static bool HasCustomDatablocks => HasMTFO && UnsafeHasCustomDatablocks();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool UnsafeHasCustomDatablocks() => MTFOPathAPI.HasRundownPath;

        public static void AddOnHotReload(Action action)
        {
            if (!HasMTFO) return;
            AddOnHotReload_Unsafe(action);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AddOnHotReload_Unsafe(Action action)
        {
            MTFOHotReloadAPI.OnHotReload += action;
        }
    }
}
