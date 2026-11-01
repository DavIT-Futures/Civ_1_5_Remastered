using System;

namespace Civilization1Remaster.GameModel.State
{
	[Flags]
	public enum UnitStatus
	{
		None = 0,
		Sentry = 1,
		Fortifying = 4,
		Fortified = 8,
		FirstUnknown = 0x10,
		Veteran = 0x20,
		SettlerBuildRoadOrRail = 2,
		SettlerBuildIrrigation = 0x40,
		SettlerBuildMineOrForest = 0x80,
		SettlerBuildFortress = 0xc0,
		SettlerCleanPollution = 0x82,
		SettlerBuildMask = 0xc2
	}
}
