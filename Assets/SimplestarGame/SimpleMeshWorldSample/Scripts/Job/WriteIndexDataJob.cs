using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace SimplestarGame
{
    /// <summary>
    /// 頂点インデックス書き込みジョブ
    /// </summary>
    [BurstCompile]
    public struct WriteIndexDataJob : IJobParallelFor
    {
        public NativeArray<int> indexData;
        public void Execute(int index)
        {
            this.indexData[index] = index;
        }
    }
}
