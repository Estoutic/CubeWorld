using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Процедурный генератор ландшафта на основе Perlin Noise.
    /// Генерирует высоту, пещеры и слои блоков.
    /// </summary>
    public static class WorldGenerator
    {
        // === Настройки генерации ===

        // Высота
        public const int SeaLevel = 40;
        public const int MaxHeight = 100;
        public const int MinHeight = 5;

        // Шум высоты
        private const float HeightScale = 0.008f;    // Масштаб (чем меньше — тем шире холмы)
        private const int HeightOctaves = 5;          // Октавы шума
        private const float HeightPersistence = 0.45f;// Затухание амплитуды
        private const float HeightLacunarity = 2.2f;  // Увеличение частоты

        // Пещеры (3D шум)
        private const float CaveScale = 0.035f;
        private const float CaveThreshold = 0.58f;    // Чем выше — тем меньше пещер
        private const int CaveMinY = 3;               // Пещеры не ниже этой высоты

        // Seed мира
        private static int _seed = 42;
        private static int _seedX, _seedZ, _seedCave;

        /// <summary>Устанавливает seed мира.</summary>
        public static void SetSeed(int seed)
        {
            _seed = seed;
            // Генерируем смещения из seed для разных шумов
            var rng = new System.Random(seed);
            _seedX = rng.Next(-100000, 100000);
            _seedZ = rng.Next(-100000, 100000);
            _seedCave = rng.Next(-100000, 100000);
        }

        static WorldGenerator()
        {
            SetSeed(42);
        }

        /// <summary>
        /// Генерирует блоки для одного чанка по его мировым координатам.
        /// Вызывается из фонового потока — не использует Unity API.
        /// </summary>
        public static void GenerateChunkData(byte[,,] blocks, Vector3Int chunkPos)
        {
            int chunkWorldX = chunkPos.x * Chunk.SIZE;
            int chunkWorldY = chunkPos.y * Chunk.SIZE;
            int chunkWorldZ = chunkPos.z * Chunk.SIZE;

            for (int x = 0; x < Chunk.SIZE; x++)
            for (int z = 0; z < Chunk.SIZE; z++)
            {
                int worldX = chunkWorldX + x;
                int worldZ = chunkWorldZ + z;

                // Высота ландшафта через многооктавный шум
                float heightNoise = OctavePerlin(
                    (worldX + _seedX) * HeightScale,
                    (worldZ + _seedZ) * HeightScale,
                    HeightOctaves, HeightPersistence, HeightLacunarity);

                // Преобразуем шум [0..1] в высоту [MinHeight..MaxHeight]
                int surfaceHeight = Mathf.RoundToInt(
                    Mathf.Lerp(MinHeight, MaxHeight, heightNoise));

                for (int y = 0; y < Chunk.SIZE; y++)
                {
                    int worldY = chunkWorldY + y;

                    if (worldY > surfaceHeight)
                    {
                        // Над поверхностью — воздух
                        blocks[x, y, z] = (byte)BlockType.Air;
                        continue;
                    }

                    // Проверяем пещеры через 3D шум
                    if (worldY > CaveMinY && worldY < surfaceHeight - 1)
                    {
                        float cave = OctavePerlin3D(
                            (worldX + _seedCave) * CaveScale,
                            worldY * CaveScale,
                            (worldZ + _seedCave) * CaveScale,
                            2, 0.5f, 2f);

                        if (cave > CaveThreshold)
                        {
                            blocks[x, y, z] = (byte)BlockType.Air;
                            continue;
                        }
                    }

                    // Определяем тип блока по глубине от поверхности
                    int depth = surfaceHeight - worldY;

                    if (worldY <= 1)
                    {
                        // Bedrock (нижний слой) — камень
                        blocks[x, y, z] = (byte)BlockType.Stone;
                    }
                    else if (depth == 0)
                    {
                        // Поверхность
                        blocks[x, y, z] = (byte)BlockType.Grass;
                    }
                    else if (depth < 4)
                    {
                        // Подповерхность — земля
                        blocks[x, y, z] = (byte)BlockType.Dirt;
                    }
                    else
                    {
                        // Глубже — камень
                        blocks[x, y, z] = (byte)BlockType.Stone;
                    }
                }
            }
        }

        /// <summary>
        /// Многооктавный 2D Perlin Noise. Возвращает значение ~[0..1].
        /// </summary>
        private static float OctavePerlin(float x, float y,
            int octaves, float persistence, float lacunarity)
        {
            float total = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float maxValue = 0f;

            for (int i = 0; i < octaves; i++)
            {
                total += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return Mathf.Clamp01(total / maxValue);
        }

        /// <summary>
        /// 3D Perlin Noise (через комбинацию 2D срезов). Возвращает ~[0..1].
        /// </summary>
        private static float OctavePerlin3D(float x, float y, float z,
            int octaves, float persistence, float lacunarity)
        {
            // Комбинируем три среза 2D-шума для получения 3D
            float xy = OctavePerlin(x, y, octaves, persistence, lacunarity);
            float xz = OctavePerlin(x + 100f, z + 100f, octaves, persistence, lacunarity);
            float yz = OctavePerlin(y + 200f, z + 200f, octaves, persistence, lacunarity);

            return (xy + xz + yz) / 3f;
        }
    }
}
