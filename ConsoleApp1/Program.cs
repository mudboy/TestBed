// See https://aka.ms/new-console-template for more information

using System.Formats.Tar;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using TestBed;
using Monads;
using Monads.monoids;
using Utils;

Console.WriteLine("Hello, World!");

TestJsonRecords();
TestResultApplicativeTraverse();
TestingGenerators();

State<int, char> state = s => ('b', 0);
var res = state.Select(c => "dog");

return;

void TestRefStructs()
{
    var x = GetEither();
}


async Task<Either<Int, string>> GetEither()
{
    //return Either.Left(10);
    var x = await Task.FromResult(10);
    var opt = Console.ReadLine().ToOption();
    var b = Gen.Bool.Run(Rng.Default());

    return b ? Either.Right("") : Either.Left(new Int(x));
}

void TestJsonRecords()
{
    var opt = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    var x = JsonSerializer.Serialize(new TestRecord("bob", 12, TrainTypes.Diesel, DateTime.Now, new SubRecord("timmy", 12.99m)), opt);
    Console.WriteLine(x);

    var xx = JsonSerializer.Deserialize<TestRecord>(x, opt);
    Console.WriteLine(xx);
}

void TestResultApplicativeTraverse()
{
    int[] ints = [1, 2, 3, 4];

    var f = (int i) => i % 2 == 0 ? Result.Failure($"{i} is even") : Result.Success(i);

    var result = ints.Traverse(f);

    Console.WriteLine(result);
}

void TestingGenerators()
{
    //Main.GenExamples();
    var r = Rng.Secure();
    var passwords = Gen.ListOfN(10, Gen.PasswordStringN(14)).Run(r);
    passwords.Print("passwords: ");

    var weighted = Gen.Weighted((Gen.Return("A"), 0.25), (Gen.Return("B"), 0.75)).List()(10).Run(r);
    weighted.Print("20 weighted a&bs ");
    weighted.CountBy(s => s).Print();

    var d = Rng.ReallyDumb();
    var weighted2 = Gen.Weighted2((Gen.Return("A"), 20), (Gen.Return("B"), 25), (Gen.Return("C"), 54), (Gen.Return("*"), 1)).ListOfN(10).Run(r);
    weighted2.Print("20 weighted a&bs ");
    weighted2.CountBy(s => s).OrderBy(x => x.Key).ToList().Print();



    var b = Gen.Both(Gen.Char, Gen.Digit).Run(r);
    b.Print();

    Gen.String(10).Run(r).CountBy(Char.IsUpper).Print("A string: ");

    Gen.NaturalInt.ListOfN(100).Run(r).Print();
}


public readonly record struct TestRecord(string Name, int Count, TrainTypes Type, DateTime Time, SubRecord SubRecord);
public readonly record struct SubRecord(string Name, decimal Price);

public enum TrainTypes
{
    Steam,
    Diesel,
    Electric 
}

public record struct Int(int Value) : Monoid<Int>
{ 
    public static implicit operator Int(int x) => new Int(x);
    public Int Combine(Int x, Int y) => x.Value + y.Value;
    public Int Zero => new (0);
}

public record struct Str(string Value) : Monoid<Str>
{
    public static implicit operator Str(string x) => new(x);
    public Str Combine(Str x, Str y) => x.Value + y.Value;
    public Str Zero => new("");
}