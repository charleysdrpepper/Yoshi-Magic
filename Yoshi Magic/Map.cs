using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing; //Bitmap/Rectangle
using System.Drawing.Imaging; //PixelFormat
using System.Runtime.InteropServices; //Marshal

using System.Windows.Forms; //Message Boxes
namespace Yoshi_Magic {
   static class Map {
       static byte[] rom = Form1.rom;
       static byte[] ram = Form1.ram;
      //static byte[] ramB = Form1.ram.buffer;
       public static int[] pal = new int[0x100];
        public static void initPal(int palos) { //Loads palette data into 32-bit array with reversed format.
            //rom.seek(0x8C88C8);
            for (int i = 0; i < 0xF0; i++) {
                int palt = Bits.getInt16(rom, palos); palos += 2; //rom.getShort(ref palos);
                //pal[i] = (short)(((palt & 0x1F) << 10) | (palt & 0x3E0) | (palt >> 10));
                pal[i] = unchecked((int)0xFF000000) | ((palt & 0x1F) << 0x13) | ((palt & 0x3E0) << 6) | ((palt >> 7) & 0xF8);
            }
        }
        
       //Only needed to display zoomed in Palette bitmap,
        public static Bitmap PixelsToImage(int[] array, int width, int height, int zoom) {
            if (zoom <= 0) { return new Bitmap(1, 1); }
            int[] array2 = new int[width*height*zoom*zoom];

            //for (int i = 0; i < (width * height); i++) {
            int i = 0;
            for (int yt = 0; yt < (height * zoom) * (width * zoom); yt += (width * zoom)* zoom) {
                for (int xt = yt; xt < yt + (width * zoom); xt += zoom) {
                    int palt = array[i++];
                    for (int y = xt; y < xt + (width * zoom)* zoom; y+=(width*zoom)) {
                        for (int x = y; x < y + zoom; x++) {
                            array2[x] = palt;
                        }
                    }
                }
            }
            return PixelsToImage(array2, width*zoom, height*zoom);
        }
        public static Bitmap PixelsToImage(int[] array, int width, int height) {
            //Parallel.
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppRgb);
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
            IntPtr pNative = bitmapData.Scan0;
            Marshal.Copy(array, 0, pNative, width * height);
            bitmap.UnlockBits(bitmapData);
            return bitmap;
            //FYI: This method works too, but seems slower:
            //var gchPixels = GCHandle.Alloc(array, GCHandleType.Pinned);
            //var bitmap = new Bitmap(width, height, width * sizeof(uint),
            //                        PixelFormat.Format32bppRgb,
            //                        gchPixels.AddrOfPinnedObject());
            //gchPixels.Free();
            //return bitmap;
        }

       //Compressed Image display
    //       Sub cimage(ByVal nud As NumericUpDown, ByVal n As Byte)
    //    'UPDATE A COMPRESSED IMAGE (Decompress if needed > Display Image)
    //    'bm = New Bitmap(16 * 8 * 2, 16 * 4) '(20, 23)
    //    Dim numud2val As Byte = nud.Value 'NumericUpDown2.Value
    //    If numud2val <> &HFF Then
    //        'If decompress = True Then
    //        'Dim offset1 As Integer
    //        FileGet(1, offset1, &H6527F4 + (CInt(numud2val) << 2) + 1) 'Maps
    //        'FileGet(1, offset1, &HA57994 + (CInt(numud2val) << 2) + 1) 'Suitcase
    //        'FileGet(1, offset1, &H9F808C + (CInt(numud2val) << 2) + 1) 'Battle Menus
    //        'FileGet(1, offset1, &H9FC058 + (CInt(numud2val) << 2) + 1) 'Battle BGs
    //        'offset1 = &H6527F4 + offset1
    //        Try
    //            Seek(1, &H6527F4 + offset1 + 1)
    //            'Seek(1, &HA57994 + offset1 + 1)
    //            'Seek(1, &H9F808C + offset1 + 1)
    //            'Seek(1, &H9FC058 + offset1 + 1)
    //            'Array.Clear(data2, 0, &H6000)
    //            num = &H2000 * n 'Offset to put data in variable data2 for sub decomp.
    //            decomp()
    //            nud.BackColor = Color.White
    //        Catch
    //            nud.BackColor = Color.Red
    //        End Try
    //        'End If
    //        'disimg(bm, n)
    //        'pb.Image = bm
    //        'disimg(n)
    //    Else
    //        nud.BackColor = Color.White
    //        Array.Clear(data2, &H2000 * n, &H2000)
    //        'pb.Image = Nothing
    //    End If....

    //    disimg(n)
    //End Sub
        public static int mapNum = 0;//0x1A4; //0x1CE; //0x110; //0;//0x196;
        public static int tileModCur = -1;
        public static int lastPressedKey = 0;
        public static int visf = 0xE; //Visibility Flags
        public static void loadMap() {
            int mapMain = 0x3A78D4 + (mapNum * 0x18);
            mapMain+=3; //todo
            //int grpImgs = 0x3AAA6C + (rom.getInt8(mapMain++) * 4);
            //rom.getInt8(grpImgs++);
            loadImg(0x3AAA6C + (rom[mapMain++] * 4));
            loadTileset(0x3AAC4C + (rom[mapMain++] * 4));
            //Tile Mods Tileset
            int tmAddr = 0x3B7108 + (rom[0x3A78D4 + (Map.mapNum * 0x18) + 0xA] << 3);
            tsetaddr[2] = 0x9744D0 + Bits.getInt32(rom, 0x9744D0 + (Bits.getInt16(rom, tmAddr + 4) << 2));
            //mapMain++;
            loadPal(0x3AAD68 + (rom[mapMain++]));

            //Counters for Animated Tiles
            frmcnt = new byte[16];
            frmnum = new byte[16];
            Map.tileModCur = -1;
        }
       //static int tableLookup(int addr, int index) {
       //    return addr + rom.getInt32(addr + (index * 4));
       //}
        public static void loadImg(int addr) {
            int FImgsTable = 0x6527F4;// 865274;
            int imgNum = rom[addr++];
            if (imgNum != 0xFF) {
                Comp.mlssdecd(rom, FImgsTable + Bits.getInt32(rom, FImgsTable + (imgNum * 4)), ram, 0);
            }
            imgNum = rom[addr++];
            if (imgNum != 0xFF) {
                Comp.mlssdecd(rom, FImgsTable + Bits.getInt32(rom, FImgsTable + (imgNum * 4)), ram, 0x2000);
            }
            imgNum = rom[addr++];
            if (imgNum != 0xFF) {
                Comp.mlssdecd(rom, FImgsTable + Bits.getInt32(rom, FImgsTable + (imgNum * 4)), ram, 0x4000);
            }
        }
       static int[] tsetaddr = new int[3];// = {0,0,0};
        public static void loadTileset(int addr) {
           //0x6FFC20 + (NumericUpDown8.Value << 2)
            int FTsetsTable = 0x6FFC20;
            tsetaddr[0] = -1;
            int num = Bits.getInt16(rom, addr); addr += 2;
            if (num != 0xFFFF) {
                //tsetaddr[0] = rom.getInt32(FTsetsTable + rom.getInt32(FTsetsTable + (num * 4)));
                tsetaddr[0] = FTsetsTable + Bits.getInt32(rom, FTsetsTable + (num * 4));
            }
            tsetaddr[1] = -1;
            num = Bits.getInt16(rom, addr);
            if (num != 0xFFFF) {
                tsetaddr[1] = FTsetsTable + Bits.getInt32(rom, FTsetsTable + (num * 4));
            }
        }
        public static void loadPal(int addr) {
            int num = rom[addr];//num=96;
            int palos = 0x8C88C8;
            palos += 0x1E0 * num;
            initPal(palos);
        }
        public static Bitmap dispImg() { //static void dispImg() {
            //Re-arranges pixels from tiled to non-tiled for image displaying.
            int pos = 0;
            int[] bmpdata = new  int[0xC000];
            //for (int row = 0; row <= 0x38; row += 8) {
            for (int row = 0; row <= 0xB8; row += 8) {
                for (int col = 0; col <= 0xF8; col += 8) {
                    for (int y = row; y <= row + 7; y++) {
                        int pix = Bits.getInt32(ram, pos); pos += 4;
                        for (int x = col; x <= col + 7; x++) {
                            bmpdata[(y << 8) + x] = pal[pix & 0xF];
                            pix >>= 4;
                        }
                    }
                }
            }
            //return PixelsToImage(bmpdata, 0x100, 0x40);
            return PixelsToImage(bmpdata, 0x100, 0xC0);
        }
        //public static Bitmap PixelsToImage2(short[] array, int width, int height) {
        //    Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format4bppIndexed); //Format16bppArgb1555); //Format32bppRgb);
        //    Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        //    BitmapData bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
        //    IntPtr pNative = bitmapData.Scan0;
        //    Marshal.Copy(array, 0, pNative, width * height);
        //    bitmap.UnlockBits(bitmapData);
        //    return bitmap;
        //}
        public static Bitmap dispTileset() {//(ByVal tsslot As Integer) {
             //tsetaddr[0]
            int tspos = 0;
            //int[] bmpdata = new int[0x20000];
            int[] bmpdata = new int[0x40000];
            //for (int row = 0; row < 16; row++) {
            //    for (int col = 0; col < 32; col++) {
            for (int row = 0; row < 0x200; row+=16) {
                for (int col = 0; col < 0x200; col+=16) {
                    for (int r = row; r < row + 16; r += 8) {
                        for (int c = col; c < col + 16; c += 8) {
                            //int tile = rom.getInt16(tsetaddr[0] + tspos); tspos += 2;
                            int tile;
                            if (tsetaddr[tspos >> 12] == -1) { tile = 0; } else { tile = Bits.getInt16(rom, tsetaddr[tspos >> 12] + (tspos & 0xFFF)); tspos += 2; } //tspos &= 0xFFF;
                            int pos = (tile & 0x3FF) << 5;
                            int tilePal = (tile >> 8) & 0xF0;
                            //int x1, x2, xi, y1, y2, yi; // Not sure how I wanted flipping coded in?  Alternative below, though!
                            //if ((tile & 0x400) == 0) { x1 = 0; x2 = 7; xi = 1; } else { x1 = 7; x2 = 0; xi = -1; };
                            //if ((tile & 0x800) == 0) { y1 = 0; y2 = 7; yi = 1; } else { y1 = 7; y2 = 0; yi = -1; };
                            for (int y = r; y <= r + 7; y++) {
                                int ry = y; if ((tile & 0x800) != 0) { ry = r + ((7 - y) & 7); } //Vert. flip
                                int pix = Bits.getInt32(ram, pos); pos += 4;
                                for (int x = c; x <= c + 7; x++) {
                                    int rx = x; if ((tile & 0x400) != 0) { rx = c + ((7 - x) & 7); } //Horr. flip
                                    bmpdata[(ry  << 9)  + rx] = pal[tilePal | (pix & 0xF)];
                                    pix >>= 4;
                                }
                            }
                        }
                    }
                }
            }
            return PixelsToImage(bmpdata, 0x200, 0x200);// 0x100);
        }
        //public static int loadSolid = 0;
        public static Bitmap dispMap() {
            byte[] mdl1 = rom;
            int width = 0, height = 0;
            for (int mapl = 0; mapl < 6; mapl+=2) {
                int layi = Bits.getInt16(rom, 0x3AAE08 + (mapNum * 8) + mapl);
                if (layi != 0xFFFF) { // null; }
                    int a = 0x754D74 + Bits.getInt32(rom, 0x754D74 + (layi * 4));
                    if (width < mdl1[a]) { width = mdl1[a]; }
                    if (height < mdl1[a+1]) { height = mdl1[a+1]; }
                }
            }
            width *= 240; height *= 160;
            int[] bmpdata = new int[width * height];
           // priomap = new byte[width * height];
             genPixPosTabel(width);
            //for (int i = 0; i < (width * height); i++) { //Backdrop
            //    bmpdata[i] = pal[0];
            //}
            //switch (rom[0x3A78D4 + (mapNum * 0x18) + 2] & 0x60) {
            //    case 0:
            //    case 0x60: //Unused?
            //        addLayer(bmpdata, 0, width);//, 3);
            //        addLayer(bmpdata, 2, width);//, 2);
            //        addLayer(bmpdata, 4, width);//, 1);
            //        break;
            //    case 0x20:
            //        addLayer(bmpdata, 2, width);//, 3);
            //        addLayer(bmpdata, 0, width);//, 2);
            //        addLayer(bmpdata, 4, width);//, 1);
            //        break;
            //    case 0x40:
            //        addLayer(bmpdata, 2, width);//, 3);
            //        addLayer(bmpdata, 4, width);//, 2);
            //        addLayer(bmpdata, 0, width);//, 1);
            //        break;
            //}

            //8009 = Item that pops out of item blocks sprite.
             //for (int i = 0; i < 10; i++) {
             //    int x = 0x20, y = 0x20;
             //    loadSprite(bmpdata,0x8009, i, x + (i<<4), y, width);
             //    //x += 0x20;
             //}
            //PREPARE BLENDING INFO!
             blendflag = new byte[(width * height)>>3];
            int bldaddr = 0x3B78AC + (rom[0x3A78D4 + (mapNum * 0x18) + 0x9] << 3);
            bldmod = Bits.getInt32(rom, bldaddr);
            bldmod = ((bldmod & 0xFC0) << 2) | (bldmod & 0x3F);
            int evaddr = Bits.getInt32(rom, bldaddr + 4);
            if (evaddr != 0) {
                bldmod |= 0x40;
                int evdata = Bits.getInt32(rom, evaddr & 0x1FFFFFF);
                eva = evdata & 0x1F;
                evb = (evdata >> 5) & 0x1F;
            }
             switch (rom[0x3A78D4 + (mapNum * 0x18) + 2] & 0x60) {
                 case 0:
                 case 0x60: //Unused?
                     addSprites(bmpdata, width, 1);
                     addLayer(bmpdata, 4, width);//, 3);
                     addSprites(bmpdata, width, 2);
                     addLayer(bmpdata, 2, width);//, 2);
                     addSprites(bmpdata, width, 3);
                     addLayer(bmpdata, 0, width);//, 1);
                     break;
                 case 0x20:
                     addSprites(bmpdata, width, 1);
                     addLayer(bmpdata, 4, width);//, 3);
                     addSprites(bmpdata, width, 2);
                     addLayer(bmpdata, 0, width);//, 2);
                     addSprites(bmpdata, width, 3);
                     addLayer(bmpdata, 2, width);//, 1);
                     break;
                 case 0x40:
                     addSprites(bmpdata, width, 1);
                     addLayer(bmpdata, 0, width);//, 3);
                     addSprites(bmpdata, width, 2);
                     addLayer(bmpdata, 4, width);//, 2);
                     addSprites(bmpdata, width, 3);
                     addLayer(bmpdata, 2, width);//, 1);
                     break;
             }
            for (int i = 0; i < (width * height); i++) { //Backdrop
                int mappos = i, layiOffset = 0x20;
                if (((blendflag[mappos >> 3] >> (mappos & 7)) & 1) == 1) {
                    //int blue = (byte)bmpdata[mappos];
                    if ((bldmod & (0xC0 | (layiOffset << 8))) == (0x40 | (layiOffset << 8))) {
                        bmpdata[mappos] = blendit(bmpdata[mappos], pal[0], eva, evb);
                    }
                    blendflag[mappos >> 3] ^= (byte)(1 << (mappos & 7));
                } else if (bmpdata[mappos] == 0) {
                    bmpdata[mappos] = pal[0];
                    //priomap[(ry * bitmapWidth) + rx] = priority;
                    //if ((bldmod & 0xC0) == 0x40) {
                    if ((bldmod & (0xC0 | layiOffset)) == (0x40 | layiOffset)) {
                        blendflag[mappos >> 3] |= (byte)(1 << (mappos & 7));
                    }
                }
                //bmpdata[i] = pal[0];
            }
            //Solidity Map
            if ((visf & 0x10) != 0) {
                int[] bmpdata2 = new int[width * height];
                int solidloc = 0x8E08E0 + Bits.getInt32(rom, 0x8E08E0 + (Bits.getInt16(rom, 0x3AAE08 + (mapNum * 8) + 6) * 4));
                int solidset = Bits.getInt32(rom, 0x3AADD0 + (rom[0x3A78D4 + (mapNum * 0x18) + 6] * 4)) & 0x1FFFFFF;
                for (int r = 0; r < height; r += 16) {
                    for (int c = 0; c < width; c += 16) {
                        int solidinfo = Bits.getInt32(rom, solidset + (rom[solidloc++] * 4));
                        applySolidityTile(bmpdata2, width, c, r, solidinfo);
                    }
                }
                for (int i = 0; i < (width*height) ; i++) {
                    bmpdata[i] = blendit(bmpdata[i], bmpdata2[i], 8, 16);
                }
            }
            //Warps
            if ((visf & 0x100) != 0) {
                int wptr = Bits.getInt32(rom, 0x3AF418 + Map.mapNum * 4) & 0x1FFFFFF; //0xA52A2A //Brown
                do {
                    //Area
                    //for (int b = rom[wptr + 1] * 16; b < rom[wptr + 3] * 16 + 15; b++) {
                    //    for (int a = rom[wptr] * 16; a < rom[wptr + 2] * 16 + 15; a++) {
                    //        bmpdata[b * width + a] = 0xA52A2A;
                    //    }
                    //}
                    //Perimeter
                    try {
                        if (rom[wptr + 0] * 16 < width) {
                            int c = rom[wptr + 3] * 16 + 15; if ((bmpdata.Length/width) < c) { c = bmpdata.Length/width; }
                            for (int b = rom[wptr + 1] * 16; b < c; b++) {
                                bmpdata[b * width + (rom[wptr] * 16)] = 0xA52A2A;
                                bmpdata[b * width + (rom[wptr] * 16 + 1)] = 0xA52A2A;
                            }
                        }
                        if (rom[wptr + 1] * 16 * width < bmpdata.Length) {
                            int c = rom[wptr + 2] * 16 + 15; if (width < c) { c = width; }
                            for (int a = rom[wptr] * 16; a < rom[wptr + 2] * 16 + 15; a++) {
                                bmpdata[(rom[wptr + 1] * 16) * width + a] = 0xA52A2A;
                                bmpdata[(rom[wptr + 1] * 16 + 1) * width + a] = 0xA52A2A;
                            }
                        }
                        if (rom[wptr + 2] * 16 < width) {
                            int c = rom[wptr + 3] * 16 + 15; if ((bmpdata.Length / width) < c) { c = bmpdata.Length / width; }
                            for (int b = rom[wptr + 1] * 16; b < rom[wptr + 3] * 16 + 15; b++) {
                                bmpdata[b * width + (rom[wptr + 2] * 16 + 14)] = 0xA52A2A;
                                bmpdata[b * width + (rom[wptr + 2] * 16 + 15)] = 0xA52A2A;
                            }
                        }
                        if (rom[wptr + 3] * 16 * width < bmpdata.Length) {
                            int c = rom[wptr + 2] * 16 + 15; if (width < c) { c = width; }
                            for (int a = rom[wptr] * 16; a < rom[wptr + 2] * 16 + 15; a++) {
                                bmpdata[(rom[wptr + 3] * 16 + 14) * width + a] = 0xA52A2A;
                                bmpdata[(rom[wptr + 3] * 16 + 15) * width + a] = 0xA52A2A;
                            }
                        }
                        
                        //int a = rom[wptr] * 16;
                        //for (int a = rom[wptr] * 16; a < rom[wptr + 2] * 16 + 15; a++) {
                        //    bmpdata[(rom[wptr + 1] * 16) * width + a] = 0xA52A2A;
                        //    bmpdata[(rom[wptr + 1] * 16 + 1) * width + a] = 0xA52A2A;
                        //    bmpdata[(rom[wptr + 3] * 16 + 14) * width + a] = 0xA52A2A;
                        //    bmpdata[(rom[wptr + 3] * 16 + 15) * width + a] = 0xA52A2A;
                        //}
                        //for (int b = rom[wptr + 1] * 16; b < rom[wptr + 3] * 16 + 15; b++) {
                        //    bmpdata[b * width + (rom[wptr] * 16)] = 0xA52A2A;
                        //    bmpdata[b * width + (rom[wptr] * 16 + 1)] = 0xA52A2A;
                        //    bmpdata[b * width + (rom[wptr + 2] * 16 + 14)] = 0xA52A2A;
                        //    bmpdata[b * width + (rom[wptr + 2] * 16 + 15)] = 0xA52A2A;
                        //}
                    } catch { }
                    if ((rom[wptr + 0xA] & 2) != 0) { break; }
                    wptr += 0xC;
                } while (true);
            }
            return PixelsToImage(bmpdata, width, height);
        }
        public static void applySolidityTile(int[] bmpdata2, int width, int c, int r, int solidinfo) {
            //Wall
            int shgt = (((solidinfo >> 8) & 0xF) << ((solidinfo >> 0x1F) & 1)) * 8;
            if ((solidinfo & 0x80) == 0x80) { shgt = 0; }
            for (int y = r - shgt; y < r + 16; y++) {
                if (y < 0) { y = 0; }
                for (int x = c; x < c + 16; x++) {
                    bmpdata2[(y * width) + x] = 0x800000;// +(((r + 16 - y) >> 4 << 1) * 0x010202);
                    //bmpdata2[(y * width) + x] = blendit(bmpdata2[(y * width) + x], 0x800000, 8, 8);
                }
            }
            //Solid floor
            int solidh = 0x008000;
            if ((solidinfo & 0x4) == 4) { solidh = 0x808000; }
            if ((solidinfo & 0x80) == 0x80) { solidh = 0x80; }
            //if ((solidinfo & 0x78) != 0) { solidh = (solidinfo & 0xF) * 0x101010; }
            //if ((solidinfo & 0x78) == 9) { solidh = 0xFFFFFF; }
            //if ((solidinfo & 0x78) >= 0x50) { solidh = 0xFFFFFF; }
            //if ((solidinfo & 0x78) == 0x20) { solidh = 0x00FFFF; }
            //if ((solidinfo & 0x78) == 0x40) { solidh = 0xFFFF00; }
            //if (((solidinfo & 0x78) != 0x0) && //Nothing special.
            //    ((solidinfo & 0x78) != 0x08) &&//Metal floor.
            //    ((solidinfo & 0x78) != 0x18) &&
            //    ((solidinfo & 0x78) != 0x38)) { solidh = 0xFFFF00; }
            //if ((solidinfo & 0x78) == 0x78) { solidh = 0xFFFFff; }
            switch (solidinfo & 0x7) {
                case 0:
                    for (int y = r - shgt; y < r - shgt + 16; y++) {
                        if (y < 0) { y = 0; }
                        for (int x = c; x < c + 16; x++) {
                            bmpdata2[(y * width) + x] = solidh;
                            //bmpdata2[(y * width) + x] = blendit(bmpdata2[(y * width) + x], solidh, 8, 8);
                        }
                    }
                    break;
                case 1:
                    if ((solidinfo & 0x78) == 8) { solidh = 0x606060; }
                    for (int y = r - shgt; y < r - shgt + 16; y++) {
                        if (y < 0) { y = 0; }
                        for (int x = c; x < c + 16; x++) {
                            bmpdata2[(y * width) + x] = solidh;
                            //bmpdata2[(y * width) + x] = blendit(bmpdata2[(y * width) + x], solidh, 8, 8);
                        }
                    }
                    break;
                case 4: // // stairs.
                    for (int y = r - shgt; y < r - shgt + 16; y++) {
                        if (y < 0) { y = 0; }
                        for (int x = c; x < c + 16; x++) {
                            int y2 = y - (x & 0xF); if (y2 < 0) { continue; }
                            bmpdata2[(y2 * width) + x] = solidh;
                        }
                    }
                    break;
                case 5: // \\ stairs.
                    for (int y = r - shgt; y < r - shgt + 16; y++) {
                        if (y < 0) { y = 0; }
                        for (int x = c; x < c + 16; x++) {
                            int y2 = y - (0xF - (x & 0xF)); if (y2 < 0) { continue; }
                            bmpdata2[(y2 * width) + x] = solidh;
                        }
                    }
                    break;
                case 6: // || stairs.
                    for (int y = r - shgt - 16; y < r - shgt + 16; y++) {
                        if (y < 0) { y = 0; }
                        for (int x = c; x < c + 16; x++) {
                            bmpdata2[(y * width) + x] = solidh;
                        }
                    }
                    break;
            }
            //Secondary Flooring
            if ((solidinfo & 0x80) == 0x80) { return; } //shgt = 0; }
            r = r - shgt - (((solidinfo >> 12) & 0xF) << ((solidinfo >> 0x1F) & 1)) * 8; //Gap
            shgt = (((solidinfo >> 20) & 0xF) << ((solidinfo >> 0x1F) & 1)) * 8;
            if (shgt == 0) { return; }
            //Solid floor
            solidh = 0x008000;
            if (((solidinfo>>16) & 0xC) == 4) { solidh = 0x808000; }
            //if ((solidinfo & 0x80) == 0x80) { solidh = 0x80; }
            //if ((solidinfo & 0x78) != 0) { solidh = (solidinfo & 0xF) * 0x101010; }
            //if ((solidinfo & 0x78) == 9) { solidh = 0xFFFFFF; }
            //if ((solidinfo & 0x78) >= 0x50) { solidh = 0xFFFFFF; }
            //if ((solidinfo & 0x78) == 0x20) { solidh = 0x00FFFF; }
            //if ((solidinfo & 0x78) == 0x40) { solidh = 0xFFFF00; }
            //if (((solidinfo & 0x78) != 0x0) && //Nothing special.
            //    ((solidinfo & 0x78) != 0x08) &&//Metal floor.
            //    ((solidinfo & 0x78) != 0x18) &&
            //    ((solidinfo & 0x78) != 0x38)) { solidh = 0xFFFF00; }
            //if ((solidinfo & 0x78) == 0x78) { solidh = 0xFFFFff; }
            switch ((solidinfo>>16) & 0xF) {
                case 0:
                    for (int y = r - shgt; y < r + 16; y++) {
                        if (y < 0) { y = 0; }
                        for (int x = c; x < c + 16; x++) {
                            bmpdata2[(y * width) + x] = 0x800000;
                        }
                    }
                    for (int y = r - shgt; y < r - shgt + 16; y++) {
                        if (y < 0) { y = 0; }
                        for (int x = c; x < c + 16; x++) {
                            bmpdata2[(y * width) + x] = solidh;
                        }
                    }
                    break;
                case 1:
                    for (int y = r - shgt; y < r + 16; y++) {
                        if (y < 0) { y = 0; }
                        for (int x = c; x < c + 16; x++) {
                            bmpdata2[(y * width) + x] = 0x800000;
                        }
                    }
                    if ((solidinfo & 0x78) == 8) { solidh = 0x606060; }
                    for (int y = r - shgt; y < r - shgt + 16; y++) {
                        if (y < 0) { y = 0; }
                        for (int x = c; x < c + 16; x++) {
                            bmpdata2[(y * width) + x] = solidh;
                        }
                    }
                    break;
                case 4: // // stairs.
                    for (int y = r - shgt; y < r + 16; y++) {
                        if (y < 0) { y = 0; }
                        for (int x = c; x < c + 16; x++) {
                            bmpdata2[(y * width) + x] = 0x800000;
                        }
                    }
                    for (int y = r - shgt; y < r - shgt + 16; y++) {
                        if (y < 0) { y = 0; }
                        for (int x = c; x < c + 16; x++) {
                            int y2 = y - (x & 0xF);
                            bmpdata2[(y2 * width) + x] = solidh;
                        }
                    }
                    break;
                case 5: // \\ stairs.
                    for (int y = r - shgt; y < r + 16; y++) {
                        if (y < 0) { y = 0; }
                        for (int x = c; x < c + 16; x++) {
                            bmpdata2[(y * width) + x] = 0x800000;
                        }
                    }
                    for (int y = r - shgt; y < r - shgt + 16; y++) {
                        if (y < 0) { y = 0; }
                        for (int x = c; x < c + 16; x++) {
                            int y2 = y - (0xF - (x & 0xF)); if (y2 < 0) { continue; }
                            bmpdata2[(y2 * width) + x] = solidh;
                        }
                    }
                    break;
                case 6: // || stairs.
                    for (int y = r - shgt; y < r + 16; y++) {
                        if (y < 0) { y = 0; }
                        for (int x = c; x < c + 16; x++) {
                            bmpdata2[(y * width) + x] = 0x800000;
                        }
                    }
                    for (int y = r - shgt - 16; y < r - shgt + 16; y++) {
                        if (y < 0) { y = 0; }
                        for (int x = c; x < c + 16; x++) {
                            bmpdata2[(y * width) + x] = solidh;
                        }
                    }
                    break;
                case 8:
                    for (int y = r - shgt; y < r + 8; y++) {
                        if (y < 0) { y = 0; }
                        for (int x = c; x < c + 16; x++) {
                            bmpdata2[(y * width) + x] = 0x800000;
                        }
                    }
                    for (int y = r - shgt; y < r - shgt + 8; y++) {
                        if (y < 0) { y = 0; }
                        for (int x = c; x < c + 16; x++) {
                            bmpdata2[(y * width) + x] = solidh;
                        }
                    }
                    break;
                case 9:
                    for (int y = r - shgt + 8; y < r + 16; y++) {
                        if (y < 0) { y = 0; }
                        for (int x = c; x < c + 16; x++) {
                            bmpdata2[(y * width) + x] = 0x800000;
                        }
                    }
                    for (int y = r - shgt + 8; y < r - shgt + 16; y++) {
                        if (y < 0) { y = 0; }
                        for (int x = c; x < c + 16; x++) {
                            bmpdata2[(y * width) + x] = solidh;
                        }
                    }
                    break;
                case 10:
                    for (int y = r - shgt; y < r + 16; y++) {
                        if (y < 0) { y = 0; }
                        for (int x = c + 8; x < c + 16; x++) {
                            bmpdata2[(y * width) + x] = 0x800000;
                        }
                    }
                    for (int y = r - shgt; y < r - shgt + 16; y++) {
                        if (y < 0) { y = 0; }
                        for (int x = c + 8; x < c + 16; x++) {
                            bmpdata2[(y * width) + x] = solidh;
                        }
                    }
                    break;
                case 11:
                    for (int y = r - shgt; y < r + 16; y++) {
                        if (y < 0) { y = 0; }
                        for (int x = c; x < c + 8; x++) {
                            bmpdata2[(y * width) + x] = 0x800000;
                        }
                    }
                    for (int y = r - shgt; y < r - shgt + 16; y++) {
                        if (y < 0) { y = 0; }
                        for (int x = c; x < c + 8; x++) {
                            bmpdata2[(y * width) + x] = solidh;
                        }
                    }
                    break;
                case 12: // \|
                    for (int y = r - shgt + 16; y < r + 16; y++) {
                        if (y < 0) { y = 0; }
                        for (int x = c; x < c + 16; x++) {
                            int y2 = y - (0xF - (x & 0xF)); if (y2 < 0) { continue; }
                            bmpdata2[(y2 * width) + x] = 0x800000;
                        }
                    }
                    for (int i = 0; i < 16; i++) {
                        int y = r - shgt + i;
                            if (y < 0) { y = 0; }
                            for (int x = c + i; x < c + 16; x++) {
                                bmpdata2[(y * width) + x] = solidh;
                            }
                    }
                    break;
                case 13: // /|
                    for (int y = r - shgt + 16; y < r + 16; y++) {
                        if (y < 0) { y = 0; }
                        for (int x = c; x < c + 16; x++) {
                            bmpdata2[(y * width) + x] = 0x800000;
                        }
                    }
                    for (int i = 0; i < 16; i++) {
                        int y = r - shgt + (i ^ 0xF);
                        if (y < 0) { y = 0; }
                        for (int x = c + i; x < c + 16; x++) {
                            bmpdata2[(y * width) + x] = solidh;
                        }
                    }
                    break;
                case 14: // |/
                    for (int y = r - shgt + 16; y < r + 16; y++) {
                        if (y < 0) { y = 0; }
                        for (int x = c; x < c + 16; x++) {
                            int y2 = y - (x & 0xF); if (y2 < 0) { continue; }
                            bmpdata2[(y2 * width) + x] = 0x800000;
                        }
                    }
                    for (int i = 0; i < 16; i++) {
                        int y = r - shgt + (i ^ 0xF);
                        if (y < 0) { y = 0; }
                        for (int x = c; x < c + i; x++) {
                            bmpdata2[(y * width) + x] = solidh;
                        }
                    }
                    break;
                case 15: // |\
                    for (int y = r - shgt + 16; y < r + 16; y++) {
                        if (y < 0) { y = 0; }
                        for (int x = c; x < c + 16; x++) {
                            bmpdata2[(y * width) + x] = 0x800000;
                        }
                    }
                    for (int i = 15; i >= 0; i--) {
                        int y = r - shgt + i;
                        if (y < 0) { y = 0; }
                        for (int x = c; x < c + i; x++) {
                            bmpdata2[(y * width) + x] = solidh;
                        }
                    }
                    break;
            }

            //for (int y = r; y < r + 16; y++) {
            //    for (int x = c; x < c + 16; x++) {
            //        //if (solidinfo != 0) { solidinfo = 0x777777; }
            //        int solidh2 = ((solidinfo >> 8) & 0xF) + ((solidinfo >> 12) & 0xF) + ((solidinfo >> 20) & 0xF);
            //        int solidh = 0x40 + ((solidh2 << ((solidinfo >> 0x1F) & 1)) << 3);
            //        //solidh +=
            //        //int solidm = 0;
            //        if ((solidinfo & 0xF) == 8) { solidh *= 0x010101; } else { solidh *= 0x000100; }
            //        if ((solidinfo & 0x80) == 0x80) { solidh = 0x80; }
            //        bmpdata[(y * width) + x] = blendit(bmpdata[(y * width) + x], solidh, 8,16);//0x8000 + ((solidinfo & 0xF00) << 3);//(solidinfo & 0xf00)<<6;
            //        //if ((solidinfo & 0x80) == 0x80) { bmpdata[(y * width) + x] = 0x80; }
            //        //bmpdata[(y * width) + x] = solidh;
            //    }
            //}
        }
        //public static byte layvis = 7;
        //static byte[] priomap = null;
        static void genPixPosTabel(int bitmapWidth) {
 int p = 0;
           //for (int h = 0; h < 4; h += 1) {
           for (short i = 0; i < 8 * bitmapWidth; i += (short)bitmapWidth) {//No flip table
               for (short j = i; j < i + 8; j++) {
                   pixPosTable[p++] = j;
               }
           }
           for (short i = 0; i < 8 * bitmapWidth; i += (short)bitmapWidth) { //X-flip table
               for (short j = (short)(i + 7); i <= j; j--) {
                   pixPosTable[p++] = j;
               }
           }
           for (short i = (short)(7 * bitmapWidth); 0 <= i; i -= (short)bitmapWidth) { //Y-flip table
               for (short j = i; j < i + 8; j++) {
                   pixPosTable[p++] = j;
               }
           }
           for (short i = (short)(7 * bitmapWidth); 0 <= i; i -= (short)bitmapWidth) { //X/Y-flip table
               for (short j = (short)(i + 7); i <= j; j--) {
                   pixPosTable[p++] = j;
               }
           }
       }
       static short[] pixPosTable = new short[0x100]; //Maybe most useful for 3DS pixel rendering? (@GBA/DS unnested tiles: Doesn't really have a noticable speed increase?)
       static byte[] blendflag = null;//Test.
       static int bldmod = 0x3C42;
       static int eva = 0x3C42;
       static int evb = 0x3C42;
       public static void loadLayer(int layiOffset) {
       }
       public static void addLayer(int[] bmpdata, int layiOffset, int bitmapWidth) {//, byte priority) { //a=ROM address of Map Data
           //Performance Critical:  Should be called every frame for animating..
           //if (((layvis >> (layiOffset >> 1)) & 1) == 0) { return; }
           if (((visf >> ((layiOffset >> 1) + 1)) & 1) == 0) { return; }
           //Tile Mod switch...
           int tmAddr = 0x3B7108 + (rom[0x3A78D4 + (Map.mapNum * 0x18) + 0xA] << 3);
           tmAddr = Bits.getInt32(rom, tmAddr);
           if (Map.tileModCur == -1) { tmAddr = 0; }
           if (tmAddr != 0) {
               tmAddr &= 0x1FFFFFF;
               tmAddr += Map.tileModCur * 0x14;
               tmAddr = Bits.getInt32(rom, tmAddr + ((4-layiOffset)<<1)) & 0x1FFFFFF;
           }
           //Regular Tilemap stuff...
           int layi = Bits.getInt16(rom, 0x3AAE08 + (mapNum * 8) + layiOffset);
           layiOffset = 8 >> (layiOffset >> 1);//For blending.
           if (layi == 0xFFFF) { return; }// null; }
           int a = 0x754D74 + Bits.getInt32(rom, 0x754D74 + (layi * 4));
           //0x754D74
           byte[] mdl1 = rom;
           int scnw = mdl1[a++] * 240;
           int scnh = mdl1[a++] * 160;
           //int[] bmpdata = new int[scnw * scnh];
           int tma = 0;
           int stem = 0;
           //Parallel.For(0, (scnh / 16), row2 => {
           //        int row = row2 * 16;
           for (int row = 0; row < scnh; row += 16) {
                   for (int col = 0; col < scnw; col += 16) {
                       if ((col & 0x3F) == 0) { stem = mdl1[a++]; }
                       stem <<= 2;
                       int tspos = ((stem & 0x300) | mdl1[a++]) * 8;
                       //Tile Mod switch...
                       if (tmAddr != 0) {
                           //int tmAddr2 = tmAddr + 4;
                           //if (((rom[tmAddr]*16) <= col) && (col < ((rom[tmAddr] + rom[tmAddr + 2])*16))
                           //    && ((rom[tmAddr + 1]*16) <= row) && (row < ((rom[tmAddr + 1] + rom[tmAddr + 3])*16))) {
                           if (((rom[tmAddr] * 16) <= row) && (row < ((rom[tmAddr] + rom[tmAddr + 2]) * 16))
                               && ((rom[tmAddr + 1] * 16) <= col) && (col < ((rom[tmAddr + 1] + rom[tmAddr + 3]) * 16))) {
                               //tspos = 0;
                               //tspos = 0x7FF & Bits.getInt16(rom, tmAddr + 4
                               //     + (((((row >> 4) - rom[tmAddr + 0]) * rom[tmAddr + 3])
                               //     + ((col >> 4) - rom[tmAddr + 1])) << 1));
                               int tspos2 = Bits.getInt16(rom, tmAddr + 4 + tma) * 8; tma += 2;
                               if (tspos2 != 0) { tspos = tspos2; }
                           }
                       }
                       for (int r = row; r < row + 16; r += 8) {
                           for (int c = col; c < col + 16; c += 8) {
                               //int tile = rom.getInt16(tsetaddr[0] + tspos); tspos += 2;
                               int tile = Bits.getInt16(rom, tsetaddr[tspos >> 12] + (tspos & 0xFFF)); tspos += 2; //tspos &= 0xFFF;
                               int pos = (tile & 0x3FF) << 5;
                               int tilePal = (tile >> 8) & 0xF0;
                               //Table test
                               int q = (tile >> 4) & 0xC0;
                               //for (int i = q; i < 0x40; i += 2) {
                               //return;
                               int tilepos = ((r * bitmapWidth) + c);
                               int k = q + 0x40;
                               while (q < k) {//  I = MIN ( 31, I1st*EVA + I2nd*EVB )
                                   int pix = ram[pos++];
                                   int pix2 = pix & 0xF;
                                   if (pix2 != 0) {
                                       int mappos = tilepos + pixPosTable[q];
                                       if (((blendflag[mappos >> 3] >> (mappos & 7)) & 1) == 1) {
                                           //int blue = (byte)bmpdata[mappos];
                                           if ((bldmod & (0xC0 | (layiOffset << 8))) == (0x40 | (layiOffset << 8))) {
                                               bmpdata[mappos] = blendit(bmpdata[mappos], pal[tilePal | pix2], eva, evb);
                                           }
                                           blendflag[mappos >> 3] ^= (byte)(1 << (mappos & 7));
                                       } else if (bmpdata[mappos] == 0) {
                                           bmpdata[mappos] = pal[tilePal | pix2];
                                           //priomap[(ry * bitmapWidth) + rx] = priority;
                                           //if ((bldmod & 0xC0) == 0x40) {
                                           if ((bldmod & (0xC0 | layiOffset)) == (0x40 | layiOffset)) {
                                               blendflag[mappos >> 3] |= (byte)(1 << (mappos & 7));
                                           }
                                       }
                                   }
                                   q++;
                                   pix2 = (pix >> 4);
                                   if (pix2 != 0) {
                                       int mappos = tilepos + pixPosTable[q];
                                       if (((blendflag[mappos >> 3] >> (mappos & 7)) & 1) == 1) {
                                           if ((bldmod & (0xC0 | (layiOffset << 8))) == (0x40 | (layiOffset << 8))) {
                                               bmpdata[mappos] = blendit(bmpdata[mappos], pal[tilePal | pix2], eva, evb);
                                           }
                                           blendflag[mappos >> 3] ^= (byte)(1 << (mappos & 7));
                                       } else if (bmpdata[mappos] == 0) {
                                           bmpdata[mappos] = pal[tilePal | pix2];
                                           //priomap[(ry * bitmapWidth) + rx] = priority;
                                           if ((bldmod & (0xC0 | layiOffset)) == (0x40 | layiOffset)) {
                                               blendflag[mappos >> 3] |= (byte)(1 << (mappos & 7));
                                           }
                                       }
                                   }
                                   q++;
                               }
                               //Table test end
                               //Since the speed of above might be about the same speed as below? The below has been commented instead of deleted.
                               //It might be worth keeping the above, if it gets used for Dream Team? (Assuming tiles use a different format.)
                               //int x1, x2, xi, y1, y2, yi; // Not sure how I wanted flipping coded in?  Alternative below, though!
                               //if ((tile & 0x400) == 0) { x1 = 0; x2 = 7; xi = 1; } else { x1 = 7; x2 = 0; xi = -1; };
                               //if ((tile & 0x800) == 0) { y1 = 0; y2 = 7; yi = 1; } else { y1 = 7; y2 = 0; yi = -1; };
                               //for (int y = r; y <= r + 7; y++) {
                               //    int ry = y; if ((tile & 0x800) != 0) { ry = r + ((7 - y) & 7); } //Vert. flip
                               //    //int pix = ram.getInt32(pos); pos += 4;
                               //    for (int x = c; x <= c + 7; x += 2) {
                               //        int rx = x; if ((tile & 0x400) != 0) { rx = c + ((7 - x) & 7); } //Horr. flip
                               //        int pix = ram.buffer[pos++];
                               //        int pix2 = pix & 0xF;
                               //        if (pix2 != 0) {
                               //            bmpdata[(ry * bitmapWidth) + rx] = pal[tilePal | (pix2)];
                               //            //priomap[(ry * bitmapWidth) + rx] = priority;
                               //        }
                               //        //pix >>= 4;
                               //        rx = x; if ((tile & 0x400) != 0) { rx = c + ((7 - (x + 1)) & 7); } //Horr. flip
                               //        pix2 = pix >> 4;
                               //        if (pix2 != 0) {
                               //            bmpdata[(ry * bitmapWidth) + rx] = pal[tilePal | (pix2)];
                               //            //priomap[(ry * bitmapWidth) + rx] = priority;
                               //        }
                               //        //pix >>= 4;
                               //    }
                               //}
                           }
                       }
                   }
                   a += (scnw / 240) & 3;
               }//);
           //return PixelsToImage(bmpdata, scnw, scnh);
       }
        //public static Bitmap dispLayer() {
        //public static void dispLayer() {

            //return null;
        //}
       public static int blendit(int first, int second, int eva, int evb) {
           int blue = (((byte)first * eva) >> 4) + (((byte)second * evb) >> 4);
           if (blue > 0xF8) { blue = 0xF8; }
           int green = (((byte)(first >> 8) * eva) >> 4) + (((byte)(second >> 8) * evb) >> 4);
           if (green > 0xF8) { green = 0xF8; }
           int red = (((byte)(first >> 16) * eva) >> 4) + (((byte)(second >> 16) * evb) >> 4);
           if (red > 0xF8) { red = 0xF8; }
           return unchecked((int)0xFF000000) | ((red << 16) | (green << 8) | blue) & 0xF8F8F8;
       }

        public static void initMap() {
            int palos = 0x8C88C8;
            palos += 0x1E0 * 96;

       }
      // DateTime a = DateTime.Now;//.Ticks;


    //'Compressed Tiling (Groups)
    //Dim ctdbase As Integer = &H83AAA6C
    //Dim cts As Short = 119
    //Dim ct1(cts), ct2(cts), ct3(cts) As Byte
    //'Compressed Tiling (Pixels)
    //Dim ctdbase2 As Integer = &H86527F4
    //Dim cts2 As Short = 161 'A0
        //}
       //*
       //Sprites stuff!!!
        static int priog = 0;
        static void addSprites(int[] bmpdata, int width, int prio) {
            priog = prio;
            //if (prio > 1) { return; } //Temp until ready.
            if ((visf & 0x20) == 0) {
                //Display the NPCs (Not sure where/how the best way to do this.)
                try {
                    int objsAddr = Bits.getInt32(rom, 0x3D6C58 + (mapNum << 2)) & 0x1FFFFFF; //TODO: Swap mapNum for value in Room Properties.
                    int amts = Bits.getInt16(rom, objsAddr);
                    int sprsAddr = objsAddr - Bits.getInt16(rom, objsAddr + 2);
                    int palsAddr = objsAddr - Bits.getInt16(rom, objsAddr + 4);
                    objsAddr = objsAddr - Bits.getInt16(rom, objsAddr + 6);
                    for (int numObj = amts >> 11; 0 < numObj; numObj--) { //Number of objects.
                        //loadSprite(bmpdata, 0x3000, 0, width);
                        int sprNum = rom[objsAddr + 5] & 0x7F;
                        if (sprNum == 0x7F) { objsAddr += 0x14; continue; }
                        sprNum = Bits.getInt16(rom, sprsAddr + ((sprNum) << 1));
                        int palNum = rom[objsAddr + 6] & 0x3F;
                        palNum = Bits.getInt16(rom, palsAddr + ((palNum) << 1));
                        int x = ((int)rom[objsAddr] << 4) + ((int)rom[objsAddr + 3] << 27 >> 27);
                        int y = ((int)rom[objsAddr + 1] << 4) + ((int)rom[objsAddr + 4] << 27 >> 27);
                        int z = (((int)rom[objsAddr + 2] & 0x7F) << 3)
                            + (((int)rom[objsAddr + 4] & 0x60) << 0x19 >> 0x1B)
                            + ((int)rom[objsAddr + 3] >> 5);
                        int aniNum = rom[objsAddr + 7];
                        //Console.WriteLine(objsAddr.ToString("X8") + ":" + sprNum.ToString("X4") +":"+palNum.ToString("X4"));
                        try {
                            loadSprite(bmpdata, sprNum, palNum, aniNum, x, y, z, width);
                        } catch { }
                        objsAddr += 0x14;
                    }
                } catch { }
                //Display non-scripted Item Blocks! (Includes mole (X) treasure as well.)
                try {
                    int sblkAddr = Bits.getInt32(rom, 0x51FA00 + (mapNum << 2)) & 0x1FFFFFF;
                    int blks = rom[sblkAddr];
                    sblkAddr -= Bits.getInt16(rom, sblkAddr + 3);
                    while (blks-- > 0) {
                        int coordPos = Bits.getInt16(rom, sblkAddr + 4);
                        int x = ((int)rom[sblkAddr + 1] << 4) + (((coordPos << 28 >> 28) << 1) | (rom[sblkAddr + 3] >> 7));
                        int y = ((int)rom[sblkAddr + 2] << 4) + (coordPos << 23 >> 27);
                        int z = ((int)(rom[sblkAddr + 3] & 0x7F) << 3) + (coordPos << 16 >> 25);
                        switch (rom[sblkAddr]) {
                            case 0x00: //Normal yellow block
                            case 0x10: //Hidden blocks
                                loadSprite(bmpdata, Bits.getInt16(rom, 0x3A0CF8), Bits.getInt16(rom, 0x3A0CFA), x, y, z, width);
                                break;
                            case 0x20: //Green block
                                loadSprite(bmpdata, Bits.getInt16(rom, 0x3A0D00), Bits.getInt16(rom, 0x3A0D02), x, y, z, width);
                                break;
                            case 0x40: //Red block
                                loadSprite(bmpdata, Bits.getInt16(rom, 0x3A0CFC), Bits.getInt16(rom, 0x3A0CFE), x, y, z, width);
                                break;
                            default:
                                //loadSprite(bmpdata, Bits.getInt16(rom, 0x3A0D04), Bits.getInt16(rom, 0x3A0D06), x, y - z, width);
                                //loadSprite(bmpdata, Bits.getInt16(rom, 0x3A0D04), 3, x, y - z, width);
                                // if ((rom[sblkAddr] != 0xA0) && (rom[sblkAddr] != 0xC0)) {
                                    // Console.WriteLine(sblkAddr.ToString("X8") + ":" + rom[sblkAddr].ToString("X2"));
                                // }
                                break;
                        }
                        sblkAddr += 8;
                    }
                } catch { }
            }
        }
        static void loadSprite(int[] bmpdata, int sprNum, int aniNum, int x1, int y1, int z1, int bitmapWidth) {
            loadSprite(bmpdata, sprNum, sprNum, aniNum, x1, y1, z1, bitmapWidth); //TODO: Set palNum=-1, and get "default palette" in next function.
        }
        static void loadSprite(int[] bmpdata, int sprNum, int palNum, int aniNum, int x1, int y1, int z1, int bitmapWidth) {
            //Priority check! @Collision Lookup - Swap bitmapWidth for Ground layer's width? (To fix some seabed map(s))
            //int cTile = rom[0x8E08E0 + Bits.getInt32(rom, 0x8E08E0 + mapNum*4) + (bitmapWidth>>4)*y1+x1];
            int solidloc = 0x8E08E0 + Bits.getInt32(rom, 0x8E08E0 + (Bits.getInt16(rom, 0x3AAE08 + (mapNum * 8) + 6) * 4));
            int solidset = Bits.getInt32(rom, 0x3AADD0 + (rom[0x3A78D4 + (mapNum * 0x18) + 6] * 4)) & 0x1FFFFFF;
            //for (int r = 0; r < height; r += 16) {
            //for (int c = 0; c < width; c += 16) {
            int solidinfo = Bits.getInt32(rom, solidset + (rom[solidloc + (bitmapWidth >> 4) * (y1 >> 4) + (x1 >> 4)] * 4));
            if (priog != ((solidinfo >> 0x18) & 3)) { return; }//Only groun priority. TODO: Add ledge priorties as well.
            y1 -= z1;
            //Sprite lookup!
            //sprNum = 0x3000;
            int main = Bits.getInt32(rom, (Bits.getInt32(rom, 0x39EE60 + (sprNum >> 12) * 4 - 4) & 0x1FFFFFF) + (sprNum & 0xFFF) * 4);
            int animain = Bits.getInt32(rom, (Bits.getInt32(rom, 0x39EE8C + (sprNum >> 12) * 4 - 4) & 0x1FFFFFF) + (((main >> 0x12) & 0x1FF) * 4)) & 0x1FFFFFF;
            //Ani Numeric Start
            //int seqs = rom.buffer[animain + 7]; //Listbox...
            int clipsList = animain - Bits.getInt16(rom, animain);
            //int clipsTotal = rom.buffer[clipsList];
            //Ani Numeric End
            //Animation List - Listbox 2 (Ignored)
            int sprshtsize = Bits.getInt16(rom, animain + 4) & 0x1FF;

            //PALETTE STUFF!
            //int main2 = Bits.getInt32(rom, (Bits.getInt32(rom, 0x39EE60 + (palNum >> 12) * 4 - 4) & 0x1FFFFFF) + (palNum & 0xFFF) * 4);
            int palmain = Bits.getInt32(rom, (Bits.getInt32(rom, 0x39EEE4 + (palNum >> 12) * 4 - 4) & 0x1FFFFFF) + ((palNum & 0x1FF) * 4)) & 0x1FFFFFF;
            //palmain + ((row * 16 + col) * 2
            int[] sprpal = new int[16];
            for (int i = 0; i < 0x10; i++) {
                int palt = Bits.getInt16(rom, palmain); palmain += 2;
                sprpal[i] = unchecked((int)0xFF000000) | ((palt & 0x1F) << 0x13) | ((palt & 0x3E0) << 6) | ((palt >> 7) & 0xF8);
            }

            int sprtblbase = Bits.getInt32(rom, 0x39EEB8 + (sprNum >> 12) * 4 - 4) & 0x1FFFFFF;
            int sprmain = sprtblbase + Bits.getInt32(rom, sprtblbase + (((main >> 9) & 0x1FF) * 4));

            //(main >> &H1B) And &H1F
            if ((main >> 0x1F) == 0) { Comp.mlssdecd(rom, sprmain, ram, 0x10000); }//Compressed?
            //TODO: Add uncompressed compatibility.
            //First test: Load Clip 0 with default palette.
            //if ((main >> 0x1e) & 1) { } //8-directional.
            //if ((main >> 0x1d) & 1) { } //4-directional.
            //0x3A05EC

            if (((main >> 0x1C) & 7) > 1) { aniNum = rom[0x3A05EC + (((main >> 0x1C) & 7) << 3) + aniNum]; }
            int horflip = aniNum >> 7; aniNum &= 0x7F;
            int frameClip = rom[animain + Bits.getInt16(rom, animain + 8 + (aniNum << 1)) + 1];
            int clipa = clipsList + Bits.getInt16(rom, clipsList + 1 + (frameClip * 2));
            //MessageBox.Show(clipa.ToString("X8"));
            int layers = rom[clipa++];
            if ((layers >> 7) == 1) { //Special Layers (Includes Rot/Scale)
                clipa += ((layers & 0x7F) * 6);
                layers = rom[clipa++];
            }
            //int ss = {0, 24, 0, 24, 26, 26};
            int tilesList = animain - Bits.getInt16(rom, animain + 2);
            int maxLayers = rom[animain + 6] & 0x7F;
            int x=0, y=0, q = 0, t = 0, tx = 0, ty = 0;
            int clipType = rom[clipa++];
            //Console.WriteLine(animain.ToString("X8") + " " + clipa.ToString("X8") + ":" + clipType + " " + (main >> 0x1f).ToString());
            int pos = 0;
            for (int lay = (layers & 0x7F); 0 < (lay & 0x7F) ; lay--) {
                switch (clipType) {
                    case 1:

                        //(lay * 3) + ((layers >> 7) * 7)
                        x = (int)rom[clipa++] << 24 >> 24; //Convert to signed.
                        y = (int)rom[clipa++] << 24 >> 24; //Convert to signed.
                        q = rom[clipa++];
                        clipa++; //t = rom[clipa++];
                        //0x39EE04 //tx, ty

                        /*
                        int tileoffsetnum = rom[tilesList + 1 + (0 * (maxLayers + 1)) + lay];
                        int pixbyte = tileoffsetnum * 0x20;
                        tx = rom[0x39EE04];
                        ty = rom[0x39EE04 + 2];
                        pos = 0x10000 + pixbyte;
                        for (int tiley = 0x100 + y; tiley < tx; tiley += 8) {
                            for (int tilex = 0x100 + x; tilex < tx; tilex += 8) {
                                //int tile = rom.getInt16(tsetaddr[tspos >> 12] + (tspos & 0xFFF)); tspos += 2; //tspos &= 0xFFF;
                                //int pos = (tile & 0x3FF) << 5;
                                //int tilePal = (tile >> 8) & 0xF0;
                                //Table test
                                //int q = (tile >> 4) & 0xC0;
                                //for (int i = q; i < 0x40; i += 2) {
                                //return;
                                int tilepos = ((tiley * bitmapWidth) + tilex);
                                int k = q + 0x40;
                                while (q < k) {
                                    int pix = ram[pos++];
                                    int pix2 = pix & 0xF;
                                    if (pix2 != 0) {
                                        bmpdata[tilepos + pixPosTable[q]] = sprpal[pix2];
                                        //priomap[(ry * bitmapWidth) + rx] = priority;
                                    }
                                    q++;
                                    pix2 = (pix >> 4);
                                    if (pix2 != 0) {
                                        bmpdata[tilepos + pixPosTable[q]] = sprpal[pix2];
                                        //priomap[(ry * bitmapWidth) + rx] = priority;
                                    }
                                    q++;
                                }
                            }
                        }*/
                        //   For tiley = 0 To (ty >> 3) - 1
                        //    For tilex = 0 To (tx >> 3) - 1
                        //        For ypix = 0 To 7 '&H0040 
                        //            For xpix = 0 To 3 '&H0008 '16*8
                        //                For xypix = 0 To 1 '&H0002
                        //                    'pix = data2(&H2000 * numb + row * &H20 * &H20 + col * &H20 + ypix * 4 + xpix) >> xypix * 4 And &HF
                        //                    'bmp.SetPixel(col * 8 + xpix * 2 + xypix, row * 8 + ypix, b(pix + NumericUpDown5.Value * 16))
                        //                    'pix = data2((CInt(tileoffsetnum) << 5) + (tiley * (tx >> 3)) + (tilex << 5) + (ypix << 2) + xpix) >> (xypix << 2) And &HF
                        //                    pix = data2(pixbyte) >> (xypix << 2) And &HF
                        //                    If pix <> 0 Then
                        //                        b = Color.FromArgb((palette1(pix + (&H10 * pal2)) And &H1F) * 8, (palette1(pix + (&H10 * pal2)) >> 5 And &H1F) * 8, (palette1(pix + (&H10 * pal2)) >> 10 And &H1F) * 8)
                        //                        Try
                        //                            clipbmp.SetPixel((PictureBox2.Width >> 1) + xs + (tilex << 3) + (xpix << 1) + xypix, (PictureBox2.Height >> 1) + ys + (tiley << 3) + ypix, b) '(pix + (NumericUpDown5.Value << 4)))
                        //                        Catch ex As Exception
                        //                            PictureBox2.Refresh()
                        //                            MsgBox("ERROR")
                        //                            GoTo errorexit1
                        //                        End Try
                        //                    End If
                        //                Next
                        //                pixbyte += 1
                        //            Next
                        //        Next
                        //    Next
                        //Next
                        //}
                        break;
                    case 2: //See 300E
                        //MessageBox.Show(clipa.ToString("x8"));
                        break;
                    case 3:
                        //int clipya = clipa;
                        //int clipb = clipa + layers * 4;
                        //for (int lay = (layers & 0x7F) - 1; 0 <= lay; lay--) {
                        //clipa = clipya + (lay * 4);
                        //MessageBox.Show(clipa.ToString("X8"));
                        x = (int)rom[clipa++]; x = x << 24 >> 24; //x+=x1; //Convert to signed.
                        y = (int)rom[clipa++]; y = y << 24 >> 24; //y+=y1; //Convert to signed.
                        q = rom[clipa++];
                        t = rom[clipa++];
                        tx = rom[0x39EE04 + ((t >> 4) << 1)];
                        ty = rom[0x39EE04 + ((t >> 4) << 1) + 1];
                        int tilesoffsetnum2;
                        if (tilesList == animain) { tilesoffsetnum2 = q; } else {
                            tilesoffsetnum2 = q;
                        }
                    //    int tileoffsetnum = rom.buffer[tilesList + 1 + (0 * (maxLayers + 1)) + lay];
                    //    int pixbyte = tilesoffsetnum2 * 0x20;
                        //tx = rom.buffer[0x39EE04 + ((t>>4) <<1)];
                        //ty = rom.buffer[0x39EE04 + ((t>>4) <<1) + 2];
                    //    int pos = 0x10000 + pixbyte;

                        // tileoffsetnum = rom[tilesList + 1 + (0 * (maxLayers + 1)) + lay];
                        //pixbyte = tilesoffsetnum2 * 0x20;
                        ////tx = rom.buffer[0x39EE04 + ((t>>4) <<1)];
                        ////ty = rom.buffer[0x39EE04 + ((t>>4) <<1) + 2];
                        //pos = 0x10000 + pixbyte;
                        //}   
                        break;
                    case 4:
                        x = (int)rom[clipa++] << 26 >> 26; //Convert to signed.
                        y = (int)rom[clipa++] << 26 >> 26; //Convert to signed.
                        tx = rom[0x39EE04 + ((((rom[clipa - 2] >> 6) << 2) | (rom[clipa - 1] >> 6)) << 1)];
                        ty = rom[0x39EE04 + ((((rom[clipa - 2] >> 6) << 2) | (rom[clipa - 1] >> 6)) << 1) + 1];
                        q = rom[clipa] & 0x7F;
                        //horflip ^= rom[clipa] >> 7;
                        if ((rom[clipa] >> 7) == 1) { t ^= 4; }
                        clipa++;
                        break;
                    case 5:
                        x = (int)rom[clipa++]; x = x << 26 >> 26; //Convert to signed.
                        y = (int)rom[clipa++]; y = y << 26 >> 26; //Convert to signed.
                        tx = rom[0x39EE04 + ((((rom[clipa - 2] >> 6) << 2) | (rom[clipa - 1] >> 6)) << 1)];
                        ty = rom[0x39EE04 + ((((rom[clipa - 2] >> 6) << 2) | (rom[clipa - 1] >> 6)) << 1) + 1];
                        q = rom[clipa++];
                        break;
                }
                pos = 0x10000 + (q * 0x20);
                if (horflip == 1) { x = -x - tx; t ^= 4; }
                x += x1; y += y1;
                for (int tiley = 0; tiley < 0 + ty; tiley += 8) {
                    for (int tilex = 0; tilex < 0 + tx; tilex += 8) {
                        //int tile = rom.getInt16(tsetaddr[tspos >> 12] + (tspos & 0xFFF)); tspos += 2; //tspos &= 0xFFF;
                        //int pos = (tile & 0x3FF) << 5;
                        //int tilePal = (tile >> 8) & 0xF0;
                        //Table test
                        //int q = (tile >> 4) & 0xC0;
                        //for (int i = q; i < 0x40; i += 2) {
                        //return;
                        int xflip = 0;
                        if ((t & 0x4) == 4) { xflip = (tx - 1) ^ 7; };
                        int yflip = 0;
                        if ((t & 0x8) == 8) { yflip = (ty - 1) ^ 7; };
                        int tilepos = ((y + (tiley ^ yflip)) * bitmapWidth) + (x + (tilex ^ xflip));
                        int z =  (t & 0xC) << 4;
                        int k = z + 0x40;
                        //try {
                        while (z < k) {
                            int pix = ram[pos++];
                            int pix2 = pix & 0xF;
                            if (pix2 != 0) {
                                int mappos = tilepos + pixPosTable[z];
                                if (((blendflag[mappos >> 3] >> (mappos & 7)) & 1) == 1) {
                                    //int blue = (byte)bmpdata[mappos];
                                    //if ((bldmod & (0xC0 | (layiOffset << 8))) == (0x40 | (layiOffset << 8))) {
                                        bmpdata[mappos] = blendit(bmpdata[mappos], sprpal[pix2], eva, evb);
                                    //}
                                    blendflag[mappos >> 3] ^= (byte)(1 << (mappos & 7));
                                } else if (bmpdata[mappos] == 0) {
                                    bmpdata[mappos] = sprpal[pix2];
                                    //priomap[(ry * bitmapWidth) + rx] = priority;
                                    //if ((bldmod & 0xC0) == 0x40) {
                                    //if ((bldmod & (0xC0 | layiOffset)) == (0x40 | layiOffset)) {
                                    //    blendflag[mappos >> 3] |= (byte)(1 << (mappos & 7));
                                    //}
                                }
                                //bmpdata[tilepos + pixPosTable[z]] = sprpal[pix2];
                                //priomap[(ry * bitmapWidth) + rx] = priority;
                            }
                            z++;
                            pix2 = (pix >> 4);
                            if (pix2 != 0) {
                                int mappos = tilepos + pixPosTable[z];
                                if (((blendflag[mappos >> 3] >> (mappos & 7)) & 1) == 1) {
                                    //int blue = (byte)bmpdata[mappos];
                                    //if ((bldmod & (0xC0 | (layiOffset << 8))) == (0x40 | (layiOffset << 8))) {
                                    bmpdata[mappos] = blendit(bmpdata[mappos], sprpal[pix2], eva, evb);
                                    //}
                                    blendflag[mappos >> 3] ^= (byte)(1 << (mappos & 7));
                                } else if (bmpdata[mappos] == 0) {
                                    bmpdata[mappos] = sprpal[pix2];
                                    //priomap[(ry * bitmapWidth) + rx] = priority;
                                    //if ((bldmod & 0xC0) == 0x40) {
                                    //if ((bldmod & (0xC0 | layiOffset)) == (0x40 | layiOffset)) {
                                    //    blendflag[mappos >> 3] |= (byte)(1 << (mappos & 7));
                                    //}
                                }
                                //bmpdata[tilepos + pixPosTable[z]] = sprpal[pix2];
                                //priomap[(ry * bitmapWidth) + rx] = priority;
                            }
                            z++;
                        }
                        //} catch { }

                    }
                }
            }
        }//*/
//        '>>1F ' 80000000
//        '>>12 and 1FF ' 07FC0000
//        '>>9 AND 1FF ' 0003FE00
//        'AND 1FF ' 000001FF
//        NumericUpDown2.Value = main And &H1FF
//        NumericUpDown3.Value = (main >> 9) And &H1FF
//        NumericUpDown4.Value = (main >> &H12) And &H1FF
//        NumericUpDown5.Value = (main >> &H1B) And &H1F 'And &H1FF

//        'Locate Palette
//        'Dim palette1(255) As Short
//        'Dim palette1(31) As Short
//        'Dim spr(1) As Byte
//        'Dim b As Color ' = Color.FromArgb(0, 0, 0) 'Red
//        For row = 0 To 1 '15
//            For col = 0 To 15
//                'FileGet(1, palette1(row * 8 + col), &H4F4CCC + ((row * 8 + col) * 2 + 1))
//                'FileGet(1, palette1(row * 16 + col), &H4F4CCC + ((row * 16 + col) * 2 + 1))
//                FileGet(1, palette1(row * 16 + col), pointer - &H8000000 + ((row * 16 + col) * 2 + 1))
//                'MsgBox(Hex(&H4F4CCC + ((row * 8 + col) * 2)))
//                b = Color.FromArgb((palette1(row * 16 + col) And &H1F) * 8, (palette1(row * 16 + col) >> 5 And &H1F) * 8, (palette1(row * 16 + col) >> 10 And &H1F) * 8) 'row * 32, col * 16, row + col)
//                For xpix = 0 To 14
//                    For ypix = 0 To 14
//                        palbmp.SetPixel(col * 15 + xpix, row * 15 + ypix, b)
//                    Next
//                Next
//            Next
//        Next
//        Panel1.Refresh()
        static byte[] frmpcnt = new byte[16];
        //static byte[] frmpnum = new byte[16];
        static byte[] frmcnt = new byte[16];
        static byte[] frmnum = new byte[16];
        public static void tileAniNext() {
            palAniNext();
            tileAniNext2();
        }
        public static void palAniNext() { //Palette Animations
            int pntr = Bits.getInt32(rom, 0x3B79C4 + (rom[0x3A78D4 + (mapNum * 0x18) + 0x8] << 2));
            if (pntr == 0) { return; }
            pntr &= 0x1FFFFFF;
            int mdat, a = 0;
            do {
                mdat = Bits.getInt32(rom, pntr); pntr += 4;
                int pntr2 = Bits.getInt32(rom, (Bits.getInt32(rom, 0x2011B4 + 0x14) & 0x1FFFFFF) + (mdat >> 8 << 2)) & 0x1FFFFFF;
                //^TODO:  Check if this mdat index is 8-bit or 24-bit.
                //pntr2 = 
                if (frmpcnt[a]++ == rom[pntr2 + 0x4]) {
                    frmpcnt[a] = 0;
                    if ((rom[pntr2] & 0xF0) == 0) {
                        int pl = ((rom[pntr2] & 0x0F) << 4) | (rom[pntr2 + 2] & 0x0F);
                        int tempColor = pal[pl];
                        int plend = rom[pntr2 + 2] >> 4;
                        while ((pl & 0xF) > plend) {
                            pal[pl] = pal[--pl];
                        }
                        pal[pl] = tempColor;
                    }
                }
                a++;
            } while ((mdat & 0x80) == 0);
        } 
       public static void tileAniNext2() {//Tile Animations
            int pntr = Bits.getInt32(rom, 0x3B283C + (rom[0x3A78D4 + (mapNum * 0x18) + 0x7] << 2));
            if (pntr == 0) { return; }
            pntr &= 0x1FFFFFF;
            int mdat, a = 0;
            do {
                mdat = Bits.getInt32(rom, pntr);
                if ((mdat & 0x40) == 0) { //No auto-animating flag on Room Load. (Manual activate) Note: mdat & 0x200 = No loop.
                    if (frmcnt[a] == 0) {
                        int pntr2 = (Bits.getInt32(rom, pntr + 4) & 0x1FFFFFF) + (frmnum[a] << 2);
                        if ((rom[pntr2 + 3] >> 7) == 0) { frmnum[a]++; } else { frmnum[a] = 0; }
                        frmcnt[a] = rom[pntr2 + 2];
                        //Get Tile Animation Graphics
                        pntr2 = 0x940C9C + Bits.getInt32(rom, 0x940C9C + (Bits.getInt16(rom, pntr2) << 2));
                        for (int i = ((mdat & 0x1F00000) >> 15); i > 0; i--) {
                            ram[((mdat & 0xFFC00) >> 5) + i] = rom[pntr2 + i];
                        }
                    } else {
                        frmcnt[a]--;
                    }
                }
                pntr += 8; a++;
            } while ((mdat & 0x80) == 0);
        }
    }
}
