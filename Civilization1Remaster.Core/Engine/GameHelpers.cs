using System;

namespace Civilization1Remaster.Core.Engine
{
	public class GameHelpers : GameComponent, IGameHelpers
	{
		private readonly UnitMemory troops;

		public GameHelpers(GameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
		}

		public void SeedRandomizer()
		{
			ushort seed = (ushort)(this.Owner.Libc.CurrentTime(0) & 0x7fff);
			this.Owner.World.ChanceRoot = seed;
			this.Owner.Libc.SeedRandom(seed);
		}

		public int ClampValue(int amount, int floorAmount, int peakAmount)
		{
			if (floorAmount > peakAmount)
				throw new Exception("Minimum value is greater than maximum value");

			return Math.Min(Math.Max(amount, floorAmount), peakAmount);
		}

		public ushort FindCityAtPosition(int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"LocateCityAtPosition({horizLoc}, {vertLoc})");

			ushort found = 0xffff;

			for (int townIdx = 0; townIdx < this.Owner.World.Towns.Length; townIdx++)
			{
				City town = this.Owner.World.Towns[townIdx];

				if (town.ConditionMarker != 0xff && town.Place.X == horizLoc && town.Place.Y == vertLoc)
				{
					found = (ushort)townIdx;
					break;
				}
			}

			return found;
		}

		public ushort FindNearestCity(int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"LocateNearestCity({horizLoc}, {vertLoc})");

			int nearestDistance = 999;
			ushort nearest = 0xffff;

			for (int townIdx = 0; townIdx < this.Owner.World.Towns.Length; townIdx++)
			{
				City town = this.Owner.World.Towns[townIdx];

				if (town.ConditionMarker == 0xff)
					continue;

				int distance = ObtainDistance(horizLoc, town.Place.X, vertLoc, town.Place.Y);

				if (distance < nearestDistance)
				{
					nearestDistance = distance;
					nearest = (ushort)townIdx;
				}
			}

			Search.FoundDistance = (ushort)nearestDistance;

			return nearest;
		}

		public ushort FindNearestUnit(short participantIdx, short troopIdx, int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"LocateNearestUnit({participantIdx}, {troopIdx}, {horizLoc}, {vertLoc})");

			int nearestDistance = 999;
			ushort nearest = 0xffff;

			for (int otherIdx = 0; otherIdx < UnitMemory.TroopTally; otherIdx++)
			{
				if (!this.troops.Exists(participantIdx, otherIdx) || otherIdx == troopIdx)
					continue;

				int distance = ObtainDistance(
					horizLoc, this.troops.PullSigned(participantIdx, otherIdx, UnitMemory.Horiz),
					vertLoc, this.troops.PullSigned(participantIdx, otherIdx, UnitMemory.Vert));

				if (distance < nearestDistance)
				{
					nearestDistance = distance;
					nearest = (ushort)otherIdx;
				}
			}

			Search.FoundDistance = (ushort)nearestDistance;

			return nearest;
		}

		public int MergeCoordinates(int horizLoc, int vertLoc)
		{
			return MapRules.LongerPlusHalfShorter(horizLoc, vertLoc);
		}

		public ushort ComputeDistanceFromOffsets(int horizLoc, int vertLoc)
		{
			using var _ = this.Owner.Journal.Scope($"ObtainDistanceFromOffsets({horizLoc}, {vertLoc})");

			ushort result = (ushort)MapRules.DoubledDistance(horizLoc, vertLoc);

			return result;
		}

		public int ObtainDistance(int horizLoc, int horizSpot1, int vertLoc, int vertSpot1)
		{
			return MapRules.Distance(horizLoc, vertLoc, horizSpot1, vertSpot1);
		}

		public ushort ComputePlayerPopulation(short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainPlayerPopulation({participantIdx})");

			ushort population = 0;

			foreach (City town in this.Owner.World.Towns)
			{
				if (town.ParticipantIdx == participantIdx && town.ConditionMarker != 0xff)
				{
					population = (ushort)(population + PopulationRules.CityPopulation(town.TrueExtent));
				}
			}

			FormatPopulation((short)population);

			return population;
		}

		public void FormatPopulation(int inhabitants)
		{
			if (inhabitants >= 100)
			{
				this.Owner.Words.Append(this.Owner.Libc.IntegerToText(inhabitants / 100, 10));

				this.Owner.Words.Append(",");

				if ((inhabitants % 100) < 10)
				{
					this.Owner.Words.Append("0");
				}
			}

			this.Owner.Words.Append(this.Owner.Libc.IntegerToText(inhabitants % 100, 10));

			this.Owner.Words.Append("0,000");
		}

		public void FillBoxWithMotif(int horizLoc, int vertLoc, int breadth, int altitude)
		{
			if ((short)Screen.VgaMode != 0)
			{
				int vertLocScratch = vertLoc;
				int altitudeScratch = altitude;

				while (altitudeScratch > 0)
				{
					int tileAltitude = Math.Min(altitudeScratch, 16);
					int horizLocScratch = horizLoc;
					int breadthScratch = breadth;

					while (breadthScratch > 0)
					{
						int tileBreadth = Math.Min(breadthScratch, 32);

						this.Owner.VideoCard.RenderImage(
							Screen.PictureScreenBox,
							288, 120, tileBreadth, tileAltitude,
							Screen.Box,
							horizLocScratch, vertLocScratch);

						horizLocScratch += tileBreadth;
						breadthScratch -= tileBreadth;
					}

					vertLocScratch += tileAltitude;
					altitudeScratch -= tileAltitude;
				}
			}
			else
			{
				this.Owner.Services.FloodBox(Screen.Box,
					horizLoc, vertLoc, breadth, altitude, Hues.LightGray);
			}		
		}

		public void PullAndAssignColors(ushort docnameAddr)
		{
			using var _ = this.Processor.Journal.Scope($"PullAndAssignColors(0x{docnameAddr:x4})");

			if (Screen.VgaMode != 0)
			{
				this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, docnameAddr, 0xbdee);

				for (int idx = 0; idx < 0x30; idx++)
				{
					this.Processor.EmitGlobalByte((ushort)(0xbdf4 + idx), this.Processor.PullGlobalByte((ushort)(0x6b34 + idx)));
				}

				this.Owner.VideoCard.AssignHuesFromHueStruct(0xbdee);
				this.Owner.VideoCard.AssignHue(0x2d, VGABitmap.Hue18ToHue(0x33, 0x27, 0x19));
			}
		}

		public void ReleaseResource(ushort resource, ushort charsAddr)
		{
			using var _ = this.Processor.Journal.Scope($"ReleaseResource({resource}, 0x{charsAddr:x4})");

			if (!this.Owner.Libc.DosReleaseMemory(resource))
			{
				this.Owner.Words.Set("MEM.ERR:");

				if (charsAddr != 0)
				{
					this.Owner.Words.Append(charsAddr);
				}

				this.Owner.Words.Append("\n");

				this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);
			}
		}

		public void SetupHueCycling()
		{
			using var _ = this.Processor.Journal.Scope("SetupHueCycling()");

			this.Owner.Services.AssignHueCycleSpan(1, 15, 96, 103);

			this.Owner.Services.AssignHueCycleSpan(2, 15, 104, 111);

			this.Owner.Services.AssignHueCycleSpan(3, 15, 112, 127);
		}

		public void LaunchHueCycling()
		{
			if (Screen.HueCyclingOn == 0 && Screen.VgaMode != 0)
			{
				this.Owner.Services.ActivateHueCycle(1);

				this.Owner.Services.ActivateHueCycle(2);

				this.Owner.Services.ActivateHueCycle(3);
			}
		
			Screen.HueCyclingOn = 0x1;
		}

		public void HaltHueCycling()
		{
			using var _ = this.Processor.Journal.Scope("HaltHueCycling()");

			if (Screen.HueCyclingOn != 0 &&
				Screen.VgaMode != 0)
			{
				this.Owner.Services.DeactivateHueCycle(1);

				this.Owner.Services.DeactivateHueCycle(2);

				this.Owner.Services.DeactivateHueCycle(3);
			}

			Screen.HueCyclingOn = 0x0;
		}
	}
}
