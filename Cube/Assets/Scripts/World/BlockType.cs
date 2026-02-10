namespace CubeWorld.World
{
    /// <summary>
    /// Типы блоков в воксельном мире.
    /// Значение 0 (Air) = пустой блок, всё остальное — солидный.
    /// </summary>
    public enum BlockType : byte
    {
        Air = 0,
        Grass = 1,
        Dirt = 2,
        Stone = 3,
        Sand = 4,
        Snow = 5,
        Water = 6,
        Wood = 7,
        Leaves = 8
    }
}
