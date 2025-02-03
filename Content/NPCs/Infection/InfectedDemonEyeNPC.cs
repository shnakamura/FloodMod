namespace FloodMod.Content.NPCs;

public class InfectedDemonEyeNPC : ModNPC
{
    public override void SetDefaults()
    {
        base.SetDefaults();

        NPC.width = 30;
        NPC.height = 30;

        AIType = NPCID.DemonEye;
        
        NPC.aiStyle = NPCAIStyleID.DemonEye;
    }
}