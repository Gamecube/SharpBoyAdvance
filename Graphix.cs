// Copyright (C) 2015 Gamecube
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program. If not, see http://www.gnu.org/licenses/.

using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SharpBoyAdvance
{
    public unsafe class Graphix : IDisposable
    {
        #region public fields

        private bool isCompressed;
        /// <summary>
        /// Returns a value that indicates whether
        /// the GBA image is LZ77-compressed.
        /// </summary>
        public bool IsCompressed
        {
            get { return isCompressed; }
        }

        private bool is4bpp;
        /// <summary>
        /// Returns a value that indicates whether
        /// the image is in 4 bits per pixel format.
        /// </summary>
        public bool Is4bpp
        {
            get { return is4bpp; }
        }

        private byte[] imagedata;
        /// <summary>
        /// Returns the raw image data.
        /// </summary>
        public byte[] ImageData
        {
            get { return imagedata; }
        }

        #endregion

        #region constructor

        /// <summary>
        /// Creates a new GbaImage from a byte array
        /// which contains the pixel data. You must
        /// know whether the image is compresed and
        /// if it is stored with 16 or 256 colors.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="compressed"></param>
        /// <param name="bpp"></param>
        public Graphix(byte[] source, bool compressed, bool bpp)
        {
            isCompressed = compressed;
            imagedata = source;
            is4bpp = bpp;
        }

        /// <summary>
        /// Creates a new GbaImage from a bitmap.
        /// Bitmap must be 4bpp or 8bpp!
        /// </summary>
        /// <param name="source"></param>
        /// <param name="compressed"></param>
        public Graphix(Bitmap source, bool compressed)
        {
            if (source.Width % 8 != 0 || source.Height % 8 != 0)
                return;
            if (source.PixelFormat == PixelFormat.Format4bppIndexed)
                is4bpp = true;
            else if (source.PixelFormat == PixelFormat.Format8bppIndexed)
                is4bpp = false;
            else
                return;

            this.isCompressed = compressed;
            var height = source.Height;
            var width = source.Width;
            int position = 0;

            var rect = new Rectangle(0, 0, width, height);
            var data = source.LockBits(rect, ImageLockMode.ReadOnly, source.PixelFormat);
            var stride = data.Stride;
            var pntr = data.GetPointer();

            if (is4bpp)
            {
                imagedata = new byte[((width * height) / 2)];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x += 2)
                    {
                        imagedata[position] = source.FastGet4Bpp(x, y, stride, pntr);
                        position += 1;
                    };
                };
            }
            else
            {
                imagedata = new byte[(width * height)];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        imagedata[position] = source.FastGet8Bpp(x, y, stride, pntr);
                        position += 1;
                    };
                };
            }

            source.UnlockBits(data);
        }

        /// <summary>
        /// Reads an image from the rom at the given offset,
        /// with the given decryption and the given length.
        /// Length = (width % 8) * (height % 8)
        /// </summary>
        /// <param name="rom"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="bpp"></param>
        /// <param name="compressed"></param>
        public Graphix(Romfile rom, uint offset, int length, bool bpp, bool compressed)
        {
            is4bpp = bpp;
            rom.Offset = offset;
            isCompressed = compressed;

            if (compressed)
            {
                imagedata = Decryption.Decode(rom.Binary, offset);
            }
            else
            {
                if (bpp)
                    imagedata = rom.ReadBytes(length * 32);
                else
                    imagedata = rom.ReadBytes(length * 64);
            }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Draws a bitmap from the byte array.
        /// tile is a parameter that is calculated
        /// by simply doing (width_bitmap / 8);
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="palette"></param>
        /// <returns></returns>
        public Bitmap Draw(int tile, Color[] palette)
        {
            if (is4bpp)
            {
                int length = (imagedata.Length / 32);
                var bitmap = new Bitmap((tile * 8), ((int)(Math.Ceiling(
                    length / (decimal)tile)) * 8), PixelFormat.Format4bppIndexed);

                int width = bitmap.Width, height = bitmap.Height;
                var data = bitmap.LockBits(new Rectangle(0, 0, width, height),
                    ImageLockMode.WriteOnly, PixelFormat.Format4bppIndexed);
                int stride = data.Stride;
                byte* pntr = data.GetPointer();
                var counter = 0;

                var cpal = bitmap.Palette;
                for (int i = 0; i < 16; i++)
                    cpal.Entries[i] = palette[i];
                bitmap.Palette = cpal;

                for (int y = 0; y < height - 7; y += 8)
                {
                    for (int x = 0; x < width - 7; x += 8)
                    {
                        for (int y2 = 0; y2 < 8; y2++)
                        {
                            for (int x2 = 0; x2 < 8; x2 += 2)
                            {
                                byte value = imagedata[counter];
                                bitmap.FastSet4bpp(x + x2 + 1, y + y2, stride, pntr, palette[(value & 0xF0) >> 4]);
                                bitmap.FastSet4bpp(x + x2, y + y2, stride, pntr, palette[value & 0x0F]);
                                counter += 1;
                            };
                        };
                    };
                };

                bitmap.UnlockBits(data);
                return bitmap;
            }
            else
            {
                int length = (imagedata.Length / 64);
                var bitmap = new Bitmap((tile * 8), ((int)(Math.Ceiling(
                    length / (decimal)tile)) * 8), PixelFormat.Format8bppIndexed);

                int width = bitmap.Width, height = bitmap.Height;
                var data = bitmap.LockBits(new Rectangle(0, 0, width, height),
                    ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
                var stride = data.Stride;
                var pntr = data.GetPointer();
                var counter = 0;

                var cpal = bitmap.Palette;
                for (int i = 0; i < 256; i++)
                    cpal.Entries[i] = palette[i];
                bitmap.Palette = cpal;

                for (int y = 0; y < height - 7; y += 8)
                {
                    for (int x = 0; x < width - 7; x += 8)
                    {
                        for (int y2 = 0; y2 < 8; y2++)
                        {
                            for (int x2 = 0; x2 < 8; x2++)
                            {
                                byte value = imagedata[counter];
                                bitmap.FastSet8bpp(x + x2, y + y2, stride, pntr, palette[value]);
                                counter += 1;
                            };
                        };
                    };
                };

                bitmap.UnlockBits(data);
                return bitmap;
            }
        }

        /// <summary>
        /// Converts the data to an array of bytes.
        /// If variable IsCompressed is true, byte[]
        /// will be compressed before returned!
        /// </summary>
        /// <returns></returns>
        public byte[] Convert()
        {
            if (isCompressed)
                return Decryption.Encode(imagedata);
            else
                return imagedata;
        }

        /// <summary>
        /// Disposes of all managed resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        private bool disposed = false;
        /// <summary>
        /// Clears the imagedata and nulls out the array.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                  imagedata = null;
            }
        }

        #endregion
    }
}