using CellMenu;
using GameData;
using Gear;
using TMPro;
using UnityEngine;
using WeaponStatShower.ExtraDescription.Data;
using WeaponStatShower.Utils;
using WeaponStatShower.Utils.Extensions;

namespace WeaponStatShower.ExtraDescription
{
    public class DescriptionButtonHandler
    {
        private static readonly Vector2 Size = new (600, 320);
        private static readonly WeaponDescriptionBuilder DescriptionBuilder = new();

        private readonly GameObject _gameObject;
        private readonly CM_Item _item;
        private readonly TextMeshPro _text;
        private readonly RectTransform _rectTrans;
        private readonly CM_ScrollWindowInfoBox _infoBox;
        private readonly CM_PageLoadout _pageLoadout;

        private LocaleText[] _headers = null!;
        private LocaleText[] _descriptions = null!;
        private int _index = 0;
        private GearIDRange? _lastRange;

        public event Action<LocaleText>? OnDescriptionChanged;

        public DescriptionButtonHandler(CM_PlayerLobbyBar lobbyBar)
        {
            _pageLoadout = lobbyBar.m_parentPage.Cast<CM_PageLoadout>();
            _infoBox = lobbyBar.m_popupScrollWindow.InfoBox;
            // Need to instantiate a new button every time since the window is instantiated every time
            _gameObject = GameObject.Instantiate(CM_PageLoadout.Current.m_copyLobbyIdButton.gameObject, _infoBox.transform);
            _item = _gameObject.GetComponent<CM_Item>();
            _item.m_clickBlink = false;

            var box = _item.transform.GetChild(0);
            _rectTrans = box.GetComponent<RectTransform>();
            box.gameObject.SetActive(false);

            _item.OnBtnPressCallback = null;
            _item.add_OnBtnPressCallback((Action<int>)OnBtnPress);

            // Copy text into child object so we can offset it from the center
            GameObject child = new("PageNumber");
            child.transform.SetParent(_gameObject.transform, false);
            child.transform.localPosition = new Vector3(0, -110, 0);
            child.layer = LayerManager.LAYER_UI;

            var baseColor = new Color(1f, 1f, 1f, 0.2f);
            var text = _item.m_texts[0];
            _text = child.AddComponent<TextMeshPro>();
            _text.font = text.font;
            _text.fontSize = 48;
            _text.color = baseColor;
            _text.alignment = TextAlignmentOptions.Bottom;
            _text.raycastTarget = text.raycastTarget;
            _text.sortingLayerID = text.sortingLayerID;
            _text.enableWordWrapping = false;
            _text.isOrthographic = true;
            _text.rectTransform.sizeDelta = new Vector2(100, 50);
            _item.m_texts[0] = _text;
            GameObject.Destroy(text);

            _item.transform.localPosition = new(-300, -50 - Size.y / 2, -1);
            _item.m_collider.size = Size;

            _item.m_textColorOut[0] = baseColor;
            _item.m_textColorOver[0] = Color.white;
            _item.m_alphaSpriteOnHover = true;
        }

        public void SetData(GearIDRange idRange)
        {
            if (_lastRange == idRange) return;
            _lastRange = idRange;

            var catID = idRange.GetCompID(eGearComponent.Category);
            var catBlock = GearCategoryDataBlock.GetBlock(catID);
            var archBlock = ArchetypeUtil.GetMappedArchetypeDataBlock(idRange, catID, catBlock);
            bool showStats = WeaponStatShowerPlugin.ShowStats;

            bool hasCustom;
            ExtraDescriptionData? customData;
            LocaleText description;
            // Only some items have arch blocks (gun, sentry). Not checking gear cat for arch weapons
            // since any sane person will only use arch for them.
            if (archBlock != null)
            {
                description = new(archBlock.Description);
                hasCustom = DescriptionDataManager.TryGetArchData(archBlock.persistentID, out customData);
            }
            else
            {
                var itemDB = ItemDataBlock.GetBlock(idRange.GetCompID(eGearComponent.BaseItem));
                showStats = showStats && itemDB.inventorySlot == Player.InventorySlot.GearMelee;
                hasCustom = DescriptionDataManager.TryGetGearData(catID, out customData);
                description = new(catBlock.Description);
            }

            if (!showStats && (!hasCustom || customData!.Descriptions.Length == 0))
            {
                _gameObject.SetActive(false);
                return;
            }

            List<LocaleText> headerBuilder = new();
            List<LocaleText> descBuilder = new();
            int descriptionIndex = DescriptionDataManager.Current.GlobalSettings.DefaultDescriptionIndex;
            if (hasCustom)
            {
                if (customData!.DescriptionIndexOverride >= 0)
                    descriptionIndex = customData.DescriptionIndexOverride;
                descBuilder.AddRange(customData.Descriptions);
                headerBuilder.AddRange(customData.Headers);
                headerBuilder.ExpandToSize(descBuilder.Count, LocaleText.Empty);
            }

            var defaultHeader = LocaleText.Empty;
            var defaultDesc = new LocaleText(description);
            if (showStats)
            {
                (var header, var desc) = CreateGeneratedStats(idRange);
                switch (WeaponStatShowerPlugin.StatsLocation)
                {
                    case WeaponStatShowerPlugin.StatsPosition.Last:
                        headerBuilder.Add(header);
                        descBuilder.Add(desc);
                        break;
                    case WeaponStatShowerPlugin.StatsPosition.First:
                        headerBuilder.Insert(0, header);
                        descBuilder.Insert(0, desc);
                        // Shift description index up if it wasn't placed after stats page
                        if (descriptionIndex < descBuilder.Count)
                            descriptionIndex++;
                        break;
                    case WeaponStatShowerPlugin.StatsPosition.Combined:
                        defaultHeader = header;
                        defaultDesc = new LocaleText(desc.ToString() + '\n' + defaultDesc.ToString());
                        break;
                }
            }

            descriptionIndex = Math.Min(descriptionIndex, descBuilder.Count);
            // Description index could exceed available count in some cases; default to first tab if so.
            descBuilder.Insert(descriptionIndex, defaultDesc);
            headerBuilder.Insert(descriptionIndex, defaultHeader);

            _descriptions = descBuilder.ToArray();
            _headers = headerBuilder.ToArray();

            // Fill blank headers elements with the normal gear name
            LocaleText normalHeader = new(idRange.PublicGearName);
            for (int i = 0; i < _headers.Length; i++)
                if (_headers[i] == LocaleText.Empty)
                    _headers[i] = normalHeader;

            _index = 0;
            if (descriptionIndex != 0 || WeaponStatShowerPlugin.StatsLocation == WeaponStatShowerPlugin.StatsPosition.Combined)
            {
                _infoBox.m_infoMainTitleText.SetText(_headers[0]);
                _infoBox.m_infoDescriptionText.SetText(_descriptions[0]);
            }

            // May happen if combined
            if (_descriptions.Length == 1)
            {
                _gameObject.SetActive(false);
                return;
            }

            _gameObject.SetActive(true);
            _text.SetText($"<< 1 >>");
        }

        private void OnBtnPress(int _)
        {
            // I have no idea what CursorPosition is relative to, so I just use World here.
            var screenPoint = RectTransformUtility.WorldToScreenPoint(CM_Camera.Current.Camera, _pageLoadout.CursorWorldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTrans, screenPoint, CM_Camera.Current.Camera, out var localPoint);

            if (localPoint.x > 0)
                _index = (_index + 1) % _descriptions.Length;
            else if (_index > 0)
                _index--;
            else
                _index = _descriptions.Length - 1;

            _infoBox.m_infoMainTitleText.SetText(_headers[_index]);
            _infoBox.m_infoDescriptionText.SetText(_descriptions[_index]);
            _text.SetText($"<< {_index + 1} >>");
        }

        private static (LocaleText header, LocaleText desc) CreateGeneratedStats(GearIDRange idRange)
        {
            DescriptionBuilder.Inizialize(idRange, PlayerDataBlock.GetBlock(1U), WeaponStatShowerPlugin.ConfigLanguage);

            return (new(DescriptionBuilder.FireRateFormatter(idRange.PublicGearName)), new(DescriptionBuilder.DescriptionFormatter()));
        }
    }
}
