using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;
using StbImageSharp;
using System.IO;
using System.Threading;

namespace OpenTKSlicingModule
{
    public class SliceImage
    {
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
        private bool Raw;
        private int ByteSize = 1;
        private bool BigEndian = true;
        private int MaxDist;
        private int ChunkTextureSizeA;
        private int ChunkTextureSizeB;


        private byte[][] LowLoDVolData;
        private long LowLoDVolDataMBSize = 1;
        private float LowLoDVolDataScale;
        private int LowLoDVolDataWidth;
        private int LowLoDVolDataHeight;
        private int LowLoDVolDataDepth;
        private float LowLoDUnitSize;
        private int LowLoDChunkSize;
        private int LowLoDMaxDist;

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
        public SliceImage(string datapath, int dataWidth, int dataHeight, int dataDepth, Quaternion rotations, int sliceDepth, bool raw, bool endian, int byteSize)
        {
            DataPath = datapath;
            DataWidth = dataWidth;
            DataHeight = dataHeight;
            DataDepth = dataDepth;
            Rotations = rotations;
            SliceDepth = sliceDepth;
            Raw = raw;
            ByteSize = byteSize;
            BigEndian = endian;
            MaxDist = (int)MathHelper.NextPowerOfTwo(MathF.Sqrt(DataDepth * DataDepth + DataWidth * DataWidth + DataHeight * DataHeight));
            ChunkSize = MathHelper.Max(1, MaxDist / ChunksPerDim);
            ChunkDist = MathF.Sqrt(ChunkSize * ChunkSize * 2f) / 2f;
            ChunkVisibility = new bool[ChunksPerDim * ChunksPerDim];
            SliceImgA = new byte[ChunksPerDim * ChunksPerDim][];
            SliceImgB = new byte[ChunksPerDim * ChunksPerDim][];

            MakeLowLodModel();

        }

        private void MakeLowLodModel() {
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
                                Vector3 LowLoDValuePosition = new Vector3(k*LowLoDUnitSize, j*LowLoDUnitSize, i * LowLoDUnitSize);

                                long a = (long)Round(LowLoDValuePosition.X);
                                long b = (long)Round(LowLoDValuePosition.Y) * (long)DataWidth;
                                long c = (long)Round(LowLoDValuePosition.Z) * (long)DataHeight * (long)DataWidth;
                                long pos = (a+b+c) * (long)ByteSize;
                                fs.Position = pos;
                                fs.Read(pixelBytes);
                                for (int l = 0; l < pixelBytes.Length; l++)
                                {
                                    bytes[((j * LowLoDVolDataWidth + k) * ByteSize) + l] = pixelBytes[l];
                                }
                            }
                        }
                        LowLoDVolData[i] = bytes;
                    }
                }
            }
        }

        private void UpdateSlice() {
            CancelThread.Cancel();
            ASlice = true;
            if (DataPath != " ")
            {
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
                                    if (BigEndian) Array.Reverse(pixelBytes);
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
                if (LowLoDVolDataScale < 1)
                {
                    status = "Loading slice: 0,0%";
                    Task.Run(() => UpdateSlice(Math.Min(LowLoDVolDataScale * 2, 1)), cToken);
                }
                else status = "Loading slice: Completed";
            }

        }

        private void UpdateSlice(float scale) {
            byte[][] tempBytes = new byte[SliceImgA.Length][];
            int width = (int)(DataWidth * scale);
            int heigth = (int)(DataHeight * scale);
            int depth = (int)(DataDepth * scale);
            float unitSize = 1.0f / scale;
            int maxDist = (int)MathHelper.NextPowerOfTwo(Math.Sqrt(depth * depth + width * width + heigth * heigth));
            int chunkSize = MathHelper.Max(1, maxDist / ChunksPerDim);
            if (DataPath != " ")
            {
                using (FileStream fs = File.OpenRead(DataPath))
                {
                    float w2 = DataWidth / 2;
                    float h2 = DataHeight / 2;
                    float d2 = DataDepth / 2;

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
                                       
                                            long a = (long)MathHelper.Clamp(Convert.ToInt64(Round(texelPosition.X + w2)), 0, DataWidth - 1);
                                            long b = (long)MathHelper.Clamp(Convert.ToInt64(Round(h2 - texelPosition.Y)), 0, DataHeight - 1) * DataWidth;
                                            long c = (long)MathHelper.Clamp(Convert.ToInt64(Round(texelPosition.Z + d2)), 0, DataDepth - 1) * DataWidth * DataHeight;
                                            long filePos = (a + b + c) * ByteSize;
                                            Interpolate(ref pixelBytes, filePos, fs);
                                            for (int l = 0; l < ByteSize; l++)
                                            {
                                                chunkPixels[j * chunkSize * ByteSize + k * ByteSize + l] = pixelBytes[l];
                                            }
                                        }
                                    }
                                }
                                tempBytes[h * ChunksPerDim + i] = chunkPixels;
                            }
                            else
                            {
                                tempBytes[h * ChunksPerDim + i] = Array.Empty<byte>();
                            }
                        }
                    }
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

                if (scale < 1)
                {
                    status = "Loading slice: " + (100*MathF.Pow(scale, 2)).ToString("F1") + "%";
                    UpdateSlice(Math.Min(scale * 2, 1));
                }
                else status = "Loading slice: Completed";
            }
        }

        //Old raw slice reader
        ///// <summary>
        ///// Read one raw data slice according to object current state
        ///// </summary>
        //private void ReadRawSlice()
        //{
        //    //TODO:
        //    //-Make level scaling (and normalization) to textures
        //    //-Add endianess to slice texture options so it can read it
        //    //-Test that 16 bit images really work
        //    //Add trilinear interpolation
        //    //Add Image sequence support. Possible separate consturctor for img seq
        //
        //    //Start with 1x1 texture and keep enhancing the image until texture is reset or natural sized texture is achieved.
        //    //Add tricubic interpolation
        //    //-Change ImageBytes to be of type byte[SliceDim][SliceDim], so overflow wont happen. Or dont read that big of a texture and 
        //    //-change texture dynamically when zoomed in (also have to change dynamically texture to vertext binding points)
        //    //When levels get bigger Min value than Max value possibly invert grayscale like in imagemagick (could possibly use imagemagick here)
        //
        //    //Wierd slider behavior where values jump all over the place
        //    //Wierd static noice in slice image. Possibly something to do with rotations of the slice not matching the
        //    //cam rotations. Can be perceived also as slice showing wierdly rotated textures
        //    //Slice seems to be off from the center when compared to other visualization methods. Possibly something 
        //    //to do with image partition to smaller tiles. Check if error somewhere there
        //    if (DataPath != " ") {
        //        using (FileStream fs = File.OpenRead(DataPath))
        //        {
        //            float w2 = DataWidth / 2;
        //            float h2 = DataHeight / 2;
        //            float d2 = DataDepth / 2;
        //            ChunkTextureSizeA = ChunkSize;
        //            for (int h = 0; h < ChunksPerDim; h++)
        //            {
        //                for (int i = 0; i < ChunksPerDim; i++)
        //                {
        //                    if (ChunkVisibility[h * ChunksPerDim + i])
        //                    {
        //                        byte[] chunkPixels = new byte[ChunkSize * ChunkSize * ByteSize];
        //                        for (int j = 0; j < ChunkSize; j++)
        //                        {
        //                            for (int k = 0; k < ChunkSize; k++)
        //                            {
        //                                byte[] pixelBytes = new byte[ByteSize];
        //                                Vector3 texelPosition = Vector3.Transform(new Vector3((i - (ChunksPerDim / 2)) * ChunkSize + k, ((ChunksPerDim / 2) - h - 1) * ChunkSize + j, SliceDepth), Rotations);
        //                                if (MathF.Abs(texelPosition.X) > w2 || MathF.Abs(texelPosition.Y) > h2 || MathF.Abs(texelPosition.Z) > d2)
        //                                {
        //                                    for (int l = 0; l < ByteSize; l++)
        //                                    {
        //                                        pixelBytes[l] = 0;
        //                                    }
        //                                }
        //                                else
        //                                {
        //                                    long a = Convert.ToInt64(Round(texelPosition.X + w2));
        //                                    long b = Convert.ToInt64(Round(h2 - texelPosition.Y)) * DataWidth;
        //                                    long c = Convert.ToInt64(Round(texelPosition.Z + d2)) * DataWidth * DataHeight;
        //                                    long filePos = (a + b + c) * ByteSize;
        //                                    Interpolate(ref pixelBytes, filePos,fs);
        //                                    /*fs.Position = filePos;
        //                                    fs.Read(pixelBytes, 0, pixelBytes.Length);
        //
        //                                    if (BigEndian) Array.Reverse(pixelBytes);*/
        //                                    for (int l = 0; l < ByteSize; l++)
        //                                    {
        //                                        chunkPixels[j * ChunkSize * ByteSize + k * ByteSize + l] = pixelBytes[l];
        //                                    }
        //                                }
        //                            }
        //                        }
        //                        SliceImgA[h * ChunksPerDim + i] = chunkPixels;
        //                    }
        //                    else
        //                    {
        //                        SliceImgA[h * ChunksPerDim + i] = Array.Empty<byte>();
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        /// <summary>
        /// Interpolate value from a certain point from the volumetric data
        /// </summary>
        /// <param name="bytes">Byte array where the interpolate value is calculated</param>
        /// <param name="pos">Position of the file where the interpolated value is</param>
        /// <param name="fs">Filestream to the raw file</param>
        private void Interpolate(ref byte[] bytes, long pos, FileStream fs) {
            fs.Position = pos;
            fs.Read(bytes, 0, bytes.Length);

            if (BigEndian) Array.Reverse(bytes);
        }

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
        /// Sets the if the volume data is in raw file
        /// </summary>
        /// <param name="raw">True if raw, false if imageseq</param>
        public void SetRaw(bool raw)
        {
            Raw = true;
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
    }
}
