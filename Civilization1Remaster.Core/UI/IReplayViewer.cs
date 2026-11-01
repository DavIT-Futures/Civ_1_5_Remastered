namespace Civilization1Remaster.Core.UI
{
	/// <summary>The operations of ReplayViewer that the other parts of the game use</summary>
	public interface IReplayViewer
	{
		/// <summary>
		/// The replay of the game (the events in RerunContent): the mode 0x680a is asked (dialog 0x3fb0; 4 without asking
		/// when 0x680e, at the end of the game): 1 slow, 2 a key per event (Esc stops), 3 into replay.txt, 4 only the
		/// destruction screens. The leaders are set as at the start of the game (the identity bits) and put back afterwards,
		/// the map is drawn in miniature, and every event written at the top (and its city on the map). Asked again until
		/// mode 3 or more, or none.
		/// </summary>
		void ShowReplay();

		/// <summary>
		/// Shows the line of a replay event (the text at TextBuffer.Address) at the top of the screen, in capitals for the human's events
		/// (0x6810), and writes it with CR LF to the replay file (0x680c) in mode 3 of 0x680a; nothing in mode 4. The text is
		/// drawn, then drawn again shortened to 312 pixels.
		/// </summary>
		void DrawReplayPreamble();

		/// <summary>
		/// The year shown for the round of the replay, as the original computes it: 1750 + 2 x the round up to round 250, after
		/// it 1850 + what is left after 250 and up to three times 50
		/// </summary>
		ushort ObtainReplayYear(ushort round);

		/// <summary>
		/// Puts the city into the first free entry (first column 0xffff) of the replay city table (segment 0x3725: three word
		/// columns of 128 entries at 0, 0x100 and 0x200)
		/// </summary>
		void AssignReplayCityContent(ushort content, ushort horizLoc, ushort vertLoc);

		/// <summary>
		/// Sets the first column of the first entry of the replay city table whose second and third columns are horizLoc and vertLoc
		/// </summary>
		void AssignReplayCityContentAlt(ushort content, ushort horizLoc, ushort vertLoc);

		/// <summary>
		/// The entry of the replay city table nearest to the position; its distance goes to 0x6812 (999 when none is nearer).
		/// Like the original, without an entry nearer than 999 the result is what was left in the stack frame (local 4), so the
		/// frame stays, and its locals get the values the original leaves there.
		/// </summary>
		/// <returns>Entry, also in AX</returns>
		ushort ComputeDistanceToReplayCity(int horizLoc, int vertLoc);

		/// <summary>
		/// Unless 0x680a is 4: colours the land tiles of the replay map around the position (5 tiles in every direction, columns
		/// 0 - 79, rows 2 - 48) by the nearest replay city: grey (8) when it is 6 or more tiles away or on another continent,
		/// else 7 or 15 (the city's player is 1 or not) on its own tile, else the colour of its player (0x1946). The stack frame
		/// stays: ComputeDistanceToReplayCity reads what is left below it, so it must be called at the same depth as in the original.
		/// </summary>
		void DrawReplayMapTile(int horizLoc, int vertLoc);

		/// <summary>
		/// Writes the map into the replay file (handle 0x680c) as text, a line of 80 characters and CR LF for each of the 50 rows:
		/// " " ocean, "." land, "+" a city ("*" with city walls, improvement bit 1) followed by its name (as far as the line goes)
		/// </summary>
		void DumpReplayCities();

		/// <summary>
		/// The names of the civilization of the player for the replay: its name (leader record 0x1502 + 0x3a * leader) into the
		/// text at 0x1982 + 2 * player, its plural (0x14f2 + ..., or the name with "s" when empty) into the text at 0x1992 + 2 * player.
		/// The plural is made in a buffer in the stack frame, like in the original.
		/// </summary>
		void ObtainReplayPlayerLabel(short participantIdx);

		/// <summary>
		/// The end of a conquest victory: the replay of the game (0x680e set meanwhile, the 14 bytes at 0x3772:0x37c4 cleared to
		/// 0xff), the leader's music, "The entire world hails ... the CONQUEROR!" until a key or click, then the map screen
		/// again. The texts have a shadow, in other colours with the colour cycles (0xd762).
		/// The stack frame stays: a called function leaves BP changed, and the original ends with the stack that gives.
		/// </summary>
		void ShowConquerorScreen();

		/// <summary>
		/// The screen of a destroyed civilization: its leader (king00.pic with the number 0x3938 + ChiefVisuals, kink on EGA
		/// for leaders 0 and 3) looks out of the wall of the fallen leaders (0x3772:0x37c4, 14 places from slam2.pic, slag2.pic on
		/// EGA) and says his last words; the leader is put into the wall at place 0xb1d4 (the places of the two rows taken in
		/// turn), then "&lt;year&gt;: &lt;the human civilization&gt; destroy the &lt;civilization&gt;!" (the second line overwrites "the ").
		/// The file names and pictures are in the stack frame, like in the original.
		/// </summary>
		void ShowCivilizationDestroyedScreen(short participantIdx, ushort year);
	}
}
