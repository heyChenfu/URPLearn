using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.Jobs;
using System;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.XR;
using RVO;

namespace BatchRendererGroupTest 
{
    // The PackedMatrix is a convenience type that converts matrices into
    // the format that Unity-provided SRP shaders expect.
    public struct PackedMatrix
    {
        public float c0x;
        public float c0y;
        public float c0z;
        public float c1x;
        public float c1y;
        public float c1z;
        public float c2x;
        public float c2y;
        public float c2z;
        public float c3x;
        public float c3y;
        public float c3z;

        public PackedMatrix(Matrix4x4 m)
        {
            c0x = m.m00;
            c0y = m.m10;
            c0z = m.m20;
            c1x = m.m01;
            c1y = m.m11;
            c1z = m.m21;
            c2x = m.m02;
            c2y = m.m12;
            c2z = m.m22;
            c3x = m.m03;
            c3y = m.m13;
            c3z = m.m23;
        }
    }

    public struct AgentData 
    {
        public int AgentId;
        public Vector3 TargetPosition;
        public Matrix4x4 Matrix;

        public AgentData(int agentId, Vector3 targetPosition, Matrix4x4 matrix)
        {
            AgentId = agentId;
            TargetPosition = targetPosition;
            Matrix = matrix;
        }
    }


    /// <summary>
    /// https://docs.unity3d.com/Manual/batch-renderer-group-creating-batches.html
    /// </summary>
    public class BatchRendererGroupTestMono : MonoBehaviour
    {
        [SerializeField]
        public Mesh ObjMesh;
        [SerializeField] 
        public Material ObjMaterial;
        [SerializeField]
        public int TestAmount;
        [SerializeField]
        public float4 RandomPostionRange;

        private BatchRendererGroup _BatchRendererGroup;
        BatchMeshID _meshID;
        BatchMaterialID _materialID;
        GraphicsBuffer _GPUPersistentInstanceData;
        BatchID _batchID;
        RandomMoveJob _randomMoveJob;
        JobHandle _jobHandle;
        bool _isJobSchedule = false;

        //NativeArray<Vector3> _targetPoints;
        //NativeArray<Matrix4x4> _matrices;
        NativeArray<AgentData> _agentDataArr;
        NativeArray<PackedMatrix> _obj2WorldArr;
        NativeArray<PackedMatrix> _world2ObjArr;

        // Some helper constants to make calculations more convenient.
        private const int kSizeOfMatrix = sizeof(float) * 4 * 4;
        private const int kSizeOfPackedMatrix = sizeof(float) * 4 * 3; //48
        private const int kSizeOfFloat4 = sizeof(float) * 4;
        private const int kBytesPerInstance = kSizeOfPackedMatrix * 2;
        private const int kExtraBytes = kSizeOfMatrix * 2;

        // Start is called before the first frame update
        void Start()
        {
            _BatchRendererGroup = new BatchRendererGroup(OnPerformCulling, IntPtr.Zero);
            _meshID = _BatchRendererGroup.RegisterMesh(ObjMesh);
            _materialID = _BatchRendererGroup.RegisterMaterial(ObjMaterial);
            AllocateInstanceDateBuffer();

            Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)DateTime.Now.Ticks);
            //_targetPoints = new NativeArray<Vector3>(TestAmount, Allocator.TempJob);
            //_matrices = new NativeArray<Matrix4x4>(TestAmount, Allocator.TempJob);
            _agentDataArr = new NativeArray<AgentData>(TestAmount, Allocator.TempJob);
            _obj2WorldArr = new NativeArray<PackedMatrix>(TestAmount, Allocator.TempJob);
            _world2ObjArr = new NativeArray<PackedMatrix>(TestAmount, Allocator.TempJob);
            for (int i = 0; i < TestAmount; ++i)
            {
                float tmpX = random.NextFloat(RandomPostionRange.x, RandomPostionRange.y);
                float tmpZ = random.NextFloat(RandomPostionRange.z, RandomPostionRange.w);
                Vector3 pos = new Vector3(tmpX, 0, tmpZ);
                int id = Simulator.Instance.addAgent(new RVO.Vector2(tmpX, tmpZ));
                _agentDataArr[i] = new AgentData(id, pos, Matrix4x4.Translate(pos));
            }
            _randomMoveJob = new RandomMoveJob
            {
                //TargetPoints = _targetPoints,
                //Matrices = _matrices,
                AgentDataArr = _agentDataArr,
                Obj2WorldArr = _obj2WorldArr,
                World2ObjArr = _world2ObjArr,
                RandomPostionRange = RandomPostionRange,
                random = random,
                DeltaTime = 0.03f,
            };

            PopulateInstanceDataBuffer();

        }

        // Update is called once per frame
        void Update()
        {
            if (!_isJobSchedule)
            {
                _jobHandle = _randomMoveJob.Schedule(TestAmount, 64);
                _isJobSchedule = true;
            }
            else if(_jobHandle != null && _jobHandle.IsCompleted)
            {
                _jobHandle.Complete();
                _isJobSchedule = false;

                // 更新 GraphicsBuffer 数据
                uint byteAddressObjectToWorld = kSizeOfPackedMatrix * 2;
                uint byteAddressWorldToObject = byteAddressObjectToWorld + kSizeOfPackedMatrix * (uint)TestAmount;
                _GPUPersistentInstanceData.SetData(_obj2WorldArr, 0, (int)(byteAddressObjectToWorld / kSizeOfPackedMatrix), _obj2WorldArr.Length);
                _GPUPersistentInstanceData.SetData(_world2ObjArr, 0, (int)(byteAddressWorldToObject / kSizeOfPackedMatrix), _world2ObjArr.Length);

                //for (int i = 0; i < TestAmount; ++i)
                //{
                //    _objArray[i].transform.position = _matrices[i].GetColumn(3);
                //    _objArray[i].transform.rotation = Quaternion.LookRotation(
                //        _matrices[i].GetColumn(2), _matrices[i].GetColumn(1));
                //    _objArray[i].transform.localScale = new Vector3(
                //        _matrices[i].GetColumn(0).magnitude,
                //        _matrices[i].GetColumn(1).magnitude,
                //        _matrices[i].GetColumn(2).magnitude
                //    );
                //}
            }

            Simulator.Instance.doStep();

        }

        private void OnDestroy()
        {
            _GPUPersistentInstanceData?.Dispose();
            if (_jobHandle != null)
                _jobHandle.Complete();
            //_targetPoints.Dispose();
            //_matrices.Dispose();
            _agentDataArr.Dispose();
            _obj2WorldArr.Dispose();
            _world2ObjArr.Dispose();

        }

        private unsafe JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext,
            BatchCullingOutput cullingOutput, IntPtr userContext)
        {
            // UnsafeUtility.Malloc() requires an alignment, so use the largest integer type's alignment which is a reasonable default.
            int alignment = UnsafeUtility.AlignOf<long>();

            // Acquire a pointer to the BatchCullingOutputDrawCommands struct so you can easily modify it directly.
            var drawCommands = (BatchCullingOutputDrawCommands*)cullingOutput.drawCommands.GetUnsafePtr();

            // Allocate memory for the output arrays. In a more complicated implementation, you would calculate
            // the amount of memory to allocate dynamically based on what is visible.
            // This example assumes that all of the instances are visible and thus allocates
            // memory for each of them. The necessary allocations are as follows:
            // - a single draw command (which draws kNumInstances instances)
            // - a single draw range (which covers our single draw command)
            // - kNumInstances visible instance indices.
            // You must always allocate the arrays using Allocator.TempJob.
            drawCommands->drawCommands = (BatchDrawCommand*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<BatchDrawCommand>(), alignment, Allocator.TempJob);
            drawCommands->drawRanges = (BatchDrawRange*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<BatchDrawRange>(), alignment, Allocator.TempJob);
            drawCommands->visibleInstances = (int*)UnsafeUtility.Malloc(TestAmount * sizeof(int), alignment, Allocator.TempJob);
            drawCommands->drawCommandPickingInstanceIDs = null;

            drawCommands->drawCommandCount = 1;
            drawCommands->drawRangeCount = 1;
            drawCommands->visibleInstanceCount = TestAmount;

            // This example doens't use depth sorting, so it leaves instanceSortingPositions as null.
            drawCommands->instanceSortingPositions = null;
            drawCommands->instanceSortingPositionFloatCount = 0;

            // Configure the single draw command to draw kNumInstances instances
            // starting from offset 0 in the array, using the batch, material and mesh
            // IDs registered in the Start() method. It doesn't set any special flags.
            drawCommands->drawCommands[0].visibleOffset = 0;
            drawCommands->drawCommands[0].visibleCount = (uint)TestAmount;
            drawCommands->drawCommands[0].batchID = _batchID;
            drawCommands->drawCommands[0].materialID = _materialID;
            drawCommands->drawCommands[0].meshID = _meshID;
            drawCommands->drawCommands[0].submeshIndex = 0;
            drawCommands->drawCommands[0].splitVisibilityMask = 0xff;
            drawCommands->drawCommands[0].flags = 0;
            drawCommands->drawCommands[0].sortingPosition = 0;

            // Configure the single draw range to cover the single draw command which is at offset 0.
            drawCommands->drawRanges[0].drawCommandsBegin = 0;
            drawCommands->drawRanges[0].drawCommandsCount = 1;

            // This example doesn't care about shadows or motion vectors, so it leaves everything
            // at the default zero values, except the renderingLayerMask which it sets to all ones
            // so Unity renders the instances regardless of mask settings.
            //设置 filterSettings 的 renderingLayerMask 为全1(0xffffffff),表示渲染所有层的实例
            drawCommands->drawRanges[0].filterSettings = new BatchFilterSettings { renderingLayerMask = 0xffffffff, };

            // Finally, write the actual visible instance indices to the array. In a more complicated
            // implementation, this output would depend on what is visible, but this example
            // assumes that everything is visible.
            for (int i = 0; i < TestAmount; ++i)
                drawCommands->visibleInstances[i] = i;

            // This simple example doesn't use jobs, so it returns an empty JobHandle.
            // Performance-sensitive applications are encouraged to use Burst jobs to implement
            // culling and draw command output. In this case, this function returns a
            // handle here that completes when the Burst jobs finish.
            return new JobHandle();

        }

        private void AllocateInstanceDateBuffer()
        {
            _GPUPersistentInstanceData = new GraphicsBuffer(GraphicsBuffer.Target.Raw,
                BufferCountForInstances(kBytesPerInstance, TestAmount, kExtraBytes),
                sizeof(int));
        }

        // Raw buffers are allocated in ints. This is a utility method that calculates
        // the required number of ints for the data.
        int BufferCountForInstances(int bytesPerInstance, int numInstances, int extraBytes = 0)
        {
            // Round byte counts to int multiples
            bytesPerInstance = (bytesPerInstance + sizeof(int) - 1) / sizeof(int) * sizeof(int);
            extraBytes = (extraBytes + sizeof(int) - 1) / sizeof(int) * sizeof(int);
            int totalBytes = bytesPerInstance * numInstances + extraBytes;
            return totalBytes / sizeof(int);
        }

        private void PopulateInstanceDataBuffer()
        {
            // Place a zero matrix at the start of the instance data buffer, so loads from address 0 return zero.
            var zero = new Matrix4x4[1] { Matrix4x4.zero };

            // Calculates start addresses for the different instanced properties. unity_ObjectToWorld starts
            // at address 96 instead of 64, because the computeBufferStartIndex parameter of SetData
            // is expressed as source array elements, so it is easier to work in multiples of sizeof(PackedMatrix).
            //96个字节预留大小, 前64为零矩阵, 保证从地址0读取为零, 后32未使用
            uint byteAddressObjectToWorld = kSizeOfPackedMatrix * 2; //48 * 2 = 96
            //tmpWorld2ObjArr数组在tmpObj2WorldArr之后, 所以为预留大小加上tmpWorld2ObjArr大小
            uint byteAddressWorldToObject = byteAddressObjectToWorld + kSizeOfPackedMatrix * (uint)TestAmount;

            //zero 矩阵上传到开始的64字节
            _GPUPersistentInstanceData.SetData(zero, 0, 0, 1);
            //把tmpObj2WorldArr从第0开始总共tmpObj2WorldArr.Length个元素传输到GraphicsBuffer的byteAddressObjectToWorld / kSizeOfPackedMatrix即第二个位置
            _GPUPersistentInstanceData.SetData(_obj2WorldArr, 0, (int)(byteAddressObjectToWorld / kSizeOfPackedMatrix), _obj2WorldArr.Length);
            _GPUPersistentInstanceData.SetData(_world2ObjArr, 0, (int)(byteAddressWorldToObject / kSizeOfPackedMatrix), _world2ObjArr.Length);

            // Set up metadata values to point to the instance data. Set the most significant bit 0x80000000 in each
            // which instructs the shader that the data is an array with one value per instance, indexed by the instance index.
            // Any metadata values that the shader uses that are not set here will be 0. When a value of 0 is used with
            // UNITY_ACCESS_DOTS_INSTANCED_PROP (i.e. without a default), the shader interprets the
            // 0x00000000 metadata value and loads from the start of the buffer. The start of the buffer is
            // a zero matrix so this sort of load is guaranteed to return zero, which is a reasonable default value.
            //MetadataValue 告诉 BatchRendererGroup 和 Shader 如何从 GraphicsBuffer 中读取数据
            var metadata = new NativeArray<MetadataValue>(2, Allocator.Temp);
            //Value是一个32 位无符号整数, 高位设置为0x80000000, 低位包含缓冲区的地址偏移量。0x80000000是一个特殊标志位, 它将最高位设置为1
            metadata[0] = new MetadataValue { NameID = Shader.PropertyToID("unity_ObjectToWorld"), Value = 0x80000000 | byteAddressObjectToWorld, };
            metadata[1] = new MetadataValue { NameID = Shader.PropertyToID("unity_WorldToObject"), Value = 0x80000000 | byteAddressWorldToObject, };

            _batchID = _BatchRendererGroup.AddBatch(metadata, _GPUPersistentInstanceData.bufferHandle);

        }

    }

}
