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

	public sealed partial class UnitRosterReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class CitiesReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class SettlementAndScoringReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class WorldBuilderReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class ForeignAffairsReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class ResearchOfficeReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class GameLauncherReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class SpaceshipBayReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class NewGameSetupReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class ReplayViewerReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class DialogPainterReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class RulerAudienceReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class EncyclopediaReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class PlatformServicesReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class FameRecordsReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class GameHelpersReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class CityPanoramaReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class PalaceBuilderReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class ComputerPlayerReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class IntroSequenceReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class MenuBarReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class CityNamingScreenReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class UnitPickerReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class SaveGameStoreReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class TextBlockPainterReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class CityCaptureReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class MapServiceReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class WorldEventsReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class HelpPopupsReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class AdvisorScreensReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class BattlesReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class TurnControllerReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class RivalOverviewReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class CivilWarReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class WorldStatisticsReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class DiplomatMissionsReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class RoutePlannerReference(GameEngine owner) : ReferenceBase(owner);
	public sealed partial class GazetteReference(GameEngine owner) : ReferenceBase(owner);
}
