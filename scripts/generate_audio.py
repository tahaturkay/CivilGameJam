import math
import struct
import wave
import random
import os

SAMPLE_RATE = 44100

def clamp(val, min_v=-1.0, max_v=1.0):
    return max(min_v, min(max_v, val))

def save_wav(filename, samples, sample_rate=SAMPLE_RATE):
    # Normalize to avoid clipping
    max_peak = max(abs(s) for s in samples) if samples else 1.0
    if max_peak > 0.98:
        scale = 0.95 / max_peak
        samples = [s * scale for s in samples]
    else:
        # Boost quiet sounds moderately
        if max_peak > 0.05 and max_peak < 0.7:
            scale = 0.85 / max_peak
            samples = [s * scale for s in samples]

    with wave.open(filename, 'w') as wf:
        wf.setnchannels(1)  # Mono for simplicity & Unity efficiency
        wf.setsampwidth(2)  # 16 bit
        wf.setframerate(sample_rate)
        raw_data = bytearray()
        for s in samples:
            int_val = int(clamp(s) * 32767)
            raw_data.extend(struct.pack('<h', int_val))
        wf.writeframes(raw_data)
    print(f"Generated: {filename} ({len(samples)/sample_rate:.2f}s)")

# Instrument Synthesizers
def marimba_tone(freq, t, dur):
    if t > dur: return 0.0
    # Natural strike: transient + fundamental + overtone
    env = math.exp(-t * 6.0)
    sig = 0.7 * math.sin(2 * math.pi * freq * t)
    sig += 0.25 * math.sin(2 * math.pi * freq * 2.76 * t) * math.exp(-t * 14.0)
    sig += 0.15 * math.sin(2 * math.pi * freq * 5.4 * t) * math.exp(-t * 25.0)
    return sig * env

def bell_tone(freq, t, dur):
    if t > dur: return 0.0
    env = math.exp(-t * 3.5)
    sig = 0.6 * math.sin(2 * math.pi * freq * t)
    sig += 0.3 * math.sin(2 * math.pi * freq * 2.0 * t) * math.exp(-t * 5.0)
    sig += 0.15 * math.sin(2 * math.pi * freq * 3.01 * t) * math.exp(-t * 8.0)
    sig += 0.1 * math.sin(2 * math.pi * freq * 4.2 * t) * math.exp(-t * 12.0)
    return sig * env

def bass_tone(freq, t, dur):
    if t > dur: return 0.0
    env = math.exp(-t * 3.0)
    sig = 0.8 * math.sin(2 * math.pi * freq * t)
    sig += 0.3 * math.sin(2 * math.pi * freq * 2 * t)
    return sig * env

def kick_drum(t):
    if t > 0.3: return 0.0
    f = 140 * math.exp(-t * 25.0) + 40
    env = math.exp(-t * 18.0)
    return math.sin(2 * math.pi * f * t) * env

def snare_rim(t):
    if t > 0.2: return 0.0
    env = math.exp(-t * 22.0)
    noise = (random.random() * 2.0 - 1.0) * env * 0.4
    body = math.sin(2 * math.pi * 220 * t) * math.exp(-t * 30.0) * 0.4
    return noise + body

def hihat(t):
    if t > 0.08: return 0.0
    env = math.exp(-t * 45.0)
    noise = (random.random() * 2.0 - 1.0) * env * 0.25
    return noise

# 1. Ana Ekran Müziği (~40 sn loop demo)
def gen_main_menu(out_path):
    dur = 40.0
    n_samples = int(dur * SAMPLE_RATE)
    samples = [0.0] * n_samples
    bpm = 88.0
    beat_dur = 60.0 / bpm
    
    # Chords (C - G/B - Am - F)
    prog = [
        [261.63, 329.63, 392.00], # C
        [246.94, 293.66, 392.00], # G
        [220.00, 261.63, 329.63], # Am
        [174.61, 220.00, 349.23], # F
    ]
    melody = [
        523.25, 587.33, 659.25, 783.99, # C5 D5 E5 G5
        659.25, 587.33, 523.25, 392.00,
        440.00, 523.25, 659.25, 587.33,
        523.25, 440.00, 392.00, 329.63
    ]
    
    # Warm pad chords & gentle melody
    for i in range(n_samples):
        t = i / SAMPLE_RATE
        cur_bar = int(t / (beat_dur * 4)) % len(prog)
        bar_t = t % (beat_dur * 4)
        chord = prog[cur_bar]
        
        # Soft ambient wave / pad
        sig = 0.0
        for f in chord:
            sig += 0.08 * math.sin(2 * math.pi * f * t) * (0.8 + 0.2 * math.sin(2 * math.pi * 0.5 * t))
            sig += 0.04 * math.sin(2 * math.pi * (f*0.5) * t) # warm sub
        
        # Gentle sea breeze noise
        breeze = (random.random() * 2.0 - 1.0) * (0.015 + 0.01 * math.sin(2 * math.pi * 0.2 * t))
        samples[i] = sig + breeze

    # Add gentle melody notes
    note_idx = 0
    t_note = 0.0
    while t_note < dur - 1.0:
        freq = melody[note_idx % len(melody)]
        dur_note = beat_dur
        # Render marimba/bell note into samples
        start_sample = int(t_note * SAMPLE_RATE)
        for s_idx in range(int(dur_note * 2 * SAMPLE_RATE)):
            idx = start_sample + s_idx
            if idx >= n_samples: break
            t_loc = s_idx / SAMPLE_RATE
            samples[idx] += 0.22 * marimba_tone(freq, t_loc, dur_note * 1.5)
        note_idx += 1
        t_note += beat_dur

    save_wav(out_path, samples)

# 2. Ana Oyun Müziği (~48 sn loop demo)
def gen_gameplay(out_path):
    dur = 48.0
    n_samples = int(dur * SAMPLE_RATE)
    samples = [0.0] * n_samples
    bpm = 104.0
    beat_dur = 60.0 / bpm
    
    # Bassline & groove
    bass_notes = [130.81, 130.81, 164.81, 174.61, 196.00, 174.61, 146.83, 130.81] # C3, E3, F3, G3...
    
    # Place drums and bass
    total_beats = int(dur / beat_dur)
    for b in range(total_beats):
        t_beat = b * beat_dur
        start_s = int(t_beat * SAMPLE_RATE)
        # Kick on 0 and 2
        if b % 2 == 0:
            for s in range(int(0.25 * SAMPLE_RATE)):
                idx = start_s + s
                if idx < n_samples:
                    samples[idx] += 0.35 * kick_drum(s / SAMPLE_RATE)
        # Snare on 1 and 3
        if b % 2 == 1:
            for s in range(int(0.2 * SAMPLE_RATE)):
                idx = start_s + s
                if idx < n_samples:
                    samples[idx] += 0.25 * snare_rim(s / SAMPLE_RATE)
        # Hihat on every eighth
        for s in range(int(0.08 * SAMPLE_RATE)):
            idx = start_s + s
            if idx < n_samples:
                samples[idx] += 0.12 * hihat(s / SAMPLE_RATE)
        # Off-beat hihat
        start_half = start_s + int(beat_dur * 0.5 * SAMPLE_RATE)
        for s in range(int(0.08 * SAMPLE_RATE)):
            idx = start_half + s
            if idx < n_samples:
                samples[idx] += 0.08 * hihat(s / SAMPLE_RATE)

        # Bass tone
        b_note = bass_notes[b % len(bass_notes)]
        for s in range(int(beat_dur * SAMPLE_RATE)):
            idx = start_s + s
            if idx < n_samples:
                samples[idx] += 0.25 * bass_tone(b_note, s / SAMPLE_RATE, beat_dur)

    # Island melody (Dorfromantik/Islanders marimba feel)
    island_riff = [
        523.25, 659.25, 783.99, 659.25, 880.00, 783.99, 659.25, 587.33,
        523.25, 587.33, 659.25, 523.25, 392.00, 440.00, 523.25, 0.0
    ]
    t_riff = 0.0
    r_idx = 0
    step = beat_dur * 0.5
    while t_riff < dur - 1.0:
        f = island_riff[r_idx % len(island_riff)]
        if f > 0:
            start_s = int(t_riff * SAMPLE_RATE)
            for s in range(int(step * 1.8 * SAMPLE_RATE)):
                idx = start_s + s
                if idx < n_samples:
                    samples[idx] += 0.20 * marimba_tone(f, s / SAMPLE_RATE, step * 1.5)
        r_idx += 1
        t_riff += step

    save_wav(out_path, samples)

# 3. Siren Sesi (4.5s)
def gen_siren(out_path):
    dur = 4.5
    n_samples = int(dur * SAMPLE_RATE)
    samples = [0.0] * n_samples
    for i in range(n_samples):
        t = i / SAMPLE_RATE
        # Two-tone modulated emergency siren (650Hz to 950Hz sweep)
        sweep = (math.sin(2 * math.pi * 0.65 * t) + 1.0) * 0.5
        freq = 650.0 + sweep * 300.0
        phase = 2 * math.pi * freq * t
        # Horn acoustics (fundamental + odd harmonics + distortion)
        val = math.sin(phase) + 0.35 * math.sin(3 * phase) + 0.15 * math.sin(5 * phase)
        # Horn envelope with gentle fade out at end
        env = 1.0
        if t < 0.2: env = t / 0.2
        elif t > 4.0: env = (4.5 - t) / 0.5
        samples[i] = val * 0.5 * env
    save_wav(out_path, samples)

# 4. Yağmur Sesi (8.0s)
def gen_rain(out_path):
    dur = 8.0
    n_samples = int(dur * SAMPLE_RATE)
    samples = [0.0] * n_samples
    b0 = b1 = b2 = b3 = b4 = b5 = b6 = 0.0
    for i in range(n_samples):
        t = i / SAMPLE_RATE
        # Pink noise filter (Paul Kellet's method)
        white = random.random() * 2.0 - 1.0
        b0 = 0.99886 * b0 + white * 0.0555179
        b1 = 0.99332 * b1 + white * 0.0750759
        b2 = 0.96900 * b2 + white * 0.1538520
        b3 = 0.86650 * b3 + white * 0.3104856
        b4 = 0.55000 * b4 + white * 0.5329522
        b5 = -0.7616 * b5 - white * 0.0168980
        pink = b0 + b1 + b2 + b3 + b4 + b5 + b6 + white * 0.5362
        b6 = white * 0.115926
        
        # Soft rain envelope
        env = 1.0
        if t < 0.5: env = t / 0.5
        elif t > 7.5: env = (8.0 - t) / 0.5
        
        # Random raindrops on roofs
        drop = 0.0
        if random.random() < 0.003:
            drop = (random.random() * 0.2)
        samples[i] = (pink * 0.05 + drop) * env
    save_wav(out_path, samples)

# 5. Gök Gürültüsü Sesi (4.0s)
def gen_thunder(out_path):
    dur = 4.0
    n_samples = int(dur * SAMPLE_RATE)
    samples = [0.0] * n_samples
    last_val = 0.0
    for i in range(n_samples):
        t = i / SAMPLE_RATE
        # Low frequency rumble (brownian / integrated noise)
        white = random.random() * 2.0 - 1.0
        last_val = (last_val + 0.02 * white) / 1.02
        
        # Surge burst at t=0.2s
        surge = math.exp(-t * 1.5) * (0.8 + 0.4 * math.sin(2 * math.pi * 45 * t))
        # Deep sub frequency resonance
        sub = math.sin(2 * math.pi * 55 * t + math.sin(2 * math.pi * 12 * t)) * math.exp(-t * 1.2) * 0.4
        
        samples[i] = (last_val * 4.0 + sub) * surge
    save_wav(out_path, samples)

# 6. Yıldırım Sesi (1.5s)
def gen_lightning(out_path):
    dur = 1.5
    n_samples = int(dur * SAMPLE_RATE)
    samples = [0.0] * n_samples
    for i in range(n_samples):
        t = i / SAMPLE_RATE
        if t < 0.05:
            # Initial sharp electric strike / crack
            strike = (random.random() * 2.0 - 1.0) * math.exp(-t * 40.0)
            samples[i] = strike * 0.95
        else:
            # Crackle echo and bass collapse
            t_rem = t - 0.05
            crackle = (random.random() * 2.0 - 1.0) * math.exp(-t_rem * 4.0) * 0.35
            boom = math.sin(2 * math.pi * (80 - t_rem * 30) * t) * math.exp(-t_rem * 3.0) * 0.5
            samples[i] = crackle + boom
    save_wav(out_path, samples)

# 7. Bina Yükseltme Sesi (1.2s)
def gen_building_upgrade(out_path):
    dur = 1.2
    n_samples = int(dur * SAMPLE_RATE)
    samples = [0.0] * n_samples
    # Two hammer hits (0.0s and 0.18s)
    for hit_t in [0.0, 0.18]:
        start_s = int(hit_t * SAMPLE_RATE)
        for s in range(int(0.12 * SAMPLE_RATE)):
            idx = start_s + s
            if idx < n_samples:
                t_loc = s / SAMPLE_RATE
                env = math.exp(-t_loc * 35.0)
                wood = math.sin(2 * math.pi * 320 * t_loc) * env * 0.45
                click = (random.random() * 2.0 - 1.0) * env * 0.3
                samples[idx] += wood + click

    # Upward crystal arpeggio (C5 -> E5 -> G5 -> C6)
    arpeggio = [(0.35, 523.25), (0.47, 659.25), (0.59, 783.99), (0.71, 1046.50)]
    for note_t, f in arpeggio:
        start_s = int(note_t * SAMPLE_RATE)
        for s in range(int(0.4 * SAMPLE_RATE)):
            idx = start_s + s
            if idx < n_samples:
                samples[idx] += 0.35 * bell_tone(f, s / SAMPLE_RATE, 0.4)
    save_wav(out_path, samples)

# 8. Quiz Doğru Sesi (Özgün Tasarım - Akademik Çan & Dalga Tınısı)
def gen_quiz_correct(out_path):
    dur = 1.4
    n_samples = int(dur * SAMPLE_RATE)
    samples = [0.0] * n_samples
    # F#5 (739.99 Hz) -> B5 (987.77 Hz) joyful resonant acoustic chime
    chimes = [(0.0, 739.99, 0.5), (0.16, 987.77, 0.9)]
    for note_t, f, d in chimes:
        start_s = int(note_t * SAMPLE_RATE)
        for s in range(int(d * SAMPLE_RATE)):
            idx = start_s + s
            if idx < n_samples:
                t_loc = s / SAMPLE_RATE
                # Rich harmonic wooden bell
                tone = 0.5 * math.sin(2 * math.pi * f * t_loc) * math.exp(-t_loc * 4.0)
                tone += 0.25 * math.sin(2 * math.pi * f * 2.0 * t_loc) * math.exp(-t_loc * 7.0)
                tone += 0.15 * math.sin(2 * math.pi * f * 3.0 * t_loc) * math.exp(-t_loc * 10.0)
                # Subtle water drop sparkle
                drop = math.sin(2 * math.pi * (f * 1.5 + 400 * math.exp(-t_loc * 20)) * t_loc) * math.exp(-t_loc * 12.0) * 0.15
                samples[idx] += tone + drop
    save_wav(out_path, samples)

# 9. Quiz Yanlış Sesi (Özgün Tasarım - Sönümlü Ahşap Düşüş)
def gen_quiz_wrong(out_path):
    dur = 1.0
    n_samples = int(dur * SAMPLE_RATE)
    samples = [0.0] * n_samples
    # Gentle double wooden marimba downturn: Eb4 (311.13 Hz) -> Bb3 (233.08 Hz)
    notes = [(0.0, 311.13), (0.18, 233.08)]
    for note_t, f in notes:
        start_s = int(note_t * SAMPLE_RATE)
        for s in range(int(0.4 * SAMPLE_RATE)):
            idx = start_s + s
            if idx < n_samples:
                t_loc = s / SAMPLE_RATE
                # Muted wood block thud
                env = math.exp(-t_loc * 9.0)
                thud = math.sin(2 * math.pi * f * t_loc) * env * 0.55
                thud += math.sin(2 * math.pi * (f * 0.5) * t_loc) * env * 0.25
                click = (random.random() * 2.0 - 1.0) * math.exp(-t_loc * 30.0) * 0.15
                samples[idx] += thud + click
    save_wav(out_path, samples)

# 10. Afet Raporunda Bildirim Sesi (1.8s)
def gen_disaster_report_alert(out_path):
    dur = 1.8
    n_samples = int(dur * SAMPLE_RATE)
    samples = [0.0] * n_samples
    # Two short walkie-talkie / radio beeps (1200 Hz), then a deep situation tom
    for beep_t in [0.0, 0.08]:
        start_s = int(beep_t * SAMPLE_RATE)
        for s in range(int(0.05 * SAMPLE_RATE)):
            idx = start_s + s
            if idx < n_samples:
                t_loc = s / SAMPLE_RATE
                samples[idx] += 0.25 * math.sin(2 * math.pi * 1250 * t_loc) * math.exp(-t_loc * 30.0)
    
    # Deep impact / alert tom at t=0.22s
    start_s = int(0.22 * SAMPLE_RATE)
    for s in range(int(1.4 * SAMPLE_RATE)):
        idx = start_s + s
        if idx < n_samples:
            t_loc = s / SAMPLE_RATE
            f_drop = 180 * math.exp(-t_loc * 6.0) + 50
            tom = math.sin(2 * math.pi * f_drop * t_loc) * math.exp(-t_loc * 3.5) * 0.65
            samples[idx] += tom
    save_wav(out_path, samples)

# 11. Oyun Kazanma Sesi (5.0s - Ada Ayakta! Zafer Fanfarı)
def gen_game_win(out_path):
    dur = 5.0
    n_samples = int(dur * SAMPLE_RATE)
    samples = [0.0] * n_samples
    # Fanfare arpeggio (C4, E4, G4, C5, E5, G5, C6) + celebratory chords
    fanfare = [
        (0.00, 261.63, 0.2),
        (0.20, 329.63, 0.2),
        (0.40, 392.00, 0.2),
        (0.60, 523.25, 0.35),
        (0.95, 659.25, 0.35),
        (1.30, 783.99, 0.4),
        (1.70, 1046.50, 2.5), # Final long victory chord
    ]
    for note_t, f, d in fanfare:
        start_s = int(note_t * SAMPLE_RATE)
        for s in range(int(d * SAMPLE_RATE)):
            idx = start_s + s
            if idx < n_samples:
                t_loc = s / SAMPLE_RATE
                env = math.exp(-t_loc * (1.5 if d > 1.0 else 4.0))
                sig = 0.45 * math.sin(2 * math.pi * f * t_loc) * env
                sig += 0.25 * math.sin(2 * math.pi * (f * 2) * t_loc) * math.exp(-t_loc * 3.0)
                samples[idx] += sig

    # Final harmonious supporting chords at t=1.70s
    for chord_f in [523.25, 659.25, 783.99]:
        start_s = int(1.70 * SAMPLE_RATE)
        for s in range(int(2.8 * SAMPLE_RATE)):
            idx = start_s + s
            if idx < n_samples:
                t_loc = s / SAMPLE_RATE
                samples[idx] += 0.2 * math.sin(2 * math.pi * chord_f * t_loc) * math.exp(-t_loc * 1.2)
    save_wav(out_path, samples)

# 12. Oyun Kaybetme Sesi (4.0s - Ada Kaybedildi Hüzünlü Bitiş)
def gen_game_lose(out_path):
    dur = 4.0
    n_samples = int(dur * SAMPLE_RATE)
    samples = [0.0] * n_samples
    # Somber minor descent (A4, F4, D4, C#4) with sorrowful decay
    descent = [
        (0.00, 440.00, 0.6), # A4
        (0.60, 349.23, 0.6), # F4
        (1.20, 293.66, 0.8), # D4
        (2.00, 277.18, 1.8), # C#4 (tense resolving minor)
    ]
    for note_t, f, d in descent:
        start_s = int(note_t * SAMPLE_RATE)
        for s in range(int(d * SAMPLE_RATE)):
            idx = start_s + s
            if idx < n_samples:
                t_loc = s / SAMPLE_RATE
                env = math.exp(-t_loc * 2.0)
                piano = (0.5 * math.sin(2 * math.pi * f * t_loc) + 0.2 * math.sin(2 * math.pi * f * 2 * t_loc)) * env
                samples[idx] += piano

    # Wind gust / dark pad underneath
    for i in range(n_samples):
        t = i / SAMPLE_RATE
        wind_env = math.sin(math.pi * t / dur)
        white = random.random() * 2.0 - 1.0
        samples[i] += white * 0.04 * wind_env
    save_wav(out_path, samples)

# 13. Deniz Dalga Ambiyansı (15.0s - Kusursuz Seamless Loop Kıyı Dalgası)
def gen_sea_waves(out_path):
    dur = 15.0
    n_samples = int(dur * SAMPLE_RATE)
    samples = [0.0] * n_samples
    
    # 3 periodic waves of 5.0 seconds each (exact mathematical cycle for seamless looping)
    period = 5.0
    b0 = b1 = b2 = b3 = b4 = b5 = b6 = 0.0
    
    # Generate pink noise base
    pink_buffer = [0.0] * n_samples
    for i in range(n_samples):
        white = random.random() * 2.0 - 1.0
        b0 = 0.99886 * b0 + white * 0.0555179
        b1 = 0.99332 * b1 + white * 0.0750759
        b2 = 0.96900 * b2 + white * 0.1538520
        b3 = 0.86650 * b3 + white * 0.3104856
        b4 = 0.55000 * b4 + white * 0.5329522
        b5 = -0.7616 * b5 - white * 0.0168980
        pink = b0 + b1 + b2 + b3 + b4 + b5 + b6 + white * 0.5362
        b6 = white * 0.115926
        pink_buffer[i] = pink * 0.06

    for i in range(n_samples):
        t = i / SAMPLE_RATE
        # Exact wave cycle 0..1 in 5.0 seconds
        phase = (t % period) / period
        
        # Wave swell envelope: gentle build-up, crest, long soft backwash
        swell = math.pow(math.sin(math.pi * phase), 1.8)
        
        # Deep ocean body rumble (60 - 110 Hz)
        rumble = (math.sin(2 * math.pi * 75 * t) + 0.5 * math.sin(2 * math.pi * 110 * t)) * swell * 0.18
        
        # Foam / shore hiss (higher frequency pink noise modulated by wave swell)
        foam = pink_buffer[i] * (0.3 + 0.7 * swell)
        
        # Receding water bubbles / gentle gravel sizzle
        recede_phase = max(0.0, phase - 0.45)
        recede = (random.random() * 2.0 - 1.0) * math.exp(-recede_phase * 6.0) * 0.03
        
        samples[i] = rumble + foam + recede

    # Seamless boundary crossfade (0.5s) to guarantee zero click when looping
    fade_len = int(0.5 * SAMPLE_RATE)
    for i in range(fade_len):
        alpha = i / fade_len
        # Blend start with end
        end_idx = n_samples - fade_len + i
        blended = samples[i] * alpha + samples[end_idx] * (1.0 - alpha)
        samples[i] = blended
        samples[end_idx] = blended

    save_wav(out_path, samples)

def main():
    out_dir = "Assets/Audio"
    os.makedirs(out_dir, exist_ok=True)
    
    gen_main_menu(os.path.join(out_dir, "ana_ekran_muzigi.wav"))
    gen_gameplay(os.path.join(out_dir, "ana_oyun_muzigi.wav"))
    gen_siren(os.path.join(out_dir, "siren_sesi.wav"))
    gen_rain(os.path.join(out_dir, "yagmur_sesi.wav"))
    gen_thunder(os.path.join(out_dir, "gok_gurlutusu_sesi.wav"))
    gen_lightning(os.path.join(out_dir, "yildirim_sesi.wav"))
    gen_building_upgrade(os.path.join(out_dir, "bina_yukseltme_sesi.wav"))
    gen_quiz_correct(os.path.join(out_dir, "quiz_dogru_sesi.wav"))
    gen_quiz_wrong(os.path.join(out_dir, "quiz_yanlis_sesi.wav"))
    gen_disaster_report_alert(os.path.join(out_dir, "afet_raporu_bildirim.wav"))
    gen_game_win(os.path.join(out_dir, "oyun_kazanma_sesi.wav"))
    gen_game_lose(os.path.join(out_dir, "oyun_kaybetme_sesi.wav"))
    gen_sea_waves(os.path.join(out_dir, "deniz_dalga_ambiyansi.wav"))
    print("\nAll 13 audio files successfully generated in Assets/Audio!")

if __name__ == "__main__":
    main()
