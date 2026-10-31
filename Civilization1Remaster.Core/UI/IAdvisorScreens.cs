
namespace Civilization1Remaster.Core.UI
{
	/// <summary>The operations of AdvisorScreens that the other parts of the game use</summary>
	public interface IAdvisorScreens
	{
		/// <summary>
		/// Keeps the screen in the back screen and draws the header of a report (the title at the address, in the style)
		/// </summary>
		void ShowReportPreamble(GameText title, ushort style);

		/// <summary>
		/// The head of an advisor report on a screen of the colour: the title, then the kind of state by government / 2
		/// (0x473c, 0x4743, 0x474b; for the others the text at TextBuffer.Address stays) " of the " civilization, then the ruler's title
		/// (0x19b2 + 2 * government), the leader's name and the year
		/// </summary>
		void DrawReportPreamble(GameText title, ushort style);

		/// <summary>Waits for a key or a click and puts the screen back from the back screen</summary>
		void FinishReportScreen();

		/// <summary>
		/// The science report: the bar of the research (cost = known technologies * (1 before year 0, else 2) * the larger of
		/// 2 * difficulty + 0xd2de + 6 and 11 - known technologies), filled with a bulb icon per step of progress, the human
		/// player's current research, and the known technologies in 3 columns (white when the player found it first, else cyan)
		/// </summary>
		void ShowScienceReport(short participantIdx);

		/// <summary>
		/// The military report: a row for every unit type (0 - 27) the player has or builds, with the unit's picture, its name,
		/// "(attack/defence/moves)", the active units and the units in production; then the report of the units lost
		/// </summary>
		void DrawUnitCountsChart(short participantIdx);

		/// <summary>
		/// The report of the units lost: a row for every unit type (0 - 26) that some player lost, with the unit's picture (of
		/// the player), its name, the losses of the player (colour 12) and of the players 1 - 7 in their colours, 28 pixels apart
		/// </summary>
		void DrawPlayerComparisonChart(short participantIdx);

		/// <summary>
		/// The trade report: for every city of the player (after HandleCity) its luxuries / taxes / science, or "(Disorder)",
		/// the total income and the turns to the next discovery (of the human player, with the wonder effect 0x14 science * 1.5);
		/// on the right the upkeep of the buildings (count and cost, the palace 1 costs difficulty / 2 + 1 for each of the
		/// technologies 0x22 and 0x25 more) and the total cost. A new page after a key when the left column is full.
		/// </summary>
		void ShowTradeReport(short participantIdx);

		/// <summary>
		/// The foreign minister's report: a row 0x18 high for every living player 1 - 7 from y 0x20. The player himself and the
		/// players he has contact with (Relations bit 0x40) get their name and title, government, treasury, units (on the
		/// easiest two levels), a colour and peace, alliance or war with each player, the others only "no contact" (0x485b).
		/// The rows from the second player get a box "INFO" + row, and a key 1 - 7 or a click right of x 0x119 shows the
		/// intelligence report of that row's player (the rows without contact show nothing). Ends with a key or click elsewhere.
		/// </summary>
		void ShowMilitaryReport();

		/// <summary>
		/// The intelligence report on a civilization: its leader (with the traits of the leader table),
		/// capital (the last city with a palace), government, treasury, military (on the easiest two levels), its relations
		/// with the others (peace or war) and its technologies from the newest, until the screen is full.
		/// </summary>
		void ShowIntelligenceReport(short participantIdx);

		/// <summary>
		/// The attitude report (after HandleCity of each city of the player): its name, its citizens and the icons of its
		/// buildings that make people happy (mask 0x2618), 10 pixels a city, a new page after each key or click; then the
		/// percentages of happy, content and unhappy citizens of all the cities. The stack frame stays: DrawCitizenIcons reads
		/// what is left below it, so it must be called at the same depth as in the original.
		/// </summary>
		void ShowAttitudeReport(short participantIdx);

		/// <summary>
		/// Appends the count as a percentage of the total (from 300 on computed with the total in tens, as the original avoids
		/// the overflow)
		/// </summary>
		void AffixProductionCount(short count, short total);

		/// <summary>
		/// The city status report (after HandleCity of each city of the player): its name, "size-food{ shields| trade}" and what it
		/// builds "(shields/cost)" (a building for a negative index, a unit below 0x40; for another index the previous text is
		/// drawn there again), 8 pixels a city, a new page after each key or click
		/// </summary>
		void ShowCityStatusReport(short participantIdx);
	}
}
