# Text Corp

## Controls

| Action | Key |
|--------|-----|
| Move | WASD |
| Look | Mouse |
| Interact | E or LeftClick |
| Sprint | Shift |
| Crouch | Ctrl |
| Jump | Space |
| Cancel Typing | ESC |

## Project Structure
```
root/
├── Assets/         # 3D assets and models
│ └── ObjectFolder/     # Individual model folders
│ └── TexturesFolder/   # Model textures
├── Defunct/        # Previous iterations (deprecated)
├── Objects/        # Object scenes (computer, table, door)
├── Scenes/         # Room, floor, and level scenes
├── Scripts/ 
│ ├── Abstract/         # Abstract and virtual classes
│ ├── Behaviour/        # Object behavior scripts
│ ├── Interface/        # Interface classes
│ ├── UI/               # UI logic (typing, screens)
│ └── Rest/             # Player, camera, and level scripts
├── UI/             # UI and 2D assets
│ ├── FontSettings/     # Godot font settings
│ └── Shaders/          # Shader files
└── project.godot   # Godot project file (v4.6.2)
```

## Naming Convention

- Camel case for files: `FileName1.txt` or `SuperBigFileName.csv`

