using WeaponStatShower.Utils;

namespace WeaponStatShower.ExtraDescription.Data
{
    public class ExtraDescriptionData
    {
        public static readonly ExtraDescriptionData[] Template = new ExtraDescriptionData[]
        {
            new()
        };

        public uint ArchetypeID { get; set; } = 0;
        public uint GearCategoryID { get; set; } = 0;
        public LocaleText[] Headers { get; set; } = Array.Empty<LocaleText>();
        public LocaleText[] Descriptions { get; set; } = Array.Empty<LocaleText>();
        public int DescriptionIndexOverride { get; set; } = -1;
        public string Name { get; set; } = string.Empty;
    }
}
