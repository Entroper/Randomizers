﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RomUtilities;

namespace FF1Lib
{
	// ReSharper disable once InconsistentNaming
	public static class FF1Text
	{
		private static readonly Dictionary<string, byte> BytesByText;
		private static readonly string[] TextByBytes;

		static FF1Text()
		{
			BytesByText = new Dictionary<string, byte>();
			TextByBytes = new string[256];

			var lines = File.ReadAllLines($"{AppContext.BaseDirectory}/DTETable.txt");
			foreach (var line in lines)
			{
				var sides = line.Split('=');
				if (sides[1] == "\\n")
				{
					sides[1] = "\n";
				}

				var b = byte.Parse(sides[0], NumberStyles.HexNumber);
				BytesByText[sides[1]] = b;
				TextByBytes[b] = sides[1];
			}
		}

		public static string BytesToText(byte[] bytes)
		{
			var builder = new StringBuilder();
			foreach (var b in bytes)
			{
				builder.Append(TextByBytes[b]);
			}

			return builder.ToString();
		}

		public static Blob TextToBytes(string text, bool useDTE = true, bool skipNullTerminator = false)
		{
			Blob bytes = new byte[text.Length + 1];
			int i = 0, j = 0;
			while (i < text.Length - 1)
			{
				var twoChars = text.Substring(i, 2);
				if (useDTE && BytesByText.ContainsKey(twoChars))
				{
					bytes[j++] = BytesByText[twoChars];
					i += 2;
				}
				else
				{
					bytes[j++] = BytesByText[text[i++].ToString()];
				}
			}

			if (i < text.Length)
			{
				bytes[j++] = BytesByText[text[i++].ToString()];
			}

			return bytes.SubBlob(0, skipNullTerminator ? j : j + 1);
		}

		// This wraps TextToBytes for use with Credits pages.
		public static Blob TextToCredits(string[] lines)
		{
			// Starting PPU addr immediately inside the box without any padding.
			// Each line is 0x20 total characters.
			ushort topLeftOfBox = 0x20A5;

			List<Blob> buffers = new List<Blob>();
			for (int i = 0; i < lines.Length; ++i)
			{
				string line = lines[i].Trim();
				if (line == "")
				{
					continue;
				}

				int spaces = lines[i].Length - lines[i].TrimStart(' ').Length;
				ushort[] ppuPtr = { (ushort)(topLeftOfBox + (0x20 * i) + spaces) };
				buffers.Add(Blob.FromUShorts(ppuPtr));
				buffers.Add(TextToBytes(line, false, true));
				buffers.Add(Blob.FromHex("01"));
			}

			if (buffers.Count() != 0)
			{
				buffers.RemoveAt(buffers.Count() - 1);
				buffers.Add(Blob.FromHex("00"));
			}
			return Blob.Concat(buffers);
		}
	}
}
