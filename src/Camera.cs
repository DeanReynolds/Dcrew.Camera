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

        static (int Width, int Height) _oldViewportRes,
            _oldRes;

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
            get => _position.X;
            set
            {
                _position.X = value;
                _isDirty |= DirtyType.Pos;
            }
        }
        /// <summary>Y position</summary>
        public float Y
        {
            get => _position.Y;
            set
            {
                _position.Y = value;
                _isDirty |= DirtyType.Pos;
            }
        }
        /// <summary>2D vector position</summary>
        public Vector2 Pos
        {
            get => _position;
            set
            {
                _position.X = value.X;
                _position.Y = value.Y;
                _isDirty |= DirtyType.Pos;
            }
        }
        /// <summary>Z rotation</summary>
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
        public Vector2 Scale
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
                UpdateOrigin();
                _isDirty |= DirtyType.Scale;
            }
        }
        /// <summary>Origin/center-point (doesn't account for <see cref="Scale"/> or <see cref="Angle"/>)</summary>
        public Vector2 Origin { get; private set; }

        /// <summary>View/Transform matrix</summary>
        public Matrix View
        {
            get
            {
                if (_isDirty != 0)
                {
                    if (_isDirty.HasFlag(DirtyType.Angle))
                        UpdateAngle();
                    else if (_isDirty.HasFlag(DirtyType.Scale))
                        UpdateScale();
                    else if (_isDirty.HasFlag(DirtyType.Pos))
                        UpdatePos();
                    _isDirty = 0;
                }
                return _viewMatrix;
            }
        }
        public Matrix OriginMatrix => _originMatrix;
        public Matrix ScaleMatrix
        {
            get
            {
                if (_isDirty.HasFlag(DirtyType.Scale))
                {
                    UpdateScale();
                    _isDirty &= ~DirtyType.Scale;
                }
                return _scaleMatrix;
            }
        }
        public float VirtualScale { get; private set; }

        /// <summary>Mouse/Cursor position, make sure to call <see cref="UpdateMousePos(MouseState?)"/> once per frame before using this</summary>
        public Vector2 MousePos => _mousePosition;
        /// <summary>Projection matrix</summary>
        public Matrix Projection => _projectionMatrix;

        Vector2 _position,
            _scale,
            _origin,
            _mousePosition;
        float _angle,
            _rotCos,
            _rotSin,
            _invertM11,
            _invertM12,
            _invertM21,
            _invertM22,
            _invertM41,
            _invertM42;
        double _n27;
        DirtyType _isDirty;
        Matrix _viewMatrix,
            _projectionMatrix,
            _originMatrix,
            _scaleMatrix;
        (int Width, int Height) _viewportRes,
            _virtualRes;

        [Flags]
        enum DirtyType : byte { Pos = 1, Angle = 2, Scale = 4 }

        /// <summary>Create a 2D camera</summary>
        /// <param name="position">2D vector position</param>
        /// <param name="angle">Z rotation</param>
        /// <param name="scale">Scale/Zoom</param>
        /// <param name="virtualRes">Virtual resolution</param>
        public Camera(Vector2 position, float angle, Vector2 scale, (int Width, int Height) virtualRes)
        {
            _position = position;
            _angle = angle;
            _scale = scale;
            _virtualRes = virtualRes;
            UpdateViewportRes(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);
            _graphicsDevice.DeviceReset += WindowSizeChanged;
            _window.ClientSizeChanged += WindowSizeChanged;
            _scaleMatrix = _viewMatrix = new Matrix
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

        /// <summary>Call once per frame and before using <see cref="MousePos"/></summary>
        /// <param name="mouseState">Null value will auto grab latest state</param>
        public void UpdateMousePos(MouseState? mouseState = null)
        {
            mouseState ??= Mouse.GetState();
            int mouseX = mouseState.Value.Position.X,
                mouseY = mouseState.Value.Position.Y;
            _mousePosition.X = mouseX * _invertM11 + (mouseY * _invertM21) + _invertM41;
            _mousePosition.Y = mouseX * _invertM12 + (mouseY * _invertM22) + _invertM42;
        }

        /// <summary>Removes <see cref="Game.GraphicsDevice"/> and <see cref="Game.Window"/> reset/size-changed events for keeping <see cref="Origin"/> updated</summary>
        public void Dispose()
        {
            _window.ClientSizeChanged -= WindowSizeChanged;
            _graphicsDevice.DeviceReset -= WindowSizeChanged;
        }

        void UpdatePos()
        {
            float m41 = -_position.X * _scaleMatrix.M11,
                m42 = -_position.Y * _scaleMatrix.M22;
            _viewMatrix.M41 = (m41 * _rotCos) + (m42 * -_rotSin) + _origin.X;
            _viewMatrix.M42 = (m41 * _rotSin) + (m42 * _rotCos) + _origin.Y;
            _invertM41 = (float)(-((double)_viewMatrix.M21 * -_viewMatrix.M42 - (double)_viewMatrix.M22 * -_viewMatrix.M41) * _n27);
            _invertM42 = (float)(((double)_viewMatrix.M11 * -_viewMatrix.M42 - (double)_viewMatrix.M12 * -_viewMatrix.M41) * _n27);
        }
        void UpdateScale()
        {
            _scaleMatrix.M11 = _scale.X * VirtualScale;
            _scaleMatrix.M22 = _scale.Y * VirtualScale;
            _viewMatrix.M11 = _scaleMatrix.M11 * _rotCos;
            _viewMatrix.M12 = _scaleMatrix.M22 * _rotSin;
            _viewMatrix.M21 = _scaleMatrix.M11 * -_rotSin;
            _viewMatrix.M22 = _scaleMatrix.M22 * _rotCos;
            _n27 = 1d / ((double)_viewMatrix.M11 * _viewMatrix.M22 + (double)_viewMatrix.M12 * -_viewMatrix.M21);
            _invertM11 = (float)(_viewMatrix.M22 * _n27);
            _invertM12 = (float)-(_viewMatrix.M12 * _n27);
            _invertM21 = (float)(-_viewMatrix.M21 * _n27);
            _invertM22 = (float)(_viewMatrix.M11 * _n27);
            UpdatePos();
        }
        void UpdateAngle()
        {
            _rotCos = MathF.Cos(-_angle);
            _rotSin = MathF.Sin(-_angle);
            UpdateScale();
        }
        void UpdateOrigin()
        {
            VirtualScale = MathF.Min((float)_viewportRes.Width / _virtualRes.Width, (float)_viewportRes.Height / _virtualRes.Height);
            Origin = new Vector2(_originMatrix.M41 = _origin.X / VirtualScale, _originMatrix.M42 = _origin.Y / VirtualScale);
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

        void WindowSizeChanged(object sender, EventArgs e) => ScaleViewportToVirtualRes();

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
            UpdateViewportRes(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);
        }
    }
}