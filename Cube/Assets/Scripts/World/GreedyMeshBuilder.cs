using System.Collections.Generic;
using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Greedy Meshing в стиле Cube World.
    /// Использует vertex colors вместо текстурного атласа.
    /// Каждый блок — один сплошной цвет с directional shading и AO.
    /// </summary>
    public static class ChunkMeshBuilder
    {
        public delegate BlockType BlockGetter(int x, int y, int z);

        public static Mesh BuildMesh(byte[,,] blocks, int size)
        {
            return BuildMesh(blocks, size, null, Vector3Int.zero);
        }

        public static Mesh BuildMesh(byte[,,] blocks, int size,
            BlockGetter neighborGetter)
        {
            return BuildMesh(blocks, size, neighborGetter, Vector3Int.zero);
        }

        /// <summary>
        /// Генерирует меш с vertex colors и ambient occlusion.
        /// </summary>
        public static Mesh BuildMesh(byte[,,] blocks, int size,
            BlockGetter neighborGetter, Vector3Int chunkPos)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var colors = new List<Color>();
            var normals = new List<Vector3>();

            int chunkWorldX = chunkPos.x * size;
            int chunkWorldY = chunkPos.y * size;
            int chunkWorldZ = chunkPos.z * size;

            for (int axis = 0; axis < 3; axis++)
            {
                int axis1 = (axis + 1) % 3;
                int axis2 = (axis + 2) % 3;

                int[] pos = new int[3];
                int[] mask = new int[size * size];

                for (int slice = -1; slice < size; slice++)
                {
                    int maskIdx = 0;
                    for (int v = 0; v < size; v++)
                    for (int u = 0; u < size; u++)
                    {
                        pos[axis] = slice;
                        pos[axis1] = u;
                        pos[axis2] = v;
                        BlockType blockA = GetBlockSafe(blocks, size,
                            pos[0], pos[1], pos[2], neighborGetter);

                        pos[axis] = slice + 1;
                        BlockType blockB = GetBlockSafe(blocks, size,
                            pos[0], pos[1], pos[2], neighborGetter);

                        bool solidA = BlockData.IsSolid(blockA);
                        bool solidB = BlockData.IsSolid(blockB);

                        if (solidA == solidB)
                            mask[maskIdx] = 0;
                        else if (solidA)
                            mask[maskIdx] = (int)blockA;
                        else
                            mask[maskIdx] = -(int)blockB;

                        maskIdx++;
                    }

                    // Greedy merge
                    maskIdx = 0;
                    for (int v = 0; v < size; v++)
                    for (int u = 0; u < size; u++)
                    {
                        int blockId = mask[maskIdx];
                        if (blockId == 0) { maskIdx++; continue; }

                        // Расширяем по u
                        int width = 1;
                        while (u + width < size && mask[maskIdx + width] == blockId)
                            width++;

                        // Расширяем по v
                        int height = 1;
                        bool canExpand = true;
                        while (v + height < size && canExpand)
                        {
                            for (int w = 0; w < width; w++)
                            {
                                if (mask[(v + height) * size + (u + w)] != blockId)
                                { canExpand = false; break; }
                            }
                            if (canExpand) height++;
                        }

                        bool backFace = blockId < 0;
                        BlockType type = (BlockType)(backFace ? -blockId : blockId);
                        int faceIndex = GetFaceIndex(axis, backFace);

                        // Позиция квада
                        float[] origin = new float[3];
                        origin[axis] = slice + 1;
                        origin[axis1] = u;
                        origin[axis2] = v;

                        float[] du = new float[3];
                        du[axis1] = width;
                        float[] dv = new float[3];
                        dv[axis2] = height;

                        Vector3 v0 = new Vector3(origin[0], origin[1], origin[2]);
                        Vector3 vU = new Vector3(du[0], du[1], du[2]);
                        Vector3 vV = new Vector3(dv[0], dv[1], dv[2]);

                        // Нормаль
                        float[] nArr = new float[3];
                        nArr[axis] = backFace ? -1 : 1;
                        Vector3 normal = new Vector3(nArr[0], nArr[1], nArr[2]);

                        // Vertex color — центр грани для расчёта цвета
                        int centerX = chunkWorldX + (int)(origin[0] + du[0] * 0.5f);
                        int centerY = chunkWorldY + (int)(origin[1] + du[1] * 0.5f);
                        int centerZ = chunkWorldZ + (int)(origin[2] + dv[2] * 0.5f);
                        Color faceColor = BlockColors.GetFaceColor(
                            type, faceIndex, centerX, centerY, centerZ);

                        // 4 вершины квада
                        int vertStart = vertices.Count;
                        if (backFace)
                        {
                            vertices.Add(v0);
                            vertices.Add(v0 + vV);
                            vertices.Add(v0 + vU + vV);
                            vertices.Add(v0 + vU);
                        }
                        else
                        {
                            vertices.Add(v0);
                            vertices.Add(v0 + vU);
                            vertices.Add(v0 + vU + vV);
                            vertices.Add(v0 + vV);
                        }

                        for (int i = 0; i < 4; i++)
                        {
                            normals.Add(normal);
                            colors.Add(faceColor);
                        }

                        // Треугольники
                        triangles.Add(vertStart + 0);
                        triangles.Add(vertStart + 1);
                        triangles.Add(vertStart + 2);
                        triangles.Add(vertStart + 0);
                        triangles.Add(vertStart + 2);
                        triangles.Add(vertStart + 3);

                        // Очищаем маску
                        for (int dv2 = 0; dv2 < height; dv2++)
                        for (int du2 = 0; du2 < width; du2++)
                            mask[(v + dv2) * size + (u + du2)] = 0;

                        maskIdx++;
                    }
                }
            }

            Mesh mesh = new Mesh();
            mesh.indexFormat = vertices.Count > 65535
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetColors(colors);
            mesh.SetNormals(normals);
            return mesh;
        }

        private static BlockType GetBlockSafe(byte[,,] blocks, int size,
            int x, int y, int z, BlockGetter neighborGetter)
        {
            if (x >= 0 && x < size && y >= 0 && y < size && z >= 0 && z < size)
                return (BlockType)blocks[x, y, z];
            if (neighborGetter != null)
                return neighborGetter(x, y, z);
            return BlockType.Air;
        }

        private static int GetFaceIndex(int axis, bool backFace)
        {
            return axis * 2 + (backFace ? 1 : 0);
        }
    }
}
