using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Точка входа: настраивает мир, камеру и ChunkManager.
    /// Добавь на пустой GameObject в сцене.
    /// </summary>
    public class ChunkTest : MonoBehaviour
    {
        [Header("Settings")]
        public int renderDistance = 5;
        public int worldSeed = 42;

        private void Start()
        {
            // Создаём процедурный материал
            Material mat = CreateProceduralAtlasMaterial();

            // Настраиваем камеру
            Camera cam = Camera.main;
            cam.orthographic = false;
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;
            cam.transform.position = new Vector3(0, 120, 0);
            cam.transform.rotation = Quaternion.Euler(45, 0, 0);

            // Добавляем контроллер полёта на камеру
            if (cam.GetComponent<CubeWorld.Player.FlyCameraController>() == null)
                cam.gameObject.AddComponent<CubeWorld.Player.FlyCameraController>();

            // Создаём ChunkManager
            ChunkManager manager = gameObject.AddComponent<ChunkManager>();
            manager.renderDistance = renderDistance;
            manager.chunkMaterial = mat;
            manager.playerTransform = cam.transform;
            manager.worldSeed = worldSeed;
            manager.verticalChunks = 8; // 8 × 16 = 128 блоков высоты

            Debug.Log($"[ChunkTest] World started! Render distance: {renderDistance} chunks");
        }

        private void Update()
        {
            // Показываем статистику
            ChunkManager manager = GetComponent<ChunkManager>();
            if (manager != null && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[World] Active chunks: {manager.ActiveChunkCount}, " +
                          $"Camera: {Camera.main.transform.position}");
            }
        }

        /// <summary>
        /// Создаёт процедурный текстурный атлас 4×4.
        /// </summary>
        private Material CreateProceduralAtlasMaterial()
        {
            int atlasSize = BlockData.AtlasSize;
            int tilePixels = 16;
            int textureSize = atlasSize * tilePixels;

            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[textureSize * textureSize];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color(0.5f, 0.5f, 0.5f, 1f);

            // Row 3: Grass-top, Dirt, Stone, Sand
            FillTile(pixels, textureSize, tilePixels, 0, 3, new Color(0.36f, 0.70f, 0.22f));
            FillTile(pixels, textureSize, tilePixels, 1, 3, new Color(0.55f, 0.37f, 0.18f));
            FillTile(pixels, textureSize, tilePixels, 2, 3, new Color(0.50f, 0.50f, 0.50f));
            FillTile(pixels, textureSize, tilePixels, 3, 3, new Color(0.90f, 0.85f, 0.55f));

            // Row 2: Grass-side, Leaves, Wood-side, Wood-top
            FillTile(pixels, textureSize, tilePixels, 0, 2, new Color(0.36f, 0.70f, 0.22f),
                     new Color(0.55f, 0.37f, 0.18f));
            FillTile(pixels, textureSize, tilePixels, 1, 2, new Color(0.18f, 0.55f, 0.15f));
            FillTile(pixels, textureSize, tilePixels, 2, 2, new Color(0.45f, 0.30f, 0.12f));
            FillTile(pixels, textureSize, tilePixels, 3, 2, new Color(0.55f, 0.40f, 0.20f));

            // Row 1: Snow, Water
            FillTile(pixels, textureSize, tilePixels, 0, 1, new Color(0.95f, 0.97f, 1.00f));
            FillTile(pixels, textureSize, tilePixels, 1, 1, new Color(0.20f, 0.50f, 0.85f));

            texture.SetPixels(pixels);
            texture.Apply();

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.mainTexture = texture;
            mat.SetFloat("_Smoothness", 0f);
            return mat;
        }

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

                float noise = Random.Range(-0.03f, 0.03f);
                c = new Color(
                    Mathf.Clamp01(c.r + noise),
                    Mathf.Clamp01(c.g + noise),
                    Mathf.Clamp01(c.b + noise), 1f);

                pixels[(startY + y) * texSize + (startX + x)] = c;
            }
        }
    }
}
