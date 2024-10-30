using CommandLine;
using WhelanB.RemoveBackground.CLI;
using WhelanB.RemoveBackground;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using WhelanB.RemoveBackground.Models;


internal class Program
{
    public static int Main(string[] args)
    {
        try
        {
            // Process command line args and either execute single or batch file processing mode
            return CommandLine.Parser.Default.ParseArguments<SingleFileOptions, BatchFileOptions>(args)
                .MapResult(
                    (SingleFileOptions opts) => ProcessSingleFileOptions(opts),
                    (BatchFileOptions opts) => ProcessBatchFileOptions(opts),
                    errs => 1);
        }
        catch (AggregateException e) when (e.InnerException is IOException)
        {
            // If a user provides a directory path during batch mode such that an outputted background-free file might contain the same name
            // as an input file, there might be a case where the program is attempting to concurrently read and write this file. Since the user
            // shouldn't want to remove the background of a file they have potentially already removed the background of, give them a helpful nudge.
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{nameof(AggregateException)} encountered. You might be attempting to output a file that will overwrite an input file.");
            Console.ResetColor();
            return 1;
        }
        catch (Exception e) when (e is UnauthorizedAccessException || e is FileNotFoundException || e is IOException)
        {
            // Handle common potential file access issues here
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(e.ToString());
            Console.ResetColor();
            return 1;
        }
    }

    static int ProcessBatchFileOptions(BatchFileOptions opts)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        if (!Directory.Exists(opts.OutputPath))
        {
            Directory.CreateDirectory(opts.OutputPath);
        }
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"Loading Model: {Path.GetFileName(opts.ModelPath)} ");

        if (!File.Exists(opts.ModelPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Could not find model: {opts.ModelPath}");
            Console.ResetColor();
            return 1;
        }

        using BackgroundRemover remover = new(opts.ModelPath, new ModelOptions()
        {
            InputHeight = opts.ModelImageSize,
            InputWidth = opts.ModelImageSize,
            OutputHeight = opts.ModelImageSize,
            OutputWidth = opts.ModelImageSize,
            InputParamater = opts.ModelParameter
        },
        opts.gpuDeviceId.HasValue, opts.gpuDeviceId.HasValue ? opts.gpuDeviceId.Value : 0);

        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = opts.MaxDegreeOfConcurrency
        };

        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"Processing {opts.InputPath.Count()} File(s)...");
        Parallel.ForEach(opts.InputPath, parallelOptions, (path, _) =>
        {
            using var image = remover.RemoveBackground(path);
            var outputPath = Path.Combine(opts.OutputPath, opts.Prefix + Path.GetFileNameWithoutExtension(path) + ".png");
            Console.WriteLine($"Saving {outputPath} ");
            // Write with PngTransparentColorMode.Clear so we don't leak the original image
            image.SaveAsPng(outputPath, new PngEncoder() { TransparentColorMode = PngTransparentColorMode.Clear, ColorType = PngColorType.RgbWithAlpha });
        });

        watch.Stop();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Background removed in {watch.Elapsed.TotalMilliseconds}ms");
        Console.ResetColor();

        return 0;
    }
    static int ProcessSingleFileOptions(SingleFileOptions opts)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"Loading Model: {Path.GetFileName(opts.ModelPath)} ");

        if (!File.Exists(opts.ModelPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Could not find model: {opts.ModelPath}");
            Console.ResetColor();
            return 1;
        }

        using BackgroundRemover remover = new(opts.ModelPath, new ModelOptions()
        {
            InputHeight = opts.ModelImageSize,
            InputWidth = opts.ModelImageSize,
            OutputHeight = opts.ModelImageSize,
            OutputWidth = opts.ModelImageSize,
            InputParamater = opts.ModelParameter
        },
        opts.gpuDeviceId.HasValue, opts.gpuDeviceId.HasValue ? opts.gpuDeviceId.Value : 0);

        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("Processing...");
        var image = remover.RemoveBackground(opts.InputPath);
        Console.WriteLine($"Saving {opts.OutputPath} ");
        // Write with PngTransparentColorMode.Clear so we don't leak the original image
        image.SaveAsPng(opts.OutputPath, new PngEncoder() { TransparentColorMode = PngTransparentColorMode.Clear, ColorType = PngColorType.RgbWithAlpha });
        
        watch.Stop();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Background removed in {watch.Elapsed.TotalMilliseconds}ms");
        Console.ResetColor();
        return 0;
    }
}