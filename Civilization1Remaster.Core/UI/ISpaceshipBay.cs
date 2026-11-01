namespace Civilization1Remaster.Core.UI
{
	public interface ISpaceshipBay
	{
		void DrawSpaceScenery();

		void LoadSpaceScenery();

		ushort DrawSpaceship(short participantIdx, short frames, short launchable);

		ushort ObtainSpaceshipPartValue(short participantIdx, ushort row, ushort column);

		void DrawStarField();

		ushort ComputeSpaceshipSuccess(short participantIdx, short partKind);

		void AttachSpaceshipPart(short participantIdx, ushort partKind);

		void ChooseNationToView();

		void FireSpaceship(short participantIdx);
	}
}
