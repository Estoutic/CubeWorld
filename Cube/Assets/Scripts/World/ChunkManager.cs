using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Управляет загрузкой/выгрузкой чанков вокруг игрока.
    /// Генерация данных чанков происходит в фоновых потоках.
    /// Создание мешей — в главном потоке (Unity API requirement).
    /// </summary>
    public class ChunkManager : MonoBehaviour
    {
        [Header("World Settings")]
        [Tooltip("Радиус загрузки чанков вокруг игрока")]
        public int renderDistance = 8;

        [Tooltip("Максимум чанков, создаваемых за кадр")]
        public int chunksPerFrame = 4;

        [Tooltip("Transform игрока (или камеры)")]
        public Transform playerTransform;

        [Header("Materials")]
        public Material chunkMaterial;

        // Все активные чанки: ключ = координаты чанка
        private Dictionary<Vector3Int, Chunk> _chunks = new Dictionary<Vector3Int, Chunk>();

        // Пул неиспользуемых чанков
        private Queue<Chunk> _chunkPool = new Queue<Chunk>();

        // Очередь чанков, данные которых готовы (сгенерированы в фоне)
        private Queue<ChunkBuildData> _buildQueue = new Queue<ChunkBuildData>();

        // Набор чанков, которые сейчас генерируются в фоне
        private HashSet<Vector3Int> _generating = new HashSet<Vector3Int>();

        // Последняя позиция игрока в координатах чанков
        private Vector3Int _lastPlayerChunkPos;

        // Данные для передачи из фонового потока в главный
        private struct ChunkBuildData
        {
            public Vector3Int chunkPos;
            public byte[,,] blocks;
        }

        private void Start()
        {
            if (playerTransform == null)
            {
                playerTransform = Camera.main?.transform;
            }
            _lastPlayerChunkPos = GetPlayerChunkPos();
            UpdateChunks();
        }

        private void Update()
        {
            // Проверяем, переместился ли игрок в другой чанк
            Vector3Int currentChunkPos = GetPlayerChunkPos();
            if (currentChunkPos != _lastPlayerChunkPos)
            {
                _lastPlayerChunkPos = currentChunkPos;
                UpdateChunks();
            }

            // Обрабатываем очередь готовых чанков (фоновые потоки → главный поток)
            int built = 0;
            while (_buildQueue.Count > 0 && built < chunksPerFrame)
            {
                ChunkBuildData data;
                lock (_buildQueue)
                {
                    if (_buildQueue.Count == 0) break;
                    data = _buildQueue.Dequeue();
                }

                _generating.Remove(data.chunkPos);
                CreateChunkFromData(data);
                built++;
            }
        }

        /// <summary>Координаты чанка, в котором находится игрок.</summary>
        private Vector3Int GetPlayerChunkPos()
        {
            if (playerTransform == null) return Vector3Int.zero;
            Vector3 pos = playerTransform.position;
            return new Vector3Int(
                Mathf.FloorToInt(pos.x / Chunk.SIZE),
                0, // Пока только горизонтальные чанки (Y=0)
                Mathf.FloorToInt(pos.z / Chunk.SIZE)
            );
        }

        /// <summary>
        /// Загружает новые чанки и выгружает дальние.
        /// </summary>
        private void UpdateChunks()
        {
            Vector3Int center = _lastPlayerChunkPos;
            HashSet<Vector3Int> needed = new HashSet<Vector3Int>();

            // Определяем, какие чанки нужны
            for (int x = -renderDistance; x <= renderDistance; x++)
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                // Круговой радиус вместо квадратного
                if (x * x + z * z > renderDistance * renderDistance) continue;

                Vector3Int pos = new Vector3Int(center.x + x, 0, center.z + z);
                needed.Add(pos);

                // Запускаем генерацию если чанка нет и он не генерируется
                if (!_chunks.ContainsKey(pos) && !_generating.Contains(pos))
                {
                    _generating.Add(pos);
                    GenerateChunkAsync(pos);
                }
            }

            // Выгружаем чанки за пределами радиуса
            List<Vector3Int> toRemove = new List<Vector3Int>();
            foreach (var kvp in _chunks)
            {
                if (!needed.Contains(kvp.Key))
                    toRemove.Add(kvp.Key);
            }

            foreach (var pos in toRemove)
            {
                RecycleChunk(pos);
            }
        }

        /// <summary>
        /// Генерирует данные чанка в фоновом потоке.
        /// </summary>
        private async void GenerateChunkAsync(Vector3Int chunkPos)
        {
            byte[,,] blocks = new byte[Chunk.SIZE, Chunk.SIZE, Chunk.SIZE];

            await Task.Run(() =>
            {
                // Генерация данных в фоне (пока плоский ландшафт)
                int worldYBase = chunkPos.y * Chunk.SIZE;
                for (int x = 0; x < Chunk.SIZE; x++)
                for (int y = 0; y < Chunk.SIZE; y++)
                for (int z = 0; z < Chunk.SIZE; z++)
                {
                    int worldY = worldYBase + y;
                    if (worldY < 2)
                        blocks[x, y, z] = (byte)BlockType.Stone;
                    else if (worldY < 5)
                        blocks[x, y, z] = (byte)BlockType.Dirt;
                    else if (worldY == 5)
                        blocks[x, y, z] = (byte)BlockType.Grass;
                }
            });

            // Ставим в очередь на создание меша (главный поток)
            lock (_buildQueue)
            {
                _buildQueue.Enqueue(new ChunkBuildData
                {
                    chunkPos = chunkPos,
                    blocks = blocks
                });
            }
        }

        /// <summary>
        /// Создаёт GameObject чанка из подготовленных данных (главный поток).
        /// </summary>
        private void CreateChunkFromData(ChunkBuildData data)
        {
            // Может быть уже не нужен (игрок ушёл)
            if (_chunks.ContainsKey(data.chunkPos)) return;

            Chunk chunk = GetOrCreateChunk();
            chunk.Init(data.chunkPos);

            // Копируем сгенерированные блоки
            System.Array.Copy(data.blocks, chunk.Blocks, data.blocks.Length);

            chunk.SetMaterial(chunkMaterial);

            // Строим меш с доступом к соседям для бесшовных стыков
            chunk.BuildMesh((x, y, z) => GetBlockWorld(data.chunkPos, x, y, z));

            chunk.gameObject.SetActive(true);
            _chunks[data.chunkPos] = chunk;
        }

        /// <summary>
        /// Получает блок по локальным координатам чанка (может выходить за пределы).
        /// Используется для бесшовных стыков между чанками.
        /// </summary>
        private BlockType GetBlockWorld(Vector3Int chunkPos, int localX, int localY, int localZ)
        {
            // Вычисляем какой чанк и какой блок внутри него
            int cx = chunkPos.x + Mathf.FloorToInt((float)localX / Chunk.SIZE);
            int cy = chunkPos.y + Mathf.FloorToInt((float)localY / Chunk.SIZE);
            int cz = chunkPos.z + Mathf.FloorToInt((float)localZ / Chunk.SIZE);

            int bx = ((localX % Chunk.SIZE) + Chunk.SIZE) % Chunk.SIZE;
            int by = ((localY % Chunk.SIZE) + Chunk.SIZE) % Chunk.SIZE;
            int bz = ((localZ % Chunk.SIZE) + Chunk.SIZE) % Chunk.SIZE;

            Vector3Int neighborPos = new Vector3Int(cx, cy, cz);
            if (_chunks.TryGetValue(neighborPos, out Chunk neighbor))
            {
                return neighbor.GetBlock(bx, by, bz);
            }

            return BlockType.Air;
        }

        /// <summary>Получает чанк из пула или создаёт новый.</summary>
        private Chunk GetOrCreateChunk()
        {
            if (_chunkPool.Count > 0)
            {
                return _chunkPool.Dequeue();
            }

            GameObject obj = new GameObject("Chunk");
            obj.transform.SetParent(transform);
            obj.AddComponent<MeshFilter>();
            obj.AddComponent<MeshRenderer>();
            obj.AddComponent<MeshCollider>();
            return obj.AddComponent<Chunk>();
        }

        /// <summary>Убирает чанк из мира и возвращает в пул.</summary>
        private void RecycleChunk(Vector3Int pos)
        {
            if (_chunks.TryGetValue(pos, out Chunk chunk))
            {
                chunk.Clear();
                chunk.gameObject.SetActive(false);
                _chunkPool.Enqueue(chunk);
                _chunks.Remove(pos);
            }
        }

        /// <summary>Количество активных чанков (для отладки).</summary>
        public int ActiveChunkCount => _chunks.Count;

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // Рисуем границы загруженных чанков
            Gizmos.color = new Color(0, 1, 0, 0.1f);
            foreach (var kvp in _chunks)
            {
                Vector3 center = new Vector3(
                    kvp.Key.x * Chunk.SIZE + Chunk.SIZE / 2f,
                    Chunk.SIZE / 2f,
                    kvp.Key.z * Chunk.SIZE + Chunk.SIZE / 2f
                );
                Gizmos.DrawWireCube(center, Vector3.one * Chunk.SIZE);
            }
        }
    }
}
