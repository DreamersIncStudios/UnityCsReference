// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using System.Diagnostics;
using UnityEngine.Scripting;

namespace Unity.Jobs.LowLevel.Unsafe
{
    [AttributeUsage(AttributeTargets.Interface)]
    public sealed class JobProducerTypeAttribute : Attribute
    {
        public Type ProducerType { get; }

        public JobProducerTypeAttribute(Type producerType)
        {
            ProducerType = producerType;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct JobRanges
    {
        internal int    BatchSize;
        internal int    NumJobs;
        public   int    TotalIterationCount;
        internal int    NumPhases;

        internal IntPtr StartEndIndex;
        internal IntPtr PhaseData;
    }

    public enum ScheduleMode
    {
        Run          = 0,
        [Obsolete("Batched is obsolete, use Parallel or Single depending on job type. (UnityUpgradable) -> Parallel", false)]
        Batched      = 1,
        Parallel     = 1,
        Single       = 2,
    }

    [Obsolete("Reflection data is now universal between job types. The parameter can be removed.", false)]
    public enum JobType
    {
        Single      = 0,
        ParallelFor = 1
    }

    [NativeType(Header = "Runtime/Jobs/ScriptBindings/JobsBindings.h")]
    [NativeHeader("Runtime/Jobs/JobSystem.h")]
    public static class JobsUtility
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct JobScheduleParameters
        {
            public JobHandle Dependency;
            public int ScheduleMode;
            public IntPtr ReflectionData;
            public IntPtr JobDataPtr;

            unsafe public JobScheduleParameters(void* i_jobData, IntPtr i_reflectionData, JobHandle i_dependency, ScheduleMode i_scheduleMode)
            {
                Dependency = i_dependency;
                JobDataPtr = (IntPtr)i_jobData;
                ReflectionData = i_reflectionData;
                ScheduleMode = (int)i_scheduleMode;
            }
        }

        public static unsafe void GetJobRange(ref JobRanges ranges, int jobIndex, out int beginIndex, out int endIndex)
        {
            int* startEndIndices = (int*)ranges.StartEndIndex;
            beginIndex = startEndIndices[jobIndex * 2];
            endIndex = startEndIndices[jobIndex * 2 + 1];
        }

        [NativeMethod(IsFreeFunction = true, IsThreadSafe = true)]
        public static extern bool GetWorkStealingRange(ref JobRanges ranges, int jobIndex, out int beginIndex, out int endIndex);

        [FreeFunction("ScheduleManagedJob", ThrowsException = true, IsThreadSafe = true)]
        public static extern JobHandle Schedule(ref JobScheduleParameters parameters);

        [FreeFunction("ScheduleManagedJobParallelFor", ThrowsException = true, IsThreadSafe = true)]
        public static extern JobHandle ScheduleParallelFor(ref JobScheduleParameters parameters, int arrayLength, int innerloopBatchCount);

        [FreeFunction("ScheduleManagedJobParallelForDeferArraySize", ThrowsException = true, IsThreadSafe = true)]
        unsafe public static extern JobHandle ScheduleParallelForDeferArraySize(ref JobScheduleParameters parameters, int innerloopBatchCount, void* listData, void* listDataAtomicSafetyHandle);

        [FreeFunction("ScheduleManagedJobParallelForTransform", ThrowsException = true)]
        public static extern JobHandle ScheduleParallelForTransform(ref JobScheduleParameters parameters, IntPtr transfromAccesssArray);

        [FreeFunction("ScheduleManagedJobParallelForTransformReadOnly", ThrowsException = true)]
        public static extern JobHandle ScheduleParallelForTransformReadOnly(ref JobScheduleParameters parameters, IntPtr transfromAccesssArray, int innerloopBatchCount);

        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        unsafe public static extern void PatchBufferMinMaxRanges(IntPtr bufferRangePatchData, void* jobdata, int startIndex, int rangeSize);

        [FreeFunction(ThrowsException = true, IsThreadSafe = true)]
        private static extern IntPtr CreateJobReflectionData(Type wrapperJobType, Type userJobType, object managedJobFunction0, object managedJobFunction1, object managedJobFunction2);

        [Obsolete("JobType is obsolete. The parameter should be removed. (UnityUpgradable) -> !1")]
        public static IntPtr CreateJobReflectionData(Type type, JobType jobType, object managedJobFunction0, object managedJobFunction1 = null, object managedJobFunction2 = null)
        {
            return CreateJobReflectionData(type, type, managedJobFunction0, managedJobFunction1, managedJobFunction2);
        }

        public static IntPtr CreateJobReflectionData(Type type, object managedJobFunction0, object managedJobFunction1 = null, object managedJobFunction2 = null)
        {
            return CreateJobReflectionData(type, type, managedJobFunction0, managedJobFunction1, managedJobFunction2);
        }

        [Obsolete("JobType is obsolete. The parameter should be removed. (UnityUpgradable) -> !2")]
        public static IntPtr CreateJobReflectionData(Type wrapperJobType, Type userJobType, JobType jobType, object managedJobFunction0)
        {
            return CreateJobReflectionData(wrapperJobType, userJobType, managedJobFunction0, null, null);
        }

        public static IntPtr CreateJobReflectionData(Type wrapperJobType, Type userJobType, object managedJobFunction0)
        {
            return CreateJobReflectionData(wrapperJobType, userJobType, managedJobFunction0, null, null);
        }

        public static extern bool IsExecutingJob { [NativeMethod(IsFreeFunction = true, IsThreadSafe = true)] get; }

        public static extern bool JobDebuggerEnabled { [FreeFunction] get; [FreeFunction] set; }
        public static extern bool JobCompilerEnabled { [FreeFunction] get; [FreeFunction] set; }

        [FreeFunction("JobSystem::GetJobQueueWorkerThreadCount")]
        static extern int GetJobQueueWorkerThreadCount();

        [FreeFunction("JobSystem::ForceSetJobQueueWorkerThreadCount")]
        static extern void SetJobQueueMaximumActiveThreadCount(int count);

        public static extern int JobWorkerMaximumCount
        {
            [FreeFunction("JobSystem::GetJobQueueMaximumThreadCount")]
            get;
        }

        [FreeFunction("JobSystem::ResetJobQueueWorkerThreadCount")]
        public static extern void ResetJobWorkerCount();

        public static int JobWorkerCount
        {
            get { return GetJobQueueWorkerThreadCount(); }
            set
            {
                if ((value < 0) || (value > JobsUtility.JobWorkerMaximumCount))
                {
                    throw new ArgumentOutOfRangeException("JobWorkerCount", $"Invalid JobWorkerCount {value} must be in the range 0 -> {JobsUtility.JobWorkerMaximumCount}");
                }
                SetJobQueueMaximumActiveThreadCount(value);
            }
        }

        //@TODO: @timj Should we decrease this???
        public const int MaxJobThreadCount = 128;
        public const int CacheLineSize = 64;

        [FreeFunction("JobDebuggerGetSystemIdCellPtr")]
        internal static extern IntPtr GetSystemIdCellPtr();

        [FreeFunction("JobDebuggerClearSystemIds")]
        internal static extern void ClearSystemIds();

        [FreeFunction("JobDebuggerGetSystemIdMappings")]
        internal static unsafe extern int GetSystemIdMappings(JobHandle* handles, int* systemIds, int maxCount);

        internal delegate void PanicFunction_();

        internal static PanicFunction_ PanicFunction;

        [RequiredByNativeCode]
        private static void InvokePanicFunction()
        {
            var func = PanicFunction;
            if (func == null)
                return;
            func();
        }
    }
}
