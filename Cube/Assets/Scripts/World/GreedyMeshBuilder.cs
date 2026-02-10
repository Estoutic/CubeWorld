using System;
using System.Collections.Generic;
using UnityEngine;

namespace CubeWorld.World
{
    /// <summary>
    /// Greedy Meshing — объединяет соседние одинаковые грани в большие полигоны.
    /// Поддерживает доступ к соседним чанкам для бесшовных стыков.
    /// </summary>
    public static class ChunkMeshBuilder
    {
        /// <summary>
        /// Делегат для получения блока по мировым координатам.
        /// Используется для доступа к соседним чанкам на границах.
        /// </summary>
        public delegate BlockType BlockGetter(int x, int y, int z);

        /// <summary>
        /// Простая версия — без соседей (для одиночного чанка).
        /// </summary>
        public static Mesh BuildMesh(byte[,,] blocks, int size)
        {
            return BuildMesh(blocks, size, null);
        }

        /// <summary>
        /// Полная версия — с доступом к соседним блокам через neighborGetter.
        /// neighborGetter принимает локальные координаты (могут быть за пределами 0..size-1).
        /// </summary>
        public static Mesh BuildMesh(byte[,,] blocks, int size, BlockGetter neighborGetter)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();

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
                        BlockType blockA = GetBlockSafe(blocks, size, pos[0], pos[1], pos[2], neighborGetter);

                        pos[axis] = slice + 1;
                        BlockType blockB = GetBlockSafe(blocks, size, pos[0], pos[1], pos[2], neighborGetter);

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

                        int width = 1;
                        while (u + width < size && mask[maskIdx + width] == blockId)
                            width++;

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

                        float[] nArr = new float[3];
                        nArr[axis] = backFace ? -1 : 1;
                        Vector3 normal = new Vector3(nArr[0], nArr[1], nArr[2]);

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
                            normals.Add(normal);

                        Vector2 tileOffset = BlockData.GetTileOffset(type, faceIndex);
                        float t = BlockData.TileSize;
                        uvs.Add(tileOffset + new Vector2(0, 0));
                        uvs.Add(tileOffset + new Vector2(t, 0));
                        uvs.Add(tileOffset + new Vector2(t, t));
                        uvs.Add(tileOffset + new Vector2(0, t));

                        triangles.Add(vertStart + 0);
                        triangles.Add(vertStart + 1);
                        triangles.Add(vertStart + 2);
                        triangles.Add(vertStart + 0);
                        triangles.Add(vertStart + 2);
                        triangles.Add(vertStart + 3);

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
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            return mesh;
        }

        /// <summary>
        /// Получает блок с поддержкой соседних чанков.
        /// Если координаты за пределами чанка и есть neighborGetter — спрашивает у него.
        /// </summary>
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
