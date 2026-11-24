"""Re-orchestrates a piano-style MIDI anthem into an orchestral arrangement.

The Polish anthem (poland_raw.mid) was a piano reduction played twice in unison, by a trombone and a string
ensemble, so every SoundFont made it sound the same. This script keeps the notes and the timing of one copy and
gives each voice its own instruments: the top voice to trumpet and violins, the inner voices to horns and soft
strings, the bass to contrabass, tuba and timpani, each with its own pan, reverb and chorus.

Usage: python orchestrate_anthem.py SOURCE.mid TARGET.mid   (needs: pip install mido)
"""

import sys

import mido

GROUP_WINDOW = 24  # ticks: notes starting this close together belong to one chord

# channel: (program, volume, pan, reverb, chorus)
LAYOUT = {
    0: (56, 100, 54, 70, 10),   # trumpet, melody
    1: (40, 96, 40, 90, 40),    # violin, melody
    2: (60, 92, 84, 90, 20),    # french horn, inner voices
    3: (49, 80, 30, 100, 50),   # string ensemble 2, inner voices
    4: (43, 104, 70, 80, 10),   # contrabass, bass
    5: (58, 80, 64, 70, 0),     # tuba, bass
    6: (47, 90, 64, 100, 0),    # timpani, bass accents
}

# voice: [(channel, velocity scale, transpose)]
VOICES = {
    "melody": [(0, 0.95, 0), (1, 0.9, 0)],
    "inner": [(2, 0.8, 0), (3, 0.75, 0)],
    "bass": [(4, 0.95, 0), (5, 0.7, 0)],
}


def read_notes(track):
    now = 0
    held = {}
    notes = []
    for message in track:
        now += message.time
        if message.type == "note_on" and message.velocity > 0:
            held[message.note] = (now, message.velocity)
        elif message.type in ("note_off", "note_on") and message.note in held:
            start, velocity = held.pop(message.note)
            notes.append((start, max(now - start, 1), message.note, velocity))
    notes.sort()
    return notes


def chords(notes):
    group = []
    for note in notes:
        if group and note[0] - group[0][0] > GROUP_WINDOW:
            yield group
            group = []
        group.append(note)
    if group:
        yield group


def orchestrate(notes):
    events = []
    for chord in chords(notes):
        top = max(chord, key=lambda note: note[2])
        bottom = min(chord, key=lambda note: note[2])
        for note in chord:
            start, length, pitch, velocity = note
            if note is top and pitch >= 60:
                voice = "melody"
            elif note is bottom and pitch < 50:
                voice = "bass"
            else:
                voice = "inner"
            for channel, scale, transpose in VOICES[voice]:
                events.append((start, length, channel, pitch + transpose, velocity * scale))
            if voice == "bass" and len(chord) >= 3:
                events.append((start, min(length, 180), 6, pitch + 12 if pitch < 41 else pitch, velocity * 0.7))
    return events


def write(source, events, target):
    result = mido.MidiFile(type=1, ticks_per_beat=source.ticks_per_beat)
    result.tracks.append(source.tracks[0].copy())
    for channel, (program, volume, pan, reverb, chorus) in LAYOUT.items():
        track = mido.MidiTrack()
        track.append(mido.MetaMessage("track_name", name=f"voice {channel}", time=0))
        track.append(mido.Message("program_change", channel=channel, program=program, time=0))
        for control, value in ((7, volume), (10, pan), (91, reverb), (93, chorus), (11, 127)):
            track.append(mido.Message("control_change", channel=channel, control=control, value=value, time=0))
        timeline = []
        for start, length, event_channel, pitch, velocity in events:
            if event_channel == channel:
                loudness = max(1, min(127, round(velocity)))
                timeline.append((start, 1, mido.Message("note_on", channel=channel, note=pitch, velocity=loudness)))
                timeline.append((start + length, 0, mido.Message("note_off", channel=channel, note=pitch, velocity=0)))
        timeline.sort(key=lambda entry: (entry[0], entry[1]))
        now = 0
        for moment, _, message in timeline:
            track.append(message.copy(time=moment - now))
            now = moment
        track.append(mido.MetaMessage("end_of_track", time=0))
        result.tracks.append(track)
    result.save(target)


def main():
    source = mido.MidiFile(sys.argv[1])
    notes = next(read_notes(track) for track in source.tracks if any(m.type == "note_on" for m in track))
    write(source, orchestrate(notes), sys.argv[2])


if __name__ == "__main__":
    main()
