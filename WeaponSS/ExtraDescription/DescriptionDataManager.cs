using GTFO.API.Utilities;
using System.Diagnostics.CodeAnalysis;
using WeaponStatShower.Dependencies;
using WeaponStatShower.ExtraDescription.Data;
using WeaponStatShower.Json;

namespace WeaponStatShower.ExtraDescription
{
    public class DescriptionDataManager
    {
        public static readonly DescriptionDataManager Current = new();

        private const string GlobalFile = "Global.json";
        private const string DataDir = "ExtraDescriptions";

        public GlobalData GlobalSettings { get; private set; } = new();
        private readonly Dictionary<string, List<ExtraDescriptionData>> _fileData = new();
        private readonly Dictionary<uint, ExtraDescriptionData> _archIdDataMap = new();
        private readonly Dictionary<uint, ExtraDescriptionData> _gearIdDataMap = new();

        private void GlobalFileChanged(LiveEditEventArgs e)
        {
            WeaponStatShowerPlugin.LogWarning($"LiveEdit Global File Changed");
            LiveEdit.TryReadFileContent(e.FullPath, (content) =>
            {
                ReadGlobalContent(content);
                OnReload();
            });
        }

        private void ReadGlobalContent(string content)
        {
            if (JSON.TryDeserializeSafe<GlobalData>(content, out var globalData))
                GlobalSettings = globalData;
        }

        private void DataFileChanged(LiveEditEventArgs e)
        {
            WeaponStatShowerPlugin.LogWarning($"LiveEdit File Changed: {e.FileName}");
            LiveEdit.TryReadFileContent(e.FullPath, (content) =>
            {
                ReadDataContent(e.FullPath, content);
                OnReload();
            });
        }

        private void DataFileDeleted(LiveEditEventArgs e)
        {
            WeaponStatShowerPlugin.LogWarning($"LiveEdit File Removed: {e.FileName}");
            if (!_fileData.Remove(e.FullPath, out var dataList)) return;

            foreach (var data in dataList)
            {
                if (data.ArchetypeID != 0)
                    _archIdDataMap.Remove(data.ArchetypeID);
                if (data.GearCategoryID != 0)
                    _gearIdDataMap.Remove(data.GearCategoryID);
            }

            OnReload();
        }

        private void DataFileCreated(LiveEditEventArgs e)
        {
            WeaponStatShowerPlugin.LogWarning($"LiveEdit File Created: {e.FileName}");
            LiveEdit.TryReadFileContent(e.FullPath, (content) =>
            {
                ReadDataContent(e.FullPath, content);
                OnReload();
            });
        }

        private void ReadDataContent(string filepath, string content)
        {
            if (_fileData.TryGetValue(filepath, out var dataList))
            {
                foreach (var data in dataList)
                {
                    if (data.ArchetypeID != 0)
                        _archIdDataMap.Remove(data.ArchetypeID);
                    if (data.GearCategoryID != 0)
                        _gearIdDataMap.Remove(data.GearCategoryID);
                }
                dataList.Clear();
            }
            else
            {
                _fileData.Add(filepath, dataList = new());
            }

            if (JSON.TryDeserializeSafe<List<ExtraDescriptionData>>(content, out var jsonList))
            {
                dataList.EnsureCapacity(jsonList.Count);
                foreach (var data in jsonList)
                {
                    if (data.ArchetypeID == 0 && data.GearCategoryID == 0) continue;
                    
                    if (data.ArchetypeID > 0 && !_archIdDataMap.TryAdd(data.ArchetypeID, data))
                        WeaponStatShowerPlugin.LogWarning($"Duplicate archetype ID {data.ArchetypeID} found. Name: {_archIdDataMap[data.ArchetypeID].Name}, duplicate name: {data.Name}");
                    if (data.GearCategoryID > 0 && !_gearIdDataMap.TryAdd(data.GearCategoryID, data))
                        WeaponStatShowerPlugin.LogWarning($"Duplicate gear category ID {data.GearCategoryID} found. Name: {_gearIdDataMap[data.GearCategoryID].Name}, duplicate name: {data.Name}");

                    dataList.Add(data);
                }
                dataList.TrimExcess();
            }
        }

        public DescriptionDataManager()
        {
            if (!MTFOWrapper.HasMTFO) return;

            string DEFINITION_PATH = Path.Combine(MTFOWrapper.CustomPath, WeaponStatShowerPlugin.GUID);
            string GLOBAL_PATH = Path.Combine(DEFINITION_PATH, GlobalFile);
            string DATA_PATH = Path.Combine(DEFINITION_PATH, DataDir);

            if (!Directory.Exists(DEFINITION_PATH))
            {
                WeaponStatShowerPlugin.LogInfo($"No {WeaponStatShowerPlugin.ModName} directory detected. Creating templates.");
                Directory.CreateDirectory(DATA_PATH);

                StreamWriter file;
                using (file = File.CreateText(GLOBAL_PATH))
                    file.WriteLine(JSON.Serialize(new GlobalData()));

                using (file = File.CreateText(Path.Combine(DATA_PATH, "Template.json")))
                    file.WriteLine(JSON.Serialize(ExtraDescriptionData.Template));
            }
            else if (!File.Exists(GLOBAL_PATH))
            {
                using (StreamWriter file = File.CreateText(GLOBAL_PATH))
                    file.WriteLine(JSON.Serialize(new GlobalData()));
            }

            ReadGlobalContent(File.ReadAllText(GLOBAL_PATH));
            foreach (string filepath in Directory.EnumerateFiles(DATA_PATH, "*.json", SearchOption.AllDirectories))
                ReadDataContent(filepath, File.ReadAllText(filepath));

            var listener = LiveEdit.CreateListener(DEFINITION_PATH, GlobalFile, false);
            listener.FileChanged += GlobalFileChanged;

            listener = LiveEdit.CreateListener(DATA_PATH, "*.json", true);
            listener.FileCreated += DataFileCreated;
            listener.FileChanged += DataFileChanged;
            listener.FileDeleted += DataFileDeleted;
        }

        private void OnReload()
        {

        }

        internal void Init()
        {
            if (!MTFOWrapper.HasMTFO) return;

            MTFOWrapper.AddOnHotReload(OnReload);
        }

        public static bool TryGetArchData(uint archetypeID, [MaybeNullWhen(false)] out ExtraDescriptionData data)
        {
            return Current._archIdDataMap.TryGetValue(archetypeID, out data);
        }

        public static bool TryGetGearData(uint gearCategoryID, [MaybeNullWhen(false)] out ExtraDescriptionData data)
        {
            return Current._gearIdDataMap.TryGetValue(gearCategoryID, out data);
        }
    }
}
