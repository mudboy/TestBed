using BenchmarkDotNet.Attributes;
using DataFirst.Lodash;
using StringMap = System.Collections.Immutable.ImmutableDictionary<string, dynamic>;

namespace Benchy;

[MemoryDiagnoser]
public class LodashBenchmark
{
    [Benchmark]
    public StringMap Of() => Map.Of("1", "2", "3", "4","1", "2", "3", "4","1", "2", "3", "4","1", "2", "3", "4","1", "2", "3", "4","1", "2", "3", "4","1", "2", "3", "4","1", "2", "3", "4","1", "2", "3", "4","1", "2", "3", "4","1", "2", "3", "4","1", "2", "3", "4","1", "2", "3", "4","1", "2", "3", "4");
}