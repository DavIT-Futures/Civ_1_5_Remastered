using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential;

public sealed class BatchSeventyTwoDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void DisplayCivilizationScreenMatchesOriginalCode()
	{
		int[] keys = [0x20, 0x0d];
		int step = 0;

		Check(1, 4, (chance, pair) =>
		{
			int mode = (step++) % 4;
			short participantIdx = (mode & 1) == 0 ? (short)1 : (short)2;
			ushort isVga = (mode & 2) == 0 ? (ushort)1 : (ushort)0;

			return new Scenario(
				$"DisplayCivilizationScreen(participant={participantIdx}, vga={isVga})",
				game =>
				{
					game.World.PersonParticipantIdx = 1;
					game.World.Participants[1].StarshipInhabitants = 20000;
					game.World.Participants[1].StarshipLiftoffSeason = 2000;
					game.World.Participants[2].StarshipInhabitants = 15000;
					game.World.Participants[2].StarshipLiftoffSeason = 2010;
					game.World.Season = 2020;
					game.Processor.EmitGlobalWord(0xd762, isVga);
					game.VideoCard.ReserveScreen(3);
				},
				game => new MainIntroReference(game).DisplayCivilizationScreen(participantIdx),
				game => game.Prologue.DisplayCivilizationScreen(participantIdx),
				Register.Kept,
				_ => mode switch
				{
					0 => "human-vga",
					1 => "ai-vga",
					2 => "human-cga",
					_ => "ai-cga"
				});
		}, output, ["human-vga", "ai-vga", "human-cga", "ai-cga"], keys);
	}

	[Fact]
	public void DisplayMainIntroMatchesOriginalCode()
	{
		int[] keys = [0x20, 0x4e, 0x0d, 0x20];
		int step = 0;

		Check(1, 2, (chance, pair) =>
		{
			int mode = (step++) % 2;

			return new Scenario(
				$"DisplayMainIntro(mode={mode})",
				game =>
				{
					game.World.PersonParticipantIdx = 1;
					game.Processor.EmitGlobalByte(0x1a22, mode == 0 ? (byte)0x4d : (byte)0x43);
					game.VideoCard.ReserveScreen(3);
				},
				game => new MainIntroReference(game).DisplayMainIntro(),
				game => game.Prologue.DisplayMainIntro(),
				Register.Kept,
				_ => mode == 0 ? "vga" : "cga");
		}, output, ["vga", "cga"], keys);
	}
}
