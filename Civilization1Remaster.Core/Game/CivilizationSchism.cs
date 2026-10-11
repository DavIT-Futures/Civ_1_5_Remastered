using System;
using System.Collections.Generic;
using Civilization1Remaster.Game.Rules;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Civil war / schism resolution when a civilization's capital is captured.
	/// Translated from original CIV.EXE overlay 15 to pure C#.
	/// </summary>
	public class CivilizationSchism
	{
		private readonly GameEngine owner;

		public CivilizationSchism(GameEngine owner)
		{
			this.owner = owner;
		}

		/// <summary>
		/// Divides an empire into two factions (civil war / rebellion) upon loss of its capital.
		/// </summary>
		/// <param name="participantIdx">Empire experiencing the rebellion.</param>
		/// <returns>1 if schism occurred successfully, 0 otherwise.</returns>
		public ushort DivideCivilization(short participantIdx)
		{
			this.owner.Processor.Journal.OpenScope($"DivideCivilization({participantIdx})");

			// 1. Locate available slot for the rebel faction
			short rebelIdx = -1;
			for (short slot = 1; slot < 8; slot++)
			{
				if (this.owner.World.Participants[slot].TroopTally == 0 &&
				    this.owner.World.Participants[slot].TownTally == 0)
				{
					rebelIdx = slot;
					break;
				}
			}

			if (rebelIdx == -1)
			{
				this.owner.Processor.Journal.CloseScope("DivideCivilization");
				return 0;
			}

			// 2. Identify capital and tally population per continent
			int capitalContinent = -1;
			int capitalX = 0;
			int capitalY = 0;
			int capitalTownIdx = -1;
			int[] continentPop = new int[16];

			for (int t = 0; t < 128; t++)
			{
				var town = this.owner.World.Towns[t];
				if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx)
					continue;

				ushort contId = this.owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y);
				if (contId < 16)
				{
					continentPop[contId] += town.TrueExtent;
				}

				if ((town.StructureMarkers0 & 1) != 0)
				{
					if (capitalContinent == -1 || town.Place.X == this.owner.World.Participants[participantIdx].HorizBegin)
					{
						capitalContinent = contId;
						capitalX = town.Place.X;
						capitalY = town.Place.Y;
						capitalTownIdx = t;
					}
				}
			}

			if (capitalContinent == -1)
			{
				this.owner.Processor.Journal.CloseScope("DivideCivilization");
				return 0;
			}

			// 3. Initialize the rebel civilization
			bool isNewCiv = rebelIdx > this.owner.World.BotRivalTally + 1;
			this.owner.World.LiveRealms &= (short)~(1 << rebelIdx);
			ushort constructSuccess = this.owner.NewGameWizard.ConstructCivilization(rebelIdx);

			if (constructSuccess == 0)
			{
				this.owner.Processor.Journal.CloseScope("DivideCivilization");
				return 0;
			}

			this.owner.World.LiveRealms |= (short)(1 << rebelIdx);

			if (!isNewCiv)
			{
				int chiefVisuals = this.owner.World.Participants[rebelIdx].ChiefVisuals;
				if (rebelIdx != 7 && (chiefVisuals & 1) != 0 || chiefVisuals > 0xf)
				{
					this.owner.World.Participants[rebelIdx].ChiefVisuals ^= 8;
				}
				else
				{
					this.owner.World.Participants[rebelIdx].ChiefVisuals = (short)(rebelIdx + (this.owner.Libc.Dice.DrawInt(2) != 0 ? 0 : 8));
					if (this.owner.World.Participants[rebelIdx].ChiefVisuals >= 8)
					{
						this.owner.World.ParticipantIdentMarkers |= (short)(1 << rebelIdx);
					}
				}
			}

			// 4. Setup rebel civilization identities & names
			int rebelVisuals = this.owner.World.Participants[rebelIdx].ChiefVisuals;
			ushort seg = this.owner.Processor.SegData.Value16;
			this.owner.Libc.CopyText(this.owner.Processor.PullUInt16(seg, (ushort)((rebelIdx << 1) + 0x19a2)), (ushort)(rebelVisuals * 0x3a + 0x14e2));
			this.owner.Libc.CopyText(this.owner.Processor.PullUInt16(seg, (ushort)((rebelIdx << 1) + 0x1982)), (ushort)(rebelVisuals * 0x3a + 0x1502));
			this.owner.Libc.CopyText(0xba06, (ushort)(rebelVisuals * 0x3a + 0x14f2));
			if (this.owner.Processor.PullGlobalByte(0xba06) == 0)
			{
				this.owner.Libc.CopyText(0xba06, this.owner.Processor.PullUInt16(seg, (ushort)((rebelIdx << 1) + 0x1982)));
				this.owner.Libc.AppendText(0xba06, "s");
			}
			this.owner.Libc.CopyText(this.owner.Processor.PullUInt16(seg, (ushort)((rebelIdx << 1) + 0x1992)), 0xba06);

			// Announce rebellion dialog
			this.owner.Libc.CopyText(this.owner.Processor.PullGlobalWord(0x30b8), this.owner.Processor.PullUInt16(seg, (ushort)((participantIdx << 1) + 0x1982)));
			this.owner.Libc.CopyText(this.owner.Processor.PullGlobalWord(0x30ba), this.owner.Processor.PullUInt16(seg, (ushort)((rebelIdx << 1) + 0x1982)));
			this.owner.BlockText.DisplayTextDialog((ushort)((participantIdx == this.owner.World.PersonParticipantIdx) ? 0x4a18 : 0x4a20));
			this.owner.Processor.EmitGlobalWord(0x2f9e, 0x5);
			this.owner.TurnHooks.DisplayDialog(0xba06, 80, 80);

			// 5. Divide treasury, might, knowledge, and relations
			var (parentGold, rebelGold, parentMight, rebelMight) = SchismRules.ComputeAssetSplit(
				this.owner.World.Participants[participantIdx].Gold,
				this.owner.World.Participants[participantIdx].MartialMight);

			this.owner.World.Participants[participantIdx].Gold = (short)parentGold;
			this.owner.World.Participants[rebelIdx].Gold = (short)rebelGold;
			this.owner.World.Participants[participantIdx].MartialMight = (short)parentMight;
			this.owner.World.Participants[rebelIdx].MartialMight = (short)rebelMight;

			this.owner.World.Participants[rebelIdx].InquiryHeadway = this.owner.World.Participants[participantIdx].InquiryHeadway;
			this.owner.World.Participants[rebelIdx].FoundScienceTally = this.owner.World.Participants[participantIdx].FoundScienceTally;
			this.owner.World.Participants[rebelIdx].RegimeKind = this.owner.World.Participants[participantIdx].RegimeKind;

			for (int s = 0; s < 5; s++)
			{
				this.owner.World.Participants[rebelIdx].FoundScienceMarkers[s] = this.owner.World.Participants[participantIdx].FoundScienceMarkers[s];
			}

			this.owner.World.Participants[rebelIdx].TouchParticipantTimeleft = (short)(this.owner.World.RoundTally - 8);
			this.owner.World.Participants[participantIdx].Relations[rebelIdx] |= 1;
			this.owner.World.Participants[participantIdx].Relations[rebelIdx] |= 9;

			// 6. City defection logic
			int capitalContinentPop = continentPop[capitalContinent];
			int otherContinentsPop = 0;
			int[] continentDefectStatus = new int[16];

			for (int c = 0; c < 16; c++)
			{
				if (c != capitalContinent && continentPop[c] > 0)
				{
					if (otherContinentsPop <= capitalContinentPop)
					{
						otherContinentsPop += continentPop[c];
						continentDefectStatus[c] = 2; // Defect to rebels
					}
					else
					{
						capitalContinentPop += continentPop[c];
						continentDefectStatus[c] = 1; // Stay with parent
					}
				}
			}

			if (otherContinentsPop * 2 > capitalContinentPop && otherContinentsPop <= capitalContinentPop)
			{
				// Defect by whole continents
				for (short t = 0; t < 128; t++)
				{
					var town = this.owner.World.Towns[t];
					if (town.ConditionMarker != 0xff && town.ParticipantIdx == participantIdx)
					{
						ushort contId = this.owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y);
						if (contId < 16 && continentDefectStatus[contId] == 2)
						{
							AssignCityOwner(t, rebelIdx);
						}
					}
				}
			}
			else
			{
				// Defect by distance from capital
				int currentRebelPop = 0;
				int parentGrandExtent = this.owner.World.Participants[participantIdx].GrandTownExtent;
				int difficultyLevel = this.owner.Processor.PullGlobalSignedWord(0xdc6a);
				int targetDenominator = difficultyLevel + 3;

				while ((currentRebelPop * 9) / targetDenominator < parentGrandExtent)
				{
					int farthestDist = 1;
					short farthestTown = -1;

					for (short t = 0; t < 128; t++)
					{
						var town = this.owner.World.Towns[t];
						if (town.ConditionMarker != 0xff && town.ParticipantIdx == participantIdx)
						{
							int dist = this.owner.Helpers.ObtainDistance(town.Place.X, capitalX, town.Place.Y, capitalY);
							if (dist > farthestDist)
							{
								farthestDist = dist;
								farthestTown = t;
							}
						}
					}

					if (farthestTown < 0)
						break;

					currentRebelPop += this.owner.World.Towns[farthestTown].TrueExtent;
					AssignCityOwner(farthestTown, rebelIdx);
				}
			}

			this.owner.World.Participants[participantIdx].GrandTownExtent +=
				this.owner.World.Participants[rebelIdx].GrandTownExtent;

			// 7. Military units in defected cities switch allegiance
			for (short u = 0; u < 128; u++)
			{
				var troop = this.owner.World.Participants[participantIdx].Troops[u];
				if (troop.KindIdx == 0xff || troop.KindIdx == -1)
					continue;

				sbyte unitX = (sbyte)troop.Place.X;
				sbyte unitY = (sbyte)troop.Place.Y;
				byte unitType = (byte)troop.KindIdx;

				// Check if unit is standing in a defected city
				for (short t = 0; t < 128; t++)
				{
					var town = this.owner.World.Towns[t];
					if (town.ConditionMarker != 0xff && town.ParticipantIdx == rebelIdx &&
					    town.Place.X == unitX && town.Place.Y == unitY)
					{
						this.owner.UnitOps.EliminateUnit(participantIdx, u);
						this.owner.MapOps.TagTileOwned(unitX, unitY);
						this.owner.UnitOps.ConstructUnit(rebelIdx, unitType, unitX, unitY);
						break;
					}
				}
			}

			// 8. Designate new capitals
			ushort rebelCapital = ObtainDistanceToCapital(rebelIdx);
			if (rebelCapital != 0xffff)
			{
				this.owner.World.Towns[rebelCapital].StructureMarkers0 |= 1;
				this.owner.World.Participants[rebelIdx].HorizBegin = (short)this.owner.World.Towns[rebelCapital].Place.X;
			}

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.MapOps.FocusMapView(this.owner.World.PersonParticipantIdx,
					this.owner.Processor.PullGlobalSignedWord(0xd4cc),
					this.owner.Processor.PullGlobalSignedWord(0xd75e));
			}
			else if (capitalTownIdx != -1)
			{
				this.owner.World.Towns[capitalTownIdx].ParticipantIdx = -1;
				ushort parentCapital = ObtainDistanceToCapital(participantIdx);
				if (parentCapital != 0xffff)
				{
					this.owner.World.Towns[parentCapital].StructureMarkers0 |= 1;
					this.owner.World.Participants[participantIdx].HorizBegin = (short)this.owner.World.Towns[parentCapital].Place.X;
				}
				this.owner.World.Towns[capitalTownIdx].ParticipantIdx = participantIdx;
			}

			this.owner.Processor.Journal.CloseScope("DivideCivilization");
			return 1;
		}

		/// <summary>
		/// Assigns city ownership to a participant and updates population metrics.
		/// </summary>
		public void AssignCityOwner(short townIdx, short participantIdx)
		{
			this.owner.Processor.Journal.OpenScope($"AssignCityOwner({townIdx}, {participantIdx})");

			this.owner.World.Towns[townIdx].ParticipantIdx = participantIdx;

			this.owner.MapOps.MapAssignCityOwner(participantIdx, 
				this.owner.World.Towns[townIdx].Place.X, this.owner.World.Towns[townIdx].Place.Y);

			this.owner.World.Participants[participantIdx].GrandTownExtent += this.owner.World.Towns[townIdx].TrueExtent;

			this.owner.Processor.Journal.CloseScope("AssignCityOwner");
		}

		/// <summary>
		/// Finds the city that serves as the geometric center of a civilization (minimizes total distance to all other cities).
		/// </summary>
		public ushort ObtainDistanceToCapital(short participantIdx)
		{
			this.owner.Processor.Journal.OpenScope($"ObtainDistanceToCapital({participantIdx})");

			int bestCity = SchismRules.FindGeometricCenterCity(
				this.owner.World.Towns,
				participantIdx,
				(x1, x2, y1, y2) => this.owner.Helpers.ObtainDistance(x1, x2, y1, y2));

			this.owner.Processor.Journal.CloseScope("ObtainDistanceToCapital");
			return (ushort)(bestCity >= 0 ? bestCity : 0xffff);
		}
	}
}
