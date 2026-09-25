using System;
using System.Linq;

namespace TeletextSharedResources
{
    public class TeletextTools
    {
        public Byte DecodeMagazineNoFromMRAG(byte mrag1)
        {
            string binRev;
            var hr = new HammingResults84();
            hr = hr.HammingCheck84(mrag1);
            binRev = hr.CorrectedByte;

            if (hr.Status.Contains("reject"))
                return 255;
            else
            {

                //Construct return binary as MSB first
                string magBin = binRev.Substring(5, 1) + binRev.Substring(3, 1) + binRev.Substring(1, 1);

                byte magazine = Convert.ToByte(magBin, 2);

                if (magazine == 0)
                    magazine = 8;

                return Convert.ToByte(magazine);
            }
        }

        public Byte DecodePacketNoFromMRAG(byte mrag1, byte mrag2)
        {
            //Convert to binary string
            string binRev1;
            string binRev2;
            var hr1 = new HammingResults84();
            var hr2 = new HammingResults84();
            hr1 = hr1.HammingCheck84(mrag1);
            hr2 = hr2.HammingCheck84(mrag2);
            binRev1 = hr1.CorrectedByte;
            binRev2 = hr2.CorrectedByte;

            if (hr1.Status.Contains("reject") || hr2.Status.Contains("reject"))
                return 255;
            else
            {
                // Construct return binary as MSB first
                string rowBin = binRev2.Substring(7, 1) + binRev2.Substring(5, 1) + binRev2.Substring(3, 1) + binRev2.Substring(1, 1) + binRev1.Substring(7, 1);

                byte row = Convert.ToByte(rowBin, 2);
                return row;
            }
        }

        public Byte Decode84HammedValue(byte val)
        {
            //Convert to binary string
            string binRev = unHam84(val);

            // Construct return binary as MSB first
            string rowBin = binRev.Substring(7, 1) + binRev.Substring(5, 1) + binRev.Substring(3, 1) + binRev.Substring(1, 1);

            byte result = Convert.ToByte(rowBin, 2);
            return result;
        }

        private string unHam84(byte field, Boolean _useHamming = true)
        {
            //Convert to binary string
            string bin = Convert.ToString(field, 2);
            string binRev = "";
            string hammingStatus = "OK";

            // pad to 8 bits
            bin = bin.PadLeft(8, Convert.ToChar("0"));

            // reverse string as spec uses LSB first
            for (int i = bin.Length - 1; i > -1; i--)
            {
                binRev += bin.Substring(i, 1);
            }

            // apply Hamming error connection if enabled
            if (_useHamming)
            {
                Boolean parityA, parityB, parityC, parityD;
                String binRevA = Convert.ToString(field & Convert.ToByte("11000101", 2), 2);
                parityA = (binRevA.Replace("0", "").Length % 2 == 0);

                String binRevB = Convert.ToString(field & Convert.ToByte("01110001", 2), 2);
                parityB = (binRevB.Replace("0", "").Length % 2 == 0);

                String binRevC = Convert.ToString(field & Convert.ToByte("01011100", 2), 2);
                parityC = (binRevC.Replace("0", "").Length % 2 == 0);

                parityD = (binRev.Replace("0", "").Length % 2 == 0);

                if (parityA && parityB && parityC && !parityD)
                    hammingStatus = "Error in P4";

                if ((!parityA || !parityB || !parityC) && parityD)
                    hammingStatus = "Double Error";

                if ((!parityA || !parityB || !parityC) && !parityD)
                {
                    hammingStatus = "Single Error: " + binRev + " ";
                    if (!parityA && !parityB && !parityC)
                    {
                        hammingStatus += ": D1";
                        string subs = "0";
                        if (binRev.Substring(1, 1) == "0")
                            subs = "1";
                        binRev = binRev.Substring(0, 1) + subs + binRev.Substring(2);
                    }
                    if (parityA && !parityB && !parityC)
                    {
                        hammingStatus += ": D2";
                        string subs = "0";

                        if (binRev.Substring(3, 1) == "0")
                            subs = "1";
                        binRev = binRev.Substring(0, 3) + subs + binRev.Substring(4);
                    }

                    if (!parityA && parityB && !parityC)
                    {
                        hammingStatus += ": D3";
                        string subs = "0";
                        if (binRev.Substring(5, 1) == "0")
                            subs = "1";
                        binRev = binRev.Substring(0, 5) + subs + binRev.Substring(6);
                    }

                    if (!parityA && !parityB && parityC)
                    {
                        hammingStatus += ": D4";
                        string subs = "0";
                        if (binRev.Substring(7, 1) == "0")
                            subs = "1";
                        binRev = binRev.Substring(0, 7) + subs;
                    }

                    hammingStatus += " " + binRev;
                }
            }

            return binRev;
        }

        public byte HammingEncode84(string nybble)
        {
            return hammingEncode(nybble);
        }

        public byte HammingEncode84(byte b)
        {
            if (b < 128)
                return hammingEncode(Convert.ToString(b, 2));
            else
                throw new Exception("Cannot Hamming encode a byte greater than 127 using 8/4 Hamming.");
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

        public Byte[] EncodeMRAG(Int32 Magazine, Int32 Packet)
        {
            Byte[] result = new byte[2];
            String magBin = Convert.ToString((Byte)Magazine % 8, 2).PadLeft(3, Convert.ToChar("0"));
            String rowBin = Convert.ToString((Byte)Packet % 32, 2).PadLeft(5, Convert.ToChar("0"));

            //reverse bits to convert to transmission order
            String magBinRev = magBin.Substring(2, 1) + magBin.Substring(1, 1) + magBin.Substring(0, 1);
            String rowBinRev = rowBin.Substring(4, 1) + rowBin.Substring(3, 1) + rowBin.Substring(2, 1) + rowBin.Substring(1, 1) + rowBin.Substring(0, 1);

            String nybble1 = magBinRev + rowBinRev.Substring(0, 1);
            String nybble2 = rowBinRev.Substring(1);

            result[0] = HammingEncode84(nybble1);
            result[1] = HammingEncode84(nybble2);

            return result;
        }

        public Byte[] CalcParity(byte[] Bytes)
        {
            Byte[] parityBitSetList = { 0, 3, 5, 6, 9, 10, 12, 15, 17, 18, 20, 23, 24, 27, 29, 30, 33, 34, 36, 39, 40, 43, 45, 46, 48, 51, 53, 54, 57, 58, 60, 63, 65, 66, 68, 71, 72, 75, 77, 78, 80, 83, 85, 86, 89, 90, 92, 95, 96, 99, 101, 102, 105, 106, 108, 111, 113, 114, 116, 119, 120, 123, 125, 126 };

            for (int n = 0; n < Bytes.Length; n++)
            {
                if (parityBitSetList.Contains(Bytes[n]))
                {
                    Bytes[n] = (byte)(Bytes[n] | 0x80);
                }
            }

            return Bytes;
        }

        public Byte CalcParity(byte b)
        {
            Byte[] parityBitSetList = { 0, 3, 5, 6, 9, 10, 12, 15, 17, 18, 20, 23, 24, 27, 29, 30, 33, 34, 36, 39, 40, 43, 45, 46, 48, 51, 53, 54, 57, 58, 60, 63, 65, 66, 68, 71, 72, 75, 77, 78, 80, 83, 85, 86, 89, 90, 92, 95, 96, 99, 101, 102, 105, 106, 108, 111, 113, 114, 116, 119, 120, 123, 125, 126 };

            if (parityBitSetList.Contains(b))
            {
                b = (byte)(b | 0x80);
            }

            return b;
        }

    }


}