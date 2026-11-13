using System;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;

namespace EHelper.Services
{
    public static class MemoryOptimizer
    {
        [DllImport("kernel32.dll", EntryPoint = "SetProcessWorkingSetSize")]
        private static extern bool SetProcessWorkingSetSize(IntPtr proc, IntPtr min, IntPtr max);

        [DllImport("psapi.dll")]
        private static extern int EmptyWorkingSet(IntPtr hwProc);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        public static void TrimMemory()
        {
            try
            {
                // Compact Large Object Heap to defragment memory
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

                // Force full generation 0, 1 and 2 collection with heap compaction
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
                GC.WaitForPendingFinalizers();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

                // Release physical pages back to Windows memory manager using the kernel pseudo-handle
                IntPtr handle = GetCurrentProcess();
                EmptyWorkingSet(handle);
                SetProcessWorkingSetSize(handle, new IntPtr(-1), new IntPtr(-1));
            }
            catch
            {
                // Silently ignore memory trim exceptions
            }
        }
    }
}
