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
using System.Runtime.CompilerServices;

namespace SharpBoyAdvance
{
    public class Romfile : IDisposable
    {
        #region public fields

        private byte[] binary;
        /// <summary>
        /// Returns the whole rom data as
        /// an array of byte values.
        /// </summary>
        public byte[] Binary
        {
            get { return binary; }
        }

        private int size;
        /// <summary>
        /// Returns the size of the binary data.
        /// </summary>
        public int Size
        {
            get { return size; }
        }

        private string path;
        /// <summary>
        /// Returns the path of the file.
        /// </summary>
        public string FilePath
        {
            get { return path; }
        }

        private uint offset;
        /// <summary>
        /// Gets or sets the current streaming
        /// position in the binary array.
        /// </summary>
        public uint Offset
        {
            get { return offset; }
            set 
            { 
                if (value < size)
                    offset = value; 
            }
        }

        private string title;
        /// <summary>
        /// Gets or sets the title of the gba rom.
        /// Can for example be: POKEMON FIRE
        /// </summary>
        public string Title
        {
            get { return title; }
            set
            {
                if (value.Length == 12)
                {
                    byte[] bytes = encoding.GetBytes(value);
                    for (int i = 0; i < 12; i++)
                    {
                        binary[0xA0 + i] = bytes[i];
                    };
                }
            }
        }

        private string code;
        /// <summary>
        /// Gets or sets the code of the gba rom.
        /// Can for example be: BPRE, BPEE
        /// </summary>
        public string Code
        {
            get { return code; }
            set
            {
                if (value.Length == 4)
                {
                    byte[] bytes = encoding.GetBytes(value);
                    for (int i = 0; i < 4; i++)
                    {
                        binary[0xAC + i] = bytes[i];
                    };
                }
            }
        }

        /// <summary>
        /// The ANSI encoding used to read
        /// specific values from the rom.
        /// </summary>
        private Encoding encoding;

        #endregion

        #region constructors

        /// <summary>
        /// Reads all bytes from the given filepath
        /// and constructs a new rom with it.
        /// </summary>
        /// <param name="filepath"></param>
        public Romfile(string filepath)
        {
            binary = File.ReadAllBytes(filepath);
            size = binary.Length;
            path = filepath;
            construct();
        }

        /// <summary>
        /// Constructs a new rom from a given
        /// source array. Not commonly used.
        /// </summary>
        /// <param name="source"></param>
        public Romfile(byte[] source)
        {
            size = source.Length;
            path = string.Empty;
            binary = source;
            construct();
        }

        #endregion

        #region private funcs

        /// <summary>
        /// Fills all the properties.
        /// </summary>
        private void construct()
        {
            byte[] tbytes = new byte[12];
            for (int i = 0; i < 12; i++)
            {
                tbytes[i] = binary[0xA0 + i];
            };

            byte[] sbytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                sbytes[i] = binary[0xAC + i];
            };

            this.encoding = Encoding.GetEncoding(1252);
            this.title = encoding.GetString(tbytes);
            this.code = encoding.GetString(sbytes);
        }

        #endregion

        #region public methods

        #region clean-up

        private bool disposed = false;
        /// <summary>
        /// Cleans up all the managed resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Dereferences the binary array
        /// and destroys the properties.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                    binary = null;

                title = null;
                code = null;
                size = 0x0;
            }
        }

        #endregion

        #region access

        /// <summary>
        /// Reads a single byte from the binary
        /// data and increases the offset by 1.
        /// </summary>
        /// <returns></returns>
        public byte ReadByte()
        {
            byte b = binary[offset];
            this.offset += 1;
            return b;
        }

        /// <summary>
        /// Reads two bytes from the binary data in
        /// reversed order and increases the offset by 2.
        /// </summary>
        /// <returns></returns>
        public ushort ReadHWord()
        {
            byte lo = binary[offset];
            byte hi = binary[offset + 1];
            this.offset += 2;
            return (ushort)((hi << 8) | lo);
        }

        /// <summary>
        /// Reads four bytes from the binary data in
        /// reversed order and increases the offset by 4.
        /// </summary>
        /// <returns></returns>
        public uint ReadWord()
        {
            byte b1 = binary[offset];
            byte b2 = binary[offset + 1];
            byte b3 = binary[offset + 2];
            byte b4 = binary[offset + 3];
            this.offset += 4;
            return (uint)((b4 << 24) | (b3 << 16) | (b2 << 8) | b1);
        }

        /// <summary>
        /// Reads a four-byte pointer from the binary data
        /// and returns the offset where its pointing at.
        /// </summary>
        /// <returns></returns>
        public uint ReadPointer()
        {
            return (ReadWord() & 0x1FFFFFF);
        }

        /// <summary>
        /// Reads multiple bytes from the binary data
        /// and increases the offset by count.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public byte[] ReadBytes(int count)
        {
            byte[] bytes = new byte[count];
            for (int i = 0; i < count; i++)
            {
                bytes[i] = binary[offset + i];
            };

            this.offset += (uint)(count);
            return bytes;
        }

        /// <summary>
        /// Reads multiple chars in the default
        /// encoding of the romfile and increases
        /// the offset by the specified count.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public string ReadChars(int count)
        {
            byte[] bytes = new byte[count];
            for (int i = 0; i < count; i++)
            {
                bytes[i] = binary[offset + i];
            };

            this.offset += (uint)(count);

            return encoding.GetString(bytes);
        }

        /// <summary>
        /// Reads a pokemon-encoded string. First
        /// reads bytes until byte 0xFF is hit and
        /// then starting conversion to a string.
        /// </summary>
        /// <returns></returns>
        public string ReadText()
        {
            var bytes = new List<byte>();
            for (; ; )
            {
                byte b = binary[offset];
                if (b == 0xFF)
                    break;
                else
                    bytes.Add(b);
                offset += 1;
            };

            return Poketext.Decode(bytes.ToArray());
        }

        /// <summary>
        /// Writes a single byte to the binary 
        /// data and increases the offset by 1.
        /// </summary>
        /// <param name="u8"></param>
        public void WriteByte(byte u8)
        {
            binary[offset] = u8;
            offset += 1;
        }

        /// <summary>
        /// Writes two bytes to the binary data
        /// and increases the offset by 2.
        /// </summary>
        /// <param name="u16"></param>
        public void WriteHWord(ushort u16)
        {
            binary[offset] = (byte)(u16 & 255);
            binary[offset + 1] = (byte)(u16 >> 8);
            offset += 2;
        }

        /// <summary>
        /// Writes four bytes to the binary data
        /// and increases the offset by 4.
        /// </summary>
        /// <param name="u32"></param>
        public void WriteWord(uint u32)
        {
            binary[offset] = (byte)(u32 & 255);
            binary[offset + 1] = (byte)(u32 >> 24);
            binary[offset + 2] = (byte)(u32 >> 16);
            binary[offset + 3] = (byte)(u32 >> 8);
            offset += 4;
        }

        /// <summary>
        /// Writes a four-byte pointer to the binary
        /// data and increases the offset by 4.
        /// </summary>
        /// <param name="offset"></param>
        public void WritePointer(uint offset)
        {
            WriteWord(offset + 0x08000000);
        }

        /// <summary>
        /// Writes multiple bytes to the binary data
        /// and increases the offset by []-length.
        /// </summary>
        /// <param name="bytes"></param>
        public void WriteBytes(byte[] bytes)
        {
            int length = bytes.Length;
            for (int i = 0; i < length; i++)
            {
                binary[offset + i] = bytes[i];
            };

            this.offset += (uint)(length);
        }

        /// <summary>
        /// Writes multiple chars in the default
        /// encoding of the rom file and increases
        /// the offset by the length of string.
        /// </summary>
        /// <param name="chars"></param>
        public void WriteChars(string chars)
        {
            byte[] bytes = encoding.GetBytes(chars);
            int length = bytes.Length;
            for (int i = 0; i < length; i++)
            {
                binary[offset + i] = bytes[i];
            };

            this.offset += (uint)(length);
        }

        /// <summary>
        /// Writes a pokemon-encoded string to binary and
        /// increases the offset by the length of string.
        /// </summary>
        /// <param name="text"></param>
        public void WriteText(string text)
        {
            byte[] bytes = Poketext.Encode(text);
            int length = bytes.Length;
            for (int i = 0; i < length; i++)
            {
                binary[offset + i] = bytes[i];
            };

            this.offset += (uint)(length);
        }

        #endregion

        #region useful

        /// <summary>
        /// Starts looping through the rom at 'begin' and
        /// returns every found instance of the 'source'[].
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<long> SearchBytes(uint begin, byte[] source)
        {
            int length = source.Length;
            var offsets = new List<long>();
            var current = GetIndexOf(source[0], begin);

            while (current >= 0 && current <= (size - length))
            {
                byte[] bytes = BlockCopy(current, length);
                if (ArraysEqual(bytes, source))
                    offsets.Add(current);
                current = GetIndexOf(source[0], (uint)(current + length));
            };

            return offsets;
        }

        /// <summary>
        /// Finds blank space for the given amount of bytes and
        /// returns the first offset which matches the conditions.
        /// Returns 0xFFFFFFFF if no blank space has been found.
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="count"></param>
        /// <param name="search"></param>
        /// <returns></returns>
        public uint FindSpace(uint begin, int count, byte search)
        {
            int hitcount = 0;
            long found = begin;
            this.offset = begin;

            while (offset < size && hitcount < count)
            {
                if (ReadByte() != search)
                {
                    hitcount = 0;
                    found = offset;
                }
                else
                {
                    hitcount++;
                }
            };

            if (count != hitcount)
                return 0xFFFFFFFF;
            else
                return (uint)(found);
        }

        /// <summary>
        /// Returns the first found index of
        /// the byte in the unmanaged memory.
        /// </summary>
        /// <param name="b"></param>
        /// <param name="index"></param>
        private long GetIndexOf(byte b, uint index)
        {
            for (uint i = index; i < size; i++)
            {
                if (binary[i] == b)
                    return i;
            };

            return -1;
        }

        /// <summary>
        /// Copies bytes from the rom into a byte array.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        private byte[] BlockCopy(long offset, int length)
        {
            byte[] bytes = new byte[length];
            for (int i = 0; i < length; i++)
            {
                bytes[i] = binary[offset + i];
            };

            return bytes;
        }

        /// <summary>
        /// Checks whether two arrays are the same.
        /// </summary>
        /// <param name="b1"></param>
        /// <param name="b2"></param>
        private bool ArraysEqual(byte[] b1, byte[] b2)
        {
            int count = b1.Length;
            for (int i = 0; i < count; i++)
            {
                if (b1[i] != b2[i])
                    return false;
            };

            return true;
        }

        #endregion

        #region in/out

        /// <summary>
        /// Saves the current binary data
        /// into the previously used file.
        /// </summary>
        /// <returns></returns>
        public bool Save()
        {
            bool success = CheckPathExists(path);
            if (success)
                File.WriteAllBytes(path, binary);

            return success;
        }

        /// <summary>
        /// Saves the current binary data
        /// into a brand new .gba file.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public bool Save(string file)
        {
            bool success = CheckPathExists(file);
            if (success)
                File.WriteAllBytes(file, binary);

            this.path = file;
            return success;
        }

        /// <summary>
        /// Checks whether the directory, where the rom
        /// is located, exists or not.
        /// </summary>
        /// <returns></returns>
        private bool CheckPathExists(string path)
        {
            return Directory.Exists(Path.GetDirectoryName(path));
        }

        #endregion

        #endregion
    }
}