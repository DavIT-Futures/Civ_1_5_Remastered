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
		/// ?
		/// </summary>
		public void SeedRandomizer()
		{
			// function body
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.CurrentTime(0) & 0x7fff);

			this.owner.World.ChanceRoot = this.processor.RegAcc.Value16;

			this.owner.Libc.SeedRandom(this.processor.RegAcc.Value16);
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
			// function body
			if (floorAmount > peakAmount)
				throw new Exception("Minimum value is greater than maximum value");

			int backAmountInt = Math.Min(Math.Max(amount, floorAmount), peakAmount);
			this.processor.RegAcc.Value16 = (ushort)((short)backAmountInt);

			return backAmountInt;
		}

		/// <summary>
		/// The city standing on the tile
		/// </summary>
		/// <returns>City index, or 0xffff</returns>
		public ushort LocateCityAtPosition(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"LocateCityAtPosition({horizLoc}, {vertLoc})");

			ushort found = 0xffff;

			for (int townIdx = 0; townIdx < this.owner.World.Towns.Length; townIdx++)
			{
				City town = this.owner.World.Towns[townIdx];

				if (town.ConditionMarker != 0xff && town.Place.X == horizLoc && town.Place.Y == vertLoc)
				{
					found = (ushort)townIdx;
					break;
				}
			}

			this.processor.RegAcc.Value16 = found;

			this.processor.Journal.CloseScope("LocateCityAtPosition");

			return found;
		}

		/// <summary>
		/// The nearest city of any player (MapRules.Distance, the first one of equally near cities).
		/// Its distance goes to 0x6c9a, 999 when there is no city.
		/// </summary>
		/// <returns>City index, or 0xffff</returns>
		public ushort LocateNearestCity(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"LocateNearestCity({horizLoc}, {vertLoc})");

			int nearestDistance = 999;
			ushort nearest = 0xffff;

			for (int townIdx = 0; townIdx < this.owner.World.Towns.Length; townIdx++)
			{
				City town = this.owner.World.Towns[townIdx];

				if (town.ConditionMarker == 0xff)
					continue;

				int distance = ObtainDistance(horizLoc, town.Place.X, vertLoc, town.Place.Y);

				if (distance < nearestDistance)
				{
					nearestDistance = distance;
					nearest = (ushort)townIdx;
				}
			}

			this.processor.EmitGlobalWord(0x6c9a, (ushort)nearestDistance);
			this.processor.RegAcc.Value16 = nearest;

			this.processor.Journal.CloseScope("LocateNearestCity");

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
			this.processor.Journal.OpenScope($"LocateNearestUnit({participantIdx}, {troopIdx}, {horizLoc}, {vertLoc})");

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

			this.processor.EmitGlobalWord(0x6c9a, (ushort)nearestDistance);
			this.processor.RegAcc.Value16 = nearest;

			this.processor.Journal.CloseScope("LocateNearestUnit");

			return nearest;
		}

		/// <summary>
		/// The larger of the two steps plus half of the smaller one (MapRules.LongerPlusHalfShorter)
		/// </summary>
		public int MergeCoordinates(int horizLoc, int vertLoc)
		{
			int merged = MapRules.LongerPlusHalfShorter(horizLoc, vertLoc);
			this.processor.RegAcc.Value16 = (ushort)((short)merged);

			return merged;
		}

		/// <summary>
		/// Twice the larger step plus the smaller one (MapRules.DoubledDistance)
		/// </summary>
		public ushort ObtainDistanceFromOffsets(int horizLoc, int vertLoc)
		{
			this.owner.Journal.OpenScope($"ObtainDistanceFromOffsets({horizLoc}, {vertLoc})");

			this.processor.RegAcc.Value16 = (ushort)MapRules.DoubledDistance(horizLoc, vertLoc);

			this.owner.Journal.CloseScope("ObtainDistanceFromOffsets");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Distance of two positions (MapRules.Distance)
		/// </summary>
		public int ObtainDistance(int horizLoc, int horizSpot1, int vertLoc, int vertSpot1)
		{
			int distance = MapRules.Distance(horizLoc, vertLoc, horizSpot1, vertSpot1);
			this.processor.RegAcc.Value16 = (ushort)((short)distance);

			return distance;
		}

		/// <summary>
		/// Population of the civilization (PopulationRules.CityPopulation of all its cities), also written as text
		/// by PopulationToText
		/// </summary>
		public ushort ObtainPlayerPopulation(short participantIdx)
		{
			this.processor.Journal.OpenScope($"ObtainPlayerPopulation({participantIdx})");

			ushort population = 0;

			foreach (City town in this.owner.World.Towns)
			{
				if (town.ParticipantIdx == participantIdx && town.ConditionMarker != 0xff)
				{
					population = (ushort)(population + PopulationRules.CityPopulation(town.TrueExtent));
				}
			}

			PopulationToText((short)population);

			this.processor.RegAcc.Value16 = population;

			this.processor.Journal.CloseScope("ObtainPlayerPopulation");

			return population;
		}

		/// <summary>
		/// Converts population to string
		/// </summary>
		/// <param name="population"></param>
		public void PopulationToText(int inhabitants)
		{
			// function body
			if (inhabitants >= 100)
			{
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(inhabitants / 100, 10));

				this.owner.Libc.AppendText(0xba06, ",");

				if ((inhabitants % 100) < 10)
				{
					this.owner.Libc.AppendText(0xba06, "0");
				}
			}

			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(inhabitants % 100, 10));

			this.owner.Libc.AppendText(0xba06, "0,000");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public void FloodBoxWithMotif(int horizLoc, int vertLoc, int breadth, int altitude)
		{
			// function body
			if (this.processor.PullGlobalSignedWord(0xd762) != 0)
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

						this.owner.VideoCard.RenderImage(
							this.processor.PullGlobalWord(0x19e8),
							288, 120, tileBreadth, tileAltitude,
							this.processor.PullGlobalWord(0xaa),
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
				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
					horizLoc, vertLoc, breadth, altitude, 7);
			}		
		}

		/// <summary>
		/// On VGA (0xd762) loads the colours of the picture file into the palette record at 0xbdee, puts back the 48 colour
		/// bytes kept at 0x6b34 at its start, sets the palette and colour 45
		/// </summary>
		public void PullAndAssignColors(ushort docnameAddr)
		{
			this.processor.Journal.OpenScope($"PullAndAssignColors(0x{docnameAddr:x4})");

			if (this.processor.PullGlobalWord(0xd762) != 0)
			{
				this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, docnameAddr, 0xbdee);

				for (int idx = 0; idx < 0x30; idx++)
				{
					this.processor.EmitGlobalByte((ushort)(0xbdf4 + idx), this.processor.PullGlobalByte((ushort)(0x6b34 + idx)));
				}

				this.owner.VideoCard.AssignHuesFromHueStruct(0xbdee);
				this.owner.VideoCard.AssignHue(0x2d, VGABitmap.Hue18ToHue(0x33, 0x27, 0x19));
			}

			this.processor.Journal.CloseScope("PullAndAssignColors");
		}

		/// <summary>
		/// Free resource, show Memory error dialog if error happens
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="stringPtr"></param>
		public void ReleaseResource(ushort arg1, ushort charsAddr)
		{
			this.processor.Journal.OpenScope($"ReleaseResource({arg1}, 0x{charsAddr:x4})");

			// function body
			this.processor.EnterFrame();
			
			this.owner.Libc.DosReleaseMemory(arg1);

			if (this.processor.RegAcc.Value16 != 0)
			{
				this.owner.Libc.CopyText(0xba06, "MEM.ERR:");

				if (charsAddr != 0)
				{
					this.owner.Libc.AppendText(0xba06, charsAddr);
				}

				this.owner.Libc.AppendText(0xba06, "\n");

				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
			}
		
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("ReleaseResource");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void SetupHueCycling()
		{
			this.processor.Journal.OpenScope("SetupHueCycling()");

			// function body
			this.owner.Services.AssignHueCycleSpan(1, 15, 96, 103);

			this.owner.Services.AssignHueCycleSpan(2, 15, 104, 111);

			this.owner.Services.AssignHueCycleSpan(3, 15, 112, 127);

			// Far return
			this.processor.Journal.CloseScope("SetupHueCycling");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void LaunchHueCycling()
		{
			// function body
			if (this.processor.PullGlobalWord(0x2fec) == 0 && this.processor.PullGlobalWord(0xd762) != 0)
			{
				this.owner.Services.ActivateHueCycle(1);

				this.owner.Services.ActivateHueCycle(2);

				this.owner.Services.ActivateHueCycle(3);
			}
		
			this.processor.EmitGlobalWord(0x2fec, 0x1);
		}

		/// <summary>
		/// ?
		/// </summary>
		public void HaltHueCycling()
		{
			this.processor.Journal.OpenScope("HaltHueCycling()");

			// function body
			if (this.processor.PullGlobalWord(0x2fec) != 0 &&
				this.processor.PullGlobalWord(0xd762) != 0)
			{
				this.owner.Services.DeactivateHueCycle(1);

				this.owner.Services.DeactivateHueCycle(2);

				this.owner.Services.DeactivateHueCycle(3);
			}

			this.processor.EmitGlobalWord(0x2fec, 0x0);

			// Far return
			this.processor.Journal.CloseScope("HaltHueCycling");
		}
	}
}
