using System.Text.Json;

namespace TestBed;

public record TestRecord(string name, int count, DateTime time);

public class JsonRecords
{
    public static void Main()
    {
        var value = JsonSerializer.Serialize(new TestRecord("bob", 12, DateTime.Now));
        Console.WriteLine(value);
    }
}