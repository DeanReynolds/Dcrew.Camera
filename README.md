# Dcrew.MonoGame.2D Camera
 A highly-optimized, flexible and powerful 2D camera for [MonoGame](https://github.com/MonoGame/MonoGame)

## Build
### [NuGet](https://www.nuget.org/packages/Dcrew.MonoGame.2D_Camera) [![NuGet ver](https://img.shields.io/nuget/v/Dcrew.MonoGame.2D_Camera)](https://www.nuget.org/packages/Dcrew.MonoGame.2D_Camera) [![NuGet downloads](https://img.shields.io/nuget/dt/Dcrew.MonoGame.2D_Camera)](https://www.nuget.org/packages/Dcrew.MonoGame.2D_Camera)

## Features
- Efficient math used for 2D matrices
- Parallax system (Z coord, optional), ground (usual 2D gameplay level) is at Z 0 and the camera is at 1
  - e.g. if you wanted to draw a sprite(s) behind and half the size & position of ground, you'd use [Camera.View](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L206)(-0.5f) as the transformMatrix in SpriteBatch.Begin() and you would draw this before drawing the ground using [Camera.View](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L206)()
- [ScreenToWorld](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L250)(float x, float y, float z = 0) - Converts screen coords to world coords
- [ScreenToWorld](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L260)(Vector2 xy, float z = 0) - Converts screen coords to world coords
- [ScreenToWorld](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L262)(Vector3 xyz) - Converts screen coords to world coords
- [ScreenToWorld](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L264)(Point xy) - Converts screen coords to world coords
- [ScreenToWorldScale](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L266)(float z = 0) - Returns the scale of the world at Z z in relation to the screen
- [WorldToScreen](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L268)(float x, float y, float z = 0) - Converts world coords to screen coords
- [WorldToScreen](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L274)(Vector2 xy, float z = 0) - Converts world coords to screen coords
- [WorldToScreen](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L276)(Vector3 xyz) - Converts world coords to screen coords
- [WorldToScreen](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L278)(Point xy, float z = 0) - Converts world coords to screen coords
- [WorldToScreenScale](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L280)(float z = 0) - Returns the scale of the screen in relation to the world at Z z
- [UpdateMouseXY](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L289)(MouseState? mouseState = null) - Will update Camera.[MouseXY](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L102). Call once per frame and before using Camera.[MouseXY](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L102)
  - null value will auto grab latest state
- [MouseXY](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L102) - Mouse/Cursor world X/Y position, make sure to call [UpdateMouseXY](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L289)(MouseState? mouseState = null) once per frame before using this
  - Camera.[MouseX](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L104) - will return Camera.[MouseXY](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L102).X
  - Camera.[MouseY](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L106) - will return Camera.[MouseXY](https://github.com/DeanReynolds/Dcrew.MonoGame.2D-Camera/blob/master/src/Camera.cs#L102).Y
