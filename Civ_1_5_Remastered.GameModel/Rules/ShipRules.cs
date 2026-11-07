namespace Civ_1_5_Remastered.GameModel.Rules
{
	/// <summary>Remastered naval rules: civilian ships, warships and the Frigate in between.</summary>
	public static class ShipRules
	{
		public const int Trireme = 0x10;
		public const int Sail = 0x11;
		public const int Frigate = 0x12;
		public const int Ironclad = 0x13;
		public const int Cruiser = 0x14;
		public const int Battleship = 0x15;
		public const int Transport = 0x18;

		/// <summary>Frigate price in tens of shields: 50 instead of the original 40, it is a warship now.</summary>
		public const int FrigatePrice = 5;

		/// <summary>Civilian ships (Trireme, Sail, Transport) never make a Republic or Democracy unhappy.</summary>
		public static bool IsCivilianShip(int kind) => kind is Trireme or Sail or Transport;
	}
}
