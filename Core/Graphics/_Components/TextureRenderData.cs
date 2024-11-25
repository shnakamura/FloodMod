using FloodMod.Core.EC;
using ReLogic.Content;

namespace FloodMod.Core.Graphics;

public sealed class TextureRenderData : Component
{
    public Asset<Texture2D> Texture;

    public Vector2 Origin;
    
    public Color Color {
        get => color * Opacity;
        set => color = value;
    }

    private Color color = Color.White;

    public float Opacity {
        get => opacity;
        set => opacity = MathHelper.Clamp(value, 0f, 1f);
    }

    private float opacity = 1f;

    public Rectangle? SourceRectangle;

    public Rectangle? DestinationRectangle;

    public SpriteEffects Effects;

    public TextureRenderData(
        Asset<Texture2D> texture,
        Color color,
        Rectangle? sourceRectangle = null,
        Vector2 origin = default,
        SpriteEffects effects = SpriteEffects.None
    ) {
        Texture = texture;
        Color = color;
        SourceRectangle = sourceRectangle;
        Origin = origin;
        Effects = effects;
    }

    public TextureRenderData(
        Asset<Texture2D> texture,
        Color color,
        Rectangle destinationRectangle,
        Rectangle? sourceRectangle = null,
        SpriteEffects effects = SpriteEffects.None) {
        Texture = texture;
        Color = color;
        DestinationRectangle = destinationRectangle;
        SourceRectangle = sourceRectangle;
        Effects = effects;
    }
}