using WeaponStatShower.Utils;

namespace WeaponStatShower.ExtraDescription.Data
{
    public class ExtraDescriptionData
    {
        public static readonly ExtraDescriptionData[] Template = new ExtraDescriptionData[]
        {
            new(),
            new()
            {
                Headers = new LocaleText[] {LocaleText.Empty, new("Third Tab")},
                Descriptions = new LocaleText[] {new("Custom tab for testing"), new("Another custom tab under the third tab")},
                Name = "Filled Example"
            }
        };

        public uint ArchetypeID { get; set; } = 0;
        public uint GearCategoryID { get; set; } = 0;
        public LocaleText[] Headers { get; set; } = Array.Empty<LocaleText>();
        public LocaleText[] Descriptions { get; set; } = Array.Empty<LocaleText>();
        public int DescriptionIndexOverride { get; set; } = -1;
        public string Name { get; set; } = string.Empty;
    }
}
