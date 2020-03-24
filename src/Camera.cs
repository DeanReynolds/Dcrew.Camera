using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Dcrew.MonoGame._2D_Camera
{
    /// <summary>A highly-optimized, flexible and powerful 2D camera</summary>
    public sealed class Camera
    {
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
                _rotM11 = MathF.Cos(-(_angle = value));
                _rotM12 = MathF.Sin(-_angle);
                _isDirty |= DirtyType.AngleOrScale;
            }
        }
        /// <summary>Scale/Zoom</summary>
        public Vector2 Scale
        {
            get => _scale;
            set
            {
                _scale = value;
                _isDirty |= DirtyType.AngleOrScale;
            }
        }
        /// <summary>Viewport resolution</summary>
        public (int Width, int Height) ViewportRes
        {
            get => _viewportRes;
            set
            {
                if (_viewportRes != value)
                {
                    UpdateViewportRes(value);
                    _isDirty |= DirtyType.AngleOrScale;
                    _projectionMatrix.M11 = (float)(2d / _viewportRes.Width);
                    _projectionMatrix.M22 = (float)(2d / -_viewportRes.Height);
                }
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
                _isDirty |= DirtyType.AngleOrScale;
            }
        }

        /// <summary>Origin/center-point (doesn't account for <see cref="Scale"/> or <see cref="Angle"/>)</summary>
        public Vector2 Origin { get; private set; }

        /// <summary>View/Transform matrix</summary>
        public Matrix View
        {
            get
            {
                if (_isDirty.HasFlag(DirtyType.AngleOrScale))
                {
                    UpdateScale();
                    _isDirty = 0;
                }
                else if (_isDirty.HasFlag(DirtyType.Pos))
                {
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
                if (_isDirty.HasFlag(DirtyType.AngleOrScale))
                {
                    UpdateScale();
                    _isDirty = 0;
                }
                return _scaleMatrix;
            }
        }
        public float VirtualScale => _virtualScale;

        /// <summary>Mouse/Cursor position, make sure to call <see cref="UpdateMousePos(MouseState?)"/> once per frame before using this</summary>
        public Vector2 MousePos => _mousePosition;
        /// <summary>Projection matrix</summary>
        public Matrix Projection => _projectionMatrix;

        Vector2 _position,
            _scale,
            _actualScale,
            _halfViewportRes,
            _mousePosition;
        float _angle,
            _rotM11,
            _rotM12,
            _invertM11,
            _invertM12,
            _invertM21,
            _invertM22,
            _invertM41,
            _invertM42,
            _virtualScale;
        double _n27;
        DirtyType _isDirty;
        Matrix _viewMatrix,
            _projectionMatrix,
            _originMatrix,
            _scaleMatrix;
        (int Width, int Height) _viewportRes,
            _virtualRes;

        [Flags]
        enum DirtyType : byte { Pos = 1, AngleOrScale = 2 }

        /// <summary>Create a 2D camera</summary>
        /// <param name="position">2D vector position</param>
        /// <param name="angle">Z rotation</param>
        /// <param name="scale">Scale/Zoom</param>
        /// <param name="viewportRes">Main viewport resolution</param>
        /// <param name="virtualRes">Virtual resolution</param>
        public Camera(Vector2 position, float angle, Vector2 scale, (int Width, int Height) viewportRes, (int Width, int Height) virtualRes)
        {
            _position.X = position.X;
            _position.Y = position.Y;
            _rotM11 = MathF.Cos(-(_angle = angle));
            _rotM12 = MathF.Sin(-_angle);
            _scale = scale;
            _virtualRes = virtualRes;
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
            ViewportRes = viewportRes;
        }
        /// <summary>Create a 2D camera</summary>
        /// <param name="position">2D vector position</param>
        /// <param name="angle">Z rotation</param>
        /// <param name="scale">Scale/Zoom</param>
        /// <param name="viewport">Main game viewport</param>
        /// <param name="virtualRes">Virtual resolution</param>
        public Camera(Vector2 position, float angle, Vector2 scale, Viewport viewport, (int Width, int Height) virtualRes) : this(position, angle, scale, (viewport.Width, viewport.Height), virtualRes) { }

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

        void UpdatePos()
        {
            float m41 = -_position.X * _actualScale.X,
                m42 = -_position.Y * _actualScale.Y;
            _viewMatrix.M41 = (m41 * _rotM11) + (m42 * -_rotM12) + _halfViewportRes.X;
            _viewMatrix.M42 = (m41 * _rotM12) + (m42 * _rotM11) + _halfViewportRes.Y;
            _invertM41 = (float)(-((double)_viewMatrix.M21 * -_viewMatrix.M42 - (double)_viewMatrix.M22 * -_viewMatrix.M41) * _n27);
            _invertM42 = (float)(((double)_viewMatrix.M11 * -_viewMatrix.M42 - (double)_viewMatrix.M12 * -_viewMatrix.M41) * _n27);
        }
        void UpdateScale()
        {
            _actualScale = new Vector2(_scale.X * _virtualScale, _scale.Y * _virtualScale);
            _viewMatrix.M11 = _actualScale.X * _rotM11;
            _viewMatrix.M12 = _actualScale.Y * _rotM12;
            _viewMatrix.M21 = _actualScale.X * -_rotM12;
            _viewMatrix.M22 = _actualScale.Y * _rotM11;
            _scaleMatrix.M11 = _actualScale.X;
            _scaleMatrix.M22 = _actualScale.Y;
            _n27 = 1d / ((double)_viewMatrix.M11 * _viewMatrix.M22 + (double)_viewMatrix.M12 * -_viewMatrix.M21);
            _invertM11 = (float)(_viewMatrix.M22 * _n27);
            _invertM21 = (float)(-_viewMatrix.M21 * _n27);
            _invertM12 = (float)-(_viewMatrix.M12 * _n27);
            _invertM22 = (float)(_viewMatrix.M11 * _n27);
            UpdatePos();
        }
        void UpdateOrigin()
        {
            _virtualScale = MathF.Min((float)_viewportRes.Width / _virtualRes.Width, (float)_viewportRes.Height / _virtualRes.Height);
            Origin = new Vector2(_viewportRes.Width / 2f / _virtualScale, _viewportRes.Height / 2f / _virtualScale);
            _originMatrix.M41 = Origin.X;
            _originMatrix.M42 = Origin.Y;
        }
        void UpdateViewportRes((int Width, int Height) value)
        {
            _viewportRes = value;
            _halfViewportRes = new Vector2(_viewportRes.Width / 2f, _viewportRes.Height / 2f);
            UpdateOrigin();
        }
    }
}