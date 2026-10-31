using System;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x2dc4.
	/// </summary>
	public class CommonUtilities : GameModule
	{
		private readonly UnitMemory troops;

		public CommonUtilities(GameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
		}

		/// <summary>
		/// Seeds the random generator with the clock (15 bits of the seconds), kept as the seed of the world (ChanceRoot)
		/// </summary>
		public void SeedRandomizer()
		{
			ushort seed = (ushort)(this.Owner.Libc.CurrentTime(0) & 0x7fff);
			this.Owner.World.ChanceRoot = seed;
			this.Owner.Libc.SeedRandom(seed);
		}

		/// <summary>
		/// Check and adjusts value to given range
		/// </summary>
		/// <param name="value"></param>
		/// <param name="minValue"></param>
		/// <param name="maxValue"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		public int VerifyValueSpan(int amount, int floorAmount, int peakAmount)
		{
			if (floorAmount > peakAmount)
				throw new Exception("Minimum value is greater than maximum value");

			return Math.Min(Math.Max(amount, floorAmount), peakAmount);
		}

		/// <summary>
		/// The city standing on the tile
		/// </summary>
		/// <returns>City index, or 0xffff</returns>
		public ushort LocateCityAtPosition(int horizLoc, int vertLoc)
		{
			this.Processor.Journal.OpenScope($"LocateCityAtPosition({horizLoc}, {vertLoc})");

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

			this.Processor.Journal.CloseScope("LocateCityAtPosition");

			return found;
		}

		/// <summary>
		/// The nearest city of any player (MapRules.Distance, the first one of equally near cities).
		/// Its distance goes to 0x6c9a, 999 when there is no city.
		/// </summary>
		/// <returns>City index, or 0xffff</returns>
		public ushort LocateNearestCity(int horizLoc, int vertLoc)
		{
			this.Processor.Journal.OpenScope($"LocateNearestCity({horizLoc}, {vertLoc})");

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

			FoundDistance = (ushort)nearestDistance;

			this.Processor.Journal.CloseScope("LocateNearestCity");

			return nearest;
		}

		/// <summary>
		/// The nearest other unit of the player (MapRules.Distance, the first one of equally near units).
		/// Its distance goes to 0x6c9a, 999 when there is none.
		/// </summary>
		/// <param name="troopIdx">The unit itself, it is skipped</param>
		/// <returns>Unit index, or 0xffff</returns>
		public ushort LocateNearestUnit(short participantIdx, short troopIdx, int horizLoc, int vertLoc)
		{
			this.Processor.Journal.OpenScope($"LocateNearestUnit({participantIdx}, {troopIdx}, {horizLoc}, {vertLoc})");

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

			FoundDistance = (ushort)nearestDistance;

			this.Processor.Journal.CloseScope("LocateNearestUnit");

			return nearest;
		}

		/// <summary>
		/// The larger of the two steps plus half of the smaller one (MapRules.LongerPlusHalfShorter)
		/// </summary>
		public int MergeCoordinates(int horizLoc, int vertLoc)
		{
			return MapRules.LongerPlusHalfShorter(horizLoc, vertLoc);
		}

		/// <summary>
		/// Twice the larger step plus the smaller one (MapRules.DoubledDistance)
		/// </summary>
		public ushort ObtainDistanceFromOffsets(int horizLoc, int vertLoc)
		{
			this.Owner.Journal.OpenScope($"ObtainDistanceFromOffsets({horizLoc}, {vertLoc})");

			ushort result = (ushort)MapRules.DoubledDistance(horizLoc, vertLoc);

			this.Owner.Journal.CloseScope("ObtainDistanceFromOffsets");

			return result;
		}

		/// <summary>
		/// Distance of two positions (MapRules.Distance)
		/// </summary>
		public int ObtainDistance(int horizLoc, int horizSpot1, int vertLoc, int vertSpot1)
		{
			return MapRules.Distance(horizLoc, vertLoc, horizSpot1, vertSpot1);
		}

		/// <summary>
		/// Population of the civilization (PopulationRules.CityPopulation of all its cities), also written as text
		/// by PopulationToText
		/// </summary>
		public ushort ObtainPlayerPopulation(short participantIdx)
		{
			this.Processor.Journal.OpenScope($"ObtainPlayerPopulation({participantIdx})");

			ushort population = 0;

			foreach (City town in this.Owner.World.Towns)
			{
				if (town.ParticipantIdx == participantIdx && town.ConditionMarker != 0xff)
				{
					population = (ushort)(population + PopulationRules.CityPopulation(town.TrueExtent));
				}
			}

			PopulationToText((short)population);

			this.Processor.Journal.CloseScope("ObtainPlayerPopulation");

			return population;
		}

		/// <summary>
		/// Converts population to string
		/// </summary>
		/// <param name="population"></param>
		public void PopulationToText(int inhabitants)
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

		/// <summary>
		/// Fills the box with the background motif: on VGA tiles of 32 x 16 from (288, 120) of the picture screen, on EGA
		/// colour 7
		/// </summary>
		public void FloodBoxWithMotif(int horizLoc, int vertLoc, int breadth, int altitude)
		{
			if ((short)VgaMode != 0)
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
							PictureScreenBox,
							288, 120, tileBreadth, tileAltitude,
							ScreenBox,
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
				this.Owner.Services.FloodBox(ScreenBox,
					horizLoc, vertLoc, breadth, altitude, Hues.LightGray);
			}		
		}

		/// <summary>
		/// On VGA (0xd762) loads the colours of the picture file into the palette record at 0xbdee, puts back the 48 colour
		/// bytes kept at 0x6b34 at its start, sets the palette and colour 45
		/// </summary>
		public void PullAndAssignColors(ushort docnameAddr)
		{
			this.Processor.Journal.OpenScope($"PullAndAssignColors(0x{docnameAddr:x4})");

			if (VgaMode != 0)
			{
				this.Owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, docnameAddr, 0xbdee);

				for (int idx = 0; idx < 0x30; idx++)
				{
					this.Processor.EmitGlobalByte((ushort)(0xbdf4 + idx), this.Processor.PullGlobalByte((ushort)(0x6b34 + idx)));
				}

				this.Owner.VideoCard.AssignHuesFromHueStruct(0xbdee);
				this.Owner.VideoCard.AssignHue(0x2d, VGABitmap.Hue18ToHue(0x33, 0x27, 0x19));
			}

			this.Processor.Journal.CloseScope("PullAndAssignColors");
		}

		/// <summary>
		/// Free resource, show Memory error dialog if error happens
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="stringPtr"></param>
		public void ReleaseResource(ushort resource, ushort charsAddr)
		{
			this.Processor.Journal.OpenScope($"ReleaseResource({resource}, 0x{charsAddr:x4})");

			if (!this.Owner.Libc.DosReleaseMemory(resource))
			{
				this.Owner.Words.Set("MEM.ERR:");

				if (charsAddr != 0)
				{
					this.Owner.Words.Append(charsAddr);
				}

				this.Owner.Words.Append("\n");

				this.Owner.TurnHooks.DisplayDialog(TextBuffer.Address, 100, 80);
			}
			this.Processor.Journal.CloseScope("ReleaseResource");
		}

		/// <summary>
		/// Sets the colour cycles 1 - 3 of the map: the colours 96 - 103, 104 - 111 and 112 - 127, 15 beats a step
		/// </summary>
		public void SetupHueCycling()
		{
			this.Processor.Journal.OpenScope("SetupHueCycling()");

			this.Owner.Services.AssignHueCycleSpan(1, 15, 96, 103);

			this.Owner.Services.AssignHueCycleSpan(2, 15, 104, 111);

			this.Owner.Services.AssignHueCycleSpan(3, 15, 112, 127);

			this.Processor.Journal.CloseScope("SetupHueCycling");
		}

		/// <summary>Starts the colour cycles 1 - 3 (on VGA, unless they run already, HueCyclingOn)</summary>
		public void LaunchHueCycling()
		{
			if (HueCyclingOn == 0 && VgaMode != 0)
			{
				this.Owner.Services.ActivateHueCycle(1);

				this.Owner.Services.ActivateHueCycle(2);

				this.Owner.Services.ActivateHueCycle(3);
			}
		
			HueCyclingOn = 0x1;
		}

		/// <summary>Stops the colour cycles 1 - 3 (on VGA, when they run)</summary>
		public void HaltHueCycling()
		{
			this.Processor.Journal.OpenScope("HaltHueCycling()");

			if (HueCyclingOn != 0 &&
				VgaMode != 0)
			{
				this.Owner.Services.DeactivateHueCycle(1);

				this.Owner.Services.DeactivateHueCycle(2);

				this.Owner.Services.DeactivateHueCycle(3);
			}

			HueCyclingOn = 0x0;

			this.Processor.Journal.CloseScope("HaltHueCycling");
		}
	}
}
