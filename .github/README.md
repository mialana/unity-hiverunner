# Unity Hive Runner

[Demo Video](https://youtu.be/UpFucJPhbws)

## Overview
In a voxel-based world, play an ant who must balance risk vs. reward to escape from the honey-filled bee hive.

## Details
Built in Unity 6000.0.57f1. \
Tested on **MacOS** using **Metal** for GPU compute power, and **Linux Fedora 42 (NVIDIA GPU)** using **Vulkan** for GPU compute power.

## Setup

Clone this repository and select its root in Unity Hub after choosing the "Add project from disk" option on the *Projects* tab.

## Features
### Technical
1. **Voxel-Based Meshing** for Rising Honey and Honey Comb Obstacles
2. **Marching Cubes** Compute Shader
3. **Signed Distance Function (SDF)** Compute Shader
4. **Endless Chunk Generation and Culling**

### Creative
1. Custom mixed **theme music**
2. Custom **Shader Graphs** for Honey Comb, Hive Cell, Rising Honey, etc.
3. **Maya-Modeled** Hive Cell Geometry

## Credits

- [Ant Model](https://sketchfab.com/3d-models/ant-322850e9020f4178a52bcd586a3c3c22)
- [Skybox Three.JS Generator](https://tools.wwwtyro.net/space-3d/index.html#animationSpeed=1&fov=80&nebulae=true&pointStars=true&resolution=1024&seed=63wbt5wg1m80&stars=true&sun=true)
- [Seb Lague YouTube Video on Marching Cubes](https://www.youtube.com/watch?v=M3iI2l0ltbE)