<div align="center">
  <img src="NarrazStudio/Assets/AppIcon.png" alt="Narraz Studio Logo" width="120" height="120">
  
  # Narraz Studio
  
  **A simple, powerful, and completely self-contained Text-to-Speech (TTS) Engine for Windows.**
</div>

---

## 🌟 Overview

**Narraz Studio** is a premium, modern desktop application that allows users to convert text into high-quality, natural-sounding audio. It leverages the Microsoft Edge TTS API under the hood to provide incredibly realistic voices across dozens of languages without any usage limits or API keys.

Designed with a sleek **WinUI 3** interface and a fully bundled Python processing engine, Narraz Studio is a true "plug-and-play" application. You don't need to install Python, FFmpeg, or any other dependencies. Just download, open, and start narrating!

## ✨ Key Features

*   **🌍 Massive Voice Library**: Access hundreds of natural, AI-driven voices across multiple languages and locales (English, Hindi, Chinese, Urdu, and more).
*   **🎛️ Advanced Audio Adjustments**: Fine-tune the narration by adjusting the **Pitch** and **Rate** in real-time.
*   **🎵 Integrated Playback**: Preview your generated audio instantly using the built-in media controls before exporting.
*   **📦 Batch Export**: Have a folder full of text scripts? Queue multiple `.txt` files and convert them all into `.mp3` simultaneously with parallel chunk processing.
*   **🕒 History & Tracking**: Automatically keeps a log of your generated narrations, including word/character counts, dates, and used voices.
*   **🎨 Premium UI**: Features a beautiful Fluent Design interface with Dark/Light modes, Mica backdrop support, and localization in 4+ languages.
*   **🔌 Zero Dependencies**: Completely self-contained. The TTS engine and FFmpeg are bundled directly into the application.

## 🚀 How It Works

Narraz Studio uses a hybrid architecture:
1.  **Frontend (C# WinUI 3)**: Provides the beautiful, responsive user interface and handles user interactions, file management, and media playback.
2.  **Backend (Compiled Python)**: A standalone executable compiled via PyInstaller interacts with the open-source `edge-tts` library to fetch audio chunks asynchronously.
3.  **Audio Processing**: Bundled FFmpeg merges audio chunks seamlessly to bypass any length limits typically imposed by web APIs.

## 💻 Installation & Usage

Narraz Studio is designed to be completely plug-and-play.

1.  Download the latest release package.
2.  Run the application. 
3.  *That's it!* There are no prerequisites.

**To use:**
1. Paste your text into the editor.
2. Select your preferred Narrator from the settings panel.
3. Click **Play Audio** to preview, or **Export MP3** to save the file.

## 🛠️ Development Setup

If you want to build the project from source or contribute:

### Prerequisites
*   Visual Studio 2022 (with .NET Desktop Development and Windows App SDK workloads)
*   Python 3.10+ (If you plan to modify the `tts_engine.py` script)

### Building the Project
1. Clone the repository: `git clone https://github.com/bbalveervishnoi-source/NarrazStudio.git`
2. Open `NarrazStudio.sln` in Visual Studio.
3. Ensure the `Engine` folder contains `ffmpeg.exe`, `ffprobe.exe`, and `tts_engine.exe`.
   > *Note: If you modify the python script, recompile it using `pyinstaller --onefile tts_engine.py` and place the resulting executable in the `Engine` folder.*
4. Build the project using the `x64` platform target.

## ⚖️ License & Legal

Narraz Studio is **100% free** and open-source under the **GPL v3 License**.
*   **Codebase**: Free to modify and distribute under GPL v3 terms.
*   **Edge-TTS**: Operates via the open-source python wrapper.
*   **FFmpeg**: Used under the GPL/LGPL license.
*   **Commercial Use**: You are free to use the generated audio for commercial purposes (YouTube, Podcasts, Audiobooks) as there are no paid constraints or proprietary restrictions built into the codebase.

**Important Note:** Narraz Studio is protected under the GPL v3 License. Any redistribution or rebranding of this software on the Microsoft Store or any other platform without proper attribution and adherence to GPL v3 terms is strictly prohibited.

---
<div align="center">
  <i>Developed with ❤️ by Balveer Vishnoi</i>
</div>
