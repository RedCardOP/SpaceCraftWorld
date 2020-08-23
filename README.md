# SpaceCraftWorld
Cringe

## Class Responsibilities and Functionality
### Chunk
The chunk class manages the map of Voxels BlockTypes as well as acting as a intermediary between external update and edit calls and determining which subchunk requires modifications.
Functionality:
- Editing blocks
- Returning blocks
### Subchunk
The subchunk class is in charge of handiling the mesh renderer and filter for its designated subdivision. It recieves update calls and obtains voxel information from the primary chunk that it is a part of.
Functionality:
- Updating the mesh render and filter components
### World
The world class is inc
