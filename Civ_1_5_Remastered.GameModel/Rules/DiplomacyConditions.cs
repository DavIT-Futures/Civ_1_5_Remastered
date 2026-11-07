using System.Collections.Generic;
using System.Globalization;

namespace Civ_1_5_Remastered.GameModel.Rules
{
	/// <summary>
	/// Human-readable list of every condition a computer ruler weighs about the player, with the current values,
	/// for the War Advisor's conditions page. Each line is marked as good (+1) or bad (-1) for the player, or neutral (0).
	/// </summary>
	public static class DiplomacyConditions
	{
		public static List<(string Text, int Effect)> Lines(DiplomacyForecast forecast)
		{
			List<(string Text, int Effect)> lines = new List<(string Text, int Effect)>();

			if (!forecast.EnvyActive)
			{
				lines.Add(("Envy of the world leader: no.", 1));
			}
			else if (forecast.EnvyEase >= 2)
			{
				lines.Add(("Envy of the leader: gone, 60+ turns of peace.", 1));
			}
			else if (forecast.EnvyEase == 1)
			{
				lines.Add(("Envy of the leader: good mood can beat it.", 0));
			}
			else
			{
				lines.Add(("Envy: we lead after turn 200 without nukes.", -1));
			}

			lines.Add(forecast.ArmiesFacing ? ("Our armies face each other on a continent.", -1) : ("No armies facing each other.", 1));
			lines.Add(forecast.VendettaTurns > 0 ? ("Vendetta: " + forecast.VendettaTurns + " turns left.", -1) : ("No vendetta against us.", 1));

			if (forecast.TheirNukesOnly)
			{
				lines.Add(("They have nuclear weapons, we don't.", -1));
			}
			else if (forecast.OurNukes)
			{
				lines.Add(("Our nuclear weapons deter them.", 1));
			}

			lines.Add(("Army: ours " + forecast.OurMight + ", theirs " + forecast.TheirMight + " (" + Ratio(forecast.OurMight, forecast.TheirMight) + ").", 0));

			int threeTimes = (3 * forecast.TheirMight) + 1;
			lines.Add(("  Over 3x ends all hostility (" + threeTimes + "+).", forecast.OurMight >= threeTimes ? 1 : -1));

			if (forecast.AtPeace)
			{
				int halfAgain = ((3 * forecast.TheirMight) + 1) / 2;
				bool keepsTreaty = forecast.VendettaTurns == 0 && forecast.OurMight >= halfAgain;
				string rule = forecast.VendettaTurns > 0 ? "  1.5x keeps treaty, not under vendetta." : "  1.5x keeps them to the treaty (" + halfAgain + "+).";
				lines.Add((rule, keepsTreaty ? 1 : -1));
			}

			bool goodMood = forecast.Attitude > forecast.Calm;
			lines.Add(("Mood " + Signed(forecast.Attitude) + " must beat " + forecast.Calm + " to drop demands.", goodMood ? 1 : -1));

			if (forecast.AtPeace)
			{
				int turns = forecast.PeaceUnits * DiplomacyRules.TurnsPerUnit;
				string age = forecast.PeaceUnits >= DiplomacyRules.MaximumUnits ? "60+" : turns.ToString(CultureInfo.InvariantCulture);
				lines.Add(("Treaty " + age + " turns: trust " + Signed(DiplomacyRules.PeaceAttitudeBonus(forecast.PeaceUnits)) +
					(forecast.PeaceUnits < DiplomacyRules.MinimumTreatyUnits ? ", still protected." : "."), 1));
			}
			else
			{
				lines.Add(("No treaty: they may attack at any time.", -1));
			}

			if (forecast.Fear > 0)
			{
				lines.Add(("Fear of our troops near their cities: " + forecast.Fear + ".", 1));
			}

			lines.Add((Verdict(forecast), forecast.Hostile || forecast.PlansBetrayal ? -1 : 1));

			return lines;
		}

		public static string Ratio(int ours, int theirs)
		{
			if (theirs <= 0)
			{
				return "no army";
			}

			int tenths = ours * 10 / theirs;

			return (tenths / 10) + "." + (tenths % 10) + "x";
		}

		private static string Verdict(DiplomacyForecast forecast)
		{
			if (forecast.Hostile)
			{
				return "RESULT: hostile, tribute or war.";
			}

			if (forecast.PlansBetrayal)
			{
				return "RESULT: plotting to break the peace.";
			}

			return forecast.AtPeace ? "RESULT: they keep the peace." : "RESULT: ready to sign peace.";
		}

		private static string Signed(int value) => value > 0 ? "+" + value : value.ToString(CultureInfo.InvariantCulture);
	}
}
