# 内存池性能压测（SOH / LOH）

简要说明：本仓库包含用于比较在大量短生命周期数组分配场景下，使用 `ArrayPool<T>` 与直接 new 分配所带来性能和 GC 行为差异的基准代码与示例结果。

> **声明**  
> 本项目代码由 AI 生成。  
> 个人运行环境：`.NET 10.0.100`、`Windows 11`、CPU：9800x3D、内存：96GB。

测试配置示例：
- **Iterations（迭代次数）**: 5,000,000
- **SOH（Small Object Heap）数组大小**: 65,536 字节（64 KB）
- **LOH（Large Object Heap）数组大小**: 1,048,576 字节（1 MB）

示例输出（已格式化）：

```
内存池性能压测（含 SOH/LOH + GC 统计）
Iterations: 5,000,000
SOH Array Size: 65,536 bytes (64 KB)
LOH Array Size: 1,048,576 bytes (1 MB)
--------------------------------------------------

预热阶段...
预热完成。

=== SOH (64KB) 测试 ===
[不使用池] 耗时: 2467 ms | GC: Gen0=6511, Gen1=1628, Gen2=1 | Heap Delta: 0.0 MB
[使用池]   耗时: 126 ms  | GC: Gen0=1,    Gen1=1,    Gen2=1 | Heap Delta: 0.0 MB
结果校验: True (无池=1273252896, 有池=1273252896)

=== LOH (1MB) 测试 ===
[不使用池] 耗时: 80770 ms | GC: Gen0=993125, Gen1=992963, Gen2=992963 | Heap Delta: -1.0 MB
[使用池]   耗时: 58 ms    | GC: Gen0=1,      Gen1=1,      Gen2=1      | Heap Delta: 1.0 MB
结果校验: True (无池=1273252896, 有池=1273252896)
```

结论（示例）：
- 在大量短生命周期的数组分配场景中，`ArrayPool<T>` 能显著降低 GC 压力并提高吞吐（尤其对 LOH 场景影响更明显）。