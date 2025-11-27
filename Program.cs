using System;
using System.Buffers;
using System.Diagnostics;

public class PoolPerformanceTest
{
    // 测试轮数
    private const int Iterations = 5_000_000;

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
        long sum1, sum2;
        
        // --- 测试：不使用池 ---
        ForceGcAndWait();
        var gcStartNoPool = GetGcStats();
        var memStartNoPool = GC.GetTotalMemory(true);
        var sw = Stopwatch.StartNew();

        sum1 = RunWithoutPool(arraySize, Iterations); // 只执行一次
        
        sw.Stop();
        var timeNoPool = sw.ElapsedMilliseconds;
        var gcEndNoPool = GetGcStats();
        var memEndNoPool = GC.GetTotalMemory(true);

        // --- 测试：使用池 ---
        ForceGcAndWait();
        var gcStartWithPool = GetGcStats();
        var memStartWithPool = GC.GetTotalMemory(true);
        sw.Restart();

        sum2 = RunWithPool(arraySize, Iterations); // 只执行一次
        
        sw.Stop();
        var timeWithPool = sw.ElapsedMilliseconds;
        var gcEndWithPool = GetGcStats();
        var memEndWithPool = GC.GetTotalMemory(true);

        // --- 打印结果 ---
        Console.WriteLine($"[不使用池] 耗时: {timeNoPool} ms | GC: Gen0={gcEndNoPool.Gen0 - gcStartNoPool.Gen0}, Gen1={gcEndNoPool.Gen1 - gcStartNoPool.Gen1}, Gen2={gcEndNoPool.Gen2 - gcStartNoPool.Gen2} | Heap Delta: {(memEndNoPool - memStartNoPool) / 1024 / 1024:N1} MB");
        Console.WriteLine($"[使用池]   耗时: {timeWithPool} ms | GC: Gen0={gcEndWithPool.Gen0 - gcStartWithPool.Gen0}, Gen1={gcEndWithPool.Gen1 - gcStartWithPool.Gen1}, Gen2={gcEndWithPool.Gen2 - gcStartWithPool.Gen2} | Heap Delta: {(memEndWithPool - memStartWithPool) / 1024 / 1024:N1} MB");
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
}