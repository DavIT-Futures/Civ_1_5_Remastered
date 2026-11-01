namespace Civilization1Remaster.Core
{
	public abstract class GameComponent
	{
		protected const ushort CitizenPicturesAddress = 0x6e96;

		private UnitMemory? unitTable;

		protected GameComponent(GameEngine owner)
		{
			this.Owner = owner;
			this.Processor = owner.Processor;
		}

		protected GameEngine Owner { get; }
		protected CentralProcessor Processor { get; }

		protected UnitMemory Troops => this.unitTable ??= new UnitMemory(this.Processor);

		protected GameGlobals.CityTurnVariables CityTurn => this.Owner.Globals.CityTurn;

		protected GameGlobals.MenuVariables Menu => this.Owner.Globals.Menu;

		protected GameGlobals.KingVariables King => this.Owner.Globals.King;

		protected GameGlobals.PalaceVariables Palace => this.Owner.Globals.Palace;

		protected GameGlobals.StoryVariables Story => this.Owner.Globals.Story;

		protected GameGlobals.NewWorldVariables NewWorld => this.Owner.Globals.NewWorld;

		protected GameGlobals.SpaceshipVariables Spaceship => this.Owner.Globals.Spaceship;

		protected GameGlobals.ReplayVariables Replay => this.Owner.Globals.Replay;

		protected GameGlobals.MouseVariables Mouse => this.Owner.Globals.Mouse;

		protected GameGlobals.StackVariables Stack => this.Owner.Globals.Stack;

		protected GameGlobals.FileVariables Files => this.Owner.Globals.Files;

		protected GameGlobals.MapViewVariables MapView => this.Owner.Globals.MapView;

		protected GameGlobals.ScreenVariables Screen => this.Owner.Globals.Screen;

		protected GameGlobals.WritingVariables Writing => this.Owner.Globals.Writing;

		protected GameGlobals.ScoringVariables Scoring => this.Owner.Globals.Scoring;

		protected GameGlobals.SearchVariables Search => this.Owner.Globals.Search;

		protected GameGlobals.IconVariables Icons => this.Owner.Globals.Icons;

		protected GameGlobals.TurnVariables Turn => this.Owner.Globals.Turn;

		protected GameTables Tables => this.Owner.Tables;

		protected CivilizationRectangle DrawingBox => new CivilizationRectangle(this.Processor, CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, Screen.Box));

		protected ushort MultiplyWords(ushort multiplicand, ushort multiplier)
		{
			this.Processor.RegisterAccumulator.Word = multiplicand;
			this.Processor.MultiplySignedWord(this.Processor.RegisterAccumulator, this.Processor.RegisterData, multiplier);
			return this.Processor.RegisterAccumulator.Word;
		}

		protected ushort DivideWords(ushort dividend, ushort divisor)
		{
			this.Processor.RegisterAccumulator.Word = dividend;
			this.Processor.SignExtendWord(this.Processor.RegisterAccumulator, this.Processor.RegisterData);
			this.Processor.DivideSignedWord(this.Processor.RegisterAccumulator, this.Processor.RegisterData, divisor);
			return this.Processor.RegisterAccumulator.Word;
		}

		protected GameMessage Message(string text) => GameMessage.Start(this.Owner, text);

		protected GameMessage Message(ushort textAddress) => GameMessage.Start(this.Owner, textAddress);

		protected GameMessage EmptyMessage() => GameMessage.Empty(this.Owner);

		protected ushort CitizenPicture(int kind) => this.Processor.PullGlobalWord((ushort)((kind << 1) + CitizenPicturesAddress));
	}
}
