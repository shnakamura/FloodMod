using FloodMod.Common.NPCs;
using FloodMod.Core.NPCs;
using ReLogic.Content;
using Terraria.GameContent;

namespace FloodMod.Content.NPCs.Infection;

public class InfectedCrimeraNPC : ModNPC
{
    public const float FRAME_RATE = 15f;

    public const float STATE_CHASE = 0f;

    public const float STATE_SHOOT = 1f;

    public const float STATE_DASH = 2f;
    
    public static Asset<Texture2D> OutlineTexture { get; private set; }

    private ref float State => ref NPC.ai[0];

    private ref float Timer => ref NPC.ai[1];
    
    private ref float Counter => ref NPC.ai[2];

    private Player Player => Main.player[NPC.target];
    
    private float trailOpacity;
    private float outlineOpacity;
    
    private Vector2 scale = Vector2.One;

    public override void Load()
    {
        base.Load();

        if (Main.dedServ)
        {
            return;
        }

        OutlineTexture = ModContent.Request<Texture2D>(Texture + "_Outline");
    }

    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();

        Main.npcFrameCount[Type] = 2;

        NPCID.Sets.TrailingMode[Type] = 1;
        NPCID.Sets.TrailCacheLength[Type] = 20;
    }

    public override void SetDefaults()
    {
        base.SetDefaults();

        NPC.noGravity = true;

        NPC.lifeMax = 60;
        NPC.damage = 30;
        NPC.defense = 10;

        NPC.width = 32;
        NPC.height = 32;

        NPC.knockBackResist = 0.1f;
        
        AIType = -1;

        NPC.aiStyle = -1;
        
        NPC.HitSound = SoundID.NPCHit1;
        NPC.DeathSound = SoundID.NPCDeath1;
    }

    public override void FindFrame(int frameHeight)
    {
        base.FindFrame(frameHeight);

        NPC.frameCounter++;

        if (NPC.frameCounter < FRAME_RATE)
        {
            return;
        }

        var count = Main.npcFrameCount[Type];

        if (NPC.frame.Y >= count)
        {
            NPC.frame.Y = 0;
        }
        else
        {
            NPC.frame.Y += frameHeight;
        }

        NPC.frameCounter = 0f;
    }

    public override void AI()
    {
        base.AI();

        NPC.TargetClosest(false);

        if (!NPC.HasValidTarget)
        {
            return;
        }

        switch (State)
        {
            case STATE_CHASE:
                UpdateChaseState();
                break;
            case STATE_SHOOT:
                UpdateShootState();
                break;
            case STATE_DASH:
                UpdateDashState();
                break;
        }

        var factor = MathHelper.Clamp(NPC.velocity.Length() / 16f, 0f, State == STATE_DASH ? 0.2f : 0.1f); 

        scale = Vector2.SmoothStep(scale, Vector2.One + new Vector2(-factor, factor), 0.25f);
    }

    private void UpdateChaseState()
    {
        outlineOpacity = MathHelper.SmoothStep(outlineOpacity, 0f, 0.2f);
        
        var rotation = NPC.AngleTo(Player.Center) + MathHelper.PiOver2;

        NPC.rotation = NPC.rotation.AngleLerp(rotation, 0.1f);
        
        var noise = MathF.Sin(Main.GameUpdateCount * 0.1f) * 0.5f;
        var direction = NPC.DirectionTo(Player.Center).RotatedBy(noise);
        
        var velocity = direction * 0.1f;

        NPC.velocity.X += velocity.X;
        NPC.velocity.Y += velocity.Y;

        NPC.velocity = Vector2.Clamp(NPC.velocity, new Vector2(-4f), new Vector2(4f));

        Timer++;

        if (Timer < 5f * 60f)
        {
            return;
        }
        
        var distance = 16f * 16f;

        if (NPC.DistanceSQ(Player.Center) > distance * distance)
        {
            return;
        }

        State = Main.rand.NextBool() ? STATE_SHOOT : STATE_DASH;

        Timer = 0f;

        NPC.netUpdate = true;
    }

    private float fadeTimer;
    
    private void UpdateShootState()
    {
        var rotation = NPC.AngleTo(Player.Center) + MathHelper.PiOver2;

        NPC.rotation = NPC.rotation.AngleLerp(rotation, 0.1f);
        
        var distance = 16f * 16f;

        if (NPC.DistanceSQ(Player.Center) > distance * distance)
        {
            var noise = MathF.Sin(Main.GameUpdateCount * 0.1f) * 0.5f;
            var direction = NPC.DirectionTo(Player.Center).RotatedBy(noise);

            var velocity = direction * 4f;
        
            NPC.velocity = Vector2.SmoothStep(NPC.velocity, velocity, 0.25f);
        }
        else
        {
            NPC.velocity *= 0.9f;
            
            Timer++;
            
            if (fadeTimer < 30f) 
            {
                fadeTimer++;
                
                outlineOpacity = MathHelper.SmoothStep(1f, 0f, fadeTimer / 30f);
            }
            else
            {
                outlineOpacity = MathHelper.SmoothStep(0f, 1f, Timer / 60f);
            }

            if (Timer <= 60f)
            {
                return;
            }

            Projectile.NewProjectile
            (
                NPC.GetSource_FromAI(""),
                NPC.Center,
                NPC.DirectionTo(Player.Center) * 8f,
                ProjectileID.CursedFlameHostile,
                20,
                0f
            );

            fadeTimer = 0f;
            
            State = STATE_CHASE;

            Timer = 0f;

            NPC.netUpdate = true;
        }
    }

    private Vector2 target;

    private void UpdateDashState()
    {
        outlineOpacity = MathHelper.SmoothStep(outlineOpacity, 1f, 0.2f);

        var rotation = NPC.AngleTo(NPC.Center + NPC.velocity) + MathHelper.PiOver2;

        NPC.rotation = NPC.rotation.AngleLerp(rotation, 0.1f);
        
        Timer++;

        if (Counter >= 3f)
        {
            State = STATE_CHASE;
            
            Timer = 0f;
            
            Counter = 0f;
            
            NPC.netUpdate = true;
        }
        else
        {
            if (Timer == 30f)
            {
                target = Player.Center + Player.velocity;
            }
            if (Timer > 30f & Timer < 60f)
            {
                var noise = MathF.Sin(Main.GameUpdateCount * 0.1f) * 0.1f;
                var direction = NPC.DirectionTo(target).RotatedBy(noise);

                var velocity = direction * 0.4f;

                NPC.velocity += velocity;
            }
            
            if (Timer > 60f)
            {
                NPC.velocity *= 0.95f;
                
                NPC.netUpdate = true;
            }
            
            if (Timer > 90f)
            {
                Timer = 0f;

                Counter++;

                NPC.netUpdate = true;
            }
        }
    }
    
    public override void HitEffect(NPC.HitInfo hit)
    {
        base.HitEffect(hit);

        // TODO: Hit effects
        
        if (NPC.life > 0)
        {
            return;
        }
        
        // TODO: Death effects
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        var texture = TextureAssets.Npc[NPC.type].Value;
        
        var origin = NPC.frame.Size() / 2f;
        
        var offset = new Vector2(0f, NPC.gfxOffY + NPC.ModNPC?.DrawOffsetY ?? 0f);
        
        var effects = NPC.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        
        trailOpacity = MathHelper.SmoothStep(trailOpacity, State == STATE_DASH ? 1f : 0f, 0.1f);
        
        var length = NPCID.Sets.TrailCacheLength[Type];

        for (var i = 0; i < length; i += 4)
        {
            var multiplier = 1f - i / (float)length;
            
            var trailPosition = NPC.oldPos[i] + NPC.Size / 2f - Main.screenPosition + offset;
            var trailColor = NPC.GetAlpha(drawColor) * multiplier * trailOpacity;
            
            Main.EntitySpriteDraw
            (
                OutlineTexture.Value,
                trailPosition,
                NPC.frame,
                NPC.GetAlpha(new Color(154, 68, 64)) * outlineOpacity * trailOpacity * multiplier,
                NPC.rotation,
                origin,
                scale * NPC.scale,
                effects
            );
            
            Main.EntitySpriteDraw
            (
                texture,
                trailPosition,
                NPC.frame,
                trailColor,
                NPC.rotation,
                origin,
                scale * NPC.scale,
                effects
            );
        }
        
        var position = NPC.Center - Main.screenPosition + offset;

        var color = NPC.GetAlpha(drawColor);
        
        Main.EntitySpriteDraw
        (
            OutlineTexture.Value,
            position,
            NPC.frame,
            NPC.GetAlpha(new Color(154, 68, 64)) * outlineOpacity,
            NPC.rotation,
            origin,
            scale * NPC.scale,
            effects
        );

        Main.EntitySpriteDraw
        (
            texture,
            position,
            NPC.frame,
            color,
            NPC.rotation,
            origin,
            scale * NPC.scale,
            effects
        );

        return false;
    }
}