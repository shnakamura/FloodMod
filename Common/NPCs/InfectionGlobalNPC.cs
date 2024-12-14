namespace FloodMod.Common.NPCs;

public sealed class InfectionGlobalNPC : GlobalNPC
{
    /// <summary>
    ///     Whether the <see cref="NPC"/> attached to this global is infected or not.
    /// </summary>
    public bool Infected { get; set; }

    public override bool InstancePerEntity { get; } = true;
}