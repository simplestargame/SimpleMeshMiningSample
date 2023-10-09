using System;
using System.IO;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace SimplestarGame
{
    /// <summary>
    /// .caw ファイル
    /// </summary>
    public class CAWFile
    {
        /// <summary>
        /// データの読み込み
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <returns>キューブデータ</returns>
        public static CubeData ReadCAWFile(string filePath)
        {
            var data = File.ReadAllBytes(filePath);
            return GetCAWFile(data);
        }

        /// <summary>
        /// データの取得
        /// </summary>
        /// <param name="data">データ</param>
        /// <returns>キューブデータ</returns>
        public static CubeData GetCAWFile(byte[] data)
        {
            NativeArray<int> vertexCounts = new NativeArray<int>(7, Allocator.Persistent);
            var fileSize = data.Length;
            return new CubeData
            {
                vertexCounts = vertexCounts,
                vertexData = UnsafeReadCAWFile(fileSize, data, vertexCounts)
            };
        }

        /// <summary>
        /// ファイルから頂点データの読み込み
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <param name="vertexCounts">成功時、Disposeする必要あり</param>
        /// <param name="vertexData">成功時、Disposeする必要あり</param>
        /// <returns>成功時true</returns>
        static unsafe NativeArray<CustomVertexLayout> UnsafeReadCAWFile(int fileSize, byte[] data, NativeArray<int> vertexCounts)
        {
            var headerByteCount = 4 + 7 * sizeof(int);
            var vertexCount = (fileSize - headerByteCount) / sizeof(CustomVertexLayout);
            var magicCode = new NativeArray<byte>(4, Allocator.Persistent);
            var vertexData = new NativeArray<CustomVertexLayout>(vertexCount, Allocator.Persistent);
            using (BinaryReader reader = new BinaryReader(new MemoryStream(data)))
            {
                reader.Read(new Span<byte>(magicCode.GetUnsafePtr(), magicCode.Length));
                if (magicCode[0] == 'c' && magicCode[1] == 'a' && magicCode[2] == 'w')
                {
                    reader.Read(new Span<byte>(vertexCounts.GetUnsafePtr(), vertexCounts.Length * sizeof(int)));
                    reader.Read(new Span<byte>(vertexData.GetUnsafePtr(), vertexData.Length * sizeof(CustomVertexLayout)));
                }
            }
            magicCode.Dispose();
            return vertexData;
        }

        public class CubeData
        {
            public NativeArray<int> vertexCounts;
            public NativeArray<CustomVertexLayout> vertexData;
        }

        /// <summary>
        /// キューブの +X 方向に面するう頂点で作られている三角形リストを意味する
        /// </summary>
        public const int PLUS_X = 0;
        /// <summary>
        /// キューブの +Y 方向に面するう頂点で作られている三角形リストを意味する
        /// </summary>
        public const int PLUS_Y = 1;
        /// <summary>
        /// キューブの +Z 方向に面するう頂点で作られている三角形リストを意味する
        /// </summary>
        public const int PLUS_Z = 2;
        /// <summary>
        /// キューブの -X 方向に面するう頂点で作られている三角形リストを意味する
        /// </summary>
        public const int MINUS_X = 3;
        /// <summary>
        /// キューブの -Y 方向に面するう頂点で作られている三角形リストを意味する
        /// </summary>
        public const int MINUS_Y = 4;
        /// <summary>
        /// キューブの -Z 方向に面するう頂点で作られている三角形リストを意味する
        /// </summary>
        public const int MINUS_Z = 5;
        /// <summary>
        /// キューブの上記いずれの方向にも面していない頂点で作られている三角形リストを意味する
        /// </summary>
        public const int REMAIN = 6;
    }
}
