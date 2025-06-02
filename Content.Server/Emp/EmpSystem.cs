using Content.Server.Explosion.EntitySystems;
using Content.Server.Power.EntitySystems;
using Content.Server.Radio;
using Content.Server.SurveillanceCamera;
using Content.Shared.Emp;
using Content.Shared.Examine;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Audio.Systems; // 🌟Starlight🌟 start
using Robust.Shared.Player; 
using Robust.Shared.Audio;
using Robust.Shared.Timing; 
using Content.Shared.NPC.Systems;
using Content.Shared.Weapons.Melee.Events; 
using Content.Shared.Popups; // 🌟Starlight🌟 end

namespace Content.Server.Emp;

public sealed class EmpSystem : SharedEmpSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!; // 🌟Starlight🌟
    [Dependency] private readonly SharedAudioSystem _audio = default!; // 🌟Starlight🌟

    public const string EmpPulseEffectPrototype = "EffectEmpPulse";
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmpDisabledComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<EmpOnTriggerComponent, TriggerEvent>(HandleEmpTrigger);
        SubscribeLocalEvent<EmpImmuneComponent, EmpAttemptEvent>(OnEmpAttempt);
        SubscribeLocalEvent<EmpOnMeleeHitComponent, MeleeHitEvent>(OnMeleeHit); // 🌟Starlight🌟

        SubscribeLocalEvent<EmpDisabledComponent, RadioSendAttemptEvent>(OnRadioSendAttempt);
        SubscribeLocalEvent<EmpDisabledComponent, RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);
        SubscribeLocalEvent<EmpDisabledComponent, ApcToggleMainBreakerAttemptEvent>(OnApcToggleMainBreaker);
        SubscribeLocalEvent<EmpDisabledComponent, SurveillanceCameraSetActiveAttemptEvent>(OnCameraSetActive);
    }

    /// <summary>
    ///   Triggers an EMP pulse at the given location, by first raising an <see cref="EmpAttemptEvent"/>, then a raising <see cref="EmpPulseEvent"/> on all entities in range.
    /// </summary>
    /// <param name="coordinates">The location to trigger the EMP pulse at.</param>
    /// <param name="range">The range of the EMP pulse.</param>
    /// <param name="energyConsumption">The amount of energy consumed by the EMP pulse.</param>
    /// <param name="duration">The duration of the EMP effects.</param>
    public void EmpPulse(MapCoordinates coordinates, float range, float energyConsumption, float duration)
    {
        foreach (var uid in _lookup.GetEntitiesInRange(coordinates, range))
        {
            TryEmpEffects(uid, energyConsumption, duration);
        }
        Spawn(EmpPulseEffectPrototype, coordinates);
    }

    /// <summary>
    ///    Attempts to apply the effects of an EMP pulse onto an entity by first raising an <see cref="EmpAttemptEvent"/>, followed by raising a <see cref="EmpPulseEvent"/> on it.
    /// </summary>
    /// <param name="uid">The entity to apply the EMP effects on.</param>
    /// <param name="energyConsumption">The amount of energy consumed by the EMP.</param>
    /// <param name="duration">The duration of the EMP effects.</param>
    public void TryEmpEffects(EntityUid uid, float energyConsumption, float duration)
    {
        var attemptEv = new EmpAttemptEvent();
        RaiseLocalEvent(uid, attemptEv);
        if (attemptEv.Cancelled)
            return;
        
        // 🌟Starlight🌟 Don't apply EMP effects to Clockwork faction members
        // if (_npcFaction.IsMember(uid, "Clockwork"))
        //     return;

        DoEmpEffects(uid, energyConsumption, duration);
    }

    /// <summary>
    ///    Applies the effects of an EMP pulse onto an entity by raising a <see cref="EmpPulseEvent"/> on it.
    /// </summary>
    /// <param name="uid">The entity to apply the EMP effects on.</param>
    /// <param name="energyConsumption">The amount of energy consumed by the EMP.</param>
    /// <param name="duration">The duration of the EMP effects.</param>
    public void DoEmpEffects(EntityUid uid, float energyConsumption, float duration)
    {
        var ev = new EmpPulseEvent(energyConsumption, false, false, TimeSpan.FromSeconds(duration));
        RaiseLocalEvent(uid, ref ev);
        if (ev.Affected)
        {
            Spawn(EmpDisabledEffectPrototype, Transform(uid).Coordinates);
        }
        if (ev.Disabled)
        {
            var disabled = EnsureComp<EmpDisabledComponent>(uid);
            disabled.DisabledUntil = Timing.CurTime + TimeSpan.FromSeconds(duration);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<EmpDisabledComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.DisabledUntil < Timing.CurTime)
            {
                RemComp<EmpDisabledComponent>(uid);
                var ev = new EmpDisabledRemoved();
                RaiseLocalEvent(uid, ref ev);
            }
        }
    }

    private void HandleEmpTrigger(EntityUid uid, EmpOnTriggerComponent comp, TriggerEvent args)
    {
        EmpPulse(_transform.GetMapCoordinates(uid), comp.Range, comp.EnergyConsumption, comp.DisableDuration);
        args.Handled = true;
    }
    
    // 🌟Starlight🌟
    private void OnMeleeHit(EntityUid uid, EmpOnMeleeHitComponent component, MeleeHitEvent args)
    {
        // Check if the cooldown has passed
        var currentTime = Timing.CurTime;
        if (currentTime < component.NextEmpTime)
            return;

        // Set the next available time
        component.NextEmpTime = currentTime + TimeSpan.FromSeconds(component.Cooldown);
        
        var user = args.User;
        
        Timer.Spawn(TimeSpan.FromSeconds(component.Cooldown), () =>
        {
            if (!Exists(uid) || !TryComp<EmpOnMeleeHitComponent>(uid, out _))
                return;

            if (Exists(user))
            {
                _popup.PopupEntity("EMP ability is ready!", uid, user, PopupType.Medium);
            }
            // else
            // {   // little fallback
            //     _popup.PopupEntity("EMP ability is ready!", uid, PopupType.Medium);
            // }
            var soundSpec = new SoundPathSpecifier("/Audio/_Starlight/Effects/emprecharge.ogg");
            _audio.PlayPvs(soundSpec, uid, AudioParams.Default.WithVolume(-5f));
        });

        // Trigger EMP pulse at the first hit entity's location
        if (args.HitEntities.Count > 0)
        {
            var hitEntity = args.HitEntities[0];
            var hitEntityCoords = _transform.GetMapCoordinates(hitEntity);
            EmpPulse(hitEntityCoords, component.Range, component.EnergyConsumption, component.DisableDuration);
        }
    }
    // 🌟Starlight🌟
    
    private void OnExamine(EntityUid uid, EmpDisabledComponent component, ExaminedEvent args) => args.PushMarkup(Loc.GetString("emp-disabled-comp-on-examine"));

    private void OnRadioSendAttempt(EntityUid uid, EmpDisabledComponent component, ref RadioSendAttemptEvent args) => args.Cancelled = true;

    private void OnRadioReceiveAttempt(EntityUid uid, EmpDisabledComponent component, ref RadioReceiveAttemptEvent args) => args.Cancelled = true;

    private void OnApcToggleMainBreaker(EntityUid uid, EmpDisabledComponent component, ref ApcToggleMainBreakerAttemptEvent args) => args.Cancelled = true;

    private void OnCameraSetActive(EntityUid uid, EmpDisabledComponent component, ref SurveillanceCameraSetActiveAttemptEvent args) => args.Cancelled = true;
    
    private void OnEmpAttempt(EntityUid uid, EmpImmuneComponent comp, EmpAttemptEvent args) => args.Cancel();
}

/// <summary>
/// Raised on an entity before <see cref="EmpPulseEvent"/>. Cancel this to prevent the emp event being raised.
/// </summary>
public sealed partial class EmpAttemptEvent : CancellableEntityEventArgs
{
}

[ByRefEvent]
public record struct EmpPulseEvent(float EnergyConsumption, bool Affected, bool Disabled, TimeSpan Duration);

[ByRefEvent]
public record struct EmpDisabledRemoved();
