using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Civilization1Remaster.Tests.GoldenMaster;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential;

public sealed class BatchSeventyThreeDifferentialTests(ITestOutputHelper output)
{
	/// <summary>The variables of the timer interrupt (0x40 - 0x6f), which the snapshot leaves out</summary>
	private static readonly (ushort Segment, ushort Begin, int Length)[] ClockVariables = [(0x3b01, 0x40, 0x30)];

	[Fact]
	public void PointerEventMatchesOriginalCode()
	{
		Check(1, 60, (chance, pair) =>
		{
			ushort condition = (ushort)chance.Next(4);
			byte enabled = (byte)chance.Next(2);
			byte busy = (byte)chance.Next(2);
			byte shift = (byte)chance.Next(4);
			ushort buttons = (ushort)chance.Next(4);
			ushort horiz = (ushort)chance.Next(640);
			ushort vert = (ushort)chance.Next(200);
			ushort pressed = (ushort)chance.Next(4);
			int renewals = Calls(pair.Rewritten.Engine, pair, "RenewScreen");

			return new Scenario(
				$"PointerEvent(ax={condition}, bx={buttons}, cx={horiz}, dx={vert}, 5403={enabled}, 5402={busy}, 587c={shift})",
				game =>
				{
					game.Processor.RegAcc.Value16 = condition;
					game.Processor.RegBase.Value16 = buttons;
					game.Processor.RegCount.Value16 = horiz;
					game.Processor.RegData.Value16 = vert;
					game.Processor.EmitGlobalByte(0x587c, shift);
					game.Processor.EmitGlobalByte(0x5403, enabled);
					game.Processor.EmitGlobalByte(0x5402, busy);
					game.Processor.EmitGlobalWord(0x5874, pressed);
				},
				game => new PlatformServicesReference(game).PointerEvent(),
				game => game.Services.PointerEvent(),
				Register.Kept | Register.Acc | Register.Count,
				// From what the call did: the screen renewed, the marker 0x5402 taken (busy before) or left alone, and the
				// horizontal position stored shifted or not
				game =>
				{
					byte marker = game.Processor.Storage.PullUInt8(0x3b01, 0x5402);
					string screen = Calls(game, pair, "RenewScreen") > renewals ? "renewed"
						: marker == 2 && busy != 2 ? "busy" : "ignored";

					// RenewScreen draws the pointer again and stores its position itself
					return screen == "renewed" ? screen : screen + (game.Processor.Storage.PullUInt16(0x3b01, 0x586e) != horiz ? ", shifted" : "");
				},
				[(0x3b01, 0x586e, 8)]);
		}, output, ["renewed", "busy", "busy, shifted", "ignored", "ignored, shifted"]);
	}

	[Fact]
	public void ClockInterruptMatchesOriginalCode()
	{
		Check(1, 40, (chance, pair) =>
		{
			ushort counter54 = (ushort)(chance.Next(2) == 0 ? 1 : chance.Next(2, 6));
			byte counter59 = (byte)(chance.Next(2) == 0 ? 1 : chance.Next(2, 20));
			byte flag58 = (byte)chance.Next(2);
			ushort tempo48 = (ushort)(0x100 + chance.Next(3) * 0x40);
			ushort tempo46 = chance.Next(2) == 0 ? tempo48 : (ushort)(0x100 + chance.Next(3) * 0x40);
			ushort count44 = (ushort)(chance.Next(2) == 0 ? 0 : chance.Next(1, 6));
			ushort beat4c = (ushort)chance.Next(1, 4);
			ushort turns4a = (ushort)chance.Next(100);
			uint time42 = (uint)chance.Next();
			int beats = Calls(pair.Rewritten.Engine, pair, "OnClockBeat");

			return new Scenario(
				$"ClockInterrupt(54={counter54}, 59={counter59}, 58={flag58}, 46={tempo46:x}, 48={tempo48:x}, 44={count44}, 4c={beat4c})",
				game =>
				{
					// The test calls the interrupt itself: no timer interrupt of the fixed clock in between (since B6.0 the reads
					// of the timer variables count as polls of the clock)
					game.Processor.AllowClock = false;
					game.Processor.EmitUInt32(0x3b01, 0x42, time42);
					game.Processor.EmitGlobalWord(0x54, counter54);
					game.Processor.EmitGlobalByte(0x59, counter59);
					game.Processor.EmitGlobalWord(0x46, tempo46);
					game.Processor.EmitGlobalWord(0x48, tempo48);
					game.Processor.EmitGlobalWord(0x4a, turns4a);
					game.Processor.EmitGlobalWord(0x44, count44);
					game.Processor.EmitGlobalWord(0x4c, beat4c);
					game.Processor.EmitGlobalByte(0x58, flag58);
				},
				game => new PlatformServicesReference(game).ClockInterrupt(),
				game => game.Services.ClockInterrupt(),
				Register.Kept | Register.Acc | Register.Base | Register.Count | Register.Data,
				// From the variables after the call: a beat (0x54 reloaded and OnClockBeat), the 20th beat (0x59 reloaded) with
				// the flag 0x58 cleared, a new tempo taken (0x48 = 0x46, 0x56 = 0x20) or the same one, and the countdown 0x44
				game =>
				{
					string label = "no beat";

					if (Calls(game, pair, "OnClockBeat") > beats)
					{
						label = "beat";

						if (game.Processor.Storage.PullUInt8(0x3b01, 0x59) == 0x14)
						{
							label += flag58 != 0 && game.Processor.Storage.PullUInt8(0x3b01, 0x58) == 0 ? ", flag cleared"
								: game.Processor.Storage.PullUInt16(0x3b01, 0x4a) == (ushort)(turns4a + 1) ? ", new tempo" : ", same tempo";
						}
					}

					return label + (game.Processor.Storage.PullUInt16(0x3b01, 0x44) != count44 ? ", countdown" : "");
				},
				ClockVariables);
		}, output, ["no beat", "no beat, countdown", "beat", "beat, countdown", "beat, flag cleared", "beat, new tempo", "beat, same tempo"]);
	}

	[Fact]
	public void RefreshHueCyclingMatchesOriginalCode()
	{
		Check(1, 30, (chance, pair) =>
		{
			bool backwards = chance.Next(2) == 0;
			int timer = chance.Next(3) switch { 0 => 0, 1 => 1, _ => chance.Next(2, 5) };
			int span = chance.Next(1, 16);
			int position = chance.Next(3) switch { 0 => 0, 1 => 3 * span, _ => 3 * chance.Next(span + 1) };

			return new Scenario(
				$"RefreshHueCycling(backwards={backwards}, timer={timer}, colours={span + 1}, position={position})",
				game =>
				{
					game.VideoCard.ReserveScreen(0);
					game.Services.AssignHueCycleSpan(1, 2, 16, (ushort)(16 + span));
					game.Services.ActivateHueCycle(backwards ? (ushort)0xffff : (ushort)1);

					ushort cycle = game.Processor.PullUInt16(0x35cf, 2);
					game.Processor.EmitUInt8(0x35cf, (ushort)(cycle + 8), (byte)timer);
					game.Processor.EmitUInt16(0x35cf, (ushort)(cycle + 2), (ushort)position);
				},
				game => new PlatformServicesReference(game).RefreshHueCycling(),
				game => game.Services.RefreshHueCycling(),
				Register.Kept,
				// From the cycle after the call: not active (timer 0), waiting (timer counted down), or a step (timer reloaded,
				// the position moved by 3 or wrapped around the colours)
				game =>
				{
					ushort cycle = game.Processor.Storage.PullUInt16(0x35cf, 2);
					int timerAfter = game.Processor.Storage.PullUInt8(0x35cf, (ushort)(cycle + 8));
					int positionAfter = game.Processor.Storage.PullUInt16(0x35cf, (ushort)(cycle + 2));

					if (timer == 0)
						return timerAfter == 0 ? "inactive" : "?";
					if (timerAfter == timer - 1 && timerAfter != 0)
						return "waiting";

					string direction = backwards ? "backwards" : "forwards";
					return Math.Abs(positionAfter - position) == 3 ? $"{direction} step" : $"{direction} step wrapped";
				},
				[(0x35cf, 0, 0x220)]);
		}, output, ["inactive", "waiting", "forwards step", "forwards step wrapped", "backwards step", "backwards step wrapped"]);
	}

	[Fact]
	public void RunGameMatchesOriginalCode()
	{
		// The game never ends with the scripted keys: both versions run until the keyboard gives up (an exception), and
		// everything up to there (the calls with their arguments, the state, the screens) must be the same
		Check(1, 2, (chance, pair) =>
		{
			int mouse = Calls(pair.Rewritten.Engine, pair, "AssignMouseCursorImage");
			int turns = Calls(pair.Rewritten.Engine, pair, "HandleGameEnd");
			int worlds = Calls(pair.Rewritten.Engine, pair, "FreshWorldChoices");

			return new Scenario(
				"RunGame()",
				game =>
				{
					HeadlessGame headless = game == pair.Original.Engine ? pair.Original : pair.Rewritten;
					headless.Keyboard.TurnKeyLimit = 300;
					game.Libc.Clock = () => new DateTime(2026, 10, 16, 12, 0, 0);
				},
				game => new GameLauncherReference(game).RunGame(),
				game => game.Startup.RunGame(),
				Register.Kept,
				game => (Calls(game, pair, "AssignMouseCursorImage") > mouse ? "mouse, " : "no mouse, ") +
					(Calls(game, pair, "FreshWorldChoices") > worlds ? "new world, " : "") +
					(Calls(game, pair, "HandleGameEnd") > turns ? "turns" : "no turns"));
		}, output, ["mouse, new world, turns"]);
	}
}
