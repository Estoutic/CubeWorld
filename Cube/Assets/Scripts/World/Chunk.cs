using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Компонент чанка. Хранит данные блоков и управляет мешем.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class Chunk : MonoBehaviour
    {
        public const int SIZE = 16;

        /// <summary>Координаты чанка в сетке чанков (не мировые).</summary>
        public Vector3Int ChunkPos { get; private set; }

        /// <summary>3D-массив блоков. Каждый byte = BlockType.</summary>
        public byte[,,] Blocks { get; private set; }

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();
        }

        /// <summary>Инициализирует чанк с координатами.</summary>
        public void Init(Vector3Int chunkPos)
        {
            ChunkPos = chunkPos;
            Blocks = new byte[SIZE, SIZE, SIZE];
            transform.position = new Vector3(
                chunkPos.x * SIZE,
                chunkPos.y * SIZE,
                chunkPos.z * SIZE
            );
            gameObject.name = $"Chunk_{chunkPos.x}_{chunkPos.y}_{chunkPos.z}";
        }

        /// <summary>Генерирует плоский тестовый ландшафт.</summary>
        public void GenerateTestData()
        {
            if (Blocks == null) Blocks = new byte[SIZE, SIZE, SIZE];

            // Плоский ландшафт на высоте Y=0 чанка (мировой Y зависит от ChunkPos.y)
            int worldYBase = ChunkPos.y * SIZE;
            for (int x = 0; x < SIZE; x++)
            for (int y = 0; y < SIZE; y++)
            for (int z = 0; z < SIZE; z++)
            {
                int worldY = worldYBase + y;
                if (worldY < 2)
                    Blocks[x, y, z] = (byte)BlockType.Stone;
                else if (worldY < 5)
                    Blocks[x, y, z] = (byte)BlockType.Dirt;
                else if (worldY == 5)
                    Blocks[x, y, z] = (byte)BlockType.Grass;
                else
                    Blocks[x, y, z] = (byte)BlockType.Air;
            }
        }
