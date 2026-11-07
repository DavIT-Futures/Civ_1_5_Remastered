namespace Civ_1_5_Remastered.Core.UI.Help
{
	/// <summary>Remastered "Game Concept" entries of the Civilopedia that are not part of the original data files.</summary>
	public static class CivilopediaConcepts
	{
		public const ushort DiplomacyIndex = 100;
		public const ushort WarIndex = 101;
		public const ushort AutomationIndex = 102;

		private static readonly string[] DiplomacyPages =
		[
			"Every civilization remembers how you treated it. Its attitude is never shown as a number, but the WAR ADVISOR (F12) estimates it as Friendly, Neutral, Wary, Hostile or Vengeful. Press 1-6 there for the reasons and the forecast of the next audience; with an embassy press T to talk to their ruler at once (once per turn).",
			"A ruler audience happens when your units meet theirs (at most once every 16 turns) or when your Diplomat visits their city. An embassy (Diplomat: Establish Embassy) reveals their treaties, vendettas and secret plans to break the peace.",
			"ATTITUDE starts from the character of the leader (aggressive leaders are worse) and changes with:\n+1 or +2 for each strong rival they are at war with,\n-1 if your army is weaker than theirs,\n-2 while they hold a VENDETTA against you,\n+1 after 32 turns of peace, +2 after 60 turns.\n\nWhen their attitude is good they drop their demands. Some wonders remove hostility completely. A ruler is never hostile when your army is more than three times stronger.",
			"HOSTILITY comes from armies facing each other on a shared continent, from nuclear weapons, or from a Vendetta. A hostile ruler asks you to join a war against a common enemy, then demands TRIBUTE (up to 1000 gold, more on harder levels, double under a Vendetta) or an advance. Refusing means war.\n\nTroops of yours near their cities make them afraid instead: they offer gold or advances to keep the peace.",
			"GETTING GOLD AND SCIENCE:\nExchange: offered when you know at least as many advances as they do and nobody is hostile.\nPeace gifts: when your troops threaten their cities, refuse their peace offer and they offer an advance, gold or, with one city left, everything. Impossible as a Republic or Democracy: the Senate signs.\nTribute under peace: once per 16 turns (not as Republic or Democracy). Without troops near them it only provokes war.",
			"THE PEACE COUNTER of every pair of civilizations grows in steps of 4 turns and is shown by the War Advisor. Long peace builds trust (+1 attitude after 32 turns, +2 after 60).\n\nA treaty protects you for at least 16 turns: a ruler who secretly plans to break it must wait, and drops the plan when your army is 1.5 times stronger (unless he holds a Vendetta). Signing peace or paying in an audience never starts a secret plan. Any war resets the counter.",
			"VENDETTA: attacking a civilization you have a treaty with, or inciting revolt in its cities, causes a 48 turn Vendetta; repeat offences add more (up to 60 turns). Accepting an alliance against a third civilization gives it a 24 turn Vendetta. Rebels of a civil war start with 60 turns.\n\nWhile it lasts: double tribute, hostility unless you are three times stronger, no peace gifts, and treaties tend to be broken.",
			"ENDING A VENDETTA:\n- it fades 4 turns for every 4 turns of peace without fighting (half as fast in a war without battles),\n- a new government (revolution, lost capital) forgets it at once,\n- paying tribute halves it,\n- REPARATIONS, asked by their emissary: 100 gold per 4 turns left, times 67% (Chieftain) up to 200% (Emperor), 300 to 3000 gold. Or your best advance plus half the gold. Expensive on purpose: sneak attacks must not pay.",
		];

		private static readonly string[] AutomationPages =
		[
			"PRODUCTION QUEUE: in the city screen press Q (or click between CHANGE and BUY) to plan up to five items that will be built after the current one. When the city finishes something it starts the next item of the queue without asking. Items that can no longer be built (an improvement already present, a wonder finished elsewhere) are skipped. Remove an item by choosing it in the queue menu.",
			"AUTOMATED SETTLERS: give Settlers the order A (or Orders: Automate) and choose a mode:\n- Build roads only: roads on desert, plains and grassland around our cities.\n- Improve everything: irrigation, mines and roads; a mine may replace irrigation and the other way round.\n- Improve, keep existing work: never destroys an irrigation or a mine.\n- Clean up pollution.\n- Connect cities: roads, then railroads once known.",
			"JUNGLE AND SWAMP: improving Settlers drain them into grassland. Jungle with gems is first turned into forest, so the bonus becomes game. Swamp with oil keeps its bonus and only gets a road.",
			"GOTO: units now take the fastest route over the whole map: railroads are almost free and never left without need, roads cost a third of a move, other tiles their terrain cost. Among equally fast routes the shortest and straightest one wins. Enemy units and foreign cities are avoided, unexplored land counts as one move.",
			"Automated Settlers pick the most useful tile nearby, walk there and work until it is done, then look for the next job. Pollution is cleaned first. Two automated Settlers never choose the same tile. When there is nothing left to do they report it and wait for orders. Selecting the unit yourself (click its stack) cancels the automation. Automation and queues are saved with the game in a .EXT file.",
		];

		private static readonly string[] WarPages =
		[
			"WAR starts when either side attacks, when a hostile ruler's demands are refused, or when a treaty is cancelled. Attacking a civilization you have a peace treaty with is a SNEAK ATTACK, the worst crime in diplomacy: before you do it, the game tells you how long the Vendetta will last and how much gold the reparations will cost. A Republic or Democracy needs the Senate's consent, which is never given.",
			"OLD RULES: a single sneak attack left a permanent Vendetta that only a change of their government removed. Every later audience made them hostile again, and even after signing peace they secretly planned to break it, with a 1 in 3 chance each turn. One attack meant endless war.\n\nREMASTERED RULES fix this in five ways, described on the next page.",
			"1. A Vendetta expires (48 turns, see Diplomacy).\n2. Reparations buy forgiveness, at a high price.\n3. A treaty lasts at least 16 turns, plans to break it are dropped when you are much stronger, and paying or signing in an audience never starts such a plan.\n4. The War Advisor (F12) shows peace length, attitude, Vendetta, reparations and, with an embassy, their war plans.\n5. The peace counter rewards long peace with better attitude.",
			"STACKS: in the original game a stack outside a city or fortress died completely when its defender lost. Now, on land, only the defender dies; every other unit of the stack loses 2 hit points and only units that cannot take it die. Healthy veterans and elite units survive. Stacks at sea (ships and their cargo) still sink together.",
			"COMPUTER WARS: computer civilizations make peace when they meet unless one of them is much stronger on a shared continent or has nuclear weapons. Republics and Democracies never start wars, and some ancient wonders keep their owner out of computer wars. A war fades when the two sides lose contact for 16 turns. Their own Vendettas against each other block new treaties until they expire.",
			"ADVICE:\n- keep a strong army: nobody stays hostile when you are three times stronger,\n- station defenders near their border cities to make them pay for peace,\n- build embassies to see who plans a war,\n- think twice before joining an alliance: the enemy will hold a Vendetta,\n- if you must strike first, have the reparations ready or take their capital.",
		];

		public static bool IsSpecial(ushort index) => index == DiplomacyIndex || index == WarIndex || index == AutomationIndex;

		public static string Title(ushort index) => index switch
		{
			WarIndex => "War",
			AutomationIndex => "Automation",
			_ => "Diplomacy",
		};

		public static string[] Pages(ushort index) => index switch
		{
			WarIndex => WarPages,
			AutomationIndex => AutomationPages,
			_ => DiplomacyPages,
		};

		/// <summary>Original concept entries with the same name are hidden, so the remastered page replaces them.</summary>
		public static bool Replaces(string name) =>
			string.Equals(name, "Diplomacy", System.StringComparison.OrdinalIgnoreCase) ||
			string.Equals(name, "War", System.StringComparison.OrdinalIgnoreCase) ||
			string.Equals(name, "Automation", System.StringComparison.OrdinalIgnoreCase);
	}
}
