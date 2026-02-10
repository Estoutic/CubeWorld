using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Данные текстурного атласа для блоков.
    /// Атлас — это одна текстура, разбитая на сетку тайлов.
    /// Каждый тип блока знает свою позицию в этой сетке.
    /// </summary>
    public static class BlockData
    {
        // Размер атласа в тайлах (например, 4×4 = 16 тайлов)
        public const int AtlasSize = 4;
        public static readonly float TileSize = 1f / AtlasSize;

        /// <summary>
        /// Возвращает UV-координаты тайла в атласе для данного блока и грани.
        /// Позиция тайла задаётся как (column, row) от левого нижнего угла.
        /// </summary>
        public static Vector2 GetTileOffset(BlockType type, int faceIndex)
        {
            // faceIndex: 0=Right(+X), 1=Left(-X), 2=Top(+Y), 3=Bottom(-Y), 4=Front(+Z), 5=Back(-Z)
            Vector2Int tile = GetTilePosition(type, faceIndex);
            return new Vector2(tile.x * TileSize, tile.y * TileSize);
        }

        /// <summary>
        /// Позиция тайла в атласе (column, row). 
        /// Разные грани одного блока могут иметь разные текстуры (как трава).
        /// </summary>
        private static Vector2Int GetTilePosition(BlockType type, int faceIndex)
        {
            switch (type)
            {
                case BlockType.Grass:
                    if (faceIndex == 2) return new Vector2Int(0, 3); // Top: зелёная трава
                    if (faceIndex == 3) return new Vector2Int(1, 3); // Bottom: земля
                    return new Vector2Int(0, 2); // Sides: трава с боков

                case BlockType.Dirt:
                    return new Vector2Int(1, 3);

                case BlockType.Stone:
                    return new Vector2Int(2, 3);

                case BlockType.Sand:
                    return new Vector2Int(3, 3);

                case BlockType.Snow:
                    return new Vector2Int(0, 1);

                case BlockType.Water:
                    return new Vector2Int(1, 1);

                case BlockType.Wood:
                    if (faceIndex == 2 || faceIndex == 3) return new Vector2Int(3, 2); // Top/Bottom
                    return new Vector2Int(2, 2); // Sides

                case BlockType.Leaves:
                    return new Vector2Int(1, 2);

                default:
                    return new Vector2Int(0, 0);
            }
        }

        /// <summary>
        /// Является ли блок солидным (непрозрачным).
        /// Воздух и вода — не солидные.
        /// </summary>
        public static bool IsSolid(BlockType type)
        {
            return type != BlockType.Air && type != BlockType.Water;
        }
    }
}
