using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Цвета блоков в стиле Cube World.
    /// Каждый блок — один сплошной цвет с лёгкими вариациями.
    /// </summary>
    public static class BlockColors
    {
        // Базовые цвета блоков (как в Cube World — яркие, насыщенные)
        private static readonly Color[] BaseColors = new Color[256];

        static BlockColors()
        {
            // Инициализируем цвета
            BaseColors[(int)BlockType.Air]    = Color.clear;
            BaseColors[(int)BlockType.Grass]  = new Color(0.40f, 0.75f, 0.25f); // Яркая зелень
            BaseColors[(int)BlockType.Dirt]   = new Color(0.50f, 0.36f, 0.22f); // Коричневый (менее красный)
            BaseColors[(int)BlockType.Stone]  = new Color(0.52f, 0.52f, 0.55f); // Серый
            BaseColors[(int)BlockType.Sand]   = new Color(0.92f, 0.87f, 0.58f); // Песочный
            BaseColors[(int)BlockType.Snow]   = new Color(0.95f, 0.97f, 1.00f); // Белый
            BaseColors[(int)BlockType.Water]  = new Color(0.25f, 0.55f, 0.90f); // Голубой
            BaseColors[(int)BlockType.Wood]   = new Color(0.48f, 0.32f, 0.15f); // Тёмное дерево
            BaseColors[(int)BlockType.Leaves] = new Color(0.20f, 0.60f, 0.18f); // Тёмная зелень
        }

        /// <summary>
        /// Возвращает цвет блока с лёгкой вариацией на основе позиции.
        /// Это создаёт естественный вид без текстур.
        /// </summary>
        public static Color GetColor(BlockType type, int worldX, int worldY, int worldZ)
        {
            Color baseColor = BaseColors[(int)type];
            if (type == BlockType.Air) return Color.clear;

            // Хэш позиции для стабильной вариации цвета
            int hash = Hash(worldX, worldY, worldZ);
            float variation = ((hash & 0xFF) / 255f - 0.5f) * 0.04f; // ±2% вариация (уменьшена)

            return new Color(
                Mathf.Clamp01(baseColor.r + variation),
                Mathf.Clamp01(baseColor.g + variation),
                Mathf.Clamp01(baseColor.b + variation),
                1f
            );
        }

        /// <summary>
        /// Цвет грани с учётом направления (верх светлее, низ темнее).
        /// Имитирует простое освещение без дополнительных расчётов.
        /// </summary>
        public static Color GetFaceColor(BlockType type, int faceIndex,
            int worldX, int worldY, int worldZ)
        {
            Color color = GetColor(type, worldX, worldY, worldZ);

            // Subtle directional shading — дополняет Unity-освещение
            // Мягкие вариации, основную работу делает шейдер с реальным светом
            float shade;
            switch (faceIndex)
            {
                case 2: shade = 1.0f;  break; // Top    (+Y)
                case 3: shade = 0.75f; break; // Bottom (-Y)
                case 0: // Right  (+X)
                case 5: shade = 0.90f; break; // Back   (-Z)
                case 1: // Left   (-X)
                case 4: shade = 0.85f; break; // Front  (+Z)
                default: shade = 0.87f; break;
            }

            return new Color(color.r * shade, color.g * shade, color.b * shade, 1f);
        }

        /// <summary>Простой хэш для стабильной псевдослучайности.</summary>
        private static int Hash(int x, int y, int z)
        {
            int h = x * 374761393 + y * 668265263 + z * 1274126177;
            h = (h ^ (h >> 13)) * 1103515245;
            return h ^ (h >> 16);
        }
    }
}
