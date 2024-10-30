using CommandLine;

namespace WhelanB.RemoveBackground.CLI
{
    internal class CommandLineOptions
    {
        [Option('m', "model", Required = true, HelpText = "Path to the ONNX model you wish to use")]
        public string ModelPath { get; set; } = string.Empty;

        [Option('p', "parameter", HelpText = "Image input parameter name for supplied model (default:input_image)")]
        public string ModelParameter { get; set; } = "input_image";

        [Option('s', "size", HelpText = "Size of the image (NxN) expected by model (default:320)")]
        public int ModelImageSize { get; set; } = 320;

        [Option('g', "gpu", HelpText = "If set, processing will execute on the GPU with the supplied deviceId, instead of the CPU.")]
        public int? gpuDeviceId { get; set; }
    }

    [Verb("single", isDefault: true, HelpText = "Remove the background from a single file")]
    internal class SingleFileOptions : CommandLineOptions
    {
        [Option('i', "input", Required = true, HelpText = "Input image to remove the background from")]
        public string InputPath { get; set; } = string.Empty;

        [Option('o', "output", Required = true, HelpText = "Output path to save image with removed background")]
        public string OutputPath { get; set; } = string.Empty;
    }

    [Verb("batch", HelpText = "Remove the background from multiple images.")]
    internal class BatchFileOptions : CommandLineOptions
    {
        [Option('i', "input", Required = true, HelpText = "One or more paths to an image for background removal")]
        public IEnumerable<string> InputPath { get; set; } = new List<string>();

        [Option('o', "output", Required = true, HelpText = "Output directory to write images with background removed")]
        public string OutputPath { get; set; } = string.Empty;

        [Option('c', "concurrency", HelpText = "The max number of concurrent background removals that can execute at one time. default:1")]
        public int MaxDegreeOfConcurrency { get; set; } = 1;

        [Option('f', "prefix", HelpText = "Prefix outputted filenames with this string")]
        public string Prefix { get; set; } = string.Empty;
    }
}
