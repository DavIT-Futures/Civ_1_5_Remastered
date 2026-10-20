using System;

namespace Civilization1Original.Game.Rules
{
	/// <summary>
	/// Pure C# domain rules and calculators for advisor reports summaries:
	/// City Status totals (Food, Production/Shields, Trade, Population, Yields),
	/// Trade Advisor totals (Trade, Taxes, Science, Luxuries, Gross Yields),
	/// Science Advisor progress (Current beakers, Required cost, Beakers/turn, Turns to advance).
	/// </summary>
	public static class AdvisorStatistics
	{
		/// <summary>
		/// Calculates the number of turns required to complete the current scientific research.
		/// </summary>
		/// <param name="currentBeakers">Current accumulated research points (InquiryHeadway).</param>
		/// <param name="requiredBeakers">Total research cost for the technology.</param>
		/// <param name="sciencePerTurn">Total science generated per turn by all cities.</param>
		/// <returns>Turns remaining, or 0 if complete, or -1 if no progress is being made.</returns>
		public static int CalculateTurnsToDiscovery(int currentBeakers, int requiredBeakers, int sciencePerTurn)
		{
			int remaining = requiredBeakers - currentBeakers;
			if (remaining <= 0)
			{
				return 0;
			}

			if (sciencePerTurn <= 0)
			{
				return -1; // Infinite / stagnant
			}

			// Ceiling division: (remaining + sciencePerTurn - 1) / sciencePerTurn
			return (remaining + sciencePerTurn - 1) / sciencePerTurn;
		}

		/// <summary>
		/// Calculates total gross city yields (Food + Shields/Production + Trade).
		/// </summary>
		public static int CalculateTotalYields(int totalFood, int totalProduction, int totalTrade)
		{
			return totalFood + totalProduction + totalTrade;
		}

		/// <summary>
		/// Formats a concise summary string for City Status report (F1).
		/// </summary>
		public static string FormatCityStatusSummary(int cityCount, int totalPop, int totalFood, int totalProduction, int totalTrade)
		{
			int totalYields = CalculateTotalYields(totalFood, totalProduction, totalTrade);
			return $"TOTAL ({cityCount} cities, Pop {totalPop}): Food {totalFood} | Prod {totalProduction} | Trade {totalTrade} (Yields: {totalYields})";
		}

		/// <summary>
		/// Formats trade breakdown and yields summary for Trade Advisor report (F5).
		/// </summary>
		public static string FormatTradeTotals(int totalTrade, int totalTax, int totalScience, int totalLuxuries)
		{
			return $"Total Trade: {totalTrade} (Tax:{totalTax}$  Sci:{totalScience}~  Lux:{totalLuxuries}\\/)";
		}

		/// <summary>
		/// Formats nation-wide gross yields for Trade Advisor report (F5).
		/// </summary>
		public static string FormatTradeYields(int totalFood, int totalProduction, int totalTrade)
		{
			int totalYields = CalculateTotalYields(totalFood, totalProduction, totalTrade);
			return $"Total Yields: {totalYields} (Food:{totalFood}  Prod:{totalProduction}  Trade:{totalTrade})";
		}

		/// <summary>
		/// Formats science progress summary line for Science Advisor report (F6).
		/// </summary>
		public static string FormatScienceSummary(int currentBeakers, int requiredBeakers, int sciencePerTurn)
		{
			int turns = CalculateTurnsToDiscovery(currentBeakers, requiredBeakers, sciencePerTurn);
			string turnsText = turns switch
			{
				0 => "Completed!",
				1 => "1 turn",
				> 1 => $"{turns} turns",
				_ => "-- (no progress)"
			};

			int pct = requiredBeakers > 0 ? Math.Clamp((currentBeakers * 100) / requiredBeakers, 0, 100) : 0;
			return $"Science: {currentBeakers}/{requiredBeakers} ({pct}%) | +{sciencePerTurn}/turn | Next: {turnsText}";
		}
	}
}
