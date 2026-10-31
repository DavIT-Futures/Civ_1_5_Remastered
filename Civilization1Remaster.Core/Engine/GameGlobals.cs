using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Civilization1Remaster.Core.Engine
{
	/// <summary>
	/// The named global variables of the game (B4) as C# fields (B6.4), in groups. GameEngine.PrepareMachine attaches
	/// every variable over its address in the data segment, so the memory (and the saved games) see the same values.
	/// </summary>
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

		/// <summary>All the variables of all the groups</summary>
		public IEnumerable<GlobalWord> All => Groups.SelectMany(group => group.Words);

		private IEnumerable<VariableGroup> Groups => [CityTurn, Menu, King, Palace, Story, NewWorld, Spaceship, Replay, Mouse, Stack, Files, MapView, Screen, Writing, Scoring, Search, Icons, Turn];

		/// <summary>A group of variables: its GlobalWord fields are its variables</summary>
		public abstract class VariableGroup
		{
			internal IEnumerable<GlobalWord> Words => GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
				.Where(member => member.FieldType == typeof(GlobalWord))
				.Select(member => (GlobalWord)member.GetValue(this)!);
		}
	}
}
