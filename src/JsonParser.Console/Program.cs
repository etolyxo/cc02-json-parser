using Newtonsoft.Json;
using JsonParser.Console.PerformanceComparisonModels;
using JsonParser.Core.Parsing;
using System.Diagnostics;
using System.Text;

class Program
{
    private const int WarmupIterations = 10;
    private const int TestIterations = 100;

    static void Main(string[] args)
    {
        Console.WriteLine("JSON Parser Performance Benchmark");
        Console.WriteLine("=================================");
        Console.WriteLine($"Warmup iterations: {WarmupIterations}");
        Console.WriteLine($"Test iterations: {TestIterations}");
        Console.WriteLine();

        var results = new List<BenchmarkResult>();

        // Test 1: Simple objects
        Console.WriteLine("Testing simple objects...");
        var simpleJson = GenerateSimpleObjectJson();
        results.AddRange(BenchmarkParsers("Simple Object", simpleJson,
            () => new TypedJsonParser<SimpleObject>(),
            json => JsonConvert.DeserializeObject<SimpleObject>(json),
            json => System.Text.Json.JsonSerializer.Deserialize<SimpleObject>(json)));

        // Test 2: Large array of simple objects
        Console.WriteLine("Testing large arrays...");
        var largeArrayJson = GenerateLargeArrayJson(10000);
        results.AddRange(BenchmarkParsers("Large Array (10k items)", largeArrayJson,
            () => new TypedJsonParser<SimpleObject[]>(),
            json => JsonConvert.DeserializeObject<SimpleObject[]>(json),
            json => System.Text.Json.JsonSerializer.Deserialize<SimpleObject[]>(json)));

        // Test 3: Deeply nested objects
        Console.WriteLine("Testing deeply nested objects...");
        var deeplyNestedJson = GenerateDeeplyNestedJson(50);
        results.AddRange(BenchmarkParsers("Deeply Nested (50 levels)", deeplyNestedJson,
            () => new TypedJsonParser<DeeplyNestedObject>(),
            json => JsonConvert.DeserializeObject<DeeplyNestedObject>(json),
            json => System.Text.Json.JsonSerializer.Deserialize<DeeplyNestedObject>(json)));

        // Test 4: Complex objects with mixed types
        Console.WriteLine("Testing complex objects...");
        var complexJson = GenerateComplexObjectJson();
        results.AddRange(BenchmarkParsers("Complex Object", complexJson,
            () => new TypedJsonParser<ComplexObject>(),
            json => JsonConvert.DeserializeObject<ComplexObject>(json),
            json => System.Text.Json.JsonSerializer.Deserialize<ComplexObject>(json)));

        // Test 5: Very large single object
        Console.WriteLine("Testing very large objects...");
        var veryLargeJson = GenerateVeryLargeObjectJson();
        results.AddRange(BenchmarkParsers("Very Large Object", veryLargeJson,
            () => new TypedJsonParser<ComplexObject>(),
            json => JsonConvert.DeserializeObject<ComplexObject>(json),
            json => System.Text.Json.JsonSerializer.Deserialize<ComplexObject>(json)));

        // Display results
        DisplayResults(results);
    }

    static List<BenchmarkResult> BenchmarkParsers<T>(
        string testName,
        string json,
        Func<TypedJsonParser<T>> customParserFactory,
        Func<string, T?> newtonsoftParser,
        Func<string, T?> systemTextParser)
    {
        var results = new List<BenchmarkResult>();

        Console.WriteLine($"  Running {testName}...");

        // Benchmark Custom Parser
        results.Add(BenchmarkParser(testName, "Custom Parser", json, () =>
        {
            var parser = customParserFactory();
            return parser.Parse(json);
        }));

        // Benchmark Newtonsoft.Json
        results.Add(BenchmarkParser(testName, "Newtonsoft.Json", json, () =>
        {
            return newtonsoftParser(json);
        }));

        // Benchmark System.Text.Json
        results.Add(BenchmarkParser(testName, "System.Text.Json", json, () =>
        {
            return systemTextParser(json);
        }));

        return results;
    }

    static BenchmarkResult BenchmarkParser<T>(string testName, string parserName, string json, Func<T> parseFunc)
    {
        var result = new BenchmarkResult
        {
            TestName = testName,
            ParserName = parserName
        };

        try
        {
            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
            {
                parseFunc();
            }

            // Force garbage collection before measurement
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var memoryBefore = GC.GetTotalMemory(false);
            var stopwatch = Stopwatch.StartNew();

            // Actual benchmark
            for (int i = 0; i < TestIterations; i++)
            {
                parseFunc();
            }

            stopwatch.Stop();
            var memoryAfter = GC.GetTotalMemory(false);

            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            result.MemoryUsed = Math.Max(0, memoryAfter - memoryBefore);
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            Console.WriteLine($"    Error in {parserName}: {ex.Message}");
        }

        return result;
    }

    static string GenerateSimpleObjectJson()
    {
        return """{"Name": "John Doe", "Age": 30, "IsActive": true, "Score": 95.5}""";
    }

    static string GenerateLargeArrayJson(int count)
    {
        var sb = new StringBuilder();
        sb.Append('[');

        for (int i = 0; i < count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append($$"""
{ "Name": "Person{{i}}", "Age": {{20 + (i % 50)}}, "IsActive": {{(i % 2 == 0).ToString().ToLower()}}, "Score": {{50.0 + (i % 100)}} }
""");
        }

        sb.Append(']');
        return sb.ToString();
    }

    static string GenerateDeeplyNestedJson(int depth)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < depth; i++)
        {
            sb.Append($"{{\"Name\": \"Level{i}\", \"Level\": {i}, \"Tags\": [\"tag{i}a\", \"tag{i}b\"], \"Metadata\": {{\"key{i}\": \"value{i}\"}}, \"Child\": ");
        }

        sb.Append("null");

        for (int i = 0; i < depth; i++)
        {
            sb.Append('}');
        }

        return sb.ToString();
    }

    static string GenerateComplexObjectJson()
    {
        var items = new StringBuilder();
        items.Append('[');
        for (int i = 0; i < 100; i++)
        {
            if (i > 0) items.Append(',');
            items.Append($"{{\"Name\": \"Item{i}\", \"Age\": {i}, \"IsActive\": {(i % 2 == 0).ToString().ToLower()}, \"Score\": {i * 1.5}}}");
        }
        items.Append(']');

        var scores = new StringBuilder();
        scores.Append('[');
        for (int i = 0; i < 1000; i++)
        {
            if (i > 0) scores.Append(',');
            scores.Append(i * 0.1);
        }
        scores.Append(']');

        return $@"{{
                ""Id"": 12345,
                ""Name"": ""Complex Test Object"",
                ""CreatedAt"": ""2023-12-25T10:30:00Z"",
                ""Items"": {items},
                ""Properties"": {{""prop1"": ""value1"", ""prop2"": ""value2"", ""prop3"": ""value3""}},
                ""Scores"": {scores},
                ""IsEnabled"": true
            }}";
    }

    static string GenerateVeryLargeObjectJson()
    {
        var items = new StringBuilder();
        items.Append('[');
        for (int i = 0; i < 5000; i++)
        {
            if (i > 0) items.Append(',');
            items.Append($"{{\"Name\": \"Item{i}\", \"Age\": {i}, \"IsActive\": {(i % 2 == 0).ToString().ToLower()}, \"Score\": {i * 1.5}}}");
        }
        items.Append(']');

        var properties = new StringBuilder();
        properties.Append('{');
        for (int i = 0; i < 100; i++)
        {
            if (i > 0) properties.Append(',');
            properties.Append($"\"prop{i}\": \"This is a longer property value {i} with more text to increase size\"");
        }
        properties.Append('}');

        var scores = new StringBuilder();
        scores.Append('[');
        for (int i = 0; i < 10000; i++)
        {
            if (i > 0) scores.Append(',');
            scores.Append(i * 0.1);
        }
        scores.Append(']');

        return $@"{{
                ""Id"": 99999,
                ""Name"": ""Very Large Test Object with a much longer name that contains more characters"",
                ""CreatedAt"": ""2023-12-25T10:30:00Z"",
                ""Items"": {items},
                ""Properties"": {properties},
                ""Scores"": {scores},
                ""IsEnabled"": true
            }}";
    }

    static void DisplayResults(List<BenchmarkResult> results)
    {
        Console.WriteLine();
        Console.WriteLine("BENCHMARK RESULTS");
        Console.WriteLine("=================");
        Console.WriteLine();

        var groupedResults = results.GroupBy(r => r.TestName);

        foreach (var group in groupedResults)
        {
            Console.WriteLine($"Test: {group.Key}");
            Console.WriteLine(new string('-', 60));

            var successfulResults = group.Where(r => r.Success).OrderBy(r => r.ElapsedMilliseconds).ToList();

            if (successfulResults.Any())
            {
                var fastest = successfulResults.First();

                Console.WriteLine($"{"Parser",-20} {"Time (ms)",-12} {"Memory (KB)",-12} {"Relative Speed",-15}");
                Console.WriteLine(new string('-', 60));

                foreach (var result in successfulResults)
                {
                    var relativeSpeed = result.ElapsedMilliseconds == fastest.ElapsedMilliseconds
                        ? "1.00x (fastest)"
                        : $"{(double)result.ElapsedMilliseconds / fastest.ElapsedMilliseconds:F2}x slower";

                    Console.WriteLine($"{result.ParserName,-20} {result.ElapsedMilliseconds,-12} {result.MemoryUsed / 1024,-12:F1} {relativeSpeed,-15}");
                }
            }

            var failedResults = group.Where(r => !r.Success);
            foreach (var failed in failedResults)
            {
                Console.WriteLine($"{failed.ParserName,-20} FAILED: {failed.Error}");
            }

            Console.WriteLine();
        }

        // Summary
        Console.WriteLine("SUMMARY");
        Console.WriteLine("=======");

        var parserStats = results.Where(r => r.Success)
            .GroupBy(r => r.ParserName)
            .Select(g => new
            {
                Parser = g.Key,
                AvgTime = g.Average(r => r.ElapsedMilliseconds),
                TotalTime = g.Sum(r => r.ElapsedMilliseconds),
                AvgMemory = g.Average(r => r.MemoryUsed),
                TestCount = g.Count()
            })
            .OrderBy(s => s.AvgTime);

        Console.WriteLine($"{"Parser",-20} {"Avg Time (ms)",-15} {"Total Time (ms)",-16} {"Avg Memory (KB)",-16}");
        Console.WriteLine(new string('-', 70));

        foreach (var stat in parserStats)
        {
            Console.WriteLine($"{stat.Parser,-20} {stat.AvgTime,-15:F1} {stat.TotalTime,-16} {stat.AvgMemory / 1024,-16:F1}");
        }
    } 
}
