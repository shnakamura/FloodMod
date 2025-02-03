namespace FloodMod.Common.NPCs;

public sealed class InfectionGlobalNPC : GlobalNPC
{
    public bool Infected { get; set; }

    public override bool InstancePerEntity { get; } = true;
}