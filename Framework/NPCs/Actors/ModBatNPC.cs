namespace FloodMod.Framework.NPCs.Actors;

public abstract class ModBatNPC : ModNPC
{
    protected Player Target => Main.player[NPC.target];

    /// <summary>
    ///     Gets the movement speed of the NPC, in pixels per frame.
    /// </summary>
    public float Speed { get; set; } = 2f;

    public override void SetDefaults()
    {
        base.SetDefaults();

        NPC.noGravity = true;
    }

    public override void AI()
    {
        base.AI();
        
        UpdateTarget();
        UpdateMovement();
    }

    protected virtual void UpdateTarget()
    {
        if (NPC.HasValidTarget)
        {
            return;
        }
        
        NPC.TargetClosest();
    }

    protected virtual void UpdateMovement()
    {
        var direction = NPC.DirectionTo(Target.Center);
        var velocity = direction * Speed;
        
        
    }
}