﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FF1Lib
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct TileProp
	{
		public byte Byte1;
		public byte Byte2;

		public byte ShopId
		{
			get { return Byte2; }
			set { Byte2 = value; }
		}

		public TileProp(byte p1, byte p2)
		{
			Byte1 = p1;
			Byte2 = p2;
		}
	}

	public class TileSet
	{
		public MemTable<TileProp> TileProperties;
		public MemTable<byte> TileAttributes;
		public MemTable<byte> TopLeftTiles;
		public MemTable<byte> TopRightTiles;
		public MemTable<byte> BottemLeftTiles;
		public MemTable<byte> BottemRightTiles;

		public TileSet(FF1Rom _rom, byte idx)
		{
			TileProperties = new MemTable<TileProp>(_rom, 0x800 + 0x100 * idx, 128);
			TileAttributes = new MemTable<byte>(_rom, 0x400 + 0x80 * idx, 128);

			TopLeftTiles = new MemTable<byte>(_rom, 0x1000 + 0x200 * idx, 128);
			TopRightTiles = new MemTable<byte>(_rom, 0x1080 + 0x200 * idx, 128);
			BottemLeftTiles = new MemTable<byte>(_rom, 0x1100 + 0x200 * idx, 128);
			BottemRightTiles = new MemTable<byte>(_rom, 0x1180 + 0x200 * idx, 128);
		}

		public void LoadData()
		{
			TileProperties.LoadTable();
			TileAttributes.LoadTable();
			TopLeftTiles.LoadTable();
			TopRightTiles.LoadTable();
			BottemLeftTiles.LoadTable();
			BottemRightTiles.LoadTable();
		}

		public void StoreData()
		{
			TileProperties.StoreTable();
			TileAttributes.StoreTable();
			TopLeftTiles.StoreTable();
			TopRightTiles.StoreTable();
			BottemLeftTiles.StoreTable();
			BottemRightTiles.StoreTable();
		}
	}
}
