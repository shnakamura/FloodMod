namespace FloodMod.Content.Tiles;

public class BlightstoneTile : ModTile
{
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();

        Main.tileMergeDirt[Type] = true;
        
        Main.tileSolid[Type] = true;
        Main.tileLighted[Type] = true;
        Main.tileBlockLight[Type] = true;
        
        TileID.Sets.Conversion.Stone[Type] = true;
        
        // TODO: Find a suitable dust.
        MineResist = 1f;
        HitSound = SoundID.Tink;
        
        AddMapEntry(new Color(170, 137, 73));
    }

    public override void NumDust(int i, int j, bool fail, ref int num)
    {
        base.NumDust(i, j, fail, ref num);

        num = fail ? 1 : 3;
    }
}