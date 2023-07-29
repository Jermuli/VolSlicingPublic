using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;
using StbImageSharp;
using ImageMagick;
using System.IO;
using System.Threading;

namespace OpenTKSlicingModule
{
    public class SliceImage
    {
        public enum FileType
        {
            ImageSequence,
            Tiff3D,
            Raw
        }

        public enum InterpolationMethod
        {
            NearestNeighbor,
            Trilinear,
            Tricubic
        }

        public byte[][] SliceImgA;
        public byte[][] SliceImgB;
        public bool ASlice = true;
        public int ChunkSize;
        private readonly int ChunksPerDim = 16;
        private float ChunkDist;
        public bool[] ChunkVisibility;
        private string DataPath;
        private int DataWidth;
        private int DataHeight;
        private int DataDepth;
        private Quaternion Rotations;
        private int SliceDepth;
        public int SliceDim;
        private int ByteSize = 1;
        private bool BigEndian = true;
        private int MaxDist;
        private int ChunkTextureSizeA;
        private int ChunkTextureSizeB;
        private FileType DataType = FileType.Raw;
        private string ImageSeqFileTemplate;
        private int LastSeqImgNumber = -1;
        //private int LastSeqImgNumberTrilinear = -1;
        //private int LastSeqImgNumberTricubic = -1;
        private byte[] LastSeqImg = Array.Empty<byte>();
        //private byte[][] LastSeqImgTrilinear = new byte[2][];
        //private byte[][] LastSeqImgTricubic = new byte[4][];
        private InterpolationMethod Method = InterpolationMethod.NearestNeighbor;

        private byte[][] LowLoDVolData = Array.Empty<byte[]>();
        private long LowLoDVolDataMBSize = 1;
        private float LowLoDVolDataScale;
        private int LowLoDVolDataWidth;
        private int LowLoDVolDataHeight;
        private int LowLoDVolDataDepth;
        private float LowLoDUnitSize;
        private int LowLoDChunkSize;
        private int LowLoDMaxDist;

        private float InterpolationScale = 1f;

        private CancellationTokenSource CancelThread = new CancellationTokenSource();
        private readonly object SliceLock = new object();
        public string status = "";

        /// <summary>
        /// Constructor for slice image
        /// </summary>
        /// <param name="datapath">Filepath to the volumetric data</param>
        /// <param name="dataWidth">Width of the data</param>
        /// <param name="dataHeight">Height of the data</param>
        /// <param name="dataDepth">Depth of the data</param>
        /// <param name="rotations">Arcball camera rotations</param>
        /// <param name="sliceDepth">Depth which the slice is taken. 0 is the middle point according to current rotations</param>
        /// <param name="raw">Is file raw</param>
        /// <param name="byteSize">Byte size of the raw data</param>
        public SliceImage(string datapath, int dataWidth, int dataHeight, int dataDepth, Quaternion rotations, int sliceDepth, 
                          bool endian, int byteSize, FileType fType, InterpolationMethod iMethod, string fileTemplate, 
                          float iScale, long lowLoDMaxSize)
        {
            DataPath = datapath;
            ImageSeqFileTemplate = fileTemplate;
            DataWidth = dataWidth;
            DataHeight = dataHeight;
            DataDepth = dataDepth;
            Rotations = rotations;
            SliceDepth = sliceDepth;
            ByteSize = byteSize;
            BigEndian = endian;
            DataType = fType;
            Method = iMethod;
            InterpolationScale = iScale;
            LowLoDVolDataScale = lowLoDMaxSize;
            MaxDist = (int)MathHelper.NextPowerOfTwo(MathF.Sqrt(DataDepth * DataDepth + DataWidth * DataWidth + DataHeight * DataHeight));
            ChunkSize = MathHelper.Max(1, MaxDist / ChunksPerDim);
            ChunkDist = MathF.Sqrt(ChunkSize * ChunkSize * 2f) / 2f;
            ChunkVisibility = new bool[ChunksPerDim * ChunksPerDim];
            SliceImgA = new byte[ChunksPerDim * ChunksPerDim][];
            SliceImgB = new byte[ChunksPerDim * ChunksPerDim][];

            MakeLowLodModel();

        }

        /// <summary>
        /// Makes Low LoD model from the volume data
        /// </summary>
        private void MakeLowLodModel() {
            //CancelThread.Cancel();
            double scale = Math.Pow((double)((double)LowLoDVolDataMBSize * 1024 * 1024) / ((double)DataWidth * (double)DataHeight * (double)DataDepth * (double)ByteSize), 1.0 / 3.0);
            LowLoDVolDataScale = MathF.Min(1, Convert.ToSingle(scale));
            LowLoDVolDataWidth = (int)(DataWidth * LowLoDVolDataScale);
            LowLoDVolDataHeight = (int)(DataHeight * LowLoDVolDataScale);
            LowLoDVolDataDepth = (int)(DataDepth * LowLoDVolDataScale);
            LowLoDUnitSize = 1.0f / LowLoDVolDataScale;
            LowLoDVolData = new byte[LowLoDVolDataDepth][];

            LowLoDMaxDist = (int)MathHelper.NextPowerOfTwo(Math.Sqrt(LowLoDVolDataDepth * LowLoDVolDataDepth + LowLoDVolDataWidth * LowLoDVolDataWidth + LowLoDVolDataHeight * LowLoDVolDataHeight));
            LowLoDChunkSize = MathHelper.Max(1, LowLoDMaxDist / ChunksPerDim);

            if (DataPath != " ")
            {
                float w2 = DataWidth / 2;
                float h2 = DataHeight / 2;
                float d2 = DataDepth / 2;
                switch (DataType) {
                    case FileType.Raw:
                        using (FileStream fs = File.OpenRead(DataPath))
                        {
                            for (int i = 0; i < LowLoDVolData.Length; i++)
                            {
                                byte[] bytes = new byte[LowLoDVolDataHeight * LowLoDVolDataWidth * ByteSize];
                                byte[] pixelBytes = new byte[ByteSize];
                                for (int j = 0; j < LowLoDVolDataHeight; j++)
                                {
                                    for (int k = 0; k < LowLoDVolDataWidth; k++)
                                    {
                                        Vector3 LowLoDValuePosition = new Vector3(k * LowLoDUnitSize, j * LowLoDUnitSize, i * LowLoDUnitSize);

                                        Interpolate(ref pixelBytes, LowLoDValuePosition.X, LowLoDValuePosition.Y, LowLoDValuePosition.Z, fs);
                                        for (int l = 0; l < pixelBytes.Length; l++)
                                        {
                                            bytes[((j * LowLoDVolDataWidth + k) * ByteSize) + l] = pixelBytes[l];
                                        }
                                    }
                                }
                                LowLoDVolData[i] = bytes;
                            }
                        }
                        break;
                    case FileType.ImageSequence:
                        for (int i = 0; i < LowLoDVolData.Length; i++)
                        {
                            byte[] bytes = new byte[LowLoDVolDataHeight * LowLoDVolDataWidth * ByteSize];
                            byte[] pixelBytes = new byte[ByteSize];
                            for (int j = 0; j < LowLoDVolDataHeight; j++)
                            {
                                for (int k = 0; k < LowLoDVolDataWidth; k++)
                                {
                                    Vector3 LowLoDValuePosition = new Vector3(k * LowLoDUnitSize, j * LowLoDUnitSize, i * LowLoDUnitSize);

                                    Interpolate(ref pixelBytes, LowLoDValuePosition.X, LowLoDValuePosition.Y, LowLoDValuePosition.Z);
                                    for (int l = 0; l < pixelBytes.Length; l++)
                                    {
                                        bytes[((j * LowLoDVolDataWidth + k) * ByteSize) + l] = pixelBytes[l];
                                    }
                                }
                            }
                            LowLoDVolData[i] = bytes;
                        }
                        break;
                }
                
            }
        }

        /// <summary>
        /// Updates the slice first from the Low LoD version on the RAM and calls background task to update more accurate version 
        /// of the slice.
        /// </summary>
        private void UpdateSlice() {
            
            if (DataPath != " ")
            {
                CancelThread.Cancel();
                ASlice = true;
                float w2 = DataWidth / 2;
                float h2 = DataHeight / 2;
                float d2 = DataDepth / 2;
                ChunkTextureSizeA = LowLoDChunkSize;
                for (int h = 0; h < ChunksPerDim; h++)
                {
                    for (int i = 0; i < ChunksPerDim; i++)
                    {
                        if (ChunkVisibility[h * ChunksPerDim + i])
                        {
                            byte[] chunkPixels = new byte[LowLoDChunkSize*LowLoDChunkSize*ByteSize];
                            for (int j = 0; j < LowLoDChunkSize; j++)
                            {
                                for (int k = 0; k < LowLoDChunkSize; k++)
                                {
                                    byte[] pixelBytes = new byte[ByteSize];
                                    Vector3 texelPosition = Vector3.Transform(new Vector3((i - (ChunksPerDim / 2)) * LowLoDChunkSize * LowLoDUnitSize + (k * LowLoDUnitSize), ((ChunksPerDim / 2) - h - 1) * LowLoDChunkSize * LowLoDUnitSize + (j * LowLoDUnitSize), SliceDepth), Rotations);
                                    if (MathF.Abs(texelPosition.X) > w2 || MathF.Abs(texelPosition.Y) > h2 || MathF.Abs(texelPosition.Z) > d2)
                                    {
                                        for (int l = 0; l < ByteSize; l++)
                                        {
                                            pixelBytes[l] = 0;
                                        }
                                    }
                                    else
                                    {
                                        long a = (long)MathHelper.Clamp(Convert.ToInt64(Round(texelPosition.X + w2 - 1) * LowLoDVolDataScale), 0, LowLoDVolDataWidth - 1);
                                        long b = (long)MathHelper.Clamp(Convert.ToInt64(Round(h2 - texelPosition.Y - 1) * LowLoDVolDataScale), 0, LowLoDVolDataHeight - 1) * LowLoDVolDataWidth;
                                        long c = (long)MathHelper.Clamp(Convert.ToInt64(Round(texelPosition.Z + d2) * LowLoDVolDataScale), 0, LowLoDVolDataDepth - 1);
                                        for (int l = 0; l < ByteSize; l++)
                                        {
                                            pixelBytes[l] = LowLoDVolData[c][((a + b) * ByteSize) + l];
                                        }
                                    }
                                    for (int l = 0; l < ByteSize; l++)
                                    {
                                        chunkPixels[j * LowLoDChunkSize * ByteSize + k * ByteSize + l] = pixelBytes[l];
                                    }
                                }
                            }
                            SliceImgA[h * ChunksPerDim + i] = chunkPixels;
                        }
                        else
                        {
                            SliceImgA[h * ChunksPerDim + i] = Array.Empty<byte>();
                        }
                    }
                }

                //Make another thread to read from harddrive more accurate representation of the slice
                CancelThread = new CancellationTokenSource();
                CancellationToken cToken = CancelThread.Token;
                if (LowLoDVolDataScale < InterpolationScale)
                {
                    float nextSliceDataScale = 2 / MathHelper.NextPowerOfTwo(LowLoDUnitSize);
                    status = "Loading slice: 0,0%";
                    int RenderedChunks = GetRenderedChunkAmount();
                    int taskSize = (int) MathF.Log(1f / nextSliceDataScale, 2f);
                    Task.Run(() => UpdateSlice(Math.Min(nextSliceDataScale, InterpolationScale), cToken, taskSize, 0, RenderedChunks), cToken);//LowLoDVolDataScale * 2
                }
                else status = "Loading slice: Completed";
            }
        }

        /// <summary>
        /// How many chunks are visible in the rendered slice
        /// </summary>
        /// <returns>How many chunks are visible and need to be rendered</returns>
        private int GetRenderedChunkAmount() {
            int a = 0;
            for (int i = 0; i < ChunkVisibility.Length; i++) {
                if (ChunkVisibility[i]) a++;
            }
            return a;
        }

        /// <summary>
        /// Recursively calculate the size of the work done
        /// </summary>
        /// <param name="iterations">Which iteration is going on</param>
        /// <param name="renderedChunks">How many chunks are to be rendered per iteration</param>
        /// <returns>Work done according to parameters</returns>
        private float PreviousIterationProgress(int iterations, int renderedChunks) {
            if (iterations < 0) return 0f;
            else return MathF.Pow(4f, iterations) * renderedChunks + PreviousIterationProgress(iterations - 1, renderedChunks);
        }

        /// <summary>
        /// Update slice used by the background thread to read a more accurate version of the image as a background task
        /// </summary>
        /// <param name="scale">Scale which will be used for interpolating</param>
        /// <param name="token">Thread cancellation token</param>
        /// <param name="taskSize">Size of the task</param>
        /// <param name="iteration">Which iteration is ongoing</param>
        /// <param name="RenderedChunks">How many chunks in the image are to be rendered</param>
        private void UpdateSlice(float scale, CancellationToken token, int taskSize, int iteration, int RenderedChunks) {
            byte[][] tempBytes = new byte[SliceImgA.Length][];
            int width = (int)(DataWidth * scale);
            int heigth = (int)(DataHeight * scale);
            int depth = (int)(DataDepth * scale);
            float unitSize = 1.0f / scale;
            int maxDist = (int)MathHelper.NextPowerOfTwo(Math.Sqrt(depth * depth + width * width + heigth * heigth));
            int chunkSize = MathHelper.Max(1, maxDist / ChunksPerDim);
            float w2 = DataWidth / 2;
            float h2 = DataHeight / 2;
            float d2 = DataDepth / 2;
            int chunksRendered = 0;
            if (DataPath != " ")
            {
                switch (DataType)
                {
                    case FileType.Raw:
                        using (FileStream fs = File.OpenRead(DataPath))
                        {
                            for (int h = 0; h < ChunksPerDim; h++)
                            {
                                for (int i = 0; i < ChunksPerDim; i++)
                                {
                                    if (ChunkVisibility[h * ChunksPerDim + i])
                                    {
                                        byte[] chunkPixels = new byte[chunkSize * chunkSize * ByteSize];
                                        for (int j = 0; j < chunkSize; j++)
                                        {
                                            for (int k = 0; k < chunkSize; k++)
                                            {
                                                if (token.IsCancellationRequested) return;
                                                byte[] pixelBytes = new byte[ByteSize];
                                                Vector3 texelPosition = Vector3.Transform(new Vector3((i - (ChunksPerDim / 2)) * chunkSize * unitSize + (k * unitSize), ((ChunksPerDim / 2) - h - 1) * chunkSize * unitSize + (j * unitSize), SliceDepth), Rotations);
                                                if (MathF.Abs(texelPosition.X) > w2 || MathF.Abs(texelPosition.Y) > h2 || MathF.Abs(texelPosition.Z) > d2)
                                                {
                                                    for (int l = 0; l < ByteSize; l++)
                                                    {
                                                        pixelBytes[l] = 0;
                                                    }
                                                }
                                                else
                                                {
                                                    float x = MathHelper.Clamp(texelPosition.X + w2, 0, DataWidth - 1);
                                                    float y = MathHelper.Clamp(h2 - texelPosition.Y, 0, DataHeight - 1);
                                                    float z = MathHelper.Clamp(texelPosition.Z + d2, 0, DataDepth- 1);
                                                    Interpolate(ref pixelBytes, x, y, z, fs);
                                                    for (int l = 0; l < ByteSize; l++)
                                                    {
                                                        chunkPixels[j * chunkSize * ByteSize + k * ByteSize + l] = pixelBytes[l];
                                                    }
                                                }
                                            }
                                        }
                                        tempBytes[h * ChunksPerDim + i] = chunkPixels;
                                        chunksRendered++;
                                    }
                                    else
                                    {
                                        tempBytes[h * ChunksPerDim + i] = Array.Empty<byte>();
                                    }
                                    float d = (MathF.Pow(4, iteration) * (chunksRendered));
                                    float e = PreviousIterationProgress(iteration - 1, RenderedChunks);
                                    float f = PreviousIterationProgress(taskSize, RenderedChunks);
                                    float taskProgress = (d + e) / f;
                                    status = "Loading slice: " + (100 * taskProgress).ToString("F1") + "%";
                                }
                            }
                        }
                        break;
                    case FileType.ImageSequence:
                        for (int h = 0; h < ChunksPerDim; h++)
                        {
                            for (int i = 0; i < ChunksPerDim; i++)
                            {
                                if (ChunkVisibility[h * ChunksPerDim + i])
                                {
                                    byte[] chunkPixels = new byte[chunkSize * chunkSize * ByteSize];
                                    for (int j = 0; j < chunkSize; j++)
                                    {
                                        for (int k = 0; k < chunkSize; k++)
                                        {
                                            if (token.IsCancellationRequested) return;
                                            byte[] pixelBytes = new byte[ByteSize];
                                            Vector3 texelPosition = Vector3.Transform(new Vector3((i - (ChunksPerDim / 2)) * chunkSize * unitSize + (k * unitSize), ((ChunksPerDim / 2) - h - 1) * chunkSize * unitSize + (j * unitSize), SliceDepth), Rotations);
                                            if (MathF.Abs(texelPosition.X) > w2 || MathF.Abs(texelPosition.Y) > h2 || MathF.Abs(texelPosition.Z) > d2)
                                            {
                                                for (int l = 0; l < ByteSize; l++)
                                                {
                                                    pixelBytes[l] = 0;
                                                }
                                            }
                                            else
                                            {
                                                Interpolate(ref pixelBytes, MathHelper.Clamp(texelPosition.X + w2, 0, DataWidth -1), MathHelper.Clamp(h2 - texelPosition.Y, 0, DataHeight - 1), MathHelper.Clamp(texelPosition.Z + d2, 0, DataDepth - 1));
                                                for (int l = 0; l < ByteSize; l++)
                                                {
                                                    chunkPixels[j * chunkSize * ByteSize + k * ByteSize + l] = pixelBytes[l];
                                                }
                                            }
                                        }
                                    }
                                    tempBytes[h * ChunksPerDim + i] = chunkPixels;
                                    chunksRendered++;
                                }
                                else
                                {
                                    tempBytes[h * ChunksPerDim + i] = Array.Empty<byte>();
                                }
                                float d = (MathF.Pow(4, iteration) * (chunksRendered));
                                float e = PreviousIterationProgress(iteration - 1, RenderedChunks);
                                float f = PreviousIterationProgress(taskSize, RenderedChunks);
                                float taskProgress = (d + e) / f;
                                status = "Loading slice: " + (100 * taskProgress).ToString("F1") + "%";
                            }
                        }
                        break;
                }
                

                lock (SliceLock) {
                    if (ASlice)
                    {
                        SliceImgB = tempBytes;
                        ChunkTextureSizeB = chunkSize;
                    }
                    else
                    {
                        SliceImgA = tempBytes;
                        ChunkTextureSizeA = chunkSize;
                    }
                    ASlice = !ASlice;
                }

                if (scale < InterpolationScale)
                {
                    iteration++;
                    UpdateSlice(Math.Min(scale * 2, InterpolationScale), token, taskSize, iteration, RenderedChunks);
                }
                else status = "Loading slice: Completed";
            }
        }

        /// <summary>
        /// Interpolate value from a certain point from the raw file volumetric data 
        /// </summary>
        /// <param name="bytes">Byte array where the interpolate value is calculated</param>
        /// <param name="pos">Position of the file where the interpolated value is</param>
        /// <param name="fs">Filestream to the raw file</param>
        private void Interpolate(ref byte[] bytes, float x, float y, float z,  FileStream fs)
        {
            byte[] pixelBytes = new byte[ByteSize];
            bool borderCase = false;
            if (Method == InterpolationMethod.Tricubic && (x < 1 || x > DataWidth - 1 || 
                                                           y < 1 || y > DataHeight - 1 || 
                                                           z < 1 || z > DataDepth - 1)) {
                borderCase = true;
                Method = InterpolationMethod.Trilinear;
            }
            switch (Method) {
                case InterpolationMethod.NearestNeighbor:
                    long a = Convert.ToInt64(Round(x));
                    long b = Convert.ToInt64(Round(y)) * DataWidth;
                    long c = Convert.ToInt64(Round(z)) * DataWidth * DataHeight;
                    long pos = (a + b + c) * ByteSize;

                    fs.Position = pos;
                    fs.Read(pixelBytes, 0, pixelBytes.Length);

                    if (BigEndian) Array.Reverse(pixelBytes);
                    break;
                case InterpolationMethod.Trilinear:
                    long xl = Convert.ToInt64(x);
                    long yl = Convert.ToInt64(y) * DataWidth;
                    long zl = Convert.ToInt64(z) * DataWidth * DataHeight;
                    float[] numbers00 = ReadFromStreamTrilinear(fs, xl, yl, zl);
                    float[] numbers01 = ReadFromStreamTrilinear(fs, xl, yl, zl + 1);
                    float[] numbers10 = ReadFromStreamTrilinear(fs, xl, yl + 1, zl);
                    float[] numbers11 = ReadFromStreamTrilinear(fs, xl, yl + 1, zl + 1);
                    float c000 = numbers00[0];
                    float c001 = numbers01[0];
                    float c010 = numbers10[0];
                    float c100 = numbers00[1];
                    float c011 = numbers11[0];
                    float c101 = numbers01[1];
                    float c110 = numbers10[1];
                    float c111 = numbers11[1];

                    float xd = x - (int)x;
                    float yd = y - (int)y;
                    float zd = z - (int)z;

                    float c00 = c000 * (1.0f - xd) + c100 * xd;
                    float c10 = c010 * (1.0f - xd) + c110 * xd;
                    float c01 = c001 * (1.0f - xd) + c101 * xd;
                    float c11 = c011 * (1.0f - xd) + c111 * xd;

                    float c0 = c00 * (1.0f - yd) + c10 * yd;
                    float c1 = c01 * (1.0f - yd) + c11 * yd;

                    float interpolatedValue = c0 * (1.0f - zd) + c1 * zd;

                    switch (ByteSize) {
                        case 1:
                            pixelBytes = BitConverter.GetBytes((sbyte)interpolatedValue);
                            break;
                        case 2:
                            pixelBytes = BitConverter.GetBytes((ushort)interpolatedValue);
                            
                            break;
                        case 4:
                            pixelBytes = BitConverter.GetBytes(interpolatedValue);
                            break;
                        default:
                            pixelBytes = BitConverter.GetBytes(interpolatedValue);
                            break;
                    }
                    break;
                case InterpolationMethod.Tricubic:
                    //Using Catmull-Rom spline as per described in Graphic Gems V Chapter 3 by Alan W. Paeth 1995
                    long xCubic = Convert.ToInt64(x) - 1;
                    long yCubic = Convert.ToInt64(y) - 1;
                    long zCubic = Convert.ToInt64(z) - 1;
                    long widthHeight = DataWidth * DataHeight;
                    float[] interpolateValues = new float[64];
                    bool allZeros = true;
                    for (int k = 0; k < 4; k++) {
                        for (int j = 0; j < 4; j++) {
                            float[] tricubicXFloats = ReadFromStreamTricubic(fs, xCubic, (yCubic + j) * DataWidth, (zCubic + k) * widthHeight);
                            for (int i = 0; i < 4; i++) {
                                interpolateValues[k * 16 + j * 4 + i] = tricubicXFloats[i];
                                if (interpolateValues[k * 16 + j * 4 + i] != 0f) allZeros = false;
                            }
                        }
                    }
                    float value = 0f;
                    if (!allZeros) {
                        float dx = x - (int)x;
                        float dy = y - (int)y;
                        float dz = z - (int)z;
                        float[] u, v, w, r, q;
                        u = new float[4];
                        v = new float[4];
                        w = new float[4];
                        r = new float[4];
                        q = new float[4];

                        u[0] = -0.5f * Cube(dx) + Square(dx) - 0.5f * dx;
                        u[1] = 1.5f * Cube(dx) - 2.5f * Square(dx) + 1f;
                        u[2] = -1.5f * Cube(dx) + 2f * Square(dx) + 0.5f * dx;
                        u[3] = 0.5f * Cube(dx) - 0.5f * Square(dx);

                        v[0] = -0.5f * Cube(dy) + Square(dy) - 0.5f * dy;
                        v[1] = 1.5f * Cube(dy) - 2.5f * Square(dy) + 1f;
                        v[2] = -1.5f * Cube(dy) + 2f * Square(dy) + 0.5f * dy;
                        v[3] = 0.5f * Cube(dy) - 0.5f * Square(dy);

                        w[0] = -0.5f * Cube(dz) + Square(dz) - 0.5f * dz;
                        w[1] = 1.5f * Cube(dz) - 2.5f * Square(dz) + 1f;
                        w[2] = -1.5f * Cube(dz) + 2f * Square(dz) + 0.5f * dz;
                        w[3] = 0.5f * Cube(dz) - 0.5f * Square(dz);

                        int p = 0;

                        for (int k = 0; k < 4; k++)
                        {
                            q[k] = 0;
                            for (int j = 0; j < 4; j++)
                            {
                                r[j] = 0;
                                for (int i = 0; i < 4; i++)
                                {
                                    r[j] += u[i] * interpolateValues[p];
                                    p++;
                                }
                                q[k] += v[j] * r[j];
                            }
                            value += w[k] * q[k];
                        }
                        float maxVal = MathF.Pow(256, ByteSize) - 1;
                        value = MathHelper.Clamp(value, 0f, maxVal);
                    }
                    switch (ByteSize)
                    {
                        case 1:
                            pixelBytes = BitConverter.GetBytes((sbyte)value);
                            break;
                        case 2:
                            pixelBytes = BitConverter.GetBytes((ushort)value);
                            break;
                        case 4:
                            pixelBytes = BitConverter.GetBytes(value);
                            break;
                        default:
                            pixelBytes = BitConverter.GetBytes(value);
                            break;
                    }
                    break;
            }
            if (borderCase) Method = InterpolationMethod.Tricubic;
            bytes = pixelBytes;
        }

        /// <summary>
        /// Cubes a float number
        /// </summary>
        /// <param name="x">Float to be cubed</param>
        /// <returns>Cube of the parameter float</returns>
        private float Cube(float x) {
            return x * x * x;
        }

        /// <summary>
        /// Squares a float number
        /// </summary>
        /// <param name="x">Float to be squared</param>
        /// <returns>Square of the parameter float</returns>
        private float Square(float x) {
            return x * x;
        }

        /// <summary>
        /// Reads 2 floats that resides next to each other in Raw file
        /// </summary>
        /// <param name="fs">Filestream to the raw file</param>
        /// <param name="x">Absolute X-coordinate</param>
        /// <param name="y">Absolute Y-coordinate times data width</param>
        /// <param name="z">Absolute Z-coordinate times data width and heigth</param>
        /// <returns>Array containing the 2 floats</returns>
        private float[] ReadFromStreamTrilinear(FileStream fs, long x, long y, long z)
        {
            byte[] bytes = new byte[ByteSize*2];
            long a = x;
            long b = y;
            long c = z;
            long pos = (a + b + c) * ByteSize;
            fs.Position = pos;
            fs.Read(bytes, 0, bytes.Length);
            if (BigEndian) Array.Reverse(bytes);
            byte[][] numbers = new byte[2][];
            for (int j = 0; j < numbers.Length; j++) {
                numbers[j] = new byte[ByteSize];
                for (int i = 0; i < ByteSize; i++) {
                    numbers[j][i] = bytes[j * ByteSize + i];
                }
            }
            float[] floatValue = new float[2];
            for (int i = 0; i < numbers.Length; i++) {
                switch (ByteSize)
                {
                    case 1:
                        floatValue[i] = (float)bytes[0];
                        break;
                    case 2:
                        floatValue[i] = (float)BitConverter.ToUInt16(bytes, 0);
                        break;
                    case 4:
                        floatValue[i] = BitConverter.ToSingle(bytes);
                        break;
                    default:
                        floatValue[i] = BitConverter.ToSingle(bytes);
                        break;
                }
            }
            
            return floatValue;
        }

        /// <summary>
        /// Reads 4 floats that resides next to each other in Raw file
        /// </summary>
        /// <param name="fs">Filestream to the raw file</param>
        /// <param name="x">Absolute X-coordinate</param>
        /// <param name="y">Absolute Y-coordinate times data width</param>
        /// <param name="z">Absolute Z-coordinate times data width and heigth</param>
        /// <returns>Array containing the 4 floats</returns>
        private float[] ReadFromStreamTricubic(FileStream fs, long x, long y, long z)
        {
            byte[] bytes = new byte[ByteSize * 4];
            long a = x;
            long b = y;
            long c = z;
            long pos = (a + b + c) * ByteSize;
            fs.Position = pos;
            fs.Read(bytes, 0, bytes.Length);
            if (BigEndian) Array.Reverse(bytes);
            byte[][] numbers = new byte[4][];
            for (int j = 0; j < numbers.Length; j++)
            {
                numbers[j] = new byte[ByteSize];
                for (int i = 0; i < ByteSize; i++)
                {
                    numbers[j][i] = bytes[j * ByteSize + i];
                }
            }
            float[] floatValue = new float[4];
            for (int i = 0; i < numbers.Length; i++)
            {
                switch (ByteSize)
                {
                    case 1:
                        floatValue[i] = (float)bytes[0];
                        break;
                    case 2:
                        floatValue[i] = (float)BitConverter.ToUInt16(bytes, 0);
                        break;
                    case 4:
                        floatValue[i] = BitConverter.ToSingle(bytes);
                        break;
                    default:
                        floatValue[i] = BitConverter.ToSingle(bytes);
                        break;
                }
            }

            return floatValue;
        }

        /// <summary>
        /// Interpolation method for the image sequence data type. Takes absolute coordinates. 
        /// So for example X == 0 means the leftmost pixel of the Z-stack slice image.
        /// </summary>
        /// <param name="bytes">Reference bytes where interpolated pixel will be stored</param>
        /// <param name="x">Coordinate X as a absolute coordinate</param>
        /// <param name="y">Coordinate Y as a absolute coordinate</param>
        /// <param name="z">Coordinate Z as a absolute coordinate</param>
        private void Interpolate(ref byte[] bytes, float x, float y, float z)
        {
            byte[] pixelBytes = new byte[ByteSize];

            int zRound = Round(z);
            if (zRound != LastSeqImgNumber)
            {
                LastSeqImgNumber = zRound;
                List<string> imgSeq = MakeImgSeqFileList(DataPath, ImageSeqFileTemplate);

                MagickImage img = new MagickImage(imgSeq[Round(z)]);
                img.Format = MagickFormat.R;
                LastSeqImg = img.ToByteArray();
            }

            for (int i = 0; i < ByteSize; i++)
            {
                pixelBytes[i] = LastSeqImg[(Round(y) * DataWidth + Round(x)) * ByteSize + i];
            }
            bytes = pixelBytes;

            //WIP NOT WORKING version of the tricubic and trilinear interpolation
            /*byte[] pixelBytes = new byte[ByteSize];
            bool borderCase = false;
            if (Method == InterpolationMethod.Tricubic && (x < 1 || x > DataWidth - 1 ||
                                                           y < 1 || y > DataHeight - 1 ||
                                                           z < 1 || z > DataDepth - 1))
            {
                borderCase = true;
                Method = InterpolationMethod.Trilinear;
            }
            switch (Method)
            {
                case InterpolationMethod.NearestNeighbor:
            
                    int zRound = Round(z);
                    if (zRound != LastSeqImgNumber)
                    {
                        LastSeqImgNumber = zRound;
                        List<string> imgSeq = MakeImgSeqFileList(DataPath, ImageSeqFileTemplate);
                    
                        MagickImage img = new MagickImage(imgSeq[Round(z)]);
                        img.Format = MagickFormat.R;
                        LastSeqImg = img.ToByteArray();
                    }
                    
                    for (int i = 0; i < ByteSize; i++)
                    {
                        pixelBytes[i] = LastSeqImg[(Round(y) * DataWidth + Round(x)) * ByteSize + i];
                    }
                    break;
                case InterpolationMethod.Trilinear:
                    int zTrilinear = (int)z;
                    if (zTrilinear != LastSeqImgNumberTrilinear)
                    {
                        LastSeqImgNumberTrilinear = zTrilinear;
                        List<string> imgSeq = MakeImgSeqFileList(DataPath, ImageSeqFileTemplate);
                        for (int i = 0; i < 2; i++)
                        {
                            using (MagickImage img = new MagickImage(imgSeq[zTrilinear + i])) {
                                img.Format = MagickFormat.R;
                                LastSeqImgTrilinear[i] = img.ToByteArray();
                            }
                            
                        }
                    }
                    
                    long xl = Convert.ToInt64(x);
                    long yl = Convert.ToInt64(y);
             
                    float c000 = ReadFromStream(xl, yl, LastSeqImgTrilinear[0]);
                    float c001 = ReadFromStream(xl, yl, LastSeqImgTrilinear[1]);
                    float c010 = ReadFromStream(xl, yl + 1, LastSeqImgTrilinear[0]);
                    float c100 = ReadFromStream(xl + 1, yl, LastSeqImgTrilinear[0]);
                    float c011 = ReadFromStream(xl, yl + 1, LastSeqImgTrilinear[1]);
                    float c101 = ReadFromStream(xl + 1, yl, LastSeqImgTrilinear[1]);
                    float c110 = ReadFromStream(xl + 1, yl + 1, LastSeqImgTrilinear[0]);
                    float c111 = ReadFromStream(xl + 1, yl + 1, LastSeqImgTrilinear[1]);

                    float xd = x - (int)x;
                    float yd = y - (int)y;
                    float zd = z - (int)z;
             
                    float c00 = c000 * (1.0f - xd) + c100 * xd;
                    float c10 = c010 * (1.0f - xd) + c110 * xd;
                    float c01 = c001 * (1.0f - xd) + c101 * xd;
                    float c11 = c011 * (1.0f - xd) + c111 * xd;
             
                    float c0 = c00 * (1.0f - yd) + c10 * yd;
                    float c1 = c01 * (1.0f - yd) + c11 * yd;
             
                    float interpolatedValue = c0 * (1.0f - zd) + c1 * zd;
             
                    switch (ByteSize)
                    {
                        case 1:
                            pixelBytes = BitConverter.GetBytes((sbyte)interpolatedValue);
                            break;
                        case 2:
                            pixelBytes = BitConverter.GetBytes((ushort)interpolatedValue);
                            break;
                        case 4:
                            pixelBytes = BitConverter.GetBytes(interpolatedValue);
                            break;
                        default:
                            pixelBytes = BitConverter.GetBytes(interpolatedValue);
                            break;
                    }
                    break;
                case InterpolationMethod.Tricubic:
                    int zTricubic = (int)z - 1;
                    if (zTricubic != LastSeqImgNumberTricubic)
                    {
                        LastSeqImgNumberTricubic = zTricubic;
                        List<string> imgSeq = MakeImgSeqFileList(DataPath, ImageSeqFileTemplate);
                        for (int i = 0; i < 4; i++)
                        {
                            MagickImage img = new MagickImage(imgSeq[zTricubic + i]);
                            img.Format = MagickFormat.R;
                            LastSeqImgTricubic[i] = img.ToByteArray();
                        }
                    }
                    long xCubic = Convert.ToInt64(x) - 1;
                    long yCubic = Convert.ToInt64(y) - 1;
                    long zCubic = Convert.ToInt64(z) - 1;
                    long widthHeight = DataWidth * DataHeight;
                    float[] interpolateValues = new float[64];
                    bool allZeros = true;
                    for (int k = 0; k < 4; k++)
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                interpolateValues[k * 16 + j * 4 + i] = ReadFromStream(xCubic + i, (yCubic + j), k);
                                if (interpolateValues[k * 16 + j * 4 + i] != 0f) allZeros = false;
                            }
                        }
                    }
                    float value = 0f;
                    if (!allZeros)
                    {
                        float dx = x - (int)x;
                        float dy = y - (int)y;
                        float dz = z - (int)z;
                        float[] u, v, w, r, q;
                        u = new float[4];
                        v = new float[4];
                        w = new float[4];
                        r = new float[4];
                        q = new float[4];
             
                        u[0] = -0.5f * Cube(dx) + Square(dx) - 0.5f * dx;
                        u[1] = 1.5f * Cube(dx) - 2.5f * Square(dx) + 1f;
                        u[2] = -1.5f * Cube(dx) + 2f * Square(dx) + 0.5f * dx;
                        u[3] = 0.5f * Cube(dx) - 0.5f * Square(dx);
             
                        v[0] = -0.5f * Cube(dy) + Square(dy) - 0.5f * dy;
                        v[1] = 1.5f * Cube(dy) - 2.5f * Square(dy) + 1f;
                        v[2] = -1.5f * Cube(dy) + 2f * Square(dy) + 0.5f * dy;
                        v[3] = 0.5f * Cube(dy) - 0.5f * Square(dy);
             
                        w[0] = -0.5f * Cube(dz) + Square(dz) - 0.5f * dz;
                        w[1] = 1.5f * Cube(dz) - 2.5f * Square(dz) + 1f;
                        w[2] = -1.5f * Cube(dz) + 2f * Square(dz) + 0.5f * dz;
                        w[3] = 0.5f * Cube(dz) - 0.5f * Square(dz);
             
                        int p = 0;
             
                        for (int k = 0; k < 4; k++)
                        {
                            q[k] = 0;
                            for (int j = 0; j < 4; j++)
                            {
                                r[j] = 0;
                                for (int i = 0; i < 4; i++)
                                {
                                    r[j] += u[i] * interpolateValues[p];
                                    p++;
                                }
                                q[k] += v[j] * r[j];
                            }
                            value += w[k] * q[k];
                        }
                        float maxVal = MathF.Pow(256, ByteSize) - 1;
                        value = MathHelper.Clamp(value, 0f, maxVal);
                    }
                    switch (ByteSize)
                    {
                        case 1:
                            pixelBytes = BitConverter.GetBytes((sbyte)value);
                            break;
                        case 2:
                            pixelBytes = BitConverter.GetBytes((ushort)value);
                            break;
                        case 4:
                            pixelBytes = BitConverter.GetBytes(value);
                            break;
                        default:
                            pixelBytes = BitConverter.GetBytes(value);
                            break;
                    }
                    break;
            }
            if (borderCase) Method = InterpolationMethod.Tricubic;
            bytes = pixelBytes;*/
        }

        /*private float ReadFromStream(long x, long y, long z)
        {
            byte[] bytes = new byte[ByteSize];
            for (int i = 0; i < ByteSize; i++)
            {
                switch (Method) {
                    case InterpolationMethod.NearestNeighbor:
                        bytes[i] = LastSeqImg[(y * DataWidth + x) * ByteSize + i];
                        break;
                    case InterpolationMethod.Trilinear:
                        bytes[i] = LastSeqImgTrilinear[z][(y * DataWidth + x) * ByteSize + i];
                        break;
                    case InterpolationMethod.Tricubic:
                        if (LastSeqImgTricubic[z] != null) bytes[i] = LastSeqImgTricubic[z][(y * DataWidth + x) * ByteSize + i];
                        break;
                }
                
            }

            float floatValue;
            switch (ByteSize)
            {
                case 1:
                    floatValue = (float)bytes[0];
                    break;
                case 2:
                    floatValue = (float)BitConverter.ToUInt16(bytes, 0);
                    break;
                case 4:
                    floatValue = BitConverter.ToSingle(bytes);
                    break;
                default:
                    floatValue = BitConverter.ToSingle(bytes);
                    break;
            }
            return floatValue;
        }*/

        /*private float ReadFromStream(long x, long y, byte[] sliceBytes)
        {
            byte[] bytes = new byte[ByteSize];

            for (int i = 0; i < ByteSize; i++)
            {
                switch (Method)
                {
                    case InterpolationMethod.NearestNeighbor:
                        if (LastSeqImg != null) bytes[i] = LastSeqImg[(y * DataWidth + x) * ByteSize + i];
                        break;
                    case InterpolationMethod.Trilinear:
                        if (sliceBytes != null) bytes[i] = sliceBytes[(y * DataWidth + x) * ByteSize + i];
                        break;
                    case InterpolationMethod.Tricubic:
                        if (sliceBytes != null) bytes[i] = sliceBytes[(y * DataWidth + x) * ByteSize + i];
                        break;
                }

            }

            float floatValue;
            switch (ByteSize)
            {
                case 1:
                    floatValue = (float)bytes[0];
                    break;
                case 2:
                    floatValue = (float)BitConverter.ToUInt16(bytes, 0);
                    break;
                case 4:
                    floatValue = BitConverter.ToSingle(bytes);
                    break;
                default:
                    floatValue = BitConverter.ToSingle(bytes);
                    break;
            }
            return floatValue;
        }*/

        /// <summary>
        /// Round positive float to an closest integer
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        private int Round(float a) {
            int Rounded = (int)a;
            float b = Rounded;
            if (a - b >= 0.5f) Rounded += 1;
            return Rounded;
        }

        /// <summary>
        /// Exception class for image reading
        /// </summary>
        public class ImageReadException : Exception
        {
            public ImageReadException(string message) : base(message)
            {
            }
        }

        /// <summary>
        /// Changes filepath to integer according to the ending of filename. 
        /// Works as a natural sort for LINQ OrderBy aslong as the filename 
        /// stays the same.
        /// </summary>
        /// <param name="filepath">String of filepath.</param>
        /// <returns>The integer in the end of the file, or 0 if there isnt any.</returns>
        /// <exception cref="ArgumentException">Thrown when failed to sort filenames</exception>
        private static int NaturalSort(string filepath)
        {
            int filetypeI = filepath.LastIndexOf(".");
            int filepathI = filepath.LastIndexOf("\\");
            string path
                = filepath.Substring(filepathI + 1, filetypeI - filepathI - 1);
            if (Int32.TryParse(string.Concat(path.ToArray().Reverse().TakeWhile(char.IsNumber).Reverse()),
                                                                   out int result)) return result;
            return 1;
        }

        /// <summary>
        /// Makes a filelist for image sequence.
        /// </summary>
        /// <param name="filepath">Path to the image sequence file or directory
        /// containing it.</param>
        /// <param name="template">Template to filter image files.</param>
        /// <returns>List of paths containing image sequence.</returns>
        /// <exception cref="ImageReadException">Thrown when failed to make image list</exception>
        private static List<string> MakeImgSeqFileList(string filepath,
                                                         string template)
        {
            try
            {
                int filepathI = filepath.LastIndexOf("\\");
                string path = filepath[..filepathI];

                string[] files = Directory.GetFiles(path);
                List<string> filteredFiles = new();

                //Filter non-image files.
                string[] imgExtensions = { "png", "bmp", "tif", "tiff" };
                foreach (string file in files)
                {
                    string filename = file[(file.LastIndexOf("\\") + 1)..];
                    if (imgExtensions.Any(x => file.EndsWith(x))
                         && filename.StartsWith(template))
                        filteredFiles.Add(file);
                }
                return filteredFiles.OrderBy(file => NaturalSort(file)).ToList();
            }
            catch (ArgumentException e)
            {
                throw new ImageReadException("Failed to sort the files: " + e.Message);
            }
        }

        /// <summary>
        /// Change the depth of slice image
        /// </summary>
        /// <param name="depth"></param>
        public void ChangeSliceDepth(int depth) { 
            SliceDepth = depth;
            UpdateSlice();
        }

        /// <summary>
        /// Change the rotations of the slice image
        /// </summary>
        /// <param name="rotations">Rotations of the current arcball camera view</param>
        public void ChangeSliceRotations(Quaternion rotations) { 
            Rotations = rotations;
            UpdateSlice();
        }

        /// <summary>
        /// Get Byte size of the raw file
        /// </summary>
        /// <returns>Bytesize of the raw file</returns>
        public int GetByteSize() {
            return ByteSize;
        }

        /// <summary>
        /// Get diameter of the one square chunk
        /// </summary>
        /// <returns>Diameter of the chunk</returns>
        public float GetChunkDist() { 
            return ChunkDist;
        }

        /// <summary>
        /// Get how many chunks there are per dimension
        /// </summary>
        /// <returns>Amount of chunks per dimension</returns>
        public int GetChunksPerDim() {
            return ChunksPerDim;
        }

        /// <summary>
        /// Get dimension of the whole slice image.
        /// </summary>
        /// <returns>Dimension of the square slice</returns>
        public int GetMaxDist()
        {
            return MaxDist;
        }

        /// <summary>
        /// Returns the chunk size of the chunk texture
        /// </summary>
        /// <returns>Size of the chunk texture</returns>
        public int GetChunkSize() {
            if(ASlice) return ChunkTextureSizeA;
            else return ChunkTextureSizeB;
        }

        /// <summary>
        /// Returns the texture bytes of one chunk
        /// </summary>
        /// <param name="index">Index of the chunk in SliceImage array</param>
        /// <returns>Texture bytes of the chunk</returns>
        public byte[] GetSliceChunk(int index) {
            lock (SliceLock) {
                if (ASlice) return SliceImgA[index];
                else return SliceImgB[index];
            }
        }

        /// <summary>
        /// Sets big endian
        /// </summary>
        /// <param name="end">Boolean value if the file is big endian</param>
        public void SetBigEndian(bool end) { 
            BigEndian = end;
            MakeLowLodModel();
            UpdateSlice();
        }
        
        /// <summary>
        /// Sets the filepath of the volume data file
        /// </summary>
        /// <param name="path">filepath to the file</param>
        public void SetFilepath(string path)
        { 
            DataPath = path;
            MakeLowLodModel();
            UpdateSlice();
        }

        /// <summary>
        /// Sets the byte depth of the volume data
        /// </summary>
        /// <param name="byteD">How many bytes per volume data value</param>
        public void SetByteDepth(int byteD)
        { 
            ByteSize = byteD;
            MakeLowLodModel();
            UpdateSlice();
        }

        /// <summary>
        /// Sets filetype of volume data
        /// </summary>
        /// <param name="fType">FIletype of volumedata</param>
        public void SetFileType(FileType fType) { 
            DataType = fType;
            MakeLowLodModel();
            UpdateSlice();
        }

        /// <summary>
        /// Sets the interpolation method
        /// </summary>
        /// <param name="iMethod">Interpolation method</param>
        public void SetInterpolationMethod(InterpolationMethod iMethod)
        {
            Method = iMethod;
            MakeLowLodModel();
            UpdateSlice();
        }

        /// <summary>
        /// Sets the image sequence file template
        /// </summary>
        /// <param name="template">Filename template</param>
        public void SetFileTemplate(string template) {
            ImageSeqFileTemplate = template;
            MakeLowLodModel ();
            UpdateSlice();
        }

        /// <summary>
        /// Sets the interpolation scale of the data
        /// </summary>
        /// <param name="iScale">Interpolation scale</param>
        public void SetInterpolationScale(float iScale) {
            InterpolationScale = iScale;
            UpdateSlice();
        }

        /// <summary>
        /// Sets the Low LoD volume data max size
        /// </summary>
        /// <param name="maxSize">Max size of the small version of the data in MB</param>
        public void SetLowLodMaxSize(long maxSize) { 
            LowLoDVolDataMBSize = maxSize;
            MakeLowLodModel ();
            UpdateSlice ();
        }

        /// <summary>
        /// Gets the chunk dimensions
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public int GetChunkDims(int index) {
            lock (SliceLock)
            {
                if (ASlice) return (int)Math.Sqrt(SliceImgA[index].Length/ByteSize);
                else return (int)Math.Sqrt(SliceImgB[index].Length / ByteSize);
            }
        }
    }
}
