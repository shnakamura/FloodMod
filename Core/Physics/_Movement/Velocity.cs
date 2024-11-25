using FloodMod.Core.EC;

namespace FloodMod.Core.Physics;

public sealed class Velocity(float x = 0f, float y = 0f) : Component
{
    public float X = x;
    public float Y = y;

    public Vector2 Value => new(X, Y);

    public override string ToString() {
        return $"X: {X}, Y: {Y}";
    }
}