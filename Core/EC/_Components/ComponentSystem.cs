using System.Runtime.CompilerServices;
using FloodMod.Utilities;

namespace FloodMod.Core.EC;

public sealed class ComponentSystem : ModSystem
{
	private static class ComponentData<T> where T : Component
	{
		public static readonly int Id = ComponentTypeCount++;

		public static T[] Components = [];

		static ComponentData() {
			OnUpdate += UpdateComponents;
			OnDraw += DrawComponents;
		}

		private static void UpdateComponents() {
			foreach (var component in Components) {
				if (component == null) {
					continue;
				}

				component.Update();
			}
		}
		
		private static void DrawComponents() {
			foreach (var component in Components) {
				if (component == null) {
					continue;
				}
				
				component.Draw();
			}
		}
	}

    internal const byte MaskSize = sizeof(ulong) * 8;

    internal static ulong[] Flags = [];
    
    internal static event Action OnUpdate;
    internal static event Action OnDraw;

    /// <summary>
    ///     The total amount of component types registered.
    /// </summary>
	public static int ComponentTypeCount { get; private set; }

    public override void Unload() {
        base.Unload();

        OnUpdate = null;
        OnDraw = null;
    }

    public override void PostUpdateWorld() {
	    base.PostUpdateWorld();
	    
	    OnUpdate?.Invoke();
	    OnDraw?.Invoke();
    }

    /// <summary>
    ///     Gets the value of a component of the specified type from an entity.
    /// </summary>
    /// <param name="entityId">The identity of the entity to retrieve the component from.</param>
    /// <typeparam name="T">The type of the component to retrieve.</typeparam>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Get<T>(int entityId) where T : Component {
		return ComponentData<T>.Components[entityId];
	}

    /// <summary>
    ///     Sets the value of a component of the specified type to an entity.
    /// </summary>
    /// <param name="entityId">The identity of the entity to set the component to.</param>
    /// <param name="value">The value of the component to set.</param>
    /// <typeparam name="T">The type of the component to set.</typeparam>
	public static T Set<T>(int entityId, T value) where T : Component {
        ArrayUtils.EnsureCapacity(ref ComponentData<T>.Components, entityId);

        var componentId = ComponentData<T>.Id;

        var masks = MathUtils.DivCeil(ComponentTypeCount, MaskSize);
        var index = (entityId * masks) + Math.DivRem(componentId, MaskSize, out var remainder);

        ArrayUtils.EnsureCapacity(ref Flags, index);

        var mask = 1UL << remainder;

        Flags[index] |= mask;

        ComponentData<T>.Components[entityId] = value;

        return value;
	}

    /// <summary>
    ///     Checks whether an entity has a component of the specified type or not.
    /// </summary>
    /// <param name="entityId">The identity of the entity to check.</param>
    /// <typeparam name="T">The type of the component to check.</typeparam>
    /// <returns><c>true</c> if the entity has the specified component type; otherwise, <c>false</c>.</returns>
    public static bool Has<T>(int entityId) where T : Component {
        if (entityId < 0 || entityId >= ComponentData<T>.Components.Length) {
            return false;
        }

        var componentId = ComponentData<T>.Id;

        var masks = MathUtils.DivCeil(ComponentTypeCount, MaskSize);
        var index = (entityId * masks) + Math.DivRem(componentId, MaskSize, out var remainder);

        if (index < 0 || index >= Flags.Length) {
            return false;
        }

        var mask = 1UL << remainder;

        return (Flags[index] & mask) != 0;
    }

    /// <summary>
    ///     Attempts to remove a component of the specified type from an entity.
    /// </summary>
    /// <param name="entityId">The identity of the entity to remove the component from.</param>
    /// <typeparam name="T">The type of the component to remove.</typeparam>
    /// <returns><c>true</c> if the component was successfully removed from the entity; otherwise, <c>false</c>.</returns>
	public static bool Remove<T>(int entityId) where T : Component {
        if (entityId < 0 || entityId >= ComponentData<T>.Components.Length) {
            return false;
        }

        var componentId = ComponentData<T>.Id;

        var masks = MathUtils.DivCeil(ComponentTypeCount, MaskSize);
        var index = (entityId * masks) + Math.DivRem(componentId, MaskSize, out var remainder);

        if (index < 0 || index >= Flags.Length) {
            return false;
        }

        var mask = 1UL << remainder;

        Flags[index] &= ~mask;

        ComponentData<T>.Components[entityId] = null;
        
        return true;
	}
}
