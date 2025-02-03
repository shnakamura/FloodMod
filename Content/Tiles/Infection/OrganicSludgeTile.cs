namespace FloodMod.Content.Tiles;

public class OrganicSludgeTile : ModTile
{
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();

        Main.tileSolid[Type] = true;
        Main.tileLighted[Type] = true;
        Main.tileBlockLight[Type] = true;

        // TODO: Find a suitable dust.
        MineResist = 1f;
        HitSound = SoundID.NPCHit1;
        
        AddMapEntry(new Color(109, 73, 44));
    }

    public override void NumDust(int i, int j, bool fail, ref int num)
    {
        base.NumDust(i, j, fail, ref num);

        num = fail ? 1 : 3;
    }
}