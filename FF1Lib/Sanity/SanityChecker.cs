﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FF1Lib.FF1Rom;

namespace FF1Lib.Sanity
{
	public class SanityChecker
	{
		FF1Rom rom;
		List<Map> maps;
		OverworldMap overworldMap;


		MapTileSets mapTileSets;
		SCTileSet[] tileSets = new SCTileSet[8];

		EnterTeleData enter;
		ExitTeleData exit;
		NormTeleData tele;
		NPCdata npcdata;

		Dictionary<MapId, SCMap> scmaps;

		public List<SCDungeon> Dungeons { get; private set; } = new List<SCDungeon>();

		public SanityChecker(List<Map> _maps, OverworldMap _overworldMap, NPCdata _npcdata, FF1Rom _rom)
		{
			maps = _maps;
			overworldMap = _overworldMap;
			rom = _rom;
			npcdata = _npcdata;

			mapTileSets = new MapTileSets(rom);
			enter = new EnterTeleData(rom);
			tele = new NormTeleData(rom);
			exit = new ExitTeleData(rom);

			for (int i = 0; i < 8; i++) tileSets[i] = new SCTileSet(rom, (byte)i);

			Stopwatch w = Stopwatch.StartNew();

			ConcurrentBag<SCMap> tmpscmaps = new ConcurrentBag<SCMap>();
			Parallel.ForEach(Enum.GetValues<MapId>(), mapid => ProcessMap(mapid, tmpscmaps));
			scmaps = tmpscmaps.ToDictionary(m => m.MapId);

			ComposeDungeons();

			w.Stop();
		}

		private void ProcessMap(MapId mapid, ConcurrentBag<SCMap> tmpscmaps)
		{
			var e1 = maps[(int)mapid];
			var ts = tileSets[mapTileSets[mapid]];

			SCMapCheckFlags cflags = SCMapCheckFlags.None;
			if (mapid <= MapId.CastleOfOrdeals1F) cflags |= SCMapCheckFlags.NoWarp;
			if (mapid == MapId.SkyPalace2F) cflags |= SCMapCheckFlags.NoUseTiles;

			SCMap scmap = new SCMap(mapid, e1, cflags, rom, npcdata, ts, enter, exit, tele);
			tmpscmaps.Add(scmap);
		}

		private void ComposeDungeons()
		{
			int teleportindex = -1;
			HashSet<SCTeleport> usedEnterTeles = new HashSet<SCTeleport>(new SCTEleportTargetEqualityComparer());
			foreach (var et in enter.Select(e => new SCTeleport(e, SCPointOfInterestType.OwEntrance)))
			{
				teleportindex++;
				if (usedEnterTeles.Contains(et)) continue;
				Dungeons.Add(new SCDungeon(et, (OverworldTeleportIndex)teleportindex, scmaps, usedEnterTeles));
			}
		}
	}
}
