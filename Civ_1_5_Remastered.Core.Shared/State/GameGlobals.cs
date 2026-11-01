using System.Collections.Generic;
using System.Linq;

namespace Civ_1_5_Remastered.Core.Shared.State
{
	public sealed partial class GameGlobals
	{
		public CityTurnVariables CityTurn { get; } = new CityTurnVariables();

		public MenuVariables Menu { get; } = new MenuVariables();

		public KingVariables King { get; } = new KingVariables();

		public PalaceVariables Palace { get; } = new PalaceVariables();

		public StoryVariables Story { get; } = new StoryVariables();

		public NewWorldVariables NewWorld { get; } = new NewWorldVariables();

		public SpaceshipVariables Spaceship { get; } = new SpaceshipVariables();

		public ReplayVariables Replay { get; } = new ReplayVariables();

		public MouseVariables Mouse { get; } = new MouseVariables();

		public StackVariables Stack { get; } = new StackVariables();

		public FileVariables Files { get; } = new FileVariables();

		public MapViewVariables MapView { get; } = new MapViewVariables();

		public ScreenVariables Screen { get; } = new ScreenVariables();

		public WritingVariables Writing { get; } = new WritingVariables();

		public ScoringVariables Scoring { get; } = new ScoringVariables();

		public SearchVariables Search { get; } = new SearchVariables();

		public IconVariables Icons { get; } = new IconVariables();

		public TurnVariables Turn { get; } = new TurnVariables();

		public IEnumerable<GlobalWord> All => Groups.SelectMany(group => group.Words);

		private IEnumerable<VariableGroup> Groups => [CityTurn, Menu, King, Palace, Story, NewWorld, Spaceship, Replay, Mouse, Stack, Files, MapView, Screen, Writing, Scoring, Search, Icons, Turn];
	}
}
