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
            set => _xy.X = value;
        }
        /// <summary>Y position</summary>
        public float Y
        {
            get => _xy.Y;
            set => _xy.Y = value;
        }
        /// <summary>X/Y position</summary>
        public Vector2 XY
        {
            get => _xy;
            set
            {
                _xy = value;
                _xyz.X = value.X;
                _xyz.Y = value.Y;
            }
        }
        /// <summary>Z position</summary>
        public float Z
        {
            get => _xyz.Z;
            set => _xyz.Z = value;
        }
        /// <summary>X/Y/Z Position</summary>
        public Vector3 XYZ
        {
            get => _xyz;
            set => _xyz = value;
        }
        /// <summary>Z rotation (in radians)</summary>
        public float Angle
        {
            get => _angle;
            set
            {
                _angle = value;
                _angleDirty = true;
            }
        }
        /// <summary>Scale/Zoom</summary>
        public Vector2 Scale
        {
            get => _scale;
            set
            {
                _scale = value;
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
            }
        }
        /// <summary>Origin/center-point (doesn't account for <see cref="Scale"/> or <see cref="Angle"/>)</summary>
        public Vector2 Origin { get; private set; }
        /// <summary>Scale for <see cref="VirtualRes"/> in relation to the games' viewport res</summary>
        public float VirtualScale { get; private set; }

        /// <summary>Projection matrix</summary>
        public Matrix Projection => _projectionMatrix;
        /// <summary>Matrix dedicated to <see cref="Origin"/></summary>
        public Matrix OriginMatrix => _originMatrix;
        /// <summary>A rectangle covering the view (in screen coords). Accounts for <see cref="Angle"/> and <see cref="Scale"/>. Use <see cref="Rectangle.Offset(float, float)"/> with <see cref="XY"/> to get it in world position or is <see cref="WorldBounds(float[])"/> what you're looking for?</summary>
        public Rectangle Bounds
        {
            get
            {
                UpdateDirtyAngle();
                return _viewBounds;
            }
        }

        /// <summary>Mouse/Cursor world X/Y position, make sure to call <see cref="UpdateMouseXY(MouseState?)"/> once per frame before using this</summary>
        public Vector2 MouseXY => _mouseXY;
        /// <summary>Mouse/Cursor world X position, make sure to call <see cref="UpdateMouseXY(MouseState?)"/> once per frame before using this</summary>
        public float MouseX => _mouseXY.X;
        /// <summary>Mouse/Cursor world Y position, make sure to call <see cref="UpdateMouseXY(MouseState?)"/> once per frame before using this</summary>
        public float MouseY => _mouseXY.Y;

        Vector2 _xy,
            _scale,
            _origin,
            _mouseXY;
        float _angle,
            _rotCos,
            _rotSin;
        Vector3 _xyz = new Vector3(0, 0, 1);
        Matrix _projectionMatrix,
            _originMatrix;
        (int Width, int Height) _viewportRes,
            _virtualRes;
        bool _hasVirtualRes,
            _angleDirty;
        Rectangle _viewBounds;

        /// <summary>Create a 2D camera</summary>
        /// <param name="xy">X/Y position</param>
        /// <param name="angle">Z rotation (in radians)</param>
        /// <param name="scale">Scale/Zoom</param>
        /// <param name="virtualRes">Virtual resolution</param>
        public Camera(Vector2 xy, float angle, Vector2 scale, (int Width, int Height) virtualRes)
        {
            _xy = xy;
            Angle = angle;
            _scale = scale;
            _virtualRes = virtualRes;
            _hasVirtualRes = virtualRes.Width > 0 && virtualRes.Height > 0;
            UpdateViewportRes(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);
            Init();
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
        }
        /// <summary>Create a 2D camera</summary>
        /// <param name="xy">X/Y position</param>
        /// <param name="angle">Z rotation (in radians)</param>
        /// <param name="scale">Scale/Zoom</param>
        public Camera(Vector2 xy, float angle, Vector2 scale) : this(xy, angle, scale, (0, 0)) { }
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

        /// <summary>Returns true if sprites drawn using <see cref="View(float)"/> at Z <paramref name="z"/> should be visible/drawn</summary>
        public bool IsVisible(float z)
        {
            var zoomFromZ = ScaleFromZ(Z, z);
            return zoomFromZ > 0 && zoomFromZ < 10;
        }
        /// <summary>The scale matrix at Z <paramref name="z"/></summary>
        public Matrix ScaleMatrix(float z = 0)
        {
            var matrix = new Matrix { M33 = 1, M44 = 1 };
            var zoomFromZ = ScaleFromZ(Z, z);
            matrix.M11 = _scale.X * VirtualScale * zoomFromZ;
            matrix.M22 = _scale.Y * VirtualScale * zoomFromZ;
            return matrix;
        }
        /// <summary>The view/transform matrix at Z <paramref name="z"/></summary>
        public Matrix View(float z = 0)
        {
            UpdateDirtyAngle();
            var matrix = new Matrix { M33 = 1, M44 = 1 };
            float zoomFromZ = ScaleFromZ(Z, z),
                scaleM11 = _scale.X * VirtualScale * zoomFromZ,
                scaleM22 = _scale.Y * VirtualScale * zoomFromZ,
                m41 = -_xy.X * scaleM11,
                m42 = -_xy.Y * scaleM22;
            matrix.M41 = (m41 * _rotCos) + (m42 * -_rotSin) + _origin.X;
            matrix.M42 = (m41 * _rotSin) + (m42 * _rotCos) + _origin.Y;
            matrix.M11 = scaleM11 * _rotCos;
            matrix.M12 = scaleM22 * _rotSin;
            matrix.M21 = scaleM11 * -_rotSin;
            matrix.M22 = scaleM22 * _rotCos;
            return matrix;
        }
        /// <summary>The inveart matrix of <see cref="View(float)"/> at Z <paramref name="z"/></summary>
        public Matrix ViewInvert(float z = 0)
        {
            UpdateDirtyAngle();
            var matrix = new Matrix { M33 = 1, M44 = 1 };
            float zoomFromZ = ScaleFromZ(Z, z),
                scaleM11 = _scale.X * VirtualScale * zoomFromZ,
                scaleM22 = _scale.Y * VirtualScale * zoomFromZ,
                m41 = -_xy.X * scaleM11,
                m42 = -_xy.Y * scaleM22,
                viewM11 = scaleM11 * _rotCos,
                viewM12 = scaleM22 * _rotSin,
                viewM21 = scaleM11 * -_rotSin,
                viewM22 = scaleM22 * _rotCos,
                num19 = -((m41 * _rotSin) + (m42 * _rotCos) + _origin.Y),
                num21 = -((m41 * _rotCos) + (m42 * -_rotSin) + _origin.X),
                n24 = -viewM21,
                n27 = (float)(1 / (viewM11 * (double)viewM22 + viewM12 * (double)n24));
            matrix.M41 = (float)-(viewM21 * (double)num19 - viewM22 * (double)num21) * n27;
            matrix.M42 = (float)(viewM11 * (double)num19 - viewM12 * (double)num21) * n27;
            matrix.M11 = viewM22 * n27;
            matrix.M12 = -viewM12 * n27;
            matrix.M21 = n24 * n27;
            matrix.M22 = viewM11 * n27;
            return matrix;
        }
        /// <summary>Converts screen coords to world coords</summary>
        public Vector2 ScreenToWorld(float x, float y, float z = 0)
        {
            var invert = ViewInvert(z);
            return new Vector2(x * invert.M11 + (y * invert.M21) + invert.M41, x * invert.M12 + (y * invert.M22) + invert.M42);
        }
        /// <summary>Converts screen coords to world coords</summary>
        public Vector2 ScreenToWorld(Vector2 xy, float z = 0) => ScreenToWorld(xy.X, xy.Y, z);
        /// <summary>Converts screen coords to world coords</summary>
        public Vector2 ScreenToWorld(Vector3 xyz) => ScreenToWorld(xyz.X, xyz.Y, xyz.Z);
        /// <summary>Converts screen coords to world coords</summary>
        public Point ScreenToWorld(Point xy, float z = 0) => ScreenToWorld(xy.X, xy.Y, z).ToPoint();
        /// <summary>Returns the scale of the world at Z <paramref name="z"/> in relation to the screen</summary>
        public float ScreenToWorldScale(float z = 0) => 1 / Vector2.Distance(ScreenToWorld(0, 0, z), ScreenToWorld(1, 0, z));
        /// <summary>Converts world coords to screen coords</summary>
        public Vector2 WorldToScreen(float x, float y, float z = 0)
        {
            var view = View(z);
            return new Vector2(x * view.M11 + (y * view.M21) + view.M41 + x, x * view.M12 + (y * view.M22) + view.M42 + y);
        }
        /// <summary>Converts world coords to screen coords</summary>
        public Vector2 WorldToScreen(Vector2 xy, float z = 0) => WorldToScreen(xy.X, xy.Y, z);
        /// <summary>Converts world coords to screen coords</summary>
        public Vector2 WorldToScreen(Vector3 xyz) => WorldToScreen(xyz.X, xyz.Y, xyz.Z);
        /// <summary>Converts world coords to screen coords</summary>
        public Point WorldToScreen(Point xy, float z = 0) => WorldToScreen(xy.X, xy.Y, z).ToPoint();
        /// <summary>Returns the scale of the screen in relation to the world at Z <paramref name="z"/></summary>
        public float WorldToScreenScale(float z = 0) => Vector2.Distance(WorldToScreen(0, 0, z), WorldToScreen(1, 0, z));

        /// <summary>The scale of <see cref="View(float)"/> at Z <paramref name="targetZ"/> from <paramref name="z"/></summary>
        public float ScaleFromZ(float z, float targetZ) => z - targetZ == 0 ? 0 : 1 / (z - targetZ);
        /// <summary>The camera Z required for sprites at Z <paramref name="targetZ"/> that should be drawn at scale <paramref name="zoom"/></summary>
        public float ZFromScale(float zoom, float targetZ) => 1 / zoom + targetZ;

        /// <summary>A rectangle covering the view (in world coords). Accounts for <see cref="Angle"/>, <see cref="Scale"/> and every Z depth/layer in <paramref name="z"/></summary>
        public Rectangle WorldBounds(params float[] z)
        {
            UpdateDirtyAngle();
            if (z.Length == 0)
            {
                var vb = _viewBounds;
                vb.Offset(_xy);
                return vb;
            }
            Rectangle? r = null;
            foreach (var l in z)
            {
                float zoomFromZ = ScaleFromZ(Z, l);
                var dr = _viewBounds;
                dr.Offset(X * zoomFromZ, Y * zoomFromZ);
                dr.Width = (int)MathF.Ceiling(dr.Width / zoomFromZ);
                dr.Height = (int)MathF.Ceiling(dr.Height / zoomFromZ);
                if (r == null)
                    r = dr;
                else
                    r = Rectangle.Union(r.Value, dr);
            }
            return r.Value;
        }

        /// <summary>Will update <see cref="MouseXY"/>. Call once per frame and before using <see cref="MouseXY"/></summary>
        /// <param name="mouseState">null value will auto grab latest state</param>
        public void UpdateMouseXY(MouseState? mouseState = null)
        {
            mouseState ??= Mouse.GetState();
            int mouseX = mouseState.Value.Position.X,
                mouseY = mouseState.Value.Position.Y;
            _mouseXY = ScreenToWorld(mouseX, mouseY);
        }

        void UpdateDirtyAngle()
        {
            if (_angleDirty)
            {
                _rotCos = MathF.Cos(-Angle);
                _rotSin = MathF.Sin(-Angle);
                UpdateBounds();
                _angleDirty = false;
            }
        }
        void UpdateOrigin()
        {
            VirtualScale = _hasVirtualRes ? MathF.Min((float)_viewportRes.Width / _virtualRes.Width, (float)_viewportRes.Height / _virtualRes.Height) : 1;
            Origin = new Vector2(_originMatrix.M41 = _origin.X / VirtualScale, _originMatrix.M42 = _origin.Y / VirtualScale);
            UpdateDirtyAngle();
        }
        void UpdateViewportRes(int width, int height)
        {
            _viewportRes = (width, height);
            _origin = new Vector2(width / 2f, height / 2f);
            UpdateOrigin();
            _projectionMatrix.M11 = (float)(2d / _viewportRes.Width);
            _projectionMatrix.M22 = (float)(2d / -_viewportRes.Height);
        }
        void UpdateBounds()
        {
            float x = -Origin.X,
                y = -Origin.Y,
                w = MathF.Ceiling(Origin.X / _scale.X) + .5f,
                h = MathF.Ceiling(Origin.Y / _scale.Y) + .5f,
                xcos = x * _rotCos,
                ycos = y * _rotCos,
                xsin = x * _rotSin,
                ysin = y * _rotSin,
                wcos = w * _rotCos,
                wsin = w * _rotSin,
                hcos = h * _rotCos,
                hsin = h * _rotSin,
                tlx = xcos - ysin,
                tly = xsin + ycos,
                trx = wcos - ysin,
                tr_y = wsin + ycos,
                brx = wcos - hsin,
                bry = wsin + hcos,
                blx = xcos - hsin,
                bly = xsin + hcos,
                minx = tlx,
                miny = tly,
                maxx = minx,
                maxy = miny;
            if (trx < minx)
                minx = trx;
            if (brx < minx)
                minx = brx;
            if (blx < minx)
                minx = blx;
            if (tr_y < miny)
                miny = tr_y;
            if (bry < miny)
                miny = bry;
            if (bly < miny)
                miny = bly;
            if (trx > maxx)
                maxx = trx;
            if (brx > maxx)
                maxx = brx;
            if (blx > maxx)
                maxx = blx;
            if (tr_y > maxy)
                maxy = tr_y;
            if (bry > maxy)
                maxy = bry;
            if (bly > maxy)
                maxy = bly;
            var r = new Rectangle((int)minx, (int)miny, (int)MathF.Ceiling(maxx - minx), (int)MathF.Ceiling(maxy - miny));
            _viewBounds = r;
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