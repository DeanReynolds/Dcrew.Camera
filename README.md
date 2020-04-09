# Dcrew.MonoGame.2D Camera
 A highly-optimized, flexible and powerful 2D camera for [MonoGame](https://github.com/MonoGame/MonoGame)

## Build
### [NuGet](https://www.nuget.org/packages/Dcrew.MonoGame.2D_Camera) [![NuGet ver](https://img.shields.io/nuget/v/Dcrew.MonoGame.2D_Camera)](https://www.nuget.org/packages/Dcrew.MonoGame.2D_Camera) [![NuGet downloads](https://img.shields.io/nuget/dt/Dcrew.MonoGame.2D_Camera)](https://www.nuget.org/packages/Dcrew.MonoGame.2D_Camera)

## Features
- Efficient math used for 2D matrices
- Parallax system (Z coord, optional), ground (usual 2D gameplay level) is at Z 0 and the camera is at 1
  - e.g. if you wanted to draw a sprite(s) behind and half the size & position of ground, you'd use [Camera.View](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L206)(-0.5f) as the transformMatrix in SpriteBatch.Begin() and you would draw this before drawing the ground using [Camera.View](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L206)()
- [ScreenToWorld](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L250)(float x, float y, float z = 0) - Converts screen coords to world coords
- [ScreenToWorld](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L256)(Vector2 xy) - Converts screen coords to world coords
- [ScreenToWorld](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L258)(Point xy) - Converts screen coords to world coords
- [WorldToScreen](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L262)(float x, float y, float z = 0) - Converts world coords to screen coords
- [WorldToScreen](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L268)(Vector2 xy) - Converts world coords to screen coords
- [WorldToScreen](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L270)(Point xy) - Converts world coords to screen coords
