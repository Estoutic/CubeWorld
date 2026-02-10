using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Компонент чанка. Хранит данные блоков и управляет мешем.
    /// Прикрепляется к GameObject в сцене.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class Chunk : MonoBehaviour
    {
        public const int SIZE = 16;

        // 3D-массив блоков. Каждый byte = BlockType.
        public byte[,,] Blocks { get; private set; }

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();
            Blocks = new byte[SIZE, SIZE, SIZE];
        }

        /// <summary>
        /// Заполняет чанк тестовыми данными — несколько слоёв блоков.
        /// </summary>
        public void GenerateTestData()
        {
            for (int x = 0; x < SIZE; x++)
            for (int y = 0; y < SIZE; y++)
            for (int z = 0; z < SIZE; z++)
            {
                if (y == 0)
                    Blocks[x, y, z] = (byte)BlockType.Stone;
                else if (y < 4)
                    Blocks[x, y, z] = (byte)BlockType.Dirt;
                else if (y == 4)
                    Blocks[x, y, z] = (byte)BlockType.Grass;
                else
                    Blocks[x, y, z] = (byte)BlockType.Air;
            }
        }

        /// <summary>
        /// Перестраивает меш на основе текущего массива блоков.
        /// Вызывай после любого изменения блоков.
        /// </summary>
        public void BuildMesh()
        {
            Mesh mesh = ChunkMeshBuilder.BuildMesh(Blocks, SIZE);
            _meshFilter.mesh = mesh;
            _meshCollider.sharedMesh = mesh;
        }

        /// <summary>
        /// Устанавливает блок в позиции (x, y, z).
        /// </summary>
        public void SetBlock(int x, int y, int z, BlockType type)
        {
            if (x < 0 || x >= SIZE || y < 0 || y >= SIZE || z < 0 || z >= SIZE)
                return;
            Blocks[x, y, z] = (byte)type;
        }

        /// <summary>
        /// Возвращает тип блока в позиции (x, y, z).
        /// </summary>
        public BlockType GetBlock(int x, int y, int z)
        {
            if (x < 0 || x >= SIZE || y < 0 || y >= SIZE || z < 0 || z >= SIZE)
                return BlockType.Air;
            return (BlockType)Blocks[x, y, z];
        }

        /// <summary>
        /// Устанавливает материал для рендера чанка.
        /// </summary>
        public void SetMaterial(Material material)
        {
            _meshRenderer.material = material;
        }
    }
}
