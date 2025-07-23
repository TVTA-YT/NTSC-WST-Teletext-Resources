using System;
using ExtensionMethods;
using System.Linq;
using System.Text;

namespace TeletextSharedResources
{
    public struct Line
    {
        public Int32 Magazine;
        public Int32 Row;
        public String Page;
        public String TimeCode;
        public LineTypes Type;
        public String Text;
        public Int64 StartPos;
        public Int64 EndPos;
        public byte LineNo;  //the "VBI" line as passed from the card (not actually VBIs but some internal construct)
        public Int32 Frame;
        public ControlFlags Flags;
        public Byte MRAG1, MRAG2;
        public Byte PU, PT, MU, MT, HU, HT, CA, CB;
        public Byte[] Bytes;
        public String MagPage;
        public String[] FastextLinks;
        public int PacketSize;

        public Line(Int32 Magazine, String Page, String Subpage, byte PacketNo, byte RowNo, string Text, String flags = "10000000000", int PacketSize = 42) : this()
        {
            String textForByte = (PacketNo == 0) ? Text.PadLeft(40, (Char)0x20) : Text.PadRight(40, (Char)0x00);
            //textForByte = Text.PadLeft(PacketSize, (Char)0x00);
            flags = flags.PadLeft(16);

            Clear();
            EndPos = 0;
            Flags = new ControlFlags();
            Frame = 0;
            LineNo = PacketNo;
            Magazine = Magazine % 8;
            this.Page = Page;
            MagPage = Magazine.ToString() + Page;
            Row = RowNo;
            StartPos = 0;
            this.Text = Text;
            Bytes = Encoding.ASCII.GetBytes("\x00\x00" + textForByte);
            TimeCode = Subpage;
            Type = LineTypes.Blank;
            Flags = new ControlFlags();
            Flags.C4_Erase = (flags.Substring(1, 1) == "1" ? true : false);
            Flags.C5_Newsflash = (flags.Substring(2, 1) == "1" ? true : false);
            Flags.C6_Subtitle = (flags.Substring(3, 1) == "1" ? true : false);
            Flags.C7_SuppressHeader = (flags.Substring(4, 1) == "1" ? true : false);
            Flags.C8_Update = (flags.Substring(5, 1) == "1" ? true : false);
            Flags.C9_InterruptedSequence = (flags.Substring(6, 1) == "1" ? true : false);
            Flags.C10_InhibitDisplay = (flags.Substring(7, 1) == "1" ? true : false);
            Flags.C11_MagazineSerial = (flags.Substring(8, 1) == "1" ? true : false);
            Flags.C12 = (flags.Substring(9, 1) == "1" ? true : false);
            Flags.C13 = (flags.Substring(10, 1) == "1" ? true : false);
            Flags.C14 = (flags.Substring(11, 1) == "1" ? true : false);

            switch (PacketNo)
            {
                case 0:
                    Type = LineTypes.Header;
                    break;
                case 25:
                    Type = LineTypes.FastextDisplay;
                    break;
                default:
                    Type = LineTypes.Line;
                    break;
            }
            CalcHammingCodes();
            CalcParity();
        }

        public Line(Int32 Magazine, String Page, String Subpage, byte PacketNo, byte RowNo, byte[] Bytes, String flags = "10000000000") : this()
        {

            byte[] Bytes7f = new byte[Bytes.Length - 2];

            string text = "";
            for (int n = 2; n < Bytes.Length; n++)
            {
                Bytes7f[n - 2] = (byte)(Bytes[n] & 0x7f);
                //text += Convert.ToChar(Bytes[n] & 0x7f).ToString();
                var ch = (char)(Bytes[n] & 0x7f);
                text += ch;
            }

            string textForByte = System.Text.ASCIIEncoding.UTF8.GetString(Bytes);

            if (PacketNo == 0)
            {
                text = "   P" + Magazine.ToString() + Page + " " + text.Substring(8);
            }

            flags = flags.PadLeft(16);

            Line newline = new Line();
            Clear();
            EndPos = 0;
            Flags = new ControlFlags();
            Frame = 0;
            LineNo = PacketNo;
            Magazine = Magazine % 8;
            this.Page = Page;
            MagPage = Magazine.ToString() + Page;
            Row = RowNo;
            StartPos = 0;
            Text = text;
            this.Bytes = Bytes;
            TimeCode = Subpage;
            Flags = new ControlFlags();
            Flags.C4_Erase = (flags.Substring(1, 1) == "1" ? true : false);
            Flags.C5_Newsflash = (flags.Substring(2, 1) == "1" ? true : false);
            Flags.C6_Subtitle = (flags.Substring(3, 1) == "1" ? true : false);
            Flags.C7_SuppressHeader = (flags.Substring(4, 1) == "1" ? true : false);
            Flags.C8_Update = (flags.Substring(5, 1) == "1" ? true : false);
            Flags.C9_InterruptedSequence = (flags.Substring(6, 1) == "1" ? true : false);
            Flags.C10_InhibitDisplay = (flags.Substring(7, 1) == "1" ? true : false);
            Flags.C11_MagazineSerial = (flags.Substring(8, 1) == "1" ? true : false);
            Flags.C12 = (flags.Substring(9, 1) == "1" ? true : false);
            Flags.C13 = (flags.Substring(10, 1) == "1" ? true : false);
            Flags.C14 = (flags.Substring(11, 1) == "1" ? true : false);

            switch (PacketNo)
            {
                case 0:
                    Type = LineTypes.Header;
                    break;
                case 25:
                    Type = LineTypes.FastextDisplay;
                    break;
                default:
                    Type = LineTypes.Line;
                    break;
            }
            CalcHammingCodes();
            CalcParity();
        }


        public Line(Int32 Magazine, Byte Packet, String[] FastextLinks) : this()
        {
            EndPos = 0;
            Flags = new ControlFlags();
            Frame = 0;
            LineNo = Packet;
            Magazine = Magazine % 8;
            Row = Packet;
            StartPos = 0;
            FastextLinks = FastextLinks;
            Bytes = new Byte[42];
            TimeCode = "00:00";

            CalcHammingCodes();
            Bytes[0] = MRAG1;
            Bytes[1] = MRAG2;

            switch (Packet)
            {
                case 0:
                    Type = LineTypes.Header;
                    break;
                default:
                    Type = LineTypes.Line;
                    break;
            }
            CalcHammingCodes();
            CalcParity();
        }

        public void Clear()
        {
            Magazine = 0;
            Row = 0;
            Page = "";
            TimeCode = "00:00";
            Type = LineTypes.Blank;
            Text = "";
            StartPos = 0;
            EndPos = 0;
            LineNo = 0;
            Frame = 0;
            Flags = new ControlFlags();
            MRAG1 = MRAG2 = PU = PT = MU = MT = HU = HT = CA = CB = 0;
            Bytes = new Byte[42];
        }

        public void CalcHammingCodes()
        {
            //Convert data to binary
            String magBin = Convert.ToString((Byte)this.Magazine % 8, 2).PadLeft(3, Convert.ToChar("0"));
            String rowBin = Convert.ToString((Byte)this.Row % 32, 2).PadLeft(5, Convert.ToChar("0"));

            String puBin = "";
            String ptBin = "";

            String thtBin = "";
            String thuBin = "";
            String tmuBin = "";
            String tmtBin = "";

            String controlAbin = "";
            String controlBbin = "";

            if (this.Row == 0)
            {
                puBin = Convert.ToString(Convert.ToByte(this.Page.Substring(1, 1), 16) % 32, 2).PadLeft(4, Convert.ToChar("0"));
                ptBin = Convert.ToString(Convert.ToByte(this.Page.Substring(0, 1), 16) % 32, 2).PadLeft(4, Convert.ToChar("0"));

                thtBin = "";
                thuBin = "";
                tmuBin = "";
                tmtBin = "";

                string strTimeCode = this.TimeCode.Replace(":", "");
                if (strTimeCode.Length != 4)
                    strTimeCode = "0000";
                
                thtBin = Convert.ToString(Convert.ToByte(strTimeCode.Substring(0, 1), 16) % 32, 2).PadLeft(4, Convert.ToChar("0"));
                thuBin = Convert.ToString(Convert.ToByte(strTimeCode.Substring(1, 1), 16) % 32, 2).PadLeft(4, Convert.ToChar("0"));
                tmtBin = Convert.ToString(Convert.ToByte(strTimeCode.Substring(2, 1), 16) % 32, 2).PadLeft(4, Convert.ToChar("0"));
                tmuBin = Convert.ToString(Convert.ToByte(strTimeCode.Substring(3, 1), 16) % 32, 2).PadLeft(4, Convert.ToChar("0"));
                

                tmtBin = this.Flags.C4_Erase == true ? "1" + tmtBin.Substring(1) : tmtBin;
                thtBin = this.Flags.C6_Subtitle == true ? "1" + thtBin.Substring(1) : thtBin;
                thtBin = this.Flags.C5_Newsflash == true ? thtBin.Substring(0, 1) + "1" + thtBin.Substring(2) : thtBin;


                controlAbin += this.Flags.C10_InhibitDisplay == true ? "1" : "0";
                controlAbin += this.Flags.C9_InterruptedSequence == true ? "1" : "0";
                controlAbin += this.Flags.C7_SuppressHeader == true ? "1" : "0";
                controlAbin += this.Flags.C6_Subtitle == true ? "1" : "0";


                controlBbin += this.Flags.C14 == true ? "1" : "0";
                controlBbin += this.Flags.C13 == true ? "1" : "0";
                controlBbin += this.Flags.C12 == true ? "1" : "0";
                controlBbin += this.Flags.C11_MagazineSerial == true ? "1" : "0";
            }

            //Char[] magBinRevC = (Char[])magBin;//.Reverse();
            //Char[] rowBinRevC = (Char[])rowBin.ToCharArray();//.Reverse();

            //reverse bits to convert to transmission order
            String magBinRev = magBin.Substring(2, 1) + magBin.Substring(1, 1) + magBin.Substring(0, 1);
            String rowBinRev = rowBin.Substring(4, 1) + rowBin.Substring(3, 1) + rowBin.Substring(2, 1) + rowBin.Substring(1, 1) + rowBin.Substring(0, 1);

            String nybble1 = magBinRev + rowBinRev.Substring(0, 1);
            String nybble2 = rowBinRev.Substring(1);

            //Work out MRAG
            this.MRAG1 = hammingEncode(nybble1);
            this.Bytes[0] = MRAG1;
            this.MRAG2 = hammingEncode(nybble2);
            this.Bytes[1] = MRAG2;


            if (this.Row == 0)
            {
                this.PU = hammingEncode(puBin.Reverse());
                this.PT = hammingEncode(ptBin.Reverse());

                if (this.TimeCode.Length == 5)
                {
                    this.HT = hammingEncode(thtBin.Reverse());
                    this.HU = hammingEncode(thuBin.Reverse());
                    this.MT = hammingEncode(tmtBin.Reverse());
                    this.MU = hammingEncode(tmuBin.Reverse());
                }

                this.CA = hammingEncode(controlAbin.Reverse());
                this.CB = hammingEncode(controlBbin.Reverse());

                this.Bytes[0] = this.MRAG1;
                this.Bytes[1] = this.MRAG2;
                this.Bytes[2] = this.PU;
                this.Bytes[3] = this.PT;
                this.Bytes[4] = this.MU;
                this.Bytes[5] = this.MT;
                this.Bytes[6] = this.HU;
                this.Bytes[7] = this.HT;
                this.Bytes[8] = this.CA;
                this.Bytes[9] = this.CB;
            }

            //Check if line is Header or Line
 /*           switch (this.Type)
            {
                case LineTypes.Header:
                    Byte hammed;
                    String strPageUnits = Convert.ToString(Convert.ToByte(this.Page.Substring(1, 1), 16), 2).PadLeft(4, Convert.ToChar("0"));
                    hammed = hammingEncode(strPageUnits.Reverse());
                    this.Bytes[3] = hammed;
                    this.PU = hammed;

                    String strPageTens = Convert.ToString(Convert.ToByte(this.Page.Substring(0, 1), 16), 2).PadLeft(4, Convert.ToChar("0"));
                    hammed = hammingEncode(strPageTens.Reverse());
                    this.Bytes[4] = hammed;
                    this.PT = hammed;

                    String strTimeCodeMinutesUnits = Convert.ToString(Convert.ToByte(this.TimeCode.Substring(3, 1), 16), 2).PadLeft(4, Convert.ToChar("0"));
                    hammed = hammingEncode(strTimeCodeMinutesUnits.Reverse());
                    this.Bytes[5] = hammed;
                    this.MU = hammed;

                    String strTimeCodeMinutesTens = Convert.ToString(Convert.ToByte(this.TimeCode.Substring(2, 1), 16), 2).PadLeft(4, Convert.ToChar("0")).Substring(1, 3);
                    strTimeCodeMinutesTens = (this.Flags.C4_Erase ? "1" : "0") + strTimeCodeMinutesTens;
                    hammed = hammingEncode(strTimeCodeMinutesTens.Reverse());
                    this.Bytes[6] = hammed;
                    this.MT = hammed;

                    String strTimeCodeHoursUnits = Convert.ToString(Convert.ToByte(this.TimeCode.Substring(1, 1), 16), 2).PadLeft(4, Convert.ToChar("0"));
                    hammed = hammingEncode(strTimeCodeHoursUnits.Reverse());
                    this.Bytes[7] = hammed;
                    this.HU = hammed;

                    String strTimeCodeHoursTens = Convert.ToString(Convert.ToByte(this.TimeCode.Substring(0, 1), 16), 2).PadLeft(4, Convert.ToChar("0")).Substring(2, 2);
                    strTimeCodeHoursTens = (this.Flags.C6_Subtitle ? "1" : "0") + (this.Flags.C5_Newsflash ? "1" : "0") + strTimeCodeHoursTens;
                    hammed = hammingEncode(strTimeCodeHoursTens.Reverse());
                    this.Bytes[8] = hammed;
                    this.HT = hammed;

                    String strControlGroupA = (this.Flags.C10_InhibitDisplay ? "1" : "0") + (this.Flags.C9_InterruptedSequence ? "1" : "0") + (this.Flags.C8_Update ? "1" : "0") + (this.Flags.C7_SuppressHeader ? "1" : "0");
                    hammed = hammingEncode(strControlGroupA.Reverse());
                    this.Bytes[9] = hammed;
                    this.CA = hammed;

                    String strControlGroupB = (this.Flags.C14 ? "1" : "0") + (this.Flags.C13 ? "1" : "0") + (this.Flags.C12 ? "1" : "0") + (this.Flags.C11_MagazineSerial ? "1" : "0");
                    hammed = hammingEncode(strControlGroupB.Reverse());
                    this.Bytes[10] = hammed;
                    this.CA = hammed;

                    break;
                case LineTypes.Line:
                    break;
                default:
                    break;
            }*/

            if (this.FastextLinks != null)
            {
                if (this.FastextLinks.Length != 7)
                    System.Diagnostics.Debug.WriteLine("Incorrect number of links for a Fastext data line.  First element will be FL and can be ignored.");
                else
                {
                    // Set this.Bytes[2:41] to be the fastext data for the links provided
                    String designationCode = "0000";
                    this.Bytes[2] = hammingEncode(designationCode.Reverse());

                    for (Int32 n = 0; n < 6; n++)
                    {
                        // Get the next link
                        String strLink = this.FastextLinks[n + 1];
                        if (strLink.Length != 3)
                            strLink = "1FF";

                        //Convert the magpage elements in to binary strings
                        String pgUnitsBin = Convert.ToString(Convert.ToByte(strLink.Substring(2, 1), 16), 2).PadLeft(4, '0');
                        String pgTensBin = Convert.ToString(Convert.ToByte(strLink.Substring(1, 1), 16), 2).PadLeft(4, '0');
                        String magazineBin = Convert.ToString(Convert.ToByte(strLink.Substring(0, 1), 16), 2).PadLeft(3, '0');

                        // Subpages in Fastext links are not supported in .TTI format, so not supported here for now - they are defaulted to 00:00
                        // S1 - S4 are the bits in 3F7F, where S1 is the rightmost hex char F
                        String s1Bin = "0000";
                        String s2Bin = "000" + magazineBin.Substring(2, 1);
                        String s3Bin = "0000";
                        String s4Bin = "00" + magazineBin.Substring(0, 2);

                        // Ham the bits and store in the line bytes
                        this.Bytes[3 + (n * 6) + 0] = hammingEncode(pgUnitsBin.Reverse());
                        this.Bytes[3 + (n * 6) + 1] = hammingEncode(pgTensBin.Reverse());
                        this.Bytes[3 + (n * 6) + 2] = hammingEncode(s1Bin.Reverse());
                        this.Bytes[3 + (n * 6) + 3] = hammingEncode(s2Bin.Reverse());
                        this.Bytes[3 + (n * 6) + 4] = hammingEncode(s3Bin.Reverse());
                        this.Bytes[3 + (n * 6) + 5] = hammingEncode(s4Bin.Reverse());

                    }
                }
            }

            //if (this.Row == 0)
            //    System.Diagnostics.Debug.WriteLine(MagPage + " Subpage: " + TimeCode + "  A: " + CA + "  B: " + CB);

        }

        public void CalcParity()
        {
            Byte[] parityBitSetList = { 0, 3, 5, 6, 9, 10, 12, 15, 17, 18, 20, 23, 24, 27, 29, 30, 33, 34, 36, 39, 40, 43, 45, 46, 48, 51, 53, 54, 57, 58, 60, 63, 65, 66, 68, 71, 72, 75, 77, 78, 80, 83, 85, 86, 89, 90, 92, 95, 96, 99, 101, 102, 105, 106, 108, 111, 113, 114, 116, 119, 120, 123, 125, 126 };

            for (int n=0; n < Bytes.Length; n++)
            {
                if (parityBitSetList.Contains(Bytes[n]))
                {
                    Bytes[n] = (byte)(Bytes[n] | 0x80);
                }
            }
        }

        private Byte hammingEncode(String nybble)
        {
            //Not right, do bits need mirroring?
            String outByte = "";
            Boolean p1, p2, p3, p4, d1, d2, d3, d4;

            // Extract bits from string
            d1 = (nybble.Substring(0, 1) == "1") ? true : false;
            d2 = (nybble.Substring(1, 1) == "1") ? true : false;
            d3 = (nybble.Substring(2, 1) == "1") ? true : false;
            d4 = (nybble.Substring(3, 1) == "1") ? true : false;

            // Do Hamming
            p1 = true ^ d1 ^ d3 ^ d4;
            p2 = true ^ d1 ^ d2 ^ d4;
            p3 = true ^ d1 ^ d2 ^ d3;
            p4 = true ^ p1 ^ d1 ^ p2 ^ d2 ^ p3 ^ d3 ^ d4;

            // Assemble output Byte
            outByte += (d4) ? "1" : "0";
            outByte += (p4) ? "1" : "0";
            outByte += (d3) ? "1" : "0";
            outByte += (p3) ? "1" : "0";
            outByte += (d2) ? "1" : "0";
            outByte += (p2) ? "1" : "0";
            outByte += (d1) ? "1" : "0";
            outByte += (p1) ? "1" : "0";
            //outByte.Rev();

            /*outByte = "";
            outByte += (p1) ? "1" : "0";
            outByte += (d1) ? "1" : "0";
            outByte += (p2) ? "1" : "0";
            outByte += (d2) ? "1" : "0";
            outByte += (p3) ? "1" : "0";
            outByte += (d3) ? "1" : "0";
            outByte += (p4) ? "1" : "0";
            outByte += (d4) ? "1" : "0";*/

            return Convert.ToByte(outByte, 2);
        }

    }

    public enum LineTypes
    {
        Unknown,
        Blank,
        Header,
        Line,
        FastextDisplay,
        FastextLinks,
        Enhanced,
        DataServices
    }
}