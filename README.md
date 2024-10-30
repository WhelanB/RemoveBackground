# Remove Background
A .NET library and CLI tool to remove the background from images.

Interfaces with ONNX models such as u2net/BiRefNet to produce and apply a mask which isolates the subject from the background.

The repo contains two projects contained in a single solution, a CLI tool for running the project and a C# library for embedding the project into another application.

In order to use this, you must supply your own copy of the u2net/BiRefNet models, as .onnx files

## Example:
![A dog standing in grass](Examples/original.png) ![The same dog, now isolated from the background](Examples/no-bg.png)


## CLI

The CLI contains two functions for processing files:

### Single
This runs over a single file, and outputs the subject removed from the background as a PNG format image.

It can be run either with:
```RemoveBackground params...``` or ```RemoveBackground single params...```

Parameters are:
```
  -i, --input        Required. Input image to remove the background from

  -o, --output       Required. Output path to save image with removed background

  -m, --model        Required. Path to the ONNX model you wish to use

  -p, --parameter    Image input parameter name for supplied model (default:input_image)

  -s, --size         Size of the image (NxN) expected by model (default:320)

  -g, --gpu          If set, processing will execute on the GPU with the supplied deviceId, instead of the CPU.

  --help             Display this help screen.

  --version          Display version information.
```

A minimal example of removing the background of an image, ```test.png``` and outputting it as ```test_no_bg.png``` using an ONNX model stored at ```u2net.onnx``` would be:

```RemoveBackground -i test.png -m u2net.onnx -o test_no_bg.png```

### Batch
Batch mode accepts file globbing as an input, and so can process multiple files in one go. There are some additional parameters available, such as how many images to process concurrently:

It can be run with:
```RemoveBackground batch params...```

A minimal example of removing the background of all JPEG files in a directory (in PowerShell) and storing them in a folder called ```remove_dir```, processing 100 images at a time, would be:

```RemoveBackground batch -i $(Get-ChildItem -Recurse -include *.jpg -File -Name) -m u2net.onnx -o remove_dir -p "input_image" -c 100```

Parameter options are:
```
  -i, --input          Required. One or more paths to an image for background removal

  -o, --output         Required. Output directory to write images with background removed

  -c, --concurrency    The max number of concurrent background removals that can execute at one time. default:1

  -f, --prefix         Prefix outputted filenames with this string

  -m, --model          Required. Path to the ONNX model you wish to use

  -p, --parameter      Image input parameter name for supplied model (default:input_image)

  -s, --size           Size of the image (NxN) expected by model (default:320)

  -g, --gpu            If set, processing will execute on the GPU with the supplied deviceId, instead of the CPU.

  --help               Display this help screen.

  --version            Display version information.
```

### CUDA
CUDA can be used to speed up processing on larger models. To use CUDA, you must have CUDA 12.x and cuDNN 9.x installed. When executing either command, simply add ```-g deviceId``` to use the GPU with the provided deviceId.

## Library
The repo also contains a library version of the project which can be embedded into other .NET projects. The CLI is an example of a project that uses the Library.

``` C#
    using WhelanB.RemoveBackground;
    ...
    {
        using BackgroundRemover remover = new("model.onnx", new ModelOptions(){...});
        using var image = remover.RemoveBackground(path);
        image.SaveAsPng(outputPath, 
            new PngEncoder() 
            { 
                TransparentColorMode = PngTransparentColorMode.Clear, ColorType = PngColorType.RgbWithAlpha 
            }
        );
    }
    
```
## Building
The project currently targets .NET 8.0. To build, from the root of the project, run ```dotnet publish```.