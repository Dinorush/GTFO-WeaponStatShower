using CellMenu;
using GameData;
using Gear;
using UnityEngine;
using WeaponStatShower.ExtraDescription.Data;
using WeaponStatShower.Utils;
using WeaponStatShower.Utils.Extensions;

namespace WeaponStatShower.ExtraDescription
{
    public class DescriptionButtonHandler
    {
        private static readonly Vector2 Size = new (530f, 210f);
        private const float ThickBarBuffer = 7;
        private static readonly WeaponDescriptionBuilder DescriptionBuilder = new();

        private readonly GameObject _gameObject;
        private readonly CM_Item _item;
        private readonly RectTransform _rectTrans;
        private readonly SpriteRenderer[] _renderers;
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
            _item.m_texts[0].SetText(string.Empty);
            _item.m_clickBlink = false;

            var rectTrans = _item.transform.GetChild(0);
            _rectTrans = rectTrans.GetComponent<RectTransform>();
            _renderers = new SpriteRenderer[]
            {
                rectTrans.GetChild(0).GetComponent<SpriteRenderer>(), // Top
                rectTrans.GetChild(1).GetComponent<SpriteRenderer>(), // Left
                rectTrans.GetChild(2).GetComponent<SpriteRenderer>(), // Bottom
                rectTrans.GetChild(3).GetComponent<SpriteRenderer>(), // Right
            };

            _item.OnBtnPressCallback = null;
            _item.add_OnBtnPressCallback((Action<int>)OnBtnPress);

            // Button formatting
            var baseColor = new Color(1f, 1f, 1f, 0.2f);
            foreach (var renderer in _renderers)
                renderer.color = baseColor;
            // Collider is centered at the transform, so need to shift all bars up-left by half the size
            _item.transform.localPosition = new(-295 - ThickBarBuffer, -75 - Size.y / 2, -1);
            _item.m_collider.size = Size;

            _renderers[0].transform.localPosition = new Vector3(ThickBarBuffer - Size.x / 2, Size.y / 2, 0);
            _renderers[0].size = new(Size.x, 1);
            _renderers[1].transform.localPosition = new Vector3(-Size.x / 2, Size.y / 2, 0);
            _renderers[1].size = new(ThickBarBuffer + 1, Size.y);
            _renderers[2].transform.localPosition = new Vector3(ThickBarBuffer - Size.x / 2, -Size.y / 2, 0);
            _renderers[2].size = new(Size.x, 1);
            _renderers[3].transform.localPosition = new Vector3(ThickBarBuffer + Size.x / 2, Size.y / 2, 0);
            _renderers[3].size = new(1, Size.y);

            _item.m_spriteColorOut = baseColor;
            _item.m_alphaSpriteOnHover = true;
            _item.m_hoverSpriteArray = _renderers;
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

            if (showStats)
            {
                (var header, var desc) = CreateGeneratedStats(idRange);
                headerBuilder.Add(header);
                descBuilder.Add(desc);
            }

            // Description index could exceed available count in some cases; default to first tab if so.
            descBuilder.Insert(descriptionIndex <= descBuilder.Count ? descriptionIndex : 0, new(description));
            headerBuilder.Insert(descriptionIndex <= descBuilder.Count ? descriptionIndex : 0, LocaleText.Empty);

            _descriptions = descBuilder.ToArray();
            _headers = headerBuilder.ToArray();

            // Fill blank headers elements with the normal gear name
            LocaleText defaultHeader = new(idRange.PublicGearName);
            for (int i = 0; i < _headers.Length; i++)
                if (_headers[i] == LocaleText.Empty)
                    _headers[i] = defaultHeader;

            _index = 0;
            _gameObject.SetActive(true);
            if (descriptionIndex != 0)
            {
                _infoBox.m_infoMainTitleText.SetText(_headers[0]);
                _infoBox.m_infoDescriptionText.SetText(_descriptions[0]);
            }
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
        }

        private static (LocaleText header, LocaleText desc) CreateGeneratedStats(GearIDRange idRange)
        {
            DescriptionBuilder.Inizialize(idRange, PlayerDataBlock.GetBlock(1U), WeaponStatShowerPlugin.ConfigLanguage);

            return (new(DescriptionBuilder.FireRateFormatter(idRange.PublicGearName)), new(DescriptionBuilder.DescriptionFormatter()));
        }
    }
}
