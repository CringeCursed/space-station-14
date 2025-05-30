using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Weapons.Components
{
    [RegisterComponent]
    public sealed partial class BindableWeaponComponent : Component
    {
        /// <summary>
        /// The entity that this weapon is currently bound to.
        /// </summary>
        [DataField]
        public EntityUid BoundUser = EntityUid.Invalid;
    }
}
