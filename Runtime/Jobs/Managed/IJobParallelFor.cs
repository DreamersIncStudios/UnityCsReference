// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Jobs
{
    [JobProducerType(typeof(IJobParallelForExtensions.ParallelForJobStruct<>))]
    public interface IJobParallelFor
    {
        void Execute(int index);
    }

    public static class IJobParallelForExtensions
    {
        internal struct ParallelForJobStruct<T> where T : struct, IJobParallelFor
        {
            public static readonly IntPtr jobReflectionData = JobsUtility.CreateJobReflectionData(typeof(T), (ExecuteJobFunction)Execute);

            public delegate void ExecuteJobFunction(ref T data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            public static unsafe void Execute(ref T jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                while (true)
                {
                    int begin;
                    int end;
                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                        break;

                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), begin, end - begin);

                    var endThatCompilerCanSeeWillNeverChange = end;
                    for (var i = begin; i < endThatCompilerCanSeeWillNeverChange; ++i)
                        jobData.Execute(i);
                }
            }
        }

        unsafe public static JobHandle Schedule<T>(this T jobData, int arrayLength, int innerloopBatchCount, JobHandle dependsOn = new JobHandle()) where T : struct, IJobParallelFor
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), ParallelForJobStruct<T>.jobReflectionData, dependsOn, ScheduleMode.Parallel);
            return JobsUtility.ScheduleParallelFor(ref scheduleParams, arrayLength, innerloopBatchCount);
        }

        unsafe public static void Run<T>(this T jobData, int arrayLength) where T : struct, IJobParallelFor
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), ParallelForJobStruct<T>.jobReflectionData, new JobHandle(), ScheduleMode.Run);
            JobsUtility.ScheduleParallelFor(ref scheduleParams, arrayLength, arrayLength);
        }
    }
}
