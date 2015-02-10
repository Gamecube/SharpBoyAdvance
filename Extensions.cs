// GameBoyAdvanced - A fast library to access and modify ROMs.
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
using System.Text;
using System.Drawing;
using System.Drawing.Text;
using System.Drawing.Design;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Drawing.Configuration;
using System.Runtime.InteropServices;

namespace SharpBoyAdvance
{
    public static class ImageExtensions
    {
        #region bitmap image

        /// <summary>
        /// Receives the pointer to the location of the
        /// locked bitmap-data which is needed for the
        /// extension methods GetPixel and SetPixel.
        /// </summary>
        /// <param name="data"></param>
        public static unsafe byte* GetPointer(this BitmapData data)
        {
            return (byte*)(data.Scan0.ToPointer());
        }

        /// <summary>
        /// Gets the byte that represents two pixel's colors.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="stride"></param>
        /// <param name="pointer"></param>
        /// <returns></returns>
        public static unsafe byte FastGet4Bpp(this Bitmap source, int x, int y, int stride, byte* pointer)
        {
            var offset = ((stride * y) + (x >> 1));
            return *(pointer + offset);
        }

        /// <summary>
        /// Gets the byte that represents one pixel's color.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="stride"></param>
        /// <param name="pointer"></param>
        /// <returns></returns>
        public static unsafe byte FastGet8Bpp(this Bitmap source, int x, int y, int stride, byte* pointer)
        {
            var offset = ((stride * y) + x);
            return *(pointer + offset);
        }

        /// <summary>
        /// Sets a pixel at the specified position with the specified color.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="stride"></param>
        /// <param name="pointer"></param>
        /// <param name="color"></param>
        public static unsafe void FastSet4bpp(this Bitmap source, int x, int y, int stride, byte* pointer, Color color)
        {
            var offset = ((stride * y) + (x >> 1));
            int value = Marshal.ReadByte((IntPtr)pointer, offset);
            int index = source.Palette.Entries.GetIndex(color);
            if (index != -1)
            {
                if ((x & 1) == 1)
                    value = ((value & 0xF0) | (index));
                else
                    value = ((value & 0x0F) | (index << 4));
                Marshal.WriteByte((IntPtr)pointer, offset, (byte)value);
            }
        }

        /// <summary>
        /// Sets a pixel at the specified position with the specified color.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="stride"></param>
        /// <param name="pointer"></param>
        /// <param name="color"></param>
        public static unsafe void FastSet8bpp(this Bitmap source, int x, int y, int stride, byte* pointer, Color color)
        {
            var offset = ((stride * y) + x);
            int index = source.Palette.Entries.GetIndex(color);
            if (index != -1)
            {
                Marshal.WriteByte((IntPtr)pointer, offset, (byte)index);
            }
        }

        #endregion
    }

    public static class ArrayExtensions
    {
        #region color array

        /// <summary>
        /// Returns the index of a color in a palette.
        /// Returns -1 if a non-existing color was passed.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="value"></param>
        public static int GetIndex(this Color[] array, Color value)
        {
            int length = array.Length;
            for (int i = 0; i < length; i++)
                if (ColorsEqual(array[i], value))
                    return i;
            return -1;
        }

        /// <summary>
        /// Checks if the RGB values of two colors are the same.
        /// </summary>
        /// <param name="c1"></param>
        /// <param name="c2"></param>
        /// <returns></returns>
        private static bool ColorsEqual(Color c1, Color c2)
        {
            return (c1.R == c2.R && c1.G == c2.G && c1.B == c2.B);
        }

        #endregion

        #region integer array

        public static int IndexOf(this int[] array, int value)
        {
            int length = array.Length;
            for (int i = 0; i < length; i++)
                if (array[i] == value)
                    return i;
            return -1;
        }

        #endregion

        #region string array

        public static int IndexOf(this string[] array, string value)
        {
            int length = array.Length;
            for (int i = 0; i < length; i++)
                if (array[i] == value)
                    return i;
            return -1;
        }

        #endregion

        #region byte array

        public static int IndexOf(this byte[] array, byte value)
        {
            int length = array.Length;
            for (int i = 0; i < length; i++)
                if (array[i] == value)
                    return i;
            return -1;
        }

        #endregion
    }
}