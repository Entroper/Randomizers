﻿using RomUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace FF1Lib
{
	public enum PoolSize
	{
		[Description("4 characters")]
		Size4,
		[Description("5 characters")]
		Size5,
		[Description("6 characters")]
		Size6,
		[Description("7 characters")]
		Size7,
		[Description("8 characters")]
		Size8,
	}
	public partial class FF1Rom : NesRom
	{
		public const int Nop = 0xEA;
		public const int SardaOffset = 0x393E9;
		public const int SardaSize = 7;
		public const int CanoeSageOffset = 0x39482;
		public const int CanoeSageSize = 5;
		public const int PartyShuffleOffset = 0x312E0;
		public const int PartyShuffleSize = 3;
		public const int MapSpriteOffset = 0x03400;
		public const int MapSpriteSize = 3;
		public const int MapSpriteCount = 16;

		public const int CaravanFairyCheck = 0x7C4E5;
		public const int CaravanFairyCheckSize = 7;

		public const string BattleBoxDrawInFrames = "06"; // Half normal (Must divide 12)
		public const string BattleBoxDrawInRows = "02";

		public const string BattleBoxUndrawFrames = "04"; // 2/3 normal (Must  divide 12)
		public const string BattleBoxUndrawRows = "03";

		// Required for npc quest item randomizing
		public void PermanentCaravan()
		{
			Put(CaravanFairyCheck, Enumerable.Repeat((byte)Nop, CaravanFairyCheckSize).ToArray());
		}
		// Required for npc quest item randomizing
		// Doesn't substantially change anything if EnableNPCsGiveAnyItem isn't called
		public void CleanupNPCRoutines()
		{
			// Have ElfDoc set his own flag instead of the prince's so that
			// the prince can still set his own flag after giving a shuffled item
			Data[0x39302] = (byte)ObjectId.ElfDoc;
			Data[0x3931F] = (byte)ObjectId.ElfDoc;

			// Convert Talk_ifcanoe into Talk_ifairship
			Data[0x39534] = UnsramIndex.AirshipVis;
			// Point Talk_ifairship person to old Talk_ifcanoe routine
			Data[0x391B5] = 0x33;
			Data[0x391B6] = 0x95;

			// Then we move Talk_earthfire to Talk_norm to clear space for
			// new item gift routine without overwriting Talk_chime
			Data[0x391D3] = 0x92;
			Data[0x391D4] = 0x94;

			// Swap string pointer in index 2 and 3 for King, Bikke, Prince, and Lefein
			var temp = Data[ItemLocations.KingConeria.Address];
			Data[ItemLocations.KingConeria.Address] = Data[ItemLocations.KingConeria.Address - 1];
			Data[ItemLocations.KingConeria.Address - 1] = temp;
			temp = Data[ItemLocations.Bikke.Address];
			Data[ItemLocations.Bikke.Address] = Data[ItemLocations.Bikke.Address - 1];
			Data[ItemLocations.Bikke.Address - 1] = temp;
			temp = Data[ItemLocations.ElfPrince.Address];
			Data[ItemLocations.ElfPrince.Address] = Data[ItemLocations.ElfPrince.Address - 1];
			Data[ItemLocations.ElfPrince.Address - 1] = temp;
			temp = Data[ItemLocations.CanoeSage.Address - 1];
			Data[ItemLocations.CanoeSage.Address - 1] = Data[ItemLocations.CanoeSage.Address - 2];
			Data[ItemLocations.CanoeSage.Address - 2] = temp;
			temp = Data[ItemLocations.Lefein.Address];
			Data[ItemLocations.Lefein.Address] = Data[ItemLocations.Lefein.Address - 1];
			Data[ItemLocations.Lefein.Address - 1] = temp;

			// And do the same swap in the vanilla routines so those still work if needed
			Data[0x392A7] = 0x12;
			Data[0x392AA] = 0x13;
			Data[0x392FC] = 0x13;
			Data[0x392FF] = 0x12;
			Data[0x39326] = 0x12;
			Data[0x3932E] = 0x13;
			Data[0x3959C] = 0x12;
			Data[0x395A4] = 0x13;

			// When getting jump address from lut_MapObjTalkJumpTbl (starting 0x3902B), store
			// it in tmp+4 & tmp+5 (unused normally) instead of tmp+6 & tmp+7 so that tmp+6
			// will still have the mapobj_id (allowing optimizations in TalkRoutines)
			Data[0x39063] = 0x14;
			Data[0x39068] = 0x15;
			Data[0x3906A] = 0x14;
			Data[0x39070] = 0x14;
			Data[0x39075] = 0x15;
			Data[0x39077] = 0x14;
		}

		public void EnableEarlySarda()
		{
			var nops = new byte[SardaSize];
			for (int i = 0; i < nops.Length; i++)
			{
				nops[i] = Nop;
			}

			Put(SardaOffset, nops);
		}

		public void EnableEarlySage()
		{
			var nops = new byte[CanoeSageSize];
			for (int i = 0; i < nops.Length; i++)
			{
				nops[i] = Nop;
			}

			Put(CanoeSageOffset, nops);
		}

		public void PartyRoulette()
		{
			// First, disable the 'B' button to prevent going back after roulette spin
			Data[0x39C92] = 0xF7; // F7 here is really a relative jump value of -9
			Data[0x39C9B] = 0xF7;
			Data[0x39CA4] = 0xF7;
			Data[0x39CBD] = 0xEA;
			Data[0x39CBE] = 0xEA;

			// Then skip the check for directionaly input and just constantly cycle class selection 0 through 5
			Put(0x39D25, Enumerable.Repeat((byte)0xEA, 14).ToArray());
		}

		private enum FF1Class
		{
			Fighter = 0,
			Thief = 1,
			BlackBelt = 2,
			RedMage = 3,
			WhiteMage = 4,
			BlackMage = 5,
			None = 6,
		}

		private readonly List<byte> AllowedClassBitmasks = new List<byte> {
			/*   lut_ClassMask:
             *       ;0=FI,1=TH,  BB,  RM,  WM,  BM, None
             *   .byte $80, $40, $20, $10, $08, $04, $02
			 */
			          0x80,0x40,0x20,0x10,0x08,0x04,0x02};

		private readonly List<FF1Class> DefaultChoices = Enumerable.Range(0, 6).Select(x => (FF1Class)x).ToList();

		void UpdateCharacterFromOptions(int slotNumber, bool forced, IList<FF1Class> options, MT19337 rng)
		{
			const int lut_PtyGenBuf = 0x784AA;       // offset for party generation buffer LUT
			const int lut_AllowedClasses = 0x78110;  // offset for allowed classes per slot LUT

			var i = slotNumber - 1;

			if (forced) // if forced
			{
				FF1Class forcedclass;
				if (options.Any())
				{
					forcedclass = options.PickRandom(rng);
				}
				else
				{
					forcedclass = (FF1Class)(Enum.GetValues(typeof(FF1Class))).
						GetValue(rng.Between(0, slotNumber == 1 ? 5 : 6));
				}
				options.Clear();
				options.Add(forcedclass);
			}

			// don't make any changes if there's nothing to do
			if (!options.Any()) return;

			byte allowedFlags = 0b0000_0000;
			foreach (FF1Class option in options)
			{
				allowedFlags |= AllowedClassBitmasks[(int)option];
			}

			// set default member
			var defaultclass = (forced || !DefaultChoices.SequenceEqual(options)) ? (int)options.PickRandom(rng) : slotNumber - 1;
			Data[lut_PtyGenBuf + i * 0x10] = defaultclass == 6 ? (byte)0xFF : (byte)defaultclass;

			// set allowed classes
			Data[lut_AllowedClasses + i] = allowedFlags;

			options.Clear();
		}

		public void PartyComposition(MT19337 rng, Flags flags, Preferences preferences)
		{
			var options = new List<FF1Class>();

			// Do each slot - so ugly!
			if ((flags.FIGHTER1 ?? false)) options.Add(FF1Class.Fighter);
			if ((flags.THIEF1 ?? false)) options.Add(FF1Class.Thief);
			if ((flags.BLACK_BELT1 ?? false)) options.Add(FF1Class.BlackBelt);
			if ((flags.RED_MAGE1 ?? false)) options.Add(FF1Class.RedMage);
			if ((flags.WHITE_MAGE1 ?? false)) options.Add(FF1Class.WhiteMage);
			if ((flags.BLACK_MAGE1 ?? false)) options.Add(FF1Class.BlackMage);
			UpdateCharacterFromOptions(1, (flags.FORCED1 ?? false), options, rng);

			if ((flags.FIGHTER2 ?? false)) options.Add(FF1Class.Fighter);
			if ((flags.THIEF2 ?? false)) options.Add(FF1Class.Thief);
			if ((flags.BLACK_BELT2 ?? false)) options.Add(FF1Class.BlackBelt);
			if ((flags.RED_MAGE2 ?? false)) options.Add(FF1Class.RedMage);
			if ((flags.WHITE_MAGE2 ?? false)) options.Add(FF1Class.WhiteMage);
			if ((flags.BLACK_MAGE2 ?? false)) options.Add(FF1Class.BlackMage);
			if ((flags.NONE_CLASS2 ?? false)) options.Add(FF1Class.None);
			UpdateCharacterFromOptions(2, (flags.FORCED2 ?? false), options, rng);

			if ((flags.FIGHTER3 ?? false)) options.Add(FF1Class.Fighter);
			if ((flags.THIEF3 ?? false)) options.Add(FF1Class.Thief);
			if ((flags.BLACK_BELT3 ?? false)) options.Add(FF1Class.BlackBelt);
			if ((flags.RED_MAGE3 ?? false)) options.Add(FF1Class.RedMage);
			if ((flags.WHITE_MAGE3 ?? false)) options.Add(FF1Class.WhiteMage);
			if ((flags.BLACK_MAGE3 ?? false)) options.Add(FF1Class.BlackMage);
			if ((flags.NONE_CLASS3 ?? false)) options.Add(FF1Class.None);
			UpdateCharacterFromOptions(3, (flags.FORCED3 ?? false), options, rng);

			if ((flags.FIGHTER4 ?? false)) options.Add(FF1Class.Fighter);
			if ((flags.THIEF4 ?? false)) options.Add(FF1Class.Thief);
			if ((flags.BLACK_BELT4 ?? false)) options.Add(FF1Class.BlackBelt);
			if ((flags.RED_MAGE4 ?? false)) options.Add(FF1Class.RedMage);
			if ((flags.WHITE_MAGE4 ?? false)) options.Add(FF1Class.WhiteMage);
			if ((flags.BLACK_MAGE4 ?? false)) options.Add(FF1Class.BlackMage);
			if ((flags.NONE_CLASS4 ?? false)) options.Add(FF1Class.None);
			UpdateCharacterFromOptions(4, (flags.FORCED4 ?? false), options, rng);

			// Load stats for None
			PutInBank(0x1F, 0xC783, Blob.FromHex("2080B3C931F053EA"));
			PutInBank(0x00, 0xB380, Blob.FromHex("BD0061C9FFD013A9019D0161A9009D07619D08619D0961A931600A0A0A0AA860"));

			//clinic stuff
			PutInBank(0x0E, 0xA6F3, Blob.FromHex("203E9B"));
			PutInBank(0x0E, 0x9B3E, Blob.FromHex("BD0061C9FFD00568684C16A7BD016160"));

			// curing ailments out of battle, allow the waste of things in battle
			PutInBank(0x0E, 0xB388, Blob.FromHex("2077C2EAEAEAEAEAEAEAEA"));
			PutInBank(0x1F, 0xC277, CreateLongJumpTableEntry(0x0F, 0x8BB0));
			PutInBank(0x0F, 0x8BB0, Blob.FromHex("A5626A6A6A29C0AABD0061C9FFD003A90060BD016160"));

			// Better Battle sprite, in 0C_9910_DrawCharacter.asm
			PutInBank(0x0F, 0x8BD0, Blob.FromHex("A86A6A6AAABD0061C9FFF00898AABDA86B8DB36860"));
			PutInBank(0x0C, 0x9910, Blob.FromHex("20A4C8C9FFD001608A0A0AA8"));
			PutInBank(0x1F, 0xC8A4, CreateLongJumpTableEntry(0x0F, 0x8BD0));

			// MapMan for Nones and Fun% Party Leader
			byte leader = (byte)((byte)preferences.MapmanSlot << 6);
            Data[0x7D8BC] = leader;
			PutInBank(0x1F, 0xE92E, Blob.FromHex("20608FEAEAEAEA"));
			PutInBank(0x02, 0x8F60, Blob.FromHex($"A9008510AD{leader:X2}61C9FFF00160A92560"));

			// Draw Complex String extension for class FF
			PutInBank(0x1F, 0xC27D, Blob.FromHex("C9FFF0041869F060203EE0A997853EA9C2853F2045DE4C4EE0EA973CA800"));
			PutInBank(0x1F, 0xDF0C, Blob.FromHex("207DC2"));

			// Skip targeting None with aoe spells
			PutInBank(0x0C, 0xB453, Blob.FromHex("20AAC8C9FFF015EA"));
			PutInBank(0x1F, 0xC8AA, CreateLongJumpTableEntry(0x0F, 0x8BF0));
			PutInBank(0x0F, 0x8BF0, Blob.FromHex("ADCE6BAA6A6A6AA8B90061C9FFF0068A09808D8A6C60"));

			// Rewrite class promotion to not promote NONEs, See 0E_95AE_DoClassChange.asm
			PutInBank(0x0E, 0x95AE, Blob.FromHex("A203BCD095B900613006186906990061CA10EFE65660"));
			PutInBank(0x0E, 0x95D0, Blob.FromHex("C0804000")); // lut used by the above code
		}

		public void PubReplaceClinic(MT19337 rng, Flags flags)
		{
			// Copy some CHR data to make the Tavern look more like one.
			const int ShopTileDataOffet = 0x24000;
			const int TileSize = 16;
			const int ArmorTileOffset = 14 * 1 * TileSize;
			const int ClinicTileOffset = 14 * 4 * TileSize;
			const int ItemTileOffset = 14 * 6 * TileSize;
			const int CaravanTileOffset = 14 * 7 * TileSize;
			const int DecorationOffset = TileSize * 4;
			const int VendorOffset = TileSize * 8;

			Put(ShopTileDataOffet + ClinicTileOffset, Get(ShopTileDataOffet + CaravanTileOffset, TileSize * 4)); // Tablecloth
			Put(ShopTileDataOffet + ClinicTileOffset + DecorationOffset, Get(ShopTileDataOffet + ItemTileOffset + DecorationOffset, TileSize * 4)); // Barrels of fine ale
			Put(ShopTileDataOffet + ClinicTileOffset + VendorOffset, Get(ShopTileDataOffet + ArmorTileOffset + VendorOffset, TileSize * 6)); // Armorer tending bar
			Put(0x03250, Get(0x03258, 4)); // Caravan palette

			List<byte> options = new List<byte> { };
			if ((flags.TAVERN1 ?? false)) options.Add(0x0);
			if ((flags.TAVERN2 ?? false)) options.Add(0x1);
			if ((flags.TAVERN3 ?? false)) options.Add(0x2);
			if ((flags.TAVERN4 ?? false)) options.Add(0x3);
			if ((flags.TAVERN5 ?? false)) options.Add(0x4);
			if ((flags.TAVERN6 ?? false)) options.Add(0x5);

			if (options.Count == 0) options = new List<byte> { 0x0, 0x1, 0x2, 0x3, 0x4, 0x5 };
			List<byte> pub_lut = new List<byte> { };
			while (pub_lut.Count < 7)
			{
				options.Shuffle(rng);
				pub_lut.AddRange(options);
			}
			pub_lut.Insert(3, (byte) 0xFF); // Will break if Melmond ever gets a clinic, Nones will need to be hired dead, this results in them being alive.
			Put(0x38066, Blob.FromHex("9D8A9F8E9B97")); // Replaces "CLINIC" with "TAVERN"
														// EnterClinic
			PutInBank(0x0E, 0xA5A1, Blob.FromHex("60A9008524852520589DA902856318201C9DB0ECAD62008D0D0320EDA6B0034C6CA620D7A6B0DAAD62008D0C03209BAA20C2A8B0CCAD6200D0C72089A6AD0C03186D0C036D0C03AABD10036A6A6A29C0AAAD0D03F0458A690A8510A96185118A488512A9638513A000A90091109112C8C00A30F7C040D0F59D266120E99CA448B90A9D9D00612071C2A00E20799068AA900918BD006169069D0061A9019D0A61A9009D016120349D200CE92078A7A921205BAA2043A7A5240525F0F74CA2A52043A7A5240525F0F74CA1A5A923205BAAA9008D24008D25004C60A6EAEAEAEAEAEAEAEAEAEAEAEAEA"));
			PutInBank(0x0E, 0x9D0A, pub_lut.Take(8).ToArray());
			// Clinic_SelectTarget followed by ClinicBuildNameString
			PutInBank(0x0E, 0xA6D7, Blob.FromHex("A903203BAA20EDA6A910853EA903853F2032AA4C07A9A000A200866320959DD01C8A2A2A2A29036910991003A900991103A90199120398186903A8E6638A186940AAD0D8A900991003A563C90160EAEA"));
			// Moved routine
			PutInBank(0x0E, 0xA3F8, Blob.FromHex("7D9C"));
			PutInBank(0x0E, 0x9C7D, Blob.FromHex("A5626A6A6A29C08D0A03A666D01EA018AE0A03BD1861F032C8BD1961F02CC8BD1A61F026C8BD1B61F0203860A01CAE0A03BD1C61F025C8BD1D61F01FC8BD1E61F019C8BD1F61F013386098186D0A03AAAD0C0338E91B9D0061186098186D0A03AAAD0C0338E9439D00611860"));
			// New routine that unequips all gear and sets all substats to zero
			PutInBank(0x0E, 0x9CE9, Blob.FromHex("188A69188510A9618511A007B110297F91108810F7A900A0089110C8C00ED0F960"));

			PutInBank(0x1F, 0xC271, CreateLongJumpTableEntry(0x00, 0xC783));

			//ReviveorBuy routine
			PutInBank(0x0E, 0x9D1C, Blob.FromHex("A903203BAAA912853EA99D853F2032AAA9028D62004C07A9"));

			// Revive Hire text options
			PutInBank(0x0E, 0x9D12, Blob.FromHex("9BA8B9AC320191AC2300"));

			// Clinic_InitialText followed by ShouldSkipChar followed by "Hire a\n" text
			PutInBank(0x0E, 0x9D58, Blob.FromHex("205BAAA0FFC8B9B09D991003D0F7A902991003C8A648BD0A9D69F0991003C8A905991003C8A9C5991003C8A900991003A9108D3E00A9038D3F004C32AAAD0D03D010BD0061C9FFD003A90160BD0161C90160BD0061C9FF6091AC23C1A40100"));

			// New routine to level up replaced character and zero some stuff, needs new level up stuff in bank 1B
			PutInBank(0x0E, 0x9D34, Blob.FromHex("A99D48A94B48A98748A9A9488A182A2A2A8510A91B4C03FEA9008D24008D25008D012060"));

			Data[0x101A] = 0x13;
			Data[0x109A] = 0x13;
			Data[0x111A] = 0x76;
			Data[0x119A] = 0x77;

			if (flags.RecruitmentModeHireOnly ?? false) {
				PutInBank(0x0E, 0xA5B0, Blob.FromHex("EAEAEAEAEAA901EA"));
				PutInBank(0x0E, 0xA5C7, Blob.FromHex("D9"));
			}

			if (!flags.RecruitmentModeReplaceOnlyNone ?? false)
			{
				PutInBank(0x0E, 0x9DAA, Blob.FromHex("A90060"));
			}


		}

		public void DisablePartyShuffle()
		{
			var nops = new byte[PartyShuffleSize];
			for (int i = 0; i < nops.Length; i++)
			{
				nops[i] = Nop;
			}
			Put(PartyShuffleOffset, nops);

			nops = new byte[2];
			for (int i = 0; i < nops.Length; i++)
			{
				nops[i] = Nop;
			}
			Put(0x39A6B, nops);
			Put(0x39A74, nops);
		}

		public void EnableSpeedHacks()
		{
			// Screen wipe
			Data[0x7D6EE] = 0x08; // These two values must evenly divide 224 (0xE0), default are 2 and 4
			Data[0x7D6F5] = 0x10;
			Data[0x7D713] = 0x0A; // These two values must evenly divide 220 (0xDC), default are 2 and 4
			Data[0x7D71A] = 0x14; // Don't ask me why they aren't the same, it's the number of scanlines to stop the loop at

			// Dialogue boxes
			Data[0x7D620] = 0x0B; // These two values must evenly divide 88 (0x58), the size of the dialogue box
			Data[0x7D699] = 0x0B;

			// Battle entry
			Data[0x7D90A] = 0x11; // This can be just about anything, default is 0x41, sfx lasts for 0x20

			// All kinds of palette cycling
			Data[0x7D955] = 0x00; // This value is ANDed with a counter, 0x03 is default, 0x01 is double speed, 0x00 is quadruple

			// Battle
			Data[0x31ECE] = 0x60; // Double character animation speed
			Data[0x2DFB4] = 0x04; // Quadruple run speed

			Data[0x33D4B] = 0x04; // Explosion effect count (big enemies), default 6
			Data[0x33CCD] = 0x04; // Explosion effect count (small enemies), default 8
			Data[0x33DAA] = 0x04; // Explosion effect count (mixed enemies), default 15

			// Draw and Undraw Battle Boxes faster
			Put(0x2DA12, Blob.FromHex("3020181008040201")); // More practical Respond Rates
			Put(0x7F4AA, Blob.FromHex($"ADFC6048A90F2003FE20008AA9{BattleBoxDrawInFrames}8517A90F2003FE20208A2085F4C617D0F1682003FE60"));
			Put(0x7F4FF, Blob.FromHex($"ADFC6048A90F2003FE20808AA9{BattleBoxUndrawFrames}8517A90F2003FE20A08A2085F4C617D0F1682003FE60"));

			// Gain multiple levels at once.
			//Put(0x2DD82, Blob.FromHex("20789f20579f48a5802907c907f008a58029f0690785806820019c4ce89b")); old
			PutInBank(0x1B, 0x8850, Blob.FromHex("4C4888"));
			// Skip stat up messages
			PutInBank(0x1B, 0x89F0, Blob.FromHex("4CE38B"));

			// Default Response Rate 8 (0-based)
			Data[0x384CB] = 0x07; // Initialize respondrate to 7
			Put(0x3A153, Blob.FromHex("4CF0BF")); // Replace reset respond rate with a JMP to...
			Put(0x3BFF0, Blob.FromHex("A90785FA60")); // Set respondrate to 7

			// Faster Lineup Modifications
			var animationOffsets = new List<int> { 0x39AA0, 0x39AB4, 0x39B10, 0x39B17, 0x39B20, 0x39B27 };
			animationOffsets.ForEach(addr => Data[addr] = 0x04);

			// Move NPCs out of the way.
			MoveNpc(MapId.Coneria, 0, 0x11, 0x02, inRoom: false, stationary: true); // North Coneria Soldier
			MoveNpc(MapId.Coneria, 4, 0x12, 0x14, inRoom: false, stationary: true); // South Coneria Gal
			MoveNpc(MapId.Coneria, 7, 0x1E, 0x0B, inRoom: false, stationary: true); // East Coneria Guy
			MoveNpc(MapId.Elfland, 0, 0x27, 0x18, inRoom: false, stationary: true); // Efland Entrance Elf
			MoveNpc(MapId.Onrac, 13, 0x29, 0x1B, inRoom: false, stationary: true); // Onrac Guy
			MoveNpc(MapId.Lefein, 3, 0x21, 0x07, inRoom: false, stationary: true); // Lefein Guy
																				   //MoveNpc(MapId.Waterfall, 1, 0x0C, 0x34, inRoom: false, stationary: false); // OoB Bat!
			MoveNpc(MapId.EarthCaveB3, 10, 0x09, 0x0B, inRoom: true, stationary: false); // Earth Cave Bat B3
			MoveNpc(MapId.EarthCaveB3, 7, 0x0B, 0x0B, inRoom: true, stationary: false); // Earth Cave Bat B3
			MoveNpc(MapId.EarthCaveB3, 8, 0x0A, 0x0C, inRoom: true, stationary: false); // Earth Cave Bat B3
			MoveNpc(MapId.EarthCaveB3, 9, 0x09, 0x25, inRoom: false, stationary: false); // Earth Cave Bat B3
			MoveNpc(MapId.EarthCaveB5, 1, 0x22, 0x34, inRoom: false, stationary: false); // Earth Cave Bat B5
			MoveNpc(MapId.ConeriaCastle1F, 5, 0x07, 0x0F, inRoom: false, stationary: true); // Coneria Ghost Lady
		}

		public void EnableConfusedOldMen(MT19337 rng)
		{
			List<(byte, byte)> coords = new List<(byte, byte)> {
				( 0x2A, 0x0A ), ( 0x28, 0x0B ), ( 0x26, 0x0B ), ( 0x24, 0x0A ), ( 0x23, 0x08 ), ( 0x23, 0x06 ),
				( 0x24, 0x04 ), ( 0x26, 0x03 ), ( 0x28, 0x03 ), ( 0x28, 0x04 ), ( 0x2B, 0x06 ), ( 0x2B, 0x08 )
			};
			coords.Shuffle(rng);

			List<int> sages = Enumerable.Range(0, 12).ToList(); // But the 12th Sage is actually id 12, not 11.
			sages.ForEach(sage => MoveNpc(MapId.CrescentLake, sage < 11 ? sage : 12, coords[sage].Item1, coords[sage].Item2, inRoom: false, stationary: false));
		}

		public void EnableIdentifyTreasures()
		{
			Put(0x2B192, Blob.FromHex("C1010200000000"));
		}

		public void EnableDash()
		{
			Put(0x7D077, Blob.FromHex("A5424A69004A69000A242050014A853460"));
		}

		public void EnableBuyTen()
		{
			Put(0x380FF, Blob.FromHex("100001110001120001130000"));
			Put(0x38248, Blob.FromHex("8BB8BC018BB8BCFF8180018EBB5B00"));
			Put(0x3A8E4, Blob.FromHex("A903203BAAA9122026AAA90485634C07A9A903203BAAA91F2026AAA90385634C07A9EA"));
			Put(0x3A32C, Blob.FromHex("71A471A4"));
			Put(0x3A45A, Blob.FromHex("2066A420EADD20EFA74CB9A3A202BD0D039510CA10F860A909D002A925205BAAA5664A9009EAEAEAEAEAEAEAEAEA20F5A8B0E3A562F0054A90DCA909856AA90D205BAA2057A8B0D82076AAA66AF017861318A200A003BD0D0375109D0D03E888D0F4C613D0EB2068AA20C2A8B0ADA562D0A9208CAA9005A9104C77A4AE0C03BD206038656AC9649005A90C4C77A49D206020F3A4A9134C77A4A200A00338BD1C60FD0D039D1C60E888D0F34CEFA7"));
			Put(0x3AA65, Blob.FromHex("2076AAA90E205BAA2066A4208E8E4C32AAA662BD00038D0C0320B9ECA202B5109D0D03CA10F860A202BD0D03DD1C60D004CA10F51860"));
			Put(0x3A390, Blob.FromHex("208CAA"));
			Put(0x3A3E0, Blob.FromHex("208CAA"));
			Put(0x3A39D, Blob.FromHex("20F3A4"));
			Put(0x3A404, Blob.FromHex("20F3A4"));
			Put(0x3AACB, Blob.FromHex("18A202B5106A95109D0D03CA10F5"));
		}

		public void EnableBuyQuantity()
		{
			Put(0x39E00, Blob.FromHex("ad0a0385104c668eae0c03bd2060186d0a03c9649001609d206060a903203baaa9018d0a03a520290f856120009e2032aa2043a7a525d056a524d05aa520290fc561f0ed8561c900f0e7c904f02fc908f01ac901f00ace0a03d0d0ee0a03d0cbee0a03c964d0c4ce0a03d0bfad0a0318690a8d0a03c96490b2a96310f5ad0a0338e90af0021002a9018d0a03109d38a90085248525601890f6"));
			Put(0x39E99, Blob.FromHex("a90e205baaa5620a0a0a186916aabd00038d0c0320b9ecae0a03a9008d0b038d0e038d0f0318ad0b0365108d0b03ad0e0365118d0e03ad0f0369008d0f03b005caf00dd0e1a9ff8d0b038d0e038d0f03ad0f038512ad0e038511ad0b03851020429f2032aa60"));
			Put(0x39EFF, Blob.FromHex("ad1e60cd0f03f0049016b016ad1d60cd0e03f004900ab00aad1c60cd0b03b00238601860ad1c6038ed0b038d1c60ad1d60ed0e038d1d60ad1e60ed0f038d1e604cefa74c8e8e"));
			Put(0x3A494, Blob.FromHex("201b9eb0e820999e20c2a8b0e0a562d0dc20ff9e9008a910205baa4c81a420089e9008a90c205baa4c81a420239fa913205baa4c81a4eaeaea"));
		}

		public void NewAstosRoutine(bool isNPCShuffle)
		{
			if (isNPCShuffle)
			{
				// Talk_Astos must jump to new Astos routine in bank 0E (we fill some of the space that was where party generation used to be)
				PutInBank(0x0E, 0x9338, Blob.FromHex("4C459F"));
				var newAstosRoutine = "EAEAEAAD2260F025A513F0212093DDB01E8467A5108D0E03A5118D0F03A416207F90207392A97D20C590A93A60A51160";
				PutInBank(0x0E, 0x9F45, Blob.FromHex(newAstosRoutine));
			}
		}

		public void EnableSaveOnDeath()
		{
			// this routine assumes NewAstosRoutine has been run (which is set to run on all seeds from now on)
			// rewrite rando's GameOver routine to jump to a new section that will save the game data
			PutInBank(0x1B, 0x801A, Blob.FromHex("4CF58F"));
			// write new routine to save data at game over (the game will save when you clear the final textbox and not before)
			PutInBank(0x1B, 0x8FF5, Blob.FromHex("20E38BA200BD0061C9FFF041BD0C619D0A61BD0D619D0B61BD28639D2063BD29639D2163BD2A639D2263BD2B639D2363BD2C639D2463BD2D639D2563BD2E639D2663BD2F639D2763A9009D01618A186940AAD0B1A56AC97FD0062071914CD290C97ED01E207191AD3F6229FE8D3F62AD406229FE8D4062AD416229FE8D41624CD290C97DD009207191207E914CD290C97CD0062071914CD290C97AD0062071914CD290C979D0062071914CD290C978D0062071914CD290C977D0062071914CD290C97BD018AD186209018D1862AD196229FE8D1962AD1A6229FE8D1A62AD0460D02EAD0060F04FAD0160CD0164D008AD0260CD0264F03FAD016038E9078D1060AD026038E9078D1160A9048D1460D026AD056038E9078D1060AD066038E9078D1160A9018D1460AD0060F00AA9988D0160A9A98D0260A200BD00609D0064BD00619D0065BD00629D0066BD00639D0067E8D0E5A9558DFE64A9AA8DFF64A9008DFD64A200187D00647D00657D00667D0067E8D0F149FF8DFD644C1D80A616BD0062090129FD9D006260A513186920C93CB00DAAC90CD004FE006060DE006060A513A467C96C901CAD1C6038ED0E038D1C60AD1D60ED0F038D1D60AD1E60E9008D1E6060C944B009BECE91A9009D006160BEDE91A9009D00616018191A1B58595A5B98999A9BD8D9DADB1C1D1E1F5C5D5E5F9C9D9E9FDCDDDEDF"));
		}

		public void ShuffleAstos(Flags flags, MT19337 rng)
		{
			// Assume NewAstosRoutine() is on
			const int NpcTalkOffset = 0x390D3;
			const int newTalk_Astos = 0x9F45;
			const int newTalk_AstosBank = 0x0E;
			const int NpcTalkSize = 2;

			// NPC pool to swap Astos with
			List<ObjectId> npcpool = new List<ObjectId> { ObjectId.Astos, ObjectId.Bahamut, ObjectId.CanoeSage, ObjectId.CubeBot, ObjectId.ElfDoc,
			ObjectId.Fairy, ObjectId.King, ObjectId.Matoya, ObjectId.Nerrick, ObjectId.Princess2, ObjectId.Smith,
			ObjectId.Titan, ObjectId.Unne, ObjectId.Sarda, ObjectId.ElfPrince, ObjectId.Lefein };

			var scriptStandardNPCItemTrade = Blob.FromHex("5693");
			var scriptTalk_Astos = Blob.FromHex("3893");

			// Select random npc
			ObjectId newastos = npcpool.PickRandom(rng);

			newastos = ObjectId.Sarda;

			// If Astos, we're done here
			if (newastos == ObjectId.Astos) return;

			// If not get NPC talk routine, get NPC object
			var talkscript = Get(NpcTalkOffset + (byte)newastos * NpcTalkSize, 2);
			var talkvalue = Get(MapObjOffset + (byte)newastos * MapObjSize, 4);

			// Switch astos to TalkScripts.StandardNPCItemTrade, set scriptvalue to crown Item.Crown;
			Put(NpcTalkOffset + (byte)ObjectId.Astos * NpcTalkSize, scriptStandardNPCItemTrade);
			Put(MapObjOffset + (byte)ObjectId.Astos * MapObjSize, Blob.FromHex("02")); // 0x02 = Crown

			// Swtich NPC to Astos
			Put(NpcTalkOffset + (byte)newastos * NpcTalkSize, scriptTalk_Astos);

			// Change dialog a bit for non-item giving NPCs
			Put(0x285EF + 18, Blob.FromHex("00"));

			if (newastos == ObjectId.Titan)
			{
				// Check required item, don't try to give any item
				PutInBank(newTalk_AstosBank, newTalk_Astos + 3, Blob.FromHex("AD2960F025EAEAEAEAEAEAEAEA"));

				// Change dialog
				PutInBank(newTalk_AstosBank, newTalk_Astos + 42, Blob.FromHex("A912"));

				// No need to restore item
				if (flags.SaveGameWhenGameOver)
					Put(0x6CFF5 + 0x89, Blob.FromHex("EAEAEA"));
			}
			else if (newastos == ObjectId.Bahamut)
			{   // Change Talk_Astos to make it work with Bahamut, and also modify DoClassChange

				// Change routine to check for Tail, give promotion and trigger the battle at the same time
				var newroutine =
					"AD2D60" +  // LDA item_tail - Load Tail
					"F025" +    // BEQ @Default 
					"A416" +    // LDY tmp+6 - Load this object instead of Astos
					"207F90" +  // JSR SetEventFlag (207F90)
					"207392" +  // JSR HideThisMapObject
					"A97D" +    // LDA #BTL_ASTOS
					"20C590" +  // JSR TalkBattle
					"20AE95" +  // JSR DoClassChange
					"A912" +    // LDA Astos dialog, A512 would be this npc dialog
					"60";       // RTS

				PutInBank(newTalk_AstosBank, newTalk_Astos + 3, Blob.FromHex(newroutine));

				// DoClassChange reload the map to show the new class sprites, this break TalkBattle, so we stop it from reloading the map
				// INC dlgflg_reentermap (E656) => NOPx2 (EAEA)
				if ((bool)flags.EnablePoolParty || (bool)flags.EnableRandomPromotions)
					PutInBank(0x0E, 0x95AE + 20, Blob.FromHex("EAEA"));
				else   
					PutInBank(0x0E, 0x95AE + 19, Blob.FromHex("EAEA"));

				// Modify GameOver routine for compatibility
				if (flags.SaveGameWhenGameOver)
				{
					// Different changes if EnableTwelveClasses is activated
					if ((bool)flags.EnablePoolParty || (bool)flags.EnableRandomPromotions)
					{
						Blob PromoteArray = Get(0x395AE, 12);
						sbyte[] inversedPromoted = new sbyte[12];
						for (int i = 0; i < 12; i++)
						{
							inversedPromoted[PromoteArray[i]] = (sbyte)i;
						}
						PutInBank(0x1B, 0x8FF5 + 0x0189, Blob.FromHex("A200209391A240209391A280209391A2C020939160BC00613006B99F919D006160") + Blob.FromSBytes(inversedPromoted));
					}
					else
						PutInBank(0x1B, 0x8FF5 + 0x0189, Blob.FromHex("A200209391A240209391A280209391A2C020939160BD0061300638E9069D006160"));
				}
			}
			else if (newastos == ObjectId.Unne)
			{
				// Change required item to Slab, don't give any item
				PutInBank(newTalk_AstosBank, newTalk_Astos + 3, Blob.FromHex("AD2860F025EAEAEAEAEAEAEAEA"));

				// Change dialog
				PutInBank(newTalk_AstosBank, newTalk_Astos + 42, Blob.FromHex("A912"));

				// No need to restore item
				if (flags.SaveGameWhenGameOver)
					PutInBank(0x1B, 0x8FF5 + 0x89, Blob.FromHex("EAEAEA"));
			}
			else if (newastos == ObjectId.ElfDoc)
			{
				// Change required item to Herb, don't give any item
				PutInBank(newTalk_AstosBank, newTalk_Astos + 3, Blob.FromHex("AD2460F025EAEAEAEAEAEAEAEA"));

				// Change dialog
				PutInBank(newTalk_AstosBank, newTalk_Astos + 42, Blob.FromHex("A912"));

				// No need to restore item
				if (flags.SaveGameWhenGameOver)
					PutInBank(0x1B, 0x8FF5 + 0x89, Blob.FromHex("EAEAEA"));
			}
			else if (newastos == ObjectId.Lefein)
			{
				// Check if we speak Lefein
				PutInBank(newTalk_AstosBank, newTalk_Astos, Blob.FromHex("A00B207990902618"));

				// Change dialog
				Put(MapObjOffset + (byte)ObjectId.Lefein * MapObjSize + 1, Blob.FromHex("D0"));
			}
			else if (newastos == ObjectId.ElfPrince)
			{
				// Check if we gave the herb                              
				PutInBank(newTalk_AstosBank, newTalk_Astos, Blob.FromHex("A005207990902618"));

				// Change dialog
				Put(MapObjOffset + (byte)ObjectId.ElfPrince * MapObjSize + 1, Blob.FromHex("10"));
			}
			else if (newastos == ObjectId.Sarda && !(flags.EarlySarda ?? false))
			{
				// Check if we defeated the Vampire
				PutInBank(newTalk_AstosBank, newTalk_Astos, Blob.FromHex("A00C209190B02618"));

				// Change dialog
				Put(MapObjOffset + (byte)ObjectId.Sarda * MapObjSize + 1, Blob.FromHex("81"));
			}
			else
			{
				if (talkscript == scriptStandardNPCItemTrade)
				{
					// Check required item
					PutInBank(newTalk_AstosBank, newTalk_Astos + 4, Blob.FromSBytes(new sbyte[] { (sbyte)(talkvalue[0] + 32) }));
				}
				else
				{
					// Just skip item check, then we're set
					PutInBank(newTalk_AstosBank, newTalk_Astos + 3, Blob.FromHex("EAEAEAEAEA"));
				}
			}
		}

		private void EnableEasyMode()
		{
			ScaleEncounterRate(0.20, 0.20);
			var enemies = Get(EnemyOffset, EnemySize * EnemyCount).Chunk(EnemySize);
			foreach (var enemy in enemies)
			{
				var hp = BitConverter.ToUInt16(enemy, 4);
				hp = (ushort)(hp * 0.1);
				var hpBytes = BitConverter.GetBytes(hp);
				Array.Copy(hpBytes, 0, enemy, 4, 2);
			}

			Put(EnemyOffset, enemies.SelectMany(enemy => enemy.ToBytes()).ToArray());
		}

		public void EasterEggs()
		{
			Put(0x2ADDE, Blob.FromHex("91251A682CC18EB1B74DB32505C1BE9296991E2F1AB6A4A9A8BE05C1C1C1C1C1C19B929900"));
		}

		/// <summary>
		/// Unused method, but this would allow a non-npc shuffle king to build bridge without rescuing princess
		/// </summary>
		public void EnableEarlyKing()
		{
			Data[0x390D5] = 0xA1;
		}

		public void EnableFreeBridge()
		{
			// Set the default bridge_vis byte on game start to true. It's a mother beautiful bridge - and it's gonna be there.
			Data[0x3008] = 0x01;
		}

		public void EnableFreeShip()
		{
			Data[0x3000] = 1;
			Data[0x3001] = 152;
			Data[0x3002] = 169;
		}

		public void EnableFreeAirship()
		{
			Data[0x3004] = 1;
			Data[0x3005] = 153;
			Data[0x3006] = 165;
		}

		public void EnableFreeCanal()
		{
			Data[0x300C] = 0;
		}

		public void EnableCanalBridge()
		{
			// Inline edit to draw the isthmus or the bridge, but never the open canal anymore.
			// See 0F_8780_IsOnEitherBridge for this and the IsOnBridge replacement called from below.
			Put(0x7E3BB, Blob.FromHex("A266A0A420DFE3B0E1A908AE0C60F002A910"));

			/**
			 *  A slight wrinkle from normal cross page jump in that we need to preserve the status register,
			 *  since the carry bit is what's used to determine if you're on a bridge or canal, of course.
			 *
			    LDA $60FC
				PHA
				LDA #$0F
				JSR $FE03 ;SwapPRG_L
				JSR $8780
				PLA
				PHP
				JSR $FE03 ;SwapPRG_L
				PLP
				RTS
			**/
			Put(0x7C64D, Blob.FromHex("ADFC6048A90F2003FE20808768082003FE2860"));
		}

		public void EnableChaosRush()
		{
			// Overwrite Keylocked door in ToFR tileset with normal door.
			Put(0x0F76, Blob.FromHex("0300"));
		}

		public void EnableFreeLute()
		{
			Data[0x3020 + (int)Item.Lute] = 0x01;
		}

		public void EnableFreeTail()
		{
			Data[0x3020 + (int)Item.Tail] = 0x01;
		}

		public void EnableFreeOrbs()
		{
			const int initItemOffset = 0x3020;
			Data[initItemOffset + (int)Item.EarthOrb] = 0x01;
			Data[initItemOffset + (int)Item.FireOrb] = 0x01;
			Data[initItemOffset + (int)Item.WaterOrb] = 0x01;
			Data[initItemOffset + (int)Item.AirOrb] = 0x01;
		}

		public void ChangeUnrunnableRunToWait()
		{
			// See Unrunnable.asm
			// Replace DrawCommandMenu with a cross page jump to a replacement that swaps RUN for WAIT if the battle is unrunnable.
			// The last 5 bytes here are the null terminated WAIT string (stashed in some leftover space of the original subroutine)
			Put(0x7F700, Blob.FromHex("ADFC6048A90F2003FE204087682003FE4C48F6A08A929D00"));

			// Replace some useless code with a special handler for unrunnables that prints 'Nothing Happens'
			// We then update the unrunnable branch to point here instead of the generic Can't Run handler
			// See Disch's comments here: Battle_PlayerTryRun  [$A3D8 :: 0x323E8]
			Put(0x32409, Blob.FromHex("189005A94E4C07AAEAEAEAEAEAEAEA"));
			// new delta to special unrunnable message handler done in 
		}

		public void SeparateUnrunnables()
		{
			// See SetRunnability.asm
			// replace a segment of code in PrepareEnemyFormation with a JSR to a new routine in dummied ROM space in bank 0B
			Put(0x2E141, Blob.FromHex("200C9B"));
			// move the rest of PrepareEnemyFormation up pad the excised code with NOPs
			Put(0x2E144, Get(0x2E160, 0x1E));
			PutInBank(0x0B, 0xA162, Enumerable.Repeat((byte)0xEA, 0x1C).ToArray());
			// write the new routine
			Put(0x2DB0C, Blob.FromHex("AD6A001023AD926D8D8A6DAD936D8D8B6DA2008E886D8E896D8E8C6D8E8D6DAD916D29FE8D916D60AD916D29FD8D916D60"));
			// change checks for unrunnability in bank 0C to check last two bits instead of last bit
			Put(0x313D3, Blob.FromHex("03")); // changes AND #$01 to AND #$03 when checking start of battle for unrunnability
			// the second change is done in AllowStrikeFirstAndSurprise, which checks the unrunnability in battle
			// alter the default formation data to set unrunnability of a formation to both sides if the unrunnable flag is set
			var formData = Get(FormationDataOffset, FormationDataSize * FormationCount).Chunk(FormationDataSize);
			for(int i = 0; i < NormalFormationCount; ++i)
			{
				if ((formData[i][UnrunnableOffset] & 0x01) != 0)
					formData[i][UnrunnableOffset] |= 0x02;
			}
			formData[126][UnrunnableOffset] |= 0x02; // set unrunnability for WzSahag/R.Sahag fight
			formData[127][UnrunnableOffset] |= 0x02; // set unrunnability for IronGol fight
			
			Put(FormationsOffset, formData.SelectMany(formation => formation.ToBytes()).ToArray());
		}

		public void ImproveTurnOrderRandomization(MT19337 rng)
		{
			// Shuffle the initial bias so enemies are no longer always at the start initially.
			List<byte> turnOrder = new List<byte> { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x80, 0x81, 0x82, 0x83 };
			turnOrder.Shuffle(rng);
			Put(0x3215C, turnOrder.ToArray());

			// Rewrite turn order shuffle to Fisher-Yates.
			Put(0x3217A, Blob.FromHex("A90C8D8E68A900AE8E68205DAEA8AE8E68EAEAEAEAEAEA"));
		}

		public void EnableInventoryAutosort()
		{
			//see 0F_8670_SortInventory.asm
			Put(0x7EF58, Blob.FromHex("A90F2003FE20008DEAEAEA"));
			PutInBank(0x0F, 0x8D00, Blob.FromHex("8663A9009D0003A900856218A000A219BD2060F0058A990003C8E8E01CD0F1A216BD2060F0058A990003C8E8E019D0F1A200BD2060F0058A990003C8E8E011D0F1C8AD3560F00399000360"));
		}

		public void EnableCritNumberDisplay()
		{
			// Overwrite the normal critical hit handler by calling ours instead
			PutInBank(0x0C, 0xA94B, Blob.FromHex("20D1CFEAEA"));
			PutInBank(0x1F, 0xCFD1, CreateLongJumpTableEntry(0x0F, 0x92A0));
		}

		public void EnableMelmondGhetto(bool enemizerOn)
		{
			// Set town desert tile to random encounters.
			// If enabled, trap tile shuffle will change that second byte to 0x00 afterward.
			Data[0x00864] = 0x0A;
			Data[0x00865] = enemizerOn ? (byte)0x00 : (byte)0x80;

			// Give Melmond Desert backdrop
			Data[0x0334D] = (byte)Backdrop.Desert;

			if(!enemizerOn) // if enemizer formation shuffle is on, it will have assigned battles to Melmond already
				Put(0x2C218, Blob.FromHex("0F0F8F2CACAC7E7C"));
		}

		public void NoDanMode()
		{
			// Instead of looping through the 'check to see if characters are alive' thing, just set it to 4 and then remove the loop.
			// EA EA EA EA EA EA (sports)
			Put(0x6CB43, Blob.FromHex("A204A004EAEAEAEAEAEAEAEAEAEAEAEAEA"));

		}

		public void ShuffleWeaponPermissions(MT19337 rng)
		{
			const int WeaponPermissionsOffset = 0x3BF50;
			ShuffleGearPermissions(rng, WeaponPermissionsOffset);
		}

		public void ShuffleArmorPermissions(MT19337 rng)
		{
			const int ArmorPermissionsOffset = 0x3BFA0;
			ShuffleGearPermissions(rng, ArmorPermissionsOffset);
		}

		public void ShuffleGearPermissions(MT19337 rng, int offset)
		{
			const int PermissionsSize = 2;
			const int PermissionsCount = 40;

			// lut_ClassEquipBit: ;  FT   TH   BB   RM   WM   BM      KN   NJ   MA   RW   WW   BW
			// .WORD               $800,$400,$200,$100,$080,$040,   $020,$010,$008,$004,$002,$001
			var mask = 0x0820; // Fighter/Knight class bit lut. Each class is a shift of this.
			var order = Enumerable.Range(0, 6).ToList();
			order.Shuffle(rng);

			var oldPermissions = Get(offset, PermissionsSize * PermissionsCount).ToUShorts();
			var newPermissions = oldPermissions.Select(item =>
			{
				UInt16 shuffled = 0x0000;
				for (int i = 0; i < 6; ++i)
				{
					// Shift the mask into each class's slot, then AND with vanilla permission.
					// Shift left to vanilla fighter, shift right into new permission.
					shuffled |= (ushort)(((item & (mask >> i)) << i) >> order[i]);
				}
				return shuffled;
			});

			Put(offset, Blob.FromUShorts(newPermissions.ToArray()));
		}

		public void EnableCardiaTreasures(MT19337 rng, Map cardia)
		{
			// Assign items to the chests.
			// Incomplete.

			// Put the chests in Cardia
			var room = Map.CreateEmptyRoom((3, 4), 1);
			room[1, 1] = 0x75;
			cardia.Put((0x2A, 0x07), room);
			cardia[0x0B, 0x2B] = (byte)Tile.Doorway;

			room[1, 1] = 0x76;
			cardia.Put((0x26, 0x1C), room);
			cardia[0x20, 0x27] = (byte)Tile.Doorway;
		}

		public void CannotSaveOnOverworld()
		{
			// Hacks the game to disallow saving on the overworld with Tents, Cabins, or Houses
			Put(0x3B2F9, Blob.FromHex("1860"));
		}

		public void CannotSaveAtInns()
		{
			// Hacks the game so that Inns do not save the game
			Put(0x3A53D, Blob.FromHex("EAEAEA"));
		}
		public void EnableTwelveClasses()
		{
			// Expand characters shown in party creation screen; set to 0C for all promoted classes
			PutInBank(0x1E, 0x80F5, Blob.FromHex("06"));
			// Add the classes to authorized classes lut
			PutInBank(0x1E, 0x811B, Blob.FromHex("804020100804"));
			// Reduce count to $10 in PtyGen_DrawChars because we only loaded one row of sprites
			PutInBank(0x1E, 0x8373, Blob.FromHex("EA"));

			// New CHRLoad routine so we can load promoted classes' sprites in memory by only loading one row of sprites instead of two
			var newfCHRLoad = Blob.FromHex("A000B1108D0720C8D0F8E611E611CAD0F160");

			// Put new CHRLoad routine
			PutInBank(0x1F, 0xEAD5, Blob.FromHex("20EBFE"));
			PutInBank(0x1F, 0xFEEB, newfCHRLoad);

			// Starting stats for promoted classes, same as base classes
			Put(0x30A0, Get(0x3040, 0x0A));
			Put(0x30B0, Get(0x3050, 0x0A));
			Put(0x30C0, Get(0x3060, 0x0A));
			Put(0x30D0, Get(0x3070, 0x0A));
			Put(0x30E0, Get(0x3080, 0x0A));
			Put(0x30F0, Get(0x3090, 0x0A));

			// Starting magic for promoted classes. Instead of checking for the class ID of the characters, we compare with the unused class ID in the starting stats array.
			PutInBank(0x1F, 0xC7CA, Blob.FromHex("B940B0"));

			// New promotion routine to not bug out with already promoted classes and allow random promotion; works with Nones; see 0E_95AE_DoClassChange-2.asm
			PutInBank(0x0E, 0x95AE, Blob.FromHex("A20020C595A24020C595A28020C595A2C020C595E65660BC00613006B9809C9D006160"));
			// lut for standard promotion, can be modified or randomized
			PutInBank(0x0E, 0x9C80, Blob.FromHex("060708090A0B060708090A0B"));
		}

		public void EnableRandomPromotions(Flags flags, MT19337 rng)
		{
			// Need EnableTwelveClasses()
			// Promotions list
			List<sbyte> promotions = new List<sbyte> { 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B };

			// Include base classes
			if (flags.IncludeBaseClasses ?? false)
			{
				promotions.AddRange(new List<sbyte> { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 });
				promotions.Shuffle(rng);
			}
			else
			{
				// If not shuffle list first then add promoted classes so that already promoted classes don't bug
				promotions.Shuffle(rng);
				promotions.AddRange(new List<sbyte> { 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B });
			}

			// Insert randomized promotions
			PutInBank(0x0E, 0x9C80, Blob.FromSBytes(promotions.ToArray()));
		}

		public void EnablePoolParty(Flags flags, MT19337 rng)
		{
			// Need EnableTwelveClasses()
			// New DoPartyGen_OnCharacter and update references; see 1E_85B0_DoPartyGen_OnCharacter.asm
			PutInBank(0x1E, 0x85B0, Blob.FromHex("A667BD01030D41038D410320A480200F82A667AC4003A524F016BD0003C9FFF00CB926869D01034D41038D41034C2C81A525F0118AC900F00AA9009D0103A9FF9D00033860A520290FC561F0C18561C900F0BBC898C9099002A0008C4003B926862C4103F0EDB942039D0003A901853720B0824CBE858040201008040201"));
			PutInBank(0x1E, 0x8032, Blob.FromHex("B085"));
			PutInBank(0x1E, 0x803B, Blob.FromHex("B085"));
			PutInBank(0x1E, 0x8044, Blob.FromHex("B085"));
			PutInBank(0x1E, 0x804D, Blob.FromHex("B085"));

			// Routine to load the random pool in memory
			PutInBank(0x1E, 0x80C1, Blob.FromHex("A23FBDAA849D0003CA10F7A209BD30869D4003CA10F760"));

			// Zero out free space
			sbyte[] zerofill = new sbyte[0x54];
			PutInBank(0x1E, 0x80D8, Blob.FromSBytes(zerofill));

			// Change reference in NewGamePartyGeneration
			PutInBank(0x1E, 0x801E, Blob.FromHex("20C180EAEAEAEAEAEAEAEA"));

			// Standard party pool lut, byte1 = selection; byte2 = availability mask; byte3-10: characters pool
			PutInBank(0x1E, 0x8630, Blob.FromHex("00FC0001020304050607"));

			int size = 6;
			Blob sizebyte = Blob.FromHex("");

			switch (flags.PoolSize)
			{
				case PoolSize.Size4: size = 4; sizebyte = Blob.FromHex("F0"); break;
				case PoolSize.Size5: size = 5; sizebyte = Blob.FromHex("F8"); break;
				case PoolSize.Size6: size = 6; sizebyte = Blob.FromHex("FC"); break;
				case PoolSize.Size7: size = 7; sizebyte = Blob.FromHex("FE"); break;
				case PoolSize.Size8: size = 8; sizebyte = Blob.FromHex("FF"); break;
			}

			int class_span = 5;
			if (flags.IncludePromClasses ?? false) class_span = 11;
			Blob pool = Blob.FromHex("");
			for (int i = 0; i < size; i++)
				pool += Blob.FromSBytes(new List<sbyte> { (sbyte)Rng.Between(rng, 0, class_span) }.ToArray());

			// Pool size : 4 0xF0; 5 0xF8; 6 0xFC; 7 0xFE; 8 0xFF)
			PutInBank(0x1E, 0x8630, Blob.FromHex("00") + sizebyte + pool);

			// Starting party composition
			PutInBank(0x1E, 0x84AA, pool.SubBlob(0, 1));
			PutInBank(0x1E, 0x84BA, Blob.FromHex("FF"));
			PutInBank(0x1E, 0x84CA, Blob.FromHex("FF"));
			PutInBank(0x1E, 0x84DA, Blob.FromHex("FF"));
		}

	}
}
