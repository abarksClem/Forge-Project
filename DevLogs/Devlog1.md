# Devlog 1 — Week 1
**Focus:** Getting the project set up and deciding how the game plays.
 
---
 
## What I changed this week
 
- Created the Unity project (Unity 6, Universal 2D template) and connected it to GitHub.
- Added `.gitignore` and `.gitattributes` so Unity's generated folders (`Library`, `Temp`, `Logs`) stay out of the repo and art/audio files use Git LFS.
- Set up the folder structure. Everything I make goes in `Assets/_Project/` (Art, Scripts, Scenes, Prefabs, Audio...). Imported assets go in `Assets/ThirdParty/`.  
All I have right now are the default assets, along with some assets from Unity that I used for testing.
- Added a `Devlogs` folder with this first log.
- Added design docs in `Docs/`: a game structure overview plus three diagrams (systems architecture, player prefab, enemy AI state machine).
What I learned: `.gitignore` has to sit in the project root next to `Assets`. My first `git add` tried to commit Unity's `Library` folder because the file wasn't being picked up.
 
 
## What I plan to add next (Week 2)
 
**Player movement.** It's the foundation for everything else, so it needs to feel good first.
 
- Run left and right with smooth acceleration
- Jump, with a short hop if the button is released early
- Coyote time and jump buffering, so jumps feel forgiving
- A simple test level built with a Tilemap
- A camera that follows the player
**Stretch goal:** a basic sword attack.
 
