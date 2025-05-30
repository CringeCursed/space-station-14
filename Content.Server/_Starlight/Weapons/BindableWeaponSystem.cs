using Content.Shared._Starlight.Weapons.Actions.Events;
using Content.Shared._Starlight.Weapons.Components;
using Content.Shared.Item;
using Content.Shared.Actions;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Log;
using Content.Shared.Popups;
using Content.Server.Popups;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Projectiles;

namespace Content.Server._Starlight.Weapons
{

    public sealed class BindableWeaponSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly SharedHandsSystem _hands = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly SharedProjectileSystem _projectile = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BindableWeaponComponent, BindWeaponActionEvent>(OnBindWeapon);
            SubscribeLocalEvent<ActionsComponent, CallWeaponActionEvent>(OnCallWeapon);
        }

        private void OnBindWeapon(EntityUid uid, BindableWeaponComponent component, BindWeaponActionEvent args)
        {
            if (!TryUseWeaponBind(uid, args))
                return;
    
            var performer = args.Performer;
            if (performer == EntityUid.Invalid)
                return;

            component.BoundUser = performer;
            _popup.PopupEntity(Loc.GetString("bindable-weapon-popup-bound"), performer, performer);

            if (TryComp<ActionsComponent>(performer, out var actions))
            {
                EntityUid? callAction = null;
                _actions.AddAction(performer, ref callAction, "ActionCallWeapon", performer, actions);
            }
        }

        private void OnCallWeapon(EntityUid performer, ActionsComponent component, CallWeaponActionEvent args)
        {
            if (!TryUseWeaponRecall(performer, args))
                return;

            var query = EntityQueryEnumerator<BindableWeaponComponent>();
            while (query.MoveNext(out var weaponUid, out var bindable))
            {
                if (bindable.BoundUser == performer)
                {
                    if (TryComp<EmbeddableProjectileComponent>(weaponUid, out var embeddable) && embeddable.EmbeddedIntoUid != null)
                    {
                        _projectile.EmbedDetach(weaponUid, embeddable, performer);
                    }
                    if (TryComp<HandsComponent>(performer, out var hands))
                    {
                        _hands.TryForcePickupAnyHand(performer, weaponUid, handsComp: hands);
                        _popup.PopupEntity(Loc.GetString("bindable-weapon-popup-recalled"), performer, performer);
                    }
                    return;
                }
            }

            _popup.PopupEntity(Loc.GetString("bindable-weapon-popup-notfound"), performer, performer);

            // Remove the action after recall failure
            if (TryComp<ActionsComponent>(performer, out var actions))
            {
                foreach (var action in actions.Actions)
                {
                    if (EntityManager.MetaQuery.TryGetComponent(action, out var meta) && meta.EntityPrototype != null && meta.EntityPrototype.ID == "ActionCallWeapon")
                    {
                        _actions.RemoveAction(performer, action, actions);
                        break;
                    }
                }
            }
        }

        private bool TryUseWeaponRecall(EntityUid uid, CallWeaponActionEvent action)
        {
            if (action.Handled)
                return false;

            action.Handled = true;

            return true;
        }
        private bool TryUseWeaponBind(EntityUid uid, BindWeaponActionEvent action)
        {
            if (action.Handled)
                return false;

            action.Handled = true;

            return true;
        }
    }
}
