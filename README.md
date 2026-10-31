# Quiz Royale — The Endless Expedition

A real Unity 2022.3 WebGL game: one human against 15 computer-controlled rivals. Inspired by quiz-powered battle games, with original characters and visuals. Not affiliated with Blooket.

## Play

Desktop browser recommended. Explore a 424 × 424-unit 3D map containing nine districts, wooded areas, ruins, 70 supply caches and nine quiz beacons. Answer questions to gain energy and health. Eliminate rivals while staying inside the closing storm.

Every victory or defeat automatically starts a fresh arena. Victory builds your crown streak, with the best streak saved in your browser. There is no final arena. This version is single-player versus bots; it does not use an online multiplayer server.

| Control | Action |
| --- | --- |
| WASD / arrows | Move |
| Shift | Sprint |
| Space / hold left mouse | Fire at nearest rival within 27 units |
| Q | Answer a question to replenish supplies |
| 1–4 | Choose an answer |
| M | Show full map |
| Esc | Pause |

The world pauses during quizzes and when the full map is open. Mint map dot = you; coral = bots; gold = quiz beacons. Nearby quiz beacons increase answer rewards. Gold supply caches give energy and health. Rivals drop supplies when eliminated.

## Hosting

The `docs` directory is the complete, prebuilt Unity WebGL website. Publish with GitHub Pages using **Deploy from a branch → main → /docs**. Compression is disabled so no special server headers are required. `.nojekyll` keeps the Unity build files intact. Use an HTTP server for local play; opening index.html directly from disk will not load WebAssembly correctly.

## Edit and rebuild

Open this project with Unity **2022.3.62f2** and the WebGL Build Support module installed. The main game source is `Assets/Scripts/Royale.cs` and the build entry point is `Assets/Editor/WebBuild.cs`.

Batch build: invoke Unity with `-batchmode -nographics -quit -projectPath <project-folder> -executeMethod WebBuild.Build -buildTarget WebGL -logFile <log-path>`. This recreates the Arena scene and the browser build under `docs`. If using the custom web page, preserve `docs/index.html` before rebuilding and restore it afterward.

All art is generated from Unity primitives at runtime. No external images, fonts, services, API keys or paid assets are required.
