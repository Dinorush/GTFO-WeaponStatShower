using WeaponStatShower.Utils.Language.Models;

namespace WeaponStatShower.Utils.Language
{
    public class LanguageDatas
    {
        public FiremodeLanguageModel firemode { get; set; } = new();
        public MeleeLanguageModel melee { get; set; } = new();
        public SleepersLanguageModel sleepers { get; set; } = new();
        public string damage { get; set; } = string.Empty;
        public string clip { get; set; } = string.Empty;
        public string maxAmmo { get; set; } = string.Empty;
        public string falloff { get; set; } = string.Empty;
        public string reload { get; set; } = string.Empty;
        public string stagger { get; set; } = string.Empty;
        public string precision { get; set; } = string.Empty;
        public string pierceCount { get; set; } = string.Empty;
        public string rateOfFire { get; set; } = string.Empty;
        public string aimDSpread { get; set; } = string.Empty;
        public string hipSpread { get; set; } = string.Empty;
        public string spread { get; set; } = string.Empty;
        public string deployable { get; set; } = string.Empty;
        public string longChargeUp { get; set; } = string.Empty;
        public string shortChargeUp { get; set; } = string.Empty;
    }
}
