using System.Diagnostics;
using System.Text;

namespace XliffBatchTranslate
{
    internal static class Program
    {
        /// <summary>
        /// Convert a user-supplied language input (code or name) into a full language name
        /// expected by the translation prompt. Extend this mapping as needed.
        /// </summary>
        private static string NormalizeName(string lang)
        {
            if (string.IsNullOrWhiteSpace(lang))
            {
                return lang;
            }

            switch (lang.Trim().ToLowerInvariant())
            {
                case "es":
                case "es-es":
                case "spanish":
                    return "Spanish";
                case "de":
                case "de-de":
                case "german":
                    return "German";
                case "fr":
                case "fr-fr":
                case "french":
                    return "French";
                case "pt":
                case "pt-pt":
                case "portuguese":
                    return "Portuguese";
                case "it":
                case "it-it":
                case "italian":
                    return "Italian";
                case "nl":
                case "nl-nl":
                case "dutch":
                    return "Dutch";
                case "ko":
                case "ko-kr":
                case "korean":
                    return "Korean";
                case "zh":
                case "zh-cn":
                case "chinese":
                    return "Chinese";
                case "ru":
                case "ru-ru":
                case "russian":
                    return "Russian";
                default:
                    // Capitalize the first letter for unrecognized names (e.g., "arabic" -> "Arabic")
                    return char.ToUpper(lang[0]) + lang.Substring(1);
            }
        }

        /// <summary>
        /// Convert a user-supplied language input (code or name) into a BCP‑47 code
        /// to be written to the XLIFF file. Extend this mapping as needed.
        /// </summary>
        private static string NormalizeCode(string lang)
        {
            if (string.IsNullOrWhiteSpace(lang))
            {
                return lang;
            }

            switch (lang.Trim().ToLowerInvariant())
            {
                case "spanish":
                case "es":
                case "es-es":
                    return "es";
                case "german":
                case "de":
                case "de-de":
                    return "de";
                case "french":
                case "fr":
                case "fr-fr":
                    return "fr";
                case "portuguese":
                case "pt":
                case "pt-pt":
                    return "pt";
                case "italian":
                case "it":
                case "it-it":
                    return "it";
                case "dutch":
                case "nl":
                case "nl-nl":
                    return "nl";
                case "korean":
                case "ko":
                case "ko-kr":
                    return "ko";
                case "chinese":
                case "zh":
                case "zh-cn":
                    return "zh";
                case "russian":
                case "ru":
                case "ru-ru":
                    return "ru";
                default:
                    // If unknown, just return the original input
                    return lang;
            }
        }

        public static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            // Usage:
            // dotnet run -- <inputFolder> <outputFolder> <targetLanguage> [endpoint] [model]
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: dotnet run -- <inputFolder> <outputFolder> <targetLanguage> [endpoint] [model]");
                Console.WriteLine("Example: dotnet run -- ./in ./out German http://127.0.0.1:1234/v1/chat/completions Unbabel/TowerInstruct-7B-v0.2");
                return 2;
            }

            var inputFolder = Path.GetFullPath(args[0]);
            var outputFolder = Path.GetFullPath(args[1]);
            var languageArg = args[2];

            var endpoint = args.Length >= 4 ? args[3] : "http://127.0.0.1:1234/v1/chat/completions";
            var model = args.Length >= 5 ? args[4] : "Unbabel/TowerInstruct-7B-v0.2";

            if (!Directory.Exists(inputFolder))
            {
                Console.WriteLine($"Error: input folder not found: {inputFolder}");
                return 2;
            }

            Directory.CreateDirectory(outputFolder);

            // No system prompt for TowerInstruct multilingual models.
            string systemMessage = string.Empty;

            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
                var lm = new LmStudioClient(http, endpoint, model, systemMessage);

                // Normalize user language input to a friendly name and a BCP‑47 code
                var targetLanguageName = NormalizeName(languageArg);
                var targetLanguageCode = NormalizeCode(languageArg);

                var options = new XliffTranslateOptions
                {
                    TargetLanguage = targetLanguageName,
                    TargetLanguageCode = targetLanguageCode,
                    TranslateIfTargetMissingOrSameAsSource = true,
                    MaxTokens = 256,
                    Temperature = 0.0, // deterministic
                    UseCache = true
                };

                var logger = new FileTranslationLogger(
                    Path.Combine(outputFolder, "translation.log"));
                var translator = new XliffTranslator(lm, options, logger);

                var files = EnumerateXliffFiles(inputFolder).ToList();
                if (files.Count == 0)
                {
                    Console.WriteLine("No .xlf/.xliff files found.");
                    return 0;
                }

                // Optional: Pre‑count total trans‑units for better overall progress.
                // If you prefer faster startup, set this false.
                var doPrecount = true;
                var totalUnits = 0;
                if (doPrecount)
                {
                    Console.WriteLine($"Scanning {files.Count} files to count trans‑units...");
                    foreach (var f in files)
                    {
                        totalUnits += translator.CountTransUnits(f);
                    }
                }

                Console.WriteLine($"Files: {files.Count}");
                if (doPrecount)
                {
                    Console.WriteLine($"Total trans‑units: {totalUnits}");
                }

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

                    // Create a progress reporter to display per‑file progress.
                    var fileProgress = new Progress<FileProgress>(p =>
                    {
                        var pct = (double)p.ProcessedUnits / p.TotalUnits * 100.0;
                        // \r to overwrite the same line. The two spaces indent the progress below the file header.
                        Console.Write($"\r  progress: {p.ProcessedUnits}/{p.TotalUnits} units ({pct:0.0}%)");
                    });

                    var perFileStats = await translator.TranslateFileAsync(inFile, outFile, fileProgress);

                    // Write a newline after the progress bar so subsequent output starts on a new line.
                    Console.WriteLine();

                    overall.TotalTransUnits += perFileStats.TotalTransUnits;
                    overall.TranslatedCount += perFileStats.TranslatedCount;
                    overall.SkippedCount += perFileStats.SkippedCount;
                    overall.CacheHitCount += perFileStats.CacheHitCount;
                    overall.FailureCount += perFileStats.FailureCount;

                    processedUnits += perFileStats.TotalTransUnits;

                    // Per‑file summary
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
}