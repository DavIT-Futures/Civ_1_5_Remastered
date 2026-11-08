using System.IO.Compression;
using Civ_1_5_Remastered.Core.Shared.Texts;
using Civ_1_5_Remastered.Imaging.Pictures;
using Civ_1_5_Remastered.Tests.Solution;

namespace Civ_1_5_Remastered.Tests.Core.Game
{
	/// <summary>Renders the main map screen of a save to a PNG file (opt-in tool: CIV_PREVIEW=folder).</summary>
	public class MapScreenPreview
	{
		[Fact]
		public async Task RenderMainScreen()
		{
			string? output = Environment.GetEnvironmentVariable("CIV_PREVIEW");

			if (GameFiles.Folder == null || output == null)
			{
				return;
			}

			string save = Environment.GetEnvironmentVariable("CIV_PREVIEW_SAVE") ?? "CIVIL0.SVE";

			await Task.Run(() =>
			{
				using ComputerExpansionSimulation game = new ComputerExpansionSimulation(save);
				game.Engine.ImageToolkit.LoadPictureOrColors(3, 0, 0, GameTexts.FileSpritesPicture, 0);
				game.Engine.ImageToolkit.LoadPictureOrColors(3, 0, 0, GameTexts.FileSpritesPictureSecond, 0);

				// The unit sprites, cut like NewGameSetup.CutEnhancedGraphicsUnitSprites does.
				for (int index = 0; index < 0xc; index++)
				{
					game.Engine.Processor.EmitGlobalWord((ushort)(0xb886 + (index << 5)), game.Engine.VideoCard.ScreenToPicture(3, (ushort)(index << 4), 0, 0x10, 0x10));
					game.Engine.Processor.EmitGlobalWord((ushort)(0xb888 + (index << 5)), game.Engine.VideoCard.ScreenToPicture(3, (ushort)(index << 4), 0x10, 0x10, 0x10));
				}

				game.Engine.World.ParticipantMarkers = (short)(1 << game.Engine.World.PersonParticipantIndex);
				string[] size = (Environment.GetEnvironmentVariable("CIV_PREVIEW_VIEW") ?? "15x12").Split('x');
				game.Engine.MapViewport.Request(int.Parse(size[0]), int.Parse(size[1]));
				game.Engine.TurnHooks.RepaintMainScreen();
				short person = game.Engine.World.PersonParticipantIndex;
				var capital = game.Engine.World.Towns.First(town => town.ConditionMarker != 0xff && town.ParticipantIndex == person).Place;
				game.Engine.MapOperations.FocusMapView(person, capital.X - (game.Engine.MapViewport.Columns / 2), capital.Y - (game.Engine.MapViewport.Rows / 2));

				if (Environment.GetEnvironmentVariable("CIV_PREVIEW_CLASSIC") != null)
				{
					// A 320 x 200 screen begins with a full clear: it must appear centred over the map.
					game.Engine.Services.FloodBox(game.Engine.Globals.Screen.Box, 0, 0, 320, 200, 1);
					game.Engine.Services.FloodBox(game.Engine.Globals.Screen.Box, 10, 10, 300, 180, 7);
					game.Engine.TextPainter.DrawTextWithShadow("A CLASSIC 320 x 200 SCREEN", 60, 90, 15);
				}

				Save(game.Engine.VideoCard.Displays[0], Path.Combine(output, Path.GetFileNameWithoutExtension(save) + "_map.png"));
			}).WaitAsync(TimeSpan.FromMinutes(2));
		}

		private static void Save(VideoGraphicsBitmap display, string path)
		{
			int width = display.Extent.Width;
			int height = display.Extent.Height;
			byte[] raw = new byte[height * ((width * 3) + 1)];

			for (int row = 0; row < height; row++)
			{
				int line = row * ((width * 3) + 1);

				for (int column = 0; column < width; column++)
				{
					int colour = display.SwatchColor[display.PictureStorage[(row * display.Pitch) + column]];
					raw[line + 1 + (column * 3)] = (byte)(colour >> 16);
					raw[line + 2 + (column * 3)] = (byte)(colour >> 8);
					raw[line + 3 + (column * 3)] = (byte)colour;
				}
			}

			using FileStream file = File.Create(path);
			file.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
			byte[] header = new byte[13];
			System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header, width);
			System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
			header[8] = 8;
			header[9] = 2;
			Chunk(file, "IHDR", header);
			using MemoryStream packed = new MemoryStream();

			using (ZLibStream zip = new ZLibStream(packed, CompressionLevel.Optimal, true))
			{
				zip.Write(raw);
			}

			Chunk(file, "IDAT", packed.ToArray());
			Chunk(file, "IEND", Array.Empty<byte>());
		}

		private static void Chunk(Stream file, string kind, byte[] data)
		{
			byte[] length = new byte[4];
			System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
			file.Write(length);
			byte[] body = System.Text.Encoding.ASCII.GetBytes(kind).Concat(data).ToArray();
			file.Write(body);
			uint check = 0xffffffff;

			foreach (byte value in body)
			{
				check ^= value;

				for (int bit = 0; bit < 8; bit++)
				{
					check = (check & 1) != 0 ? (check >> 1) ^ 0xedb88320 : check >> 1;
				}
			}

			byte[] sum = new byte[4];
			System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(sum, ~check);
			file.Write(sum);
		}
	}
}
