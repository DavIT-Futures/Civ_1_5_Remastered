using System;

namespace Civilization1Remaster.Core.Engine.Platform
{
	public class GameHelpers : GameComponent, IGameHelpers
	{
		private readonly UnitMemory troops;

		public GameHelpers(IGameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
		}

		public void SeedRandomizer()
		{
			ushort seed = (ushort)(this.Owner.Runtime.CurrentTime(0) & 0x7fff);
			this.Owner.World.ChanceRoot = seed;
			this.Owner.Runtime.SeedRandom(seed);
		}

		public int ClampValue(int amount, int floorAmount, int peakAmount)
		{
			if (floorAmount > peakAmount)
			{
				throw new Exception("Minimum value is greater than maximum value");
			}

			return Math.Min(Math.Max(amount, floorAmount), peakAmount);
		}

		public ushort FindCityAtPosition(int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"LocateCityAtPosition({horizontalLocation}, {verticalLocation})");

			ushort found = 0xffff;

			for (int townIndex = 0; townIndex < this.Owner.World.Towns.Length; townIndex++)
			{
				City town = this.Owner.World.Towns[townIndex];

				if (town.ConditionMarker != 0xff && town.Place.X == horizontalLocation && town.Place.Y == verticalLocation)
				{
					found = (ushort)townIndex;
					break;
				}
			}

			return found;
		}

		public ushort FindNearestCity(int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"LocateNearestCity({horizontalLocation}, {verticalLocation})");

			int nearestDistance = 999;
			ushort nearest = 0xffff;

			for (int townIndex = 0; townIndex < this.Owner.World.Towns.Length; townIndex++)
			{
				City town = this.Owner.World.Towns[townIndex];

				if (town.ConditionMarker == 0xff)
				{
					continue;
				}

				int distance = ObtainDistance(horizontalLocation, town.Place.X, verticalLocation, town.Place.Y);

				if (distance < nearestDistance)
				{
					nearestDistance = distance;
					nearest = (ushort)townIndex;
				}
			}

			Search.FoundDistance = (ushort)nearestDistance;

			return nearest;
		}

		public ushort FindNearestUnit(short participantIndex, short troopIndex, int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"LocateNearestUnit({participantIndex}, {troopIndex}, {horizontalLocation}, {verticalLocation})");

			int nearestDistance = 999;
			ushort nearest = 0xffff;

			for (int otherIndex = 0; otherIndex < UnitMemory.TroopTally; otherIndex++)
			{
				if (!this.troops.Exists(participantIndex, otherIndex) || otherIndex == troopIndex)
				{
					continue;
				}

				int distance = ObtainDistance(
					horizontalLocation, this.troops.PullSigned(participantIndex, otherIndex, UnitMemory.Horizontal),
					verticalLocation, this.troops.PullSigned(participantIndex, otherIndex, UnitMemory.Vertical));

				if (distance < nearestDistance)
				{
					nearestDistance = distance;
					nearest = (ushort)otherIndex;
				}
			}

			Search.FoundDistance = (ushort)nearestDistance;

			return nearest;
		}

		public int MergeCoordinates(int horizontalLocation, int verticalLocation)
		{
			return MapRules.LongerPlusHalfShorter(horizontalLocation, verticalLocation);
		}

		public ushort ComputeDistanceFromOffsets(int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Owner.Journal.Scope($"ObtainDistanceFromOffsets({horizontalLocation}, {verticalLocation})");

			ushort result = (ushort)MapRules.DoubledDistance(horizontalLocation, verticalLocation);

			return result;
		}

		public int ObtainDistance(int horizontalLocation, int firstHorizontalSpot, int verticalLocation, int firstVerticalSpot)
		{
			return MapRules.Distance(horizontalLocation, verticalLocation, firstHorizontalSpot, firstVerticalSpot);
		}

		public ushort ComputePlayerPopulation(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainPlayerPopulation({participantIndex})");

			ushort population = 0;

			foreach (City town in this.Owner.World.Towns)
			{
				if (town.ParticipantIndex == participantIndex && town.ConditionMarker != 0xff)
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
				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(inhabitants / 100, 10));

				this.Owner.Words.Append(",");

				if ((inhabitants % 100) < 10)
				{
					this.Owner.Words.Append("0");
				}
			}

			this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(inhabitants % 100, 10));

			this.Owner.Words.Append("0,000");
		}

		public void FillBoxWithMotif(int horizontalLocation, int verticalLocation, int breadth, int altitude)
		{
			if ((short)Screen.VideoGraphicsMode != 0)
			{
				int verticalLocationScratch = verticalLocation;
				int altitudeScratch = altitude;

				while (altitudeScratch > 0)
				{
					int tileAltitude = Math.Min(altitudeScratch, 16);
					int horizontalLocationScratch = horizontalLocation;
					int breadthScratch = breadth;

					while (breadthScratch > 0)
					{
						int tileBreadth = Math.Min(breadthScratch, 32);

						this.Owner.VideoCard.RenderImage(
							Screen.PictureScreenBox,
							288, 120, tileBreadth, tileAltitude,
							Screen.Box,
							horizontalLocationScratch, verticalLocationScratch);

						horizontalLocationScratch += tileBreadth;
						breadthScratch -= tileBreadth;
					}

					verticalLocationScratch += tileAltitude;
					altitudeScratch -= tileAltitude;
				}
			}
			else
			{
				this.Owner.Services.FloodBox(Screen.Box,
					horizontalLocation, verticalLocation, breadth, altitude, Hues.LightGray);
			}
		}

		public void PullAndAssignColors(ushort documentNameAddress)
		{
			using var _ = this.Processor.Journal.Scope($"PullAndAssignColors(0x{documentNameAddress:x4})");

			if (Screen.VideoGraphicsMode != 0)
			{
				this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, documentNameAddress, 0xbdee);

				for (int index = 0; index < 0x30; index++)
				{
					this.Processor.EmitGlobalByte((ushort)(0xbdf4 + index), this.Processor.PullGlobalByte((ushort)(0x6b34 + index)));
				}

				this.Owner.VideoCard.AssignHuesFromHueStructure(0xbdee);
				this.Owner.VideoCard.AssignHue(0x2d, VideoGraphicsBitmap.HueFromDigitalLevels(0x33, 0x27, 0x19));
			}
		}

		public void ReleaseResource(ushort resource, ushort charactersAddress)
		{
			using var _ = this.Processor.Journal.Scope($"ReleaseResource({resource}, 0x{charactersAddress:x4})");

			if (!this.Owner.Runtime.OperatingSystemReleaseMemory(resource))
			{
				this.Owner.Words.Set("MEM.ERR:");

				if (charactersAddress != 0)
				{
					this.Owner.Words.Append(charactersAddress);
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
			if (Screen.HueCyclingOn == 0 && Screen.VideoGraphicsMode != 0)
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
				Screen.VideoGraphicsMode != 0)
			{
				this.Owner.Services.DeactivateHueCycle(1);

				this.Owner.Services.DeactivateHueCycle(2);

				this.Owner.Services.DeactivateHueCycle(3);
			}

			Screen.HueCyclingOn = 0x0;
		}
	}
}
