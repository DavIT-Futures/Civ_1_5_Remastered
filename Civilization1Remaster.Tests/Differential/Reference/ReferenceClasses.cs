using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;

namespace Civilization1Remaster.Tests.Differential.Reference
{
	// The reference (oracle) classes: the original code of the rewritten functions, one generated file per function
	// (extract_reference.py). These parts give them what the original classes had.

	public abstract class ReferenceBase
	{
		protected readonly GameEngine owner;
		protected readonly CPU processor;
		protected readonly UnitMemory troops;

		protected ReferenceBase(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
			this.troops = new UnitMemory(owner.Processor);
		}
	}

	public sealed partial class UnitManagementReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class CityManagementReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class CityFoundingAndScoreReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class GameInitAndIntroReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class GovernmentAndDiplomacyReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class ScienceAndAdvisorsReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class GameStartupReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class SpaceshipScreenReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class StartGameMenuReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class GameReplayReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class MeetWithKingReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class CivilopediaReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class SystemServicesReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class HallOfFameReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class CommonUtilitiesReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class CityViewReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class PalaceReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class AIPlayerReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class MainIntroReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class GameMenusReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class NewCityScreenReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class ArmySelectionReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class GameLoadAndSaveReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class TextBlockRendererReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class TradeAndConquestReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class MapManagementReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class TurnEventsReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class InGameHelpReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class AdvisorReportsReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class UnitCombatReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class PlayerTurnReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class CivilizationInfoReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class CivilizationSchismReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class WorldMapReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class DiplomatActionsReference(GameEngine owner) : ReferenceBase(owner);
}
