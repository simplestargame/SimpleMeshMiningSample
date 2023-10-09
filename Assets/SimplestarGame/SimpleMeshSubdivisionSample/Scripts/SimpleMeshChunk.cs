using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace SimplestarGame
{
    public enum ChunkLevel
    {
        Cube1 = 0,
        Cube2,
        Cube4,
        Cube8,
        Cube16,
        Cube32,
        Cube64,
        Cube128,
        Cube256
    }

    public enum CubeSize
    {
        Size1 = 0,
        Size2,
        Size4,
    }

    public class SimpleMeshChunk : MonoBehaviour
    {
        public const int dataEdgeCubeCount = 256;
        public static readonly int[] levelEdgeCubes = new int[] { 1, 2, 4, 8, 16, 32, 64, 128, 256 };
        public static readonly CubeSize[] levelCubeSizes = new CubeSize[] { CubeSize.Size1, CubeSize.Size1, CubeSize.Size1, CubeSize.Size1, CubeSize.Size1, CubeSize.Size1, CubeSize.Size1, CubeSize.Size1, CubeSize.Size2 };
        public static readonly half[] cubeSizeToOffset = new half[] { (half)0, (half)0.5, (half)1.5 };

        public ChunkLevel chunkLevel;
        public CubeSize cubeSize;
        public Vector3Int offset;
        public Vector3 minBounds;
        public Vector3 maxBounds;
        public Vector3 center;

        public GameObject meshObject;
        public List<SimpleMeshChunk> children;
 
        // Job実行結果を格納する
        public float distance;
        public float dot;

        public void SetData(ChunkLevel chunkLevel, Vector3Int chunkOffset)
        {
            this.chunkLevel = chunkLevel;
            var edgeCubes = levelEdgeCubes[(int)(chunkLevel)];
            this.offset = chunkOffset;
            this.minBounds = this.transform.position - Vector3Int.one;
            this.maxBounds = this.transform.position + Vector3Int.one * (edgeCubes + 1);
            this.center = (this.maxBounds + this.minBounds) / 2f;
        }

        public void DestroyMesh()
        {
            if (this.meshObject != null)
            {
                if (this.meshObject.TryGetComponent(out MeshFilter meshFilter))
                {
                    if (null != meshFilter.sharedMesh)
                    {
                        meshFilter.sharedMesh.Clear();
                    }
                    Destroy(meshFilter.sharedMesh);
                }
                Destroy(this.meshObject);
                this.meshObject = null;
            }
        }

        public void DestroyChildren()
        {
            if (this.children != null)
            {
                foreach (var child in this.children)
                {
                    child.DestroyMesh();
                    child.DestroyChildren();
                    Destroy(child.gameObject);
                }
                this.children.Clear();
                this.children = null;
            }
        }
    }
}
