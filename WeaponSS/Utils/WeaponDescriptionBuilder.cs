using GameData;
using Gear;
using Player;
using System.Text;
using System.Text.Json;
using WeaponStatShower.Utils.Language;

namespace WeaponStatShower.Utils
{
    internal class WeaponDescriptionBuilder
    {
        private PlayerDataBlock _playerDB;
        private GearIDRange _idRange;
        private uint _categoryID;
        private GearCategoryDataBlock _gearCategoryDB;
        private ItemDataBlock _itemDB;
        private LanguageDatas _languageDatas;
        private LanguageEnum _language;
        private SleepersDatas _sleepersDatas;
        private string _lastSleepersDatas;
        private readonly StringBuilder _strBuilder = new();

        private const string DIVIDER = " | ";
        private const string CLOSE_COLOR_TAG = "</color>";
        private const string SHOTGUN_PREFAB = "Assets/AssetPrefabs/Items/Weapons/GearSetup/ShotgunWeaponFirstPerson.prefab";

        internal void Inizialize(GearIDRange idRange, PlayerDataBlock playerDB, LanguageEnum language) // Inizialize
        {
            _idRange = idRange;
            _playerDB = playerDB;
            _categoryID = idRange.GetCompID(eGearComponent.Category);
            _gearCategoryDB = GameDataBlockBase<GearCategoryDataBlock>.GetBlock(_categoryID);
            _itemDB = ItemDataBlock.GetBlock(_gearCategoryDB.BaseItem);
            bool changedLanguage = DeserializeLanguageJson(language);
            SetSleepersDatas(changedLanguage);
        }

        private bool DeserializeLanguageJson(LanguageEnum language)
        {
            if (_language == language && _languageDatas != null) return false;

            var languageStrings = JsonSerializer.Deserialize<LanguageDatasClass>(LocalizedString.JsonString)!;
            _languageDatas = language.Equals(LanguageEnum.English) ? languageStrings.english : languageStrings.chinese;
            _language = language;
            return true;
        }

        private void SetSleepersDatas(bool force)
        {
            if (_lastSleepersDatas == WeaponStatShowerPlugin.SleepersShown && !force) return;
            _lastSleepersDatas = WeaponStatShowerPlugin.SleepersShown;

            var activatedSleepers = _lastSleepersDatas.Split(',');
            if (activatedSleepers[0].Trim().Length == 0)
            {
                WeaponStatShowerPlugin.LogWarning("Empty String in the config file, applying Default values");
                activatedSleepers = new string[] { "ALL" };
            }
            _sleepersDatas = new SleepersDatas(activatedSleepers, _languageDatas.sleepers);
        }

        public string DescriptionFormatter()
        {
            if (_itemDB.inventorySlot == InventorySlot.GearMelee)
            {
                var meleeArchetypeDB = MeleeArchetypeDataBlock.GetBlock(GearBuilder.GetMeleeArchetypeID(_gearCategoryDB));
                return VerboseDescriptionFormatter(meleeArchetypeDB) + GetFormatedWeaponStats(meleeArchetypeDB, _itemDB);
            }

            var (archetypeDB, isSentry) = GetRangedArchetype();
            if (archetypeDB == null) return string.Empty;

            bool isShotgun;
            if (isSentry)
                isShotgun = (eWeaponFireMode)_idRange.GetCompID(eGearComponent.FireMode) == eWeaponFireMode.SentryGunShotgunSemi;
            else
                isShotgun = _itemDB.FirstPersonPrefabs != null && _itemDB.FirstPersonPrefabs.Contains(SHOTGUN_PREFAB);

            return VerboseDescriptionFormatter(archetypeDB, isSentry) + GetFormatedWeaponStats(archetypeDB, isShotgun, isSentry);
        }

        internal string FireRateFormatter(string gearPublicName)
        {
            if (_itemDB.inventorySlot == InventorySlot.GearMelee)
            {
                var meleeArchetypeDB = MeleeArchetypeDataBlock.GetBlock(GearBuilder.GetMeleeArchetypeID(_gearCategoryDB));
                var meleeLanguage = _languageDatas.melee;
                return meleeArchetypeDB.persistentID switch
                {
                    1 => meleeLanguage.hammer,
                    2 => meleeLanguage.knife,
                    3 => meleeLanguage.spear,
                    4 => meleeLanguage.bat,
                    _ => gearPublicName,
                };
            }

            var (archetypeDB, _) = GetRangedArchetype();
            if (archetypeDB == null) return gearPublicName; // non-weapon tools: BIO_TRACKER, MINE_DEPLOYER, etc.

            return VerbosePublicNameFireMode(archetypeDB);
        }

        private (ArchetypeDataBlock?, bool) GetRangedArchetype()
        {
            bool isSentry = _categoryID == 12; // PersistentID for Sentry Gun
            return (ArchetypeUtil.GetMappedArchetypeDataBlock(_idRange, _categoryID, _gearCategoryDB), isSentry);
        }

        private string VerbosePublicNameFireMode(ArchetypeDataBlock archetypeDB)
        {
            var firemodeLanguage = _languageDatas.firemode;
            var fireMode = archetypeDB.FireMode;
            _strBuilder.Clear();

            switch (fireMode)
            {
                case eWeaponFireMode.Auto:
                case eWeaponFireMode.SentryGunAuto:
                    _strBuilder.Append($"{firemodeLanguage.fullA} (<#12FF50>{_languageDatas.rateOfFire} {GetRateOfFire(archetypeDB, fireMode)}{CLOSE_COLOR_TAG})");
                    break;

                case eWeaponFireMode.Semi:
                case eWeaponFireMode.SentryGunSemi:
                    _strBuilder.Append($"{firemodeLanguage.semiA} (<#12FF50>{_languageDatas.rateOfFire} {GetRateOfFire(archetypeDB, fireMode)}{CLOSE_COLOR_TAG})");
                    break;

                case eWeaponFireMode.Burst:
                case eWeaponFireMode.SentryGunBurst:
                    if (archetypeDB.BurstShotCount != 1)
                    {
                        _strBuilder.Append($"{firemodeLanguage.burst} (<#704dfa>#{archetypeDB.BurstShotCount}{CLOSE_COLOR_TAG}{DIVIDER}<#12FF50>{_languageDatas.rateOfFire} {GetRateOfFire(archetypeDB, fireMode)}{CLOSE_COLOR_TAG})");
                    }
                    else
                    {
                        _strBuilder.Append($"{firemodeLanguage.burst} (<#12FF50>{_languageDatas.rateOfFire} {GetRateOfFire(archetypeDB, eWeaponFireMode.Semi)}{CLOSE_COLOR_TAG})");
                    }
                    break;

                case eWeaponFireMode.SemiBurst:
                    _strBuilder.Append($"S-Burst (<#12FF50>#{archetypeDB.BurstShotCount} every {archetypeDB.SpecialSemiBurstCountTimeout}\'{CLOSE_COLOR_TAG})");
                    break;

                case eWeaponFireMode.SentryGunShotgunSemi:
                    _strBuilder.Append($"{firemodeLanguage.shotgunSentry} (<#12FF50>{_languageDatas.rateOfFire} {GetRateOfFire(archetypeDB, fireMode)}{CLOSE_COLOR_TAG})");
                    break;

                default:
                    WeaponStatShowerPlugin.LogError("FireMode not found");
                    break;
            }

            return _strBuilder.ToString();
        }

        private string VerboseDescriptionFormatter(ArchetypeDataBlock archetypeDB, bool isSentry)
        {
            _strBuilder.Clear();

            float chargeupTime = archetypeDB.SpecialChargetupTime;
            if (chargeupTime > 0)
            {
                var label = chargeupTime > 0.4 ? _languageDatas.longChargeUp : _languageDatas.shortChargeUp;
                _strBuilder.AppendLine($"{label} ({FormatFloat(chargeupTime, 2)})");
            }

            return _strBuilder.Length == 0 ? string.Empty : _strBuilder.ToString() + "\n";
        }

        private string VerboseDescriptionFormatter(MeleeArchetypeDataBlock meleeArchetypeDB)
        {
            _strBuilder.Clear();

            if (meleeArchetypeDB.CameraDamageRayLength < 1.76)
            {
                _strBuilder.AppendLine(_languageDatas.melee.shortRange);
            }
            else if (meleeArchetypeDB.CameraDamageRayLength < 2.5)
            {
                _strBuilder.AppendLine(_languageDatas.melee.mediumRange);
            }
            else
            {
                _strBuilder.AppendLine(_languageDatas.melee.longRange);
            }

            _strBuilder.Append(meleeArchetypeDB.CanHitMultipleEnemies ? _languageDatas.melee.canPierce + "\n" : string.Empty);
            return _strBuilder.Length == 0 ? string.Empty : _strBuilder.ToString();
        }

        private string GetFormatedWeaponStats(ArchetypeDataBlock archetypeDB, bool isShotgun, bool isSentry = false)
        {
            if (archetypeDB == null) return string.Empty;

            int count = 0;
            int dividerThreshold = _language != LanguageEnum.English ? 3 : 4;
            _strBuilder.Clear();

            void Divider()
            {
                if (count >= dividerThreshold)
                {
                    _strBuilder.AppendLine();
                    count = 0;
                }
                else if (count > 0)
                {
                    _strBuilder.Append(DIVIDER);
                }
            }

            string damageValue = FormatFloat(archetypeDB.Damage, 2) + (isShotgun ? $"(x{archetypeDB.ShotgunBulletCount})" : "");
            AppendStat(ref count, "<#9D2929>", _languageDatas.damage, damageValue);
            AppendStatIf(ref count, Divider, !isSentry, "<color=orange>", _languageDatas.clip, archetypeDB.DefaultClipSize.ToString());
            Divider();
            AppendStat(ref count, "<#FFD306>", _languageDatas.maxAmmo, GetTotalAmmo(archetypeDB, _itemDB, isSentry).ToString());
            AppendStatIf(ref count, Divider, !isSentry, "<#C0FF00>", _languageDatas.reload, FormatFloat(archetypeDB.DefaultReloadTime, 2).ToString());
            AppendStatIf(ref count, Divider, archetypeDB.PrecisionDamageMulti != 1f, "<#18A4A9>", _languageDatas.precision, FormatFloat(archetypeDB.PrecisionDamageMulti, 2).ToString());
            Divider();
            AppendStat(ref count, "<#6764de>", _languageDatas.falloff, ((int)archetypeDB.DamageFalloff.x).ToString() + "m");
            AppendStatIf(ref count, Divider, archetypeDB.StaggerDamageMulti != 1f, "<color=green>", _languageDatas.stagger, FormatFloat(archetypeDB.StaggerDamageMulti, 2).ToString());
            AppendStatIf(ref count, Divider, archetypeDB.HipFireSpread != 0f && !isShotgun, "<#cc9347>", _languageDatas.hipSpread, FormatFloat(archetypeDB.HipFireSpread, 2).ToString());
            AppendStatIf(ref count, Divider, archetypeDB.AimSpread != 0f && !isShotgun, "<#e6583c>", _languageDatas.aimDSpread, FormatFloat(archetypeDB.AimSpread, 2).ToString());
            AppendStatIf(ref count, Divider, archetypeDB.ShotgunBulletSpread + archetypeDB.ShotgunConeSize != 0f && isShotgun, "<#e6583c>", _languageDatas.spread, FormatFloat(archetypeDB.ShotgunBulletSpread + archetypeDB.ShotgunConeSize, 2).ToString());
            AppendStatIf(ref count, Divider, archetypeDB.PiercingBullets, "<#097345>", _languageDatas.pierceCount, archetypeDB.PiercingDamageCountLimit.ToString());
            _strBuilder.AppendLine();
            
            _strBuilder.Append(_sleepersDatas.VerboseKill(archetypeDB));
            return _strBuilder.ToString();
        }

        private string GetFormatedWeaponStats(MeleeArchetypeDataBlock meleeArchetypeDB, ItemDataBlock itemDB)
        {
            if (meleeArchetypeDB == null) return string.Empty;
            int count = 0;
            var meleeLanguage = _languageDatas.melee;
            _strBuilder.Clear();

            void Divider()
            {
                if (count >= 3)
                {
                    _strBuilder.AppendLine();
                    count = 0;
                }
                else if (count > 0)
                {
                    _strBuilder.Append(DIVIDER);
                }
            }

            _strBuilder.AppendLine();
            AppendStat(ref count, "<#9D2929>", $"{_languageDatas.damage}.{meleeLanguage.light}", meleeArchetypeDB.LightAttackDamage.ToString());
            Divider();
            AppendStat(ref count, "<color=orange>", $"{_languageDatas.damage}.{meleeLanguage.heavy}", meleeArchetypeDB.ChargedAttackDamage.ToString());
            AppendStatIf(ref count, Divider, !meleeArchetypeDB.AllowRunningWhenCharging, "<#FFD306>", meleeLanguage.canRunWhileCharging);
            AppendStatIf(ref count, Divider, meleeArchetypeDB.LightStaggerMulti != 1f, "<#C0FF00>", $"{_languageDatas.stagger}.{meleeLanguage.light}", meleeArchetypeDB.LightStaggerMulti.ToString());
            AppendStatIf(ref count, Divider, meleeArchetypeDB.ChargedStaggerMulti != 1f, "<color=green>", $"{_languageDatas.stagger}.{meleeLanguage.heavy}", meleeArchetypeDB.ChargedStaggerMulti.ToString());
            AppendStatIf(ref count, Divider, meleeArchetypeDB.LightPrecisionMulti != 1f, "<#004E2C>", $"{_languageDatas.precision}.{meleeLanguage.light}", meleeArchetypeDB.LightPrecisionMulti.ToString());
            AppendStatIf(ref count, Divider, meleeArchetypeDB.ChargedPrecisionMulti != 1f, "<#55022B>", $"{_languageDatas.precision}.{meleeLanguage.heavy}", meleeArchetypeDB.ChargedPrecisionMulti.ToString());
            AppendStatIf(ref count, Divider, meleeArchetypeDB.LightSleeperMulti != 1f, "<#A918A7>", $"{meleeLanguage.sleepingEnemiesMultiplier}.{meleeLanguage.light}", meleeArchetypeDB.LightSleeperMulti.ToString());
            AppendStatIf(ref count, Divider, meleeArchetypeDB.ChargedSleeperMulti != 1f, "<#025531>", $"{meleeLanguage.sleepingEnemiesMultiplier}.{meleeLanguage.heavy}", meleeArchetypeDB.ChargedSleeperMulti.ToString());
            AppendStatIf(ref count, Divider, meleeArchetypeDB.LightBackstabberMulti != 1f, "<#18A4A9>", $"{meleeLanguage.backstabMultiplier}.{meleeLanguage.light}", meleeArchetypeDB.LightBackstabberMulti.ToString());
            AppendStatIf(ref count, Divider, meleeArchetypeDB.ChargedBackstabberMulti != 1f, "<#75A2AA>", $"{meleeLanguage.backstabMultiplier}.{meleeLanguage.heavy}", meleeArchetypeDB.ChargedBackstabberMulti.ToString());
            AppendStatIf(ref count, Divider, meleeArchetypeDB.LightEnvironmentMulti != 1f, "<#18A4A9>", $"{meleeLanguage.environmentMultiplier}.{meleeLanguage.light}", meleeArchetypeDB.LightEnvironmentMulti.ToString());
            AppendStatIf(ref count, Divider, meleeArchetypeDB.ChargedEnvironmentMulti != 1f, "<#75A2AA>", $"{meleeLanguage.environmentMultiplier}.{meleeLanguage.heavy}", meleeArchetypeDB.ChargedEnvironmentMulti.ToString());
            _strBuilder.AppendLine();
            
            _strBuilder.Append(_sleepersDatas.VerboseKill(meleeArchetypeDB) ?? string.Empty);
            return _strBuilder.ToString();
        }

        private void AppendStatIf(ref int count, Action divider, bool condition, string color, string label, string value = "")
        {
            if (condition)
            {
                divider();
                AppendStat(ref count, color, label, value);
            }
        }

        private void AppendStat(ref int count, string color, string label, string value = "")
        {
            _strBuilder.Append(color);
            _strBuilder.Append(label);
            if (!string.IsNullOrEmpty(value))
            {
                _strBuilder.Append(' ');
                _strBuilder.Append(value);
            }
            _strBuilder.Append(CLOSE_COLOR_TAG);
            count++;
        }

        private static float FormatFloat(float value, int v)
        {
            return (float)Math.Round((decimal)value, v);
        }

        private int GetAmmoMax(ItemDataBlock itemDataBlock)
        {
            var ammoType = PlayerAmmoStorage.GetAmmoTypeFromSlot(itemDataBlock.inventorySlot);
            return ammoType switch
            {
                AmmoType.Standard => _playerDB.AmmoStandardMaxCap,
                AmmoType.Special => _playerDB.AmmoSpecialMaxCap,
                AmmoType.Class => _playerDB.AmmoClassMaxCap,
                AmmoType.CurrentConsumable => itemDataBlock.ConsumableAmmoMax,
                _ => -1,
            };
        }

        private int GetTotalAmmo(ArchetypeDataBlock archetypeDB, ItemDataBlock itemDB, bool isSentry = false)
        {
            int max = GetAmmoMax(itemDB);
            float costOfBullet = archetypeDB.CostOfBullet;

            if (isSentry)
            {
                costOfBullet = costOfBullet * itemDB.ClassAmmoCostFactor;
                if (archetypeDB.ShotgunBulletCount > 0f)
                    costOfBullet *= archetypeDB.ShotgunBulletCount;
            }

            int maxBullets = (int)(max / costOfBullet);
            return isSentry ? maxBullets : maxBullets + archetypeDB.DefaultClipSize;
        }

        private string GetRateOfFire(ArchetypeDataBlock archetypeDB, eWeaponFireMode fireMode)
        {
            float value = -1f;
            switch (fireMode)
            {
                case eWeaponFireMode.Auto:
                case eWeaponFireMode.SentryGunAuto:
                    value = 1 / archetypeDB.ShotDelay;
                    break;

                case eWeaponFireMode.Semi:
                case eWeaponFireMode.SentryGunShotgunSemi:
                    value = 1 / (archetypeDB.ShotDelay + archetypeDB.SpecialChargetupTime);
                    break;

                case eWeaponFireMode.Burst:
                case eWeaponFireMode.SentryGunBurst:
                    float shotsPerSecondSB = 1 / (archetypeDB.BurstDelay + archetypeDB.SpecialChargetupTime + (archetypeDB.ShotDelay * (archetypeDB.BurstShotCount - 1)));
                    value = shotsPerSecondSB * archetypeDB.BurstShotCount;
                    break;
            }
            return FormatFloat(value, 1).ToString();
        }
    }
}