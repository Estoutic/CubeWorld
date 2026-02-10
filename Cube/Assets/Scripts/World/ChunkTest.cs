using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Точка входа для тестирования чанка.
    /// Добавь этот скрипт на пустой GameObject в сцене.
    /// Он создаст чанк, заполнит тестовыми данными и отобразит.
    /// </summary>
    public class ChunkTest : MonoBehaviour
    {
        [Header("Atlas Settings")]
        [Tooltip("Если не задан — создаст процедурный атлас")]
        public Material chunkMaterial;

        private Chunk _chunk;

        private void Start()
        {
            // Создаём материал с процедурным атласом если не задан
            if (chunkMaterial == null)
            {
                chunkMaterial = CreateProceduralAtlasMaterial();
            }

            // Создаём GameObject для чанка
            CreateTestChunk();

            // Настраиваем камеру чтобы видеть чанк
            SetupCamera();
        }

        private void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            
            // Переключаем на перспективную камеру (проект создан как 2D)
            cam.orthographic = false;
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 500f;
            
            // Позиция: сверху-сбоку, смотрим на центр чанка
            cam.transform.position = new Vector3(28, 16, -12);
            cam.transform.LookAt(new Vector3(8, 2, 8));
        }

        private void CreateTestChunk()
        {
            GameObject chunkObj = new GameObject("TestChunk");
            chunkObj.AddComponent<MeshFilter>();
            chunkObj.AddComponent<MeshRenderer>();
            chunkObj.AddComponent<MeshCollider>();

            _chunk = chunkObj.AddComponent<Chunk>();
            _chunk.GenerateTestData();
            _chunk.SetMaterial(chunkMaterial);
            _chunk.BuildMesh();

            // Статистика
            Mesh mesh = chunkObj.GetComponent<MeshFilter>().sharedMesh;
            Debug.Log($"[ChunkTest] Chunk created! " +
                      $"Vertices: {mesh.vertexCount}, " +
                      $"Triangles: {mesh.triangles.Length / 3}");
        }

        /// <summary>
        /// Создаёт процедурный текстурный атлас 4×4 (64×64 px).
        /// Каждый тайл — 16×16 px с уникальным цветом для каждого типа блока.
        /// </summary>
        private Material CreateProceduralAtlasMaterial()
        {
            int atlasSize = BlockData.AtlasSize;
            int tilePixels = 16;
            int textureSize = atlasSize * tilePixels;

            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point; // Пиксельный стиль, без размытия
            texture.wrapMode = TextureWrapMode.Clamp;

            // Заливаем всё прозрачным
            Color[] pixels = new Color[textureSize * textureSize];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color(0.5f, 0.5f, 0.5f, 1f);

            // Рисуем тайлы для каждого типа блока
            // Row 3 (top): Grass-top, Dirt, Stone, Sand
            FillTile(pixels, textureSize, tilePixels, 0, 3, new Color(0.36f, 0.70f, 0.22f)); // Grass top
            FillTile(pixels, textureSize, tilePixels, 1, 3, new Color(0.55f, 0.37f, 0.18f)); // Dirt
            FillTile(pixels, textureSize, tilePixels, 2, 3, new Color(0.50f, 0.50f, 0.50f)); // Stone
            FillTile(pixels, textureSize, tilePixels, 3, 3, new Color(0.90f, 0.85f, 0.55f)); // Sand

            // Row 2: Grass-side, Leaves, Wood-side, Wood-top
            FillTile(pixels, textureSize, tilePixels, 0, 2, new Color(0.36f, 0.70f, 0.22f), // Grass side
                     new Color(0.55f, 0.37f, 0.18f)); // bottom half = dirt
            FillTile(pixels, textureSize, tilePixels, 1, 2, new Color(0.18f, 0.55f, 0.15f)); // Leaves
            FillTile(pixels, textureSize, tilePixels, 2, 2, new Color(0.45f, 0.30f, 0.12f)); // Wood side
            FillTile(pixels, textureSize, tilePixels, 3, 2, new Color(0.55f, 0.40f, 0.20f)); // Wood top

            // Row 1: Snow, Water
            FillTile(pixels, textureSize, tilePixels, 0, 1, new Color(0.95f, 0.97f, 1.00f)); // Snow
            FillTile(pixels, textureSize, tilePixels, 1, 1, new Color(0.20f, 0.50f, 0.85f)); // Water

            texture.SetPixels(pixels);
            texture.Apply();

            // Используем URP Lit шейдер
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.mainTexture = texture;
            mat.SetFloat("_Smoothness", 0f); // Без блеска
            return mat;
        }

        /// <summary>
        /// Заливает один тайл в атласе цветом. Опционально — нижняя половина другим цветом.
        /// </summary>
        private void FillTile(Color[] pixels, int texSize, int tileSize,
                              int tileX, int tileY, Color color, Color? bottomColor = null)
        {
            int startX = tileX * tileSize;
            int startY = tileY * tileSize;
            int halfY = tileSize / 2;

            for (int y = 0; y < tileSize; y++)
            for (int x = 0; x < tileSize; x++)
            {
                Color c = color;
                if (bottomColor.HasValue && y < halfY)
                    c = bottomColor.Value;

                // Добавляем лёгкий шум для естественности
                float noise = Random.Range(-0.03f, 0.03f);
                c = new Color(
                    Mathf.Clamp01(c.r + noise),
                    Mathf.Clamp01(c.g + noise),
                    Mathf.Clamp01(c.b + noise),
                    1f
                );

                pixels[(startY + y) * texSize + (startX + x)] = c;
            }
        }
    }
}
