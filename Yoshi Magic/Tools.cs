using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Forms;
using System.Drawing; //Size
namespace Yoshi_Magic {
    class Tools {
        byte[] rom;
        List<int> addrs = new List<int>(0x200); //int addrInd = 0;
        NumericUpDown nud1 = new NumericUpDown();
        ListBox lb1 = new ListBox();
        Form ts = new Form(); //ts for Tools
        public void init(byte[] romArray) {
            rom = romArray;
            //if (ts.IsDisposed || !ts.Visible) {
            
            //ts.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            ts.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            int w = 800, h = 600;
            ts.ClientSize = new Size(w, h);

            nud1.Width = w;
            nud1.Hexadecimal = true;
            nud1.Maximum = 0x1FFFFFF;
            nud1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            ts.Controls.Add(nud1);
            lb1.Top = nud1.Height;
            lb1.Size = new System.Drawing.Size(w, h - nud1.Height);
            lb1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            lb1.Font = new Font("Consolas", 10);
            ts.Controls.Add(lb1);
            ts.Show(); //Setting the owner to "this" form makes windows stay in front of main form, but not in front of other apps like .TopMost.
            nud1.ValueChanged += new EventHandler(this.ts_nud1ValChanged);
            lb1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ts_KeyDown);
            //this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
        }
        private void ts_nud1ValChanged(object sender, EventArgs e) {
            scan((int)nud1.Value);
        }
        private void ts_KeyDown(object sender, KeyEventArgs e) {
            //MessageBox.Show(e.KeyCode.ToString("X"));
            switch (e.KeyCode) {
                case (Keys)0x31:
                    scan(prev);
                    break;
                case (Keys)0x32:
                    scan(next);
                    break;
                case (Keys)0x33:
                    if (lb1.SelectedIndex == -1) { return; }
                    scan(addrs[lb1.SelectedIndex]);
                    break;
            }
        }
        int prev = 0, cur = 0, next = 0;//0x2000000;
        private void scan(int addr) {
            uint val = (uint)Bits.getInt32(rom, 0);
            int i = 4; prev = 0; cur = 0; next = 0x2000000;
            while (true) {
                int val2 = -1;
                if ((i & 3) == 0) { //ARM
                    //if ((val & 0xFE000000) == 0xFA000000) { //BLX
                    //    val2 = i + 4 + ((int)val << 0x8 >> 0x8 << 0x2);
                    //    if ((val & 0x01000000) != 0) { val2 += 2; }
                    //} else
                    if ((val & 0x0F000000) == 0x0B000000) { //BL
                        val2 = i + 4 + ((int)val << 0x8 >> 0x8 << 0x2);
                    }
                }
                if ((i & 1) == 0) { //THUMB
                    if ((val & 0xF800F800) == 0xF800F000) { //BL
                        val2 = i + (((int)val << 0x15 >> 0x15 << 0xC) | (((int)val & 0x7FF0000) >> 0xF));
                    }
                }
                if ((i & 0) == 0) { //POINTERS
                    if ((val & 0xFE000000) == 0x08000000) {
                        val2 = (int)val & 0x1FFFFFE;
                    }
                }
                //if (val2 < 0) {
                //} else
                if (val2 < prev) {
                    //Do nothing.
                } else if (val2 < cur) {
                    prev = val2;
                } else if (val2 == cur) {
                    addrs.Add(i-4);
                } else if (val2 <= addr) {
                    prev = cur;
                    cur = val2;
                    addrs.Clear();
                    addrs.Add(i-4);
                } else if (val2 < next) {
                    next = val2;
                }
                
                if (i >= rom.Length) { break; }
                val = (val >> 8) | ((uint)rom[i++] << 24);
            }
            lb1.Items.Clear();
            for (int j = 0; j < addrs.Count; j++) {
                int val3 = Bits.getInt32(rom, addrs[j]);
                //if ((val3 & 0xFE000000) == 0xFA000000) { //BLX
                //    lb1.Items.Add(j.ToString("X8") + " | " + addrs[j].ToString("X8") + " (Arm BLX: " + val3.ToString("X8") + ")");
                //} else
                //if ((val3 & 0x0F000000) == 0x0B000000) { //BL
                //    lb1.Items.Add(j.ToString("X8") + " | " + addrs[j].ToString("X8") + " (Arm BL: " + val3.ToString("X8") + ")");
                //}
                //if ((val3 & 0xF800F800) == 0xF800F000) { //BL
                //    lb1.Items.Add(j.ToString("X8") + " | " + addrs[j].ToString("X8") + " (Thumb BL: " + val3.ToString("X8") + ")");
                //}
                //if ((val3 & 0xFE000000) == 0x08000000) { //POINTERS
                //    lb1.Items.Add(j.ToString("X8") + " | " + addrs[j].ToString("X8") + " (Pointer: " + val3.ToString("X8") + ")");
                //}
                lb1.Items.Add(j.ToString("X8") + " | " + addrs[j].ToString("X8") + ":" + val3.ToString("X8"));
            }
            lb1.SelectedIndex = 0;
            ts.Text = "List for: " + cur.ToString("X8") + "  Previous (1): " + prev.ToString("X8") + "  Next (2): " + next.ToString("X8");
        }
    }
}
