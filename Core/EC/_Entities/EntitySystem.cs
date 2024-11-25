using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FloodMod.Utilities;

namespace FloodMod.Core.EC;

public sealed class EntitySystem : ModSystem
{
    private static readonly Queue<int> Indices = [];

    private static Entity[] entities = [];

	private static int nextEntityId;

    public static Entity Create() {
		int id;

		if (!Indices.TryDequeue(out id)) {
			id = nextEntityId++;
		}

        ArrayUtils.EnsureCapacity(ref entities, id);

        var entity = new Entity(id);

        entities[id] = entity;

		return entity;
	}

	public static bool Destroy(int entityId) {
		if (entityId < 0 || entityId >= entities.Length) {
			return false;
		}

        for (var i = 0; i < ComponentSystem.ComponentTypeCount; i++) {
            var masks = MathUtils.DivCeil(ComponentSystem.ComponentTypeCount, ComponentSystem.MaskSize);
            var index = (entityId * masks) + Math.DivRem(i, ComponentSystem.MaskSize, out var remainder);

            var mask = 1UL << remainder;

            ComponentSystem.Flags[index] &= ~mask;
        }

		return true;
	}
}
