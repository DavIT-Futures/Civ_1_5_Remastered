using System;
using System.Collections.Generic;
using Civ_1_5_Remastered.GameModel.State;

namespace Civ_1_5_Remastered.GameModel.Rules
{
	/// <summary>
	/// Side-effect free estimate of what a computer ruler would do in the next audience with the player.
	/// Mirrors the weighing done by the ruler audience (continents, nuclear weapons, enemies),
	/// without the random parts, so the War Advisor can explain hostility and friendship.
	/// </summary>
	public sealed class DiplomacyForecast
	{
		public const int NuclearUnitKind = 25;

		public int Attitude { get; private set; }

		public int CharacterScore { get; private set; }

		public int VendettaScore { get; private set; }

		public int RivalWarsScore { get; private set; }

		public int MightScore { get; private set; }

		public int PeaceScore { get; private set; }

		public bool AtPeace { get; private set; }

		public bool Hostile { get; private set; }

		public bool PlansBetrayal { get; private set; }

		public int Fear { get; private set; }

		public int Tribute { get; private set; }

		public int PeaceGold { get; private set; }

		public int UnderPeaceGold { get; private set; }

		public bool OffersPeace { get; private set; }

		public bool OffersExchange { get; private set; }

		/// <summary>The world-leader envy applies (turn over 200, we lead with over 4 cities, no nuclear weapons).</summary>
		public bool EnvyActive { get; private set; }

		/// <summary>How far a long peace eases the envy, see <see cref="DiplomacyRules.EnvyEase"/>.</summary>
		public int EnvyEase { get; private set; }

		public bool ArmiesFacing { get; private set; }

		public bool TheirNukesOnly { get; private set; }

		public bool OurNukes { get; private set; }

		public int OurMight { get; private set; }

		public int TheirMight { get; private set; }

		public int PeaceUnits { get; private set; }

		public int VendettaTurns { get; private set; }

		/// <summary>The attitude a ruler needs to exceed to drop his hostility: 1 at war, 0 under a treaty.</summary>
		public int Calm { get; private set; }

		public List<string> HostileReasons { get; } = new List<string>();

		public List<string> CalmingReasons { get; } = new List<string>();

		public static DiplomacyForecast Compute(Player[] participants, int holder, int person, int aggression, int hardnessTier, int roundTally,
			bool wonderShield, int holderTechnologies, int personTechnologies, bool holderKnowsNewTechnology)
		{
			Player leader = participants[holder];
			Player human = participants[person];
			ushort relation = leader.Relations[person];
			bool vendetta = (relation & DiplomacyRules.VendettaRelation) != 0;
			bool leaderNukes = leader.LiveTroops[NuclearUnitKind] != 0;
			bool humanNukes = human.LiveTroops[NuclearUnitKind] != 0;

			DiplomacyForecast forecast = new DiplomacyForecast
			{
				AtPeace = (relation & DiplomacyRules.PeaceRelation) != 0,
			};

			int danger = 0;
			int tribute = 0;
			bool hostile = false;
			bool facing = false;

			for (int continent = 1; continent < 15; continent++)
			{
				PlayerContinent own = leader.Landmasses[continent];
				PlayerContinent theirs = human.Landmasses[continent];
				int cities = leader.TownTally > 1 ? 1 : 0;

				if (own.TownTally > cities && theirs.Guard > own.Assault)
				{
					danger += (hardnessTier == 0 ? 4 : 2) * theirs.Guard / (own.Assault + 1);
					continue;
				}

				if (theirs.Assault != 0 && own.Assault != 0)
				{
					hostile = true;
					facing = true;
				}

				if (theirs.TownTally != 0)
				{
					tribute += own.TownTally > 1 ? own.Guard - theirs.Guard : own.Guard;
				}
				else if (own.TownTally != 0)
				{
					tribute += (own.Assault - theirs.Guard) / 2;
				}
				else
				{
					tribute += (own.Guard - theirs.Guard) / 4;
				}
			}

			bool threat = human.Standing == 7 && human.TownTally > 4 && leader.TownTally > 1 && !humanNukes && roundTally > 200;
			forecast.PeaceUnits = forecast.AtPeace ? DiplomacyRules.PeaceUnits(participants, holder, person) : 0;
			forecast.EnvyActive = threat;
			forecast.EnvyEase = DiplomacyRules.EnvyEase(forecast.AtPeace, forecast.PeaceUnits);
			forecast.ArmiesFacing = facing;
			forecast.TheirNukesOnly = leaderNukes && !humanNukes;
			forecast.OurNukes = humanNukes;
			forecast.OurMight = human.MartialMight;
			forecast.TheirMight = leader.MartialMight;
			forecast.VendettaTurns = DiplomacyRules.GrudgeTurns(relation);
			forecast.Calm = forecast.AtPeace ? 0 : 1;

			if (threat && forecast.EnvyEase >= 2)
			{
				threat = false;
				forecast.CalmingReasons.Add("60+ turns of peace ended their envy.");
			}

			bool envyOutweighable = !threat || forecast.EnvyEase >= 1;
			bool vendettaAnger = vendetta && 3 * leader.MartialMight > human.MartialMight;

			if ((threat || vendettaAnger) && !humanNukes)
			{
				hostile = true;
				tribute = Math.Clamp(tribute, 10 * leader.Standing, 9999);
				danger = 0;
			}

			if (leaderNukes && !humanNukes)
			{
				hostile = true;
				tribute = Math.Clamp(tribute, 100, 9999);
				forecast.HostileReasons.Add("They have nuclear weapons, we don't.");
			}

			if (humanNukes)
			{
				if (!leaderNukes)
				{
					danger = 4;
					forecast.CalmingReasons.Add("They fear our nuclear weapons.");
				}
				else
				{
					tribute = leader.LiveTroops[NuclearUnitKind] * (tribute / 2) / human.LiveTroops[NuclearUnitKind];
				}
			}

			if (danger != 0)
			{
				hostile = false;
			}

			if (wonderShield)
			{
				threat = false;
				hostile = false;
				forecast.CalmingReasons.Add("Our wonders keep them peaceful.");
			}

			if (vendetta)
			{
				tribute <<= 1;
			}

			tribute = Math.Clamp(tribute * (hardnessTier + 1) / 32, 0, 20) * 50;

			if (tribute > human.Gold && tribute <= 2 * human.Gold && human.Gold >= 50)
			{
				tribute = human.Gold / 50 * 50;
			}

			if (3 * leader.MartialMight < human.MartialMight)
			{
				if (hostile)
				{
					forecast.CalmingReasons.Add("Our army is over 3x stronger.");
				}

				hostile = false;
			}

			if (tribute == 0)
			{
				hostile = false;
			}

			forecast.CharacterScore = -aggression;
			forecast.VendettaScore = vendetta ? -2 : 0;

			for (int other = 1; other < participants.Length; other++)
			{
				if (other == person || other == holder || (leader.Relations[other] & 3) != 1)
				{
					continue;
				}

				if (leader.MartialMight < participants[other].MartialMight * 4)
				{
					forecast.RivalWarsScore++;
				}

				if (participants[other].MartialMight > leader.MartialMight)
				{
					forecast.RivalWarsScore++;
				}
			}

			forecast.MightScore = human.MartialMight < leader.MartialMight ? -1 : 0;
			forecast.PeaceScore = forecast.AtPeace ? DiplomacyRules.PeaceAttitudeBonus(DiplomacyRules.PeaceUnits(participants, holder, person)) : 0;
			forecast.Attitude = forecast.CharacterScore + forecast.VendettaScore + forecast.RivalWarsScore + forecast.MightScore + forecast.PeaceScore;

			int calm = forecast.AtPeace ? 0 : 1;

			if (hostile && calm < forecast.Attitude && envyOutweighable)
			{
				hostile = false;
				forecast.CalmingReasons.Add("Their good attitude outweighs it.");
			}

			if (hostile && leader.RegimeKind >= 4 && !vendetta && 3 * human.MartialMight > 2 * leader.MartialMight && envyOutweighable)
			{
				hostile = false;
				forecast.CalmingReasons.Add("Their republic avoids a war with us.");
			}

			if (hostile && forecast.AtPeace && (leader.MartialMight < human.MartialMight || vendetta))
			{
				hostile = false;

				if (DiplomacyRules.WeighBreakingPeace(DiplomacyRules.PeaceUnits(participants, holder, person),
					leader.MartialMight, human.MartialMight, vendetta) >= 0)
				{
					forecast.PlansBetrayal = true;
				}
				else
				{
					forecast.CalmingReasons.Add("Our army is 1.5x stronger: they keep the treaty.");
				}
			}

			if (facing)
			{
				forecast.HostileReasons.Add("Our armies face each other.");
			}

			if (vendettaAnger)
			{
				forecast.HostileReasons.Add("Vendetta: they want revenge.");
			}

			if (threat)
			{
				forecast.HostileReasons.Add("We lead the world without nukes.");
			}

			if (danger > 0)
			{
				forecast.CalmingReasons.Add("Our troops near their cities.");
			}

			if (forecast.RivalWarsScore > 0)
			{
				forecast.CalmingReasons.Add("They are busy fighting rivals.");
			}

			if (forecast.PeaceScore > 0)
			{
				forecast.CalmingReasons.Add("They trust our long peace.");
			}

			forecast.Hostile = hostile;
			forecast.Fear = danger;
			forecast.Tribute = hostile ? tribute : 0;
			forecast.OffersPeace = !hostile && !forecast.AtPeace;
			forecast.PeaceGold = !forecast.AtPeace && !vendetta ? Math.Clamp((danger * 2) - 4, 0, leader.Gold / 50) * 50 : 0;
			forecast.UnderPeaceGold = forecast.AtPeace && human.RegimeKind < 4 ? Math.Clamp(danger, 0, leader.Gold / 50) * 50 : 0;

			bool blocked = ((relation & DiplomacyRules.RecentCombatRelation) != 0 || hostile) && holderTechnologies > 6;
			forecast.OffersExchange = !blocked && personTechnologies >= holderTechnologies && holderKnowsNewTechnology;

			return forecast;
		}
	}
}
