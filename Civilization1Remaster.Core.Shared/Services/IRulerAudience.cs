namespace Civilization1Remaster.Core.Shared.Services
{
	public interface IRulerAudience
	{
		void LaunchAudience(short participantIndex, int horizontalLocation, int verticalLocation, ushort occasion);

		void FinishAudience();

		ushort WelcomeEmissary(short participantIndex, int horizontalLocation, int verticalLocation);

		void ObtainKingSalutation(short participantIndex);

		void ShowKingMessage(short participantIndex);

		void LogAudience(short participantIndex);

		void ChoreographKingSpeaking();

		void ShowKingScreen(short participantIndex);

		void LoadKingScenery(short participantIndex);

		void ReleaseKingResources();

		void ChoreographKingLikeness(ushort eyes, ushort mouth, ushort advisorPose);

		ushort ShowKingDialog(ushort charactersAddress, ushort horizontalLocation, ushort verticalLocation);
	}
}
