using CellMenu;
using WeaponStatShower.ExtraDescription;

namespace WeaponStatShower.Patches
{
    internal class DescriptionToggle : Patch
    {
        public override string Name => PatchName;
        
        public static Patch Instance { get; private set; } = null!;

        private const string PatchName = nameof(DescriptionToggle);
        private static DescriptionButtonHandler _button = null!;

        public override void Initialize()
        {
            Instance = this;
        }

        public override void Execute()
        {
            PatchMethod<CM_PlayerLobbyBar>(nameof(CM_PlayerLobbyBar.ShowWeaponSelectionPopup), PatchType.Postfix);
            PatchMethod<CM_PlayerLobbyBar>(nameof(CM_PlayerLobbyBar.OnWeaponSlotItemSelected), PatchType.Postfix);
        }

        public static void CM_PlayerLobbyBar__ShowWeaponSelectionPopup__Postfix(CM_PlayerLobbyBar __instance)
        {
            _button = new DescriptionButtonHandler(__instance);

            // Need to manually call this since it's not called in every case we need it to be
            if (__instance.selectedWeaponSlotItem != null)
                CM_PlayerLobbyBar__OnWeaponSlotItemSelected__Postfix(__instance.selectedWeaponSlotItem);
        }

        public static void CM_PlayerLobbyBar__OnWeaponSlotItemSelected__Postfix(CM_InventorySlotItem slotItem)
        {
            if (_button == null || slotItem.m_gearID == null) return;

            _button.SetData(slotItem.m_gearID);
        }
    }
}
