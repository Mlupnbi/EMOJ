using System;

namespace EvenMoreOverpoweredJourney.Research.Crafting
{
    [Flags]
    public enum CraftEnvironmentFlags : ushort
    {
        None = 0,
        Water = 1 << 0,
        Lava = 1 << 1,
        Honey = 1 << 2,
        Graveyard = 1 << 3,
        Snow = 1 << 4,
        AlchemyTable = 1 << 5,
    }
}
