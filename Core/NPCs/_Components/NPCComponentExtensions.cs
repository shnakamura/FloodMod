namespace FloodMod.Core.NPCs;

/// <summary>
///     Provides <see cref="NPC"/> extension methods regarding <see cref="NPCComponent"/>.
/// </summary>
/// <remarks>
///     Kept outside of the <c>Utilities.Extensions</c> namespace for convenience.
/// </remarks>
public static class NPCComponentExtensions
{
    public static T Enable<T>(this NPC npc) where T : NPCComponent
    {
        if (!npc.TryGetGlobalNPC(out T component))
        {
            throw new InvalidNPCComponentException($"Component of type {typeof(T).FullName} could not be enabled.");
        }

        component!.Enabled = true;

        return component;
    }
}