using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using FloodMod.Common.NPCs;
using ReLogic.Content;
using Terraria.Audio;
using Terraria.GameContent;

namespace FloodMod.Content.NPCs;

public class InfectionFormNPC : ModNPC
{
    /// <summary>
    /// </summary>
    public const float IDLE_STATE = 0f;

    /// <summary>
    ///     The maximum movement speed of this NPC while idling, in pixels per tick.
    /// </summary>
    public const float IDLE_MOVEMENT_SPEED = 3f;

    /// <summary>
    ///     The maximum movement acceleration of this NPC while idling, in pixels per tick.
    /// </summary>
    public const float IDLE_MOVEMENT_ACCELERATION = 0.05f;

    /// <summary>
    /// </summary>
    public const float ATTACK_STATE = 1f;

    /// <summary>
    ///     The minimum attack distance of this NPC, in pixels.
    /// </summary>
    public const float ATTACK_DISTANCE = 40f * 16f;

    /// <summary>
    ///     The maximum movement speed of this NPC while attacking, in pixels per tick.
    /// </summary>
    public const float ATTACK_MOVEMENT_SPEED = 5f;

    /// <summary>
    ///     The maximum movement acceleration of this NPC while attacking, in pixels per tick.
    /// </summary>
    public const float ATTACK_MOVEMENT_ACCELERATION = 0.15f;

    /// <summary>
    /// </summary>
    public const float LATCH_STATE = 2f;

    public static readonly SoundStyle IdleSound = new($"{nameof(FloodMod)}/Assets/Sounds/InfectionFormIdle", 10)
    {
        MaxInstances = 1,
        Volume = 0.8f,
        PitchVariance = 0.15f
    };

    public static readonly SoundStyle DeathSound = new($"{nameof(FloodMod)}/Assets/Sounds/InfectionFormDeath", 3)
    {
        Volume = 0.8f,
        PitchVariance = 0.15f
    };

    public static readonly Asset<Texture2D> ParticleTexture = ModContent.Request<Texture2D>($"{nameof(FloodMod)}/Assets/Textures/Particles/Blood");

    private Vector2 offset;
    private Vector2 scale = Vector2.One;

    /// <summary>
    ///     Whether this NPC is in its idle state or not.
    /// </summary>
    public bool IsIdling => State == IDLE_STATE;

    /// <summary>
    ///     Whether this NPC is in its attack state or not.
    /// </summary>
    public bool IsAttacking => State == ATTACK_STATE;

    /// <summary>
    /// </summary>
    public bool IsLatching => State == LATCH_STATE;

    /// <summary>
    ///     The index of the target <see cref="NPC" /> associated with this NPC.
    /// </summary>
    public int Index { get; private set; }

    private NPC Target => Main.npc[Index];

    private ref float State => ref NPC.ai[0];

    private ref float Timer => ref NPC.ai[1];

    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();

        Main.npcFrameCount[Type] = 12;

        ProjectileID.Sets.TrailingMode[Type] = 2;
        ProjectileID.Sets.TrailCacheLength[Type] = 16;
    }

    public override void SetDefaults()
    {
        base.SetDefaults();

        NPC.noTileCollide = false;
        NPC.noGravity = false;
        NPC.hide = true;

        NPC.knockBackResist = 0.5f;

        NPC.lifeMax = 30;
        NPC.defense = 2;
        NPC.damage = 15;

        NPC.width = 24;
        NPC.height = 24;

        AIType = -1;

        NPC.aiStyle = -1;

        NPC.DeathSound = DeathSound;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        base.SendExtraAI(writer);

        writer.Write(Index);

        writer.WriteVector2(offset);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        base.ReceiveExtraAI(reader);

        Index = reader.Read7BitEncodedInt();

        offset = reader.ReadVector2();
    }

    public override void AI()
    {
        base.AI();

        switch (State)
        {
            case IDLE_STATE:
                UpdateIdle();
                break;
            case ATTACK_STATE:
                UpdateAttack();
                break;
            case LATCH_STATE:
                UpdateLatch();
                break;
        }

        UpdateCollision();
        UpdateScale();
    }

    public override void FindFrame(int frameHeight)
    {
        base.FindFrame(frameHeight);

        NPC.spriteDirection = NPC.direction;

        if (MathF.Floor(NPC.velocity.Y) == 0f)
        {
            var minFrame = IsAttacking || IsLatching ? 5 : 0;
            var maxFrame = IsAttacking || IsLatching ? 10 : 5;

            NPC.frameCounter++;

            if (NPC.frameCounter < 8f)
            {
                return;
            }

            NPC.frame.Y += frameHeight;

            if (NPC.frame.Y >= maxFrame * frameHeight)
            {
                NPC.frame.Y = minFrame * frameHeight;
            }

            NPC.frameCounter = 0f;
        }
        else
        {
            NPC.frame.Y = 11 * frameHeight;
        }
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        var texture = TextureAssets.Npc[Type].Value;

        var origin = new Vector2(NPC.frame.Width / 2f, NPC.frame.Height);
        
        var position = NPC.Center - Main.screenPosition + new Vector2(0f, NPC.gfxOffY + DrawOffsetY + origin.Y / 2f);

        var effects = NPC.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

        if (State == ATTACK_STATE)
        {
            var length = ProjectileID.Sets.TrailCacheLength[Type];

            for (var i = 0; i < 4; i++)
            {
                var progress = 1f - i / (float)length;

                var trailPosition = NPC.oldPos[i] - NPC.Size / 2f - Main.screenPosition + new Vector2(0f, NPC.gfxOffY + DrawOffsetY);

                Main.EntitySpriteDraw
                (
                    texture,
                    trailPosition,
                    NPC.frame,
                    NPC.GetAlpha(drawColor) * length,
                    NPC.oldRot[i],
                    origin,
                    scale,
                    effects
                );
            }
        }

        Main.EntitySpriteDraw
        (
            texture,
            position,
            NPC.frame,
            NPC.GetAlpha(drawColor),
            NPC.rotation,
            origin,
            scale,
            effects
        );

        return false;
    }

    public override void DrawBehind(int index)
    {
        base.DrawBehind(index);

        Main.instance.DrawCacheNPCsOverPlayers.Add(index);
    }

    private void MoveTowards(float position)
    {
        NPC.direction = MathF.Sign(position - NPC.Center.X);

        var speed = IsAttacking ? ATTACK_MOVEMENT_SPEED : IDLE_MOVEMENT_SPEED;
        var acceleration = IsAttacking ? ATTACK_MOVEMENT_ACCELERATION : IDLE_MOVEMENT_ACCELERATION;

        NPC.velocity.X += acceleration * NPC.direction;
        NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X, -speed, speed);
    }

    private void UpdateIdle()
    {
        if (TrySearchTarget(out var result))
        {
            Index = result.whoAmI;

            State = ATTACK_STATE;

            NPC.netUpdate = true;
        }
        else
        {
            var target = 0f;

            if (NPC.velocity.Y != 0f)
            {
                target = NPC.velocity.X * 0.1f;
            }

            NPC.rotation = NPC.rotation.AngleLerp(target, 0.2f);
            
            Timer++;
        }
    }

    private void UpdateAttack()
    {
        if (!IsValidTarget(Target))
        {
            State = IDLE_STATE;

            NPC.netUpdate = true;
        }
        else
        {
            NPC.direction = MathF.Sign(NPC.velocity.X);

            var target = 0f;

            if (NPC.velocity.Y != 0f)
            {
                target = NPC.velocity.X * 0.1f;
            }

            NPC.rotation = NPC.rotation.AngleLerp(target, 0.2f);

            MoveTowards(Target.Center.X);

            if (!NPC.Hitbox.Intersects(Target.Hitbox) || !Target.TryGetGlobalNPC(out InfectionGlobalNPC globalNPC))
            {
                return;
            }

            globalNPC.Infected = true;

            offset = (Target.Center - NPC.Center) * 0.75f;

            State = LATCH_STATE;

            NPC.netUpdate = true;
        }
    }

    private void UpdateLatch()
    {
        if (!IsValidTarget(Target))
        {
            State = IDLE_STATE;

            NPC.netUpdate = true;
        }
        else
        {
            NPC.velocity = Vector2.Zero;

            NPC.Center = Target.Center - offset;
            NPC.gfxOffY = Target.gfxOffY;

            var target = NPC.AngleTo(Target.Center);

            if (NPC.direction == -1)
            {
                target += MathHelper.Pi;
            }

            NPC.rotation = NPC.rotation.AngleLerp(target, 0.5f);
        }
    }

    private void UpdateCollision()
    {
        if (Collision.WetCollision(NPC.position, NPC.width, NPC.height))
        {
            NPC.velocity.Y -= 0.3f;

            if (NPC.velocity.Y > -3f)
            {
                return;
            }

            NPC.velocity.Y = -3f;
        }

        if (NPC.velocity.Y == 0f && NPC.collideX && NPC.position.X == NPC.oldPosition.X)
        {
            NPC.velocity.Y = -Main.rand.NextFloat(4f, 6f);

            NPC.netUpdate = true;
        }

        Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
    }

    private void UpdateScale()
    {
        var target = State switch
        {
            IDLE_STATE => new Vector2(1f, 1f + (MathF.Sin(Main.GameUpdateCount * 0.01f) * 0.5f) * (MathF.Sin(Main.GameUpdateCount * 0.01f) * 0.5f)),
            _ => Vector2.One
        };

        scale = Vector2.SmoothStep(scale, target, 0.2f);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsValidTarget(NPC npc)
    {
        return npc.active
               && npc.type != Type
               && npc.whoAmI != NPC.whoAmI
               && npc.DistanceSQ(NPC.Center) <= ATTACK_DISTANCE * ATTACK_DISTANCE;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TrySearchTarget([MaybeNullWhen(false)] out NPC result)
    {
        result = null;

        var closest = float.MaxValue;
        var position = NPC.Center;

        foreach (var npc in Main.ActiveNPCs)
        {
            if (IsValidTarget(npc))
            {
                var distance = Vector2.DistanceSquared(npc.Center, position);

                if (distance < closest)
                {
                    closest = distance;
                    result = npc;
                }
            }
        }

        return result != null;
    }
}