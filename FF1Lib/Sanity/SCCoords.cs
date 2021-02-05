﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FF1Lib.Sanity
{
	public struct SCCoords
	{
		public byte X { get; set; }

		public byte Y { get; set; }

		public SCCoords(int x, int y)
		{
			X = (byte)x;
			Y = (byte)y;
		}

		public override string ToString()
		{
			return "(" + X.ToString() + ", " + Y.ToString() + ")";
		}

		public SCCoords SmLeft => new SCCoords((0x3F + X) & 0x3F, Y);

		public SCCoords SmRight => new SCCoords((X + 1) & 0x3F, Y);

		public SCCoords SmUp => new SCCoords(X, (0x3F + Y) & 0x3F);

		public SCCoords SmDown => new SCCoords(X, (Y + 1) & 0x3F);

		public static bool operator ==(SCCoords a, SCCoords b)
		{
			return a.X == b.X && a.Y == b.Y;
		}

		public static bool operator !=(SCCoords a, SCCoords b)
		{
			return a.X != b.X || a.Y != b.Y;
		}

		static SCCoordsEqualityComparer cec = new SCCoordsEqualityComparer(); 

		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}

	public class SCCoordsEqualityComparer : IEqualityComparer<SCCoords>
	{
		public bool Equals(SCCoords x, SCCoords y)
		{
			return x.X == y.X && x.Y == y.Y;
		}

		public int GetHashCode([DisallowNull] SCCoords obj)
		{
			return obj.X & 0x0F + (obj.Y & 0x0f) * 16 + (obj.X & 0xF0) * 256 + (obj.Y & 0xF0) * 4096;
		}
	}
}
