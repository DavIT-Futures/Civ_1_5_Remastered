using System;
using System.Collections.Generic;
using Civilization1Remaster.Video;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;
using Civilization1Remaster.Game.State;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	public class BatchSixtyNineDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void OpeningSequenceMatchesOriginalCode()
		{
			int step = 0;

			Check(1, 2, (chance, pair) =>
			{
				bool earthMap = step++ == 0;
				ushort landMass = (ushort)chance.Next(1, 3);
				ushort temperature = (ushort)chance.Next(3);
				ushort climate = (ushort)chance.Next(3);
				ushort age = 0;
				ushort rngSeed = (ushort)chance.Next(1, 0x7fff);

				return new Scenario(
					$"OpeningSequence(earth={earthMap}, land={landMass}, temp={temperature}, clim={climate}, age={age})",
					game =>
					{
						game.Processor.EmitGlobalWord(0xd76a, earthMap ? (ushort)1 : (ushort)0);
						game.Processor.EmitGlobalWord(0x7ef6, landMass);
						game.Processor.EmitGlobalWord(0x7ef8, temperature);
						game.Processor.EmitGlobalWord(0x7efa, climate);
						game.Processor.EmitGlobalWord(0x7efc, age);
						game.Processor.EmitGlobalWord(0x5386, rngSeed);
						game.Libc.CopyText(0x3b20, "earth.mp");

						// Stage 5 with empty prompt at 0x3b28 and no key pressed makes DisplayStoryIntro return 0 fast without opening story.txt
						for (int idx = 0; idx < 0x80; idx++)
							game.Processor.EmitGlobalWord((ushort)(0xdcfe + (idx << 1)), 0);
						game.Processor.EmitGlobalByte(0x3b28, 0);
						game.Processor.EmitGlobalWord(0x67fe, 0);
						game.Processor.EmitGlobalWord(0x3b62, 5);
						game.Processor.EmitGlobalWord(0x6802, 0);
						game.Processor.EmitGlobalWord(0x6806, 0);
						game.Processor.EmitGlobalWord(0x6800, 0);
					},
					game =>
					{
						new GameInitAndIntroReference(game).OpeningSequence();
						game.Processor.EmitGlobalWord(0x6800, 0);
					},
					game =>
					{
						game.WorldSetup.OpeningSequence();
						game.Processor.EmitGlobalWord(0x6800, 0);
					},
					Register.Kept,
					game => earthMap ? "earth" : "generated");
			}, output, ["earth", "generated"]);
		}

		[Fact]
		public void SettleBattleMatchesOriginalCode()
		{
			Check(2, 45, (chance, pair) =>
			{
				int mode = chance.Next(8);
				ushort marker = mode == 0 ? (ushort)0 : (ushort)1;
				short personIdx = 1;
				short participant1 = mode switch
				{
					1 => personIdx,
					3 => personIdx,
					6 => 0, // barbarian attacker
					_ => (short)chance.Next(1, 4),
				};
				short participant2 = mode switch
				{
					1 => 2,
					3 => 0, // barbarian leader defender
					_ => (short)(participant1 == 1 ? 2 : 1),
				};

				short troop1 = 0;
				short troop2 = 0;
				byte kind1 = mode switch
				{
					2 => 0x19, // nuclear
					3 => 12,   // armor for guaranteed win vs leader
					4 => 12,   // armor for stack wipe
					5 => 12,   // armor vs city defender
					7 => 1,    // militia vs strong defender
					_ => (byte)chance.Next(1, 15),
				};
				byte kind2 = mode switch
				{
					3 => 0x1a, // barbarian leader
					7 => 11,   // mech inf
					_ => (byte)chance.Next(1, 10),
				};

				short x1 = 20;
				short y1 = 20;
				short x2 = 21;
				short y2 = 20;
				if (chance.Next(6) == 0)
				{
					x1 = 0x4f;
					x2 = 0;
				}
				else if (chance.Next(6) == 0)
				{
					x1 = 0;
					x2 = 0x4f;
				}

				bool inCity = mode == 5 || (mode == 6 && chance.Next(2) == 0);
				bool inFortress = !inCity && chance.Next(4) == 0;
				bool stackedDefender = mode == 4 && !inCity && !inFortress;
				bool peaceTreaty = mode == 1 || (mode != 3 && mode != 4 && mode != 5 && chance.Next(4) == 0);
				bool sneakFlag = chance.Next(3) == 0;
				bool animate = chance.Next(2) == 0;
				byte steps1 = (byte)chance.Next(1, 4);
				byte status1 = (byte)(chance.Next(2) == 0 ? (int)UnitStatusEnum.Veteran : 0);
				byte status2 = (byte)(chance.Next(2) == 0 ? (int)UnitStatusEnum.Fortified : 0);
				ushort hardness = (ushort)chance.Next(4);
				ushort rngSeed = (ushort)chance.Next(1, 0x7fff);

				return new Scenario(
					$"SettleBattle(mode={mode}, p1={participant1}, p2={participant2}, k1={kind1}, k2={kind2}, marker={marker})",
					game =>
					{
						game.Processor.EmitGlobalWord(0x5386, rngSeed);
						game.World.PersonParticipantIdx = personIdx;
						game.World.HardnessTier = (short)hardness;
						game.Processor.EmitGlobalWord(0xb278, animate ? (ushort)0 : (ushort)1);
						game.Processor.EmitGlobalWord(0xd806, 1);

						for (int p = 0; p < 8; p++)
						{
							for (int u = 0; u < 128; u++)
							{
								ushort addr = (ushort)(0x81d4 + p * 0x600 + u * 0xc);
								game.Processor.EmitUInt8(game.Processor.SegData.Value16, (ushort)(addr + 3), 0xff);
							}
						}

						for (int c = 0; c < 128; c++)
						{
							game.World.Towns[c].ConditionMarker = 0xff;
						}

						game.MapOps.DiscardTileImprovements(0xff, x2, y2);
						if (inCity)
						{
							game.MapOps.AssignTileImprovements(0x1, x2, y2);
							game.World.Towns[0].ConditionMarker = 0;
							game.World.Towns[0].Place = new System.Drawing.Point(x2, y2);
							game.World.Towns[0].ParticipantIdx = participant2;
							game.World.Towns[0].TrueExtent = 4;
							game.World.Towns[0].StructureMarkers0 = 0x81;
							game.World.Towns[0].StructureMarkers1 = 0;
							game.World.Participants[participant2].TownTally = 2;
						}
						else if (inFortress)
						{
							game.MapOps.AssignTileImprovements(0x20, x2, y2);
						}

						SetupUnit(game, participant1, troop1, status1, (byte)x1, (byte)y1, kind1, steps1, 0xff);
						SetupUnit(game, participant2, troop2, status2, (byte)x2, (byte)y2, kind2, 3, stackedDefender ? (byte)1 : (byte)0xff);
						if (stackedDefender)
						{
							SetupUnit(game, participant2, 1, 0, (byte)x2, (byte)y2, 2, 3, (byte)troop2);
						}

						if (inCity)
						{
							// Put an AI unit on an adjacent tile (x2, y2 - 1) to exercise the adjacent stack loop at L0a4c
							SetupUnit(game, 3, 0, 0, (byte)x2, (byte)(y2 - 1), 2, 3, 0xff);
							game.MapOps.Chart.AssignOwnerBits(x2, y2 - 1, 3 | MapLayers.UnitsPresent);
						}

						ushort rel = 0;
						if (peaceTreaty) rel |= 2;
						if (sneakFlag) rel |= 0x200;
						game.World.Participants[participant1].Relations[participant2] = rel;
						game.World.Participants[participant2].Relations[participant1] = rel;
						game.World.Participants[personIdx].RegimeKind = 2;
					},
					game => new UnitCombatReference(game).SettleBattle(participant1, troop1, participant2, troop2, marker),
					game => game.Combat.SettleBattle(participant1, troop1, participant2, troop2, marker),
					Register.Kept | Register.Acc,
					game =>
					{
						ushort ax = game.Processor.RegAcc.Value16;
						if (marker == 0) return "estimate";
						if (ax == 0xffff) return "cancelled";
						if (kind1 == 0x19) return "nuclear";
						if (ax == 1 && participant2 == 0 && kind2 == 0x1a) return "ransom";
						if (ax == 1 && stackedDefender) return "stackDestroyed";
						if (ax == 1 && inCity) return "cityDefender";
						return ax == 1 ? "attackerWins" : "defenderWins";
					});
			}, output, ["estimate", "cancelled", "nuclear", "ransom", "stackDestroyed", "cityDefender", "attackerWins", "defenderWins"],
			[0x0d, 0x5000, 0x0d, 0x0d, 0x0d]);
		}

		private static void SetupUnit(
			GameEngine game,
			short participantIdx,
			short troopIdx,
			byte status,
			byte x,
			byte y,
			byte kind,
			byte steps,
			byte nextInStack)
		{
			ushort addr = (ushort)(0x81d4 + participantIdx * 0x600 + troopIdx * 0xc);
			game.Processor.EmitUInt8(game.Processor.SegData.Value16, (ushort)(addr + 0), status);
			game.Processor.EmitUInt8(game.Processor.SegData.Value16, (ushort)(addr + 1), x);
			game.Processor.EmitUInt8(game.Processor.SegData.Value16, (ushort)(addr + 2), y);
			game.Processor.EmitUInt8(game.Processor.SegData.Value16, (ushort)(addr + 3), kind);
			game.Processor.EmitUInt8(game.Processor.SegData.Value16, (ushort)(addr + 4), steps);
			game.Processor.EmitUInt8(game.Processor.SegData.Value16, (ushort)(addr + 5), 0);
			game.Processor.EmitUInt8(game.Processor.SegData.Value16, (ushort)(addr + 6), 0xff);
			game.Processor.EmitUInt8(game.Processor.SegData.Value16, (ushort)(addr + 7), 0xff);
			game.Processor.EmitUInt8(game.Processor.SegData.Value16, (ushort)(addr + 8), 0);
			game.Processor.EmitUInt8(game.Processor.SegData.Value16, (ushort)(addr + 9), 0xff);
			game.Processor.EmitUInt8(game.Processor.SegData.Value16, (ushort)(addr + 10), nextInStack);
			game.Processor.EmitUInt8(game.Processor.SegData.Value16, (ushort)(addr + 11), 0xff);
			game.World.Vitals.Wipe(participantIdx, troopIdx);
		}
	}
}
