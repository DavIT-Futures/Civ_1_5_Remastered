using System;
using System.Collections.Generic;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Pure C# domain logic and game UI integration for government changes, revolutions, and diplomacy.
	/// Translated from original CIV.EXE segment 0x2517.
	/// </summary>
	public class GovernmentAndDiplomacy
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public GovernmentAndDiplomacy(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// Evaluates periodic peace treaty maintenance, expires stale wars, and triggers AI peace proposals.
		/// </summary>
		public void HandlePeaceTreaties(short participantIdx)
		{
			this.processor.Journal.OpenScope($"HandlePeaceTreaties({participantIdx})");

			if (this.owner.World.Participants[participantIdx].RegimeKind == 0)
			{
				if (participantIdx == this.owner.World.PersonParticipantIdx)
				{
					if ((this.owner.World.RoundTally & 3) == 0)
					{
						if ((this.owner.World.Participants[participantIdx].Relations[0] & 4) != 0)
						{
							this.owner.World.Participants[participantIdx].Relations[0] &= 0xfffb;
						}
						else
						{
							SwitchGovernment(participantIdx, 1);
						}
					}
				}
				else
				{
					if ((this.owner.World.RoundTally & 3) != 0)
					{
						this.owner.CityOps.PlayerEnjoysWonderEffect(participantIdx, 1);
						if (this.processor.RegAcc.Value16 != 0)
						{
							SwitchGovernment(participantIdx, 1);
						}
					}
					else
					{
						SwitchGovernment(participantIdx, 1);
						if (participantIdx == this.owner.World.PersonParticipantIdx)
						{
							this.owner.World.Participants[participantIdx].Relations[0] &= 0xfffb;
						}
					}
				}
			}

			if (participantIdx != this.owner.World.PersonParticipantIdx)
			{
				if ((this.owner.World.Participants[participantIdx].Relations[0] & 4) != 0)
				{
					SwitchGovernment(participantIdx, this.owner.World.Participants[participantIdx].RegimeKind);
				}
			}

			for (short other = 1; other < 8; other++)
			{
				if ((this.owner.World.Participants[participantIdx].Relations[other] & 0x100) != 0)
				{
					if (this.owner.Libc.Dice.DrawInt(3) == 0)
					{
						this.owner.World.Participants[participantIdx].Relations[other] &= 0xfefd;
						this.owner.World.Participants[participantIdx].Relations[other] |= 0x200;
					}
				}

				if ((this.owner.World.RoundTally & 0xf) == 0)
				{
					if ((this.owner.World.Participants[participantIdx].Relations[other] & 0x10) == 0)
					{
						if ((this.owner.World.Participants[participantIdx].Relations[other] & 1) != 0 &&
							(this.owner.World.Participants[participantIdx].Relations[other] & 2) == 0)
						{
							short human = this.owner.World.PersonParticipantIdx;
							bool humanKnowsP1 = (this.owner.World.Participants[human].Relations[participantIdx] & 0x40) != 0;
							bool humanKnowsP2 = (this.owner.World.Participants[human].Relations[other] & 0x40) != 0;

							if ((humanKnowsP1 || humanKnowsP2) && participantIdx != human && other != human)
							{
								this.owner.Libc.CopyText(0xba06, "The war between\nthe ");
								ushort p1NamePtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((participantIdx << 1) + 0x1992));
								this.owner.Libc.AppendText(0xba06, p1NamePtr);
								this.owner.Libc.AppendText(0xba06, " and the\n");
								ushort p2NamePtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((other << 1) + 0x1992));
								this.owner.Libc.AppendText(0xba06, p2NamePtr);
								this.owner.Libc.AppendText(0xba06, " has ended.\n");
								this.processor.EmitGlobalWord(0x2f9e, 1);
								this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
							}

							this.owner.World.Participants[participantIdx].Relations[other] &= 0xfffe;
							this.owner.World.Participants[other].Relations[participantIdx] &= 0xfffe;
						}
					}

					this.owner.World.Participants[participantIdx].Relations[other] &= 0xffef;
					this.owner.World.Participants[other].Relations[participantIdx] &= 0xffef;
				}
			}

			if (participantIdx == 0)
			{
				this.processor.Journal.CloseScope("HandlePeaceTreaties");
				return;
			}

			if ((this.owner.World.RoundTally & 0x1f) != (participantIdx << 2))
			{
				this.processor.Journal.CloseScope("HandlePeaceTreaties");
				return;
			}

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.processor.Journal.CloseScope("HandlePeaceTreaties");
				return;
			}

			for (short other = 1; other < 8; other++)
			{
				if ((this.owner.World.Participants[participantIdx].Relations[other] & 0x20) == 0)
					continue;

				short human = this.owner.World.PersonParticipantIdx;
				if ((this.owner.World.Participants[human].Relations[other] & 2) == 0)
				{
					this.owner.World.Participants[participantIdx].Relations[other] &= 0xffdf;
					continue;
				}

				if ((this.owner.World.Participants[participantIdx].Relations[human] & 0x20) != 0)
				{
					this.owner.World.Participants[participantIdx].Relations[other] &= 0xffdf;
					continue;
				}

				int p1Might = this.owner.World.Participants[participantIdx].MartialMight;
				int otherMight = this.owner.World.Participants[other].MartialMight;

				if ((p1Might << 1) < otherMight)
				{
					this.owner.Audience.WelcomeEmissary(participantIdx, -1, 0);
					if (this.processor.RegAcc.Value16 == 0)
					{
						int goldOffer = Math.Clamp(this.owner.World.Participants[participantIdx].Gold / 100, 1, 10) * 50;
						ushort otherCivNamePtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((other << 1) + 0x1992));
						this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30ba), otherCivNamePtr);

						this.processor.EmitGlobalByte(0xba06, 0);
						this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((short)goldOffer, 10));
						this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30bc), 0xba06);

						this.processor.EmitGlobalByte(0xba06, 0);
						this.owner.BlockText.DisplayTextDialog(0x2988);
						this.processor.EmitGlobalWord(0x3936, 1);
						this.processor.EmitGlobalWord(0xd76c, 0xc);

						this.owner.Audience.DisplayKingDialog(0xba06, 0x14, 0x8b);
						if (this.processor.RegAcc.Value16 == 1)
						{
							this.owner.World.Participants[human].Gold += (short)goldOffer;
							this.owner.World.Participants[participantIdx].Gold += (short)goldOffer;

							AssignDiplomacyFlags(participantIdx, human, 2);
							WipeDiplomacyFlags(other, human, 2);
							this.owner.World.Participants[other].Relations[human] |= 8;
						}
						this.owner.Audience.FinishAudience();
					}
				}

				if (otherMight > p1Might && (p1Might << 1) >= otherMight)
				{
					this.owner.Audience.WelcomeEmissary(participantIdx, -1, 0);
					if (this.processor.RegAcc.Value16 == 0)
					{
						this.processor.EmitGlobalByte(0xba06, 0);
						ushort p1Name = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((participantIdx << 1) + 0x1992));
						this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30ba), p1Name);

						ushort textDialogId = ((this.owner.World.Participants[participantIdx].Relations[human] & 2) != 0) ? (ushort)0x2990 : (ushort)0x299b;
						this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30be), textDialogId);
						this.owner.BlockText.DisplayTextDialog(0x29a6);
						this.processor.EmitGlobalWord(0x3936, 1);
						this.processor.EmitGlobalWord(0xd76c, 0xc);

						this.owner.Audience.DisplayKingDialog(0xba06, 0x14, 0x8b);
						if (this.processor.RegAcc.Value16 == 1)
						{
							AssignDiplomacyFlags(participantIdx, human, 2);
							WipeDiplomacyFlags(other, human, 2);
							this.owner.World.Participants[other].Relations[human] |= 8;
							this.owner.World.Participants[participantIdx].TouchParticipantTimeleft = (short)(this.owner.World.RoundTally + 16);
						}
						this.owner.Audience.FinishAudience();
					}
				}

				this.owner.World.Participants[participantIdx].Relations[other] &= 0xffdf;
			}

			this.processor.Journal.CloseScope("HandlePeaceTreaties");
		}

		/// <summary>
		/// Changes a player's government regime, validating prerequisites and updating taxation.
		/// </summary>
		public void SwitchGovernment(short participantIdx, short regimeKind)
		{
			this.processor.Journal.OpenScope($"SwitchGovernment({participantIdx}, {regimeKind})");

			short originalRegime = this.owner.World.Participants[participantIdx].RegimeKind;
			bool isHumanControlled = (this.owner.World.ParticipantMarkers & (1 << participantIdx)) != 0;

			if (isHumanControlled)
			{
				this.owner.Libc.CopyText(0xba06, "Select type of\nGovernment...\n ");

				List<short> availableRegimes = new List<short>();
				this.owner.CityOps.PlayerEnjoysWonderEffect(participantIdx, 1);
				bool hasPyramids = (this.processor.RegAcc.Value16 != 0);

				for (short r = 1; r <= 5; r++)
				{
					if (GovernmentRules.CanAdoptGovernment(r, tech => this.owner.Advisors.PlayerKnowsTechnology(participantIdx, tech) != 0, hasPyramids))
					{
						availableRegimes.Add(r);
						ushort namePtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((r << 1) + 0x1966));
						this.owner.Libc.AppendText(0xba06, namePtr);
						this.owner.Libc.AppendText(0xba06, "\n ");
					}
				}

				if (regimeKind != 0)
				{
					int selectedIdx = -1;
					while (selectedIdx < 0 || selectedIdx >= availableRegimes.Count)
					{
						this.owner.TurnHooks.DisplayDialog(0xba06, 100, 64);
						selectedIdx = (short)this.processor.RegAcc.Value16;
					}
					regimeKind = availableRegimes[selectedIdx];
				}

				this.owner.World.Participants[participantIdx].RegimeKind = regimeKind;
				this.owner.NewGameWizard.ImportGovernmentGraphics();
				this.owner.TurnHooks.RepaintMainScreen();
				this.owner.NewGameWizard.DisplayRevolutionScreen();
			}
			else
			{
				// AI government change
				bool sameRegime = (this.owner.World.Participants[participantIdx].RegimeKind == regimeKind);
				bool relationFlag4NotSet = (this.owner.World.Participants[participantIdx].Relations[0] & 4) == 0;

				if (!(sameRegime && relationFlag4NotSet))
				{
					// If human has contact with this AI, display notification dialog
					var human = this.owner.World.Participants[this.owner.World.PersonParticipantIdx];
					if ((human.Relations[participantIdx] & 0x40) != 0)
					{
						this.processor.EmitGlobalByte(0xba06, 0x0);
						if ((this.owner.World.Participants[participantIdx].Relations[0] & 4) != 0)
						{
							ushort civNamePtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((participantIdx << 1) + 0x1982));
							this.owner.Libc.AppendText(0xba06, civNamePtr);
							this.owner.Libc.AppendText(0xba06, " government\nchanged to ");
							ushort regimeNamePtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((regimeKind << 1) + 0x1966));
							this.owner.Libc.AppendText(0xba06, regimeNamePtr);
							this.owner.Libc.AppendText(0xba06, "!\n");
							this.owner.World.Participants[participantIdx].Relations[0] &= 0xfffb;
						}
						else
						{
							this.owner.Libc.AppendText(0xba06, "The ");
							ushort civNamePtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((participantIdx << 1) + 0x1982));
							this.owner.Libc.AppendText(0xba06, civNamePtr);
							this.owner.Libc.AppendText(0xba06, " government\nhas been overthrown!\n");
							this.owner.World.Participants[participantIdx].Relations[0] |= 4;
						}

						this.processor.EmitGlobalWord(0x2f9e, 0x1);
						this.owner.TurnHooks.DisplayDialog(0xba06, 80, 64);
					}
				}

				this.owner.World.Participants[participantIdx].RegimeKind = regimeKind;
				this.owner.World.Participants[participantIdx].LevyRatio = GovernmentRules.ComputeDefaultLevyRatio(regimeKind);
			}

			if (this.owner.World.Participants[participantIdx].RegimeKind != originalRegime)
			{
				this.owner.UnitOps.AttachReplayContent(8, (byte)participantIdx, (byte)this.owner.World.Participants[participantIdx].RegimeKind);

				for (short other = 0; other < 8; other++)
				{
					this.owner.World.Participants[participantIdx].Relations[other] &= 0xfff7;
				}
			}

			this.processor.Journal.CloseScope("SwitchGovernment");
		}

		/// <summary>
		/// Conducts peace negotiations and audiences between two civilizations.
		/// </summary>
		public void BargainPeaceTreaty(short participantIdx1, short participantIdx2, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"BargainPeaceTreaty({participantIdx1}, {participantIdx2}, {horizLoc}, {vertLoc})");

			if (participantIdx1 == 0 || participantIdx2 == 0)
			{
				this.processor.Journal.CloseScope("BargainPeaceTreaty");
				return;
			}

			short human = this.owner.World.PersonParticipantIdx;
			if (participantIdx1 == human)
			{
				if ((this.owner.World.Participants[participantIdx1].Relations[participantIdx2] & 1) == 0 ||
					(this.owner.World.RoundTally - this.owner.World.Participants[participantIdx2].TouchParticipantTimeleft >= 0x10))
				{
					this.owner.Audience.LaunchAudience(participantIdx2, horizLoc, vertLoc, 0);
				}
			}

			if (participantIdx2 == human)
			{
				if ((this.owner.World.Participants[participantIdx1].Relations[participantIdx2] & 1) == 0 ||
					(this.owner.World.RoundTally - this.owner.World.Participants[participantIdx1].TouchParticipantTimeleft >= 0x10))
				{
					this.owner.Audience.LaunchAudience(participantIdx1, horizLoc, vertLoc, 0);
				}
			}

			this.owner.World.Participants[participantIdx1].Relations[participantIdx2] |= 0x11;
			this.owner.World.Participants[participantIdx2].Relations[participantIdx1] |= 0x11;

			if (participantIdx1 == human || participantIdx2 == human)
			{
				this.processor.Journal.CloseScope("BargainPeaceTreaty");
				return;
			}

			// AI vs AI peace negotiation
			if (((participantIdx1 + participantIdx2 + (this.owner.World.RoundTally & 0xff)) & 3) != 0 &&
				(this.owner.World.Participants[participantIdx1].Relations[participantIdx2] & 1) != 0)
			{
				this.processor.Journal.CloseScope("BargainPeaceTreaty");
				return;
			}

			if ((this.owner.World.Participants[participantIdx1].Relations[participantIdx2] & 8) != 0 ||
				(this.owner.World.Participants[participantIdx2].Relations[participantIdx1] & 8) != 0)
			{
				this.processor.Journal.CloseScope("BargainPeaceTreaty");
				return;
			}

			bool p1Agrees = (AIWeighsTreaty(participantIdx1, participantIdx2) != 0);
			bool p2Agrees = (AIWeighsTreaty(participantIdx2, participantIdx1) != 0);

			bool p1HasWonder7 = false;
			this.owner.CityOps.PlayerEnjoysWonderEffect(participantIdx1, 7);
			if (this.processor.RegAcc.Value16 != 0) p1HasWonder7 = true;

			bool p2HasWonder7 = false;
			this.owner.CityOps.PlayerEnjoysWonderEffect(participantIdx2, 7);
			if (this.processor.RegAcc.Value16 != 0) p2HasWonder7 = true;

			bool bothAgree = true;
			if (!p1Agrees && !p2HasWonder7 && this.owner.World.Participants[participantIdx1].RegimeKind < 4)
			{
				bothAgree = false;
			}
			else if (!p2Agrees && !p1HasWonder7 && this.owner.World.Participants[participantIdx2].RegimeKind < 4)
			{
				bothAgree = false;
			}

			if (bothAgree)
			{
				SwapTechnology(participantIdx1, participantIdx2);

				if ((this.owner.World.Participants[participantIdx1].Relations[participantIdx2] & 2) == 0)
				{
					bool humanKnowsP1 = (this.owner.World.Participants[human].Relations[participantIdx1] & 0x40) != 0;
					bool humanKnowsP2 = (this.owner.World.Participants[human].Relations[participantIdx2] & 0x40) != 0;

					if (humanKnowsP1 || humanKnowsP2)
					{
						this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((participantIdx1 << 1) + 0x1992)));
						this.owner.Libc.AppendText(0xba06, " and\n");
						this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((participantIdx2 << 1) + 0x1992)));
						this.owner.Libc.AppendText(0xba06, " sign\na peace treaty.\n");
						this.processor.EmitGlobalWord(0x2f9e, 1);
						this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
					}

					AssignDiplomacyFlags(participantIdx1, participantIdx2, 2);
				}
			}
			else
			{
				// Treaty refused or broken
				if ((this.owner.World.Participants[participantIdx1].Relations[participantIdx2] & 2) != 0 ||
					(this.owner.World.Participants[participantIdx1].Relations[participantIdx2] & 1) == 0)
				{
					bool humanKnowsP1 = (this.owner.World.Participants[human].Relations[participantIdx1] & 0x40) != 0;
					bool humanKnowsP2 = (this.owner.World.Participants[human].Relations[participantIdx2] & 0x40) != 0;

					if (humanKnowsP1 || humanKnowsP2)
					{
						this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((participantIdx1 << 1) + 0x1992)));
						this.owner.Libc.AppendText(0xba06, " and\n");
						this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((participantIdx2 << 1) + 0x1992)));

						if ((this.owner.World.Participants[participantIdx1].Relations[participantIdx2] & 2) != 0)
						{
							this.owner.Libc.AppendText(0xba06, 0x2a36);
						}
						else
						{
							this.owner.Libc.AppendText(0xba06, 0x2a4d);
						}

						this.processor.EmitGlobalWord(0x2f9e, 1);
						this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
					}

					WipeDiplomacyFlags(participantIdx1, participantIdx2, 2);
				}
			}

			this.processor.Journal.CloseScope("BargainPeaceTreaty");
		}

		/// <summary>
		/// Sets diplomatic relation bit flags between two civilizations.
		/// </summary>
		public void AssignDiplomacyFlags(short participantIdx1, short participantIdx2, ushort markers)
		{
			this.processor.Journal.OpenScope($"AssignDiplomacyFlags({participantIdx1}, {participantIdx2}, {markers})");

			if (markers == 2 &&
				(this.owner.World.Participants[participantIdx1].Relations[participantIdx2] & markers) == 0)
			{
				this.owner.UnitOps.AttachReplayContent(3, (byte)(((ushort)participantIdx1 << 4) | (ushort)participantIdx2));
			}

			this.owner.World.Participants[participantIdx1].Relations[participantIdx2] |= markers;
			this.owner.World.Participants[participantIdx2].Relations[participantIdx1] |= markers;

			if (markers == 2)
			{
				this.owner.World.Participants[participantIdx1].Relations[participantIdx2] &= 0xffdf;
				this.owner.World.Participants[participantIdx2].Relations[participantIdx1] &= 0xffdf;
			}

			this.processor.Journal.CloseScope("AssignDiplomacyFlags");
		}

		/// <summary>
		/// Clears diplomatic relation bit flags between two civilizations.
		/// </summary>
		public void WipeDiplomacyFlags(short participantIdx1, short participantIdx2, ushort markers)
		{
			this.processor.Journal.OpenScope($"WipeDiplomacyFlags({participantIdx1}, {participantIdx2}, {markers})");

			if (markers == 2 &&
				(this.owner.World.Participants[participantIdx1].Relations[participantIdx2] & markers) != 0)
			{
				this.owner.UnitOps.AttachReplayContent(2, (byte)((short)(participantIdx1 << 4) | participantIdx2));
			}

			this.owner.World.Participants[participantIdx1].Relations[participantIdx2] &= (ushort)(~markers);
			this.owner.World.Participants[participantIdx2].Relations[participantIdx1] &= (ushort)(~markers);

			if ((markers & 2) != 0)
			{
				this.owner.World.Participants[participantIdx1].Relations[participantIdx2] &= 0xfffb;
				this.owner.World.Participants[participantIdx2].Relations[participantIdx1] &= 0xfffb;
			}

			this.processor.Journal.CloseScope("WipeDiplomacyFlags");
		}

		/// <summary>
		/// Evaluates whether AI player 1 agrees to a peace treaty with player 2.
		/// </summary>
		public ushort AIWeighsTreaty(short participantIdx1, short participantIdx2)
		{
			this.processor.Journal.OpenScope($"AIWeighsTreaty({participantIdx1}, {participantIdx2})");

			bool diagBit0 = (this.owner.World.DiagMarkers & 1) != 0;
			short humanIdx = this.owner.World.PersonParticipantIdx;
			var p1 = this.owner.World.Participants[participantIdx1];
			var p2 = this.owner.World.Participants[participantIdx2];
			var human = this.owner.World.Participants[humanIdx];

			ushort p1HumanRel = p1.Relations[humanIdx];
			ushort p2HumanRel = p2.Relations[humanIdx];

			int p1VisualOffset = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(p1.ChiefVisuals * 0x3a + 0x1512));

			bool agrees = GovernmentRules.EvaluatePeaceTreaty(
				diagBit0,
				humanIdx,
				participantIdx1,
				participantIdx2,
				p1HumanRel,
				p2HumanRel,
				p1.MartialMight,
				p2.MartialMight,
				human.MartialMight,
				p1.LiveTroops[25],
				p2.LiveTroops[25],
				p1VisualOffset,
				other => p1.Relations[other],
				lm => (p1.Landmasses[lm].TownTally, p1.Landmasses[lm].Guard, p1.Landmasses[lm].Assault),
				lm => (p2.Landmasses[lm].Guard, p2.Landmasses[lm].Assault));

			this.processor.RegAcc.Value16 = (ushort)(agrees ? 1 : 0);
			this.processor.Journal.CloseScope("AIWeighsTreaty");
			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Exchanges technologies between two civilizations.
		/// </summary>
		public void SwapTechnology(short participantIdx1, short participantIdx2)
		{
			this.processor.Journal.OpenScope($"SwapTechnology({participantIdx1}, {participantIdx2})");

			short techForP1 = -1;
			short techForP2 = -1;

			for (short tech = 0; tech < 72; tech++)
			{
				bool p1Knows = (this.owner.Advisors.PlayerKnowsTechnology(participantIdx1, tech) != 0);
				bool p2Knows = (this.owner.Advisors.PlayerKnowsTechnology(participantIdx2, tech) != 0);

				if (!p1Knows && p2Knows)
				{
					techForP1 = tech;
				}
				if (!p2Knows && p1Knows)
				{
					techForP2 = tech;
				}
			}

			if (techForP1 != -1 && techForP2 != -1)
			{
				this.owner.Advisors.EarnTechnology(participantIdx1, techForP1, participantIdx2);
				this.owner.Advisors.EarnTechnology(participantIdx2, techForP2, participantIdx1);
				this.processor.RegAcc.Value16 = 1;
			}
			else
			{
				this.processor.RegAcc.Value16 = 0;
			}

			this.processor.Journal.CloseScope("SwapTechnology");
		}

		/// <summary>
		/// Locates distance to nearest city of player possessing a palace/capital.
		/// </summary>
		public ushort LocateNearestCityOfPlayer(short participantIdx, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"LocateNearestCityOfPlayer({participantIdx}, {horizLoc}, {vertLoc})");

			int minDistance = 16;

			for (short t = 0; t < 128; t++)
			{
				var town = this.owner.World.Towns[t];
				if (town.ConditionMarker != 0xff &&
					town.ParticipantIdx == participantIdx &&
					(town.StructureMarkers0 & 1) != 0)
				{
					int dist = this.owner.Helpers.ObtainDistance(town.Place.X, horizLoc, town.Place.Y, vertLoc);
					if (dist < minDistance)
					{
						minDistance = dist;
					}
				}
			}

			this.processor.RegAcc.Value16 = (ushort)minDistance;
			this.processor.Journal.CloseScope("LocateNearestCityOfPlayer");
			return this.processor.RegAcc.Value16;
		}
	}
}
