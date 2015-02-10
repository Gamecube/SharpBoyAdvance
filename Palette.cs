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
    public class GbaPalette
    {
        #region public fields

        private bool isCompressed;
        /// <summary>
        /// Returns a value that indicates whether
        /// the palette is LZ77 compressed or not.
        /// </summary>
        public bool IsCompressed
        {
            get { return isCompressed; }
        }

        private byte[] palette;
        /// <summary>
        /// Returns the raw, uncompressed data
        /// of the palette in the rom.
        /// </summary>
        public byte[] Palette
        {
            get { return palette; }
        }

        #endregion

        #region constructor

        /// <summary>
        /// Creates a palette from an uncompressed source array.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="isCompressed"></param>
        public GbaPalette(byte[] source, bool compressed)
        {
            if (source.Length == 16 || source.Length == 256)
            {
                isCompressed = compressed;
                palette = source;
            }
        }

        /// <summary>
        /// Creates a palette from a color array with
        /// 16 or 256 entries. Other counts not allowed!
        /// </summary>
        /// <param name="source"></param>
        /// <param name="isCompressed"></param>
        public GbaPalette(Color[] source, bool isCompressed)
        {
            if (source.Length == 16)
            {
                int position = 0;
                this.palette = new byte[32];
                for (int i = 0; i < 16; i++)
                {
                    var color = source[i];
                    byte red = (byte)(Math.Floor(color.R / 8f));
                    byte green = (byte)(Math.Floor(color.G / 8f));
                    byte blue = (byte)(Math.Floor(color.B / 8f));

                    var value = (ushort)(red | (green << 5) | (blue << 10));
                    this.palette[position + 1] = (byte)((value & 0xFF00) >> 8);
                    this.palette[position] = (byte)(value & 0xFF);

                    position += 2;
                };
            }
            else if (source.Length == 256)
            {
                int position = 0;
                this.palette = new byte[256];
                for (int i = 0; i < 256; i++)
                {
                    var color = source[i];
                    byte red = (byte)(Math.Floor(color.R / 8f));
                    byte green = (byte)(Math.Floor(color.G / 8f));
                    byte blue = (byte)(Math.Floor(color.B / 8f));

                    var value = (ushort)(red | (green << 5) | (blue << 10));
                    this.palette[position + 1] = (byte)((value & 0xFF00) >> 8);
                    this.palette[position] = (byte)(value & 0xFF);

                    position += 2;
                };
            }
        }

        /// <summary>
        /// Reads a/an un/compressed palette in the rom.
        /// </summary>
        /// <param name="rom"></param>
        /// <param name="offset"></param>
        /// <param name="compressed"></param>
        /// <param name="full"></param>
        public GbaPalette(Romfile rom, uint offset, bool compressed, int colors)
        {
            isCompressed = compressed;
            rom.Offset = offset;

            if (compressed)
                palette = Decryption.Decode(rom.Binary, offset);
            else
                palette = rom.ReadBytes(colors * 2);
        }

        #endregion

        #region public methods

        /// <summary>
        /// Converts the internal byte array
        /// and returns it for further usage.
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            if (isCompressed)
                return Decryption.Encode(palette);
            else
                return palette;
        }

        /// <summary>
        /// Converts the internal byte array
        /// into a 16 or 256 color array.
        /// </summary>
        /// <returns></returns>
        public Color[] ToPalette()
        {
            int count = palette.Length / 2;
            var colors = new Color[count];
            int processed = 0;

            for (int i = 0; i < count; i++)
            {
                var hword = (ushort)((palette[processed + 1] << 8) | palette[processed]);
                var blue = (((hword & 0x7C00) >> 10) * 8);
                var green = (((hword & 0x3E0) >> 5) * 8);
                var red = ((hword & 0x1F) * 8);

                colors[i] = Color.FromArgb(red, green, blue);
                processed += 2;
            };

            return colors;
        }

        #endregion
    }
}