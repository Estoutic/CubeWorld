using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Управляет загрузкой/выгрузкой чанков вокруг игрока.
    /// Поддерживает вертикальные столбцы чанков для ландшафта с высотой.
    /// </summary>
    public class ChunkManager : MonoBehaviour
    {
        [Header("World Settings")]
        public int renderDistance = 8;
        public int chunksPerFrame = 4;
        public Transform playerTransform;
        public int worldSeed = 42;

        [Header("Vertical")]
        [Tooltip("Количество чанков по высоте (8 × 16 = 128 блоков)")]
        public int verticalChunks = 8;

        [Header("Materials")]
        public Material chunkMaterial;

        private Dictionary<Vector3Int, Chunk> _chunks = new Dictionary<Vector3Int, Chunk>();
        private Queue<Chunk> _chunkPool = new Queue<Chunk>();
        private Queue<ChunkBuildData> _buildQueue = new Queue<ChunkBuildData>();
        private HashSet<Vector3Int> _generating = new HashSet<Vector3Int>();
        private Vector3Int _lastPlayerChunkPos;

        private struct ChunkBuildData
        {
            public Vector3Int chunkPos;
            public byte[,,] blocks;
        }

        private void Start()
        {
            WorldGenerator.SetSeed(worldSeed);

            if (playerTransform == null)
                playerTransform = Camera.main?.transform;

            _lastPlayerChunkPos = GetPlayerChunkPos();
            UpdateChunks();
        }

        private void Update()
        {
            Vector3Int currentChunkPos = GetPlayerChunkPos();
            if (currentChunkPos != _lastPlayerChunkPos)
            {
                _lastPlayerChunkPos = currentChunkPos;
                UpdateChunks();
            }

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

        private Vector3Int GetPlayerChunkPos()
        {
            if (playerTransform == null) return Vector3Int.zero;
            Vector3 pos = playerTransform.position;
            return new Vector3Int(
                Mathf.FloorToInt(pos.x / Chunk.SIZE),
                0, // Отслеживаем только XZ для загрузки
                Mathf.FloorToInt(pos.z / Chunk.SIZE)
            );
        }

        private void UpdateChunks()
        {
            Vector3Int center = _lastPlayerChunkPos;
            HashSet<Vector3Int> needed = new HashSet<Vector3Int>();

            // Горизонтальный радиус + вертикальные столбцы
            for (int x = -renderDistance; x <= renderDistance; x++)
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                if (x * x + z * z > renderDistance * renderDistance) continue;

                for (int y = 0; y < verticalChunks; y++)
                {
                    Vector3Int pos = new Vector3Int(center.x + x, y, center.z + z);
                    needed.Add(pos);

                    if (!_chunks.ContainsKey(pos) && !_generating.Contains(pos))
                    {
                        _generating.Add(pos);
                        GenerateChunkAsync(pos);
                    }
                }
            }

            List<Vector3Int> toRemove = new List<Vector3Int>();
            foreach (var kvp in _chunks)
            {
                if (!needed.Contains(kvp.Key))
                    toRemove.Add(kvp.Key);
            }
            foreach (var pos in toRemove)
                RecycleChunk(pos);
        }

        private async void GenerateChunkAsync(Vector3Int chunkPos)
        {
            byte[,,] blocks = new byte[Chunk.SIZE, Chunk.SIZE, Chunk.SIZE];

            await Task.Run(() =>
            {
                WorldGenerator.GenerateChunkData(blocks, chunkPos);
            });

            lock (_buildQueue)
            {
                _buildQueue.Enqueue(new ChunkBuildData
                {
                    chunkPos = chunkPos,
                    blocks = blocks
                });
            }
        }

        private void CreateChunkFromData(ChunkBuildData data)
        {
            if (_chunks.ContainsKey(data.chunkPos)) return;

            Chunk chunk = GetOrCreateChunk();
            chunk.Init(data.chunkPos);
            System.Array.Copy(data.blocks, chunk.Blocks, data.blocks.Length);
            chunk.SetMaterial(chunkMaterial);
            chunk.BuildMesh((x, y, z) => GetBlockWorld(data.chunkPos, x, y, z));
            chunk.gameObject.SetActive(true);
            _chunks[data.chunkPos] = chunk;

            // Диагностика первых нескольких чанков
            if (_chunks.Count <= 3)
            {
                var mf = chunk.GetComponent<MeshFilter>();
                int verts = mf.mesh != null ? mf.mesh.vertexCount : 0;
                int nonAir = 0;
                for (int x = 0; x < Chunk.SIZE; x++)
                for (int y = 0; y < Chunk.SIZE; y++)
                for (int z = 0; z < Chunk.SIZE; z++)
                    if (data.blocks[x,y,z] != 0) nonAir++;
                Debug.Log($"[DIAG] Chunk {data.chunkPos}: verts={verts}, " +
                          $"nonAir={nonAir}, pos={chunk.transform.position}, " +
                          $"mat={chunk.GetComponent<MeshRenderer>().material?.shader?.name}");
            }
        }

        private BlockType GetBlockWorld(Vector3Int chunkPos, int localX, int localY, int localZ)
        {
            int cx = chunkPos.x + Mathf.FloorToInt((float)localX / Chunk.SIZE);
            int cy = chunkPos.y + Mathf.FloorToInt((float)localY / Chunk.SIZE);
            int cz = chunkPos.z + Mathf.FloorToInt((float)localZ / Chunk.SIZE);

            int bx = ((localX % Chunk.SIZE) + Chunk.SIZE) % Chunk.SIZE;
            int by = ((localY % Chunk.SIZE) + Chunk.SIZE) % Chunk.SIZE;
            int bz = ((localZ % Chunk.SIZE) + Chunk.SIZE) % Chunk.SIZE;

            Vector3Int neighborPos = new Vector3Int(cx, cy, cz);
            if (_chunks.TryGetValue(neighborPos, out Chunk neighbor))
                return neighbor.GetBlock(bx, by, bz);

            return BlockType.Air;
        }

        private Chunk GetOrCreateChunk()
        {
            if (_chunkPool.Count > 0)
                return _chunkPool.Dequeue();

            GameObject obj = new GameObject("Chunk");
            obj.transform.SetParent(transform);
            obj.AddComponent<MeshFilter>();
            obj.AddComponent<MeshRenderer>();
            obj.AddComponent<MeshCollider>();
            return obj.AddComponent<Chunk>();
        }

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

        public int ActiveChunkCount => _chunks.Count;

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            Gizmos.color = new Color(0, 1, 0, 0.05f);
            foreach (var kvp in _chunks)
            {
                Vector3 center = new Vector3(
                    kvp.Key.x * Chunk.SIZE + Chunk.SIZE / 2f,
                    kvp.Key.y * Chunk.SIZE + Chunk.SIZE / 2f,
                    kvp.Key.z * Chunk.SIZE + Chunk.SIZE / 2f
                );
                Gizmos.DrawWireCube(center, Vector3.one * Chunk.SIZE);
            }
        }
    }
}
