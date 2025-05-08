using BCnEncoder.Encoder;
using BCnEncoder.ImageSharp;
using BCnEncoder.Shared;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PinguTools.Image;

public static class Helper
{
    public async static Task<byte[]> ConvertDdsAsync(byte[] rawImage, int? width = null, int? height = null, CompressionFormat format = CompressionFormat.Bc1)
    {
        using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(rawImage);

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

        using var ms = new MemoryStream();
        await encoder.EncodeToStreamAsync(img, ms);

        return ms.ToArray();
    }
}