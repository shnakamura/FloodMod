using FloodMod.Core.EC;
using FloodMod.Core.Physics;

namespace FloodMod.Core.Graphics;

public sealed class PixellatedTextureRenderer() : Component
{
    public override void Draw() {
        base.Draw();

        if (!Entity.Has<Transform>() || !Entity.Has<TextureRenderData>()) {
            return;
        }
        
        var transform = Entity.Get<Transform>();
        var info = Entity.Get<TextureRenderData>();

        PixellatedRendererSystem.Queue(
            () => {
                if (info.DestinationRectangle.HasValue) {
                    Main.spriteBatch.Draw(
                        info.Texture.Value,
                        info.DestinationRectangle.Value,
                        info.SourceRectangle,
                        info.Color,
                        transform.Rotation,
                        info.Origin,
                        info.Effects,
                        0f
                    );
                }
                else {
                    Main.spriteBatch.Draw(
                        info.Texture.Value,
                        transform.Position - Main.screenPosition,
                        info.SourceRectangle,
                        info.Color,
                        transform.Rotation,
                        info.Origin,
                        transform.Scale,
                        info.Effects,
                        0f
                    );
                }
            }
        );
    }
}

public class TextureRenderInfo { }