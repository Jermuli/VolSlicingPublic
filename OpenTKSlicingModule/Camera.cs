using OpenTK.Mathematics;
using System;

namespace OpenTKSlicingModule
{

    // Heavily modified camera class from the OpenTK learning demos. https://github.com/opentk/LearnOpenTK/blob/master/Common/Camera.cs
    public class Camera
    {
        // Those vectors are directions pointing outwards from the camera to define how it rotated.
        private Vector3 _front = -Vector3.UnitZ;

        private Vector3 _up = Vector3.UnitY;

        private Vector3 _right = Vector3.UnitX;

        private float _fov = MathHelper.PiOver2*2/3;

        /// <summary>
        /// Constructor for the cam class
        /// </summary>
        /// <param name="position">Cam position</param>
        /// <param name="aspectRatio">Aspect ratio</param>
        public Camera(Vector3 position, float aspectRatio, bool orth)
        {
            Position = position;
            AspectRatio = aspectRatio;
            IsOrthographic = orth;
        }

        private Vector3 _pos = (0f,0f,0f);

        // The position of the camera
        public Vector3 Position { 
            get => _pos;
            set 
            {
                _pos = value;
                UpdateVectors();
            } 
        }

        // This is simply the aspect ratio of the viewport, used for the projection matrix.
        public float AspectRatio { private get; set; }

        public Vector2 ViewSize { get; set; } = new Vector2(1f, 1f);

        public float Zoom { get; set; } = 1f;

        public bool IsOrthographic { get; set; } = true;

        public Vector3 Front => _front;

        public Vector3 Up => _up;

        public Vector3 Right => _right;

        public Vector3 InvUp = (0, 1f, 0);

        // The field of view (FOV) is the vertical angle of the camera view.
        // This has been discussed more in depth in a previous tutorial,
        // but in this tutorial, you have also learned how we can use this to simulate a zoom feature.
        // We convert from degrees to radians as soon as the property is set to improve performance.
        public float Fov
        {
            get => MathHelper.RadiansToDegrees(_fov);
            set
            {
                var angle = MathHelper.Clamp(value, 1f, 90f);
                _fov = MathHelper.DegreesToRadians(angle);
            }
        }

        public Vector3 offset = new Vector3(0f, 0f, 0f);

        public float farClipPlane = 100f;

        // Get the view matrix using the amazing LookAt function described more in depth on the web tutorials
        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position + offset, offset, _up);
        }

        // Get the projection matrix using the same method we have used up until this point
        public Matrix4 GetProjectionMatrix()
        {
            if (IsOrthographic) return Matrix4.CreateOrthographic(ViewSize.X * Zoom * 7, ViewSize.Y * Zoom * 7, 1f, farClipPlane);
            return Matrix4.CreatePerspectiveFieldOfView(_fov, AspectRatio, 1f, farClipPlane);
            /*if(IsOrthographic) return Matrix4.CreateOrthographic(ViewSize.X*Zoom*7, ViewSize.Y*Zoom*7 ,1f, farClipPlane);
            return Matrix4.CreatePerspectiveFieldOfView(_fov, AspectRatio, 1f, farClipPlane);*/
        }

        public void SetClipPlane(float plane) {
            farClipPlane = plane;
        }


        // This function is going to update the direction vertices using some of the math learned in the web tutorials.
        private void UpdateVectors()
        {
            Vector3 camDir = Vector3.Normalize(Position);
            //_right = Vector3.Normalize(Vector3.Cross(InvUp, camDir));
            _right = Vector3.Normalize(Vector3.Cross(_up, camDir));
            _up = Vector3.Cross(camDir, _right);
        }
    }
}
