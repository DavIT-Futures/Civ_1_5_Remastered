using System;
using Civ_1_5_Remastered.GameModel.State;

namespace Civ_1_5_Remastered.Core.Game.Towns
{
	public partial class Cities
	{
		private void CountCorruptionAndTradeRoutes(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];
			short owner = (short)CityTurn.Owner;
			Player player = this.Owner.World.Participants[owner];

			this.round.Horizontal = (ushort)(short)town.Place.X;
			this.round.Vertical = (ushort)(short)town.Place.Y;
			CityTurn.Corruption = 0;

			if (player.RegimeKind == 3)
			{
				CityTurn.PalaceDistance = 0xa;
			}

			short corruption = (short)((short)((short)CityTurn.Trade * (short)CityTurn.PalaceDistance) * 3);
			CityTurn.Corruption = (ushort)(corruption / (short)(ushort)((20 * player.RegimeKind) + 0x50));

			if (town.Has(CityStructure.Courthouse) || town.Has(CityStructure.Palace))
			{
				CityTurn.Corruption = (ushort)((short)CityTurn.Corruption / 2);
			}

			if (player.RegimeKind == 5)
			{
				CityTurn.Corruption = 0;
			}

			town.RootCommerce = (sbyte)(byte)((byte)CityTurn.Trade - (byte)CityTurn.Corruption);

			for (ushort route = 0; route < 3; route++)
			{
				this.round.Item = route;
				this.round.OtherCity = (byte)town.CommerceTownIndices[route];

				if (this.round.OtherCity == 0xff)
				{
					continue;
				}

				City partner = this.Owner.World.Towns[this.round.OtherCity];
				short share = (short)(partner.RootCommerce + CityTurn.Trade + 4);
				share = (short)(partner.ParticipantIndex != owner ? share / 8 : share / 16);
				CityTurn.Trade = (ushort)(CityTurn.Trade + share);
			}

			this.round.Item = 3;
		}

		private void SplitTrade(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];
			Player player = this.Owner.World.Participants[(short)CityTurn.Owner];

			short corruption = (short)((short)((short)CityTurn.Trade * (short)CityTurn.PalaceDistance) * 3);
			CityTurn.Corruption = (ushort)(corruption / (short)(ushort)((20 * player.RegimeKind) + 0x50));

			if (player.RegimeKind == 5)
			{
				CityTurn.Corruption = 0;
			}

			if (town.Has(CityStructure.Courthouse))
			{
				CityTurn.Corruption = (ushort)((short)CityTurn.Corruption / 2);
			}

			short trade = (short)CityTurn.Trade;
			short corrupt = (short)CityTurn.Corruption;

			CityTurn.Luxuries = (ushort)this.Owner.Helpers.ClampValue(((-(player.ResearchLevyRatio + player.LevyRatio - 10) * (trade - corrupt)) + 5) / 10, 0, trade);

			CityTurn.Taxes = (ushort)this.Owner.Helpers.ClampValue(((player.LevyRatio * (trade - corrupt)) + 5) / 10, 0,
				trade - (short)CityTurn.Luxuries - corrupt);

			CityTurn.Science = (ushort)(CityTurn.Trade - CityTurn.Luxuries - CityTurn.Taxes - CityTurn.Corruption);

			CityTurn.Taxes = (ushort)(CityTurn.Taxes + (ushort)(TallySpecialists(1) << 1));
			CityTurn.Science = (ushort)(CityTurn.Science + (ushort)(TallySpecialists(2) << 1));
			CityTurn.Luxuries = (ushort)(CityTurn.Luxuries + (ushort)(TallySpecialists(3) << 1));

			foreach (ushort improvement in new ushort[] { 0x10, 0x200 })
			{
				if ((town.StructureMarkersLow & improvement) == 0)
				{
					continue;
				}

				CityTurn.Luxuries = (ushort)(CityTurn.Luxuries + ((ushort)((short)CityTurn.Luxuries / 2)));
				CityTurn.Taxes = (ushort)(CityTurn.Taxes + ((ushort)((short)CityTurn.Taxes / 2)));
			}

			if (town.PresentMakingIndex == SpecialProjects.Wealth)
			{
				int wealthGain = GetSpecialProjectConversion(town);
				CityTurn.Taxes = (ushort)(CityTurn.Taxes + wealthGain);
			}
		}

		private int GetSpecialProjectConversion(City town)
		{
			if ((town.ConditionMarker & 1) != 0)
			{
				return 0;
			}

			int netShields = System.Math.Max(0, (short)CityTurn.Shields - (short)CityTurn.UnitUpkeep);
			return netShields / 2;
		}

		private void CountScienceAndContent(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];
			short owner = (short)CityTurn.Owner;
			Player player = this.Owner.World.Participants[owner];

			this.round.Value = CityTurn.Science;

			foreach (ushort improvement in new ushort[] { 0x20, 0x800 })
			{
				if ((town.StructureMarkersLow & improvement) == 0)
				{
					continue;
				}

				this.round.Value = (ushort)(this.round.Value + ((ushort)((short)CityTurn.Science / 2)));

				if (PlayerEnjoysWonderEffect(owner, 0xc) != 0)
				{
					this.round.Value = (ushort)(this.round.Value + (CityTurn.Science / 3));
				}
			}

			if (ObtainWonderCityIdentifier(10) == (ushort)townIndex)
			{
				this.round.Value = (ushort)(this.round.Value * 2);
			}

			if (town.PresentMakingIndex == SpecialProjects.Research)
			{
				int researchGain = GetSpecialProjectConversion(town);
				this.round.Value = (ushort)(this.round.Value + researchGain);
			}

			CityTurn.Science = this.round.Value;
			CityTurn.Happy = 0;

			if (owner == this.Owner.World.PersonParticipantIndex)
			{
				short step = (short)(-((this.Owner.World.HardnessTier * 2) - 0xe) / 2 * ((player.RegimeKind / 2) + 2));

				if (step == 0)
				{
					throw new Exception("Division by zero");
				}

				short spread = (short)((short)((townIndex % step) + player.TownTally - step) / step);
				this.round.Content = (ushort)(spread + town.TrueExtent + this.Owner.World.HardnessTier - 6);
			}
			else
			{
				this.round.Content = (ushort)(town.TrueExtent - 3);
			}

			CityTurn.Unhappy = this.round.Content;

			CityTurn.ExcessUnhappy = 0;

			if (town.TrueExtent < (short)CityTurn.Unhappy)
			{
				CityTurn.ExcessUnhappy = (ushort)(CityTurn.Unhappy - town.TrueExtent);
				CityTurn.Unhappy = (ushort)town.TrueExtent;
			}

			ReshuffleCitizens(townIndex, this.round.Specialists);
		}

		private void ApplyLuxuries(short townIndex, short marker)
		{
			if (marker == 1 && CityTurn.ScreenTab == 1)
			{
				this.round.IconVertical = 0x74;
				DrawCitizenIcons(townIndex, 100, (short)this.round.IconVertical, this.round.Specialists, 92);
				this.round.IconVertical = (ushort)(this.round.IconVertical + 0x10);
				this.Owner.VideoCard.RenderLine(Screen.Box,
					100, (short)this.round.IconVertical - 2, 222, (short)this.round.IconVertical - 2, Hues.Blue);
			}

			CityTurn.Happy = (ushort)((short)CityTurn.Luxuries / 2);

			ReshuffleCitizens(townIndex, this.round.Specialists);

			if (marker == 1 && CityTurn.ScreenTab == 1 && CityTurn.Happy != 0)
			{
				DrawCitizenIcons(townIndex, 100, (short)this.round.IconVertical, this.round.Specialists, 92);
				this.Owner.Services.DrawPictureToScreen(Screen.Box, 208, (short)this.round.IconVertical + 4,
					Tables.SpritePicture(0xe));
				this.round.IconVertical = (ushort)(this.round.IconVertical + 0x10);
				this.Owner.VideoCard.RenderLine(Screen.Box,
					100, (short)this.round.IconVertical - 2, 222, (short)this.round.IconVertical - 2, Hues.Blue);
			}
		}

		private void ApplyContentImprovements(short townIndex, short marker)
		{
			City town = this.Owner.World.Towns[townIndex];
			short owner = (short)CityTurn.Owner;
			bool tab = marker == 1 && CityTurn.ScreenTab == 1;

			this.round.IconHorizontal = 0xd0;

			if (town.Has(CityStructure.Colosseum))
			{
				CityTurn.Unhappy = (ushort)(CityTurn.Unhappy - 3);

				if (tab)
				{
					DrawContentIcon(14);
				}
			}

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
					{
						DrawContentIcon(11);
					}
				}
			}

			if (town.Has(CityStructure.Temple))
			{
				if (this.Owner.Advisors.PlayerKnowsTechnology(owner, 0x1a) != 0)
				{
					CityTurn.Unhappy = (ushort)(CityTurn.Unhappy - 2);
				}
				else if (this.Owner.Advisors.PlayerKnowsTechnology(owner, 0x19) != 0)
				{
					CityTurn.Unhappy = (ushort)(CityTurn.Unhappy - 1);
				}

				if (PlayerEnjoysWonderEffect(owner, 6) != 0)
				{
					ushort relief = (ushort)(this.Owner.Advisors.PlayerKnowsTechnology(owner, 0x1a) != 0 ? 2 : 1);
					CityTurn.Unhappy = (ushort)(CityTurn.Unhappy - relief);
				}

				if (tab)
				{
					DrawContentIcon(4);
				}
			}

			ReshuffleCitizens(townIndex, this.round.Specialists);

			if (marker == 1 && CityTurn.ScreenTab == 1)
			{
				if ((town.StructureMarkersLow & 0x2408) != 0)
				{
					DrawCitizenIcons(townIndex, 100, (short)this.round.IconVertical, this.round.Specialists, 92);
					this.round.IconVertical = (ushort)(this.round.IconVertical + 0x10);
					this.Owner.VideoCard.RenderLine(Screen.Box,
						100, (short)this.round.IconVertical - 2, 222, (short)this.round.IconVertical - 2, Hues.Blue);
				}
			}
		}

		private void DrawContentIcon(short icon)
		{
			DrawResourceIcon(icon, (short)this.round.IconHorizontal, (short)this.round.IconVertical);
			this.round.IconHorizontal = (ushort)(this.round.IconHorizontal - 0x10);
		}

		private void ApplyMilitaryHappiness(short townIndex, short marker)
		{
			City town = this.Owner.World.Towns[townIndex];
			short owner = (short)CityTurn.Owner;
			Player player = this.Owner.World.Participants[owner];
			bool tab = marker == 1 && CityTurn.ScreenTab == 1;

			this.round.IconHorizontal = 0xd0;

			if (player.RegimeKind < 4)
			{
				this.round.MartialLawUnits = 0;
				this.round.UnitsAway = 0;

				this.round.Value = this.Owner.MapOperations.ObtainUnitAtTile((short)this.round.Horizontal, (short)this.round.Vertical);
				this.round.NewUnit = this.round.Value;

				while (this.round.NewUnit != 0xffff)
				{
					this.round.MartialLawUnits = (ushort)(this.round.MartialLawUnits + 1);

					if ((short)this.round.MartialLawUnits >= 0x20)
					{
						break;
					}

					short troopIndex = (short)this.round.NewUnit;

					if (this.troops.KindField(owner, troopIndex, UnitKindTable.AssaultPower) != 0)
					{
						this.round.UnitsAway = (ushort)(this.round.UnitsAway + 1);

						if (tab && (short)this.round.UnitsAway <= 3)
						{
							this.Owner.Services.DrawPictureToScreen(Screen.Box, (short)this.round.IconHorizontal,
								(short)this.round.IconVertical - 1,
								Tables.SpritePicture(this.troops.Pull(owner, troopIndex, UnitMemory.Kind) + (owner << 5) + 0x40));
							this.round.IconHorizontal = (ushort)(this.round.IconHorizontal - 2);
						}
					}

					this.round.NewUnit = (ushort)this.troops.PullSigned(owner, troopIndex, UnitMemory.FollowingInStack);

					if (this.round.NewUnit == this.round.Value)
					{
						this.round.NewUnit = 0xffff;
					}
				}

				for (ushort slot = 0; slot < 2; slot++)
				{
					this.round.NewUnit = slot;

					if (CityTurn.Unhappy == 0)
					{
						continue;
					}

					if ((byte)town.Unfamiliar[slot] == 0xff)
					{
						continue;
					}

					if (Tables.UnitKindWord(town.Unfamiliar[slot] & 0x3f, UnitKindTable.AssaultPower) != 0)
					{
						this.round.UnitsAway = (ushort)(this.round.UnitsAway + 1);
					}
				}

				this.round.NewUnit = 2;

				if ((short)this.round.UnitsAway > 3)
				{
					this.round.UnitsAway = 3;
				}

				CityTurn.Unhappy = (ushort)(CityTurn.Unhappy - (ushort)this.Owner.Helpers.ClampValue((short)this.round.UnitsAway, 0, (short)CityTurn.Unhappy));
			}
			else
			{
				this.round.Value = (ushort)(PlayerEnjoysWonderEffect(owner, 16) < 1 ? 1 : 0);

				if (player.RegimeKind == 5)
				{
					this.round.Value = (ushort)(this.round.Value + 1);
				}

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
							this.Owner.Services.DrawPictureToScreen(Screen.Box, (short)this.round.IconHorizontal,
								(short)this.round.IconVertical + 4, Tables.SpritePicture(0xd));
							this.round.IconHorizontal = (ushort)(this.round.IconHorizontal - 2);
						}

						this.round.NewUnit = drawn;
					}
				}
			}

			ReshuffleCitizens(townIndex, this.round.Specialists);

			if (marker == 1 && CityTurn.ScreenTab == 1)
			{
				DrawCitizenIcons(townIndex, 100, (short)this.round.IconVertical, this.round.Specialists, 92);
				this.round.IconVertical = (ushort)(this.round.IconVertical + 0x10);
				this.Owner.VideoCard.RenderLine(Screen.Box,
					100, (short)this.round.IconVertical - 2, 222, (short)this.round.IconVertical - 2, Hues.Blue);
			}
		}

		private void ApplyWonderHappiness(short townIndex, short marker)
		{
			short owner = (short)CityTurn.Owner;

			this.round.Value = (ushort)(CityTurn.Happy - CityTurn.Unhappy);

			if (PlayerEnjoysWonderEffect(owner, 2) != 0)
			{
				CityTurn.Happy = (ushort)(CityTurn.Happy + 1);
			}

			if (PlayerEnjoysWonderEffect(owner, 0x15) != 0)
			{
				CityTurn.Happy = (ushort)(CityTurn.Happy + 1);
			}

			if (ObtainWonderCityIdentifier(11) == (ushort)townIndex)
			{
				CityTurn.Unhappy = 0;
			}

			if (PlayerEnjoysWonderEffect(owner, 0xd) != 0)
			{
				short marvelIndex = this.Owner.World.MarvelTownIndex[13];
				ushort marvelContinent = this.Owner.MapOperations.ObtainContinentIdentifier(this.Owner.World.Towns[marvelIndex].Place.X, this.Owner.World.Towns[marvelIndex].Place.Y);
				if (this.Owner.MapOperations.ObtainContinentIdentifier((short)this.round.Horizontal, (short)this.round.Vertical) == marvelContinent)
				{
					CityTurn.Unhappy = (ushort)(CityTurn.Unhappy - 2);
				}
			}

			ReshuffleCitizens(townIndex, this.round.Specialists);

			if (marker == 1 && CityTurn.ScreenTab == 1 &&
				(ushort)(CityTurn.Happy - CityTurn.Unhappy) != this.round.Value)
			{
				DrawCitizenIcons(townIndex, 100, (short)this.round.IconVertical, this.round.Specialists, 92);
				this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTexts.TextWonders, 190, (short)this.round.IconVertical + 5, Hues.White);
				this.round.IconVertical = (ushort)(this.round.IconVertical + 0x10);
				this.Owner.VideoCard.RenderLine(Screen.Box,
					100, (short)this.round.IconVertical - 2, 222, (short)this.round.IconVertical - 2, Hues.Blue);
			}

			if ((this.Owner.World.DiagonalMarkers & 2) == 0)
			{
				CityTurn.Unhappy = 0;
				CityTurn.Happy = 0;
			}
		}

		private void StartUnitSupport(short townIndex)
		{
			this.round.UnitsDrawn = 0;
			CityTurn.SpecialSlots = 0;
			Turn.UnitSupportCount = 0;
			CityTurn.UnitUpkeep = 0;

			CountSpecialSlots(townIndex);

			this.round.DrawHorizontal = 8;
			this.round.DrawVertical = 0x45;
			this.round.UnitHorizontal = 0x64;
			this.round.UnitVertical = 0x74;
		}

		private bool SupportUnit(short townIndex, short marker)
		{
			City town = this.Owner.World.Towns[townIndex];
			short owner = (short)CityTurn.Owner;
			short troopIndex = (short)this.round.Index;

			if (this.troops.Pull(owner, troopIndex, UnitMemory.Kind) != 0xff && this.troops.PullSigned(owner, troopIndex, UnitMemory.BaseTown) == townIndex)
			{
				if (this.troops.PullSigned(owner, troopIndex, UnitMemory.Kind) < 0x1a)
				{
					CityTurn.SpecialSlots = (ushort)(CityTurn.SpecialSlots + 1);

					if (town.TrueExtent < (short)CityTurn.SpecialSlots ||
						(this.Owner.World.Participants[owner].RegimeKind > 1 && (this.Owner.World.DiagonalMarkers & 2) != 0))
					{
						CityTurn.UnitUpkeep = (ushort)(CityTurn.UnitUpkeep + 1);
					}
				}

				if (marker == 0 && DisbandUnsupportedUnit(townIndex))
				{
					return true;
				}

				if (marker == 1)
				{
					DrawSupportedUnit(townIndex);
				}
			}

			DrawUnitInCity(marker);
			return false;
		}

		private bool DisbandUnsupportedUnit(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];
			short owner = (short)CityTurn.Owner;
			short person = this.Owner.World.PersonParticipantIndex;
			bool enoughShields = (short)CityTurn.Shields >= (short)CityTurn.UnitUpkeep;

			if (enoughShields)
			{
				if ((town.ConditionMarker & 1) == 0 || ((byte)((byte)this.round.Index + (byte)(this.Owner.World.RoundTally & 0xff)) & 7) != 0 ||
					owner == person || this.Owner.World.Participants[owner].RegimeKind < 4)
				{
					return false;
				}
			}

			this.round.Found = 0xffff;
			this.round.Best = 0xffff;

			for (ushort otherIndex = 0; otherIndex < 0x80; otherIndex++)
			{
				this.round.Selected = otherIndex;
				short other = (short)otherIndex;

				if (this.troops.Pull(owner, other, UnitMemory.Kind) == 0xff || this.troops.PullSigned(owner, other, UnitMemory.BaseTown) != townIndex ||
					this.troops.PullSigned(owner, other, UnitMemory.Kind) >= 0x1a)
				{
					continue;
				}

				this.round.Distance = (ushort)this.Owner.Helpers.ObtainDistance(town.Place.X, this.troops.PullSigned(owner, other, UnitMemory.Horizontal),
					town.Place.Y, this.troops.PullSigned(owner, other, UnitMemory.Vertical));

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
						this.Owner.UnitOperations.DestroyUnit(owner, farthest);
						town.ConditionMarker &= 0xfe;
					}
				}

				return false;
			}

			if (owner == person)
			{
				EmptyMessage()
					.City(townIndex)
					.Text(" can't support\n");
				this.Owner.Words.Append((ushort)((this.troops.TroopKind(owner, farthest) * UnitKindTable.Stride) + UnitKindTable.Origin));
				this.Owner.Words.Append(".\n Unit Disbanded.\n");
				Screen.DialogStyle = 3;
				this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);
			}

			this.Owner.UnitOperations.DestroyUnit(owner, (short)this.round.Found);
			return true;
		}

		private void DrawSupportedUnit(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];
			short owner = (short)CityTurn.Owner;
			short troopIndex = (short)this.round.Index;
			Player player = this.Owner.World.Participants[owner];

			if (this.troops.PullSigned(owner, troopIndex, UnitMemory.Kind) < 0x1a)
			{
				if (CityTurn.UnitUpkeep != 0)
				{
					this.Owner.Services.DrawPictureToScreen(Screen.Box, (short)this.round.DrawHorizontal + 8,
						(short)this.round.DrawVertical + 12, Tables.SpritePicture(0x9));
				}

				if (this.troops.Pull(owner, troopIndex, UnitMemory.Kind) == 0)
				{
					this.Owner.Services.DrawPictureToScreen(Screen.Box, (short)this.round.DrawHorizontal,
						(short)this.round.DrawVertical + 12, Tables.SpritePicture(0x8));

					if (player.RegimeKind >= 2)
					{
						this.Owner.Services.DrawPictureToScreen(Screen.Box, (short)this.round.DrawHorizontal + 2,
							(short)this.round.DrawVertical + 12, Tables.SpritePicture(0x8));
					}
				}
				else
				{
					this.round.Value = (ushort)(PlayerEnjoysWonderEffect(owner, 16) < 1 ? 1 : 0);

					if (player.RegimeKind == 5)
					{
						this.round.Value = (ushort)(this.round.Value + 1);
					}

					if (player.RegimeKind >= 4 && this.round.Value != 0 &&
						MilitaryUnhappinessRules.CausesUnhappinessAway(this.troops.Pull(owner, troopIndex, UnitMemory.Kind), this.troops.KindField(owner, troopIndex, UnitKindTable.AssaultPower)) &&
						(this.troops.KindField(owner, troopIndex, UnitKindTable.LandGroup) == UnitKindTable.AirUnits ||
							this.troops.Pull(owner, troopIndex, UnitMemory.Horizontal) != (byte)(sbyte)town.Place.X ||
							this.troops.Pull(owner, troopIndex, UnitMemory.Vertical) != (byte)(sbyte)town.Place.Y))
					{
						this.Owner.Services.DrawPictureToScreen(Screen.Box, (short)this.round.DrawHorizontal,
							(short)this.round.DrawVertical + 12, Tables.SpritePicture(0xd));

						if ((short)this.round.Value > 1)
						{
							this.Owner.Services.DrawPictureToScreen(Screen.Box, (short)this.round.DrawHorizontal + 2,
								(short)this.round.DrawVertical + 12, Tables.SpritePicture(0xd));
						}
					}
				}
			}

			this.Owner.Services.DrawPictureToScreen(Screen.Box, (short)this.round.DrawHorizontal, (short)this.round.DrawVertical,
				Tables.SpritePicture(this.troops.Pull(owner, troopIndex, UnitMemory.Kind) + (owner << 5) + 0x40));

			DrawMiniMapDot(this.troops.Pull(owner, troopIndex, UnitMemory.Horizontal), this.troops.Pull(owner, troopIndex, UnitMemory.Vertical), 7);

			this.round.DrawHorizontal = (ushort)(this.round.DrawHorizontal + 0x10);

			if ((short)this.round.DrawHorizontal >= 0x70)
			{
				this.round.DrawHorizontal = 8;
				this.round.DrawVertical = (ushort)(this.round.DrawVertical + 0x10);

				if ((short)this.round.DrawVertical > 0x55)
				{
					this.round.DrawVertical = (ushort)(this.round.DrawVertical - 0x18);
				}
			}
		}

		private void DrawUnitInCity(short marker)
		{
			short owner = (short)CityTurn.Owner;
			short troopIndex = (short)this.round.Index;

			if ((short)this.round.UnitsDrawn >= 0x12 || CityTurn.ScreenTab != 0)
			{
				return;
			}

			if (this.troops.Pull(owner, troopIndex, UnitMemory.Kind) == 0xff || marker != 1 ||
				this.troops.Pull(owner, troopIndex, UnitMemory.Horizontal) != this.round.Horizontal ||
				this.troops.Pull(owner, troopIndex, UnitMemory.Vertical) != this.round.Vertical)
			{
				return;
			}

			this.Owner.MapOperations.DrawUnitSprite(owner, troopIndex, (short)this.round.UnitHorizontal, (short)this.round.UnitVertical);

			this.Owner.Words.Clear();
			this.Owner.Conquest.ObtainCityLabel(this.troops.PullSigned(owner, troopIndex, UnitMemory.BaseTown));
			this.Owner.Words[3] = 0x2e;
			this.Owner.Words[4] = 0;
			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), (short)this.round.UnitHorizontal, (short)this.round.UnitVertical + 15, Hues.Black);

			ushort drawn = this.round.UnitsDrawn;
			this.round.UnitsDrawn = (ushort)(drawn + 1);
			this.round.SetFrame(0x74 - ((ushort)(drawn << 1)), (ushort)troopIndex);
			this.round.UnitHorizontal = (ushort)(this.round.UnitHorizontal + 0x12);

			if ((short)this.round.UnitsDrawn % 6 == 0)
			{
				this.round.UnitHorizontal = 0x64;
				this.round.UnitVertical = (ushort)(this.round.UnitVertical + 0x10);
			}
		}

		private void CountSupportCosts(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];
			Player player = this.Owner.World.Participants[(short)CityTurn.Owner];

			if ((short)CityTurn.Shields < (short)CityTurn.SpecialSlots)
			{
				Turn.AnarchyPressure = (ushort)(Turn.AnarchyPressure + ((ushort)((short)(CityTurn.SpecialSlots - CityTurn.Shields) * 5)));
			}

			Turn.AnarchyPressure = (ushort)(Turn.AnarchyPressure + (ushort)this.Owner.Helpers.ClampValue((short)CityTurn.SpecialSlots, 0, town.TrueExtent));

			short basePressure = (short)(town.Has(CityStructure.Marketplace) ? 5 : 7);
			short factor = (short)(basePressure - Tables.LeaderWord(player.ChiefVisuals, LeaderTable.Leaning));
			Turn.DemocracyPressure = (ushort)(Turn.DemocracyPressure - ((ushort)(factor * (short)CityTurn.AttackersAway)));
		}
	}
}
