using System.Drawing;
using Civilization1Remaster.Core;
using Civilization1Remaster.Foundation.Squeeze;
using Civilization1Remaster.Foundation.Support;
using Civilization1Remaster.Imaging;
using Civilization1Remaster.Machine;

namespace Civilization1Remaster.Tests.Core
{
	public class DrawingPathTests
	{
		private const ushort MainBox = 0x98;
		private const ushort SecondBox = 0x19c2;
		private const ushort ActiveBoxPointer = 0xaa;
		private const uint ScreensDigest = 126574273u;

		[Fact]
		public void FloodBoxIsRelativeToTheBoxAndSplitsTheStyle()
		{
			GameEngine? engine = Prepared();
			if (engine == null)
			{
				return;
			}

			engine.Services.FloodBox(MainBox, 3, 4, 2, 2, 0x0007);
			engine.Services.FloodBox(MainBox, 4, 4, 2, 1, 0x0305);

			Assert.Equal(7, engine.VideoCard.ObtainDot(0, 13, 9));
			Assert.Equal(7 ^ 5, engine.VideoCard.ObtainDot(0, 14, 9));
			Assert.Equal(5, engine.VideoCard.ObtainDot(0, 15, 9));
			Assert.Equal(7, engine.VideoCard.ObtainDot(0, 14, 10));
			Assert.Equal(0, engine.VideoCard.ObtainDot(0, 12, 9));
		}

		[Fact]
		public void EmptyFloodBoxDrawsNothing()
		{
			GameEngine? engine = Prepared();
			if (engine == null)
			{
				return;
			}

			engine.Services.FloodBox(MainBox, 0, 0, 0, 10, 0x0007);
			engine.Services.FloodBox(MainBox, 0, 0, 10, -1, 0x0007);

			Assert.All(Pixels(engine, 0), dot => Assert.Equal(0, dot));
		}

		[Fact]
		public void AssignDotUsesTheActiveBoxAndClipsInclusively()
		{
			GameEngine? engine = Prepared();
			if (engine == null)
			{
				return;
			}

			engine.Services.AssignDot(12, 13, 0x000f);
			engine.Services.AssignDot(200, 150, 0x0003);
			engine.Services.AssignDot(201, 10, 0x0004);
			engine.Services.AssignDot(1, 5, 6, 0x000c);

			Assert.Equal(0x0f, engine.VideoCard.ObtainDot(0, 22, 18));
			Assert.Equal(3, engine.VideoCard.ObtainDot(0, 210, 155));
			Assert.Equal(0, engine.VideoCard.ObtainDot(0, 211, 15));
			Assert.Equal(0x0c, engine.VideoCard.ObtainDot(1, 15, 11));
		}

		[Fact]
		public void MissingScreenThrows()
		{
			GameEngine? engine = Prepared();
			if (engine == null)
			{
				return;
			}

			SetBox(engine, MainBox, display: 3, 0, 0, 10, 10);

			Exception problem = Assert.Throws<Exception>(() => engine.Services.FloodBox(MainBox, 0, 0, 2, 2, 1));
			Assert.Equal("The screen 3 is not allocated", problem.Message);
			Assert.Equal("The screen 3 is not allocated", Assert.Throws<Exception>(() => engine.VideoCard.ObtainDot(3, 0, 0)).Message);
			Assert.Equal("The screen 3 is not allocated", Assert.Throws<Exception>(() => engine.VideoCard.RenderLine(MainBox, 0, 0, 1, 1, 1)).Message);
		}

		[Fact]
		public void ScreensAfterAllDrawingCallsAreUnchanged()
		{
			GameEngine? engine = Prepared();
			if (engine == null)
			{
				return;
			}

			Random random = new Random(1991);

			for (int idx = 0; idx < 60; idx++)
			{
				int x = random.Next(-10, 210), y = random.Next(-10, 160);
				ushort style = (ushort)(random.Next(16) | (random.Next(4) << 8));

				engine.Services.FloodBox(MainBox, x, y, random.Next(30), random.Next(30), style);
				engine.Services.AssignDot(x, y, style);
				engine.Services.AssignDot(1, y, x, style);
				engine.VideoCard.RenderLine(MainBox, x, y, random.Next(200), random.Next(150), style);
				engine.VideoCard.SubstituteHue(MainBox, x, y, random.Next(40), random.Next(40), (byte)random.Next(16), (byte)random.Next(16));
			}

			engine.TextPainter.RenderText("Hello Civilization 4000 BC", 5, 70, 0x000e);
			Box(engine, MainBox).TypefaceIdx = 2;
			engine.TextPainter.RenderText("Second font ?!", 5, 90, 0x0309);
			engine.VideoCard.RenderImage(MainBox, 0, 0, 80, 60, SecondBox, 100, 100);

			byte[] both = Pixels(engine, 0).Concat(Pixels(engine, 1)).ToArray();
			Assert.Equal(ScreensDigest, CRC32.ObtainCRC32(both));
		}

		private static GameEngine? Prepared()
		{
			if (GameFiles.Folder == null)
			{
				return null;
			}

			GameEngine engine = new GameEngine(new SilentHost());
			engine.Processor.StandardFolder = GameFiles.Folder + Path.DirectorySeparatorChar;
			engine.PrepareMachine();
			engine.VideoCard.ReserveScreen(1);

			SetBox(engine, MainBox, display: 0, 10, 5, 200, 150);
			SetBox(engine, SecondBox, display: 1, 0, 0, 320, 200);
			engine.Processor.EmitGlobalWord(ActiveBoxPointer, MainBox);

			return engine;
		}

		private static CivRectangle Box(GameEngine engine, ushort boxAddr) =>
			new CivRectangle(engine.Processor, CPU.AsLinearAddress(engine.Processor.SegData.Value16, boxAddr));

		private static void SetBox(GameEngine engine, ushort boxAddr, ushort display, short horiz, short vert, short breadth, short altitude)
		{
			CivRectangle box = Box(engine, boxAddr);
			box.DisplayIdx = display;
			box.Horiz = horiz;
			box.Vert = vert;
			box.Breadth = breadth;
			box.Altitude = altitude;
			box.ForeHues = 0x000f;
			box.RearHues = 0;
			box.TypefaceIdx = 1;
		}

		private static byte[] Pixels(GameEngine engine, ushort display)
		{
			VGABitmap screen = engine.VideoCard.Displays[display];
			byte[] pixels = new byte[320 * 200];

			for (int y = 0; y < 200; y++)
			{
				for (int x = 0; x < 320; x++)
				{
					pixels[(y * 320) + x] = screen.ObtainDot(x, y);
				}
			}

			return pixels;
		}

		private sealed class SilentHost : IGameHost
		{
			public Point PointerSite => Point.Empty;
			public PointerButtons PointerClicks => PointerButtons.None;

			public void OnDisplayCountChanged()
			{
			}

			public void ShowMessage(string message, string caption)
			{
			}

			public HostDialogResult ShowRetryCancel(string message, string caption) => HostDialogResult.Cancel;
		}
	}
}
