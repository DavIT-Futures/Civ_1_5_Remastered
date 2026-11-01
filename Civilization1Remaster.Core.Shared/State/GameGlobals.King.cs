namespace Civilization1Remaster.Core.Shared.State
{
	public sealed partial class GameGlobals
	{
		public sealed class KingVariables : VariableGroup
		{
			private readonly GlobalWord step = new GlobalWord(0x67f4);
			private readonly GlobalWord speaking = new GlobalWord(0xd76c);
			private readonly GlobalWord answer = new GlobalWord(0x3934);
			private readonly GlobalWord emissaryAudience = new GlobalWord(0xd804);
			private readonly GlobalWord mood = new GlobalWord(0x3936);
			private readonly GlobalWord picture = new GlobalWord(0x67f8);
			private readonly GlobalWord pause = new GlobalWord(0x67f6);
			private readonly GlobalWord pose = new GlobalWord(0x7ef4);
			private readonly GlobalWord audiencePlayer = new GlobalWord(0x67f2);
			private readonly GlobalWord audienceWithoutPictures = new GlobalWord(0x67f0);
			private readonly GlobalWord savedScreenStyle = new GlobalWord(0x67ec);
			private readonly GlobalWord pictureLoaded = new GlobalWord(0x67ea);
			private readonly GlobalWord advisorPoseShown = new GlobalWord(0x67e8);
			private readonly GlobalWord scenePicture = new GlobalWord(0xb1e6);
			private readonly GlobalWord facesPicture = new GlobalWord(0x7efe);
			private readonly GlobalWord audienceWelcomed = new GlobalWord(0x6b8e);
			private readonly GlobalWord modernPictures = new GlobalWord(0x67ee);
			private readonly GlobalWord screenKept = new GlobalWord(0x67fa);
			private readonly GlobalWord audienceInitialPrompt = new GlobalWord(0xd2dc);

			public ushort Step { get => this.step.Value; set => this.step.Value = value; }

			public ushort Speaking { get => this.speaking.Value; set => this.speaking.Value = value; }

			public ushort Answer { get => this.answer.Value; set => this.answer.Value = value; }

			public ushort EmissaryAudience { get => this.emissaryAudience.Value; set => this.emissaryAudience.Value = value; }

			public ushort Mood { get => this.mood.Value; set => this.mood.Value = value; }

			public ushort Picture { get => this.picture.Value; set => this.picture.Value = value; }

			public ushort Pause { get => this.pause.Value; set => this.pause.Value = value; }

			public ushort Pose { get => this.pose.Value; set => this.pose.Value = value; }

			public ushort AudiencePlayer { get => this.audiencePlayer.Value; set => this.audiencePlayer.Value = value; }

			public ushort AudienceWithoutPictures { get => this.audienceWithoutPictures.Value; set => this.audienceWithoutPictures.Value = value; }

			public ushort SavedScreenStyle { get => this.savedScreenStyle.Value; set => this.savedScreenStyle.Value = value; }

			public ushort PictureLoaded { get => this.pictureLoaded.Value; set => this.pictureLoaded.Value = value; }

			public ushort AdvisorPoseShown { get => this.advisorPoseShown.Value; set => this.advisorPoseShown.Value = value; }

			public ushort ScenePicture { get => this.scenePicture.Value; set => this.scenePicture.Value = value; }

			public ushort FacesPicture { get => this.facesPicture.Value; set => this.facesPicture.Value = value; }

			public ushort AudienceWelcomed { get => this.audienceWelcomed.Value; set => this.audienceWelcomed.Value = value; }

			public ushort ModernPictures { get => this.modernPictures.Value; set => this.modernPictures.Value = value; }

			public ushort ScreenKept { get => this.screenKept.Value; set => this.screenKept.Value = value; }

			public ushort AudienceInitialPrompt { get => this.audienceInitialPrompt.Value; set => this.audienceInitialPrompt.Value = value; }
		}
	}
}
