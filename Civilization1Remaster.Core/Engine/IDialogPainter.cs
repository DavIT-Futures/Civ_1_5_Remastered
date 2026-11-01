namespace Civilization1Remaster.Core.Engine
{
	public interface IDialogPainter
	{
		bool ChoicesIndentTokens { get; set; }

		bool ChoicesIndentTokenDown { get; set; }

		ushort ShowMenu(ushort charsAddr, int horizLoc, int vertLoc, ushort marker);

		ushort ShowMenu(string text, int horizLoc, int vertLoc, ushort marker);

		void RunMenu(ushort charsAddr, int horizLoc, int vertLoc, ushort shownChoice);

		void FillBoxWithDoubleShadow(int horizLoc, int vertLoc, int breadth, int altitude, ushort style);

		void DrawBox(int horizLoc, int vertLoc, int breadth, int altitude, ushort style);

		void DrawShadowBox(int horizLoc, int vertLoc, int breadth, int altitude, ushort style, ushort style1);

		ushort ReadNavigationKey();
	}
}
