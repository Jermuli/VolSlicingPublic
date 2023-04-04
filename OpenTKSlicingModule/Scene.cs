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
                          -0.5f,  0.5f, -0.5f,      1.0f, 0.0f, 0.0f, //X R
                          0.5f,  0.5f, -0.5f,       1.0f, 0.0f, 0.0f,

                          0.5f,  0.5f, -0.5f,      0.0f, 0.0f, 1.0f, //Y B
                          0.5f,  -0.5f, -0.5f,     0.0f, 0.0f, 1.0f,

                          0.5f,  0.5f, -0.5f,      0.0f, 1.0f, 0.0f, //Z G
                          0.5f,  0.5f, 0.5f,       0.0f, 1.0f, 0.0f,

                          -0.5f,  -0.5f, -0.5f,     1.0f, 1.0f, 1.0f,
                          0.5f, -0.5f, -0.5f,       1.0f, 1.0f, 1.0f,

                          -0.5f, -0.5f, -0.5f,       1.0f, 1.0f, 1.0f,
                          -0.5f, 0.5f, -0.5f,        1.0f, 1.0f, 1.0f,


                          -0.5f, 0.5f, 0.5f,        1.0f, 1.0f, 1.0f, //Back square
                          0.5f, 0.5f, 0.5f,         1.0f, 1.0f, 1.0f,

                          -0.5f, 0.5f, 0.5f,        1.0f, 1.0f, 1.0f,
                          -0.5f, -0.5f, 0.5f,       1.0f, 1.0f, 1.0f,

                          -0.5f, -0.5f, 0.5f,       1.0f, 1.0f, 1.0f,
                          0.5f, -0.5f, 0.5f,        1.0f, 1.0f, 1.0f,

                          0.5f, -0.5f, 0.5f,        1.0f, 1.0f, 1.0f,
                          0.5f, 0.5f, 0.5f,         1.0f, 1.0f, 1.0f,


                          -0.5f, -0.5f, -0.5f,      1.0f, 1.0f, 1.0f, //Square connectors Z defined earlier
                          -0.5f, -0.5f, 0.5f,       1.0f, 1.0f, 1.0f,

                          0.5f, -0.5f, -0.5f,       1.0f, 1.0f, 1.0f,
                          0.5f, -0.5f, 0.5f,        1.0f, 1.0f, 1.0f,

                          -0.5f, 0.5f, -0.5f,       1.0f, 1.0f, 1.0f,
                          -0.5f, 0.5f, 0.5f,        1.0f, 1.0f, 1.0f
        };       

        private uint[] inds = { 0, 1,
                                2, 3,
                                4, 5,
                                6, 7,
                                8, 9,
                                10, 11,
                                12, 13,
                                14, 15,
                                16, 17,
                                18, 19,
                                20, 21,
                                22, 23
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

        private uint[] indsText = { 0, 1, 2,
                                    2, 3, 0
        };

        private float[] vertsTextX = new float[20];
        private float[] vertsTextY = new float[20];
        private float[] vertsTextZ = new float[20];
        private float[] vertsText0 = new float[20];

        private int VAOAxelText;

        private int VBOAxelText;

        private int EBOAxelText;

        private Shader shadAxelText;

        private Texture textTexture;

        private int VAOSq;

        private int VBOSq;

        private int EBOSq;

        private Shader shadSq;

        private int zoomPercent = 100;

        private Camera cam = new Camera(Vector3.UnitZ * 3, 2, true);

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

        private Quaternion Rots = new Quaternion(0, 0, 0);

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
        public Scene(int width, int height, int depth, int winWidth, int winHeight, bool isOrtho) {

            cam.IsOrthographic = isOrtho;

            float textBaseSize = MathF.Pow(width * width + height * height, 0.5f) * 0.05f;

            Vector2 textX = new Vector2(0f, 0.125f);
            Vector2 textY = new Vector2(0.125f, 0.125f);
            Vector2 textZ = new Vector2(0.25f, 0.125f);
            Vector2 text0 = new Vector2(0f, 0.75f);
            Vector2[] textures = new[] { textX, textY, textZ, text0 };
            float textureW = 0.125f;
            float textureH = 0.125f;
            float textW = 2f;
            float textH = 3f;

            float[][] textTextureVerts = { vertsTextX, vertsTextY, vertsTextZ, vertsText0 };


            for (int i = 0; i < textTextureVerts.Length; i++)
            {
                textTextureVerts[i][0] = -textW * textBaseSize / 2;
                textTextureVerts[i][1] = -textH * textBaseSize / 2;
                textTextureVerts[i][2] = 0;

                textTextureVerts[i][3] = textures[i].X + textureW;
                textTextureVerts[i][4] = textures[i].Y - textureH;

                textTextureVerts[i][5] = -textW * textBaseSize / 2;
                textTextureVerts[i][6] = textH * textBaseSize / 2;
                textTextureVerts[i][7] = 0;

                textTextureVerts[i][8] = textures[i].X + textureW;
                textTextureVerts[i][9] = textures[i].Y;

                textTextureVerts[i][10] = textW * textBaseSize / 2;
                textTextureVerts[i][11] = textH * textBaseSize / 2;
                textTextureVerts[i][12] = 0;

                textTextureVerts[i][13] = textures[i].X ;
                textTextureVerts[i][14] = textures[i].Y;

                textTextureVerts[i][15] = textW * textBaseSize / 2;
                textTextureVerts[i][16] = -textH * textBaseSize / 2;
                textTextureVerts[i][17] = 0;

                textTextureVerts[i][18] = textures[i].X ;
                textTextureVerts[i][19] = textures[i].Y - textureH;
            }

            mouseMult.X = 2880f / ((float)winWidth);
            mouseMult.Y = 1620f / ((float)winHeight);
            size = new Vector2(winWidth, winHeight);
            cam.ViewSize = size;
            cam.Zoom = (float) zoomPercent / 100f;

            DataDepth = depth;
            DataHeight = height;
            DataWidth = width;

            for(int i = 0; i < verts.Length; i += 6)
            {
                verts[i] = verts[i] * width;
                verts[i + 1] = verts[i + 1] * height;
                verts[i + 2] = verts[i + 2] * depth;
            }

            float maxDist = MathF.Pow((depth * depth + width * width + height * height) * 0.25f, 0.5f);

            vertsSq = new float[]{
                          -maxDist, maxDist, SliceDepth,
                          maxDist, maxDist, SliceDepth,
                          maxDist,  -maxDist, SliceDepth,
                          -maxDist, -maxDist, SliceDepth
            };

            float distance = maxDist * 5f;

            float a = (0 * (MathF.PI)) / 180;
            float b = (-90f * (MathF.PI)) / 180;

            float XRatio = MathF.Cos(a) * MathF.Cos(b);
            float YRatio = MathF.Sin(a);
            float ZRatio = MathF.Cos(a) * MathF.Sin(b);

            Vector3 ratios = new Vector3(XRatio, YRatio, ZRatio);
            cam.Position = (distance * ratios);

            float ClipPlane = distance * 11;
            cam.SetClipPlane(ClipPlane);

            GL.Enable(EnableCap.DepthTest);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            VAO = GL.GenVertexArray();
            VBO = GL.GenBuffer();
            EBO = GL.GenBuffer();

            VAOSq = GL.GenVertexArray();
            VBOSq = GL.GenBuffer();
            EBOSq = GL.GenBuffer();

            VAOAxelText = GL.GenVertexArray();
            VBOAxelText = GL.GenBuffer();
            EBOAxelText = GL.GenBuffer();


            GL.BindVertexArray(VAO);

            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * verts.Length, verts, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, inds.Length * sizeof(uint), inds, BufferUsageHint.StaticDraw);


            GL.BindVertexArray(VAOSq);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOSq);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * vertsSq.Length, vertsSq, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBOSq);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indsSq.Length * sizeof(uint), indsSq, BufferUsageHint.StaticDraw);


            GL.BindVertexArray(VAOAxelText);

            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOAxelText);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * vertsTextX.Length, vertsTextX, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBOAxelText);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indsText.Length * sizeof(uint), indsText, BufferUsageHint.StaticDraw);


            shad = new Shader("Shaders/shader.vert", "Shaders/shader.frag");
            shad.Use();

            shadSq = new Shader("Shaders/shaderSlice.vert", "Shaders/shaderSlice.frag");
            shadSq.Use();

            shadAxelText = new Shader("Shaders/shaderAxelText.vert", "Shaders/shaderAxelText.frag");
            shadAxelText.Use();


            textTexture = Texture.LoadFromFile("Textures/Font.bmp");
            textTexture.Use(TextureUnit.Texture0);
        }

        /// <summary>
        /// Render the scene
        /// </summary>
        /// <param name="width">Vol data width</param>
        /// <param name="height">Vol data heigth</param>
        /// <param name="depth">Vol data depth</param>
        public void Render(int width, int height, int depth)
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
            var modelSq = Matrix4.CreateFromQuaternion(Rots);

            shadSq.SetMatrix4("model", modelSq);
            shadSq.SetMatrix4("view", cam.GetViewMatrix());
            shadSq.SetMatrix4("projection", cam.GetProjectionMatrix());
            Vector3 dimensions = new Vector3(width, height, depth) * 0.5f;
            shadSq.SetVector3("CubeDims", dimensions);

            GL.DrawElements(PrimitiveType.Triangles, indsSq.Length, DrawElementsType.UnsignedInt, 0);



            GL.BindVertexArray(VAOAxelText);

            Matrix4 textRot = modelSq;

            float textPadding = MathF.Pow(vertsTextX[10] * vertsTextX[10] + vertsTextX[11] * vertsTextX[11], 0.5f);

            Vector3 offsetX = new Vector3(-(width / 2 + textPadding), height / 2 + textPadding, -(depth / 2 + textPadding));
            Vector3 offsetY = new Vector3(width / 2 + textPadding, -(height / 2 + textPadding), -(depth / 2 + textPadding));
            Vector3 offsetZ = new Vector3(width / 2 + textPadding, height / 2 + textPadding, depth / 2 + textPadding);
            Vector3 offset0 = new Vector3(width / 2 + textPadding, height / 2 + textPadding, -(depth / 2 + textPadding));

            RenderAxelText(vertsTextX, textRot, offsetX, cam.GetViewMatrix(), cam.GetProjectionMatrix());
            RenderAxelText(vertsTextY, textRot, offsetY, cam.GetViewMatrix(), cam.GetProjectionMatrix());
            RenderAxelText(vertsTextZ, textRot, offsetZ, cam.GetViewMatrix(), cam.GetProjectionMatrix());
            RenderAxelText(vertsText0, textRot, offset0, cam.GetViewMatrix(), cam.GetProjectionMatrix());

            GL.Finish();
            GL.Enable(EnableCap.DepthTest);
            sw.Stop();
            deltaTime = sw.ElapsedMilliseconds / 1000f;
            sw.Restart();
        }

        /// <summary>
        /// Renders text to the virtual space
        /// </summary>
        /// <param name="textVerts"> Vertices for the text and texture locations</param>
        /// <param name="textRot"> Rotation of the text </param>
        /// <param name="textOffset"> Offset of the text from point (0,0,0)</param>
        /// <param name="view"> View matrix </param>
        /// <param name="proj"> Proj matrix </param>
        private void RenderAxelText(float[] textVerts, Matrix4 textRot, Vector3 textOffset, Matrix4 view, Matrix4 proj) {
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOAxelText);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * textVerts.Length, textVerts, BufferUsageHint.StaticDraw);

            textTexture.Use(TextureUnit.Texture0);

            shadAxelText.Use();

            var modelText = textRot * Matrix4.CreateTranslation(textOffset);

            shadAxelText.SetMatrix4("model", modelText);
            shadAxelText.SetMatrix4("view", view);
            shadAxelText.SetMatrix4("projection", proj);


            GL.DrawElements(PrimitiveType.Triangles, indsText.Length, DrawElementsType.UnsignedInt, 0);
        }

        /// <summary>
        /// Widen or narrow the FOV to change zoom
        /// </summary>
        /// <param name="zoom"> Zoom amount. The zooming is done on dynamic scale, so the value should be +1 and -1 on default. Meaning a one increment
        /// or decrement on a dynamic scale. </param>
        public void Zoom(int zoom) {

            int multiplier = 100;
            if (zoomPercent + zoom > 10000) multiplier = 10000;
            else if (zoomPercent + zoom >= 1000 && zoomPercent + zoom <= 10000) multiplier = 1000;
            else if (zoomPercent + zoom <= 100) multiplier = 10;

            zoomPercent = MathHelper.Clamp(zoom * multiplier + zoomPercent, 10, 50000);

            cam.Fov = 60f * 100 / (float)zoomPercent;
            cam.Zoom = 100 / (float) zoomPercent;
        }

        public int GetZoomLevel() {
            return zoomPercent;
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

            float a = (-deltaX * (MathF.PI)) / 180; 
            float b = (-deltaY * (MathF.PI)) / 180;

            Quaternion quart = new Quaternion(cam.Right.X * b + a * cam.Up.X, cam.Right.Y * b + a * cam.Up.Y, cam.Right.Z * b + a * cam.Up.Z);
            Vector3 transformVect = Vector3.Transform(cam.Position, quart);
            Rots = quart * Rots;
            cam.Position = transformVect;
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

            float maxDist = MathF.Pow((DataDepth * DataDepth + DataWidth * DataWidth + DataHeight * DataHeight) * 0.25f, 0.5f) * 3;

            float zoomMultiplier = MathF.Sqrt(100f / (float) zoomPercent);

            Vector3 sizeMult = new Vector3(DataWidth/100, DataHeight/100, DataDepth/100);
            Vector3 temp = cam.offset;
            cam.offset += (cam.Up * deltaY) * sizeMult * zoomMultiplier;
            cam.offset += (cam.Right * deltaX) * sizeMult * zoomMultiplier;
            if (MathF.Abs(cam.offset.X) > maxDist ||
               MathF.Abs(cam.offset.Y) > maxDist ||
               MathF.Abs(cam.offset.Z) > maxDist) cam.offset = temp;
        }

        /// <summary>
        /// Gets the maximum values for visible slice for the vol data from the current viewpoint.
        /// </summary>
        /// <returns>The amount slice can move from the center position to positive or negative direction. </returns>
        public int GetDiagonalLength() {

            float w = DataWidth / 2;
            float h = DataHeight / 2;
            float d = DataDepth / 2;

            Vector3 vec = cam.Position.Normalized();

            /*piste = n * vec;
            kohtisuora = piste - (w, h, d);
            kohtisuora = (n * vec - (w, h, d));
            0 = kohtisuora.X * vec.X + kohtisuora.Y * vec.Y + kohtisuora.Z * vec.Z;
            0 = (n * vec.X - (w)) * vec.X + (n * vec.Y - (h)) * vec.Y + (n * vec.Z - (d)) * vec.Z;
            0 = n * vec.X * vec.X - w * vec.X + n * vec.Y * vec.Y - h * vec.Y + n * vec.Z * vec.Z - d * vec.Z;

            w * vec.X + h * vec.Y + d * vec.Z = n * (vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z);
            Itseisarvojen avulla saadaan ratkaistua symmetrisyyksien takia kaikki mahdolliset kulmat, eikä 
            vain oikean etu ylä kahdenneksen tapaukset*/

            float n = (w * MathF.Abs(vec.X) + h * MathF.Abs(vec.Y) + d * MathF.Abs(vec.Z)) / 
                      (vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z);
            int distance = (int)n;

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
        /// Sets if the projection should be orthogonal. If not we use perspective
        /// </summary>
        /// <param name="orth"></param>
        public void SetOrtho(bool orth) {
            cam.IsOrthographic = orth;
        }

        /// <summary>
        /// Resizes the viewport and changes cam aspects accordingly
        /// </summary>
        /// <param name="width">Width of viewport</param>
        /// <param name="height">Heigth of viewport</param>
        public void Resize(int width, int height) {
            mouseMult.X = 6284f / ((float)width); // Pi * 2 * 1000 aprox 6284f
            mouseMult.Y = 6284f / ((float)height);
            GL.Viewport(0, 0, width, height);
            cam.AspectRatio = (float) width / (float) height;
            size = new Vector2(width, height);
            cam.ViewSize = size;
        } 
    }
}
