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
        private const float CaveScale = 0.03f;
        private const float CaveThreshold = 0.52f;    // Понижен — больше пещер
        private const int CaveMinY = 3;

        // Входы в пещеры
        private const float CaveEntranceScale = 0.02f;
        private const float CaveEntranceThreshold = 0.6f;  // Понижен — больше входов на склонах
        private const float SlopeThreshold = 2f;           // Понижен — даже на пологих склонах

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

            // Кэшируем высоты для всего чанка + соседние столбцы (для расчёта наклона)
            int[,] heightMap = new int[Chunk.SIZE + 2, Chunk.SIZE + 2];
            for (int x = -1; x <= Chunk.SIZE; x++)
            for (int z = -1; z <= Chunk.SIZE; z++)
            {
                int worldX = chunkWorldX + x;
                int worldZ = chunkWorldZ + z;
                float heightNoise = OctavePerlin(
                    (worldX + _seedX) * HeightScale,
                    (worldZ + _seedZ) * HeightScale,
                    HeightOctaves, HeightPersistence, HeightLacunarity);
                heightMap[x + 1, z + 1] = Mathf.RoundToInt(
                    Mathf.Lerp(MinHeight, MaxHeight, heightNoise));
            }

            for (int x = 0; x < Chunk.SIZE; x++)
            for (int z = 0; z < Chunk.SIZE; z++)
            {
                int worldX = chunkWorldX + x;
                int worldZ = chunkWorldZ + z;
                int surfaceHeight = heightMap[x + 1, z + 1];

                // Рассчитываем крутизну склона (разница высот с соседями)
                int hN = heightMap[x + 1, z + 2];
                int hS = heightMap[x + 1, z];
                int hE = heightMap[x + 2, z + 1];
                int hW = heightMap[x, z + 1];
                float slope = Mathf.Max(
                    Mathf.Abs(surfaceHeight - hN),
                    Mathf.Abs(surfaceHeight - hS),
                    Mathf.Abs(surfaceHeight - hE),
                    Mathf.Abs(surfaceHeight - hW));

                for (int y = 0; y < Chunk.SIZE; y++)
                {
                    int worldY = chunkWorldY + y;

                    if (worldY > surfaceHeight)
                    {
                        blocks[x, y, z] = (byte)BlockType.Air;
                        continue;
                    }

                    // Проверяем пещеры через 3D шум
                    bool isCave = false;
                    if (worldY > CaveMinY)
                    {
                        float cave = OctavePerlin3D(
                            (worldX + _seedCave) * CaveScale,
                            worldY * CaveScale,
                            (worldZ + _seedCave) * CaveScale,
                            2, 0.5f, 2f);

                        int depth = surfaceHeight - worldY;

                        if (cave > CaveThreshold)
                        {
                            // Обычная пещера — всегда вырезаем под поверхностью
                            if (depth >= 2)
                            {
                                isCave = true;
                            }
                            // Вход в пещеру на склоне
                            else if (depth >= 0 && slope >= SlopeThreshold)
                            {
                                float entranceNoise = OctavePerlin(
                                    (worldX + _seedCave + 500) * CaveEntranceScale,
                                    (worldZ + _seedCave + 500) * CaveEntranceScale,
                                    2, 0.5f, 2f);
                                if (entranceNoise > CaveEntranceThreshold)
                                {
                                    isCave = true;
                                }
                            }
                        }

                        // Вертикальные провалы (sinkholes) — дыры в земле ведущие в пещеры
                        if (!isCave && depth >= 0 && depth <= 4 && cave > CaveThreshold - 0.08f)
                        {
                            float sinkhole = OctavePerlin(
                                (worldX + _seedCave + 1000) * 0.012f,
                                (worldZ + _seedCave + 1000) * 0.012f,
                                2, 0.5f, 2f);
                            if (sinkhole > 0.7f)
                            {
                                isCave = true;
                            }
                        }

                        // Горизонтальные тоннели на уровне чуть ниже поверхности
                        // Создают заметные входы в склонах холмов
                        if (!isCave && worldY > CaveMinY + 5)
                        {
                            // Тоннель на фиксированных высотах (каждые ~20 блоков)
                            int tunnelBand = worldY % 20;
                            if (tunnelBand >= 0 && tunnelBand <= 2 && depth <= 6 && depth >= -1)
                            {
                                float tunnel = OctavePerlin(
                                    (worldX + _seedCave + 2000) * 0.025f,
                                    (worldZ + _seedCave + 2000) * 0.025f,
                                    2, 0.5f, 2f);
                                if (tunnel > 0.65f)
                                {
                                    isCave = true;
                                }
                            }
                        }
                    }

                    if (isCave)
                    {
                        blocks[x, y, z] = (byte)BlockType.Air;
                        continue;
                    }

                    // Определяем тип блока по глубине от поверхности
                    int blockDepth = surfaceHeight - worldY;

                    if (worldY <= 1)
                    {
                        blocks[x, y, z] = (byte)BlockType.Stone;
                    }
                    else if (blockDepth == 0)
                    {
                        blocks[x, y, z] = (byte)BlockType.Grass;
                    }
                    else if (blockDepth < 4)
                    {
                        blocks[x, y, z] = (byte)BlockType.Dirt;
                    }
                    else
                    {
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
