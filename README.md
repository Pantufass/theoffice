# Text corp (title in progress)

Use camel case for most cases (examples: FileName1.txt or SuperBigFileName.csv)
Godot Version 4.6.2

## Project Structure

root/
├── Assets/ # folder containing 3D assets and models
│ └── ObjectFolder # folders of each model
│   └── TexturesFolder # textures of the model inside
├── Defunct/ # previous iterations' content
├── Objects/ # folder containing the scenes of objects like computer, table, door
├── Scenes/ # folder containing the scenes like rooms, floors or levels
├── Scripts/ # scripts folder
│ ├── Abstract/ # abstract and virtual classes folder
│ ├── Behaviour/ # behaviour scripts of objects
│ ├── Interface/ # interface classes
│ ├── UI/ # scripts with UI logic like typing or screens
│ └── Rest/ # player, camera and level scripts
├── UI/ # folder containing UI, 2D assets
│ ├── FontSettings/ # folder with godot font settings
│ └── Shaders/ # folder with shaders
└── project.godot

## Controls

| Action | Key |
|--------|-----|
| Move | WASD |
| Look | Mouse |
| Interact | E - Left click | 
| Sprint | Shift |
| Crouch | Ctrl |
| Jump | Space |
| Cancel Typing | Backspace |

## Resources

CSG Nodes Prototyping https://docs.godotengine.org/en/stable/tutorials/3d/csg_tools.html
Godot-Rive extension https://github.com/kibble-cabal/godot-rive