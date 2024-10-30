using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using WhelanB.RemoveBackground.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace WhelanB.RemoveBackground
{
    public class BackgroundRemover : IDisposable
    {
        private ModelOptions _modelOptions;
        private InferenceSession _session;

        /// <summary>
        /// Create a new instance of the BackgroundRemover class by loading a model from disk.
        /// </summary>
        /// <param name="modelPath">Path to an ONNX model which can remove the background from an image</param>
        /// <param name="options">Options to invoke the ONNX model with</param>
        /// <param name="useGpu">Whether to execute on the GPU or CPU</param>
        /// <param name="gpuDeviceId">GPU device ID to use if useGpu is set (default: 0)</param>
        /// <exception cref="FileNotFoundException">throws if the model at <paramref name="modelPath"/> cannot be found</exception>
        public BackgroundRemover(string modelPath, ModelOptions? options, bool useGpu = false, int gpuDeviceId = 0) 
        {
            if (!File.Exists(modelPath))
            {
                throw new FileNotFoundException(modelPath);
            }
            if (useGpu)
            {
                using var gpuOptions = SessionOptions.MakeSessionOptionWithCudaProvider(gpuDeviceId);
                _session = new InferenceSession(modelPath, gpuOptions);
            }
            else
            {
                _session = new InferenceSession(modelPath);
            }

            _modelOptions = options ?? new();
        }

        /// <summary>
        /// Create a new instance of the BackgroundRemover class by loading a model from memory.
        /// </summary>
        /// <param name="modelData">Model data as a byte array</param>
        /// <param name="options">Options to invoke the ONNX model with</param>
        /// <param name="useGpu">Whether to execute on the GPU or CPU</param>
        /// <param name="gpuDeviceId">GPU device ID to use if useGpu is set (default: 0)</param>
        public BackgroundRemover(byte[] modelData, ModelOptions? options, bool useGpu = false, int gpuDeviceId = 0)
        {
            if (useGpu)
            {
                using var gpuOptions = SessionOptions.MakeSessionOptionWithCudaProvider(gpuDeviceId);
                _session = new InferenceSession(modelData, gpuOptions);
            }
            else
            {
                _session = new InferenceSession(modelData);
            }

            _modelOptions = options ?? new();
        }

        /// <summary>
        /// Remove the background from an image at the supplied filepath
        /// </summary>
        /// <param name="filePath">path to the image file you wish to remove the background from</param>
        /// <returns>ImageSharp image containing the input image with the background removed</returns>
        public Image<Rgba32> RemoveBackground(string filePath)
        {
            using var imageForModel = Image.Load<Rgba32>(filePath);
            return RemoveBackground(imageForModel);
        }

        /// <summary>
        /// Removes the background from the supplied Image object
        /// </summary>
        /// <param name="imageForModel">ImageSharp image to remove the background from</param>
        /// <returns>ImageSharp image containing the input image with the background removed</returns>
        public Image<Rgba32> RemoveBackground(Image<Rgba32> imageForModel)
        {
            using var mask = new Image<L8>(_modelOptions.OutputWidth, _modelOptions.OutputHeight);
            DenseTensor<float> _inputTensor = new([1, 3, _modelOptions.InputHeight, _modelOptions.InputWidth]);

            int imageWidth = imageForModel.Width;
            int imageHeight = imageForModel.Height;

            var imageForMasking = imageForModel.Clone();
            imageForModel.Mutate(i =>
            {
                i.Resize(new ResizeOptions()
                {
                    Size = new Size(_modelOptions.InputWidth, _modelOptions.InputHeight),
                    Mode = ResizeMode.Stretch
                });
            });

            // Create a DenseTensor containing image data
            
            imageForModel.ProcessPixelRows(accessor =>
            {
                for (int y = imageForModel.Height - accessor.Height; y < accessor.Height; y++)
                {
                    Span<Rgba32> pixelSpan = accessor.GetRowSpan(y);
                    for (int x = imageForModel.Width - accessor.Width; x < accessor.Width; x++)
                    {
                        _inputTensor[0, 0, y, x] = (pixelSpan[x].R - 127) / 128f;
                        _inputTensor[0, 1, y, x] = (pixelSpan[x].G - 127) / 128f;
                        _inputTensor[0, 2, y, x] = (pixelSpan[x].B - 127) / 128f;
                    }
                }
            });

            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = _session.Run(
            [
                NamedOnnxValue.CreateFromTensor(_modelOptions.InputParamater, _inputTensor)
            ]);

            if (results.FirstOrDefault()?.Value is not Tensor<float> output)
                throw new ApplicationException("Unable to process image");

            mask.ProcessPixelRows(accessor =>
            {
                for (int y = _modelOptions.InputHeight - accessor.Height; y < accessor.Height; y++)
                {
                    Span<L8> pixelSpan = accessor.GetRowSpan(y);
                    for (int x = _modelOptions.InputWidth - accessor.Width; x < accessor.Width; x++)
                    {
                        pixelSpan[x] = new L8((byte)Math.Clamp((output[0, 0, y, x] * 255), 0, 255));
                    }
                }
            });

            // Resize the mask back to the original image size
            mask.Mutate(i =>
            {
                i.Resize(new ResizeOptions()
                {
                    Size = new Size(imageForMasking.Width, imageForMasking.Height),
                    Mode = ResizeMode.Stretch
                });
            });

            // Capture the mask data into an array so it can be used
            byte[] maskBytes = new byte[mask.Width * mask.Height];

            mask.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<L8> pixelSpan = accessor.GetRowSpan(y);
                    for (int x = 0; x < accessor.Width; x++)
                    {
                        maskBytes[(y * accessor.Width) + x] = pixelSpan[x].PackedValue;
                    }
                }
            });

            imageForMasking.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgba32> pixelSpan = accessor.GetRowSpan(y);
                    for (int x = 0; x < accessor.Width; x++)
                    {
                        pixelSpan[x].A = maskBytes[(y * accessor.Width) + x];
                    }
                }
            });

            return imageForMasking;
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}
