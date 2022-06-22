using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace NearDuplicateDetection
{
    public class AverageHash
    {
        private const int width = 8;
        private const int height = 8;
        private const int pixelsNumber = width * height;
        private const ulong sugnificantMask = 1UL << (pixelsNumber - 1);

        public ulong Hash(Image<Rgba32> image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            image.Mutate(ctx => ctx
                                .Resize(width, height)
                                .Grayscale(GrayscaleMode.Bt601)
                                .AutoOrient());

            var hash = 0UL;

            image.ProcessPixelRows((imageAccessor) =>
            {
                uint averageValue = 0;
                for (var y = 0; y < height; y++)
                {
                    Span<Rgba32> row = imageAccessor.GetRowSpan(y);
                    for (var x = 0; x < width; x++)
                    {
                        // We know 4 bytes (RGBA) are used to describe one pixel
                        // Also, it is already grayscaled, so R=G=B. Therefore, we can take one of these
                        // values for average calculation. We take the R (the first of each 4 bytes).
                        averageValue += row[x].R;
                    }
                }

                averageValue /= pixelsNumber;

                // Compute the hash: each bit is a pixel
                // 1 = higher than average, 0 = lower than average
                var mask = sugnificantMask;

                for (var y = 0; y < height; y++)
                {
                    Span<Rgba32> row = imageAccessor.GetRowSpan(y);
                    for (var x = 0; x < width; x++)
                    {
                        if (row[x].R >= averageValue)
                        {
                            hash |= mask;
                        }

                        mask >>= 1;
                    }
                }
            });

            return hash;
        }
    }
}
