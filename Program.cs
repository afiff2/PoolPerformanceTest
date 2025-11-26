using System;
using System.Buffers;
using System.Diagnostics;

public class PoolPerformanceTest
{
    // 高强度测试参数（根据你的 96GB 内存可进一步调高）
    private const int Iterations = 5_000_000; // 可根据需要调整

    // SOH < 85,000 bytes
    private const int SOH_ArraySize = 65_536; // 64 KB

    // LOH >= 85,000 bytes
    private const int LOH_ArraySize = 1_048_576; // 1 MB

    public static void Main(string[] args)
    {
        Console.WriteLine("内存池性能压测（含 SOH/LOH + GC 统计）");
        Console.WriteLine($"Iterations: {Iterations:N0}");
        Console.WriteLine($"SOH Array Size: {SOH_ArraySize:N0} bytes ({SOH_ArraySize / 1024} KB)");
        Console.WriteLine($"LOH Array Size: {LOH_ArraySize:N0} bytes ({LOH_ArraySize / 1024 / 1024} MB)");
        Console.WriteLine("--------------------------------------------------\n");

        // Warm-up
        Console.WriteLine("预热阶段...");
        RunWithoutPool(SOH_ArraySize, 1000);
        RunWithPool(SOH_ArraySize, 1000);
        RunWithoutPool(LOH_ArraySize, 100);
        RunWithPool(LOH_ArraySize, 100);
        Console.WriteLine("预热完成。\n");

        // Run tests
        RunTest("SOH (64KB)", SOH_ArraySize);
        Console.WriteLine();
        RunTest("LOH (1MB)", LOH_ArraySize);
    }

    private static void RunTest(string label, int arraySize)
    {
        Console.WriteLine($"=== {label} 测试 ===");

        ForceGcAndWait();
        var gcStart = GetGcStats();
        var memStart = GC.GetTotalMemory(true);

        long sum1 = RunWithoutPool(arraySize, Iterations);
        var gcNoPool = GetGcStats();
        var memNoPool = GC.GetTotalMemory(true);
        var timeNoPool = Benchmark(() => RunWithoutPool(arraySize, Iterations)); // warm path, so 2nd run is more accurate

        ForceGcAndWait();
        var gcStart2 = GetGcStats();
        var memStart2 = GC.GetTotalMemory(true);

        long sum2 = RunWithPool(arraySize, Iterations);
        var gcWithPool = GetGcStats();
        var memWithPool = GC.GetTotalMemory(true);
        var timeWithPool = Benchmark(() => RunWithPool(arraySize, Iterations));

        Console.WriteLine($"[不使用池] 耗时: {timeNoPool} ms | GC: Gen0={gcNoPool.Gen0 - gcStart.Gen0}, Gen1={gcNoPool.Gen1 - gcStart.Gen1}, Gen2={gcNoPool.Gen2 - gcStart.Gen2} | Heap Delta: {(memNoPool - memStart) / 1024 / 1024:N1} MB");
        Console.WriteLine($"[使用池]   耗时: {timeWithPool} ms | GC: Gen0={gcWithPool.Gen0 - gcStart2.Gen0}, Gen1={gcWithPool.Gen1 - gcStart2.Gen1}, Gen2={gcWithPool.Gen2 - gcStart2.Gen2} | Heap Delta: {(memWithPool - memStart2) / 1024 / 1024:N1} MB");
        Console.WriteLine($"结果校验: {sum1 == sum2} (无池={sum1}, 有池={sum2})");
    }

    private static long RunWithoutPool(int arraySize, int iterations)
    {
        long total = 0;
        for (int i = 0; i < iterations; i++)
        {
            byte[] buffer = new byte[arraySize];
            buffer[0] = (byte)i;
            buffer[arraySize / 2] = (byte)(i >> 8);
            total += buffer[0] + buffer[arraySize / 2];
        }
        return total;
    }

    private static long RunWithPool(int arraySize, int iterations)
    {
        long total = 0;
        for (int i = 0; i < iterations; i++)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(arraySize);
            try
            {
                buffer[0] = (byte)i;
                buffer[arraySize / 2] = (byte)(i >> 8);
                total += buffer[0] + buffer[arraySize / 2];
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        return total;
    }

    private static (int Gen0, int Gen1, int Gen2) GetGcStats()
    {
        return (GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2));
    }

    private static void ForceGcAndWait()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private static long Benchmark(Action action)
    {
        ForceGcAndWait();
        var sw = Stopwatch.StartNew();
        action();
        sw.Stop();
        return sw.ElapsedMilliseconds;
    }
}