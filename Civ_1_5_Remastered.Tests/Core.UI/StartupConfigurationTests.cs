using Civ_1_5_Remastered.Core.Composition;
using Civ_1_5_Remastered.Core.Shared.Services;
using Xunit;

namespace Civ_1_5_Remastered.Tests.Core.UI
{
	public class StartupConfigurationTests
	{
		[Fact]
		public void InquireConfigurationAppliesAdLibChoice()
		{
			GameEngine engine = new GameEngine(new SilentGameHost());

			engine.VideoCard.Tokens.Enqueue('1');
			engine.VideoCard.Tokens.Enqueue('4');
			engine.VideoCard.Tokens.Enqueue('1');

			engine.StartupPreferences.InquireConfiguration();

			Assert.Equal("adlib", engine.Audio.DriverPreference);
			Assert.Equal(0x41, engine.Processor.PullGlobalByte(IGameLauncher.SoundDriverOptionAddress));
			Assert.Equal(1, engine.Globals.Mouse.Present);
			Assert.Equal(320, engine.VideoCard.Displays[0].Extent.Width);
			Assert.Equal(200, engine.VideoCard.Displays[0].Extent.Height);
		}

		[Fact]
		public void InquireConfigurationAppliesNoSoundAndKeyboardOnly()
		{
			GameEngine engine = new GameEngine(new SilentGameHost());

			engine.VideoCard.Tokens.Enqueue('1');
			engine.VideoCard.Tokens.Enqueue('1');
			engine.VideoCard.Tokens.Enqueue('2');

			engine.StartupPreferences.InquireConfiguration();

			Assert.Equal("none", engine.Audio.DriverPreference);
			Assert.Equal(0x4e, engine.Processor.PullGlobalByte(IGameLauncher.SoundDriverOptionAddress));
			Assert.Equal(0, engine.Globals.Mouse.Present);
		}

		[Fact]
		public void InquireConfigurationAcceptsEnterDefault()
		{
			GameEngine engine = new GameEngine(new SilentGameHost());

			engine.VideoCard.Tokens.Enqueue('\r');
			engine.VideoCard.Tokens.Enqueue('\r');
			engine.VideoCard.Tokens.Enqueue('\r');

			engine.StartupPreferences.InquireConfiguration();

			Assert.Equal("none", engine.Audio.DriverPreference);
			Assert.Equal(0x4e, engine.Processor.PullGlobalByte(IGameLauncher.SoundDriverOptionAddress));
			Assert.Equal(1, engine.Globals.Mouse.Present);
		}

		[Fact]
		public void InquireConfigurationAppliesRolandChoice()
		{
			GameEngine engine = new GameEngine(new SilentGameHost());

			engine.VideoCard.Tokens.Enqueue('1');
			engine.VideoCard.Tokens.Enqueue('5');
			engine.VideoCard.Tokens.Enqueue('1');

			engine.StartupPreferences.InquireConfiguration();

			Assert.Equal("roland", engine.Audio.DriverPreference);
			Assert.Equal(0x52, engine.Processor.PullGlobalByte(IGameLauncher.SoundDriverOptionAddress));
		}

		[Fact]
		public void InquireConfigurationAppliesFast486AdLibChoice()
		{
			GameEngine engine = new GameEngine(new SilentGameHost());

			engine.VideoCard.Tokens.Enqueue('1');
			engine.VideoCard.Tokens.Enqueue('7');
			engine.VideoCard.Tokens.Enqueue('1');

			engine.StartupPreferences.InquireConfiguration();

			Assert.Equal("asound2", engine.Audio.DriverPreference);
			Assert.Equal(0x41, engine.Processor.PullGlobalByte(IGameLauncher.SoundDriverOptionAddress));
		}
	}
}
