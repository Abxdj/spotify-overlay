# Spotify Overlay

A floating, always-on-top Spotify controller for Windows, styled after the classic iPod. Shows live album art and track info, gives you playback controls, and renders a real-time audio visualizer driven by a system-audio FFT. Auto-hides when nothing's playing.

![Spotify Overlay](screenshot.png)

## Features

- **Floating widget** — frameless, transparent, always-on-top; drag it anywhere
- **Live now-playing** — album art, title, and artist, polled from the Spotify Web API
- **Playback controls** — previous / play-pause / next, laid out on an iPod-style click wheel
- **Real-time visualizer** — captures system audio via WASAPI loopback and runs an FFT for a reactive bar spectrum (works with any audio, not just Spotify)
- **Auto-hide** — invisible while idle, appears when music starts
- **Token caching** — log in once; the app refreshes and reuses the token silently

## Tech stack

- **C# / WPF** (.NET 9) — the overlay UI and rendering
- **[SpotifyAPI-NET](https://github.com/JohnnyCrazy/SpotifyAPI-NET)** — Web API client + PKCE auth
- **[NAudio](https://github.com/naudio/NAudio)** — WASAPI loopback capture and FFT

Authentication uses the **Authorization Code flow with PKCE** — no client secret is stored. The access token is cached in `%APPDATA%\SpotifyOverlay\`.

## Setup

1. **Register a Spotify app** at the [Spotify Developer Dashboard](https://developer.spotify.com/dashboard).
   - Add redirect URI: `http://127.0.0.1:5543/callback`
   - Enable the **Web API**
   - Copy the **Client ID**

2. **Set your Client ID** as an environment variable:
   ```
   setx SPOTIFY_CLIENT_ID "your_client_id_here"
   ```
   Reopen your terminal afterward so the variable takes effect.

3. **Build and run:**
   ```
   dotnet run
   ```
   On first launch a browser opens for Spotify login. After that it starts silently.

## Requirements

- Windows 10/11
- .NET 9 SDK
- Spotify **Premium** (playback control endpoints require it)

## Notes

- The visualizer reads your **default playback device** via WASAPI loopback.
- Publish as a folder (`dotnet publish -c Release -r win-x64 --self-contained true`), **not** single-file — single-file publishing breaks the embedded OAuth callback server that handles the login redirect.

## License

MIT
