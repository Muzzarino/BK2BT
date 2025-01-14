﻿/****************************************************************************
*                                                                           *
* BK2BT - An N64 Graphics Microcode Converter                               *
* https://www.YouTube.com/Trenavix/                                         *
* Copyright (C) 2017 Trenavix. All rights reserved.                         *
*                                                                           *
* License:                                                                  *
* GNU/GPLv2 http://www.gnu.org/licenses/gpl-2.0.html                        *
*                                                                           *
****************************************************************************/

using System;
using System.IO;

public class BTBinFile
{
    private byte[] CurrentBin;

	public BTBinFile(byte[] newBin)
	{
       this.CurrentBin = newBin;
    }

    public UInt32 getGeoAddr()
    {
        return ReadFourBytes(0x04);
    }

    public UInt32 getF3DEX2SetupAddr()
    {
        return ReadFourBytes(0x0C);
    }
    public UInt32 getDLAddr()
    {
        return ReadFourBytes(0x0C)+0x08;
    }

    public void updateF3DEX2SetupAddr(UInt32 newAddr)
    {
        WriteFourBytes(getF3DEX2SetupAddr(), newAddr);
    }
    public UInt16 getTextureSetupAddr()
    {
        return ReadTwoBytes(0x08);
    }
    public UInt16 getTextureCount()
    {
        return ReadTwoBytes((uint)(getTextureSetupAddr()+0x04));
    }
    public uint getTextureDataAddr()
    {
        return (uint)(getTextureSetupAddr() + 0x8+(getTextureCount() * 0x10));
    }


    public UInt32 getVTXSetupAddr()
    {
        return ReadFourBytes(0x10);
    }
    public UInt32 getCollisionSetupAddr()
    {
        return ReadFourBytes(0x1C);
    }
    public UInt16 getVertexCount()
    {
        return (UInt16)((getCollisionSetupAddr() - (getVTXSetupAddr() + 0x18))/0x10);
    }

    public byte[][] getVTXArray()
    {
        byte[][] array = new byte[getVertexCount()][];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = new byte[16];
            for (int j = 0; j < 16; j++)
            {
                array[i][j] = getByte((uint)(getVTXSetupAddr() + 0x18+(0x10*i)+j));
            }
        }
        return array;
    }

    public UInt32 getVTXAddr()
    {
        return ReadFourBytes(0x10)+0x18;
    }

    public uint F3DCommandsLength()
    {
        return ReadFourBytes(getF3DEX2SetupAddr());
    }
    public byte[] getF3DSegment()
    {
        byte[] F3DSeg = new byte[F3DCommandsLength()];
        for (int i = 0; i < F3DCommandsLength()*8; i++)
        {
            F3DSeg[i] = CurrentBin[getF3DEX2SetupAddr()+8+i];
        }
        return F3DSeg;
    }

    public byte[] getCurrentBin()
    {
        return CurrentBin;
    }

    public uint getEndBinAddr()
    {
        return (uint)(CurrentBin.Length-1);
    }

    public UInt16 ReadTwoBytes(uint offset)
    {
        UInt16 value = getByte(offset);
        for (uint i = offset; i < offset + 2; i++)
        {
            value = (UInt16)((value << 8) | CurrentBin[i]);
        }
        return value;
    }
    public UInt16 ReadTwoSignedBytes(uint offset)
    {
        return (UInt16)((getByte(offset + 1) << 8) | getByte(offset));
    }
    public UInt32 ReadFourBytes(uint offset)
    {
        UInt32 value = getByte(offset);
        for (uint i = offset; i < offset + 4; i++)
        {
            value = (value << 8) | CurrentBin[i];
        }
        return value;
    }

    public UInt64 ReadEightBytes(uint offset)
    {
        UInt64 value = getByte(offset);
        for (uint i = offset; i < offset + 8; i++)
        {
            value = (value << 8) | CurrentBin[i];
        }
        return value;
    }
    public void WriteFourBytes(uint offset, UInt32 bytes)
    {
        byte[] currentbyte = BitConverter.GetBytes(bytes);
        for (uint i = offset; i > offset - 4; i--)
        {
            CurrentBin[i + 3] = currentbyte[offset - i];
        }
    }
    public void WriteTwoBytes(uint offset, UInt16 bytes)
    {
        byte[] currentbyte = BitConverter.GetBytes(bytes);
        for (uint i = offset; i > offset - 2; i--)
        {
            CurrentBin[i + 1] = currentbyte[offset - i];
        }
    }
    public void WriteEightBytes(uint offset, UInt64 bytes)
    {
        byte[] currentbyte = BitConverter.GetBytes(bytes);
        for (uint i = offset; i > offset - 8; i--)
        {
            CurrentBin[i + 7] = currentbyte[offset - i];
        }
    }
    public byte getByte(uint offset)
    {
        return CurrentBin[offset];
    }
    public void changeByte(uint offset, byte newbyte)
    {
        if (offset > getEndBinAddr())
        {
            Array.Resize(ref CurrentBin, (int)offset + 1);
        }
        CurrentBin[offset] = newbyte;
    }
    public void copyBytes(uint srcAddr, uint destAddr, uint size)
    {
        byte[] tempbuffer = new byte[size];
        for (uint i = 0; i < size; i++)
        {
            tempbuffer[i] = CurrentBin[srcAddr + i];
        }
        for (uint i = 0; i < size; i++)
        {
            changeByte(destAddr + i, tempbuffer[i]);
        }
    }
    public void cutBytes(uint srcAddr, uint destAddr, uint size)
    {
        byte[] tempbuffer = new byte[size];
        for (uint i = 0; i < size; i++)
        {
            tempbuffer[i] = CurrentBin[srcAddr + i];
        }
        copyBytes(srcAddr, srcAddr - size, getEndBinAddr() - srcAddr); //Copybytes backward to "cut"
        copyBytes(destAddr, destAddr + size, getEndBinAddr() - size - destAddr);
        for (uint i = 0; i < size; i++)
        {
            changeByte(destAddr + i, tempbuffer[i]);
        }
    }
    public byte[] copyBytestoArray(uint srcAddr, uint size)
    {
        byte[] newarray = new byte[size];
        for (int i = 0; i < size; i++)
        {
            newarray[i] = CurrentBin[srcAddr + i];
        }
        return newarray;
    }
    public void writeByteArray(uint offset, byte[] array)
    {
        for (uint i = 0; i < array.Length; i++)
        {
            changeByte(offset + i, array[i]);
        }
    }
    public void changeEndBinAddr(uint newsize)
    {
        Array.Resize(ref CurrentBin, (int)newsize);
    }
}
