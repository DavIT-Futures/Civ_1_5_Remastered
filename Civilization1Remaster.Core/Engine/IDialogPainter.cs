namespace Civilization1Remaster.Core.Engine
{
	public interface IDialogPainter
	{
		bool ChoicesIndentTokens { get; set; }

		bool ChoicesIndentTokenDown { get; set; }

		ushort ShowMenu(ushort charactersAddress, int horizontalLocation, int verticalLocation, ushort marker);

		ushort ShowMenu(string text, int horizontalLocation, int verticalLocation, ushort marker);

		void RunMenu(ushort charactersAddress, int horizontalLocation, int verticalLocation, ushort shownChoice);

		void FillBoxWithDoubleShadow(int horizontalLocation, int verticalLocation, int breadth, int altitude, ushort style);

		void DrawBox(int horizontalLocation, int verticalLocation, int breadth, int altitude, ushort style);

		void DrawShadowBox(int horizontalLocation, int verticalLocation, int breadth, int altitude, ushort style, ushort firstStyle);

		ushort ReadNavigationKey();
	}
}
