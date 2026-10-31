
using System;

namespace Civilization1Remaster.Core.Game
{
	/// <summary>The trade, corruption, science and happiness of a city, the support of its units</summary>
	partial class Cities
	{
		/// <summary>
		/// The corruption and the trade routes: 0xd2e0 the corruption (trade x the distance to the palace 0x6b30 x 3 / (20
		/// x government + 80); communism counts the distance as 10; a courthouse or the palace halves it, democracy has
		/// none), RootCommerce the trade less it; each trade route adds an eighth (a foreign city) or a sixteenth (an own
		/// city) of the trade of both cities + 4. The locals of HandleCity are used as in the original.
		/// </summary>
		private void CountCorruptionAndTradeRoutes(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;
			Player player = this.Owner.World.Participants[owner];

			this.round.Horiz = (ushort)(short)town.Place.X;
			this.round.Vert = (ushort)(short)town.Place.Y;
			CityTurn.Corruption = 0;

			if (player.RegimeKind == 3)
				CityTurn.PalaceDistance = 0xa;

			// The corruption
			short corruption = (short)((short)((short)CityTurn.Trade * (short)CityTurn.PalaceDistance) * 3);
			CityTurn.Corruption = (ushort)(corruption / (short)(ushort)(20 * player.RegimeKind + 0x50));

			if (town.Has(CityStructure.Courthouse) || town.Has(CityStructure.Palace))
			{
				// A courthouse or the palace
				CityTurn.Corruption = (ushort)((short)CityTurn.Corruption / 2);
			}

			if (player.RegimeKind == 5)
				CityTurn.Corruption = 0;

			town.RootCommerce = (sbyte)(byte)((byte)CityTurn.Trade - (byte)CityTurn.Corruption);

			// The trade routes
			for (ushort route = 0; route < 3; route++)
			{
				this.round.Item = route;
				this.round.OtherCity = (byte)town.CommerceTownIndices[route];

				if (this.round.OtherCity == 0xff)
					continue;

				City partner = this.Owner.World.Towns[this.round.OtherCity];
				short share = (short)(partner.RootCommerce + CityTurn.Trade + 4);
				share = (short)(partner.ParticipantIdx != owner ? share / 8 : share / 16);
				CityTurn.Trade = (ushort)(CityTurn.Trade + share);
			}

			this.round.Item = 3;
		}

		/// <summary>
		/// The trade of the city split: 0xd2e0 the corruption again (of the trade with the routes; democracy none, a
		/// courthouse halves it), 0x70e0 the luxuries, 0xe17a the taxes, 0x70e6 the science by the rates of the owner; the
		/// specialists add 2 each (taxmen, scientists, entertainers: TallySpecialists 1, 2, 3); a library (0x10) and a
		/// university (0x200) each add half of the luxuries and taxes (as translated). The locals of HandleCity are used as
		/// in the original.
		/// </summary>
		private void SplitTrade(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			Player player = this.Owner.World.Participants[(short)CityTurn.Owner];

			// The corruption of the whole trade
			short corruption = (short)((short)((short)CityTurn.Trade * (short)CityTurn.PalaceDistance) * 3);
			CityTurn.Corruption = (ushort)(corruption / (short)(ushort)(20 * player.RegimeKind + 0x50));

			if (player.RegimeKind == 5)
				CityTurn.Corruption = 0;

			if (town.Has(CityStructure.Courthouse))
			{
				CityTurn.Corruption = (ushort)((short)CityTurn.Corruption / 2);
			}

			// The luxuries, the taxes and the science
			short trade = (short)CityTurn.Trade;
			short corrupt = (short)CityTurn.Corruption;

			CityTurn.Luxuries = (ushort)this.Owner.Helpers.ClampValue(((-(player.ResearchLevyRatio + player.LevyRatio - 10) * (trade - corrupt)) + 5) / 10, 0, trade);

			CityTurn.Taxes = (ushort)this.Owner.Helpers.ClampValue(((player.LevyRatio * (trade - corrupt)) + 5) / 10, 0,
				trade - (short)CityTurn.Luxuries - corrupt);

			CityTurn.Science = (ushort)(CityTurn.Trade - CityTurn.Luxuries - CityTurn.Taxes - CityTurn.Corruption);

			// The specialists
			CityTurn.Taxes = (ushort)(CityTurn.Taxes + (ushort)(TallySpecialists(1) << 1));
			CityTurn.Science = (ushort)(CityTurn.Science + (ushort)(TallySpecialists(2) << 1));
			CityTurn.Luxuries = (ushort)(CityTurn.Luxuries + (ushort)(TallySpecialists(3) << 1));

			// The library and the university

			foreach (ushort improvement in new ushort[] { 0x10, 0x200 })
			{
				if ((town.StructureMarkers0 & improvement) == 0)
					continue;

				CityTurn.Luxuries = (ushort)(CityTurn.Luxuries + ((ushort)((short)CityTurn.Luxuries / 2)));
				CityTurn.Taxes = (ushort)(CityTurn.Taxes + ((ushort)((short)CityTurn.Taxes / 2)));
			}
		}

		/// <summary>
		/// The science (0x70e6, as translated): a marketplace (0x20) and a bank (0x800) each add a half (and a third more
		/// with the wonder 0xc), the city of the wonder 10 doubles it; 0x70e2 cleared. 0x70e4 / 0xa8 the content citizens:
		/// for the human by the level, the government, the cities and the number of the city, for a computer player the
		/// size - 3; 0x6542 the citizens above the size (then 0x70e4 the size); then the citizens are reshuffled. The
		/// locals of HandleCity are used as in the original.
		/// </summary>
		private void CountScienceAndContent(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;
			Player player = this.Owner.World.Participants[owner];

			this.round.Value = CityTurn.Science;

			foreach (ushort improvement in new ushort[] { 0x20, 0x800 })
			{
				if ((town.StructureMarkers0 & improvement) == 0)
					continue;

				this.round.Value = (ushort)(this.round.Value + ((ushort)((short)CityTurn.Science / 2)));

				if (PlayerEnjoysWonderEffect(owner, 0xc) != 0)
				{
					this.round.Value = (ushort)(this.round.Value + (CityTurn.Science / 3));
				}
			}

			// The city of the wonder 10
			if (ObtainWonderCityID(10) == (ushort)townIdx)
			{
				this.round.Value = (ushort)(this.round.Value * 2);
			}

			CityTurn.Science = this.round.Value;
			CityTurn.Happy = 0;

			// The content citizens
			if (owner == this.Owner.World.PersonParticipantIdx)
			{
				short step = (short)(-(this.Owner.World.HardnessTier * 2 - 0xe) / 2 * (player.RegimeKind / 2 + 2));

				if (step == 0)
					throw new Exception("Division by zero");

				short spread = (short)((short)(townIdx % step + player.TownTally - step) / step);
				this.round.Content = (ushort)(spread + town.TrueExtent + this.Owner.World.HardnessTier - 6);
			}
			else
			{
				this.round.Content = (ushort)(town.TrueExtent - 3);
			}

			CityTurn.Unhappy = this.round.Content;

			// The citizens above the size
			CityTurn.ExcessUnhappy = 0;

			if (town.TrueExtent < (short)CityTurn.Unhappy)
			{
				CityTurn.ExcessUnhappy = (ushort)(CityTurn.Unhappy - town.TrueExtent);
				CityTurn.Unhappy = (ushort)town.TrueExtent;
			}

			ReshuffleCitizens(townIdx, this.round.Specialists);
		}

		/// <summary>
		/// The luxuries: on the city screen with the tab 1 the citizens are drawn (0x42 the line), then 0x70e2 = half of the
		/// luxuries and the citizens reshuffled again, drawn once more with the luxury icon when there are any. The locals of
		/// HandleCity are used as in the original.
		/// </summary>
		private void ApplyLuxuries(short townIdx, short marker)
		{
			if (marker == 1 && CityTurn.ScreenTab == 1)
			{
				this.round.IconVert = 0x74;
				DrawCitizenIcons(townIdx, 100, (short)this.round.IconVert, this.round.Specialists, 92);
				this.round.IconVert = (ushort)(this.round.IconVert + 0x10);
				this.Owner.VideoCard.RenderLine(Screen.Box,
					100, (short)this.round.IconVert - 2, 222, (short)this.round.IconVert - 2, Hues.Blue);
			}

			CityTurn.Happy = (ushort)((short)CityTurn.Luxuries / 2);

			ReshuffleCitizens(townIdx, this.round.Specialists);

			if (marker == 1 && CityTurn.ScreenTab == 1 && CityTurn.Happy != 0)
			{
				DrawCitizenIcons(townIdx, 100, (short)this.round.IconVert, this.round.Specialists, 92);
				this.Owner.Services.DrawPictureToScreen(Screen.Box, 208, (short)this.round.IconVert + 4,
					SpritePicture(0xe));
				this.round.IconVert = (ushort)(this.round.IconVert + 0x10);
				this.Owner.VideoCard.RenderLine(Screen.Box,
					100, (short)this.round.IconVert - 2, 222, (short)this.round.IconVert - 2, Hues.Blue);
			}

		}

		/// <summary>
		/// The improvements that make citizens content (0x70e4 the unhappy ones less): a colosseum (0x2000) 3; with the advance
		/// 0x1d a cathedral (0x400) 4, or 6 with the wonder 9; a temple (8) 2 with the advance 0x1a, else 1 with 0x19, and the wonder
		/// 6 as much again (2 with 0x1a, else 1). On the city screen with the tab 1 their icons are drawn (0x6 the column, 0x42 the
		/// line), then the citizens are reshuffled and drawn again when the city has any of them. The locals of HandleCity are
		/// used as in the original.
		/// </summary>
		private void ApplyContentImprovements(short townIdx, short marker)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;
			bool tab = marker == 1 && CityTurn.ScreenTab == 1;

			this.round.IconHoriz = 0xd0;

			// The colosseum
			if (town.Has(CityStructure.Colosseum))
			{
				CityTurn.Unhappy = (ushort)(CityTurn.Unhappy - 3);

				if (tab)
					DrawContentIcon(14);
			}

			// The cathedral (the advance 0x1d; the wonder 9)
			if (this.Owner.Advisors.PlayerKnowsTechnology(owner, 0x1d) != 0)
			{
				if (town.Has(CityStructure.Cathedral))
				{
					ushort relief = (ushort)(PlayerEnjoysWonderEffect(owner, 9) != 0 ? 6 : 4);
					CityTurn.Unhappy = (ushort)(CityTurn.Unhappy - relief);
				}

				if (tab)
				{
					if (town.Has(CityStructure.Cathedral))
						DrawContentIcon(11);
				}
			}

			// The temple (the advances 0x1a and 0x19; the wonder 6)

			if (town.Has(CityStructure.Temple))
			{
				if (this.Owner.Advisors.PlayerKnowsTechnology(owner, 0x1a) != 0)
					CityTurn.Unhappy = (ushort)(CityTurn.Unhappy - 2);
				else if (this.Owner.Advisors.PlayerKnowsTechnology(owner, 0x19) != 0)
					CityTurn.Unhappy = (ushort)(CityTurn.Unhappy - 1);

				if (PlayerEnjoysWonderEffect(owner, 6) != 0)
				{
					ushort relief = (ushort)(this.Owner.Advisors.PlayerKnowsTechnology(owner, 0x1a) != 0 ? 2 : 1);
					CityTurn.Unhappy = (ushort)(CityTurn.Unhappy - relief);
				}

				if (tab)
					DrawContentIcon(4);
			}

			ReshuffleCitizens(townIdx, this.round.Specialists);

			if (marker == 1 && CityTurn.ScreenTab == 1)
			{
				if ((town.StructureMarkers0 & 0x2408) != 0)
				{
					DrawCitizenIcons(townIdx, 100, (short)this.round.IconVert, this.round.Specialists, 92);
					this.round.IconVert = (ushort)(this.round.IconVert + 0x10);
					this.Owner.VideoCard.RenderLine(Screen.Box,
						100, (short)this.round.IconVert - 2, 222, (short)this.round.IconVert - 2, Hues.Blue);
				}
			}

		}

		/// <summary>An icon of the citizens' contentment on the city screen at the column 0x6 (which moves 16 left)</summary>
		private void DrawContentIcon(short icon)
		{
			DrawResourceIcon(icon, (short)this.round.IconHoriz, (short)this.round.IconVert);
			this.round.IconHoriz = (ushort)(this.round.IconHoriz - 0x10);
		}

		/// <summary>
		/// The military and the citizens: below republic (government under 4) up to 3 attack units in the city (its stack,
		/// at most 32 steps, and the units in the special slots while some are unhappy) make as many unhappy citizens
		/// content (drawn on the city screen, 0xe6 the count); in a republic or democracy every attack unit away (0x6546)
		/// makes one citizen unhappy (two in a democracy, none in a republic with the wonder 16; drawn on the screen). Then
		/// the citizens are reshuffled and drawn again. The locals of HandleCity are used as in the original.
		/// </summary>
		private void ApplyMilitaryHappiness(short townIdx, short marker)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;
			Player player = this.Owner.World.Participants[owner];
			bool tab = marker == 1 && CityTurn.ScreenTab == 1;

			this.round.IconHoriz = 0xd0;

			if (player.RegimeKind < 4)
			{
				// Martial law: the attack units in the city
				this.round.MartialLawUnits = 0;
				this.round.UnitsAway = 0;

				this.round.Value = this.Owner.MapOps.ObtainUnitAtTile((short)this.round.Horiz, (short)this.round.Vert);
				this.round.NewUnit = this.round.Value;

				while (this.round.NewUnit != 0xffff)
				{
					this.round.MartialLawUnits = (ushort)(this.round.MartialLawUnits + 1);

					if ((short)this.round.MartialLawUnits >= 0x20)
						break;

					short troopIdx = (short)this.round.NewUnit;

					if (this.troops.KindField(owner, troopIdx, UnitKindTable.AssaultPower) != 0)
					{
						this.round.UnitsAway = (ushort)(this.round.UnitsAway + 1);

						if (tab && (short)this.round.UnitsAway <= 3)
						{
							this.Owner.Services.DrawPictureToScreen(Screen.Box, (short)this.round.IconHoriz,
								(short)this.round.IconVert - 1,
								SpritePicture((this.troops.Pull(owner, troopIdx, UnitMemory.Kind) + (owner << 5) + 0x40)));
							this.round.IconHoriz = (ushort)(this.round.IconHoriz - 2);
						}
					}

					this.round.NewUnit = (ushort)this.troops.PullSigned(owner, troopIdx, UnitMemory.FollowingInStack);

					if (this.round.NewUnit == this.round.Value)
						this.round.NewUnit = 0xffff;
				}

				// The attack units in the special slots
				for (ushort slot = 0; slot < 2; slot++)
				{
					this.round.NewUnit = slot;

					if (CityTurn.Unhappy == 0)
						continue;

					if ((byte)town.Unfamiliar[slot] == 0xff)
						continue;

					if (UnitKindWord(town.Unfamiliar[slot] & 0x3f, UnitKindTable.AssaultPower) != 0)
						this.round.UnitsAway = (ushort)(this.round.UnitsAway + 1);
				}

				this.round.NewUnit = 2;

				if ((short)this.round.UnitsAway > 3)
					this.round.UnitsAway = 3;

				CityTurn.Unhappy = (ushort)(CityTurn.Unhappy - (ushort)this.Owner.Helpers.ClampValue((short)this.round.UnitsAway, 0, (short)CityTurn.Unhappy));
			}
			else
			{
				// The units away make citizens unhappy (not with the wonder 16 in a republic; two in a democracy)
				this.round.Value = (ushort)(PlayerEnjoysWonderEffect(owner, 16) < 1 ? 1 : 0);

				if (player.RegimeKind == 5)
					this.round.Value = (ushort)(this.round.Value + 1);

				if (this.round.Value != 0)
				{
					short unhappy = (short)((short)this.round.Value * (short)CityTurn.AttackersAway);
					CityTurn.Unhappy = (ushort)(CityTurn.Unhappy + unhappy);

					if (tab)
					{
						ushort drawn = 0;

						for (; unhappy > (short)drawn; drawn++)
						{
							this.round.NewUnit = drawn;
							this.Owner.Services.DrawPictureToScreen(Screen.Box, (short)this.round.IconHoriz,
								(short)this.round.IconVert + 4, SpritePicture(0xd));
							this.round.IconHoriz = (ushort)(this.round.IconHoriz - 2);
						}

						this.round.NewUnit = drawn;
					}
				}
			}

			ReshuffleCitizens(townIdx, this.round.Specialists);

			if (marker == 1 && CityTurn.ScreenTab == 1)
			{
				DrawCitizenIcons(townIdx, 100, (short)this.round.IconVert, this.round.Specialists, 92);
				this.round.IconVert = (ushort)(this.round.IconVert + 0x10);
				this.Owner.VideoCard.RenderLine(Screen.Box,
					100, (short)this.round.IconVert - 2, 222, (short)this.round.IconVert - 2, Hues.Blue);
			}

		}

		/// <summary>
		/// The wonders and the citizens: the wonders 2 and 0x15 each make one more citizen happy (0x70e2), the city of the
		/// wonder 11 has no unhappy ones, the wonder 13 makes two content in the cities on its continent; the citizens are
		/// reshuffled and drawn on the city screen (tab 1) when something changed (0xe8 the balance before). Without the
		/// bit 2 of DiagMarkers 0x70e4 and 0x70e2 are cleared. The locals of HandleCity are used as in the original.
		/// </summary>
		private void ApplyWonderHappiness(short townIdx, short marker)
		{
			short owner = (short)CityTurn.Owner;

			this.round.Value = (ushort)(CityTurn.Happy - CityTurn.Unhappy);

			if (PlayerEnjoysWonderEffect(owner, 2) != 0)
				CityTurn.Happy = (ushort)(CityTurn.Happy + 1);

			if (PlayerEnjoysWonderEffect(owner, 0x15) != 0)
				CityTurn.Happy = (ushort)(CityTurn.Happy + 1);

			if (ObtainWonderCityID(11) == (ushort)townIdx)
				CityTurn.Unhappy = 0;

			if (PlayerEnjoysWonderEffect(owner, 0xd) != 0)
			{
				// The wonder 13 on the continent of the city
				short marvelIdx = this.Owner.World.MarvelTownIdx[13];
				ushort marvelContinent = this.Owner.MapOps.ObtainContinentID(this.Owner.World.Towns[marvelIdx].Place.X, this.Owner.World.Towns[marvelIdx].Place.Y);
				if (this.Owner.MapOps.ObtainContinentID((short)this.round.Horiz, (short)this.round.Vert) == marvelContinent)
					CityTurn.Unhappy = (ushort)(CityTurn.Unhappy - 2);
			}

			ReshuffleCitizens(townIdx, this.round.Specialists);

			if (marker == 1 && CityTurn.ScreenTab == 1 &&
				(ushort)(CityTurn.Happy - CityTurn.Unhappy) != this.round.Value)
			{
				DrawCitizenIcons(townIdx, 100, (short)this.round.IconVert, this.round.Specialists, 92);
				this.Owner.TextPainter.DrawTextToScreen0(GameTexts.TextWonders, 190, (short)this.round.IconVert + 5, Hues.White);
				this.round.IconVert = (ushort)(this.round.IconVert + 0x10);
				this.Owner.VideoCard.RenderLine(Screen.Box,
					100, (short)this.round.IconVert - 2, 222, (short)this.round.IconVert - 2, Hues.Blue);
			}

			if ((this.Owner.World.DiagMarkers & 2) == 0)
			{
				CityTurn.Unhappy = 0;
				CityTurn.Happy = 0;
			}
		}

		/// <summary>
		/// The start of the units of the city (again after a unit was disbanded): 0x4e the units drawn in the city, 0xdeb8
		/// the support (the special slots in use first), 0xdeb6, 0xd2f6 the support above the size; the places on the city
		/// screen (0xf6 / 0xfc the supported units, 0xfa / 0x100 the units in the city). The locals of HandleCity are used
		/// as in the original.
		/// </summary>
		private void StartUnitSupport(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			this.round.UnitsDrawn = 0;
			CityTurn.SpecialSlots = 0;
			Turn.UnitSupportCount = 0;
			CityTurn.UnitUpkeep = 0;

			CountSpecialSlots(townIdx);

			this.round.DrawHoriz = 8;
			this.round.DrawVert = 0x45;
			this.round.UnitHoriz = 0x64;
			this.round.UnitVert = 0x74;
		}

		/// <summary>
		/// The unit 0xea of the owner: one supported by the city adds its support (but diplomats and caravans: above the size,
		/// or every one for a government above monarchy with the bit 2 of DiagMarkers), may be disbanded in the turn and is
		/// drawn among the supported units of the city screen; every unit in the city is drawn in its list. True when a unit
		/// was disbanded for lack of shields (all the units are counted again).
		/// </summary>
		private bool SupportUnit(short townIdx, short marker)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;
			short troopIdx = (short)this.round.Index;

			if (this.troops.Pull(owner, troopIdx, UnitMemory.Kind) != 0xff && this.troops.PullSigned(owner, troopIdx, UnitMemory.BaseTown) == townIdx)
			{
				if (this.troops.PullSigned(owner, troopIdx, UnitMemory.Kind) < 0x1a)
				{
					CityTurn.SpecialSlots = (ushort)(CityTurn.SpecialSlots + 1);

					if (town.TrueExtent < (short)CityTurn.SpecialSlots ||
						(this.Owner.World.Participants[owner].RegimeKind > 1 && (this.Owner.World.DiagMarkers & 2) != 0))
					{
						CityTurn.UnitUpkeep = (ushort)(CityTurn.UnitUpkeep + 1);
					}
				}

				if (marker == 0 && DisbandUnsupportedUnit(townIdx))
					return true;

				if (marker == 1)
					DrawSupportedUnit(townIdx);
			}

			DrawUnitInCity(marker);
			return false;
		}

		/// <summary>
		/// In the turn: without shields for the support (or for a computer player in a republic or democracy, its city with
		/// the flag 1, every eighth turn), the unit of the city farthest away (but diplomats and caravans) goes: for lack
		/// of shields it is disbanded (the human is told) and true is returned; else a land unit other than settlers is
		/// disbanded and the flag 1 cleared, unless 0x8078 and the owner knows fewer advances than the human (then 0xdb42 =
		/// -999). The locals of HandleCity (0x108 the unit, 0xc8 its distance) are used as in the original.
		/// </summary>
		private bool DisbandUnsupportedUnit(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;
			short person = this.Owner.World.PersonParticipantIdx;
			bool enoughShields = (short)CityTurn.Shields >= (short)CityTurn.UnitUpkeep;

			if (enoughShields)
			{
				if ((town.ConditionMarker & 1) == 0 || ((byte)((byte)this.round.Index + (byte)(this.Owner.World.RoundTally & 0xff)) & 7) != 0 ||
					owner == person || this.Owner.World.Participants[owner].RegimeKind < 4)
				{
					return false;
				}
			}

			// The unit of the city farthest away
			this.round.Found = 0xffff;
			this.round.Best = 0xffff;

			for (ushort otherIdx = 0; otherIdx < 0x80; otherIdx++)
			{
				this.round.Selected = otherIdx;
				short other = (short)otherIdx;

				if (this.troops.Pull(owner, other, UnitMemory.Kind) == 0xff || this.troops.PullSigned(owner, other, UnitMemory.BaseTown) != townIdx ||
					this.troops.PullSigned(owner, other, UnitMemory.Kind) >= 0x1a)
				{
					continue;
				}


				this.round.Distance = (ushort)this.Owner.Helpers.ObtainDistance(town.Place.X, this.troops.PullSigned(owner, other, UnitMemory.Horiz),
					town.Place.Y, this.troops.PullSigned(owner, other, UnitMemory.Vert));

				if ((short)this.round.Distance > (short)this.round.Best)
				{
					this.round.Best = this.round.Distance;
					this.round.Found = this.round.Selected;
				}
			}

			this.round.Selected = 0x80;
			short farthest = (short)this.round.Found;

			if (enoughShields)
			{
				if (Turn.LateGameNudge != 0 &&
					this.Owner.World.Participants[owner].FoundScienceTally < this.Owner.World.Participants[person].FoundScienceTally)
				{
					Turn.DemocracyPressure = 0xfc19;
				}
				else if ((short)this.round.Best > 0)
				{
					if (this.troops.KindField(owner, farthest, UnitKindTable.LandGroup) == UnitKindTable.LandUnits && this.troops.Pull(owner, farthest, UnitMemory.Kind) != 0)
					{
						this.Owner.UnitOps.DestroyUnit(owner, farthest);
						town.ConditionMarker &= 0xfe;
					}
				}

				return false;
			}

			// No shields for the support: the unit is disbanded
			if (owner == person)
			{
				EmptyMessage()
					.City(townIdx)
					.Text(" can't support\n");
				this.Owner.Words.Append((ushort)(this.troops.TroopKind(owner, farthest) * UnitKindTable.Stride + UnitKindTable.Origin));
				this.Owner.Words.Append(".\n Unit Disbanded.\n");
				Screen.DialogStyle = 3;
				this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);
			}

			this.Owner.UnitOps.DestroyUnit(owner, (short)this.round.Found);
			return true;
		}

		/// <summary>
		/// A unit supported by the city on the city screen (0xf6 / 0xfc its place, 8 a row, 3 rows): the shield of its support
		/// (when some is paid), the food of settlers (two for a government above anarchy), the unhappy faces of an attack unit
		/// away in a republic or democracy (two in a democracy; none in a republic with the wonder 16), the unit and its dot on
		/// the mini map
		/// </summary>
		private void DrawSupportedUnit(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;
			short troopIdx = (short)this.round.Index;
			Player player = this.Owner.World.Participants[owner];

			if (this.troops.PullSigned(owner, troopIdx, UnitMemory.Kind) < 0x1a)
			{
				if (CityTurn.UnitUpkeep != 0)
				{
					this.Owner.Services.DrawPictureToScreen(Screen.Box, (short)this.round.DrawHoriz + 8,
						(short)this.round.DrawVert + 12, SpritePicture(0x9));
				}

				if (this.troops.Pull(owner, troopIdx, UnitMemory.Kind) == 0)
				{
					// The food of settlers
					this.Owner.Services.DrawPictureToScreen(Screen.Box, (short)this.round.DrawHoriz,
						(short)this.round.DrawVert + 12, SpritePicture(0x8));

					if (player.RegimeKind >= 2)
					{
						this.Owner.Services.DrawPictureToScreen(Screen.Box, (short)this.round.DrawHoriz + 2,
							(short)this.round.DrawVert + 12, SpritePicture(0x8));
					}
				}
				else
				{
					// The unhappy faces of an attack unit away
					this.round.Value = (ushort)(PlayerEnjoysWonderEffect(owner, 16) < 1 ? 1 : 0);

					if (player.RegimeKind == 5)
						this.round.Value = (ushort)(this.round.Value + 1);

					if (player.RegimeKind >= 4 && this.troops.KindField(owner, troopIdx, UnitKindTable.AssaultPower) != 0 && this.round.Value != 0 &&
						(this.troops.KindField(owner, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.AirUnits ||
							this.troops.Pull(owner, troopIdx, UnitMemory.Horiz) != (byte)(sbyte)town.Place.X ||
							this.troops.Pull(owner, troopIdx, UnitMemory.Vert) != (byte)(sbyte)town.Place.Y))
					{
						this.Owner.Services.DrawPictureToScreen(Screen.Box, (short)this.round.DrawHoriz,
							(short)this.round.DrawVert + 12, SpritePicture(0xd));

						if ((short)this.round.Value > 1)
						{
							this.Owner.Services.DrawPictureToScreen(Screen.Box, (short)this.round.DrawHoriz + 2,
								(short)this.round.DrawVert + 12, SpritePicture(0xd));
						}
					}
				}
			}

			// The unit and its dot on the mini map
			this.Owner.Services.DrawPictureToScreen(Screen.Box, (short)this.round.DrawHoriz, (short)this.round.DrawVert,
				SpritePicture((this.troops.Pull(owner, troopIdx, UnitMemory.Kind) + (owner << 5) + 0x40)));

			DrawMiniMapDot(this.troops.Pull(owner, troopIdx, UnitMemory.Horiz), this.troops.Pull(owner, troopIdx, UnitMemory.Vert), 7);

			this.round.DrawHoriz = (ushort)(this.round.DrawHoriz + 0x10);

			if ((short)this.round.DrawHoriz >= 0x70)
			{
				this.round.DrawHoriz = 8;
				this.round.DrawVert = (ushort)(this.round.DrawVert + 0x10);

				if ((short)this.round.DrawVert > 0x55)
					this.round.DrawVert = (ushort)(this.round.DrawVert - 0x18);
			}
		}

		/// <summary>
		/// A unit standing in the city on the city screen with the tab 0 (at most 18, 6 a row; 0xfa / 0x100 the place): the unit
		/// and the abbreviation of its home city; its number goes into the list at BP-0x74 (0x4e the count)
		/// </summary>
		private void DrawUnitInCity(short marker)
		{
			short owner = (short)CityTurn.Owner;
			short troopIdx = (short)this.round.Index;

			if ((short)this.round.UnitsDrawn >= 0x12 || CityTurn.ScreenTab != 0)
				return;

			if (this.troops.Pull(owner, troopIdx, UnitMemory.Kind) == 0xff || marker != 1 ||
				this.troops.Pull(owner, troopIdx, UnitMemory.Horiz) != this.round.Horiz ||
				this.troops.Pull(owner, troopIdx, UnitMemory.Vert) != this.round.Vert)
			{
				return;
			}

			this.Owner.MapOps.DrawUnitSprite(owner, troopIdx, (short)this.round.UnitHoriz, (short)this.round.UnitVert);

			// The abbreviation of the home city (3 letters and a dot)
			this.Owner.Words.Clear();
			this.Owner.Conquest.ObtainCityLabel(this.troops.PullSigned(owner, troopIdx, UnitMemory.BaseTown));
			this.Owner.Words[3] = 0x2e;
			this.Owner.Words[4] = 0;
			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), (short)this.round.UnitHoriz, (short)this.round.UnitVert + 15, Hues.Black);

			ushort drawn = this.round.UnitsDrawn;
			this.round.UnitsDrawn = (ushort)(drawn + 1);
			this.round.SetFrame(0x74 - ((ushort)(drawn << 1)), (ushort)troopIdx);
			this.round.UnitHoriz = (ushort)(this.round.UnitHoriz + 0x12);

			if ((short)this.round.UnitsDrawn % 6 == 0)
			{
				this.round.UnitHoriz = 0x64;
				this.round.UnitVert = (ushort)(this.round.UnitVert + 0x10);
			}
		}

		/// <summary>
		/// In the turn: the support the shields cannot pay adds 5 each to 0xe3c2, the supported units (at most the size)
		/// add one each; 0xdb42 falls by the attack units away (0x6546) x (5 with a library, else 7, less the word 2 of the
		/// record of the owner's leader at 0x1516).
		/// </summary>
		private void CountSupportCosts(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			Player player = this.Owner.World.Participants[(short)CityTurn.Owner];

			if ((short)CityTurn.Shields < (short)CityTurn.SpecialSlots)
			{
				Turn.AnarchyPressure = (ushort)(Turn.AnarchyPressure + ((ushort)((short)(CityTurn.SpecialSlots - CityTurn.Shields) * 5)));
			}

			Turn.AnarchyPressure = (ushort)(Turn.AnarchyPressure + (ushort)this.Owner.Helpers.ClampValue((short)CityTurn.SpecialSlots, 0, town.TrueExtent));

			short basePressure = (short)(town.Has(CityStructure.Marketplace) ? 5 : 7);
			short factor = (short)(basePressure - LeaderWord(player.ChiefVisuals, LeaderTable.Leaning));
			Turn.DemocracyPressure = (ushort)(Turn.DemocracyPressure - ((ushort)(factor * (short)CityTurn.AttackersAway)));
		}
	}
}
