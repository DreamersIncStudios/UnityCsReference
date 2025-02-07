// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngineInternal;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngineInternal.Input
{
    [NativeHeader("Modules/Input/Private/InputModuleBindings.h")]
    [NativeHeader("Modules/Input/Private/InputInternal.h")]
    internal partial class NativeInputSystem
    {
        internal static extern bool hasDeviceDiscoveredCallback { set; }

        public static extern double currentTime { get; }

        public static extern double currentTimeOffsetToRealtimeSinceStartup { get; }

        [FreeFunction("AllocateInputDeviceId")]
        public static extern int AllocateDeviceId();

        // C# doesn't allow taking the address of a value type because of pinning requirements for the heap.
        // And our bindings generator doesn't support overloading. So ugly code following here...
        public static unsafe void QueueInputEvent<TInputEvent>(ref TInputEvent inputEvent)
            where TInputEvent : struct
        {
            QueueInputEvent((IntPtr)UnsafeUtility.AddressOf<TInputEvent>(ref inputEvent));
        }

        public static extern void QueueInputEvent(IntPtr inputEvent);

        public static extern long IOCTL(int deviceId, int code, IntPtr data, int sizeInBytes);

        public static extern void SetPollingFrequency(float hertz);

        public static extern void Update(NativeInputUpdateType updateType);

        internal static extern ulong GetBackgroundEventBufferSize();

        [Obsolete("This is not needed any longer.")]
        public static void SetUpdateMask(NativeInputUpdateType mask)
        {
        }

        [StaticAccessor("InputModuleBindings::Android", StaticAccessorType.DoubleColon)]
        internal class Android
        {
            /// <summary>
            /// Allows processing of input events from null devices.
            /// Events coming from Android's instrumentation will sometimes have MotionEvent.GetDevice return null. For ex., happens on NVIDIA Shield
            /// Primary usage are tests, where we want our events to be handled.
            /// By default, this behavior is disabled, since Gear VR expects Unity to ignore such events.
            /// </summary>
            [NativeProperty("AllowNullInputDevices")]
            internal static extern bool allowNullDevices { get; set; }
        }
    }
}
