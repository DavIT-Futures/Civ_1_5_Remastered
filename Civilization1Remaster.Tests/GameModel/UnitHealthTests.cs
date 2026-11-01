using Civilization1Remaster.GameModel.Rules;

namespace Civilization1Remaster.Tests.GameModel
{
	public class UnitHealthTests
	{
		[Fact]
		public void FreshUnitsHaveFullHealth()
		{
			UnitHealth health = new UnitHealth();

			Assert.Equal(0, health.ObtainLostHitPoints(1, 2, 3));
			Assert.Equal(3, health.ObtainHitPoints(1, 2, 3, false));
			Assert.Equal(4, health.ObtainHitPoints(1, 2, 3, true));
			Assert.False(health.VerifyElite(1, 2, 3));
		}

		[Fact]
		public void DamageBelongsToTheUnitKind()
		{
			UnitHealth health = new UnitHealth();
			health.Assign(1, 2, 3, 2, false);

			Assert.Equal(2, health.ObtainLostHitPoints(1, 2, 3));
			Assert.Equal(1, health.ObtainHitPoints(1, 2, 3, false));
			Assert.Equal(0, health.ObtainLostHitPoints(1, 2, 4));
		}

		[Fact]
		public void HitPointsNeverDropBelowOne()
		{
			UnitHealth health = new UnitHealth();
			health.Assign(0, 0, 1, 99, false);

			Assert.Equal(CombatResolver.PeakStrikeMarks - 1, health.ObtainLostHitPoints(0, 0, 1));
			Assert.Equal(1, health.ObtainHitPoints(0, 0, 1, false));
		}

		[Fact]
		public void AceGetsFiveHitPoints()
		{
			UnitHealth health = new UnitHealth();
			health.Assign(2, 5, 7, 1, true);

			Assert.True(health.VerifyElite(2, 5, 7));
			Assert.Equal(5, health.ObtainFullHitPoints(2, 5, 7, false));
			Assert.Equal(4, health.ObtainHitPoints(2, 5, 7, false));
		}

		[Fact]
		public void Mending()
		{
			UnitHealth health = new UnitHealth();
			health.Assign(1, 1, 1, 3, false);

			Assert.Equal(1, health.MendAmount(1, 1, 1, 1));
			Assert.Equal(2, health.ObtainLostHitPoints(1, 1, 1));
			Assert.Equal(2, health.MendAmount(1, 1, 1, 5));
			Assert.Equal(0, health.MendAmount(1, 1, 1, 1));
			Assert.Equal(0, health.MendAmount(1, 1, 2, 1));
			Assert.Equal(0, health.MendAmount(1, 1, 1, 0));
		}

		[Fact]
		public void InvalidSlotsAreIgnored()
		{
			UnitHealth health = new UnitHealth();
			health.Assign(8, 0, 1, 2, true);
			health.Assign(0, 128, 1, 2, true);
			health.Wipe(-1, 0);

			Assert.Equal(0, health.ObtainLostHitPoints(8, 0, 1));
		}

		[Fact]
		public void StoreAndImportRoundTrip()
		{
			UnitHealth health = new UnitHealth();
			health.Assign(0, 0, 1, 1, false);
			health.Assign(7, 127, 25, 4, true);
			health.Assign(3, 64, 0, 2, true);

			MemoryStream sink = new MemoryStream();
			health.Store(sink);
			Assert.Equal(5 + (8 * 128 * 3), sink.Length);
			Assert.Equal("OCHP"u8.ToArray(), sink.ToArray().AsSpan(0, 4).ToArray());

			sink.Position = 0;
			UnitHealth copy = new UnitHealth();
			Assert.True(copy.Import(sink));

			Assert.Equal(1, copy.ObtainLostHitPoints(0, 0, 1));
			Assert.Equal(4, copy.ObtainLostHitPoints(7, 127, 25));
			Assert.True(copy.VerifyElite(7, 127, 25));
			Assert.Equal(2, copy.ObtainLostHitPoints(3, 64, 0));
			Assert.Equal(0, copy.ObtainLostHitPoints(3, 65, 0));
		}

		[Fact]
		public void ImportRejectsBadData()
		{
			UnitHealth health = new UnitHealth();
			health.Assign(1, 1, 1, 1, false);
			MemoryStream sink = new MemoryStream();
			health.Store(sink);
			byte[] good = sink.ToArray();

			Assert.False(new UnitHealth().Import(new MemoryStream(good.AsSpan(0, good.Length - 1).ToArray())));

			byte[] badSignature = (byte[])good.Clone();
			badSignature[0] = (byte)'X';
			Assert.False(new UnitHealth().Import(new MemoryStream(badSignature)));

			byte[] badEdition = (byte[])good.Clone();
			badEdition[4] = 2;
			Assert.False(new UnitHealth().Import(new MemoryStream(badEdition)));
		}

		[Fact]
		public void ImportClampsDamage()
		{
			UnitHealth health = new UnitHealth();
			health.Assign(0, 0, 1, 1, false);
			MemoryStream sink = new MemoryStream();
			health.Store(sink);
			byte[] content = sink.ToArray();
			content[5 + 1] = 200;

			UnitHealth copy = new UnitHealth();
			Assert.True(copy.Import(new MemoryStream(content)));
			Assert.Equal(CombatResolver.PeakStrikeMarks - 1, copy.ObtainLostHitPoints(0, 0, 1));
		}
	}
}
