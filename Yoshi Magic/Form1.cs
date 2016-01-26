// compile with: /unsafe
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging; //PixelFormat
using System.Runtime.InteropServices; //Marshal
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;
//Needed
using System.Diagnostics;
using System.Threading;

namespace Yoshi_Magic {
    //public static class global {
    //    public static Bits rom = new Bits();
    //}

    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e) {
            if (System.IO.File.Exists(Properties.Settings.Default.LastRom)) { OpenROM(Properties.Settings.Default.LastRom); } else { OpenROMdialogue(); }
            //loadEntireMap();
        }
        private void OpenROMdialogue() {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Open a GBA/NDS File";
            ofd.Filter = "GBA/NDS/3DS file(*.gba;*.nds;*.3ds;*.cia)|*.gba;*.nds;*.3ds;*.cia";
            if (ofd.ShowDialog() == DialogResult.OK) { OpenROM(ofd.FileName); }
        }
        public static byte[] rom = null;
        public static byte[] ram = null;
        //Public OpenFileDialog1 = ""
        byte[] temp3 =new byte[0x800000];//new byte[0x5E3000];
        unsafe private void OpenROM(string filename) {
            Properties.Settings.Default.LastRom = filename;
            Properties.Settings.Default.Save();
            //string symtPath = System.IO.Path.GetTempPath() + "yoshimagict\\";
            //System.IO.Directory.CreateDirectory(symtPath);
            //TODO: Perhaps delete all ym temp files if directory aleady exists? And/or analyze them?

            //mainMem.setPath(filename)
            string vstr = ""; //Dim vstr As String = ""
            if (filename.EndsWith(".gba", true, null)) { //Load entire ROM to buffer; GBA ROMs are maxed at 32 MB.
                listBox1.Visible = false; textBox1.Visible = false;
                rom = Bits.openFile(filename);
                //rom.seek(0xA0);
                vstr = Bits.getString(rom, 0xA0, 16);
                if (vstr != "MARIO&LUIGIUA88E") { return; }
                ram = new byte[0x40000];
                loadEntireMap();
            } else if (filename.EndsWith(".nds", true, null)) {
                nds a = new nds();
                a.openNDS(filename);
            } else if (filename.EndsWith(".cia", true, null)) { //CIA is good source of unencrypted ROMs(?) (At-least at one site.)
                //M&L:Paper Jam for now.
                //No NCSD? NCCH stuff starts at 00003940.
                //0x3AE0 //exefs offset, size
                byte[] ncch = new byte[0x200];
                using (FileStream a = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)) {
                    a.Seek(0x3940, System.IO.SeekOrigin.Begin); //a.Seek(Bits.getInt32(ncsd, 0x120) << 9, System.IO.SeekOrigin.Begin);
                    a.Read(ncch, 0, 0x200);
                    byte[] temp1 = new byte[Bits.getInt32(ncch, 0x1A4) * 0x200]; //Load from ROM. exefs/.code in uncompressed=max 0x03F00000 bytes
                    //byte[] temp2 = new byte[Bits.getInt32(ncch, 0x1A4) * 0x200]; //Load from Xorpad.
                    //Bits.getInt32(ncch, 0x1A0);//ExeFS Offset
                    a.Seek(0x3940 +( Bits.getInt32(ncch, 0x1A0)) * 0x200, SeekOrigin.Begin);
                    a.Read(temp1, 0, Bits.getInt32(ncch, 0x1A4) * 0x200);
                    //Comp.LZSS_Decompress(temp1, 0x384A18, temp3, 0x5E3000);
                    //41BA00 = exefs size.
                    //6540 = exefs addr.
                    //6740 = .code addr. (66E0 = hashes)
                    //3E6228 = .code size
                    //3EC968 = END
                    Comp.LZSS_Decompress(temp1, 0x3E6228, temp3, 0x800000);
                    //System.IO.File.WriteAllBytes("C:/Users/Tea/Desktop/mlpjexefs.bin", temp3);
                }
            } else if (filename.EndsWith(".3ds", true, null)) { //3DS cartridge sizes range from 1 GB to 8 GB.
                //System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();
                //process.PriorityClass = System.Diagnostics.ProcessPriorityClass.High;

                // Set the current thread to run at 'Highest' Priority
                //Thread thread = System.Threading.Thread.CurrentThread;
                //thread.Priority = ThreadPriority.Highest;

                //System.Diagnostics.Process.Start("explorer", System.IO.Path.GetTempPath() + "yoshimagict\\");
                //Shell("explorer " & System.IO.Path.GetTempPath & "yoshimagict\", AppWinStyle.NormalFocus);
                //DateTime t3, t4, t5;
                //t1 = DateTime.Now;
                //string tempROM = symtPath + "tempROM.3ds";
                //System.IO.File.Delete(tempROM);
                //Bits.fastCopy(filename, tempROM);
                //Bits.CopyFile5(filename, tempROM);
                //t2 = DateTime.Now;
                //System.IO.File.Copy(filename, tempROM);
                //byte[] z = System.IO.File.ReadAllBytes(filename);
                //System.IO.File.WriteAllBytes(tempROM, z);
                
                byte[] ncsd = new byte[0x4000];
                using (FileStream a = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)) {
                    a.Seek(0, SeekOrigin.Begin);
                    a.Read(ncsd, 0, 0x4000);// 0x330);
                    //TODO: (0x1150 = CTR-P-AYMP for EUR)
                    //TODO: CHECK IF ENCRYPTED
                    string romDir = Path.GetDirectoryName(filename) + "\\";
                    string[] xorpad = { romDir + "00040000000D9000.Main.exefs_norm.xorpad", //Might swap this one with next.
                                        romDir + "00040000000D9000.Main.exheader.xorpad",
                                        romDir + "00040000000D9000.Main.romfs.xorpad",
                                        romDir + "00040000000D9000.Manual.romfs.xorpad",
                                        romDir + "00040000000D9000.UpdateData.romfs.xorpad"
                                      };
                    if (!File.Exists(xorpad[0]) || !File.Exists(xorpad[1]) || !File.Exists(xorpad[2])) {
                        MessageBox.Show("As this 3DS ROM is encrypted, xorpads are needed to decrypt it. The first three are required for this editor, but the last two are optional.\n"
                            + xorpad[0] + "\n" + xorpad[1] + "\n" + xorpad[2] + "\n" + xorpad[3] + "\n" + xorpad[4]
                            );
                        return;
                    }
                    byte[] ncch = new byte[0x200];
                    a.Seek(Bits.getInt32(ncsd, 0x120) << 9, System.IO.SeekOrigin.Begin);
                    a.Read(ncch, 0, 0x200);
                    byte[] temp1 = new byte[Bits.getInt32(ncch, 0x1A4) * 0x200]; //Load from ROM. exefs/.code in uncompressed=max 0x03F00000 bytes
                    byte[] temp2 = new byte[Bits.getInt32(ncch, 0x1A4) * 0x200]; //Load from Xorpad.
                    //Bits.getInt32(ncch, 0x1A0);//ExeFS Offset
                    a.Seek((Bits.getInt32(ncsd, 0x120) + Bits.getInt32(ncch, 0x1A0)) * 0x200, SeekOrigin.Begin);
                    a.Read(temp1, 0, Bits.getInt32(ncch, 0x1A4) * 0x200);
                    //t3 = DateTime.Now;
                    using (FileStream b = new FileStream(xorpad[0], FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                        //for (int pos1 = 0; pos1 < Bits.getInt32(ncch, 0x1A4) * 0x200; pos1 += 0x200) {
                        //a.Seek((Bits.getInt32(ncsd, 0x120) + Bits.getInt32(ncch, 0x1A0)) * 0x200 + pos1, System.IO.SeekOrigin.Begin);
                        ////a.Seek(Bits.getInt32(ncsd, 0x120) * 0x200 + pos1, System.IO.SeekOrigin.Begin);
                        //a.Read(temp1, 0, 0x200);
                        b.Seek(0, SeekOrigin.Begin);
                        b.Read(temp2, 0, Bits.getInt32(ncch, 0x1A4) * 0x200);
                        b.Close();
                    }
                    //for (int pos2 = pos1; pos2 < pos1 + 0x200; pos2++) {

                    //for (int pos2 = 0; pos2 < Bits.getInt32(ncch, 0x1A4) * 0x200; pos2++) {
                    //    temp1[pos2] ^= temp2[pos2];
                    //}
                    //t4 = DateTime.Now;
                    //byte[] temp3 = new byte[0x5E3000];
                    fixed (byte* ptemp1 = temp1, ptemp2 = temp2) {
                        byte* pt1 = ptemp1;
                        byte* pt2 = ptemp2;
                        int pos2end = Bits.getInt32(ncch, 0x1A4) * 0x200;
                        for (int pos2 = 0; pos2 < pos2end; pos2 += 8) {
                            //TODO: Check ms time of this versus "safe" method. Show progress information on screen.
                            *((long*)(pt1 + pos2)) ^= *((long*)(pt2 + pos2)); //pos2 += 8; 
                            //*((long*)(pt1 + pos2)) ^= *((long*)(pt2 + pos2));
                        }
                        
                        //fixed (byte* ptemp3 = temp3) {
                        Comp.LZSS_Decompress(temp1, 0x384A18, temp3, 0x5E3000); //0x5E2000); //0x38437C = (U) from
                        //}
                        //File.WriteAllBytes("C:\\Users\\Tea\\Desktop\\exefstestdump(E).bin", temp3);
                    }
                    
                    //Enemy names
                    byte[] enames = new byte[0x1000];//0xFA0
                    byte[] enamesX = new byte[0x1000];//0xFA0
                    //Going straight to the table; skipping the RomFS tree. Not passing GO!, and not collecting $200.
                    //a.Seek(0x25A6B950, SeekOrigin.Begin);
                    a.Seek(0x25C3E840 - 0x940, SeekOrigin.Begin);
                    //a.Read(enames, 0, 0xFA0);// Bits.getInt32(ncch, 0x1A4) * 0x200);
                    a.Read(enames, 0, 0x1000);
                    //a.Close();
                    using (FileStream b = new FileStream(xorpad[2], FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                        //b.Seek(0x25A6B950 - 0x434000, SeekOrigin.Begin); //C290
                        b.Seek(0x25C3E840 - 0x434000 - 0x940, SeekOrigin.Begin);
                        //b.Read(enamesX, 0, 0xFA0); //Bits.getInt32(ncch, 0x1A4) * 0x200);
                        b.Read(enamesX, 0, 0x1000);
                        b.Close();
                    }
                    fixed (byte* fRom = enames, fXor = enamesX) {
                        //byte* pRom = fRom;
                        //byte* pXor = fXor;
                        //0x25A6B950 - 0x434000
                        //  25A6C8F0 ; FA0
                        int pos2end = 0x1000;// 0xFA0;//Bits.getInt32(ncch, 0x1A4) * 0x200;
                        for (int pos2 = 0; pos2 < pos2end; pos2 += 8) {
                            *((long*)(fRom + pos2)) ^= *((long*)(fXor + pos2)); //pos2 += 8; 
                            //*((long*)(pt1 + pos2)) ^= *((long*)(pt2 + pos2));
                        }
                    }
                    int amt = Bits.getInt32(enames, 0);
                    string[] enemyStr = new string[amt];
                    while (amt-- != 0) {
                        enemyStr[amt] = Bits.getString16V(enames, Bits.getInt32(enames, (amt + 1) * 4), 0);
                    }
                    //Enemy Data!
                    StringBuilder str = new StringBuilder(0x200);
                    for (int i = 0; i < 0xB6; i++) {
                        listBox1.Items.Add(enemyStr[Bits.getInt16(temp3, 0x54CBD8 + (i * 0x3C))]);


                        str.Append("ID #" + i.ToString("X2") + " " + enemyStr[Bits.getInt16(temp3, 0x54CBD8 + (i * 0x3C))] + "\r\n");
                        string[] desc = { "Name Index", "??? MSBs of Name Index?", "", "", "", "", "??? Seems to match entry #", "", "", "", "", "", "? (Left); Level (Right)", "HP", "Power", "Defense", "Speed",
                                "", "", "", "Exp", "Coins", "Coin Rate", "Item", "Item Chance", "Rare Item", "Rare Item Chance", "", "", "(Unused?)" };
                        //textBox1.Text = "";
                        for (int j = 0; j < 0x1E; j++) {
                            str.Append(Bits.getInt16(temp3, 0x54CBD8 + ((j * 2) + (i * 0x3C))).ToString("X4") + " = " + desc[j] + "\r\n");
                        }
                        str.Append("\r\n");
                    }
                    textBox1.Text = str.ToString();
                    //listBox1.SelectedIndexChanged += new System.Windows.Forms.selec;
                    //StringBuilder testt = new StringBuilder(0x4000);
                    //for (int i = 0; i < 0xB6; i++) {
                    //    testt.AppendLine(enemyStr[Bits.getInt16(temp3, 0x54CBD8 + (i * 0x3C))]);
                    //}
                    //textBox1.Text = testt.ToString();
                    //listBox1.Items.AddRange(enemyStr);
                    //a.Seek((Bits.getInt32(ncsd, 0x120) + Bits.getInt32(ncch, 0x1A0)) * 0x200 + pos1, System.IO.SeekOrigin.Begin);
                    //a.Write(temp1, 0, 0x200);
                    //}
                    //b.Close();
                    //}
                    //t5 = DateTime.Now;
                    //a.Seek((Bits.getInt32(ncsd, 0x120) + Bits.getInt32(ncch, 0x1A0)) * 0x200, System.IO.SeekOrigin.Begin);
                    //a.Write(temp1, 0, Bits.getInt32(ncch, 0x1A4) * 0x200);
                    a.Close();
                }
                //DateTime t6 = DateTime.Now;//.Ticks;
                //Console.WriteLine("COPY FILE: " + (t2 - t1).ToString() + "\n LOAD EXEFS: " + (t3 - t2).ToString() + "\n LOAD XORPAD: " + (t4 - t3).ToString()
                //    + "\n APPLY XORPAD: " + (t5 - t4).ToString() + "\n WRITE ROM: " + (t6 - t5).ToString() + "\n TOTAL: " + (t6 - t1).ToString());
                //Decryption stuff
                //filename.
            }
            //ElseIf filename.EndsWith(".nds", True, Nothing) Then //DS cartridge sizes range from 8 MB to 512 MB. (0.5 GB)
            //Load 0x200 bytes of header to 0x023FFE00.
            //    mainMem.setBufferSize(&H3FFFFF)
            //    mainMem.openFilePart(0, &H3FFE00, &H200)
            //    mainMem.setPos(&H3FFE00)
            //    vstr = mainMem.getString(16)
            //    'Dim DTCM(&H3FFF) as byte '0x027E0000 in BIS
            //End If
            //Properties.Settings.Default.LastRom = filename;
            //Properties.Settings.Default.Save();
            //' My.Settings.LastRomPath.Add(OpenFileDialog1.FileName)
            //' If My.Settings.LastRomPath.Count > 10 Then My.Settings.LastRomPath.RemoveAt(0)
            //' MsgBox(My.Settings.LastRomPath.ToString())

            //MessageBox.Show(vstr);
            //switch (vstr) {
            //case "MARIO&LUIGIUA88E": //SS(U)
            //    break;
            //case "MARIO&LUIGIPA88P": //SS(E)
            //case "MARIO&LUIGIJA88J": //SS(J)
            //case "M&L DEMO USAB88E": //SS Demo(U)
            //    break;
            //case "MARIO&LUIGI2ARME": //PIT(U)
            //    break;
            //case "MARIO&LUIGI2A58P": //PIT Demo(E)
            //    break;
            //case "MARIO&LUIGI3CLJE": //BIS(U)
            //    break;
            //default:
            //    break;
            //}
            //       Bitmap.p
            //Imaging.PixelFormat.Format16bppRgb555

        }
        private void openToolStripMenuItem_Click(object sender, EventArgs e) {
            OpenROMdialogue();
        }
        private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
            Bits.saveFile(Properties.Settings.Default.LastRom, rom);
        }
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e) {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Save ROM File";
            //sfd.Filter = "GBA/NDS/3DS file(*.gba;*.nds;*.3ds)|*.gba;*.nds;*.3ds";
            if (sfd.ShowDialog() == DialogResult.OK)
                Bits.saveFile(sfd.FileName, rom);
        }

        //Bitmap mapimg = new Bitmap(1, 1);//100, 100, PixelFormat.Format16bppArgb1555);
        //Bitmap mapimg2 = new Bitmap(1, 1);
        Bitmap mapimg3 = new Bitmap(1, 1);
        //private void pictureBox1_Click(object sender, EventArgs e) {
        //int[] t = { 0xFF0000, 0x00FF0000, 0x00FF00, 0x0000FF ,
        //           0xFF0000, 0x00222233, 0x477882, 0x0000FF ,
        //           0xFFFF00, 0x00222233, 0x477882, 0x00FF00, 
        //           0x00FFFF, 0x00FFFFFF, 0xFFFFFF, 0xFF0000};
        //Dim nval As Integer = NumericUpDown1.Value
        //Dim n As Integer = 0
        //'Dim palimg(&H3FFF) As Short
        //Seek(1, &H8C88C8 + &H1E0 * nval + 1)
        //'Seek(1, &H8D49A0 + (&H1E0 * nval) + 1) '***THE ALTERNATE PALETTE DATABASE*** Not sure how it's used.
        //For row = 0 To &H3C00 Step &H400
        //    For col = row To row + &H78 Step &H8 '&H40
        //        FileGet(1, palette1(n))
        //        'After removing all SETPIXEL code, remove below line. b()
        //        'b(n) = Color.FromArgb((palette1(n) And &H1F) << 3, (palette1(n) >> 5 And &H1F) << 3, (palette1(n) >> 10 And &H1F) << 3)
        //        palette1(n) = &H8000S | ((palette1(n) & &H1F) << 10) | (palette1(n) & &H3E0) | (palette1(n) >> 10)
        //        For ypix = col To col + &H380 Step &H80 '&H8
        //            For xpix = ypix To ypix + 7 '16*8
        //                palimg(xpix) = palette1(n)
        //            Next
        //        Next
        //        n += 1
        //    Next
        //Next
        //Map.initPal(0x8C88C8 +( 0x1E0 * 96)); //initPal();
        //LOAD MAP STUFF
        //Map.loadMap();
        //mapimg2 = Map.PixelsToImage(Map.pal, 16, 15, 8);
        // //Map.loadImg();
        // mapimg = Map.dispImg();
        // mapimg3 = Map.dispMap(); // Map.dispTileset();
        // pictureBox1.Refresh();
        //END LOAD MAP STUFF
        //mapimg = PixelsToImage(palimg, 4, 4);
        // Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb,);
        //return;
        private void scriptingTest() {
        /******
         * EVENT CODE TEST
         */ 
        //StringBuilder test = new StringBuilder(0x4000);
        byte[] a = new byte[0x1000]; //Write flag log
        byte[] b = new byte[0x1000]; //Read flag log
        byte[] c = new byte[0x1000]; //Bitlens in args needed. (For comparison w/ actual data.)
        byte[] d = new byte[0x1000];
        //int genaddr = 0x9000000;
        int z = 0x300;
        int addr = 0x21DC54; //282794 = Battle; 2994A4 = Suitcase Events; 299AE0 = End
        int[] evDbRef = { 0x282794, 0x2994A4, 0x299AE0 }; 
        int eN = 0, eN2 = 0;
        for (int eventType = 0; eventType < 3; eventType++) { //0-2 = Field, Battle, Suitcase
            int numOfArgsList = Bits.getInt32(rom, 0x3B98C4 + (eventType << 2)) & 0x1FFFFFF;
            int isMemSetList = Bits.getInt32(rom, 0x3B9704 + (eventType << 2)) & 0x1FFFFFF;
            int argsInfoOffsetList = Bits.getInt32(rom, 0x3BA4A8 + (eventType << 2)) & 0x1FFFFFF;
            int argsbitLenList = Bits.getInt32(rom, 0x3B9D00 + (eventType << 2)) & 0x1FFFFFF;
            //int argsSignedList =  rom.getInt32(0x... + (eventType << 2)) & 0x1FFFFFF;
            int i = evDbRef[eventType];
            while (addr < i) {
                if (0x27fb78 == addr) { addr = 0x2816d1; } //0x0827FB78 = Monster idle stuff... (Starts as data/not scripting.)
                short bitind = 8;
                //Command
                int cmd = rom[addr++]; eN += 1;
                //DEBUG: # OF CMDS FOR EACH USED.
                if (d[(eventType << 8) + cmd] != 255) {
                    d[(eventType << 8) + cmd] += 1;
                }
                if ((cmd >= 0xC0) || (cmd==0)) {
                    Bits.setInt32(d, z, addr - 1); z += 4;
                }
                //test.Append(cmd.ToString("X2"));
                //Var Flags for args.
                int args = rom[numOfArgsList + cmd]; eN2 += args;
                int varFlags = Bits.readbits(rom, ref addr, ref bitind, args);
                //test.Append(cmd.ToString("X2") + " " + varFlags.ToString("X2"));
                //Var Index for memory set.
                int isMemSet = rom[isMemSetList + cmd];
                int memSetInd = 0;
                if (isMemSet == 1) {
                    memSetInd = Bits.readbits(rom, ref addr, ref bitind, 0xD);
                    //test.Append(" " + memSetInd.ToString("X4"));
                    //DEBUG: SET FLAG CODE HERE TEST!!!
                    if ((memSetInd >= 0x3D) && (memSetInd < 0x103D)) {
                        a[(memSetInd - 0x3D) >> 3] |= (byte)(1 << ((memSetInd - 0x3D) & 7));
                        if (((memSetInd - 0x3D) >> 8) == 5) { textBox1.Text += " " + addr.ToString("X8"); }
                    }
                } //else { test.Append(" XXXX"); }
                //Arg bit lengths
                int argsInfoOffset = Bits.getInt16(rom, argsInfoOffsetList + cmd * 2);
                while (args-- > 0) {
                    int argVal = 0;
                    int numOfBits = rom[argsbitLenList + argsInfoOffset++];
                    if ((numOfBits >> 7) == 1) { //32-bit Pointer
                        if (bitind != 8) { bitind = 8; addr += 1; }
                        argVal = Bits.getInt32(rom, addr); addr += 4;
                    } else {
                        argVal = Bits.readbits(rom, ref addr, ref bitind, numOfBits);
                    }
                    //test.Append(" " + argVal.ToString("X8"));
                    ////DEBUG: Earliest pointed to address.
                    //if ((eventType > 0) && ((argVal >> 0x1B) == 1) && (argVal < genaddr)) {
                    //        genaddr = argVal;
                    //}
                    //DEBUG: BITLENS LOG
                    for (byte testbit = c[argsbitLenList + argsInfoOffset - 0x3B98D0 - 1]; testbit < 32; testbit++) {
                        if ((argVal>>testbit) ==1) {
                            c[argsbitLenList + argsInfoOffset - 0x3B98D0 - 1] = (byte)(testbit + 1);
                        }
                    }
                    //DEBUG: READ FLAG CODE HERE TEST!!!
                    if ((varFlags & 1) == 1){
                        if ((argVal >= 0x3D) && (argVal < 0x103D)) {
                            b[(argVal - 0x3D) >> 3] |= (byte)(1 << ((argVal - 0x3D) & 7));
                        }
                    }
                    varFlags >>= 1;
                }
                //test.AppendLine();
                if (bitind != 8) { bitind = 8; addr += 1; } //MAYBE?
            }
        }
        //listBox1.Items.
        //System.IO.File.WriteAllBytes("C:/Users/Tea/Desktop/writtenflags.bin",a);
        //System.IO.File.WriteAllBytes("C:/Users/Tea/Desktop/readflag.bin",b);
        //System.IO.File.WriteAllBytes("C:/Users/Tea/Desktop/argbitlenslog.bin", c);
        //System.IO.File.WriteAllBytes("C:/Users/Tea/Desktop/cmdsused.bin", d);
        //textBox1.Text = "Done!"; //test.ToString();
         //*/
        this.Text = eN.ToString("X8") + "   " + eN2.ToString("X8") + "   ";
        }
        //void initPal() {
        //    rom.seek(0x8C88C8);
        //    short[] pal = new short[0x100];
        //    for (int i = 0; i < 0xF0; i++) {
        //        int palt = rom.getShort();
        //        pal[i] = (short)(((palt & 0x1F) << 10) | (palt & 0x3E0) | (palt >> 10));
        //    }
        //}
        //Bitmap mapimg = new Bitmap(100, 100, PixelFormat.Format32bppPArgb);
        private void pictureBox1_Paint(object sender, PaintEventArgs e) {
            //e.Graphics.DrawImage(mapimg, 0, 0);
            //e.Graphics.DrawImage(mapimg2, 257, 0);
            //e.Graphics.DrawImage(mapimg3, 0, 257)
            e.Graphics.DrawImage(mapimg3, 0, 0);
            //e.Graphics.DrawImage(mapimg2, this.Width - 512, 0);
        }

        private void nextMapToolStripMenuItem_Click(object sender, EventArgs e) {
            if (Map.mapNum == 0x210) { return; }
            Map.mapNum += 1;
            loadEntireMap();
        }

        private void previousMapToolStripMenuItem_Click(object sender, EventArgs e) {
            if (Map.mapNum == 0) { return; }
            Map.mapNum -= 1;
            loadEntireMap();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e) {
            //MessageBox.Show(e.KeyCode.ToString("X"));
            switch (e.KeyCode) {
                case (Keys)0x20: //Spacebar
                    int tmAddr = 0x3B7108 + (rom[0x3A78D4 + (Map.mapNum * 0x18) + 0xA] << 3);
                    tmAddr = Bits.getInt32(rom, tmAddr);
                    if (tmAddr == 0) { return; }
                    tmAddr &= 0x1FFFFFF;
                    //toolStripButton5.Text = rom[0x3A78D4 + (Map.mapNum * 0x18) + 0xA].ToString("X") + "   " + tmAddr.ToString("X") + "   " + Map.tileModCur.ToString("X");
                    if (Map.tileModCur == -1) { Map.tileModCur++; }
                    else if ((rom[tmAddr + (Map.tileModCur++ * 0x14) + 0x13] & 0x80) == 0x80) { Map.tileModCur = -1; } 
                    //Map.tileModCur += 1;
                    //if (Map.tileModCur >= 0) {
                    //    Map.tileModCur = -1;
                    //}
                    //loadEntireMap();
                    mapimg3 = Map.dispMap();
                    pictureBox1.Refresh();
                    break;
                case (Keys)0x21:
                    if (Map.mapNum == 0x210) { return; }
                    Map.mapNum += 1;
                    loadEntireMap();
                    break;
                case (Keys)0x22:
                    if (Map.mapNum == 0) { return; }
                    Map.mapNum -= 1;
                    loadEntireMap();
                    break;
                case (Keys)0x54:
                    Tools a = new Tools();
                    a.init(rom);
                    break;
                default:
                    if (((Keys)0x30 <= e.KeyCode) && (e.KeyCode < (Keys)0x3A)) {
                        if (Map.lastPressedKey == (int)e.KeyCode) {
                            Map.visf ^= 1 << ((int)e.KeyCode - 0x30);
                        }
                        updateToolBox((int)e.KeyCode, 1);
                        //Map.lastPressedKey = (int)e.KeyCode;
                        if ((Map.visf & 0x40) != 0) {
                            timer1.Enabled = true;
                        } else {
                            timer1.Enabled = false;
                        }
                        mapimg3 = Map.dispMap();
                        pictureBox1.Refresh();
                    }
                    break;
                //    Map.mapNum += 10;
                //    break;
            }
            //loadEntireMap();
        }
        void loadEntireMap() {
            this.Text = Map.mapNum.ToString("X4");
            Map.loadMap(); //Init: Load necessary stuff once.
            // DateTime a = DateTime.Now;//.Ticks;
            mapimg3 = Map.dispMap(); //Should be called every frame?
            // DateTime c = DateTime.Now;//.Ticks;
            // Console.WriteLine((c - a).ToString()); //Make this as short as possible? Perferrably 0.017 ... I'm getting 0.025-0.03
            pictureBox1.Width = mapimg3.Width; pictureBox1.Height = mapimg3.Height;
            pictureBox1.Refresh();
            //DateTime c = DateTime.Now;//.Ticks;
            //Console.WriteLine((c - a).ToString()); //Make this as short as possible? Perferrably 0.017 ... I'm getting 0.025-0.03
            updateToolBox(Map.lastPressedKey, 0); //if (ts.Visible == true) { ((PictureBox)ts.Controls[0]).Image = Map.dispTileset(); }
            //Parallel.For(0, 40, row2 => { Console.Write((row2 + " ").ToString()); });
        }
        private void toolStripButton1_Click(object sender, EventArgs e) { //L1
            if (toolStripButton1.Checked == true) { Map.visf ^= 2; loadEntireMap(); }
            toolStripButton1.Checked = true;
            toolStripButton2.Checked = false;
            toolStripButton3.Checked = false;
            updateToolBox(0x31, 1);
        }

        private void toolStripButton2_Click(object sender, EventArgs e) { //L2
            if (toolStripButton2.Checked == true) { Map.visf ^= 4; loadEntireMap(); }
            toolStripButton1.Checked = false;
            toolStripButton2.Checked = true;
            toolStripButton3.Checked = false;
            updateToolBox(0x32, 1);
        }

        private void toolStripButton3_Click(object sender, EventArgs e) { //L3
            if (toolStripButton3.Checked == true) { Map.visf ^= 8; loadEntireMap(); }
            toolStripButton1.Checked = false;
            toolStripButton2.Checked = false;
            toolStripButton3.Checked = true;
            updateToolBox(0x33, 1);
        }
        Form ts = new Form();//null;
        //PictureBox tspicBox1 = new System.Windows.Forms.PictureBox();
        void updateToolBox(int controlSet, int showForm) { //showForm is temp arg???
            //if (ts.Visible == true) { tspicBox1.Image = Map.dispTileset(); }
            if (Map.lastPressedKey != controlSet) {
                ts.Controls.Clear();
                Map.lastPressedKey = controlSet;
            }
            if (ts.IsDisposed || !ts.Visible) {
                if (showForm == 0) { return; }
                ts = new Form();
                //ts.FormBorderStyle = FormBorderStyle.FixedToolWindow;
                ts.FormBorderStyle = FormBorderStyle.SizableToolWindow;
                ts.ClientSize = new Size(512, 769); //ts.Width = 512; ts.Height = 512;
                //ts.StartPosition = FormStartPosition.Manual;// Screen.PrimaryScreen.Bounds.Width - 512; 
                //ts.SetDesktopLocation(Screen.PrimaryScreen.Bounds.Width - 525, 25);
                //ts.BackgroundImageLayout = ImageLayout.None;
                ts.AutoScroll = true; //Adds scrollbars to form.
                //ts.StartPosition
                ts.Show(this); //Setting the owner to "this" form makes windows stay in front of main form, but not in front of other apps like .TopMost.
                ts.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
                
            }
            switch (controlSet) {
                case 0x31:
                case 0x32:
                case 0x33:
                    if (ts.Controls.Count == 0) { //Init
                        PictureBox tspicBox1 = new System.Windows.Forms.PictureBox();
                        tspicBox1.Size = new System.Drawing.Size(512, 512);
                        ts.Controls.Add(tspicBox1);
                        //tsClick += new System.EventHandler(this.tsClick);
                        tspicBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.tsClick);
                        tspicBox1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.tsClickMove);
                        tspicBox1.Paint += new System.Windows.Forms.PaintEventHandler(this.tsPaint);
                    }
                    ((PictureBox)ts.Controls[0]).Image = Map.dispTileset(); //tspicBox1.Image = Map.dispTileset();
                    break;
                case 0x34: //Solidity
                    if (ts.Controls.Count == 0) { //Init
                        PictureBox tspicBox1 = new System.Windows.Forms.PictureBox();
                        tspicBox1.Size = new System.Drawing.Size(4096, 752);
                        ts.Controls.Add(tspicBox1);
                        //tsClick += new System.EventHandler(this.tsClick);
                        tspicBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.tsClick2);
                        tspicBox1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.tsClick2);
                        tspicBox1.Paint += new System.Windows.Forms.PaintEventHandler(this.tsPaint2);
                    }
                    int width = 4096;
                    int[] img = new int[4096 * 752];
                     int solidset = Bits.getInt32(rom, 0x3AADD0 + (rom[0x3A78D4 + (Map.mapNum * 0x18) + 6] * 4)) & 0x1FFFFFF;
                //for (int r = 0; r < height; r += 16) {
                     for (int c = 0; c < width; c += 16) {
                         int solidinfo = Bits.getInt32(rom, solidset); solidset += 4;
                         //int solidinfo = 0;
                         Map.applySolidityTile(img, 4096, c, 752 - 16, solidinfo);
                     }
                    ((PictureBox)ts.Controls[0]).Image = Map.PixelsToImage(img, 4096, 752);
                    break;
            }
        }

        int tsTile = 0;
        int tselwidth = 0;
        int tselheight = 0;
        void tsClick(object sender, MouseEventArgs e) {
            tsTile = ((e.Y >> 4) * 0x20) | ((e.X >> 4) & 0x1F);
            tselwidth = 0; tselheight = 0;
            //MessageBox.Show("X:"+e.X + "   Y:"+e.Y);
            //tspicBox1.Invalidate();
            ((PictureBox)ts.Controls[0]).Invalidate();
        }
        void tsClickMove(object sender, MouseEventArgs e) {
            //tsTile = ((e.Y >> 4) * 0x20) | ((e.X >> 4) & 0x1F);
            //MessageBox.Show("X:"+e.X + "   Y:"+e.Y);
            if (MouseButtons == MouseButtons.Left) {
                tselwidth = ((e.X >> 4) & 0x1F) - (tsTile & 0x1F);
                tselheight = (e.Y >> 4) - (tsTile >> 5);
            }
            //tspicBox1.Invalidate();
            ((PictureBox)ts.Controls[0]).Invalidate();
        }
        void tsPaint(object sender, PaintEventArgs e) {
            e.Graphics.DrawRectangle(new Pen(Color.Red, 2), (tsTile & 0x1F) << 4, (tsTile >> 5) << 4, (tselwidth<<4)+15, (tselheight<<4)+15);
        }
        int tsTile2 = 0;
        void tsClick2(object sender, MouseEventArgs e) {
            if (MouseButtons == MouseButtons.Left) {
                tsTile2 = (e.X >> 4); //MessageBox.Show("X:"+e.X + "   Y:"+e.Y);
                if (tsTile2 < 0) { tsTile2 = 0; } else if (tsTile2 > 0xFF) { tsTile2 = 0xFF; } //Bounds checking.
                ((PictureBox)ts.Controls[0]).Invalidate();
                int solidset = Bits.getInt32(rom, 0x3AADD0 + (rom[0x3A78D4 + (Map.mapNum * 0x18) + 6] * 4)) & 0x1FFFFFF;
                int solidinfo = Bits.getInt32(rom, solidset + (tsTile2 * 4));
                ts.Text = solidinfo.ToString("X8");
            }
        }
        void tsPaint2(object sender, PaintEventArgs e) {
            e.Graphics.DrawRectangle(new Pen(Color.Red, 2), tsTile2 << 4, 0, 15, 752 - 1);
        }
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e) {
            //picPlaceTile(e.X, e.Y);
            picPlaceMassTiles(e.X, e.Y);   
        }
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e) {
            if (MouseButtons == MouseButtons.Left) {
                //if ((e.X >= 0) && (e.X < pictureBox1.Width) && (e.Y >= 0) && (e.Y < pictureBox1.Height)) {
                    //picPlaceTile(e.X, e.Y);
                    picPlaceMassTiles(e.X, e.Y);
                //}
            }
        }
        void picPlaceMassTiles(int x, int y) {
            if (Map.lastPressedKey == 0x34) {
                if (!((x >= 0) && (x < pictureBox1.Width) && (y >= 0) && (y < pictureBox1.Height))) { return; }
                int solidloc = 0x8E08E0 + Bits.getInt32(rom, 0x8E08E0 + (Bits.getInt16(rom, 0x3AAE08 + (Map.mapNum * 8) + 6) * 4));
                if (MouseButtons == MouseButtons.Left) { //Set
                    rom[solidloc + (y >> 4) * (pictureBox1.Width >> 4) + (x >> 4)] = (byte)tsTile2;
                    loadEntireMap();
                } else if (MouseButtons == MouseButtons.Right) { //Get
                    tsTile2 = rom[solidloc + (y >> 4) * (pictureBox1.Width >> 4) + (x >> 4)];
                    ((PictureBox)ts.Controls[0]).Invalidate();
                }
                return;
            }
            int reserve = tsTile;
            for (int r = 0; r <= tselheight; r++) {
                for (int c = 0; c <= tselwidth; c++) {
                    tsTile = reserve + (r * 0x20) + c;
                    picPlaceTile(x + (c << 4), y + (r << 4));
                }
            }
            tsTile = reserve;
            loadEntireMap();
        }
        private void picPlaceTile(int x, int y) {
            if (!((x >= 0) && (x < pictureBox1.Width) && (y >= 0) && (y < pictureBox1.Height))) { return; }
            //if (e.Button == System.Windows.Forms.MouseButtons.Left) {
            //    MessageBox.Show("X:" + e.X + "   Y:" + e.Y);
            //}
            int laysel = -1;
            //if (toolStripButton1.Checked) { laysel = 0; }
            //if (toolStripButton2.Checked) { laysel = 2; }
            //if (toolStripButton3.Checked) { laysel = 4; }
            if (Map.lastPressedKey == 0x31) { laysel = 0; }
            if (Map.lastPressedKey == 0x32) { laysel = 2; }
            if (Map.lastPressedKey == 0x33) { laysel = 4; }
            if (laysel == -1) { return; }
            int layi = Bits.getInt16(rom, 0x3AAE08 + (Map.mapNum * 8) + laysel);
            if (layi != 0xFFFF) { // null; }
                byte[] mdl1 = rom;
                int a = 0x754D74 + Bits.getInt32(rom, 0x754D74 + (layi * 4));
                //a += 2 + (mdl1[a] * 15) * (e.Y >> 4); //WIP
                a += 2 + ((mdl1[a] * 20) - ((mdl1[a] >> 2) * 5)) * (y >> 4); //WIP
                // a += 2 + ((mdl1[a] * 19) + mdl1[a]) * (e.Y >> 4); //WIP
                a += (x >> 6) * 5;
                mdl1[a] &= (byte)(0xFF3F >> (((x >> 4) & 3) * 2));
                mdl1[a] |= (byte)((tsTile & 0x300) >> (2 + (((x >> 4) & 3) * 2)));
                a += 1 + ((x >> 4) & 3);
                mdl1[a] = (byte)(tsTile);
                //loadEntireMap();
                //If (NumericUpDown12.Value + h) = 0 Then
                //       theloc = Loc(1) + 1 + ((NumericUpDown11.Value + w) >> 2) * 5
                //       FileGet(1, stem, theloc)
                //   Else
                //       theloc = CInt(((Loc(1) + (NumericUpDown12.Value + h) * (((scnw * 15) >> 2) + 1) * 5 + 3) And &HFFFCS) + ((NumericUpDown11.Value + w) >> 2) * 5) - 1
                //       FileGet(1, stem, theloc)
                //   End If
                //   'stem = (stem And (&HFF Xor (&H3 << (Math.Abs((NumericUpDown11.Value And &H3) - 3) << 1)))) + _
                //   '(((NumericUpDown10.Value >> 3) And &H3) << (Math.Abs((NumericUpDown11.Value And &H3) - 3) << 1))
                //   'NumericUpDown9 'Tileset X
                //   'NumericUpDown10 'Tileset Y
                //   'NumericUpDown11 'Map X
                //   'NumericUpDown12 'Map Y
                //   stem = (stem And (Not (&H3 << (Math.Abs(((NumericUpDown11.Value + w) And &H3) - 3) << 1)))) + _
                //           ((((NumericUpDown10.Value + h) >> 3) And &H3) << (Math.Abs(((NumericUpDown11.Value + w) And &H3) - 3) << 1))
                //   'stem = (stem >> (Math.Abs(((NumericUpDown10.Value >> 3) And &H3) - 3) << 1)) And 3
                //   FilePut(1, stem, Loc(1))
                //   Dim leaf As Byte
                //   Seek(1, Loc(1) + 1 + ((NumericUpDown11.Value + w) And &H3))
                //   leaf = (((NumericUpDown10.Value + h) And &H7) << 5) Or ((NumericUpDown9.Value + w) And &H1F)
                //   FilePut(1, leaf)

            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e) {
            string[] desc = { "Name Index", "??? MSBs of Name Index?", "", "", "", "", "??? Seems to match entry #", "", "", "", "", "", "? (Left); Level (Right)", "HP", "Power", "Defense", "Speed",
                                "", "", "", "Exp", "Coins", "Coin Rate", "Item", "Item Chance", "Rare Item", "Rare Item Chance", "", "", "(Unused?)" };
            textBox1.Text = "";
            for (int i = 0; i < 0x1E; i++) {
                textBox1.Text += Bits.getInt16(temp3, 0x54CBD8 + ((i * 2) + (listBox1.SelectedIndex * 0x3C))).ToString("X4") + " = " + desc[i] + "\r\n";
            }
        }
        private void toolStripButton4_Click(object sender, EventArgs e) {
            Map.visf ^= 0x10;
            loadEntireMap();
        }

        private void toolStripButton5_Click(object sender, EventArgs e) {
            //0821DC48 = "SQUAREENIX"
            //0821DC54 = Field Events (Note: NPC scripts (Ex: Mario, Luigi) that do nothing are 0821F808)
            //08282794 = Battle AI
            //082994A4 = Suitcase Events
            //08299AE0 = Unknown, but likely not event scripts.
            //int eN = 0; //# of cmds/entries needed... but for information purposes only?
            ////int eN2 = 0; //32-bits needed... but for information purposes only?
            //int addr0 = 0x21DC54;
            ////Start loop?
            //int cmd = rom[addr0++]; eN += 1;
            //int args = rom[(Bits.getInt32(rom, 0x3B98C4 + (0 << 2)) & 0x1FFFFFF) + cmd];
            //int bitcount = args;
            //scriptingTest();
            //timer1.Enabled = !timer1.Enabled;
            Map.visf ^= 0x40;
            if ((Map.visf & 0x40) != 0) {
                timer1.Enabled = true;
            } else {
                timer1.Enabled = false;
            }
        }
        int fps = 0;
        int csecp = 0;

    //Dim frmnum(15) As Byte
    //Dim frmcnt(15) As Byte
    //Private Sub Timer1_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer1.Tick
    //    ''FPS - For the Gameboy Advance:  "The refresh rate is just shy of 60 frames per second (59.73 Hz)." ; About 0.01666..7 secs. per frame.
    //    Dim csec As Integer = TimeOfDay.Second
    //    If csec = csecp Then
    //        fps += 1
    //    Else
    //        CheckBox5.Text = "fps: " & fps
    //        'If fps >= 60 Then CheckBox5.ForeColor = Color.DarkGreen Else CheckBox5.ForeColor = Color.Black
    //        fps = 0 : csecp = csec
    //    End If
        private void timer1_Tick(object sender, EventArgs e) {
            //timer1.Enabled = false;
            //MainLoop();
            //return;
            int csec = DateTime.Now.Second;
            if (csec == csecp) {
                fps++;
            } else {
                toolStripButton5.Text = "fps: " + fps;
                fps = 0; csecp = csec;
            }

            //Tile animation data!
            //0x3B283C + rom[
            Map.tileAniNext();
            mapimg3 = Map.dispMap(); //Should be called every frame?
            //DateTime c = DateTime.Now;//.Ticks;
            //Console.WriteLine((c - a).ToString()); //Make this as short as possible? Perferrably 0.017 ... I'm getting 0.025-0.03
            //pictureBox1.Width = mapimg3.Width; pictureBox1.Height = mapimg3.Height;
            pictureBox1.Refresh();
        }
        //private void MainLoop() { //Better contro over fps? (Maybe wait unti 61+ fps for most of animating?)
        //    int FPS = 60;
        //    long ticks1 = 0; //Time of last frame.
        //    long ticks2 = 0; //Current tick beng checked.
        //    long ticks3 = 0; //Time of current second.
        //    int interval = (int)Stopwatch.Frequency / FPS;
        //    //this.BackgroundImage = AnimatedForm.Properties.Resources.blockanimation as Bitmap;
        //    //this.Region = BmpToRegion.Convert(this.BackgroundImage);

        //    //GifImage gif = new GifImage(this.BackgroundImage);
        //    //gif.ReverseAtEnd = true;
        //    ticks3 = Stopwatch.GetTimestamp();
        //    while (!this.IsDisposed && ((Map.visf & 0x40) != 0)) {
        //        Application.DoEvents();

        //        ticks2 = Stopwatch.GetTimestamp();
        //        if (ticks2 >= ticks1 + interval) {
        //            ticks1 = Stopwatch.GetTimestamp();

        //            //Actions
        //            //this.BackgroundImage = gif.GetNextFrame();
        //            //this.Region = BmpToRegion.Convert(this.BackgroundImage);
        //            //
        //            Map.tileAniNext();
        //            mapimg3 = Map.dispMap();
        //            pictureBox1.Refresh();
        //            if (ticks1 <= (ticks3 + Stopwatch.Frequency)) {
        //                fps++;
        //            } else {
        //                if (fps != 0)
        //                    toolStripButton5.Text = "fps: " + (fps).ToString();
        //                else
        //                    toolStripButton5.Text = "fps: ?";
        //                //this.Invalidate(); //refreshes the form
        //                fps = 0;
        //                ticks3 = Stopwatch.GetTimestamp();
        //            }
        //        }// else { fps++; }

        //        Thread.Sleep(1); //frees up the cpu
        //    }
        //}
    }
}
