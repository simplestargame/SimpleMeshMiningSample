using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace SimplestarGame
{
    /// <summary>
    /// Chunk結合したメッシュの総頂点数を予め調べる処理
    /// バッファを固定長で確保してから、そのバッファにデータを書き込んでいく処理がその先に待つ
    /// 今回は InsetCube のため、+X と -Y, -Z 方向の面側のカリングをしない判定となっている
    /// </summary>
    [BurstCompile]
    unsafe public struct CountChunkVertexJob : IJobParallelFor
    {
        [ReadOnly] NativeArray<XYZ> xyz;
        [ReadOnly] NativeArray<int> vertexCounts;
        [ReadOnly] NativeArray<byte> voxelData;
        NativeArray<int> results;
        Vector3Int chunkOffset;
        int edgeMaxCubeIndex;
        int dataEdgeCubeCount;
        int d;

        public CountChunkVertexJob(NativeArray<XYZ> xyz, 
            NativeArray<int> vertexCounts, NativeArray<byte> voxelData, NativeArray<int> results, Vector3Int chunkOffset
            , int edgeCubeCount, int dataEdgeCubeCount, int d)
        {
            this.xyz = xyz;
            this.vertexCounts = vertexCounts;
            this.voxelData = voxelData;
            this.results = results;
            this.chunkOffset = chunkOffset;
            this.edgeMaxCubeIndex = edgeCubeCount;
            this.dataEdgeCubeCount = dataEdgeCubeCount;
            this.d = d;
        }

        /// <summary>
        /// ボクセルの x, y, z 座標指定での値取得
        /// </summary>
        /// <param name="x">幅座標</param>
        /// <param name="y">高さ座標</param>
        /// <param name="z">奥行き座標</param>
        /// <returns></returns>
        byte GetVoxelValue(int x, int y, int z)
        {
            for (int xo = 0; xo < this.d; xo++)
            {
                for (int yo = 0; yo < this.d; yo++)
                {
                    for (int zo = 0; zo < this.d; zo++)
                    {
                        var v = *((byte*)this.voxelData.GetUnsafeReadOnlyPtr()
                            + ((x + xo) * this.dataEdgeCubeCount * this.dataEdgeCubeCount + (y + yo) * this.dataEdgeCubeCount + (z + zo)));
                        if (v != 0)
                        {
                            return v;
                        }
                    }
                }
            }
            return 0;
        }

        public void Execute(int index)
        {
            this.results[index] = 0;
            var xyz = this.xyz[index] * this.d;
            var x = xyz.x + chunkOffset.x;
            var y = xyz.y + chunkOffset.y;
            var z = xyz.z + chunkOffset.z;
            var e = this.edgeMaxCubeIndex > 2;

            if (this.GetVoxelValue(x, y, z) != 0)
            {
                for (int c = 0; c < this.vertexCounts.Length; c++)
                {
                    var count = this.vertexCounts[c];
                    if (c == CAWFile.PLUS_X)
                    {
                        if ((e || xyz.x != this.edgeMaxCubeIndex) && x + d != this.dataEdgeCubeCount)
                        {
                            if (this.GetVoxelValue(x + d, y, z) == 0)
                            {
                                this.results[index] += count;
                            }
                        }
                        else
                        {
                            this.results[index] += count;
                        }
                    }
                    else if (c == CAWFile.PLUS_Y)
                    {
                        if ((e || xyz.y != this.edgeMaxCubeIndex) && y + d != this.dataEdgeCubeCount)
                        {
                            if (this.GetVoxelValue(x, y + d, z) == 0)
                            {
                                this.results[index] += count;
                            }
                        }
                        else
                        {
                            this.results[index] += count;
                        }
                    }
                    else if (c == CAWFile.PLUS_Z)
                    {
                        if ((e || xyz.z != this.edgeMaxCubeIndex) && z + d != this.dataEdgeCubeCount)
                        {
                            if (this.GetVoxelValue(x, y, z + d) == 0)
                            {
                                this.results[index] += count;
                            }
                        }
                        else
                        {
                            this.results[index] += count;
                        }
                    }
                    else if (c == CAWFile.MINUS_X)
                    {
                        if ((e || xyz.x != 0) && x != 0)
                        {
                            if (this.GetVoxelValue(x - d, y, z) == 0)
                            {
                                this.results[index] += count;
                            }
                        }
                        else
                        {
                            this.results[index] += count;
                        }
                    }
                    else if (c == CAWFile.MINUS_Y)
                    {
                        if ((e || xyz.y != 0) && y != 0)
                        {
                            if (this.GetVoxelValue(x, y -d, z) == 0)
                            {
                                this.results[index] += count;
                            }
                        }
                        else
                        {
                            this.results[index] += count;
                        }
                    }
                    else if (c == CAWFile.MINUS_Z)
                    {
                        if ((e || xyz.z != 0) && z != 0)
                        {
                            if (this.GetVoxelValue(x, y, z - d) == 0)
                            {
                                this.results[index] += count;
                            }
                        }
                        else
                        {
                            this.results[index] += count;
                        }
                    }
                    else if (c == CAWFile.REMAIN)
                    {
                        this.results[index] += count;
                    }
                }
            }
        }
    }
}