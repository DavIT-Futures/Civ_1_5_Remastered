namespace Civilization1Remaster.Core.UI
{
	public interface IRulerAudience
	{
		void LaunchAudience(short participantIdx, int horizLoc, int vertLoc, ushort occasion);

		void FinishAudience();

		ushort WelcomeEmissary(short participantIdx, int horizLoc, int vertLoc);

		void ObtainKingSalutation(short participantIdx);

		void ShowKingMessage(short participantIdx);

		void LogAudience(short participantIdx);

		void ChoreographKingSpeaking();

		void ShowKingScreen(short participantIdx);

		void LoadKingScenery(short participantIdx);

		void ReleaseKingResources();

		void ChoreographKingLikeness(ushort eyes, ushort mouth, ushort advisorPose);

		ushort ShowKingDialog(ushort charsAddr, ushort horizLoc, ushort vertLoc);
	}
}
