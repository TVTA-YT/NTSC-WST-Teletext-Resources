using ExtensionMethods;
using System;

namespace TeletextSharedResources
{
    public class HammingResults84
    {
        public String CorrectedByte;
        public String Status;
        public String ValueBin;
        public String ValueHex;
        public Byte Value;

        public HammingResults84 HammingCheck84(byte field)
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
            if (true)
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
                    hammingStatus = "Error in P4, but accept data";

                if ((!parityA || !parityB || !parityC) && parityD)
                    hammingStatus = "Double Error, reject data";

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

            HammingResults84 hr = new HammingResults84();
            hr.CorrectedByte = binRev;
            hr.Status = hammingStatus;
            hr.ValueBin = binRev.Substring(7, 1) + binRev.Substring(5, 1) + binRev.Substring(3, 1) + binRev.Substring(1, 1);
            hr.Value = Convert.ToByte(hr.ValueBin, 2);
            hr.ValueHex = hr.Value.toHex(1);

            return hr;
        }

    }


}