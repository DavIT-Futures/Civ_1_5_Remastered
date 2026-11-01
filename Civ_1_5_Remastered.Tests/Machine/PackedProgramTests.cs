using Civ_1_5_Remastered.Machine.Library;

namespace Civ_1_5_Remastered.Tests.Machine
{
	public class PackedProgramTests
	{
		private const int UnpackedSize = 0x31330;

		[Fact]
		public void TheGameProgramUnpacksToItsFullImage()
		{
			if (GameFiles.Folder == null)
			{
				return;
			}

			byte[] image = PackedProgram.Unpack(File.ReadAllBytes(Path.Combine(GameFiles.Folder, "CIV.EXE")), 0x1000);

			Assert.Equal(UnpackedSize, image.Length);
		}

		[Fact]
		public void AProgramWithoutPackingIsRejected()
		{
			byte[] plain = new byte[0x40];
			plain[0] = (byte)'M';
			plain[1] = (byte)'Z';
			plain[0x08] = 2;

			Assert.Throws<InvalidDataException>(() => PackedProgram.Unpack(plain, 0x1000));
		}
	}
}
