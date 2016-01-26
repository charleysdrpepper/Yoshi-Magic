using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yoshi_Magic {
    class nds {
        byte[] header;
        //byte[] arm9;
        //byte[] arm7;
        byte[] fat;
        public void openNDS(string filename) {
            header = Bits.openFilePart(filename, 0, 0x200);
            string vstr = Bits.getString(header, 0xA0, 16);
            if ((vstr != "MARIO&LUIGI2ARME") || (vstr != "MARIO&LUIGI3CLJE")) { return; }
            //arm9 = Bits.openFilePart(filename, Bits.getInt32(header, 0x20), Bits.getInt32(header, 0x2C));
            //arm7 = Bits.openFilePart(filename, Bits.getInt32(header, 0x30), Bits.getInt32(header, 0x3C));
            fat = Bits.openFilePart(filename, Bits.getInt32(header, 0x48), Bits.getInt32(header, 0x4C));
            //If PIT:no compression; if BIS:decbis

        }
        public byte[] decbis(string file, int fileID) {
            //For now, load these tables here, but in the future... possibly make a NDS class file
            //to hold these contents for general use? (May result in not having Comp class file.)

            if (fileID < 0) {//ARM9/ARM7 boot files.
                int aEntry = 0x10 + (fileID * -10); //0x20=ARM9; 0x30=ARM7
            } else {
                int a = 0;
            }
            return null;
        }
        //            Function decbis(ByVal File_ID As Integer) As Byte()
        //        Try
        //            FileOpen(1, mainMem.getPath, OpenMode.Binary, OpenAccess.Default, OpenShare.Shared)
        //        Catch ex As Exception
        //        End Try

        //        'Dim arm9binaddr As Integer
        //        'Dim arm9binsize As Integer
        //        Dim locaddrbegin As Integer
        //        Dim locaddr As Integer
        //        Select Case File_ID
        //            Case Is < -2 'Unused
        //                MsgBox("Error decompressing unknown file:  x" & Hex(File_ID)) : Return Nothing : Exit Function
        //            Case Is < 0
        //                FileGet(1, locaddrbegin, &H20 + ((File_ID * -1) << 4) + 1)
        //                FileGet(1, locaddr, &H2C + ((File_ID * -1) << 4) + 1)
        //                locaddr += locaddrbegin 'Since locaddr was the size.
        //            Case Is < &HF000 '< &H8D '(BIS has 141 OVTs)  '< &HE7 '(BIS has 230 files.)
        //                Dim fataddr As Integer 'File Allocation Table
        //                FileGet(1, fataddr, &H48 + 1)
        //                FileGet(1, locaddrbegin, fataddr + (File_ID << 3) + 1) '&H283618 + 1)
        //                FileGet(1, locaddr) ', &H28361C + 1)
        //                'Case Is < &H10000 '(F000 - FFFF are directories.)
        //            Case Else
        //                MsgBox("Error decompressing unknown file:  x" & Hex(File_ID)) : Return Nothing : Exit Function
        //        End Select
        //        Dim h1, h2 As Integer
        //        FileGet(1, h1, locaddr - 8 + 1)
        //        FileGet(1, h2, locaddr - 4 + 1)
        //        h2 += (locaddr - locaddrbegin) 'UPDATED!
        //        locaddr -= (h1 >> &H18)
        //        '  h2 += (h1 And &HFFFFFF) ' + 100
        //        Dim decsize As Integer
        //        decsize = h2
        //        FileClose(1)
        //        Dim decdata(decsize - 1) As Byte
        //        Dim f As New IO.FileStream(mainMem.getPath, IO.FileMode.Open) 'StreamReader '(
        //        f.Seek(locaddrbegin, IO.SeekOrigin.Begin)
        //        f.Read(decdata, 0, locaddr - locaddrbegin) 'decsize)
        //        'Dim poscomp As Integer = locaddr - locaddrbegin
        //        locaddr -= locaddrbegin
        //        'Dispose(h1)
        //        Dim d1, d3, d4 As Byte, d2 As SByte
        //        Try
        //            Dim stopcomp As Integer = locaddr - (h1 And &HFFFFFF) 'UPDATED!
        //newset:     If locaddr <= stopcomp Then GoTo exit1
        //            'If h2 <= 30 Then GoTo exit1 'THE 100 SHOULD BE 0!
        //            locaddr -= 1
        //            'FileGet(1, d1, locaddr + 1)
        //            d1 = decdata(locaddr)
        //            d2 = 8
        //nextb:      d2 -= 1
        //            If d2 < 0 Then GoTo newset
        //            If d1 And &H80 Then GoTo distlen
        //            locaddr -= 1
        //            'FileGet(1, d3, locaddr + 1)
        //            'd3 = decdata(locaddr)
        //            h2 -= 1
        //            decdata(h2) = decdata(locaddr) 'd3
        //            GoTo nexta
        //distlen:    locaddr -= 1
        //            'FileGet(1, d3, locaddr + 1)
        //            d3 = decdata(locaddr)
        //            locaddr -= 1
        //            'FileGet(1, d4, locaddr + 1)
        //            'd4 = decdata(locaddr)
        //            h1 = (((CInt(d3) << 8) Or decdata(locaddr)) And &HFFF) + 2
        //            Dim d5 As Integer
        //            d5 = d3 + &H20
        //n1:         d4 = decdata(h2 + h1)
        //            h2 -= 1
        //            decdata(h2) = d4
        //            d5 -= &H10
        //            If d5 >= 0 Then GoTo n1
        //nexta:      d1 = d1 << 1
        //            If h2 > 0 Then GoTo nextb
        //exit1:
        //        Catch
        //            MsgBox("COMPLOC" & Hex(locaddr) & "DECLOC:" & Hex(h2))
        //        End Try
        //        'Dim symtPath As String = System.IO.Path.GetTempPath & "yoshimagict\"
        //        'Dim datfn As String = symtPath & "boot.dat"
        //        f.Close()
        //        'FileOpen(1, Form1.OpenFileDialog1.FileName, OpenMode.Binary, OpenAccess.Default, OpenShare.Shared)
        //        Return decdata
        //    End Function
    }
}
