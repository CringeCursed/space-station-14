namespace Content.Server.Emp;

/// <summary>
/// Component that triggers an EMP pulse when used as a melee weapon to hit entities
/// </summary>
[RegisterComponent]
[Access(typeof(EmpSystem))]
public sealed partial class EmpOnMeleeHitComponent : Component
{
    /// <summary>
    /// The range of the EMP pulse
    /// </summary>
    [DataField("range"), ViewVariables(VVAccess.ReadWrite)]
    public float Range = 3.0f;

    /// <summary>
    /// How much energy will be consumed
    /// </summary>
    [DataField("energyConsumption"), ViewVariables(VVAccess.ReadWrite)]
    public float EnergyConsumption = 10000f;
    /// <summary>
    /// How long it disables targets
    /// </summary>
    [DataField("disableDuration"), ViewVariables(VVAccess.ReadWrite)]
    public float DisableDuration = 10f;

    /// <summary>
    /// Cooldown duration between EMP effects
    /// </summary>
    [DataField("cooldown"), ViewVariables(VVAccess.ReadWrite)]
    public float Cooldown = 15f;

    /// <summary>
    /// The time when the EMP effect will be available again
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextEmpTime = TimeSpan.Zero;
}
