using System;

namespace TeletextSharedResources
{
    public class FastextState
    {
        public FastextLink Red = new FastextLink();
        public FastextLink Green = new FastextLink();
        public FastextLink Yellow = new FastextLink();
        public FastextLink Blue = new FastextLink();
        public FastextLink Four = new FastextLink();
        public FastextLink Five = new FastextLink();

        private class SubcodeMag
        {
            public String Subcode = "3F7F";
            public String MagModifier = "000";
        }

        public FastextState()
        {
            Red.Page = "1FF";
            Green.Page = "1FF";
            Yellow.Page = "1FF";
            Blue.Page = "1FF";
            Four.Page = "1FF";
            Five.Page = "1FF";

            Red.Subcode = "3F7F";
            Green.Subcode = "3F7F";
            Yellow.Subcode = "3F7F";
            Blue.Subcode = "3F7F";
            Four.Subcode = "3F7F";
            Five.Subcode = "3F7F";
        }
        public void Decode(Byte[] bytes, Int32 mag)
        {
            //Check that the designation code is between 0 and 3
            HammingResults84 dcResults = new HammingResults84();
            dcResults = dcResults.HammingCheck84(bytes[2]);

            if (dcResults.Value <= 3)
            {
                SubcodeMag RedScm;
                Red.Page = GetPageTens(0, bytes) + GetPageUnits(0, bytes);
                RedScm = GetSubcode(0, bytes);
                Red.Subcode = RedScm.Subcode;
                Red.Page = (mag ^ Convert.ToByte(RedScm.MagModifier, 2)).ToString() + Red.Page;
                if (Red.Page.Substring(0, 1) == "0")
                    Red.Page = "8" + Red.Page.Substring(1);

                SubcodeMag GreenScm;
                Green.Page = GetPageTens(1, bytes) + GetPageUnits(1, bytes);
                GreenScm = GetSubcode(1, bytes);
                Green.Subcode = GreenScm.Subcode;
                Green.Page = (mag ^ Convert.ToByte(GreenScm.MagModifier, 2)).ToString() + Green.Page;
                if (Green.Page.Substring(0, 1) == "0")
                    Green.Page = "8" + Green.Page.Substring(1);

                SubcodeMag YellowScm;
                Yellow.Page = GetPageTens(2, bytes) + GetPageUnits(2, bytes);
                YellowScm = GetSubcode(2, bytes);
                Yellow.Subcode = YellowScm.Subcode;
                Yellow.Page = (mag ^ Convert.ToByte(YellowScm.MagModifier, 2)).ToString() + Yellow.Page;
                if (Yellow.Page.Substring(0, 1) == "0")
                    Yellow.Page = "8" + Yellow.Page.Substring(1);

                SubcodeMag BlueScm;
                Blue.Page = GetPageTens(3, bytes) + GetPageUnits(3, bytes);
                BlueScm = GetSubcode(3, bytes);
                Blue.Subcode = BlueScm.Subcode;
                Blue.Page = (mag ^ Convert.ToByte(BlueScm.MagModifier, 2)).ToString() + Blue.Page;
                if (Blue.Page.Substring(0, 1) == "0")
                    Blue.Page = "8" + Blue.Page.Substring(1);

                SubcodeMag FourScm;
                Four.Page = GetPageTens(4, bytes) + GetPageUnits(4, bytes);
                FourScm = GetSubcode(4, bytes);
                Four.Subcode = FourScm.Subcode;
                Four.Page = (mag ^ Convert.ToByte(FourScm.MagModifier, 2)).ToString() + Four.Page;
                if (Four.Page.Substring(0, 1) == "0")
                    Four.Page = "8" + Four.Page.Substring(1);

                SubcodeMag FiveScm;
                Five.Page = GetPageTens(5, bytes) + GetPageUnits(5, bytes);
                FiveScm = GetSubcode(5, bytes);
                Five.Subcode = FiveScm.Subcode;
                Five.MagModifier = FiveScm.MagModifier;
                Five.Page = (mag ^ Convert.ToByte(FiveScm.MagModifier, 2)).ToString() + Five.Page;
                if (Five.Page.Substring(0, 1) == "0")
                    Five.Page = "8" + Five.Page.Substring(1);
            }

        }

        private String GetPageUnits(Int32 linkNo, Byte[] bytes)
        {
            HammingResults84 x = new HammingResults84();
            x = x.HammingCheck84(bytes[(6 * linkNo) + 3]);
            return x.ValueHex.ToUpper();
        }

        private String GetPageTens(Int32 linkNo, Byte[] bytes)
        {
            HammingResults84 x = new HammingResults84();
            x = x.HammingCheck84(bytes[(6 * linkNo) + 4]);
            return x.ValueHex.ToUpper();
        }



        private SubcodeMag GetSubcode(Int32 linkNo, Byte[] bytes)
        {
            HammingResults84 hrS1 = new HammingResults84();
            hrS1 = hrS1.HammingCheck84(bytes[(6 * linkNo) + 5]);
            String binS1 = hrS1.ValueBin;

            HammingResults84 hrS2 = new HammingResults84();
            hrS2 = hrS2.HammingCheck84(bytes[(6 * linkNo) + 6]);
            String binS2 = hrS1.ValueBin.Substring(1, 3);
            String M1 = hrS2.ValueBin.Substring(0, 1);

            HammingResults84 hrS3 = new HammingResults84();
            hrS3 = hrS3.HammingCheck84(bytes[(6 * linkNo) + 7]);
            String binS3 = hrS3.ValueBin;

            HammingResults84 hrS4 = new HammingResults84();
            hrS4 = hrS4.HammingCheck84(bytes[(6 * linkNo) + 8]);
            String binS4 = hrS4.ValueBin.Substring(2, 2);
            String M2 = hrS4.ValueBin.Substring(1, 1);
            String M3 = hrS4.ValueBin.Substring(0, 1);

            SubcodeMag sm = new SubcodeMag();
            sm.Subcode = (hrS4.ValueHex + hrS3.ValueHex + hrS2.ValueHex + hrS1.ValueHex).ToUpper();
            sm.MagModifier = M1 + M2 + M3;

            return sm;
        }
    }

    public class FastextLink
    {
        public String Page = "1FF";
        public String Subcode = "3F7F";
        public String MagModifier = "000";
    }

}
