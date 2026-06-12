using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using GTFO.API.Utilities;
using HarmonyLib;
using WeaponStatShower.Dependencies;
using WeaponStatShower.ExtraDescription;
using WeaponStatShower.Patches;
using WeaponStatShower.Utils.Language;

namespace WeaponStatShower
{
    [BepInPlugin(GUID, ModName, "2.0.1")]
    [BepInProcess("GTFO.exe")]
    [BepInDependency(MTFOWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(PartialData.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class WeaponStatShowerPlugin : BasePlugin
    {
        internal const string ModName = "Weapon Stat Shower";
        internal const string GUID = "WeaponStatShower";

        private const string SectionMain = "Config";
        private static readonly ConfigDefinition ConfigDefinition = new(SectionMain, "Version");
        private static readonly ConfigDefinition ConfigGameVersion = new(SectionMain, "GameVersion");

        private static Harmony? HarmonyInstance;
        private static readonly Dictionary<Type, Patches.Patch> RegisteredPatches = new();

        public static WeaponStatShowerPlugin Instance { get; private set; }
        internal static bool ShowStats => _showStats.Value == ShowStatSetting.Force ||
            (_showStats.Value == ShowStatSetting.True && !DescriptionDataManager.Current.GlobalSettings.PreferHideStats);
        internal static string SleepersShown => _sleepersShown.Value.Trim().ToUpper();
        internal static LanguageEnum ConfigLanguage => _configLanguage.Value;
        internal static StatsPosition StatsLocation => _statsPosition.Value;

        public override void Load()
        {
            Instance = this;
            Config.SaveOnConfigSet = true;
            LogInfo("STARTED");
            DescriptionDataManager.Current.Init();
            RegisterPatch<DescriptionToggle>();
            BuildConfig(Config);
        }

        public static void RegisterPatch<T>() where T : Patches.Patch, new()
        {
            HarmonyInstance ??= new Harmony(GUID);

            if (RegisteredPatches.TryGetValue(typeof(T), out _))
            {
                LogDebug($"Ignoring duplicate patch: {typeof(T).Name}");
                return;
            }

            var patch = new T { Harmony = HarmonyInstance };
            patch.Initialize();

            if (patch.Enabled)
            {
                LogInfo($"Applying patch: {patch.Name}");
                patch.Execute();
            }

            RegisteredPatches[typeof(T)] = patch;
        }

        public static void LogDebug(object data) => Instance.Log.LogDebug(data);

        public static void LogError(object data) => Instance.Log.LogError(data);

        public static void LogFatal(object data) => Instance.Log.LogFatal(data);

        public static void LogInfo(object data) => Instance.Log.LogInfo(data);

        public static void LogMessage(object data) => Instance.Log.LogMessage(data);

        public static void LogWarning(object data) => Instance.Log.LogWarning(data);

        private static ConfigEntry<LanguageEnum> _configLanguage = null!;
        private static ConfigEntry<string> _sleepersShown = null!;
        private static ConfigEntry<ShowStatSetting> _showStats = null!;
        private static ConfigEntry<StatsPosition> _statsPosition = null!;

        private static void BuildConfig(ConfigFile file)
        {
            string section = "ShowStat";
            _configLanguage = file.Bind(section, "Language", LanguageEnum.English, "Select the mod language.");
            _sleepersShown = file.Bind(section, "SleepersShown", "NONE", "Select which Sleepers are shown, seperated by a comma.\n" +
                "Acceptable values: ALL, NONE, STRIKER, SHOOTER, SCOUT, BIG_STRIKER, BIG_SHOOTER, CHARGER, CHARGER_SCOUT");
            _showStats = file.Bind(section, "ShowStats", ShowStatSetting.True, "Add a description tab with auto-generated weapon stats.\nForce will always create a tab, even if the rundown developer disables it.");
            _statsPosition = file.Bind(section, "StatsPosition", StatsPosition.Last, "Which tab to place the auto-generated weapon stats.\nCombined will combine it with the normal description, similar to older versions.");

            (var dir, var fileName) = (Path.GetDirectoryName(file.ConfigFilePath), Path.GetFileName(file.ConfigFilePath));
            var liveEditListener = LiveEdit.CreateListener(dir, fileName, false);
            liveEditListener.FileChanged += (_) => file.Reload();
        }

        enum ShowStatSetting
        {
            False,
            True,
            Force
        }

        public enum StatsPosition
        {
            Last,
            First,
            Combined
        }
    }
}
