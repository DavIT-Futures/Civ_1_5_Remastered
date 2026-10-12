using System.Linq;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The help window, the spaceship launch, the head of the advisor reports, the city name question and breaking a treaty
	/// against their original code
	/// </summary>
	public class BatchThirteenDifferentialTests(ITestOutputHelper output)
	{
		/// <summary>The segment 0x3772: the spaceship parts, layouts and pictures</summary>
		private static readonly (ushort, ushort, int)[] Spaceships = [(0x3772, 0, 0x8000)];

		[Fact]
		public void DisplayHelpPopupMatchesOriginalCode()
		{
			ushort[] topics = { 0x4f66, 0x2708, 0x1e60, 0x1e67, 0x1e6e };

			Check(3, 30, (chance, pair) =>
			{
				ushort topic = topics[chance.Next(topics.Length)];
				bool helpOn = chance.Next(4) != 0;
				short season = (short)(chance.Next(3) == 0 ? chance.Next(0, 2000) : chance.Next(-4000, 0));
				int reflowed = Calls(pair.Rewritten.Engine, pair, "ReflowText");

				return new Scenario($"DisplayHelpPopup(0x{topic:x4}), help {helpOn}, year {season}",
					game =>
					{
						game.World.SessionPrefMarkers = (short)(helpOn ? game.World.SessionPrefMarkers | 1 : game.World.SessionPrefMarkers & ~1);
						game.World.Season = season;
					},
					game => new InGameHelpReference(game).DisplayHelpPopup(topic),
					game => game.HelpSystem.DisplayHelpPopup(topic),
					Register.Kept, game => Calls(game, pair, "ReflowText") > reflowed ? "shown" : !helpOn ? "help off" : "after year 0");
			}, output, ["help off", "after year 0", "shown"]);
		}

		[Fact]
		public void FireSpaceshipMatchesOriginalCode()
		{
			Check(3, 16, (chance, pair) =>
			{
				short participantIdx = (short)(chance.Next(3) == 0 ? pair.Original.Engine.World.PersonParticipantIdx : chance.Next(8));
				short arrival = (short)chance.Next(1990, 2100);

				return new Scenario($"FireSpaceship({participantIdx}), arrival {arrival}",
					game => game.World.Participants[participantIdx].StarshipArrivalSeason = arrival,
					game => new SpaceshipScreenReference(game).FireSpaceship(participantIdx),
					game => game.RocketScreen.FireSpaceship(participantIdx),
					Register.Kept, game => participantIdx == game.World.PersonParticipantIdx ? "human" : "other player", Spaceships);
			}, output, ["human", "other player"]);
		}

		[Fact]
		public void RenderReportPreambleMatchesOriginalCode()
		{
			Check(10, 60, (chance, pair) =>
			{
				short regime = (short)chance.Next(-1, 8);
				ushort style = (ushort)chance.Next(16);

				return new Scenario($"RenderReportPreamble(0xbb80, {style}), government {regime}",
					game =>
					{
						game.World.Participants[game.World.PersonParticipantIdx].RegimeKind = regime;
						game.Libc.CopyText(0xbb80, "TRADE ADVISOR");
						game.Libc.CopyText(0xba06, "Leftover");
					},
					game => new AdvisorReportsReference(game).RenderReportPreamble(0xbb80, style),
					game => game.Briefings.RenderReportPreamble(0xbb80, style),
					Register.Kept, game => regime / 2 is 0 or 1 or 2 ? $"kind {regime / 2}" : "no kind");
			}, output, ["kind 0", "kind 1", "kind 2", "no kind"]);
		}

		[Fact]
		public void DisplayCityLabelPromptMatchesOriginalCode()
		{
			// A name typed over the old one and accepted, Escape (answer 0), Enter at once
			int[] keys = "Abcd\r\x1b\rZz\r".Select(letter => (int)letter).ToArray();

			Check(10, 30, (chance, pair) =>
			{
				short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx)
					.Where(idx => pair.Original.Engine.World.Towns[idx].ConditionMarker != 0xff).ToArray();
				short townIdx = towns[chance.Next(towns.Length)];

				return new Scenario($"DisplayCityLabelPrompt({townIdx})", game => { },
					game => new NewCityScreenReference(game).DisplayCityLabelPrompt(townIdx),
					game => game.CityNaming.DisplayCityLabelPrompt(townIdx),
					Register.Acc | Register.Kept, game => game.Processor.RegAcc.Value16 != 0 ? "renamed" : "cancelled");
			}, output, ["renamed", "cancelled"], keys);
		}

		[Fact]
		public void AffirmBreakTreatyMatchesOriginalCode()
		{
			// The second line (Break treaty) or the first one (Cancel action)
			int[] keys = { 0x5000, 0x0d, 0x0d, 0x0d, 0x5000, 0x0d, 0x0d };

			Check(3, 60, (chance, pair) =>
			{
				short participantIdx = (short)chance.Next(8);
				bool treaty = chance.Next(4) != 0;
				short regime = (short)(chance.Next(3) == 0 ? 4 : chance.Next(7)); // 4: the first government with a Senate
				int dialogs = Calls(pair.Rewritten.Engine, pair, "DisplayDialog");

				return new Scenario($"AffirmBreakTreaty({participantIdx}), treaty {treaty}, government {regime}",
					game =>
					{
						var person = game.World.Participants[game.World.PersonParticipantIdx];
						person.Relations[participantIdx] = (ushort)(treaty ? person.Relations[participantIdx] | 2 : person.Relations[participantIdx] & ~2);
						person.RegimeKind = regime;
					},
					game => new UnitCombatReference(game).AffirmBreakTreaty(participantIdx),
					game => game.Combat.AffirmBreakTreaty(participantIdx),
					Register.Acc | Register.Kept,
					game => game.Processor.RegAcc.Value16 switch
					{
						0 => "no treaty",
						1 => "broken",
						_ => Calls(game, pair, "DisplayDialog") - dialogs > 1 ? "overruled" : "cancelled",
					});
			}, output, ["no treaty", "broken", "overruled", "cancelled"], keys);
		}
	}
}
