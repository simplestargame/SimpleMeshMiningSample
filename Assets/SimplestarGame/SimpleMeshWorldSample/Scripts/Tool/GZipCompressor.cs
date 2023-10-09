using System;
using System.IO;
using System.IO.Compression;

// 使い方
//var compressedData = GZipCompressor.Compress(rawData);
//var rawData = GZipCompressor.Decompress(compressedData);

namespace SimplestarGame
{
    /// <summary>
    /// gzip で byte[] の圧縮や解凍を行うクラス
    /// </summary>
    public static class GZipCompressor
    {
        /// <summary>
        /// 圧縮
        /// </summary>
        /// <param name="rawData">生データ</param>
        /// <returns>圧縮された byte 配列</returns>
        public static byte[] Compress(byte[] rawData)
        {
            using (MemoryStream compressedStream = new MemoryStream())
            {
                using (GZipStream gzipStream = new GZipStream(compressedStream, CompressionMode.Compress))
                using (BinaryWriter writer = new BinaryWriter(gzipStream))
                {
                    writer.Write(rawData);
                }

                byte[] compressedData = compressedStream.ToArray();
                return compressedData;
            }
        }

        public static byte[] Decompress(byte[] compressedData)
        {
            using (MemoryStream compressedStream = new MemoryStream(compressedData))
            using (MemoryStream decompressedStream = new MemoryStream())
            {
                using (GZipStream gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                {
                    gzipStream.CopyTo(decompressedStream);
                }

                return decompressedStream.ToArray();
            }
        }
    }
}