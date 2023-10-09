using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SimplestarGame
{
    /// <summary>
    /// ƒJƒƒ‰²‚Æ‚Ì“àÏ‚ÆƒJƒƒ‰‚©‚ç‚Ì‹——£‚ğŒvZ
    /// </summary>
    [BurstCompile]
    public struct CalculateDotDistanceJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> points;
        [ReadOnly] public float3 viewPoint;
        [ReadOnly] public float3 viewDirection;
        public NativeArray<DotDistance> dotDistances;
        
        public void Execute(int index)
        {
            this.dotDistances[index] = new DotDistance
            {
                distance = math.distance(this.viewPoint, this.points[index]),
                dot = math.dot(this.viewDirection, math.normalize(this.points[index] - this.viewPoint))
            };
        }
    }
    public struct DotDistance
    {
        public float distance;
        public float dot;
    }
}