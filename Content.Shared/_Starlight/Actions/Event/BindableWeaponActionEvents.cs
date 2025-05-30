using Robust.Shared.GameObjects;
using Content.Shared.Actions;

namespace Content.Shared._Starlight.Weapons.Actions.Events
{
    /// <summary>
    /// Action event for binding a weapon to the user
    /// </summary>
    public sealed partial class BindWeaponActionEvent : InstantActionEvent { }

    /// <summary>
    /// Action event for recalling a bound weapon to the user's hand
    /// </summary>
    public sealed partial class CallWeaponActionEvent : InstantActionEvent { }
}
