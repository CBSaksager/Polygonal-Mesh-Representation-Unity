# Mesh Representation Schemes Unity

This project implements and compares different polygon mesh representation schemes in Unity. It focuses on alternatives to the traditional Half-Edge data structure, particularly using a vertex-based approach inspired by Rotation Systems.

## Codebase Structure

The project is organized into several modules, each implementing a different mesh representations or environment extensions:

## Key Features

- **Multiple mesh representations:** Half-edge and Rotation Systems
- **Face operations:** Select and split faces
- **Edge operations:** Navigate mesh topology through edges
- **Visualization:** Debug gizmos for vertices, edges, and faces
- **Performance metrics:** Timing information for mesh operations

### Half-Edge Data Structure

Located in [Assets/Scripts/Half-edge](https://github.com/CBSaksager/Polygonal-Mesh-Representation-Unity/tree/main/Assets/Scripts/Half-edge)
This folder contains the implementation of a Half-Edge mesh.

- `HalfEdgeCore.cs` - Core data structures:
  - `HEVertex` - Represents a vertex in 3D space
  - `HEHalfEdge` - Represents a directed half-edge
  - `HEFace` - Represents a mesh face
- `HalfEdgeMesh.cs` - Main mesh class with operations
- `HalfEdgeTester.cs` - MonoBehaviour for testing in Unity

### Rotation System v2

Located in [Assets/Scripts/Rotation System v2](https://github.com/CBSaksager/Polygonal-Mesh-Representation-Unity/tree/main/Assets/Scripts/Rotation%20System%20v2)
This folder contains the implementation of a Rotation System mesh **with** a face definition.

- `RotationSystem2Core.cs` - Core data structures:
  - `RSVertex` - Represents a vertex in 3D space
  - `RSEdge` - Represents an edge between two vertices
  - `RSFace` - Represents a mesh face
- `RotationSystem2Mesh.cs` - Main mesh class with operations
- `RotationSystem2Tester.cs` - MonoBehaviour for testing in Unity

### Editor Extensions

Located in [Assets/Editor](https://github.com/CBSaksager/Polygonal-Mesh-Representation-Unity/tree/main/Assets/Editor)

- `HalfEdgeTesterEditor.cs` - Custom inspector for Half-Edge testing
- `RotationSystem2TesterEditor.cs` - Custom inspector for Rotation System v2
- `RotationSystemTesterEditor.cs` - Custom inspector for original Rotation System

## Getting Started

1. Open the Unity scene `Assets/Scenes/MeshSchemes.unity`
2. Select one of the tester objects in the hierarchy:
   - `HEMeshTester` for half-edge representation
   - `RS2MeshTester` for rotation system with face implementation
   - `RSMeshTester` for the simple, yet uncompletede, rotation system with out face implementation
3. Use the Inspector buttons to create and manipulate meshes

<details>
<summary><h1>Abandoned or unfinished parts</h1></summary>
<br>
## Abandoned or unfinished parts

Some parts of the project didn't work out either because of time constraints or prioritisation of other elements.

### Original Rotation System

Located in [Assets/Scripts/Rotation System](https://github.com/CBSaksager/Polygonal-Mesh-Representation-Unity/tree/main/Assets/Scripts/Rotation%20System)
This folder contains the _original_ Rotation System implementation. Meaning the implementation with a definition of faces. This mesh does work to some extend and the Face Split algorithm works. It shows how it is possible to work on meshes with implicit faces, but does not mimic a fair real-world mesh since no data for the faces such as colors, normals and so on can be stored.

- `RotationSystemCore.cs` - Core data structures
- `RotationSystemMesh.cs` - Main mesh class with operations
- `RotationSystemTester.cs` - MonoBehaviour for testing in Unity

### PLY File Support (Outdated)

Located in `Assets/Scripts/PLY(outdated)/`
An attempt to import PLY files without luck. For a better attempt look at the branch [RsPly](https://github.com/CBSaksager/Polygonal-Mesh-Representation-Unity/tree/RsPly/Assets/Scripts/Rotation%20System). This branch does however only import PLY files into the `Rotation System v2` mesh.

- `PlyImporter.cs` - Utilities for importing PLY files
- `PlyViewer.cs` - MonoBehaviour for viewing PLY files

</details>
