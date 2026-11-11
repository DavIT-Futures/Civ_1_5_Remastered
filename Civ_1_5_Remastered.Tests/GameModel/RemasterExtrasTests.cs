using Civ_1_5_Remastered.GameModel.Rules;
using Civ_1_5_Remastered.GameModel.State;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	public class RemasterExtrasTests
	{
		[Fact]
		public void QueueKeepsOrderAndLimit()
		{
			RemasterExtras extras = new RemasterExtras();
			City town = CreateTown(owner: 1, label: 7);

			for (sbyte item = 1; item <= RemasterExtras.QueueLength; item++)
			{
				Assert.True(extras.Enqueue(3, town, (sbyte)-item));
			}

			Assert.False(extras.Enqueue(3, town, 4));
			Assert.Equal(RemasterExtras.QueueLength, extras.QueueCount(3, town));

			extras.RemoveAt(3, town, 1);
			Assert.Equal(-1, extras.Dequeue(3, town));
			Assert.Equal(-3, extras.Dequeue(3, town));
			Assert.Equal(2, extras.QueueCount(3, town));
		}

		[Fact]
		public void QueueOfLostOrRefoundedCityIsIgnored()
		{
			RemasterExtras extras = new RemasterExtras();
			City town = CreateTown(owner: 1, label: 7);
			extras.Enqueue(5, town, 2);

			town.ParticipantIndex = 3;
			Assert.Equal(0, extras.QueueCount(5, town));

			town.ParticipantIndex = 1;
			town.LabelIndex = 9;
			Assert.Equal(0, extras.QueueCount(5, town));

			town.ConditionMarker = 0xff;
			Assert.Equal(RemasterExtras.EmptySlot, extras.Dequeue(5, town));
		}

		[Fact]
		public void AutomationIsBoundToUnitKind()
		{
			RemasterExtras extras = new RemasterExtras();

			extras.AssignAutomation(1, 10, 0, SettlerAutomation.ConnectCities);

			Assert.Equal(SettlerAutomation.ConnectCities, extras.AutomationMode(1, 10, 0));
			Assert.Equal(SettlerAutomation.None, extras.AutomationMode(1, 10, 5));

			extras.ClearAutomation(1, 10);
			Assert.Equal(SettlerAutomation.None, extras.AutomationMode(1, 10, 0));
		}

		[Fact]
		public void StoreAndImportRoundTrip()
		{
			RemasterExtras extras = new RemasterExtras();
			City town = CreateTown(owner: 2, label: 4);
			extras.Enqueue(9, town, -12);
			extras.Enqueue(9, town, SpecialProjects.Wealth);
			extras.AssignAutomation(2, 77, 0, SettlerAutomation.ImproveKeepExisting);

			using MemoryStream flow = new MemoryStream();
			extras.Store(flow);
			flow.Position = 0;

			RemasterExtras copy = new RemasterExtras();
			Assert.True(copy.Import(flow));
			Assert.Equal(-12, copy.QueueItem(9, town, 0));
			Assert.Equal(SpecialProjects.Wealth, copy.QueueItem(9, town, 1));
			Assert.Equal(SettlerAutomation.ImproveKeepExisting, copy.AutomationMode(2, 77, 0));
		}

		[Fact]
		public void AutomationRemembersItsCity()
		{
			RemasterExtras extras = new RemasterExtras();

			extras.AssignAutomation(1, 4, 0, SettlerAutomation.ImproveAll, 17);
			extras.AssignAutomation(1, 5, 0, SettlerAutomation.RoadsOnly);

			Assert.Equal(17, extras.AutomationTown(1, 4, 0));
			Assert.Equal(RemasterExtras.Anywhere, extras.AutomationTown(1, 5, 0));

			using MemoryStream flow = new MemoryStream();
			extras.Store(flow);
			flow.Position = 0;

			RemasterExtras copy = new RemasterExtras();
			Assert.True(copy.Import(flow));
			Assert.Equal(17, copy.AutomationTown(1, 4, 0));

			copy.ClearAutomation(1, 4);
			Assert.Equal(RemasterExtras.Anywhere, copy.AutomationTown(1, 4, 0));
		}

		[Fact]
		public void FirstEditionFilesStillLoad()
		{
			byte[] content = new byte[4 + 1 + (RemasterExtras.TownTally * (2 + RemasterExtras.QueueLength)) +
				(RemasterExtras.ParticipantTally * RemasterExtras.TroopTally * 2)];
			"OCRX"u8.ToArray().CopyTo(content, 0);
			content[4] = 1;

			for (int townIndex = 0; townIndex < RemasterExtras.TownTally; townIndex++)
			{
				int place = 5 + (townIndex * (2 + RemasterExtras.QueueLength));
				content[place] = 0xff;

				for (int index = 0; index < RemasterExtras.QueueLength; index++)
				{
					content[place + 2 + index] = (byte)RemasterExtras.EmptySlot;
				}
			}

			int automation = 5 + (RemasterExtras.TownTally * (2 + RemasterExtras.QueueLength)) + (((2 * RemasterExtras.TroopTally) + 9) * 2);
			content[automation] = 0;
			content[automation + 1] = (byte)SettlerAutomation.CleanPollution;

			RemasterExtras extras = new RemasterExtras();
			Assert.True(extras.Import(new MemoryStream(content)));
			Assert.Equal(SettlerAutomation.CleanPollution, extras.AutomationMode(2, 9, 0));
			Assert.Equal(RemasterExtras.Anywhere, extras.AutomationTown(2, 9, 0));
		}

		[Fact]
		public void ImportRejectsForeignData()
		{
			RemasterExtras extras = new RemasterExtras();

			Assert.False(extras.Import(new MemoryStream(new byte[] { 1, 2, 3 })));
		}

		[Fact]
		public void StackSplashSparesHealthyUnits()
		{
			Assert.True(CombatResolver.FallsWithStack(1));
			Assert.True(CombatResolver.FallsWithStack(2));
			Assert.False(CombatResolver.FallsWithStack(3));
		}

		[Fact]
		public void TheWorldIsKeptWithTheGame()
		{
			RemasterExtras extras = new RemasterExtras { World = 2 };
			using MemoryStream flow = new MemoryStream();
			extras.Store(flow);
			flow.Position = 0;

			RemasterExtras loaded = new RemasterExtras();
			Assert.True(loaded.Import(flow));
			Assert.Equal(2, loaded.World);

			loaded.WipeAll();
			Assert.Equal(0, loaded.World);
		}

		private static City CreateTown(short owner, byte label) => new City
		{
			ParticipantIndex = owner,
			LabelIndex = label,
			ConditionMarker = 0,
		};
	}
}
