using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The input field, the disk question, the nuclear attack and the unit information window against their original code
	/// </summary>
	public class BatchEighteenDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void RenderTextInputFieldMatchesOriginalCode()
		{
			Check(3, 200, (chance, pair) =>
			{
				string text = new string(Enumerable.Range(0, chance.Next(0, 14)).Select(_ => "AbcmW .il"[chance.Next(9)]).ToArray());
				ushort breadth = (ushort)chance.Next(1, 17);
				ushort cursor = (ushort)chance.Next(0, 17);
				short horizLoc = (short)chance.Next(0, 200), vertLoc = (short)chance.Next(0, 180);

				return new Scenario($"RenderTextInputField({horizLoc}, {vertLoc}, {breadth}), text \"{text}\", cursor {cursor}",
					game =>
					{
						game.Libc.CopyText(0xba06, text);
						game.Processor.EmitGlobalWord(0x68cc, cursor);
					},
					game => new NewCityScreenReference(game).RenderTextInputField(horizLoc, vertLoc, breadth),
					game => game.CityNaming.RenderTextInputField(horizLoc, vertLoc, breadth),
					Register.Kept,
					game => cursor < text.Length && cursor < breadth ? "cursor on a character" : text.Length > breadth ? "text cut" : "rest empty");
			}, output, ["cursor on a character", "text cut", "rest empty"]);
		}

		[Fact]
		public void QueryForDiskMatchesOriginalCode()
		{
			ushort[]? paths = null;

			Check(3, 40, (chance, pair) =>
			{
				// The file names of the disks as loaded; each scenario puts them back first
				paths ??= Enumerable.Range(0, 4).Select(idx => pair.Original.Engine.Processor.PullGlobalWord((ushort)(0x1ace + (idx << 1)))).ToArray();
				ushort diskIdx = (ushort)chance.Next(4);
				ushort marker = new ushort[] { 0, 1, 0xffff }[chance.Next(3)];
				bool missing = marker == 0 && diskIdx != 0 && chance.Next(2) == 0;

				return new Scenario($"QueryForDisk({diskIdx}, {marker}), file missing {missing}",
					game =>
					{
						// A disk whose file is not there (only with marker 0: then disk 0 is taken without asking)
						game.Libc.CopyText(0xbb80, "NOFILE.XYZ");

						for (int idx = 0; idx < 4; idx++)
							game.Processor.EmitGlobalWord((ushort)(0x1ace + (idx << 1)), paths[idx]);

						if (missing)
							game.Processor.EmitGlobalWord((ushort)(0x1ace + (diskIdx << 1)), 0xbb80);
					},
					game => new GameStartupReference(game).QueryForDisk(diskIdx, marker),
					game => game.Startup.QueryForDisk(diskIdx, marker),
					Register.Acc | Register.Kept,
					game => missing ? "disk 0 instead" : marker == 0xffff ? "asked once" : marker == 0 ? "found at once" : "found");
			}, output, ["disk 0 instead", "asked once", "found at once", "found"], [0x0d]);
		}

		[Fact]
		public void ExplodeNuclearWeaponMatchesOriginalCode()
		{
			Check(3, 40, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				UnitMemory troops = new UnitMemory(engine.Processor);
				short participantIdx = (short)chance.Next(8);
				(int horizLoc, int vertLoc) = RandomTile(engine, chance);
				short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx).Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff).ToArray();
				short sdi = -1;

				// Mostly at a unit or a city (sometimes with SDI)
				if (chance.Next(2) == 0 && RandomUnit(engine, chance) is (short ownerIdx, short troopIdx))
				{
					(horizLoc, vertLoc) = (troops.Pull(ownerIdx, troopIdx, UnitMemory.Horiz), troops.Pull(ownerIdx, troopIdx, UnitMemory.Vert));
				}
				else if (chance.Next(2) == 0)
				{
					short townIdx = towns[chance.Next(towns.Length)];
					(horizLoc, vertLoc) = (engine.World.Towns[townIdx].Place.X, engine.World.Towns[townIdx].Place.Y);
					sdi = chance.Next(2) == 0 ? townIdx : (short)-1;
				}

				int destroyed = Calls(pair.Rewritten.Engine, pair, "VisitEachUnitInStack");

				return new Scenario($"ExplodeNuclearWeapon({participantIdx}, {horizLoc}, {vertLoc}), SDI in city {sdi}",
					game =>
					{
						if (sdi >= 0)
							game.World.Towns[sdi].StructureMarkers1 |= 1;
					},
					game => new UnitCombatReference(game).ExplodeNuclearWeapon(participantIdx, (short)horizLoc, (short)vertLoc),
					game => game.Combat.ExplodeNuclearWeapon(participantIdx, (short)horizLoc, (short)vertLoc),
					Register.Kept,
					game => (sdi >= 0 ? "SDI, " : "") + (Calls(game, pair, "VisitEachUnitInStack") > destroyed ? "units destroyed" : "no units"));
			}, output, ["SDI, no units", "units destroyed", "no units"], [0x0d]);
		}

		[Fact]
		public void DisplayUnitInfoMatchesOriginalCode()
		{
			Check(3, 60, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				UnitMemory troops = new UnitMemory(engine.Processor);

				if (RandomUnit(engine, chance) is not (short participantIdx, short troopIdx))
					return null;

				bool goTo = chance.Next(3) == 0;

				return new Scenario($"DisplayUnitInfo({participantIdx}, {troopIdx}), go to {goTo}",
					game =>
					{
						if (goTo)
						{
							UnitMemory memory = new UnitMemory(game.Processor);
							memory.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 12);
							memory.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, 34);
						}
					},
					game => new ArmySelectionReference(game).DisplayUnitInfo(participantIdx, troopIdx),
					game => game.ArmyPicker.DisplayUnitInfo(participantIdx, troopIdx),
					Register.Kept,
					game => (new UnitMemory(game.Processor).Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) != 0xff ? "go to, " : "") +
						(new UnitMemory(game.Processor).Following(participantIdx, troopIdx) != -1 ? "stack" : "alone"));
			}, output, ["go to, stack", "go to, alone", "stack", "alone"], [0x0d]);
		}
	}
}
