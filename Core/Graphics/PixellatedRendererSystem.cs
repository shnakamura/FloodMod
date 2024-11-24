using System.Collections.Generic;

namespace FloodMod.Core.Graphics;

[Autoload(Side = ModSide.Client)]
public sealed class PixellatedRendererSystem : ModSystem
{
	private static readonly List<Action> Actions = [];
	
	public static RenderTarget2D Buffer { get; private set; }

	public override void Load() {
		base.Load();

		Main.QueueMainThreadAction(static () => Buffer = new RenderTarget2D(
            Main.graphics.GraphicsDevice,
            Main.screenWidth / 2,
            Main.screenHeight / 2)
        );

		On_Main.CheckMonoliths += Main_CheckMonoliths_Hook;
		On_Main.DrawProjectiles += Main_DrawProjectiles_Hook;
        
		Main.OnResolutionChanged += Main_OnResolutionChanged_Event;
	}

	public override void Unload() {
		base.Unload();

		Main.OnResolutionChanged -= Main_OnResolutionChanged_Event;

		Main.QueueMainThreadAction(
			static () => {
				Buffer?.Dispose();
				Buffer = null;
			}
		);
	}

	// TODO: Change this and use a data structure instead.
	public static void Queue(Action action) {
		Actions.Add(action);
	}

	private static void DrawTarget() {
		if (Buffer?.IsDisposed == true) {
			return;
		}

		Main.spriteBatch.Begin(
			default,
			default,
			Main.DefaultSamplerState,
			default,
			Main.Rasterizer,
			default,
			Main.GameViewMatrix.TransformationMatrix
		);

		Main.spriteBatch.Draw(Buffer, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White);

		Main.spriteBatch.End();
	}

	private static void Main_CheckMonoliths_Hook(On_Main.orig_CheckMonoliths orig) {
		orig();

		if (Main.gameMenu) {
			return;
		}

		var device = Main.graphics.GraphicsDevice;
		var bindings = device.GetRenderTargets();

		device.SetRenderTarget(Buffer);
		device.Clear(Color.Transparent);

		Main.spriteBatch.Begin(
			default,
			default,
			Main.DefaultSamplerState,
			default,
			Main.Rasterizer,
			default,
			Matrix.CreateScale(0.5f, 0.5f, 1f)
		);

		foreach (var action in Actions) {
			action?.Invoke();
		}

		Main.spriteBatch.End();

		device.SetRenderTargets(bindings);

		Actions.Clear();
	}

    private static void Main_DrawProjectiles_Hook(On_Main.orig_DrawProjectiles orig, Main self) {
        DrawTarget();

        orig(self);
    }

    private static void Main_OnResolutionChanged_Event(Vector2 size) {
        Main.RunOnMainThread(
            () => {
                Buffer?.Dispose();

                Buffer = new(
                    Main.graphics.GraphicsDevice,
                    (int)(size.X / 2f),
                    (int)(size.Y / 2f)
                );
            }
        );
    }
}