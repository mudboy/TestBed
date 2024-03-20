using BenchmarkDotNet.Attributes;
using TestBed;

namespace Benchy;

[MemoryDiagnoser]
public class MyBenchys
{

    [Benchmark]
    public Option<IEnumerable<int>> TraverseA() => Enumerable.Range(1, 100).TraverseA(Option.Some);
    
    [Benchmark]
    public Option<IEnumerable<int>> TraverseM() => Enumerable.Range(1, 100).TraverseM(Option.Some);
    
    [Benchmark]
    public Option<IEnumerable<int>> TraverseM2() => Enumerable.Range(1, 100).Traverse2(Option.Some);
}