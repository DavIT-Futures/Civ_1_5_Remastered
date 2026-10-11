using System;
using System.IO;
using Civilization1Remaster.Emulation;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class DirectionVectorTests
	{
		[Fact]
		public void CheckDirectionVectorsInMemory()
		{
			string binPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Civilization1Remaster.Core/Assets/BinaryResources.bin"));
			if (!File.Exists(binPath))
			{
				binPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../Civilization1Remaster.Core/Assets/BinaryResources.bin"));
			}
			byte[] assets = File.ReadAllBytes(binPath);

			// BinaryResources is loaded at linear address CPU.AsLinearAddress(0x35cf, 0)
			uint baseAddr = CPU.AsLinearAddress(0x35cf, 0);
			uint dataSegAddr = CPU.AsLinearAddress(0x3b01, 0);

			int xOffset = (int)(dataSegAddr + 0x1882 - baseAddr);
			int yOffset = (int)(dataSegAddr + 0x18e4 - baseAddr);

			// Read 8 directions (indices 1 to 8, word per entry)
			short[] dx = new short[9];
			short[] dy = new short[9];

			for (int i = 1; i <= 8; i++)
			{
				int xPos = xOffset + (i * 2);
				int yPos = yOffset + (i * 2);

				dx[i] = BitConverter.ToInt16(assets, xPos);
				dy[i] = BitConverter.ToInt16(assets, yPos);
			}

			// Verify expectations:
			// 1: North (0, -1)
			// 2: NE (1, -1)
			// 3: East (1, 0)
			// 4: SE (1, 1)
			// 5: South (0, 1)
			// 6: SW (-1, 1)
			// 7: West (-1, 0)
			// 8: NW (-1, -1)
			Assert.Equal(0, dx[1]); Assert.Equal(-1, dy[1]);
			Assert.Equal(1, dx[2]); Assert.Equal(-1, dy[2]);
			Assert.Equal(1, dx[3]); Assert.Equal(0, dy[3]);
			Assert.Equal(1, dx[4]); Assert.Equal(1, dy[4]);
			Assert.Equal(0, dx[5]); Assert.Equal(1, dy[5]);
			Assert.Equal(-1, dx[6]); Assert.Equal(1, dy[6]);
			Assert.Equal(-1, dx[7]); Assert.Equal(0, dy[7]);
			Assert.Equal(-1, dx[8]); Assert.Equal(-1, dy[8]);
		}
	}
}
