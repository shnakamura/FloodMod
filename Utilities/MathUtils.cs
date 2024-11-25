using System.Runtime.CompilerServices;

namespace FloodMod.Utilities;

public static class MathUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DivCeil(int left, int right) {
        return ((left - 1) / right) + 1;
    }
}
