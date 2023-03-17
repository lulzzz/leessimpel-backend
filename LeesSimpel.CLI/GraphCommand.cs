using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using NiceIO;
using Spectre.Console;
using Spectre.Console.Cli;

class GraphCommand : AsyncCommand<GraphCommand.Settings>
{
    internal class Settings : CommandSettings
    {
        [CommandArgument(0, "<evaluation>")]
        public string Evaluation { get; set; } = null!;
        
        [CommandArgument(1, "[benchmark]")]
        public string? Benchmark { get; set; }
    }

    public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        // Create a table
        var table = new Table();

        if (settings.Benchmark == null)
            MakeTableWithoutBenchmark(settings, table);
        else 
            MakeTableWithBenchmark(settings, table);

        AnsiConsole.Write(table);
        return Task.FromResult(0);
    }

    class BenchComparison
    {
        public int bench_length;
        public int eval_length;
        public int bench_accuracy;
        public int eval_accuracy;
    }

    static BenchComparison? BenchComparisonFor(NPath evalFile, string benchmark)
    {
        var benchFile = Directories.TrainingSet.Combine(benchmark, evalFile.FileName);
        if (!benchFile.FileExists())
            return null;

        try
        {
            var evalObject = JObject.Parse(evalFile.ReadAllText());
            var benchObject = JObject.Parse(benchFile.ReadAllText());

            var evalAccuracyPercentage = (int) (evalObject["accuracy_score"]!.Value<float>() * 100);
            var benchAccuracyPercentage = (int) (benchObject["accuracy_score"]!.Value<float>() * 100);

            var evalLength = evalObject["length"]!.Value<int>();
            var benchLength = benchObject["length"]!.Value<int>();
            return new()
            {
                bench_accuracy = benchAccuracyPercentage,
                eval_accuracy = evalAccuracyPercentage,
                eval_length = evalLength,
                bench_length = benchLength
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    static void MakeTableWithBenchmark(Settings settings, Table table)
    {
        Console.WriteLine($"eval: {settings.Evaluation}");
        Console.WriteLine($"bench: {settings.Benchmark}");
        
        var comparisons = Directories.TrainingSet.Combine(settings.Evaluation).Files("*.json")
            .ToDictionary(f => f.FileNameWithoutExtension, f => BenchComparisonFor(f, settings.Benchmark!));

        var comparisonsValues = comparisons.Values.OfType<BenchComparison>().ToArray();
        var averageBenchAccuracy = (int)comparisonsValues.Average(c => c.bench_accuracy);
        var averageEvalAccuracy = (int)comparisonsValues.Average(c => c.eval_accuracy);
        var averageBenchLength= (int)comparisonsValues.Average(c => c.bench_length);
        var averageEvalLength = (int)comparisonsValues.Average(c => c.eval_length);

        
        table.AddColumn("Brief");
        table.AddColumn($"benchmark-accuracy {averageBenchAccuracy}%");
        table.AddColumn($"accuracy [{ColorFor(averageEvalAccuracy, averageEvalAccuracy, WhichIsBetter.Higher)}]{averageEvalAccuracy}%[/]");
        table.AddColumn($"benchmark-length {averageBenchLength}");
        table.AddColumn($"length {averageEvalLength} {DiffStringFor(averageEvalLength, averageBenchLength, WhichIsBetter.Lower)}");
        
        foreach (var kvp in comparisons)
        {
            BenchComparison? c = kvp.Value;
            if (c == null)
            {
                table.AddRow(kvp.Key, "no benchmark available");
                continue;
            }
            
            table.AddRow(kvp.Key,
                c.bench_accuracy.ToString(),
                $"[{ColorFor(c.eval_accuracy, c.bench_accuracy, WhichIsBetter.Higher)}]{c.eval_accuracy}[/]",
                c.bench_length.ToString(),
                $"{c.eval_length} {DiffStringFor(c.eval_length, c.bench_length, WhichIsBetter.Lower)}"
            );
        }
    }

    static string DiffStringFor(int value, int benchValue, WhichIsBetter whichIsBetter)
    {
        var lengthPercentage = (int) (100.0f * ((value - benchValue) / (float) benchValue));

        var s = lengthPercentage > 0
            ? "+" + lengthPercentage + "%"
            : "" + lengthPercentage + "%";
        return $"[{ColorFor(value, benchValue, whichIsBetter)}]{s}[/]";
    }

    enum WhichIsBetter
    {
        Higher,
        Lower
    }
    
    static string ColorFor(int value, int benchValue, WhichIsBetter whichIsBetter)
    {
        if (value > benchValue)
            return whichIsBetter == WhichIsBetter.Higher ? "green" : "red";
        if (value == benchValue)
            return "yellow";
        return whichIsBetter == WhichIsBetter.Higher ? "red" : "green";
    }

    static void MakeTableWithoutBenchmark(Settings settings, Table table)
    {
        table.AddColumn("Brief");
        table.AddColumn("accuracy");
        table.AddColumn("length");
        foreach (var eval in Directories.TrainingSet.Combine(settings.Evaluation).Files("*.json"))
        {
            try
            {
                var evalObject = JObject.Parse(eval.ReadAllText());
                var accuractPercentage = evalObject["accuracy_score"]!.Value<float>() * 100;
                var color = (accuractPercentage > 90) ? "green" : "red";
                table.AddRow(eval.FileNameWithoutExtension, $"[{color}]{(int) accuractPercentage}%[/]",
                    evalObject["length"]!.Value<int>().ToString());
            }
            catch (Exception)
            {
                table.AddRow(eval.FileNameWithoutExtension, "error");
            }
        }
    }
}