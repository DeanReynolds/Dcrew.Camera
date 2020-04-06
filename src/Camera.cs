using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Reflection;

namespace Dcrew.MonoGame._2D_Camera
{
    /// <summary>A highly-optimized, flexible and powerful 2D camera</summary>
    public sealed class Camera : IDisposable
    {
        static readonly Game _game;
        static readonly GraphicsDevice _graphicsDevice;
        static readonly GameWindow _window;

        static Camera()
        {
            foreach (var p in typeof(Game).GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static))
                if (p.GetValue(_game) is Game g)
                    _game = g;
            _graphicsDevice = _game.GraphicsDevice;
            _window = _game.Window;
        }

        /// <summary>X position</summary>
        public float X
        {
            get => _xy.X;
            set
            {
                _xy.X = value;
                _isDirty |= DirtyType.XY;
            }
        }
        /// <summary>Y position</summary>
        public float Y
        {
            get => _xy.Y;
            set
            {
                _xy.Y = value;
                _isDirty |= DirtyType.XY;
            }
        }
        /// <summary>X/Y position</summary>
        public Vector2 XY
        {
            get => _xy;
            set
            {
                _xy = value;
                _isDirty |= DirtyType.XY;
            }
        }
        /// <summary>Z rotation (in radians)</summary>
        public float Angle
        {
            get => _angle;
            set
            {
                _angle = value;
                _isDirty |= DirtyType.Angle;
            }
        }
        /// <summary>Scale/Zoom</summary>
        public Vector2 Zoom
        {
            get => _scale;
            set
            {
                _scale = value;
                _isDirty |= DirtyType.Scale;
            }
        }
        /// <summary>Virtual resolution</summary>
        public (int Width, int Height) VirtualRes
        {
            get => _virtualRes;
            set
            {
                _virtualRes = value;
                _hasVirtualRes = value.Width > 0 && value.Height > 0;
                UpdateOrigin();
                _isDirty |= DirtyType.Scale;
            }
        }
        /// <summary>Origin/center-point (doesn't account for <see cref="Zoom"/> or <see cref="Angle"/>)</summary>
        public Vector2 Origin { get; private set; }
        /// <summary>Scale for <see cref="VirtualRes"/> in relation to the games' viewport res</summary>
        public float VirtualScale { get; private set; }

        /// <summary>View/Transform matrix</summary>
        public Matrix View
        {
            get
            {
                UpdateDirtyView();
                return _viewMatrix;
            }
        }
        /// <summary>Invert of <see cref="View"/></summary>
        public Matrix Invert => _invertMatrix;
        /// <summary>Projection matrix</summary>
        public Matrix Projection => _projectionMatrix;
        /// <summary>Matrix dedicated to <see cref="Origin"/></summary>
        public Matrix OriginMatrix => _originMatrix;
        /// <summary>Matrix dedicated to <see cref="Zoom"/></summary>
        public Matrix ScaleMatrix
        {
            get
            {
                if (_isDirty.HasFlag(DirtyType.Scale))
                {
                    UpdateScale();
                    UpdateBounds();
                    _isDirty &= ~DirtyType.Scale;
                }
                return _scaleMatrix;
            }
        }
        /// <summary>A rectangle covering the view (in world coords). Accounts for <see cref="Angle"/> and <see cref="Zoom"/></summary>
        public Rectangle Bounds
        {
            get
            {
                UpdateDirtyView();
                return _viewBounds;
            }
        }

        /// <summary>Mouse/Cursor xyition, make sure to call <see cref="UpdateMouseXY(MouseState?)"/> once per frame before using this</summary>
        public Vector2 MouseXY => _mouseXY;

        Vector2 _xy,
            _scale,
            _origin,
            _mouseXY;
        float _angle,
            _rotCos,
            _rotSin,
            _n27;
        DirtyType _isDirty;
        Matrix _viewMatrix,
            _invertMatrix,
            _projectionMatrix,
            _originMatrix,
            _scaleMatrix;
        (int Width, int Height) _viewportRes,
            _virtualRes;
        bool _hasVirtualRes;
        Rectangle _viewBounds;

        [Flags]
        enum DirtyType : byte { XY = 1, Angle = 2, Scale = 4 }

        /// <summary>Create a 2D camera</summary>
        /// <param name="xy">X/Y position</param>
        /// <param name="angle">Z rotation (in radians)</param>
        /// <param name="scale">Scale/Zoom</param>
        /// <param name="virtualRes">Virtual resolution</param>
        public Camera(Vector2 xy, float angle, Vector2 scale, (int Width, int Height) virtualRes)
        {
            _xy = xy;
            _angle = angle;
            _scale = scale;
            _virtualRes = virtualRes;
            _hasVirtualRes = virtualRes.Width > 0 && virtualRes.Height > 0;
            UpdateViewportRes(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);
            Init();
            _scaleMatrix = _invertMatrix = _viewMatrix = new Matrix
            {
                M33 = 1,
                M44 = 1
            };
            _originMatrix = new Matrix
            {
                M11 = 1,
                M22 = 1,
                M33 = 1,
                M44 = 1
            };
            _projectionMatrix = new Matrix
            {
                M11 = (float)(2d / _viewportRes.Width),
                M22 = (float)(2d / -_viewportRes.Height),
                M33 = -1,
                M41 = -1,
                M42 = 1,
                M44 = 1
            };
            _isDirty |= DirtyType.Angle;
        }
        /// <summary>Create a 2D camera</summary>
        /// <param name="xy">X/Y position</param>
        /// <param name="angle">Z rotation (in radians)</param>
        /// <param name="zoom">Scale/Zoom</param>
        public Camera(Vector2 xy, float angle, Vector2 zoom) : this(xy, angle, zoom, (0, 0)) { }
        /// <summary>Create a 2D camera</summary>
        public Camera() : this(Vector2.Zero, 0, Vector2.One, (0, 0)) { }
        /// <summary>Create a 2D camera</summary>
        /// <param name="virtualRes">Virtual resolution</param>
        public Camera((int Width, int Height) virtualRes) : this(Vector2.Zero, 0, Vector2.One, virtualRes) { }
        /// <summary>Create a 2D camera</summary>
        /// <param name="xy">X/Y position</param>
        /// <param name="angle">Z rotation (in radians)</param>
        public Camera(Vector2 xy, float angle = 0) : this(xy, angle, Vector2.One, (0, 0)) { }
        /// <summary>Create a 2D camera</summary>
        /// <param name="xy">X/Y position</param>
        /// <param name="virtualRes">Virtual resolution</param>
        public Camera(Vector2 xy, (int Width, int Height) virtualRes) : this(xy, 0, Vector2.One, virtualRes) { }
        /// <summary>Create a 2D camera</summary>
        /// <param name="xy">X/Y position</param>
        /// <param name="angle">Z rotation (in radians)</param>
        /// <param name="virtualRes">Virtual resolution</param>
        public Camera(Vector2 xy, float angle, (int Width, int Height) virtualRes) : this(xy, angle, Vector2.One, virtualRes) { }

        /// <summary>Re-adds <see cref="Game.GraphicsDevice"/> and <see cref="Game.Window"/> reset/size-changed events (used for keeping <see cref="Origin"/> updated)
        /// ONLY CALL THIS IF <see cref="Dispose"/> HAS BEEN CALLED BEFORE THIS</summary>
        public void Init()
        {
            _graphicsDevice.DeviceReset += WindowSizeChanged;
            _window.ClientSizeChanged += WindowSizeChanged;
        }
        /// <summary>Removes <see cref="Game.GraphicsDevice"/> and <see cref="Game.Window"/> reset/size-changed events (used for keeping <see cref="Origin"/> updated)
        /// IF/WHEN RE-USING THIS CAMERA CALL <see cref="Init"/></summary>
        public void Dispose()
        {
            _window.ClientSizeChanged -= WindowSizeChanged;
            _graphicsDevice.DeviceReset -= WindowSizeChanged;
        }

        /// <summary>Converts screen coords to world coords</summary>
        public Vector2 ScreenToWorld(float x, float y)
        {
            UpdateDirtyView();
            return new Vector2(x * _invertMatrix.M11 + (y * _invertMatrix.M21) + _invertMatrix.M41, x * _invertMatrix.M12 + (y * _invertMatrix.M22) + _invertMatrix.M42);
        }
        /// <summary>Converts screen coords to world coords</summary>
        public Vector2 ScreenToWorld(Vector2 xy) => ScreenToWorld(xy.X, xy.Y);
        /// <summary>Converts screen coords to world coords</summary>
        public Point ScreenToWorld(Point xy) => ScreenToWorld(xy.X, xy.Y).ToPoint();
        /// <summary>Returns the scale of the world in relation to the screen</summary>
        public float ScreenToWorldScale() => 1 / Vector2.Distance(ScreenToWorld(0, 0), ScreenToWorld(1, 0));
        /// <summary>Converts world coords to screen coords</summary>
        public Vector2 WorldToScreen(float x, float y)
        {
            UpdateDirtyView();
            return new Vector2(x * _viewMatrix.M11 + (y * _viewMatrix.M21) + _viewMatrix.M41 + x, x * _viewMatrix.M12 + (y * _viewMatrix.M22) + _viewMatrix.M42 + y);
        }
        /// <summary>Converts world coords to screen coords</summary>
        public Vector2 WorldToScreen(Vector2 xy) => WorldToScreen(xy.X, xy.Y);
        /// <summary>Converts world coords to screen coords</summary>
        public Point WorldToScreen(Point xy) => WorldToScreen(xy.X, xy.Y).ToPoint();
        /// <summary>Returns the scale of the screen in relation to the world</summary>
        public float WorldToScreenScale() => Vector2.Distance(WorldToScreen(0, 0), WorldToScreen(1, 0));

        /// <summary>Call once per frame and before using <see cref="MouseXY"/></summary>
        /// <param name="mouseState">Null value will auto grab latest state</param>
        public void UpdateMouseXY(MouseState? mouseState = null)
        {
            mouseState ??= Mouse.GetState();
            int mouseX = mouseState.Value.Position.X,
                mouseY = mouseState.Value.Position.Y;
            _mouseXY = ScreenToWorld(mouseX, mouseY);
        }

        void UpdateXY()
        {
            float m41 = -_xy.X * _scaleMatrix.M11,
                m42 = -_xy.Y * _scaleMatrix.M22;
            _viewMatrix.M41 = (m41 * _rotCos) + (m42 * -_rotSin) + _origin.X;
            _viewMatrix.M42 = (m41 * _rotSin) + (m42 * _rotCos) + _origin.Y;
            float num19 = -_viewMatrix.M42;
            float num21 = -_viewMatrix.M41;
            _invertMatrix.M41 = (float)-(_viewMatrix.M21 * (double)num19 - _viewMatrix.M22 * (double)num21) * _n27;
            _invertMatrix.M42 = (float)(_viewMatrix.M11 * (double)num19 - _viewMatrix.M12 * (double)num21) * _n27;
        }
        void UpdateScale()
        {
            _scaleMatrix.M11 = _scale.X * VirtualScale;
            _scaleMatrix.M22 = _scale.Y * VirtualScale;
            _viewMatrix.M11 = _scaleMatrix.M11 * _rotCos;
            _viewMatrix.M12 = _scaleMatrix.M22 * _rotSin;
            _viewMatrix.M21 = _scaleMatrix.M11 * -_rotSin;
            _viewMatrix.M22 = _scaleMatrix.M22 * _rotCos;
            float n24 = -_viewMatrix.M21;
            _n27 = (float)(1 / (_viewMatrix.M11 * (double)_viewMatrix.M22 + _viewMatrix.M12 * (double)n24));
            _invertMatrix.M11 = _viewMatrix.M22 * _n27;
            _invertMatrix.M12 = -_viewMatrix.M12 * _n27;
            _invertMatrix.M21 = n24 * _n27;
            _invertMatrix.M22 = _viewMatrix.M11 * _n27;
            UpdateXY();
        }
        void UpdateAngle()
        {
            _rotCos = MathF.Cos(-_angle);
            _rotSin = MathF.Sin(-_angle);
            UpdateScale();
        }
        void UpdateOrigin()
        {
            VirtualScale = _hasVirtualRes ? MathF.Min((float)_viewportRes.Width / _virtualRes.Width, (float)_viewportRes.Height / _virtualRes.Height) : 1;
            Origin = new Vector2(_originMatrix.M41 = _origin.X / VirtualScale, _originMatrix.M42 = _origin.Y / VirtualScale);
            UpdateBounds();
        }
        void UpdateViewportRes(int width, int height)
        {
            _viewportRes = (width, height);
            _origin = new Vector2(width / 2f, height / 2f);
            UpdateOrigin();
            _isDirty |= DirtyType.Scale;
            _projectionMatrix.M11 = (float)(2d / _viewportRes.Width);
            _projectionMatrix.M22 = (float)(2d / -_viewportRes.Height);
        }
        void UpdateDirtyView()
        {
            if (_isDirty != 0)
            {
                if (_isDirty.HasFlag(DirtyType.Angle))
                    UpdateAngle();
                else if (_isDirty.HasFlag(DirtyType.Scale))
                    UpdateScale();
                else if (_isDirty.HasFlag(DirtyType.XY))
                    UpdateXY();
                UpdateBounds();
                _isDirty = 0;
            }
        }
        void UpdateBounds()
        {
            float tOX = Origin.X * 2,
                tOY = Origin.Y * 2,
                w = MathF.Ceiling(tOX / _scale.X) + .5f,
                h = MathF.Ceiling(tOY / _scale.Y) + .5f,
                oX = w / 2,
                oY = h / 2;
            Vector2 RotatePoint(float x, float y)
            {
                x -= oX;
                y -= oY;
                return new Vector2(x * _rotCos - y * _rotSin, x * _rotSin + y * _rotCos);
            }
            Vector2 tL = RotatePoint(0, 0),
                tR = RotatePoint(w, 0),
                bR = RotatePoint(w, h),
                bL = RotatePoint(0, h);
            int minX = (int)MathF.Min(MathF.Min(tL.X, tR.X), MathF.Min(bR.X, bL.X)),
                minY = (int)MathF.Min(MathF.Min(tL.Y, tR.Y), MathF.Min(bR.Y, bL.Y)),
                maxX = (int)MathF.Ceiling(MathF.Max(MathF.Max(tL.X, tR.X), MathF.Max(bR.X, bL.X))),
                maxY = (int)MathF.Ceiling(MathF.Max(MathF.Max(tL.Y, tR.Y), MathF.Max(bR.Y, bL.Y)));
            _viewBounds = new Rectangle(minX, minY, maxX - minX, maxY - minY);
            _viewBounds.Offset(_xy);
        }

        void WindowSizeChanged(object sender, EventArgs e)
        {
            if (_hasVirtualRes)
                ScaleViewportToVirtualRes();
            UpdateViewportRes(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);
        }

        void ScaleViewportToVirtualRes()
        {
            var targetAspectRatio = _virtualRes.Width / (float)_virtualRes.Height;
            var width2 = _graphicsDevice.PresentationParameters.BackBufferWidth;
            var height2 = (int)(width2 / targetAspectRatio + .5f);
            if (height2 > _graphicsDevice.PresentationParameters.BackBufferHeight)
            {
                height2 = _graphicsDevice.PresentationParameters.BackBufferHeight;
                width2 = (int)(height2 * targetAspectRatio + .5f);
            }
            _graphicsDevice.SetRenderTarget(null);
            _graphicsDevice.Viewport = new Viewport((_graphicsDevice.PresentationParameters.BackBufferWidth / 2) - (width2 / 2), (_graphicsDevice.PresentationParameters.BackBufferHeight / 2) - (height2 / 2), width2, height2);
        }
    }
}