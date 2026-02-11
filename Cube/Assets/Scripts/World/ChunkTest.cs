using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Точка входа: настраивает мир, камеру и ChunkManager.
    /// </summary>
    public class ChunkTest : MonoBehaviour
    {
        [Header("Settings")]
        public int renderDistance = 5;
        public int worldSeed = 42;

        private void Start()
        {
            // Материал с vertex colors (стиль Cube World)
            Material mat = CreateVertexColorMaterial();

            // Камера
            Camera cam = Camera.main;
            cam.orthographic = false;
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;
            cam.transform.position = new Vector3(0, 120, 0);
            cam.transform.rotation = Quaternion.Euler(45, 0, 0);

            // Skybox цвет
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.45f, 0.70f, 0.95f); // Голубое небо

            // Контроллер полёта
            if (cam.GetComponent<CubeWorld.Player.FlyCameraController>() == null)
                cam.gameObject.AddComponent<CubeWorld.Player.FlyCameraController>();

            // ChunkManager
            ChunkManager manager = gameObject.AddComponent<ChunkManager>();
            manager.renderDistance = renderDistance;
            manager.chunkMaterial = mat;
            manager.playerTransform = cam.transform;
            manager.worldSeed = worldSeed;
            manager.verticalChunks = 8;

            // Directional Light (солнце)
            SetupLighting();

            Debug.Log($"[CubeWorld] Started! Seed: {worldSeed}, " +
                      $"Render: {renderDistance}, Style: Vertex Colors");
        }

        private void SetupLighting()
        {
            // Ищем или создаём солнце
            Light sun = FindAnyObjectByType<Light>();
            if (sun == null)
            {
                GameObject sunObj = new GameObject("Sun");
                sun = sunObj.AddComponent<Light>();
            }
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(50, -30, 0);
            sun.color = new Color(1f, 0.96f, 0.84f); // Тёплый свет
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.8f;
        }

        private Material CreateVertexColorMaterial()
        {
            // Используем наш кастомный Lit шейдер с vertex colors
            Shader shader = Shader.Find("CubeWorld/VertexColorLit");
            if (shader != null)
            {
                Debug.Log("[CubeWorld] Using CubeWorld/VertexColorLit");
                Material mat = new Material(shader);
                mat.SetFloat("_AmbientStrength", 0.35f);
                mat.SetFloat("_ShadowStrength", 0.6f);
                return mat;
            }

            // Fallback: старый unlit шейдер
            shader = Shader.Find("CubeWorld/VertexColor");
            if (shader != null)
            {
                Debug.Log("[CubeWorld] Fallback: CubeWorld/VertexColor (no lighting)");
                return new Material(shader);
            }

            Debug.LogError("[CubeWorld] No CubeWorld shader found!");
            return new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        }

        private void Update()
        {
            ChunkManager manager = GetComponent<ChunkManager>();
            if (manager != null && Time.frameCount % 120 == 0)
            {
                Debug.Log($"[World] Chunks: {manager.ActiveChunkCount}, " +
                          $"Pos: {Camera.main.transform.position:F0}");
            }
        }
    }
}
