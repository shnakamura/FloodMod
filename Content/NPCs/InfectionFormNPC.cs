using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using FloodMod.Core.EC;
using FloodMod.Core.Graphics;
using FloodMod.Core.Physics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace FloodMod.Content.NPCs;

public class InfectionFormNPC : ModNPC
{
    public const float IDLE_STATE = 0f;
    
    /// <summary>
    ///     The maximum movement speed of this NPC while idling, in pixels per tick.
    /// </summary>
    public const float IDLE_MOVEMENT_SPEED = 3f;
    
    /// <summary>
    ///     The maximum movement acceleration of this NPC while idling, in pixels per tick.
    /// </summary>
    public const float IDLE_MOVEMENT_ACCELERATION = 0.05f;

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

    public static readonly SoundStyle IdleSound = new($"{nameof(FloodMod)}/Assets/Sounds/InfectionFormIdle", 10) {
        MaxInstances = 1,
        Volume = 0.8f,
        PitchVariance = 0.15f
    };
    
    public static readonly SoundStyle DeathSound = new($"{nameof(FloodMod)}/Assets/Sounds/InfectionFormDeath", 3) {
        Volume = 0.8f,
        PitchVariance = 0.15f
    };

    public static readonly Asset<Texture2D> ParticleTexture = ModContent.Request<Texture2D>($"{nameof(FloodMod)}/Assets/Textures/Particles/Blood");

    /// <summary>
    ///     Whether this NPC is in its idle state or not.
    /// </summary>
    public bool IsIdling => State == IDLE_STATE;
    
    /// <summary>
    ///     Whether this NPC is in its attack state or not.
    /// </summary>
    public bool IsAttacking => State == ATTACK_STATE;
    
    /// <summary>
    ///     The index of the target <see cref="NPC"/> associated with this NPC.
    /// </summary>
    public int Index { get; private set; }

    private NPC Target => Main.npc[Index];
    
    private ref float State => ref NPC.ai[0];

    private ref float Timer => ref NPC.ai[1];
    
    private Vector2 scale = Vector2.One;
    
    public override void SetStaticDefaults() {
        base.SetStaticDefaults();

        Main.npcFrameCount[Type] = 12;
    }

    public override void SetDefaults() {
        base.SetDefaults();

        NPC.noTileCollide = false;
        NPC.noGravity = false;
        
        NPC.knockBackResist = 0.5f;
        
        NPC.lifeMax = 30;
        NPC.defense = 2;
        NPC.damage = 15;

        NPC.width = 30;
        NPC.height = 30;

        AIType = -1;
        
        NPC.aiStyle = -1;

        DrawOffsetY = 2;

        NPC.DeathSound = DeathSound;
    }

    public override void SendExtraAI(BinaryWriter writer) {
        base.SendExtraAI(writer);
        
        writer.Write(Index);
    }

    public override void ReceiveExtraAI(BinaryReader reader) {
        base.ReceiveExtraAI(reader);

        Index = reader.Read7BitEncodedInt();
    }

    public override void AI() {
        base.AI();

        switch (State) {
            case IDLE_STATE:
                UpdateIdle();
                break;
            case ATTACK_STATE:
                UpdateAttack();
                break;
        }
        
        UpdateCollision();
        
        UpdateScale();
        UpdateRotation();
    }

    public override void FindFrame(int frameHeight) {
        base.FindFrame(frameHeight);

        NPC.spriteDirection = MathF.Sign(NPC.velocity.X);

        if (NPC.velocity.Y == 0f) {
            var minFrame = IsAttacking ? 5 : 0;
            var maxFrame = IsAttacking ? 10 : 5;
            
            NPC.frameCounter++;

            if (NPC.frameCounter < 8f) {
                return;
            }

            NPC.frame.Y += frameHeight;

            if (NPC.frame.Y >= maxFrame * frameHeight) {
                NPC.frame.Y = minFrame * frameHeight;
            }

            NPC.frameCounter = 0f;
        }
        else {
            NPC.frame.Y = 11 * frameHeight;
        }
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
        var texture = TextureAssets.Npc[Type].Value;
        
        var position = NPC.Center - Main.screenPosition + new Vector2(0f, NPC.gfxOffY + DrawOffsetY);

        var effects = NPC.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        
        Main.EntitySpriteDraw(
            texture,
            position,
            NPC.frame,
            NPC.GetAlpha(drawColor),
            NPC.rotation,
            NPC.frame.Size() / 2f,
            scale,
            effects,
            0f
        );
        
        return false;
    }

    public override void HitEffect(NPC.HitInfo hit) {
        base.HitEffect(hit);

        for (var i = 0; i < 20; i++) {
            var frame = new Rectangle(0, Main.rand.Next(3) * 10, 10, 10);
            
            EntitySystem.Create()
                .Set(new Transform(NPC.Center, null, Main.rand.NextFloat(MathHelper.TwoPi)))
                .Set(new Velocity(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 2f)))
                .Set(new TextureRenderData(ParticleTexture, Color.Red, frame, frame.Size() / 2f))
                .Set(new PixellatedTextureRenderer());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsValidTarget(NPC npc) {
        return npc.active && npc.type != Type && npc.whoAmI != NPC.whoAmI && npc.DistanceSQ(NPC.Center) <= ATTACK_DISTANCE * ATTACK_DISTANCE;
    }

    private bool TrySearchTarget([MaybeNullWhen(false)] out NPC result) {
        result = Main.npc[Main.maxNPCs];
        
        foreach (var npc in Main.ActiveNPCs) {
            if (IsValidTarget(npc)) {
                result = npc;
                return true;
            }
        }

        return false;
    }

    private void MoveTowards(float position) {
        NPC.direction = MathF.Sign(position - NPC.Center.X);

        var speed = IsAttacking ? ATTACK_MOVEMENT_SPEED : IDLE_MOVEMENT_SPEED;
        var acceleration = IsAttacking ? ATTACK_MOVEMENT_ACCELERATION : IDLE_MOVEMENT_ACCELERATION;
            
        NPC.velocity.X += acceleration * NPC.direction;
        NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X, -speed, speed);
    }

    private void UpdateIdle() {
        if (TrySearchTarget(out var result)) {
            Index = result.whoAmI;

            State = ATTACK_STATE;
            
            NPC.netUpdate = true;
        }
        else {
            // TODO: Idle.

        }
    }

    private void UpdateAttack() {
        if (!IsValidTarget(Target)) {
            State = IDLE_STATE;
            
            NPC.netUpdate = true;
        }
        else {
            MoveTowards(Target.Center.X);

            if (!Main.rand.NextBool(100) || NPC.velocity.Y != 0f) {
                return;
            }

            NPC.velocity.Y = -Main.rand.NextFloat(4f, 6f);
            
            NPC.netUpdate = true;
        }
    }

    private void UpdateCollision() {
        if (NPC.velocity.Y == 0f && NPC.collideX && NPC.position.X == NPC.oldPosition.X) {
            NPC.velocity.Y = -Main.rand.NextFloat(4f, 6f);

            NPC.netUpdate = true;
        }
        
        Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
    }

    private void UpdateScale() {
        var target = Vector2.One;

        if (NPC.velocity.Y != 0f) {
            var direction = NPC.velocity.SafeNormalize(Vector2.Zero);
            var offset = new Vector2(-MathF.Abs(direction.X) * 0.5f, MathF.Abs(direction.Y));

            target += offset;
        }

        scale = Vector2.SmoothStep(scale, target, 0.2f);
    }

    private void UpdateRotation() {
        var target = 0f;
        
        if (NPC.velocity.Y != 0f) {
            target = NPC.velocity.X * 0.1f;
        }
        
        NPC.rotation = NPC.rotation.AngleLerp(target, 0.2f);
    }
}