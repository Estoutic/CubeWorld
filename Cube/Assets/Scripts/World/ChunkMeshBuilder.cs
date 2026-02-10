using System.Collections.Generic;
using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Генератор меша для воксельного чанка.
    /// Создаёт полигоны только для видимых граней (face culling).
    /// </summary>
    public static class ChunkMeshBuilder
    {
        // 6 направлений проверки соседей: Right, Left, Up, Down, Front, Back
        private static readonly Vector3Int[] FaceDirections =
        {
            new Vector3Int( 1,  0,  0), // 0: Right  (+X)
            new Vector3Int(-1,  0,  0), // 1: Left   (-X)
            new Vector3Int( 0,  1,  0), // 2: Up     (+Y)
            new Vector3Int( 0, -1,  0), // 3: Down   (-Y)
            new Vector3Int( 0,  0,  1), // 4: Front  (+Z)
            new Vector3Int( 0,  0, -1), // 5: Back   (-Z)
        };

        // Вершины куба для каждой грани (4 вершины на грань, против часовой стрелки)
        private static readonly Vector3[][] FaceVertices =
        {
            // Right (+X)
            new Vector3[] {
                new Vector3(1, 0, 0), new Vector3(1, 1, 0),
                new Vector3(1, 1, 1), new Vector3(1, 0, 1)
            },
            // Left (-X)
            new Vector3[] {
                new Vector3(0, 0, 1), new Vector3(0, 1, 1),
                new Vector3(0, 1, 0), new Vector3(0, 0, 0)
            },
            // Up (+Y)
            new Vector3[] {
                new Vector3(0, 1, 0), new Vector3(0, 1, 1),
                new Vector3(1, 1, 1), new Vector3(1, 1, 0)
            },
            // Down (-Y)
            new Vector3[] {
                new Vector3(0, 0, 1), new Vector3(0, 0, 0),
                new Vector3(1, 0, 0), new Vector3(1, 0, 1)
            },
            // Front (+Z)
            new Vector3[] {
                new Vector3(1, 0, 1), new Vector3(1, 1, 1),
                new Vector3(0, 1, 1), new Vector3(0, 0, 1)
            },
            // Back (-Z)
            new Vector3[] {
                new Vector3(0, 0, 0), new Vector3(0, 1, 0),
                new Vector3(1, 1, 0), new Vector3(1, 0, 0)
            },
        };

        // Нормали для каждой грани
        private static readonly Vector3[] FaceNormals =
        {
            Vector3.right,   // Right
            Vector3.left,    // Left
            Vector3.up,      // Up
            Vector3.down,    // Down
            Vector3.forward, // Front
            Vector3.back,    // Back
        };

        /// <summary>
        /// Генерирует меш для чанка. Возвращает Mesh с только видимыми гранями.
        /// </summary>
        public static Mesh BuildMesh(byte[,,] blocks, int chunkSize)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();

            for (int x = 0; x < chunkSize; x++)
            for (int y = 0; y < chunkSize; y++)
            for (int z = 0; z < chunkSize; z++)
            {
                BlockType type = (BlockType)blocks[x, y, z];
                if (type == BlockType.Air) continue;

                Vector3 blockPos = new Vector3(x, y, z);

                // Проверяем каждую из 6 граней
                for (int face = 0; face < 6; face++)
                {
                    Vector3Int dir = FaceDirections[face];
                    int nx = x + dir.x;
                    int ny = y + dir.y;
                    int nz = z + dir.z;

                    // Грань видима если сосед за пределами чанка или несолидный
                    bool neighborSolid = false;
                    if (nx >= 0 && nx < chunkSize &&
                        ny >= 0 && ny < chunkSize &&
                        nz >= 0 && nz < chunkSize)
                    {
                        neighborSolid = BlockData.IsSolid((BlockType)blocks[nx, ny, nz]);
                    }

                    if (neighborSolid) continue;

                    // Добавляем грань
                    int vertStart = vertices.Count;
                    Vector3[] faceVerts = FaceVertices[face];
                    for (int v = 0; v < 4; v++)
                    {
                        vertices.Add(blockPos + faceVerts[v]);
                        normals.Add(FaceNormals[face]);
                    }

                    // UV координаты из текстурного атласа
                    Vector2 tileOffset = BlockData.GetTileOffset(type, face);
                    float t = BlockData.TileSize;
                    uvs.Add(tileOffset + new Vector2(0, 0));
                    uvs.Add(tileOffset + new Vector2(0, t));
                    uvs.Add(tileOffset + new Vector2(t, t));
                    uvs.Add(tileOffset + new Vector2(t, 0));

                    // Два треугольника (квад) — против часовой стрелки
                    triangles.Add(vertStart + 0);
                    triangles.Add(vertStart + 1);
                    triangles.Add(vertStart + 2);
                    triangles.Add(vertStart + 0);
                    triangles.Add(vertStart + 2);
                    triangles.Add(vertStart + 3);
                }
            }

            Mesh mesh = new Mesh();
            mesh.indexFormat = vertices.Count > 65535
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            return mesh;
        }
    }
}
