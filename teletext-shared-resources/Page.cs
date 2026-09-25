using System;
using System.IO;
using System.Security.Cryptography;

namespace TeletextSharedResources
{
    public class Page
    {
        public Line[] Lines = new Line[256];
        public Mode[,] modeMap = new Mode[25, 40];
        public Mode[,] modeMapL2 = new Mode[25, 40];
        public Int32 Subpage = 0;

        public Page()
        {
            Clear();
        }

        public Page(Line[] inLines, Mode[,] inmodeMap, Mode [,]inmodeMapL2, Int32 inSubpage)
        {
            this.Lines = inLines;
            this.modeMap = inmodeMap;
            this.modeMapL2 = inmodeMapL2;
            this.Subpage = inSubpage;
        }

        public Page Clone()
        {
            return new Page((Line[])this.Lines.Clone(), (Mode[,])this.modeMap.Clone(), (Mode[,])this.modeMapL2.Clone(), this.Subpage);
        }

        public void Clear()
        {
            for (Int32 n = 0; n < 256; n++)
            {
                this.Lines[n].Clear();
            }
            Lines[0].Flags = new ControlFlags();
        }

        public Line GetRow(Int32 row)
        {
            Line ret = new Line();

            for (Int32 n = 0; n < 256; n++)
            {
                if (this.Lines[n].Row == row && this.Lines[n].Type != LineTypes.Blank)
                    ret = this.Lines[n];

            }

            return ret;
        }

        public Byte GetPacketIndex(Int32 packetNo)
        {
            Byte ret = 255;
            for (Byte n = 0; n <= 254 && ret == 255; n++)
            {
                if (this.Lines[n].Row == packetNo && this.Lines[n].Type != null)
                    ret = n;

            }

            return ret;
        }

        public void WriteChar(Int32 packet, Int32 x, Byte c)
        {
            Line l = GetRow(packet);

            if (l.Text != null)
            {
                if (l.Text.Length < 40)
                    l.Text = l.Text.PadLeft(40 - l.Text.Length) + l.Text;
                l.Text = l.Text.Substring(0, x) + c.ToString() + l.Text.Substring(x + 1, l.Text.Length - 1 - x);
                this.Lines[l.LineNo] = l;
            }
        }

        public bool Save(string Filename, bool Append = false)
        {
            bool outcome = true;

            if (!Append && File.Exists(Filename))
            {
                File.Delete(Filename);
            }

            try
            {
                using (var stream = new FileStream(Filename, FileMode.Append))
                {
                    for (int i = 0; i < 25; i++)
                    {
                        stream.Write(Lines[GetPacketIndex(i)].Bytes, 0, Lines[GetPacketIndex(i)].Bytes.Length);
                    }
                }
            }
            catch 
            { 
                outcome = false;
            }

            return outcome;
        }
    }


}