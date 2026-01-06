using System.Diagnostics;
using System.Text;

namespace XliffBatchTranslate;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        // Usage:
        // dotnet run -- <inputFolder> <outputFolder> <targetLanguage> [endpoint] [model]
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: dotnet run -- <inputFolder> <outputFolder> <targetLanguage> [endpoint] [model]");
            Console.WriteLine("Example: dotnet run -- ./in ./out es http://127.0.0.1:1234/v1/chat/completions towerinstruct-7b-v0.2-en2es");
            return 2;
        }

        var inputFolder = Path.GetFullPath(args[0]);
        var outputFolder = Path.GetFullPath(args[1]);
        var targetLanguage = args[2];

        var endpoint = args.Length >= 4 ? args[3] : "http://127.0.0.1:1234/v1/chat/completions";
        var model = args.Length >= 5 ? args[4] : "towerinstruct-7b-v0.2-en2es";

        if (!Directory.Exists(inputFolder))
        {
            Console.WriteLine($"Error: input folder not found: {inputFolder}");
            return 2;
        }

        Directory.CreateDirectory(outputFolder);

        // Keep system message short to reduce instruction leakage.
        var systemMessage =
            "You translate UI strings for point-of-care (POC) medical software. " +
            "Preserve tokens/placeholders exactly. Output only the translation.";

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            var lm = new LmStudioClient(http, endpoint, model, systemMessage);

            var options = new XliffTranslateOptions
            {
                TargetLanguage = targetLanguage,
                TranslateIfTargetMissingOrSameAsSource = true,
                MaxTokens = 256,
                Temperature = 0.0, // deterministic
                UseCache = true
            };

            var translator = new XliffTranslator(lm, options);

            var files = EnumerateXliffFiles(inputFolder).ToList();
            if (files.Count == 0)
            {
                Console.WriteLine("No .xlf/.xliff files found.");
                return 0;
            }

            // Optional: Pre-count total trans-units for better overall progress.
            // If you prefer faster startup, set this false.
            var doPrecount = true;
            var totalUnits = 0;
            if (doPrecount)
            {
                Console.WriteLine($"Scanning {files.Count} files to count trans-units...");
                foreach (var f in files)
                {
                    totalUnits += translator.CountTransUnits(f);
                }
            }

            Console.WriteLine($"Files: {files.Count}");
            if (doPrecount)
                Console.WriteLine($"Total trans-units: {totalUnits}");

            var sw = Stopwatch.StartNew();

            var overall = new XliffTranslateStats();
            var processedUnits = 0;

            for (int i = 0; i < files.Count; i++)
            {
                var inFile = files[i];
                var rel = Path.GetRelativePath(inputFolder, inFile);
                var outFile = Path.Combine(outputFolder, rel);

                Directory.CreateDirectory(Path.GetDirectoryName(outFile)!);

                Console.WriteLine();
                Console.WriteLine($"[{i + 1}/{files.Count}] {rel}");

                var perFileStats = await translator.TranslateFileAsync(inFile, outFile);

                overall.TotalTransUnits += perFileStats.TotalTransUnits;
                overall.TranslatedCount += perFileStats.TranslatedCount;
                overall.SkippedCount += perFileStats.SkippedCount;
                overall.CacheHitCount += perFileStats.CacheHitCount;
                overall.FailureCount += perFileStats.FailureCount;

                processedUnits += perFileStats.TotalTransUnits;

                // Per-file summary
                Console.WriteLine($"  units: {perFileStats.TotalTransUnits}, translated: {perFileStats.TranslatedCount}, skipped: {perFileStats.SkippedCount}, cached: {perFileStats.CacheHitCount}, failures: {perFileStats.FailureCount}");

                // Overall progress line
                if (doPrecount && totalUnits > 0)
                {
                    var pct = (double)processedUnits / totalUnits * 100.0;
                    Console.WriteLine($"  overall: {processedUnits}/{totalUnits} units ({pct:0.0}%), elapsed: {sw.Elapsed:hh\\:mm\\:ss}");
                }
                else
                {
                    Console.WriteLine($"  overall: {i + 1}/{files.Count} files, elapsed: {sw.Elapsed:hh\\:mm\\:ss}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Done.");
            Console.WriteLine($"Elapsed: {sw.Elapsed:hh\\:mm\\:ss}");
            Console.WriteLine($"Files: {files.Count}");
            Console.WriteLine($"Total units: {overall.TotalTransUnits}");
            Console.WriteLine($"Translated: {overall.TranslatedCount}");
            Console.WriteLine($"Skipped:    {overall.SkippedCount}");
            Console.WriteLine($"Cached:     {overall.CacheHitCount}");
            Console.WriteLine($"Failures:   {overall.FailureCount}");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed: " + ex);
            return 1;
        }
    }

    private static IEnumerable<string> EnumerateXliffFiles(string root)
    {
        // Include both common extensions
        var xlf = Directory.EnumerateFiles(root, "*.xlf", SearchOption.AllDirectories);
        var xliff = Directory.EnumerateFiles(root, "*.xliff", SearchOption.AllDirectories);
        return xlf.Concat(xliff);
    }
}
