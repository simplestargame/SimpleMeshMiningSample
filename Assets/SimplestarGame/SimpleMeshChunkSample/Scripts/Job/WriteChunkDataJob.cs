using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace SimplestarGame
{
    /// <summary>
    /// 頂点カウントのジョブと処理が重なるが、こちらは確保済みのバッファに対してデータを書き込む処理
    /// fileVertexData が今回は InsetCube である場合を見越して、頂点カウントと同様に
    /// +X と -Y, -Z 方向の面側のカリングをしない判定となっている
    /// それ以外は頂点データをバッファに書き込んでいる
    /// </summary>
    [BurstCompile]
    unsafe public struct WriteChunkDataJob : IJobParallelFor
    {
        [ReadOnly] NativeArray<XYZ> xyz;
        [ReadOnly] NativeArray<int> vertexCounts;
        [ReadOnly] NativeArray<byte> voxelData;
        [ReadOnly] NativeArray<int> countOffsets;
        [ReadOnly] NativeArray<CustomVertexLayout> fileVertexData;
        [NativeDisableParallelForRestriction] NativeArray<CustomVertexLayout> vertexData;
        Vector3Int chunkOffset;
        int edgeMaxCubeIndex;
        int dataEdgeCubeCount;
        int d;
        half po;

        public WriteChunkDataJob(NativeArray<XYZ> xyz,
            NativeArray<int> vertexCounts, NativeArray<byte> voxelData, NativeArray<int> countOffsets, 
            NativeArray<CustomVertexLayout> fileVertexData, NativeArray<CustomVertexLayout> vertexData,
            Vector3Int chunkOffset, int edgeCubeCount, int dataEdgeCubeCount, int d, half po)
        {
            this.xyz = xyz;
            this.vertexCounts = vertexCounts;
            this.voxelData = voxelData;
            this.countOffsets = countOffsets;
            this.fileVertexData = fileVertexData;
            this.vertexData = vertexData;
            this.chunkOffset = chunkOffset;
            this.edgeMaxCubeIndex = edgeCubeCount;
            this.dataEdgeCubeCount = dataEdgeCubeCount;
            this.d = d;
            this.po = po;
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

        unsafe int WriteVertexData(
            int writeOffset, int readOffset, int count, XYZ xyz)
        {
            for (int v = 0; v < count; v++)
            {
                var vertex = this.fileVertexData[readOffset + v];
                var newPos = vertex.pos;
                newPos.x = (half)(newPos.x * this.d + (half)xyz.x + this.po);
                newPos.y = (half)(newPos.y * this.d + (half)xyz.y + this.po);
                newPos.z = (half)(newPos.z * this.d + (half)xyz.z + this.po);
                vertex.pos = newPos;
                this.vertexData[writeOffset + v] = vertex;
            }
            return count;
        }

        public void Execute(int index)
        {
            var xyz = this.xyz[index] * this.d;
            var x = xyz.x + chunkOffset.x;
            var y = xyz.y + chunkOffset.y;
            var z = xyz.z + chunkOffset.z;
            var e = this.edgeMaxCubeIndex > 2;

            if (this.GetVoxelValue(x, y, z) != 0)
            {
                int readOffset = 0;
                var writeOffset = this.countOffsets[index];
                for (int c = 0; c < this.vertexCounts.Length; c++)
                {
                    var count = this.vertexCounts[c];
                    if (c == CAWFile.PLUS_X)
                    {
                        if ((e || xyz.x != this.edgeMaxCubeIndex) && x + d != this.dataEdgeCubeCount)
                        {
                            if (this.GetVoxelValue(x + d, y, z) == 0)
                            {
                                writeOffset += this.WriteVertexData(writeOffset, readOffset, count, xyz);
                            }
                        }
                        else
                        {
                            writeOffset += this.WriteVertexData(writeOffset, readOffset, count, xyz);
                        }
                    }
                    else if (c == CAWFile.PLUS_Y)
                    {
                        if ((e || xyz.y != this.edgeMaxCubeIndex) && y + d != this.dataEdgeCubeCount)
                        {
                            if (this.GetVoxelValue(x, y + d, z) == 0)
                            {
                                writeOffset += this.WriteVertexData(writeOffset, readOffset, count, xyz);
                            }
                        }
                        else
                        {
                            writeOffset += this.WriteVertexData(writeOffset, readOffset, count, xyz);
                        }
                    }
                    else if (c == CAWFile.PLUS_Z)
                    {
                        if ((e || xyz.z != this.edgeMaxCubeIndex) && z + d != this.dataEdgeCubeCount)
                        {
                            if (this.GetVoxelValue(x, y, z + d) == 0)
                            {
                                writeOffset += this.WriteVertexData(writeOffset, readOffset, count, xyz);
                            }
                        }
                        else
                        {
                            writeOffset += this.WriteVertexData(writeOffset, readOffset, count, xyz);
                        }
                    }
                    else if (c == CAWFile.MINUS_X)
                    {
                        if ((e || xyz.x != 0) && x != 0)
                        {
                            if (this.GetVoxelValue(x - d, y, z) == 0)
                            {
                                writeOffset += this.WriteVertexData(writeOffset, readOffset, count, xyz);
                            }
                        }
                        else
                        {
                            writeOffset += this.WriteVertexData(writeOffset, readOffset, count, xyz);
                        }
                    }
                    else if (c == CAWFile.MINUS_Y)
                    {
                        if ((e || xyz.y != 0) && y != 0)
                        {
                            if (this.GetVoxelValue(x, y - d, z) == 0)
                            {
                                writeOffset += this.WriteVertexData(writeOffset, readOffset, count, xyz);
                            }
                        }
                        else
                        {
                            writeOffset += this.WriteVertexData(writeOffset, readOffset, count, xyz);
                        }
                    }
                    else if (c == CAWFile.MINUS_Z)
                    {
                        if ((e || xyz.z != 0) && z != 0)
                        {
                            if (this.GetVoxelValue(x, y, z - d) == 0)
                            {
                                writeOffset += this.WriteVertexData(writeOffset, readOffset, count, xyz);
                            }
                        }
                        else
                        {
                            writeOffset += this.WriteVertexData(writeOffset, readOffset, count, xyz);
                        }
                    }
                    else if (c == CAWFile.REMAIN)
                    {
                        writeOffset += this.WriteVertexData(writeOffset, readOffset, count, xyz);
                    }
                    readOffset += count;
                }
            }
        }
    }
}
