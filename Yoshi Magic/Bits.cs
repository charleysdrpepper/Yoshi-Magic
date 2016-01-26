using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace Yoshi_Magic {
    static public class Bits //File Storage/data retrieval.
    {
        //public byte[] buffer = null;
        //Dim ram() As Byte = Nothing 'Optional.
        //string path = null;
        //int pos = 0;

        static public byte[] openFile(string filename) {
            //DateTime a = DateTime.Now;
            //int var1 = 0;
            //byte[] rom = System.IO.File.ReadAllBytes(filename); //add catch?
            //for (int i = 0; i < 0x1000000; i+=4) {
            //    var1 ^= Bits.getInt32(rom, i);
            //}
            //DateTime b = DateTime.Now;
            //Console.WriteLine(var1 + " " + (b - a).ToString());
            //DateTime c = DateTime.Now;
            //int var2 = 0;
            //BinaryReader rom2 = new BinaryReader(new FileStream(filename, FileMode.Open)); //add catch?
            //for (int j = 0; j < 0x1000000; j += 4) {
            //    var2 ^= rom2.ReadInt32();//Bits.getInt32(rom, i);
            //}
            //rom2.Close();
            //DateTime d = DateTime.Now;
            //Console.WriteLine(var2 + " " + (d - c).ToString());
            return System.IO.File.ReadAllBytes(filename); //add catch?
        }
        static public void saveFile(string filename, byte[] buffer) {
            System.IO.File.WriteAllBytes(filename, buffer);
        }
        static public void fastCopy(string source, string destination) {
            int array_length = 0x100000;
            byte[] dataArray = new byte[array_length];
            using (FileStream fsread = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.None, array_length))
            using (BinaryReader bwread = new BinaryReader(fsread))
            using (FileStream fswrite = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, array_length))
            using (BinaryWriter bwwrite = new BinaryWriter(fswrite)) {
                //fswrite.SetLength(0x40000000);
                for (; ; ) {
                    int read = bwread.Read(dataArray, 0, array_length);
                    if (0 == read)
                        break;
                    bwwrite.Write(dataArray, 0, read);
                }
            }
        }
        static public void CopyFile5(string src, string dest) {
            //System.Diagnostics.Stopwatch swTotal = System.Diagnostics.Stopwatch.StartNew();
            using (var outputFile = File.Create(dest)) {
                using (var inputFile = File.OpenRead(src)) {
                    // we need two buffers so we can ping-pong
                    int CopyBufferSize = 0x100000;
                    var buffer1 = new byte[CopyBufferSize];
                    var buffer2 = new byte[CopyBufferSize];
                    var inputBuffer = buffer1;
                    int bytesRead;
                    IAsyncResult writeResult = null;
                    while ((bytesRead = inputFile.Read(inputBuffer, 0, CopyBufferSize)) != 0) {
                        // Wait for pending write
                        if (writeResult != null) {
                            writeResult.AsyncWaitHandle.WaitOne();
                            outputFile.EndWrite(writeResult);
                            writeResult = null;
                        }
                        // Assign the output buffer
                        var outputBuffer = inputBuffer;
                        // and swap input buffers
                        inputBuffer = (inputBuffer == buffer1) ? buffer2 : buffer1;
                        // begin asynchronous write
                        writeResult = outputFile.BeginWrite(outputBuffer, 0, bytesRead, null, null);
                    }
                    if (writeResult != null) {
                        writeResult.AsyncWaitHandle.WaitOne();
                        outputFile.EndWrite(writeResult);
                    }
                }
            }
            //swTotal.Stop();
            //Console.WriteLine("Total time: {0:N4} seconds.", swTotal.Elapsed.TotalSeconds);
        }
        static public byte[] openFilePart(string filename, int addr, int size) {
            byte[] data = new byte[size];
            using (FileStream a = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) { //'StreamReader '(
                a.Seek(addr, SeekOrigin.Begin);
                a.Read(data, 0, size);
                //a.Close()
            }
            return data;
        }
        //public void seek(int addr)  {
        //    pos = addr;
        //}
        //public int getInt8(byte[] buffer, int pos) { //(int addr)
        //    return buffer[pos];
        //}
        //public int getShort(byte[] buffer, ref int pos) { //(int addr)
        //    return buffer[pos++] | (buffer[pos++] << 8);
        //}
        static public int getInt16(byte[] buffer, int pos) { //(int addr)
            return buffer[pos++] | (buffer[pos] << 8);
        }
        static public int getInt32(byte[] buffer, int pos) { //(int addr)
            return buffer[pos++] | (buffer[pos++] << 8) | buffer[pos++] << 16 | (buffer[pos] << 24);
        }
        static public void setInt32(byte[] buffer, int pos, int value) { //(int addr)
            buffer[pos++] = (byte)value; value >>= 8;
            buffer[pos++] = (byte)value; value >>= 8;
            buffer[pos++] = (byte)value; value >>= 8;
            buffer[pos] = (byte)value;
        }
        //public uint getUInt32(int pos) { //(int addr)
        //    return buffer[pos++] | (buffer[pos++] << 8) | buffer[pos++] << 16 | (buffer[pos] << 24);
        //}
        static public int readbits(byte[] buffer, ref int address, ref short bitindex, int bits) {
            //while (bits > 0) {

            //}
            int readbits = 0;
            int r6 = bitindex;
            int r7 = 0 - bits;
            byte argbyte;
        b2: argbyte = buffer[address];// ': bytestring += CStr(Hex(argbyte))
            r7 += r6;
            if (r7 >= 0) { goto b1; }
            r6 = bits + r7;
            //If (r6 >= 0) And (r6 <= 8) Then
            if (r6 <= 8) {
                readbits = (argbyte & ((1 << r6) - 1)) + (readbits << 8);
            } else {
                readbits = (argbyte & 0xFF) + (readbits << 8);
            }
            r6 = 8;
            address += 1;
            goto b2;
        b1: r6 = 8 - r7;
            readbits = ((argbyte >> r7) & ((1 << bits) - 1)) + (readbits << r6);
            if (r7 == 0) { r7 = 8; address++; }
            bitindex = (short)r7;
            return readbits;
        }
        static public string getString(byte[] buffer, int pos, int length) {
            System.Text.StringBuilder strbuild = new System.Text.StringBuilder(length);
            while (length-- > 0) {
                strbuild.Append((char)buffer[pos++]);
            }
            return strbuild.ToString();
        }
        static public string getString16V(byte[] buffer, int pos, int untilVal) {
            System.Text.StringBuilder strbuild = new System.Text.StringBuilder(32);
            while (untilVal != buffer[pos]) {
                strbuild.Append((char)buffer[pos++]); pos++;
            }
            return strbuild.ToString();
        }

        //        readbits = 0
        //        Dim r6 As Integer = bitindex
        //        Dim r7 As Integer = 0 - bits
        //        Dim argbyte As Byte
        //b2:     FileGet(1, argbyte, address + 1) ': bytestring += CStr(Hex(argbyte))
        //        r7 += r6
        //        If r7 >= 0 Then GoTo b1
        //        r6 = bits + r7
        //        'If (r6 >= 0) And (r6 <= 8) Then
        //        'If (r6 >= 0) And (r6 <= 8) Then
        //        If r6 <= 8 Then
        //            readbits = (argbyte And ((1 << r6) - 1)) + (readbits << 8)
        //        Else
        //            readbits = (argbyte And &HFF) + (readbits << 8)
        //        End If
        //        r6 = 8
        //        address += 1
        //        GoTo b2
        //b1:     r6 = 8 - r7
        //        readbits = ((argbyte >> r7) And ((CLng(1) << bits) - 1)) + (readbits << r6)
        //        If r7 = 0 Then r7 = 8 : address += 1
        //        bitindex = r7
        //    End Function
    }
}
