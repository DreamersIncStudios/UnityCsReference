// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace UnityEditor.Search
{
    static class TransactionUtils
    {
        public static byte[] Serialize<T>(T data)
            where T : struct
        {
            var size = Marshal.SizeOf<T>();
            var arr = new byte[size];

            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(data, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        public static byte[] Serialize<T>(IEnumerable<T> data)
        {
            var elementSize = Marshal.SizeOf<T>();
            var nbElement = data.Count();
            var size = nbElement * elementSize;
            var array = new byte[size];

            var ptr = Marshal.AllocHGlobal(elementSize);
            var offset = 0;
            foreach (var el in data)
            {
                Marshal.StructureToPtr(el, ptr, true);
                Marshal.Copy(ptr, array, offset, elementSize);
                offset += elementSize;
            }
            Marshal.FreeHGlobal(ptr);

            return array;
        }

        public static void SerializeInto<T>(T data, byte[] buffer, int offset)
            where T : struct
        {
            var size = Marshal.SizeOf<T>();
            if ((offset + size) > buffer.Length)
                throw new ArgumentException($"Not enough space in array of size {buffer.Length} to write {size} bytes at offset {offset}.");

            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(data, ptr, true);
            Marshal.Copy(ptr, buffer, offset, size);
            Marshal.FreeHGlobal(ptr);
        }

        public static T Deserialize<T>(byte[] bytes)
            where T : struct
        {
            var tr = new T();

            var size = Marshal.SizeOf<T>();
            var ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(bytes, 0, ptr, size);

            tr = (T)Marshal.PtrToStructure(ptr, tr.GetType());
            Marshal.FreeHGlobal(ptr);

            return tr;
        }

        public static T[] ArrayDeserialize<T>(byte[] bytes)
        {
            var dataSize = Marshal.SizeOf<T>();
            var nbData = bytes.Length / dataSize;
            if (bytes.Length % dataSize != 0)
                throw new ArgumentException($"Bytes has a size that is not a multiple of size of {typeof(T)}", nameof(bytes));

            var tr = new T[nbData];

            var ptr = Marshal.AllocHGlobal(dataSize);
            for (var i = 0; i < nbData; ++i)
            {
                Marshal.Copy(bytes, i * dataSize, ptr, dataSize);
                tr[i] = (T)Marshal.PtrToStructure(ptr, typeof(T));
            }
            Marshal.FreeHGlobal(ptr);

            return tr;
        }

        public static long ArrayDeserializeInto<T>(byte[] bytes, T[] data)
        {
            var dataSize = Marshal.SizeOf<T>();
            var nbData = bytes.Length / dataSize;
            if (bytes.Length % dataSize != 0)
                throw new ArgumentException($"Bytes has a size that is not a multiple of size of {typeof(T)}", nameof(bytes));

            var maxDataCount = Math.Min(nbData, data.Length);

            var ptr = Marshal.AllocHGlobal(dataSize);
            for (var i = 0; i < maxDataCount; ++i)
            {
                Marshal.Copy(bytes, i * dataSize, ptr, dataSize);
                data[i] = (T)Marshal.PtrToStructure(ptr, typeof(T));
            }
            Marshal.FreeHGlobal(ptr);

            return maxDataCount;
        }

        public static DateTime TimeStampFromByte(byte[] bytes)
        {
            var binaryTimeStamp = BitConverter.ToInt64(bytes, 0);
            return DateTime.FromBinary(binaryTimeStamp);
        }

        public static void ReadWholeArray(FileStream fs, byte[] data)
        {
            ReadIntoArray(fs, data, data.Length);
        }

        public static void ReadIntoArray(FileStream fs, byte[] data, int count)
        {
            var offset = 0;
            var remaining = count;
            while (remaining > 0)
            {
                var read = fs.Read(data, offset, remaining);
                if (read <= 0)
                    throw new EndOfStreamException($"End of stream reached with {remaining} bytes left to read");
                remaining -= read;
                offset += read;
            }
        }
    }
}
