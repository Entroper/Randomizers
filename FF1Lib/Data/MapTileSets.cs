﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FF1Lib
{
	public class MapTileSets : MemTable<byte, MapId>
	{
		public MapTileSets(FF1Rom _rom) : base(_rom, 0x2CC0, 64)
		{

		}
	}
}
