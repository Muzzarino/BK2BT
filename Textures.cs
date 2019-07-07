/****************************************************************************
*                                                                           *
* BK2BT - An N64 Graphics Microcode Converter                               *
* https://www.YouTube.com/Trenavix/                                         *
* Copyright (C) 2017 Trenavix. All rights reserved.                         *
*                                                                           *
* License:                                                                  *
* GNU/GPLv2 http://www.gnu.org/licenses/gpl-2.0.html                        *
*                                                                           *
****************************************************************************/

using OpenTK;
using System;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using static OpenTK.GLControl;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public class Textures
{
    public static bool[] FirstTexLoad = new bool[2];
    public static uint[][] TextureArray = new uint[2][]; // Two Binfiles (Main+Alpha)
    public static uint[][] TextureAddrArray = new uint[2][];
    public static readonly byte RGBAMODE = 0;
    public static readonly byte YUVMODE = 1;
    public static readonly byte CIMODE = 2;
    public static readonly byte IAMODE = 3;
    public static readonly byte IMODE = 4;
    public static byte MODE = 0;
    public static byte BitSize = 0;
    public static bool MipMapping = false;

    public static uint currentTexAddr = 0;

    public static short[] currentPalette = new short[0]; //Init with size or error!
    public static int ShortstoLoad = 0;
    public static int BytestoLoad = 200;
    public static int Height = 32;
    public static int Width = 32;
    public static int TMEMOffset = 0;
    public static int TFlags = 0;
    public static int SFlags = 0;
    public static float S_Scale = 1;
    public static float T_Scale = 1;

    public static int LoadTexture(BTBinFile Bin)
    {
        uint CICount = (uint)currentPalette.Length;
        int NewTexture = 0;
        if (MODE == CIMODE) { NewTexture = LoadCITexture(Bin); }
        else if (MODE == RGBAMODE && BitSize == 16) { NewTexture = LoadRGBA16Texture(Bin); }
        else if (MODE == RGBAMODE && BitSize == 32) { NewTexture = LoadRGBA32Texture(Bin); }
        else if (MODE == IAMODE) { NewTexture = LoadIA8Texture(Bin); }
        return NewTexture;
    }

    public static int LoadCITexture(BTBinFile Binfile)
    {
        int id = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, id);
        uint TextureDataAddr = Binfile.getTextureDataAddr();
        byte[] CITex;
        short[] NewTexture;
        if (currentPalette.Length <= 16)
        {
            CITex = Binfile.copyBytestoArray(TextureDataAddr + currentTexAddr, (uint)(Width*Height/2));//4bpp
            NewTexture = CI4ToRGB5A1(CITex, currentPalette);
        }
        else
        {
            CITex = Binfile.copyBytestoArray(TextureDataAddr + currentTexAddr, (uint)(Width*Height));//8bpp
            NewTexture = CI8ToRGB5A1(CITex, currentPalette);
        }
        GL.TexImage2D
            (
            TextureTarget.Texture2D, 
            0, 
            PixelInternalFormat.Rgb5A1, 
            Width, 
            Height, 
            0, 
            OpenTK.Graphics.OpenGL.PixelFormat.Rgba, 
            PixelType.UnsignedShort5551, 
            NewTexture
            );
        getTSFlags();
        getSTDTextureFilters();
        return id;
    }

    public static int LoadRGBA16Texture(BTBinFile Binfile)
    {
        int id = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, id);
        uint TextureDataAddr = Binfile.getTextureDataAddr();
        short[] TexData = LoadRGBA16TextureData(Width*Height, Binfile);
        GL.TexImage2D
            (
            TextureTarget.Texture2D, 
            0, 
            PixelInternalFormat.Rgb5A1, 
            Width, 
            Height, 
            0, 
            OpenTK.Graphics.OpenGL.PixelFormat.Rgba, 
            PixelType.UnsignedShort5551, 
            TexData
            );
        getTSFlags();
        getSTDTextureFilters();
        return id;
    }

    public static int LoadRGBA32Texture(BTBinFile Binfile)
    {
        int id = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, id);
        uint TexDataAddr = Binfile.getTextureDataAddr();
        UInt32[] TexData = new UInt32[ShortstoLoad]; //actually FloatsToLoad but accounted for as Shorts
        for (uint i = 0; i < ShortstoLoad; i++)
        {
            TexData[i] = Binfile.ReadFourBytes(TexDataAddr + currentTexAddr + (i * 4));
        }
        GL.TexImage2D
            (
            TextureTarget.Texture2D, 
            0, 
            PixelInternalFormat.Rgba32f, 
            Width, 
            Height, 
            0, 
            OpenTK.Graphics.OpenGL.PixelFormat.Rgba, 
            PixelType.UnsignedInt8888, 
            TexData
            );
        getTSFlags();
        getSTDTextureFilters();
        return id;
    }

    public static int LoadIA8Texture(BTBinFile Binfile)
    {
        int id = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, id);
        uint TextureDataAddr = Binfile.getTextureDataAddr();
        byte[] TexData = Binfile.copyBytestoArray(TextureDataAddr + currentTexAddr, (uint)(Width*Height));

        GL.TexImage2D
            (
            TextureTarget.Texture2D, 
            0, 
            PixelInternalFormat.Alpha, 
            Width, 
            Height, 
            0, 
            OpenTK.Graphics.OpenGL.PixelFormat.Alpha, 
            PixelType.UnsignedByte, 
            TexData
            );
        getTSFlags();
        getSTDTextureFilters();
        return id;
    }

    public static short[] CI4ToRGB5A1(byte[] data, short[] palette)
    {
        short[] newtexture = new short[data.Length * 2];

        for (int i = 0; i < data.Length; i++)
        {
            int idx_a = (data[i] & 0xF0) >> 4;
            int idx_b = data[i] & 0x0F;

            short color_a = palette[idx_a];
            short color_b = palette[idx_b];

            int new_idx = i * 2;

            newtexture[new_idx] = color_a;
            newtexture[new_idx + 1] = color_b;
        }
        return newtexture;
    }

    public static short[] CI8ToRGB5A1(byte[] data, short[] palette)
    {
        short[] newtexture = new short[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            int idx = data[i];
            short color = palette[idx];
            newtexture[i] = color;
        }
        return newtexture;
    }

    public static short[] LoadRGBA16TextureData(int ShortsToLoad, BTBinFile Binfile)
    {
        uint TexDataAddr = Binfile.getTextureDataAddr();
        short[] data = new short[ShortsToLoad];
        for (uint i = 0; i < ShortsToLoad; i++)
        {
            data[i] = (short)Binfile.ReadTwoBytes(TexDataAddr + currentTexAddr + (uint)(i * 2));
        }
        return data;
    }

    public static Vector2 ClampMode(byte F5CmdByte6, byte F5CmdByte7)
    {
        int ClampT = (F5CmdByte6 & 0x0F) >> 2;
        int ClampS = (F5CmdByte7 & 0x0F) & 7;
        return new Vector2(ClampS, ClampT);
    }

    public static void getTSFlags()
    {  
        if (SFlags == 2) { GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge); }
        else if (SFlags == 1) { GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.MirroredRepeat); }
        else GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        if (TFlags == 2) { GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge); }
        else if (TFlags == 1) { GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.MirroredRepeat); }
        else { GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat); }
    }

    public static void getSTDTextureFilters()
    {
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
    }

    public static void InitialiseTextures(BTBinFile Bin, int BinNum)
    {
        TextureArray[BinNum] = new uint[Bin.getTextureCount()];
        FirstTexLoad[BinNum] = true;
    }
}