namespace Civilization1Remaster.Core.Engine
{
	public sealed partial class GameGlobals
	{
		/// <summary>The audiences with the kings and the advisors (B6.4: C# fields over the memory of the data segment)</summary>
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

			/// <summary>0x67f4: the step of the king's animation (0 standing still)</summary>
			public ushort Step { get => this.step.Value; set => this.step.Value = value; }

			/// <summary>0xd76c: the king speaks (counts down)</summary>
			public ushort Speaking { get => this.speaking.Value; set => this.speaking.Value = value; }

			/// <summary>0x3934: the answer of the king dialog (-1 not known yet)</summary>
			public ushort Answer { get => this.answer.Value; set => this.answer.Value = value; }

			/// <summary>0xd804: the audience with the emissary of the player took place this turn</summary>
			public ushort EmissaryAudience { get => this.emissaryAudience.Value; set => this.emissaryAudience.Value = value; }

			/// <summary>0x3936: the mood of the king in the audience, which picks his next gesture (0, 1, 2; 0xffff: no audience)</summary>
			public ushort Mood { get => this.mood.Value; set => this.mood.Value = value; }

			/// <summary>0x67f8: the picture of the king in the audience (kingNN.pic)</summary>
			public ushort Picture { get => this.picture.Value; set => this.picture.Value = value; }

			/// <summary>0x67f6: the frames of the pause of the speaking king</summary>
			public ushort Pause { get => this.pause.Value; set => this.pause.Value = value; }

			/// <summary>0x7ef4: the pose the king holds (0, 1, 2)</summary>
			public ushort Pose { get => this.pose.Value; set => this.pose.Value = value; }

			/// <summary>0x67f2: the player whose king gives the audience</summary>
			public ushort AudiencePlayer { get => this.audiencePlayer.Value; set => this.audiencePlayer.Value = value; }

			/// <summary>0x67f0: the audience shows a line over a motif instead of the throne room</summary>
			public ushort AudienceWithoutPictures { get => this.audienceWithoutPictures.Value; set => this.audienceWithoutPictures.Value = value; }

			/// <summary>0x67ec: the style of screen 0 kept during the audience</summary>
			public ushort SavedScreenStyle { get => this.savedScreenStyle.Value; set => this.savedScreenStyle.Value = value; }

			/// <summary>0x67ea: the picture of the king is loaded</summary>
			public ushort PictureLoaded { get => this.pictureLoaded.Value; set => this.pictureLoaded.Value = value; }

			/// <summary>0x67e8: the pose of the advisors shown (0xffff none)</summary>
			public ushort AdvisorPoseShown { get => this.advisorPoseShown.Value; set => this.advisorPoseShown.Value = value; }

			/// <summary>0xb1e6: the picture of the king in the throne room</summary>
			public ushort ScenePicture { get => this.scenePicture.Value; set => this.scenePicture.Value = value; }

			/// <summary>0x7efe: the picture of the faces and mouths of the king</summary>
			public ushort FacesPicture { get => this.facesPicture.Value; set => this.facesPicture.Value = value; }

			/// <summary>0x6b8e: the king welcomed the friendship</summary>
			public ushort AudienceWelcomed { get => this.audienceWelcomed.Value; set => this.audienceWelcomed.Value = value; }

			/// <summary>0x67ee: the pictures of the audience are the modern ones (technology 0x14)</summary>
			public ushort ModernPictures { get => this.modernPictures.Value; set => this.modernPictures.Value = value; }

			/// <summary>0x67fa: the screen of the king was kept in the back screen (1)</summary>
			public ushort ScreenKept { get => this.screenKept.Value; set => this.screenKept.Value = value; }

			/// <summary>0xd2dc: initial prompt flag in audience with foreign king</summary>
			public ushort AudienceInitialPrompt { get => this.audienceInitialPrompt.Value; set => this.audienceInitialPrompt.Value = value; }
		}
	}
}
