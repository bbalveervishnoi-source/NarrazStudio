# Copyright (C) 2026 Balveer
# Narraz Studio is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License.
import asyncio
import edge_tts
import sys
import json
import os
import subprocess
import io

# Force UTF-8 encoding for stdout and stderr to prevent UnicodeEncodeError with Hindi/mixed text
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8')

# Per-chunk timeout in seconds. If a chunk takes longer, it is retried.
CHUNK_TIMEOUT_SECONDS = 45
MAX_RETRIES = 2

async def list_voices():
    voices = await edge_tts.VoicesManager.create()
    relevant_voices = []
    for v in voices.voices:
        relevant_voices.append({
            "Name": v["FriendlyName"],
            "ShortName": v["ShortName"],
            "Locale": v["Locale"],
            "Gender": v["Gender"]
        })
    print(json.dumps(relevant_voices))

def _normalize_rate_pitch(rate: str, pitch: str):
    """
    Ensure rate and pitch are in the exact format edge-tts expects:
    rate  -> '+0%'  (always has sign, ends with %)
    pitch -> '+0Hz' (always has sign, ends with Hz)
    This prevents NoAudioReceived errors on non-English voices.
    """
    def add_sign(s: str, suffix: str) -> str:
        s = s.strip()
        # Strip suffix if already present
        if s.endswith(suffix):
            s = s[:-len(suffix)]
        try:
            val = int(float(s))
        except (ValueError, TypeError):
            val = 0
        sign = "+" if val >= 0 else ""
        return f"{sign}{val}{suffix}"

    return add_sign(rate, "%"), add_sign(pitch, "Hz")

async def _generate_chunk_with_retry(i, chunk, voice, rate, pitch, temp_folder, total_chunks):
    """Generate a single audio chunk with timeout and retry logic."""
    temp_file = os.path.join(temp_folder, f"chunk_{i}.mp3")
    last_error = None

    for attempt in range(MAX_RETRIES + 1):
        try:
            if attempt > 0:
                print(f"STATUS: Retrying chunk {i+1}/{total_chunks} (attempt {attempt+1})...")
                sys.stdout.flush()
                await asyncio.sleep(2)  # Brief pause before retry

            communicate = edge_tts.Communicate(chunk, voice, rate=rate, pitch=pitch)

            # Write with a hard timeout so a hung network call doesn't block forever
            await asyncio.wait_for(communicate.save(temp_file), timeout=CHUNK_TIMEOUT_SECONDS)

            # Verify file is non-empty
            if not os.path.exists(temp_file) or os.path.getsize(temp_file) == 0:
                raise Exception(f"Generated chunk {i+1} is empty — voice may not support this text/language combination.")

            return i, temp_file

        except asyncio.TimeoutError:
            last_error = f"Chunk {i+1} timed out after {CHUNK_TIMEOUT_SECONDS}s (network issue or unsupported voice/language)"
            print(f"STATUS: Timeout on chunk {i+1}, attempt {attempt+1}...")
            sys.stdout.flush()
            # Remove partial file if it exists
            if os.path.exists(temp_file):
                try:
                    os.remove(temp_file)
                except Exception:
                    pass
        except Exception as e:
            last_error = str(e)
            print(f"STATUS: Error on chunk {i+1}, attempt {attempt+1}: {e}")
            sys.stdout.flush()
            if os.path.exists(temp_file):
                try:
                    os.remove(temp_file)
                except Exception:
                    pass

    raise Exception(f"Chunk {i+1} failed after {MAX_RETRIES+1} attempts. Last error: {last_error}")

async def process_chunks(config, ffmpeg_path):
    try:
        print("STATUS: CONFIG: " + json.dumps(config, ensure_ascii=False))
        sys.stdout.flush()
    except Exception:
        pass

    text_file = config.get('text_file', '')
    voice = config['voice']
    output_final = config['output_file']
    raw_rate = config.get('rate', '+0%')
    raw_pitch = config.get('pitch', '+0Hz')
    ffmpeg_exe = os.path.join(ffmpeg_path, "ffmpeg.exe")

    # Normalize rate/pitch to prevent edge-tts NoAudioReceived on non-English voices
    rate, pitch = _normalize_rate_pitch(raw_rate, raw_pitch)
    print(f"STATUS: Voice={voice} Rate={rate} Pitch={pitch}")
    sys.stdout.flush()

    # Resolve text_file to an absolute path using several fallback locations
    candidates_tried = []
    if os.path.isabs(text_file) and os.path.exists(text_file):
        resolved_text_file = text_file
    else:
        resolved_text_file = None

        candidate = os.path.abspath(text_file)
        candidates_tried.append(candidate)
        if os.path.exists(candidate):
            resolved_text_file = candidate

        if resolved_text_file is None:
            candidate2 = os.path.join(os.getcwd(), text_file)
            candidates_tried.append(candidate2)
            if os.path.exists(candidate2):
                resolved_text_file = candidate2

        if resolved_text_file is None:
            script_dir_candidate = os.path.join(os.path.dirname(os.path.abspath(__file__)), text_file)
            candidates_tried.append(script_dir_candidate)
            if os.path.exists(script_dir_candidate):
                resolved_text_file = script_dir_candidate

        if resolved_text_file is None:
            import tempfile as _temp
            temp_candidate = os.path.join(_temp.gettempdir(), text_file)
            candidates_tried.append(temp_candidate)
            if os.path.exists(temp_candidate):
                resolved_text_file = temp_candidate

        if resolved_text_file is None:
            local_app = os.getenv('LOCALAPPDATA') or os.getenv('LOCALAPPDATA'.lower())
            if local_app:
                lad_candidate = os.path.join(local_app, 'NarrazStudio', text_file)
                candidates_tried.append(lad_candidate)
                if os.path.exists(lad_candidate):
                    resolved_text_file = lad_candidate

        if resolved_text_file is None:
            candidates_tried.append(text_file)

        if resolved_text_file:
            text_file = resolved_text_file

    # Read input text, falling back to inline config text if file is missing
    full_text = None
    if text_file and os.path.exists(text_file):
        with open(text_file, 'r', encoding='utf-8') as f:
            full_text = f.read()
    else:
        if 'text' in config and isinstance(config['text'], str) and config['text'].strip():
            full_text = config['text']
            print("STATUS: Using inline text from config (length={})".format(len(full_text)))
            sys.stdout.flush()
        else:
            print("RESULT: ERROR - Text file not found: '{}'".format(text_file))
            for c in candidates_tried:
                try:
                    print("RESULT: INFO - tried: {} (exists={})".format(c, os.path.exists(c)))
                except Exception:
                    pass
            sys.stdout.flush()
            raise FileNotFoundError(f"Text file not found: '{text_file}'. Make sure the path in the config is correct.")

    # Split text into safe chunks (max ~3500 chars) to prevent edge-tts NoAudioReceived errors
    chunk_size = 3500
    text_chunks = []
    current_chunk = ""
    for paragraph in full_text.split('\n'):
        if not paragraph.strip():
            continue
        if len(current_chunk) + len(paragraph) < chunk_size:
            current_chunk += paragraph + "\n"
        else:
            if current_chunk:
                text_chunks.append(current_chunk)
            if len(paragraph) >= chunk_size:
                for i in range(0, len(paragraph), chunk_size):
                    text_chunks.append(paragraph[i:i+chunk_size])
                current_chunk = ""
            else:
                current_chunk = paragraph + "\n"
    if current_chunk:
        text_chunks.append(current_chunk)

    import tempfile
    temp_folder = os.path.join(tempfile.gettempdir(), "narraz_temp_chunks")
    os.makedirs(temp_folder, exist_ok=True)
    temp_files = []

    try:
        total_chunks = len(text_chunks)
        completed_chunks = 0
        parallel_limit = config.get('parallel_limit', 15)
        semaphore = asyncio.Semaphore(parallel_limit)

        print(f"STATUS: Completed chunk 0/{total_chunks}")
        sys.stdout.flush()

        async def _generate_chunk(i, chunk):
            nonlocal completed_chunks
            async with semaphore:
                print(f"STATUS: Processing chunk {i+1}/{total_chunks} in parallel...")
                sys.stdout.flush()
                result = await _generate_chunk_with_retry(i, chunk, voice, rate, pitch, temp_folder, total_chunks)
                completed_chunks += 1
                print(f"STATUS: Completed chunk {completed_chunks}/{total_chunks}")
                sys.stdout.flush()
                return result

        print(f"STATUS: Starting parallel generation for {total_chunks} chunks...")
        sys.stdout.flush()

        # Run all chunk tasks concurrently and sort by index
        tasks = [_generate_chunk(i, chunk) for i, chunk in enumerate(text_chunks)]
        results = await asyncio.gather(*tasks)
        results = sorted(results, key=lambda x: x[0])

        for index, temp_file in results:
            temp_files.append(temp_file)

        # Merge chunks with FFmpeg
        print("STATUS: Merging all parts...")
        sys.stdout.flush()

        if len(temp_files) == 1:
            import shutil
            if not os.path.exists(temp_files[0]) or os.path.getsize(temp_files[0]) == 0:
                raise Exception(f"Audio generation produced empty file: {temp_files[0]}")
            shutil.copy(temp_files[0], output_final)
        elif len(temp_files) > 1:
            concat_file = os.path.join(temp_folder, "concat.txt")
            with open(concat_file, "w", encoding="utf-8") as f:
                for tf in temp_files:
                    escaped_path = os.path.abspath(tf).replace("\\", "/")
                    f.write(f"file '{escaped_path}'\n")

            proc = subprocess.run(
                [ffmpeg_exe, "-hwaccel", "auto", "-y", "-f", "concat", "-safe", "0", "-i", concat_file, "-c", "copy", output_final],
                check=False,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                text=True
            )
            if proc.returncode != 0:
                print("RESULT: ERROR - ffmpeg failed with code {}".format(proc.returncode))
                if proc.stdout:
                    print("RESULT: FFMPEG-OUT: " + proc.stdout)
                if proc.stderr:
                    print("RESULT: FFMPEG-ERR: " + proc.stderr)
                sys.stdout.flush()
                raise Exception("ffmpeg failed to merge audio")

        print("RESULT: SUCCESS")
        sys.stdout.flush()

    except Exception as e:
        print(f"RESULT: ERROR - {str(e)}")
        sys.stdout.flush()
        sys.exit(1)
    finally:
        if os.path.exists(temp_folder):
            try:
                for tf in temp_files:
                    if os.path.exists(tf):
                        os.remove(tf)
                concat_file = os.path.join(temp_folder, "concat.txt")
                if os.path.exists(concat_file):
                    os.remove(concat_file)
                os.rmdir(temp_folder)
            except Exception as e:
                pass

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python tts_engine.py <config_path|--list-voices> [ffmpeg_path]")
        sys.exit(1)

    command = sys.argv[1]

    if command == "--list-voices":
        asyncio.run(list_voices())
    else:
        if len(sys.argv) < 3:
            print("Usage: python tts_engine.py <config_path> <ffmpeg_path>")
            sys.exit(1)

        config_path = command
        ffmpeg_path = sys.argv[2]

        try:
            with open(config_path, 'r', encoding='utf-8') as f:
                config = json.load(f)

            # Resolve relative text_file path against config file directory
            cfg_text = config.get('text_file', '')
            if cfg_text and not os.path.isabs(cfg_text):
                cfg_dir = os.path.dirname(os.path.abspath(config_path))
                candidate = os.path.join(cfg_dir, cfg_text)
                if os.path.exists(candidate):
                    config['text_file'] = candidate

            asyncio.run(process_chunks(config, ffmpeg_path))
        except Exception as e:
            import traceback
            print("RESULT: ERROR - " + str(e))
            sys.stderr.write(traceback.format_exc())
            sys.exit(1)
