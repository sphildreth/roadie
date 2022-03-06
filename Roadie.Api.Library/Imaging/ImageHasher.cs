﻿using sl = SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Processing;
using System.IO;
using SixLabors.ImageSharp.PixelFormats;
using System;

namespace Roadie.Library.Imaging
{
    /// <summary>
    ///     Contains a variety of methods useful in generating image hashes for image comparison
    ///     and recognition.
    ///     Credit for the AverageHash implementation to David Oftedal of the University of Oslo.
    /// </summary>
    public static class ImageHasher
    {
        #region Private constants and utility methods

        /// <summary>
        ///     Bitcounts array used for BitCount method (used in Similarity comparisons).
        ///     Don't try to read this or understand it, I certainly don't. Credit goes to
        ///     David Oftedal of the University of Oslo, Norway for this.
        ///     http://folk.uio.no/davidjo/computing.php
        /// </summary>
        private static readonly byte[] bitCounts =
        {
            0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4, 1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 1, 2, 2, 3,
            2, 3, 3, 4,
            2, 3, 3, 4, 3, 4, 4, 5, 2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4,
            3, 4, 4, 5,
            2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 3, 4, 4, 5,
            4, 5, 5, 6,
            4, 5, 5, 6, 5, 6, 6, 7, 1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5,
            4, 5, 5, 6,
            2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 2, 3, 3, 4,
            3, 4, 4, 5,
            3, 4, 4, 5, 4, 5, 5, 6, 3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6,
            5, 6, 6, 7,
            4, 5, 5, 6, 5, 6, 6, 7, 5, 6, 6, 7, 6, 7, 7, 8
        };

        /// <summary>
        ///     Counts bits (duh). Utility function for similarity.
        ///     I wouldn't try to understand this. I just copy-pasta'd it
        ///     from Oftedal's implementation. It works.
        /// </summary>
        /// <param name="num">The hash we are counting.</param>
        /// <returns>The total bit count.</returns>
        private static uint BitCount(ulong num)
        {
            uint count = 0;
            for (; num > 0; num >>= 8) count += bitCounts[num & 0xff];

            return count;
        }

        private static ulong _averageHash = 0;

        #endregion Private constants and utility methods

        #region Public interface methods

        /// <summary>
        ///     Generate a hash for the image to be able to find like/matching images.
        /// </summary>
        /// <param name="bytes">Image bytes</param>
        /// <returns>Hash of Image</returns>
        public static ulong AverageHash(byte[] bytes)
        {
            _averageHash = 0;
            using var image = sl.Image.Load<Rgba32>(bytes);
            {
                image.Mutate(ctx => ctx.Resize(8, 8).Grayscale());
                image.ProcessPixelRows(static pixelAccessor =>
                {
                    var grayscale = new byte[64];
                    uint averageValue = 0;
                    for (var y = 0; y < 8; y++)
                    {
                        var pixelRowSpan = pixelAccessor.GetRowSpan(y);
                        for (var x = 0; x < 8; x++)
                        {
                            var pixel = pixelRowSpan[x].PackedValue;
                            var gray = (pixel & 0x00ff0000) >> 16;
                            gray += (pixel & 0x0000ff00) >> 8;
                            gray += pixel & 0x000000ff;
                            gray /= 12;
                            grayscale[x + y * 8] = (byte)gray;
                            averageValue += gray;
                        }
                    }
                    averageValue /= 64;
                    for (var i = 0; i < 64; i++)
                    {
                        if (grayscale[i] >= averageValue)
                        {
                            _averageHash |= 1UL << (63 - i);
                        }
                    }

                });
            }
            return _averageHash;
        }

        /// <summary>
        ///     Computes the average hash of the image content in the given file.
        /// </summary>
        /// <param name="path">Path to the input file.</param>
        /// <returns>The hash of the input file's image content.</returns>
        public static ulong AverageHash(string path) => AverageHash(File.ReadAllBytes(path));

        public static bool ImagesAreSame(string path1, string path2) => Similarity(path1, path2) == 100;

        public static bool ImagesAreSame(byte[] image1, byte[] image2) => Similarity(image1, image2) == 100;

        /// <summary>
        ///     Returns a percentage-based similarity value between the two given hashes. The higher
        ///     the percentage, the closer the hashes are to being identical.
        /// </summary>
        /// <param name="hash1">The first hash.</param>
        /// <param name="hash2">The second hash.</param>
        /// <returns>The similarity percentage.</returns>
        public static double Similarity(ulong hash1, ulong hash2) => (64 - BitCount(hash1 ^ hash2)) * 100 / 64.0;

        /// <summary>
        ///     Returns a percentage-based similarity value between the image content of the two given
        ///     files. The higher the percentage, the closer the image contents are to being identical.
        /// </summary>
        /// <param name="image1">The first image file.</param>
        /// <param name="image2">The second image file.</param>
        /// <returns>The similarity percentage.</returns>
        public static double Similarity(string path1, string path2)
        {
            var hash1 = AverageHash(path1);
            var hash2 = AverageHash(path2);
            return Similarity(hash1, hash2);
        }

        /// <summary>
        ///     Returns a percentage-based similarity value between the image content of the two given
        ///     files. The higher the percentage, the closer the image contents are to being identical.
        /// </summary>
        /// <param name="image1">The first image bytes.</param>
        /// <param name="image2">The second image bytes.</param>
        /// <returns>The similarity percentage.</returns>
        public static double Similarity(byte[] image1, byte[] image2)
        {
            var hash1 = AverageHash(image1);
            var hash2 = AverageHash(image2);
            return Similarity(hash1, hash2);
        }

        #endregion Public interface methods
    }
}