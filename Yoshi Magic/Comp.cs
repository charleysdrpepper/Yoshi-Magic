using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yoshi_Magic
{
    static class Comp { //Might just be a mem manager?
        //Superstar Saga Decompression ; Decompresses graphics and mini-game data.
        public static void mlssdecd(byte[] sBuf, int src, byte[] data2, int dest) {
            //byte[] sBuf = Form1.rom.buffer;
            //byte[] data2 = Form1.ram.buffer; // new byte[0x1ffff];
            src &= 0x1FFFFFF;
            dest &= 0xFFFFFF;
            src += (sBuf[src] >> 6) + 1;
            while (true) {
                byte arg1 = sBuf[src++];
                switch (arg1 >> 5) {
                    case 4: //80-9F
                        for (int c = 0; c <= (arg1 & 0x1F); c++) {
                            data2[dest++] = sBuf[src++];
                        }
                        break;
                    case 5: //A0-BF
                         for (int c = 0; c <= (arg1 & 0x1F); c++) {
                             data2[dest++] = 0;
                             data2[dest++] = sBuf[src++];
                         }
                        break;
                    case 6: //C0-DF
                        byte data1 = sBuf[src++];
                        for (int c = 0; c <= ((arg1 & 0x1F) + 1); c++) {
                            data2[dest++] = data1;
                        }
                        break;
                    case 7: //E0-FF
                        for (int c = 0; c <= (arg1 & 0x1F); c++) {
                            data2[dest++] = 0;
                        }
                        if (arg1 == 0xFF) {
                            data1 = sBuf[src++];
                            for (int c = 0; c < data1; c++) {
                                data2[dest++] = 0;
                            }
                        }
                        break;
                    default: //00-7F
                        data1 = sBuf[src++];
                        if ((arg1 == 0x7F) && (data1 == 0xFF)) { return; }
                        //int distance = -0x400 + ((arg1 & 0x3) << 8) + data1;
                        int distance = unchecked((short)0xFC00) + ((arg1 & 0x3) << 8) + data1;
                        for (int c = 0; c <= ((arg1 >> 2) + 1); c++) {
                            data2[dest] = data2[dest + distance]; dest++;
                        }
                        break;
                }

            }
        }

        //DS Decompression (LZ77)

        //BIS-only decompression (Reverse decompression) ; Decompresses Overlay files.

        //3DS Decompression (LZ78)

        //LZSS (Used by Dream Team, basically ExeFS of 3DS games.) (Might be used by BIS as well.)
        //public static byte[] Decompress(byte[] data) {
        //    try {
        //        // Compressed & Decompressed Data Information
        //        uint CompressedSize = (uint)data.Length;
        //        uint DecompressedSize = (uint)(data[0] + data[1] * 256);
        //        uint SourcePointer = 0x4;
        //        uint DestPointer = 0x0;
        //        byte[] CompressedData = data;
        //        byte[] DecompressedData = new byte[DecompressedSize];
        //        // Start Decompression
        //        while (SourcePointer < CompressedSize && DestPointer < DecompressedSize) {
        //            byte Flag = CompressedData[SourcePointer]; // Compression Flag
        //            SourcePointer++;
        //            for (int i = 7; i >= 0; i--) {
        //                if ((Flag & (1 << i)) == 0) { // Data is not compressed
        //                    DecompressedData[DestPointer++] = CompressedData[SourcePointer++];
        //                } else { // Data is compressed
        //                    int Distance = (((CompressedData[SourcePointer] & 0xF) << 8) | CompressedData[SourcePointer + 1]) + 1;
        //                    int Amount = (CompressedData[SourcePointer] >> 4) + 3;
        //                    SourcePointer += 2;
        //                    // Copy the data
        //                    for (int j = 0; j < Amount; j++)
        //                        DecompressedData[DestPointer + j] = DecompressedData[DestPointer - Distance + j];
        //                    DestPointer += (uint)Amount;
        //                }
        //                // Check for out of range
        //                if (SourcePointer >= CompressedSize || DestPointer >= DecompressedSize)
        //                    break;
        //            }
        //        }
        //        return DecompressedData;
        //    } catch {
        //        return null; // An error occured while decompressing
        //    }
        //}
        //0x38457C //Free bytes
        unsafe public static bool LZSS_Decompress(byte[] compressed, int compressed_size, byte[] decompressed, int decompressed_size) {
            //try {
            //TODO: TRANSFER BEGINNING PART OF FILE! (RAW DATA)
            fixed (byte* pCompressed = &compressed[0x200]) {
                byte* footer = pCompressed + compressed_size - 8;
                int buffer_top_and_bottom = *((int*)(footer));
                int out1 = decompressed_size;
                int index = compressed_size - ((buffer_top_and_bottom >> 24) & 0xFF);
                int stop_index =  compressed_size - (buffer_top_and_bottom & 0xFFFFFF);
                //memset(decompressed, 0, decompressed_size);
                //memcpy(decompressed, compressed, compressed_size);
                //Array.Copy((Array)compressed, (Array)decompressed, compressed_size);
                while (index > stop_index) {
                    byte control = pCompressed[--index];
                    //System.Windows.Forms.MessageBox.Show("DDD");
                    for (int i = 0; i < 8; i++) {
                        if ((index <= stop_index) || (index <= 0) || (out1 <= 0))
                            break;
                        if ((control & 0x80) == 0x80) {
                            if (index < 2)
                                return false; // Compression is out of bounds
                            index -= 2;

                            int segment_offset = pCompressed[index] | (pCompressed[index + 1] << 8);
                            int segment_size = ((segment_offset >> 12) & 15) + 3;
                            segment_offset &= 0x0FFF;
                            segment_offset += 2;

                            if (out1 < segment_size)
                                return false; // Compression is out of bounds
                            for (int j = 0; j < segment_size; j++) {
                                if (out1 + segment_offset >= decompressed_size)
                                    return false;// Compression is out of bounds
                                byte data = decompressed[out1 + segment_offset];
                                decompressed[--out1] = data;
                            }
                        } else {
                            if (out1 < 1)
                                return false; // Compression is out of bounds
                            decompressed[--out1] = pCompressed[--index];
                        }
                        control <<= 1;
                    }
                }
                //decompressed[decompressed_size - 1] = 0xFE;
                return true;
            }
            //} catch { return false; }
        }
    }
}
