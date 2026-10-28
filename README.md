# Foxy Jumpscare

A [BepInEx 6](https://builds.bepinex.dev/projects/bepinex_be) (IL2CPP) mod for **Flipping is Hard** that randomly plays a jumpscare overlay with sound while you play.

Every second the mod rolls a 1 in N chance (default: 1 in 4000, about once per hour of play on average). When it hits, a transparent video overlay is drawn over the game and a sound is played. The overlay only appears on your own screen and does not affect other players.

> **Unofficial fan mod.** Not affiliated with or endorsed by the developers of Flipping is Hard. **No video or audio is included in this repository.** You supply your own clip (see below).

## Requirements

- Flipping is Hard (Windows, Steam)
- [BepInEx 6 bleeding-edge, Windows x64, Unity IL2CPP](https://builds.bepinex.dev/projects/bepinex_be), installed next to the game `.exe` and run once, so that `BepInEx/interop/` is generated
- [.NET SDK](https://dotnet.microsoft.com/download) (to build)
- [ffmpeg](https://ffmpeg.org/download.html) (to convert your clip)
- Your own green-screen video clip

## Build

From the repository folder:

```
dotnet build -c Release -p:GamePath="C:\Path\To\Flipping is Hard"
```

Instead of passing `-p:GamePath`, you can set the `FIH_GAME_PATH` environment variable once.

The result is `bin/Release/net6.0/FoxyJumpscare.dll`.

## Prepare your media

Run the conversion script with your own green-screen video:

```
.\convert.ps1 -Video yourclip.mp4
```

Optional parameters:

| Parameter | Default | Description |
|---|---|---|
| `-Fps` | `20` | Frames per second to extract. Must match the `Fps` config value. |
| `-Key` | `0x00FF00` | Green-screen colour to make transparent. |

The script creates a `FoxyJumpscare` folder containing:

```
FoxyJumpscare/
├── frames/
│   ├── f_0001.rgba
│   ├── f_0002.rgba
│   └── ...
└── foxy.wav
```

Frames are raw RGBA at 640 px width. Keep the clip short: a 3 second clip at 20 fps is about 60 frames and uses roughly 60 to 80 MB of memory.

If green edges remain, increase the colorkey tolerance in `convert.ps1` (for example `0.35:0.15`).

## Install

Copy these into the game folder:

```
BepInEx/plugins/FoxyJumpscare.dll
BepInEx/plugins/FoxyJumpscare/frames/*.rgba
BepInEx/plugins/FoxyJumpscare/foxy.wav
```

Start the game once. It creates the config file.

## Configuration

`BepInEx/config/foxyjumpscare.cfg`

| Setting | Default | Description |
|---|---|---|
| `ChanceDenominator` | `1000` | Each second there is a 1 in N chance of the jumpscare. Set it to `5` to test. |
| `Fps` | `20` | Playback speed. Must match the fps used when converting. |

Average time between jumpscares is roughly N seconds, for example 4000 is about 67 minutes, 1000 about 17 minutes. It is random, so it can trigger twice in a row or not for hours.

## Troubleshooting

- **Build error about `GamePath`:** pass `-p:GamePath=...` or set `FIH_GAME_PATH`, and make sure the game has been run once with BepInEx so `BepInEx/interop` exists.
- **Nothing shows up:** check `BepInEx/LogOutput.log` for `Loaded N jumpscare frames`. If N is 0, the frames folder is in the wrong place.
- **Upside-down image:** remove `vflip` from `convert.ps1` and re-export.
- **Skewed or garbled image:** the width in the code (640) must match `scale=640` in ffmpeg.
- **No sound:** check that `foxy.wav` exists and that your clip has an audio track.
- **Blank or white flash:** make sure you are on the latest version of this mod, which keeps the textures from being unloaded by Unity.

## Contributing

Issues and pull requests are welcome. Please do not commit any video, image or audio files, or any files from the game's `BepInEx/interop` folder.

## License

The code is released under the [MIT License](LICENSE). The license covers this code only. It does not cover any video or audio you use with it, and it does not cover the game or its assets.