

namespace JsonParser.Console.PerformanceComparisonModels;

// Test models
public class SimpleObject
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public double Score { get; set; }
}

public class DeeplyNestedObject
{
    public string Name { get; set; } = "";
    public int Level { get; set; }
    public DeeplyNestedObject? Child { get; set; }
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ComplexObject
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public List<SimpleObject> Items { get; set; } = new();
    public Dictionary<string, string> Properties { get; set; } = new();
    public double[] Scores { get; set; } = Array.Empty<double>();
    public bool IsEnabled { get; set; }
}

public class BenchmarkResult
{
    public string TestName { get; set; } = "";
    public string ParserName { get; set; } = "";
    public long ElapsedMilliseconds { get; set; }
    public long MemoryUsed { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}
