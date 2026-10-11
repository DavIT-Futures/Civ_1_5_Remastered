using Civilization1Remaster.Game.Rules;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class UnitHealthTests
	{
		private const int Fighters = 1;
		private const int Colonists = 0;

		[Fact]
		public void FreshUnitStartsUnhurt()
		{
			UnitHealth vitality = new UnitHealth();

			Assert.Equal(0, vitality.ObtainLostHitPoints(1, 5, Fighters));
			Assert.Equal(3, vitality.ObtainHitPoints(1, 5, Fighters, false));
			Assert.Equal(4, vitality.ObtainHitPoints(1, 5, Fighters, true));
		}

		[Fact]
		public void InjuriesPersistUntilMended()
		{
			UnitHealth vitality = new UnitHealth();
			vitality.Assign(1, 5, Fighters, 2, false);

			Assert.Equal(1, vitality.ObtainHitPoints(1, 5, Fighters, false));

			Assert.True(vitality.MendOne(1, 5, Fighters));
			Assert.Equal(2, vitality.ObtainHitPoints(1, 5, Fighters, false));

			Assert.True(vitality.MendOne(1, 5, Fighters));
			Assert.False(vitality.MendOne(1, 5, Fighters));
			Assert.Equal(3, vitality.ObtainHitPoints(1, 5, Fighters, false));
		}

		[Fact]
		public void AceUnitCarriesFiveHitpoints()
		{
			UnitHealth vitality = new UnitHealth();
			vitality.Assign(2, 7, Fighters, 1, true);

			Assert.True(vitality.VerifyElite(2, 7, Fighters));
			Assert.Equal(5, vitality.ObtainFullHitPoints(2, 7, Fighters, true));
			Assert.Equal(4, vitality.ObtainHitPoints(2, 7, Fighters, true));
		}

		[Fact]
		public void RecycledUnitSlotStartsUnhurt()
		{
			UnitHealth vitality = new UnitHealth();
			vitality.Assign(1, 5, Fighters, 2, true);

			// Another unit type in the same slot: the old unit was destroyed
			Assert.Equal(0, vitality.ObtainLostHitPoints(1, 5, Colonists));
			Assert.False(vitality.VerifyElite(1, 5, Colonists));

			vitality.Wipe(1, 5);
			Assert.Equal(0, vitality.ObtainLostHitPoints(1, 5, Fighters));
		}

		[Fact]
		public void PersistedAndRestored()
		{
			UnitHealth vitality = new UnitHealth();
			vitality.Assign(0, 0, Fighters, 1, false);
			vitality.Assign(7, 127, Colonists, 3, true);

			MemoryStream flow = new MemoryStream();
			vitality.Store(flow);
			flow.Position = 0;

			UnitHealth fetched = new UnitHealth();
			Assert.True(fetched.Import(flow));

			Assert.Equal(1, fetched.ObtainLostHitPoints(0, 0, Fighters));
			Assert.Equal(3, fetched.ObtainLostHitPoints(7, 127, Colonists));
			Assert.True(fetched.VerifyElite(7, 127, Colonists));
			Assert.False(fetched.VerifyElite(0, 0, Fighters));
		}

		[Fact]
		public void CorruptDataGetsSkipped()
		{
			UnitHealth vitality = new UnitHealth();
			vitality.Assign(0, 0, Fighters, 1, false);

			Assert.False(vitality.Import(new MemoryStream(new byte[] { 1, 2, 3 })));
			Assert.Equal(0, vitality.ObtainLostHitPoints(0, 0, Fighters));
		}
	}
}
