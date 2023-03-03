using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Diagnostics;

namespace OpenTKSlicingModule
{
    public class Scene
    {
        /// <summary>
        /// Initialize numerous variables required for the scene making process.
        /// </summary>
        #region Variables
        private float[] verts = {
                          -0.5f,  0.5f, -0.5f,
                          0.5f,  0.5f, -0.5f,
                          0.5f, -0.5f, -0.5f,
                          -0.5f, -0.5f, -0.5f,
                          -0.5f,  0.5f,  0.5f,
                          0.5f,  0.5f,  0.5f,
                          0.5f, -0.5f,  0.5f,
                          -0.5f, -0.5f,  0.5f
        };

        private uint[] inds = { 0, 1,
                            1, 2,
                            2, 3,
                            3, 0,
                            4, 5,
                            5, 6,
                            6, 7,
                            7, 4,
                            0, 4,
                            1, 5,
                            2, 6,
                            3, 7
        };

        private float[] vertsSq = new float[]{
                          -MathF.Pow(3 * 0.125f, 1f/3f), MathF.Pow(3 * 0.125f, 1f/3f), 0f,
                          MathF.Pow(3 * 0.125f, 1f/3f), MathF.Pow(3 * 0.125f, 1f/3f), 0f,
                          MathF.Pow(3 * 0.125f, 1f/3f),  -MathF.Pow(3 * 0.125f, 1f/3f), 0f,
                          -MathF.Pow(3 * 0.125f, 1f/3f), -MathF.Pow(3 * 0.125f, 1f/3f), 0f
        };

        private uint[] indsSq = { 0, 1, 2,
                                  0, 3, 2

        };

        private float[] vertsMap = new float[]{
                          -0.05f, -0.05f, -0.05f,   1.0f, 0f, 0f,
                          0.05f, -0.05f, -0.05f,    1.0f, 0f, 0f,
                          -0.05f, -0.05f, -0.05f,   0f, 1.0f, 0f,
                          -0.05f, 0.05f, -0.05f,    0f, 1.0f, 0f,
                          -0.05f, -0.05f, -0.05f,   0f, 0f, 1.0f,
                          -0.05f, -0.05f, 0.05f,    0f, 0f, 1.0f

        };

        private uint[] indsMap = { 0, 1,
                                   2, 3,
                                   4, 5
        };

        private uint[] indsText = { 0, 1, 2,
                                    2, 3, 0
        };

        private float[] vertsAxelTextX = new float[20];
        private float[] vertsAxelTextY = new float[20];
        private float[] vertsAxelTextZ = new float[20];

        Vector3[] textPoints = new Vector3[3];

        private Texture textTexture;

        private int VAOText;

        private int VBOText;

        private int EBOText;

        private Shader shadText;

        private int VAOMap;

        private int VBOMap;

        private int EBOMap;

        private Shader shadMap;


        private int VAOSq;

        private int VBOSq;

        private int EBOSq;

        private Shader shadSq;

        private float zoomPercent = 1.0f;

        private Camera cam = new Camera(Vector3.UnitZ * 3, 2);

        private Vector2 size;

        private Shader shad;

        private int VAO;

        private int VBO;

        private int EBO;

        private float deltaTime;

        private Stopwatch sw = new Stopwatch();

        private (Vector2 start, Vector2 end) mouseDeltaRot;

        private (Vector2 start, Vector2 end) mouseDeltaPan;

        private Vector2 mouseMult = (1f, 1f);

        private float azimuth = -MathHelper.PiOver2;
        private float AzimuthAngle
        {
            get => MathHelper.RadiansToDegrees(azimuth);
            set
            {
                if (value > 360f) value -= 360f;
                if (value < -360f) value += 360f;
                azimuth = MathHelper.DegreesToRadians(value);
            }
        }

        private float polar;
        private float PolarAngle
        {
            get => MathHelper.RadiansToDegrees(polar);
            set
            {
                var angle = MathHelper.Clamp(value, -89f, 89f);
                polar = MathHelper.DegreesToRadians(angle);
            }
        }

        private int SliceDepth = 0;

        public int DataWidth = 1;
        public int DataHeight = 1;
        public int DataDepth = 1;
        #endregion

        /// <summary>
        /// Scene constructor. Makes a scene according to dimensions. Create initial geometry,
        /// create element buffer objects for rendered objects, set OpenGL settings and load shaders.
        /// </summary>
        /// <param name="width">Width of the vol data</param>
        /// <param name="height">Heigth of the vol data</param>
        /// <param name="depth">Depth of the vol data</param>
        /// <param name="winWidth">Width of the openTK control window</param>
        /// <param name="winHeight">Height of the openTK control window</param>
        public Scene(int width, int height, int depth, int winWidth, int winHeight) {

            float textW = 0.02f;
            float textH = 0.03f;
            float textSpace = 0.005f;

            Vector3 texXPoint = new Vector3(vertsMap[6] + textSpace + (textH / 2), vertsMap[7], vertsMap[8]);
            Vector3 texYPoint = new Vector3(vertsMap[18], vertsMap[19] + textSpace + (textH/2), vertsMap[20]);
            Vector3 texZPoint = new Vector3(vertsMap[30], vertsMap[31], vertsMap[32] + textSpace + (textH / 2));

            textPoints = new[] { texXPoint, texYPoint, texZPoint };

            float[][] textVerts = { vertsAxelTextX, vertsAxelTextY, vertsAxelTextZ };

            Vector2 xTexture = new Vector2(0, 0.125f);
            Vector2 yTexture = new Vector2(0.125f, 0.125f);
            Vector2 zTexture = new Vector2(0.25f, 0.125f);
            Vector2[] texs = new[] { xTexture, yTexture, zTexture};
            float textureW = 0.125f;
            float textureH = 0.125f;

            for (int i = 0; i < textVerts.Length; i++) {
                textVerts[i][0] = - textW / 2;
                textVerts[i][1] = - textH / 2;
                textVerts[i][2] = 0;

                textVerts[i][3] = texs[i].X;
                textVerts[i][4] = texs[i].Y - textureH;

                textVerts[i][5] =  - textW / 2;
                textVerts[i][6] = textH / 2;
                textVerts[i][7] = 0;

                textVerts[i][8] = texs[i].X;
                textVerts[i][9] = texs[i].Y;

                textVerts[i][10] = textW / 2;
                textVerts[i][11] = textH / 2;
                textVerts[i][12] = 0;

                textVerts[i][13] = texs[i].X + textureW;
                textVerts[i][14] = texs[i].Y;

                textVerts[i][15] = textW / 2;
                textVerts[i][16] = - textH / 2;
                textVerts[i][17] = 0;

                textVerts[i][18] = texs[i].X + textureW;
                textVerts[i][19] = texs[i].Y - textureH;
            }

            vertsAxelTextX = textVerts[0];
            vertsAxelTextY = textVerts[1];
            vertsAxelTextZ = textVerts[2];


            mouseMult.X = 2880f / ((float)winWidth);
            mouseMult.Y = 1620f / ((float)winHeight);

            DataDepth = depth;
            DataHeight = height;
            DataWidth = width;

            verts = new float[]{
                          width * -0.5f, height * 0.5f, depth * -0.5f,
                          width * 0.5f,  height * 0.5f, depth * -0.5f,
                          width * 0.5f, height * -0.5f, depth * -0.5f,
                          width * -0.5f, height * -0.5f, depth * -0.5f,
                          width * -0.5f, height *  0.5f, depth *  0.5f,
                          width * 0.5f, height *  0.5f, depth *  0.5f,
                          width * 0.5f, height * -0.5f, depth *  0.5f,
                          width * -0.5f, height * -0.5f, depth *  0.5f
            };

            float maxDist = MathF.Pow((depth * depth + width * width + height * height) * 0.25f, 0.5f);

            vertsSq = new float[]{
                          -maxDist, maxDist, SliceDepth,
                          maxDist, maxDist, SliceDepth,
                          maxDist,  -maxDist, SliceDepth,
                          -maxDist, -maxDist, SliceDepth
            };

            float distance = maxDist * 1.5f;

            float a = (PolarAngle * (MathF.PI)) / 180;
            float b = (AzimuthAngle * (MathF.PI)) / 180;

            float XRatio = MathF.Cos(a) * MathF.Cos(b);
            float YRatio = MathF.Sin(a);
            float ZRatio = MathF.Cos(a) * MathF.Sin(b);

            Vector3 ratios = new Vector3(XRatio, YRatio, ZRatio);
            cam.Position = (distance * ratios);

            float ClipPlane = distance * 3;
            cam.SetClipPlane(ClipPlane);

            GL.Enable(EnableCap.DepthTest);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            VAO = GL.GenVertexArray();
            VBO = GL.GenBuffer();
            EBO = GL.GenBuffer();

            VAOSq = GL.GenVertexArray();
            VBOSq = GL.GenBuffer();
            EBOSq = GL.GenBuffer();

            VAOMap = GL.GenVertexArray();
            VBOMap = GL.GenBuffer();
            EBOMap = GL.GenBuffer();

            VAOText = GL.GenVertexArray();
            VBOText = GL.GenBuffer();
            EBOText = GL.GenBuffer();


            GL.BindVertexArray(VAO);

            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * verts.Length, verts, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, inds.Length * sizeof(uint), inds, BufferUsageHint.StaticDraw);



            GL.BindVertexArray(VAOSq);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOSq);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * vertsSq.Length, vertsSq, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBOSq);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indsSq.Length * sizeof(uint), indsSq, BufferUsageHint.StaticDraw);


            GL.BindVertexArray(VAOMap);

            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOMap);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * vertsMap.Length, vertsMap, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBOMap);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indsMap.Length * sizeof(uint), indsMap, BufferUsageHint.StaticDraw);


            GL.BindVertexArray(VAOText);

            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOText);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * vertsAxelTextX.Length, vertsAxelTextX, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBOText);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indsText.Length * sizeof(uint), indsText, BufferUsageHint.StaticDraw);


            shad = new Shader("Shaders/shader.vert", "Shaders/shader.frag");
            shad.Use();

            shadSq = new Shader("Shaders/shaderSlice.vert", "Shaders/shaderSlice.frag");
            shadSq.Use();

            shadMap = new Shader("Shaders/shaderMap.vert", "Shaders/shaderMap.frag");
            shadMap.Use();

            shadText = new Shader("Shaders/shaderText.vert", "Shaders/shaderText.frag");
            shadText.Use();

            textTexture = Texture.LoadFromFile("Textures/Font.bmp");
            textTexture.Use(TextureUnit.Texture0);
        }

        /// <summary>
        /// Render the scene
        /// </summary>
        /// <param name="width">Vol data width</param>
        /// <param name="height">Vol data heigth</param>
        /// <param name="depth">Vol data depth</param>
        public void Render(int width, int height, int depth) //float alpha = 1.0f
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.BindVertexArray(VAO);

            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * verts.Length, verts, BufferUsageHint.StaticDraw);

            shad.Use();

            var model = Matrix4.Identity;
            shad.SetMatrix4("model", model);
            shad.SetMatrix4("view", cam.GetViewMatrix());
            shad.SetMatrix4("projection", cam.GetProjectionMatrix());

            GL.DrawElements(PrimitiveType.Lines, inds.Length, DrawElementsType.UnsignedInt, 0);




            GL.BindVertexArray(VAOSq);

            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOSq);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * vertsSq.Length, vertsSq, BufferUsageHint.StaticDraw);

            shadSq.Use();

            var modelSq = Matrix4.Identity *
                        Matrix4.CreateRotationX((float)MathHelper.DegreesToRadians(PolarAngle)) *
                        Matrix4.CreateRotationY((float)MathHelper.DegreesToRadians(-(AzimuthAngle + 90f)));
            shadSq.SetMatrix4("model", modelSq);
            shadSq.SetMatrix4("view", cam.GetViewMatrix());
            shadSq.SetMatrix4("projection", cam.GetProjectionMatrix());
            Vector3 dimensions = new Vector3(width, height, depth) * 0.5f;
            shadSq.SetVector3("CubeDims", dimensions);

            GL.DrawElements(PrimitiveType.Triangles, indsSq.Length, DrawElementsType.UnsignedInt, 0);




            GL.Disable(EnableCap.DepthTest);

            GL.BindVertexArray(VAOMap);

            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOMap);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * vertsMap.Length, vertsMap, BufferUsageHint.StaticDraw);

            shadMap.Use();

            float polarDir = -1f;
            var modelMap = Matrix4.Identity *
                        Matrix4.CreateRotationY((float)MathHelper.DegreesToRadians(-(AzimuthAngle + 90f))) *
                        Matrix4.CreateRotationX((float)MathHelper.DegreesToRadians(polarDir * PolarAngle))
                        ;
            Matrix4 scaleMatr = Matrix4.CreateScale(1000/size.X, 1000/size.Y, 1);
            modelMap = modelMap * scaleMatr;

            Vector3 offset = new Vector3(0.75f, -0.75f, 0);
            shadMap.SetMatrix4("model", modelMap);
            shadMap.SetVector3("offset", offset);
            GL.DrawElements(PrimitiveType.Lines, indsMap.Length, DrawElementsType.UnsignedInt, 0);



            GL.BindVertexArray(VAOText);

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
            GL.Enable(EnableCap.Blend);



            Matrix4 textRot = Matrix4.Identity * 
                              Matrix4.CreateRotationX((float)MathHelper.DegreesToRadians(PolarAngle)) *
                              Matrix4.CreateRotationY((float)MathHelper.DegreesToRadians((AzimuthAngle + 90f)));

            RenderText(vertsAxelTextX, textRot, modelMap, textPoints[0], offset);
            RenderText(vertsAxelTextY, textRot, modelMap, textPoints[1], offset);
            RenderText(vertsAxelTextZ, textRot, modelMap, textPoints[2], offset);

            GL.Disable(EnableCap.Blend);


            GL.Finish();
            GL.Enable(EnableCap.DepthTest);
            sw.Stop();
            deltaTime = sw.ElapsedMilliseconds / 1000f;
            sw.Restart();
        }

        /// <summary>
        /// Render one quad with text texture
        /// </summary>
        /// <param name="textVerts">Quad vertices</param>
        /// <param name="textRot">Rotation matrix for the text</param>
        /// <param name="objRot">Rotation matrix for text and offset</param>
        /// <param name="textOffset">Offset of the text from center</param>
        /// <param name="objOffset">Offset of the whole object</param>
        private void RenderText(float[] textVerts, Matrix4 textRot, Matrix4 objRot, Vector3 textOffset, Vector3 objOffset) {

            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOText);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * textVerts.Length, textVerts, BufferUsageHint.StaticDraw);

            textTexture.Use(TextureUnit.Texture0);

            shadText.Use();

            var modelText = textRot * Matrix4.CreateTranslation(textOffset) * objRot;

            shadText.SetMatrix4("model", modelText);
            shadText.SetVector3("offset", objOffset);

            GL.DrawElements(PrimitiveType.Triangles, indsText.Length, DrawElementsType.UnsignedInt, 0);
        }

        /// <summary>
        /// Widen or narrow the FOV to change zoom
        /// </summary>
        /// <param name="zoom">Added value to the current zoom percent value. </param>
        public void Zoom(float zoom) {
            zoomPercent = MathHelper.Clamp(zoomPercent + zoom, 0.01f, 1.5f);
            cam.Fov = 60f*zoomPercent;
        }

        /// <summary>
        /// Changes slice depth according to the amount variable. Zero is center.
        /// </summary>
        /// <param name="amount">The new depth of the slice</param>
        /// <param name="width">Width of the data.</param>
        /// <param name="height">Heigth of the data</param>
        /// <param name="depth">Depth of the data</param>
        public void MoveSlice(int amount, int width, int height, int depth) {
            SliceDepth = -amount;

            float maxDist = MathF.Pow((depth * depth + width * width + height * height) * 0.25f, 0.5f);

            vertsSq = new float[]{
                          -maxDist, maxDist, SliceDepth,
                          maxDist, maxDist, SliceDepth,
                          maxDist,  -maxDist, SliceDepth,
                          -maxDist, -maxDist, SliceDepth
            };
        }

        /// <summary>
        /// Starts tracking mouse movements for rotating the arcball camera
        /// </summary>
        /// <param name="start">Starting mousePoint</param>
        public void MouseDownRot(Vector2 start) {
            mouseDeltaRot.start = start;
        }

        /// <summary>
        /// Changes the position of the camera according to the mouse movement changes. 
        /// </summary>
        /// <param name="end">Mouse end point</param>
        public void MouseMoveRot(Vector2 end) {
            mouseDeltaRot.end = end;


            float deltaX = (float)(mouseDeltaRot.end.X - mouseDeltaRot.start.X) * deltaTime * mouseMult.X;
            float deltaY = (float)(mouseDeltaRot.end.Y - mouseDeltaRot.start.Y) * deltaTime * mouseMult.Y;

            mouseDeltaRot.start = mouseDeltaRot.end;

            AzimuthAngle += deltaX;
            PolarAngle += deltaY;

            float maxDist = MathF.Pow((DataDepth * DataDepth + DataWidth * DataWidth + DataHeight * DataHeight) * 0.25f, 0.5f);

            float distance = maxDist * 1.5f;

            float a = (PolarAngle * (MathF.PI)) / 180;
            float b = (AzimuthAngle * (MathF.PI)) / 180;

            float XRatio = MathF.Cos(a) * MathF.Cos(b);
            float YRatio = MathF.Sin(a);
            float ZRatio = MathF.Cos(a) * MathF.Sin(b);

            Vector3 ratios = new Vector3(XRatio, YRatio, ZRatio);
            cam.Position = (distance * ratios);
        }

        /// <summary>
        /// Starts tracking mouse movements for panning the camera view
        /// </summary>
        /// <param name="start">Starting mousePoint</param>
        public void MouseDownPan(Vector2 start)
        {
            mouseDeltaPan.start = start;
        }

        /// <summary>
        /// Changes the offset (and at the same time the position) of the camera according to mouse movement changes
        /// </summary>
        /// <param name="end">Mouse end point</param>
        public void MouseMovePan(Vector2 end)
        {
            mouseDeltaPan.end = end;

            float deltaX = -(float)(mouseDeltaPan.end.X - mouseDeltaPan.start.X) * deltaTime * mouseMult.X;
            float deltaY = (float)(mouseDeltaPan.end.Y - mouseDeltaPan.start.Y) * deltaTime * mouseMult.Y;

            mouseDeltaPan.start = mouseDeltaPan.end;

            Vector3 sizeMult = new Vector3(DataWidth/100, DataHeight/100, DataDepth/100);
            Vector3 temp = cam.offset;
            cam.offset += (cam.Up * deltaY) * sizeMult;
            cam.offset += (cam.Right * deltaX) * sizeMult;
            if (MathF.Abs(cam.offset.X) > DataWidth / 2f ||
               MathF.Abs(cam.offset.Y) > DataHeight / 2f ||
               MathF.Abs(cam.offset.Z) > DataDepth / 2f) cam.offset = temp;
        }

        /// <summary>
        /// Gets the maximum values for visible slice for the vol data from the current viewpoint.
        /// </summary>
        /// <returns>The amount slice can move from the center position to positive or negative direction. </returns>
        public int GetDiagonalLength() {

            float w = DataWidth / 2;
            float h = DataHeight / 2;
            float d = DataDepth / 2;

            float a = (PolarAngle * (MathF.PI)) / 180;
            float b = (AzimuthAngle * (MathF.PI)) / 180;

            float XRatio = MathF.Cos(a) * MathF.Cos(b);
            float YRatio = MathF.Sin(a);
            float ZRatio = MathF.Cos(a) * MathF.Sin(b);
            Vector3 vec = new Vector3(XRatio, YRatio, ZRatio);

            /*piste = n * vec;
            kohtisuora = piste - (w, h, d);
            kohtisuora = (n * vec - (w, h, d));
            0 = kohtisuora.X * vec.X + kohtisuora.Y * vec.Y + kohtisuora.Z * vec.Z;
            0 = (n * vec.X - (w)) * vec.X + (n * vec.Y - (h)) * vec.Y + (n * vec.Z - (d)) * vec.Z;
            0 = n * vec.X * vec.X - w * vec.X + n * vec.Y * vec.Y - h * vec.Y + n * vec.Z * vec.Z - d * vec.Z;

            w * vec.X + h * vec.Y + d * vec.Z = n * (vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z);
            Itseisarvojen avulla saadaan ratkaistua symmetrisyyksien takia kaikki mahdolliset kulmat, eikä 
            vain oikean etu ylä kahdenneksen tapaukset*/

            float n = (w * MathF.Abs(vec.X) + h * MathF.Abs(vec.Y) + d * MathF.Abs(vec.Z)) / (vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z);
            int distance = (int) (n * vec).Length;

            return distance;

            //Lazy way
            //float maxDist =  MathF.Pow((MathF.Pow(w, 2f) + MathF.Pow(d, 2f)) + MathF.Pow(h, 2f), 0.5f);
            //return (int) maxDist + 1;
        }

        /// <summary>
        /// Resets cam offset
        /// </summary>
        public void ResetOffset() {
            cam.offset = new Vector3(0f,0f,0f);
        }

        /// <summary>
        /// Resizes the viewport and changes cam aspects accordingly
        /// </summary>
        /// <param name="width">Width of viewport</param>
        /// <param name="height">Heigth of viewport</param>
        public void Resize(int width, int height) {
            mouseMult.X = 1.5f * 2880f / ((float)width);
            mouseMult.Y = 1.5f * 2880f / ((float)height);
            GL.Viewport(0, 0, width, height);
            cam.AspectRatio = (float) width / (float) height;
            size = new Vector2(width, height);
        } 
    }
}
