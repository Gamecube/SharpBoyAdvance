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
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SharpBoyAdvance
{
    public unsafe class Decryption
    {
        #region constant variables

        private const int WINDOW_SIZE = 0x1000;
        private const int BUFFER_SIZE = 0x12;
        private const int BLOCKR_SIZE = 0x8;

        #endregion

        #region decompression funcs

        /// <summary>
        /// Decodes LZ77 data at a specified offset.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="offset"></param>
        public static byte[] Decode(byte[] file, long offset)
        {
            fixed (byte* rom = &file[0])
            {
                byte* pointer = (rom + offset);
                byte[] decode = new byte[(*(uint*)pointer) >> 8];

                if (decode.Length > 0)
                {
                    fixed (byte* target = &decode[0])
                    {
                        if (!Decode(pointer, target))
                            decode = null;
                    };
                }

                return decode;
            }
        }

        /// <summary>
        /// Decodes the data at pointer and stores it
        /// at target and returns the success of the process.
        /// </summary>
        /// <param name="pointer"></param>
        /// <param name="target"></param>
        private static bool Decode(byte* pointer, byte* target)
        {
            if (*pointer++ != 0x10)
                return false;

            int position = 0;
            int length = (*(pointer++) + (*(pointer++) << 8) + (*(pointer++) << 16));

            while (position < length)
            {
                byte isDecoded = *(pointer++);
                for (int i = 0; i < BLOCKR_SIZE; i++)
                {
                    if ((isDecoded & 0x80) != 0)
                    {
                        int count = ((*pointer >> 4) + 3);
                        int cpos = (((*(pointer++) & 0xF) << 8) + *(pointer++) + 1);
                        if (cpos > length)
                            return false;

                        for (int j = 0; j < count; j++)
                        {
                            target[position] = target[(position - j - cpos) + (j % cpos)];
                            position += 1;
                        };
                    }
                    else
                    {
                        target[position++] = *(pointer++);
                    }
                    if (position >= length)
                    {
                        break;
                    }
                    unchecked
                    {
                        isDecoded <<= 1;
                    }
                };
            };

            return true;
        }

        #endregion

        #region compression funcs

        public static byte[] Encode(byte[] source)
        {
            fixed (byte* pointer = &source[0])
            {
                return Encode(pointer, source.Length);
            }
        }

        /// <summary>
        /// Encodes an unmanaged byte array and returns
        /// a managed byte array with the compressed data.
        /// </summary>
        /// <param name="pointer"></param>
        /// <param name="length"></param>
        private static byte[] Encode(byte* pointer, int length)
        {
            var encode = new List<byte>();
            int position = 0; encode.Add(0x10);

            byte* temp = (byte*)(&length);
            for (int i = 0; i < 3; i++)
                encode.Add(*(pointer++));

            while (position < length)
            {
                byte isEncoded = 0x0;
                var inline = new List<byte>();

                for (int j = 0; j < BLOCKR_SIZE; j++)
                {
                    int[] search = Search(pointer, position, length);
                    if (search[0] > 2)
                    {
                        byte lo = (byte)((((search[0] - 3) & 0xF) << 4) + (((search[1] - 1) >> 8) & 0xF));
                        inline.Add(lo); byte hi = (byte)((search[1] - 1) & 0xFF); inline.Add(hi);
                        position += search[0];
                        isEncoded |= (byte)(1 << (BLOCKR_SIZE - (j + 1)));
                    }
                    else if (search[0] >= 0)
                    {
                        inline.Add(pointer[position++]);
                    }
                    else
                    {
                        break;
                    }
                };

                encode.Add(isEncoded);
                encode.AddRange(inline);
            };

            while (encode.Count % 4 != 0)
            {
                encode.Add(0x0);
            };

            return encode.ToArray();
        }

        /// <summary>
        /// Searches for a byte row in the source
        /// array and returns the results.
        /// </summary>
        /// <param name="pointer"></param>
        /// <param name="position"></param>
        /// <param name="length"></param>
        private static int[] Search(byte* pointer, int position, int length)
        {
            if (position >= length)
                return new int[] { -0x1, 0x0 };
            if ((position < 2) || ((length - position) < 2))
                return new int[] { 0x0, 0x0 };

            var result = new List<int>();
            for (int i = 1; (i < WINDOW_SIZE) && (i < position); i++)
            {
                if (pointer[position - (i + 1)] == pointer[position])
                    result.Add(i + 1);
            };

            if (result.Count == 0)
                return new int[] { 0x0, 0x0 };

            int count = 0;
            var sWitch = true;
            while (count < BUFFER_SIZE && sWitch)
            {
                count += 1;
                for (int i = result.Count - 1; i >= 0; i--)
                {
                    if (pointer[position + count] != pointer[(position - result[i]) + (count % result[i])])
                    {
                        if (result.Count > 1)
                            result.RemoveAt(i);
                        else
                            sWitch = false;
                    }
                }
            };

            return new int[] { count, result[0] };
        }

        #endregion
    }
}