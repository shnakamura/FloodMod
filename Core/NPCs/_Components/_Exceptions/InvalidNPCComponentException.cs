namespace FloodMod.Core.NPCs;

public sealed class InvalidNPCComponentException : Exception
{
    public InvalidNPCComponentException() { }
    
    public InvalidNPCComponentException(string? message) : base(message) { }
    
    public InvalidNPCComponentException(string? message, Exception? innerException) : base(message, innerException) { }
}