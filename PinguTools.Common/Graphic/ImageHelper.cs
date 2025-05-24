using BCnEncoder.Encoder;
using BCnEncoder.ImageSharp;
using BCnEncoder.Shared;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PinguTools.Common.Graphic;

public static class ImageHelper
{
    public async static Task ConvertDdsAsync(Image<Rgba32> img, string outPath, int? width = null, int? height = null, CompressionFormat format = CompressionFormat.Bc1, CancellationToken ct = default)
    {
        if (width.HasValue || height.HasValue)
        {
            var resize = new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(width ?? img.Width, height ?? img.Height),
                Sampler = KnownResamplers.Lanczos3
            };
            img.Mutate(c => c.Resize(resize));
        }

        var encoder = new BcEncoder
        {
            OutputOptions =
            {
                Format = format,
                GenerateMipMaps = false,
                Quality = CompressionQuality.BestQuality,
                FileFormat = OutputFileFormat.Dds
            }
        };

        await using var fs = File.Create(outPath);
        await encoder.EncodeToStreamAsync(img, fs, ct);
    }

    public async static Task<bool> IsValidImageAsync(string path)
    {
        try
        {
            await Image.DetectFormatAsync(path);
            return true;
        }
        catch
        {
            return false;
        }
    }
}