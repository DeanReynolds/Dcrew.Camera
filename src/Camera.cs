using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace Dcrew.MonoGame._2D_Camera
{
    /// <summary>A highly-optimized, flexible and powerful 2D camera</summary>
    class Camera
    {
        /// <summary>X position</summary>
        public float X
        {
            get => _position.X;
            set
            {
                _position.X = value;
                UpdateRotationX();
                UpdatePosition();
            }
        }
        /// <summary>Y position</summary>
        public float Y
        {
            get => _position.Y;
            set
            {
                _position.Y = value;
                UpdateRotationY();
                UpdatePosition();
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
                UpdateRotationX();
                UpdateRotationY();
                UpdatePosition();
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
                UpdateScale();
            }
        }
        /// <summary>Scale/Zoom</summary>
        public Vector2 Scale
        {
            get => _scale;
            set
            {
                _scale = value;
                UpdateScale();
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
                    _viewportRes = value;
                    _halfViewportRes = new Vector2(_viewportRes.Width / 2f, _viewportRes.Height / 2f);
                    _virtualScale = MathF.Min((float)_viewportRes.Width / _virtualRes.Width, (float)_viewportRes.Height / _virtualRes.Height);
                    UpdateApothem();
                    UpdateScale();
                    _projection.M11 = (float)(2d / _viewportRes.Width);
                    _projection.M22 = (float)(2d / -_viewportRes.Height);
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
                _virtualScale = MathF.Min((float)_viewportRes.Width / _virtualRes.Width, (float)_viewportRes.Height / _virtualRes.Height);
                UpdateApothem();
                UpdateScale();
            }
        }

        /// <summary>X/Y view-bound apothem (doesn't account for <see cref="Scale"/>)</summary>
        public Vector2 Apothem { get; private set; }

        /// <summary>View/Transform matrix</summary>
        public Matrix View => _view;
        /// <summary>Mouse/Cursor position, make sure to call <see cref="UpdateMousePos(MouseState?)"/> once per frame before using this</summary>
        public Vector2 MousePos => _mousePosition;
        /// <summary>Projection matrix</summary>
        public Matrix Projection => _projection;

        Vector2 _position,
            _scale,
            _actualScale,
            _halfViewportRes,
            _mousePosition;
        float _angle,
            _rotM11,
            _rotM12,
            _rotX1,
            _rotY1,
            _rotX2,
            _rotY2,
            _invertM11,
            _invertM12,
            _invertM21,
            _invertM22,
            _invertM41,
            _invertM42,
            _virtualScale;
        double _n27;
        Matrix _view,
            _projection;
        (int Width, int Height) _viewportRes,
            _virtualRes;

        /// <summary>Create a 2D camera</summary>
        /// <param name="position">2D vector position</param>
        /// <param name="angle">Z rotation</param>
        /// <param name="scale">Scale/Zoom</param>
        /// <param name="viewportRes">Viewport resolution</param>
        /// <param name="virtualRes">Virtual resolution</param>
        public Camera(Vector2 position, float angle, Vector2 scale, (int Width, int Height) viewportRes, (int Width, int Height) virtualRes)
        {
            _position.X = position.X;
            _position.Y = position.Y;
            _rotM11 = MathF.Cos(-(_angle = angle));
            _rotM12 = MathF.Sin(-_angle);
            _scale = scale;
            _viewportRes = viewportRes;
            _halfViewportRes = new Vector2(_viewportRes.Width / 2f, _viewportRes.Height / 2f);
            _view = new Matrix
            {
                M33 = 1,
                M44 = 1
            };
            VirtualRes = virtualRes;
            _projection = new Matrix
            {
                M11 = (float)(2d / _viewportRes.Width),
                M22 = (float)(2d / -_viewportRes.Height),
                M33 = -1,
                M41 = -1,
                M42 = 1,
                M44 = 1
            };
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

        void UpdateRotationX()
        {
            var m41 = -_position.X * _actualScale.X;
            _rotX1 = m41 * _rotM11;
            _rotX2 = m41 * _rotM12;
        }
        void UpdateRotationY()
        {
            var m42 = -_position.Y * _actualScale.Y;
            _rotY1 = m42 * -_rotM12;
            _rotY2 = m42 * _rotM11;
        }
        void UpdatePosition()
        {
            _view.M41 = _rotX1 + _rotY1 + _halfViewportRes.X;
            _view.M42 = _rotX2 + _rotY2 + _halfViewportRes.Y;
            _invertM41 = (float)(-((double)_view.M21 * -_view.M42 - (double)_view.M22 * -_view.M41) * _n27);
            _invertM42 = (float)(((double)_view.M11 * -_view.M42 - (double)_view.M12 * -_view.M41) * _n27);
        }
        void UpdateScale()
        {
            _actualScale = new Vector2(_scale.X * _virtualScale, _scale.Y * _virtualScale);
            UpdateRotationX();
            UpdateRotationY();
            _view.M11 = _actualScale.X * _rotM11;
            _view.M21 = _actualScale.X * -_rotM12;
            _view.M12 = _actualScale.Y * _rotM12;
            _view.M22 = _actualScale.Y * _rotM11;
            _n27 = 1d / ((double)_view.M11 * _view.M22 + (double)_view.M12 * -_view.M21);
            UpdatePosition();
            _invertM11 = (float)(_view.M22 * _n27);
            _invertM21 = (float)(-_view.M21 * _n27);
            _invertM12 = (float)-(_view.M12 * _n27);
            _invertM22 = (float)(_view.M11 * _n27);
        }
        void UpdateApothem() => Apothem = new Vector2(_viewportRes.Width / 2f / _virtualScale, _viewportRes.Height / 2f / _virtualScale);
    }
}