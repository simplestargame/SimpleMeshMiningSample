using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace SimplestarGame
{
    public class IndexXYZ
    {
        public NativeArray<XYZ> xyz;
        public NativeArray<int> countOffsets;
    }

    public class SimpleMeshMiningSample : MonoBehaviour
    {
        [SerializeField] string meshFileName = "BasicCube.caw";
        [SerializeField] string dataFileName = "world000.gz";
        [SerializeField] Material material;
        [SerializeField] WebGLUtil webGLUtil;
        [SerializeField] Transform mainCamera;
        [SerializeField] Transform[] parents;
        List<Vector3> interactPoints = new List<Vector3>();
        [SerializeField] Color[] levelColors = new Color[] { 
            Color.white * 0.2f, 
            Color.white * 0.3f, 
            Color.white * 0.4f, 
            Color.white * 0.5f, 
            Color.white * 0.6f, 
            Color.white * 0.7f, 
            Color.white * 0.8f, 
            Color.white * 0.9f,
            Color.white * 0.95f, 
            Color.white };

        /// <summary>
        /// ���[���h���\������ő嗱�x�`�����N
        /// </summary>
        List<SimpleMeshChunk> chunks = new List<SimpleMeshChunk>();
        SimpleMeshChunkBuilder chunkBuilder;

        CAWFile.CubeData cubeData;
        NativeArray<byte> voxelData;
        List<IndexXYZ> levelDataList;

        Task createMeshesTask = null;
        bool cancelCreateMeshes = false;

        float timer = 0f;
        float interval = 1f; // �b
        float gridSize = 128;
        Vector3Int cameraGridPosition;

        void Awake()
        {
            Application.targetFrameRate = 60;
            GraphicsSettings.useScriptableRenderPipelineBatching = true;
        }

        IEnumerator Start()
        {
            yield return this.webGLUtil.ReadFile(Path.Combine(Application.streamingAssetsPath, this.meshFileName));
            var cawData = this.webGLUtil.GetData();
            yield return this.webGLUtil.ReadFile(Path.Combine(Application.streamingAssetsPath, this.dataFileName));
            byte[] worldDataBytes = this.webGLUtil.GetData();
#if UNITY_EDITOR || !UNITY_WEBGL
            worldDataBytes = GZipCompressor.Decompress(worldDataBytes);
#endif
            this.Start2(cawData, worldDataBytes);
        }

        async void Start2(byte[] cawData, byte[] worldDataBytes)
        {
            this.cubeData = CAWFile.GetCAWFile(cawData);
            this.voxelData = await Task.Run(() => new NativeArray<byte>(worldDataBytes, Allocator.Persistent));
            // Job�pNativeArray�m��
            this.levelDataList = await AllocateDataAsync();
            // �r���_�[��������
            this.chunkBuilder = new SimpleMeshChunkBuilder(this.cubeData, this.voxelData, this.levelDataList, this.material, this.levelColors);

            // �`�����N�I�u�W�F�N�g���쐬�A���X�g��
            this.chunks.Clear();
            foreach (var parent in this.parents)
            {
                var myChunkLevel = ChunkLevel.Cube256;
                var edgeCubes = SimpleMeshChunk.levelEdgeCubes[(int)myChunkLevel];
                for (int chunkX = 0; chunkX < 1; chunkX++)
                {
                    for (int chunkY = 0; chunkY < 1; chunkY++)
                    {
                        for (int chunkZ = 0; chunkZ < 1; chunkZ++)
                        {
                            var chunkOffset = new Vector3Int(chunkX, chunkY, chunkZ) * edgeCubes;
                            var newGameObject = new GameObject($"{chunkX}, {chunkY}, {chunkZ}");
                            newGameObject.transform.SetParent(parent.transform, false);
                            newGameObject.transform.localPosition = chunkOffset;
                            newGameObject.isStatic = true;
                            var chunk = newGameObject.AddComponent<SimpleMeshChunk>();
                            chunk.SetData(myChunkLevel, chunkOffset);
                            this.chunks.Add(chunk);
                        }
                    }
                }
            }
            this.createMeshesTask = this.CreateWorldMeshes(this.chunks, new Vector3[0]);
        }

        void OnDestroy()
        {
            // �m�ۂ������̂��J��
            foreach (var levelData in this.levelDataList)
            {
                levelData.countOffsets.Dispose();
                levelData.xyz.Dispose();
            }
            this.voxelData.Dispose();
            this.cubeData.vertexData.Dispose();
            this.cubeData.vertexCounts.Dispose();
        }

        async void Update()
        {
            // �^�C�}�[���X�V
            this.timer += Time.deltaTime;

            // �^�C�}�[���w�肵���Ԋu�𒴂����ꍇ�ɏ��������s
            if (this.timer >= this.interval)
            {
                var lastGridPosition = this.cameraGridPosition;
                this.cameraGridPosition = this.CalculateGridPosition(this.mainCamera.position);
                if (lastGridPosition != this.cameraGridPosition)
                {
                    await this.ReBuildMesh();
                }
                // �^�C�}�[�����Z�b�g
                this.timer = 0f;
            }

            // Space �L�[�������ƁA�����|�C���g�t�߂̃��b�V�����č\�z
            if (Input.GetKeyDown(KeyCode.Space))
            {
                await this.ReBuildMesh();
            }

            // �N���b�N�܂��̓^�b�v���ꂽ���_���v�Z����
            if (Input.GetMouseButtonDown(0)) // ���N���b�N�Ō�_���v�Z
            {
                // �}�E�X�|�C���^�[�̈ʒu���擾
                Vector3 mousePosition = Input.mousePosition;
                var mainCamera = this.mainCamera.GetComponent<Camera>();

                // �}�E�X�|�C���^�[�̈ʒu���J��������̋����ɕϊ�
                mousePosition.z = mainCamera.nearClipPlane;

                // �}�E�X�|�C���^�[�̈ʒu�����[���h���W�ɕϊ�
                Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);

                // �J��������}�E�X�|�C���^�[�̈ʒu�Ɍ��������C���쐬
                Ray ray = new Ray(mainCamera.transform.position, worldPosition - mainCamera.transform.position);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    var p = hit.point + ray.direction * 0.5f;
                    this.interactPoints.Add(new Vector3(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), Mathf.RoundToInt(p.z)));
                    if (this.interactPoints.Count > 10)
                    {
                        this.interactPoints.RemoveAt(0);
                    }
                    await this.ReBuildMesh();
                }
            }
        }

        Vector3Int CalculateGridPosition(Vector3 position)
        {
            // �O���b�h�̃Z���T�C�Y�ɍ��킹�Ĉʒu��؂�̂ĂČv�Z
            int x = Mathf.FloorToInt(position.x / this.gridSize);
            int y = Mathf.FloorToInt(position.y / this.gridSize);
            int z = Mathf.FloorToInt(position.z / this.gridSize);

            return new Vector3Int(x, y, z);
        }

        async Task ReBuildMesh()
        {
            while (this.createMeshesTask != null && !this.createMeshesTask.IsCompleted)
            {
                this.cancelCreateMeshes = true;
                await Task.Delay(100);
            }
            this.createMeshesTask = this.CreateWorldMeshes(this.chunks, this.interactPoints.ToArray());
        }

        /// <summary>
        /// �`�����N�����ԂɃ��b�V���I�u�W�F�N�g��
        /// </summary>
        /// <param name="chunks">�\�[�g���ꂽ�`�����N�ꗗ</param>
        /// <param name="interactPoints">�����|�C���g���W�ꗗ</param>
        /// <returns>async Task</returns>
        async Task CreateWorldMeshes(List<SimpleMeshChunk> chunks, Vector3[] interactPoints)
        {
            this.cancelCreateMeshes = false;
            // ���݂̃`�����N�ꗗ���J�����ɋ߂����Ƀ\�[�g
            var mainCamera = Camera.main.transform;
            await this.SortChunksAsync(chunks, mainCamera.position, mainCamera.forward);
            List<GameObject> meshObjectList = new List<GameObject>();
            foreach (var chunk in chunks)
            {
                if (this.cancelCreateMeshes)
                {
                    break;
                }
                if (chunk.dot < 0f && chunk.distance > 256 && chunk.cubeSize >= CubeSize.Size2)
                {
                    // �\���s�v�Ȃ��̂̓X�L�b�v
                    continue;
                }
                await this.chunkBuilder.CreateChunkMesh(meshObjectList, chunk, interactPoints, true);

                this.RemoveUnitCubeObjects(chunk, interactPoints);
            }
            await this.chunkBuilder.BakeMeshAsync(meshObjectList, true);
        }

        void RemoveUnitCubeObjects(SimpleMeshChunk chunk, Vector3[] interactPoints)
        {
            if (chunk.children != null)
            {
                foreach (var child in chunk.children)
                {
                    this.RemoveUnitCubeObjects(child, interactPoints);
                }
            }
            if (chunk.chunkLevel == ChunkLevel.Cube1 && chunk.meshObject != null && this.chunkBuilder.IsNearInteractPoints(chunk, interactPoints, 2f))
            {
                this.voxelData[chunk.offset.x * SimpleMeshChunk.dataEdgeCubeCount * SimpleMeshChunk.dataEdgeCubeCount +
                    chunk.offset.y * SimpleMeshChunk.dataEdgeCubeCount + chunk.offset.z] = 0;
                var unitCube = chunk.meshObject;
                unitCube.GetComponent<MeshCollider>().convex = true;
                unitCube.AddComponent<Rigidbody>();
                unitCube.transform.SetParent(null, true);
                chunk.meshObject = null;
                StartCoroutine(this.CoDestroyCube(unitCube, 30f));
            }
        }

        IEnumerator CoDestroyCube(GameObject unitCube, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (unitCube.TryGetComponent(out MeshFilter meshFilter))
            {
                if (null != meshFilter.sharedMesh)
                {
                    meshFilter.sharedMesh.Clear();
                }
                Destroy(meshFilter.sharedMesh);
            }
            Destroy(unitCube);
        }

        async Task SortChunksAsync(List<SimpleMeshChunk> chunks, Vector3 viewPoint, Vector3 viewDirection)
        {
            await Task.Run(() => { SortChunks(chunks, viewPoint, viewDirection); });
        }

        static void SortChunks(List<SimpleMeshChunk> chunks, Vector3 viewPoint, Vector3 viewDirection)
        {
            var points = new NativeArray<float3>(chunks.Count, Allocator.Persistent);
            var cameraDistances = new NativeArray<DotDistance>(chunks.Count, Allocator.Persistent);
            for (int i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                points[i] = chunk.center;
            }
            var calculateCameraDistanceJob = new CalculateDotDistanceJob()
            {
                points = points,
                viewPoint = viewPoint,
                viewDirection = viewDirection,
                dotDistances = cameraDistances
            };
            calculateCameraDistanceJob.Schedule(points.Length, 1).Complete();
            for (int i = 0; i < cameraDistances.Length; i++)
            {
                var cameraDistance = cameraDistances[i];
                chunks[i].distance = cameraDistance.distance;
                chunks[i].dot = cameraDistance.dot;
            }
            cameraDistances.Dispose();
            points.Dispose();
            chunks.Sort((a, b) => a.distance > b.distance ? 1 : -1);
        }

        /// <summary>
        /// �v�Z�Ŗ���g���o�b�t�@�A�g���܂킷���߂ɍŏ��Ɋm��
        /// </summary>
        /// <returns>�m�ۂ����o�b�t�@</returns>
        static async Task<List<IndexXYZ>> AllocateDataAsync()
        {
            return await Task.Run(() => {
                List<IndexXYZ> levelDataList = new List<IndexXYZ>();
                for (ChunkLevel chunkLevel = ChunkLevel.Cube1; chunkLevel <= ChunkLevel.Cube256; chunkLevel++)
                {
                    var edgeCubeCount = SimpleMeshChunk.levelEdgeCubes[(int)chunkLevel];
                    var size = edgeCubeCount * edgeCubeCount * edgeCubeCount;
                    var xyz = new NativeArray<XYZ>(size, Allocator.Persistent);
                    var countOffsets = new NativeArray<int>(size, Allocator.Persistent);
                    for (int x = 0; x < edgeCubeCount; x++)
                    {
                        for (int y = 0; y < edgeCubeCount; y++)
                        {
                            for (int z = 0; z < edgeCubeCount; z++)
                            {
                                var index = x * edgeCubeCount * edgeCubeCount + y * edgeCubeCount + z;
                                xyz[index] = new XYZ { x = (byte)x, y = (byte)y, z = (byte)z };
                                countOffsets[index] = 0;
                            }
                        }
                    }
                    levelDataList.Add(new IndexXYZ { xyz = xyz, countOffsets = countOffsets });
                }
                return levelDataList;
            });
        }
    }
}