namespace FloodMod.Core.EC;

public abstract class Component
{
    public Entity Entity { get; internal set; }
    
    public virtual void Update() { }
    
    public virtual void Draw() { }
}