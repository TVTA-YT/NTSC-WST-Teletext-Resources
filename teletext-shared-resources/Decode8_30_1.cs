using System;

namespace TeletextSharedResources
{
    public class Decode8_30_1
    {
        private String initialPage;
        private String networkIdent;
        private Int32 timeOffset;
        private Int32 timeOffsetPolarity;
        private long modifiedJulianDate;
        private String universalTimeCoordinated;
        private String statusDisplay;

        public string InitialPage { get => initialPage; set => initialPage = value; }
        public string NetworkIdent { get => networkIdent; set => networkIdent = value; }
        public int TimeOffset { get => timeOffset; set => timeOffset = value; }
        public int TimeOffsetPolarity { get => timeOffsetPolarity; set => timeOffsetPolarity = value; }
        public long ModifiedJulianDate { get => modifiedJulianDate; set => modifiedJulianDate = value; }
        public string UniversalTimeCoordinated { get => universalTimeCoordinated; set => universalTimeCoordinated = value; }
        public string StatusDisplay { get => statusDisplay; set => statusDisplay = value; }

        public Decode8_30_1(byte[] packet)
        {
            // Initial Teletext Page
            TeletextTools t = new TeletextTools();
            int pu = t.Decode84HammedValue(packet[7 - 4]);
            int pt = t.Decode84HammedValue(packet[8 - 4]);
            int s1 = t.Decode84HammedValue(packet[9 - 4]);
            int s2 = t.Decode84HammedValue(packet[10 - 4]) & 7;
            int s3 = t.Decode84HammedValue(packet[11 - 4]);
            int s4 = t.Decode84HammedValue(packet[12 - 4]) & 3;
            int mag = (t.Decode84HammedValue(packet[10 - 4]) & 8) / 8 + (t.Decode84HammedValue(packet[12 - 4]) & 12) / 2;
            initialPage = mag.ToString() + pt.ToString() + pu.ToString() + ":" + s4.ToString() + Convert.ToString(s3, 16) + s2.ToString() + Convert.ToString(s1, 16);

            // Network ID code
            string network1 = Convert.ToString(packet[13 - 4], 16);
            string network2 = Convert.ToString(packet[14 - 4], 16);
            networkIdent = network1 + network2;

            //Time offset polarity
            if ((packet[15 - 4] | 128) == 128)
                timeOffsetPolarity = -1;
            else
                timeOffsetPolarity = 1;

            //Modified Julian Day
            string mjd1 = Convert.ToString(packet[16 - 4], 16).PadLeft(2, Convert.ToChar("0"));
            string mjd2 = Convert.ToString(packet[17 - 4], 16).PadLeft(2, Convert.ToChar("0"));
            string mjd3 = Convert.ToString(packet[18 - 4], 16).PadLeft(2, Convert.ToChar("0"));

            string mjd1a = (Convert.ToInt32(mjd1.Substring(0, 1), 16) - 1).ToString();
            string mjd1b = (Convert.ToInt32(mjd1.Substring(1, 1), 16) - 1).ToString();

            string mjd2a = (Convert.ToInt32(mjd2.Substring(0, 1), 16) - 1).ToString();
            string mjd2b = (Convert.ToInt32(mjd2.Substring(1, 1), 16) - 1).ToString();

            string mjd3a = (Convert.ToInt32(mjd3.Substring(0, 1), 16) - 1).ToString();
            string mjd3b = (Convert.ToInt32(mjd3.Substring(1, 1), 16) - 1).ToString();

            if (Convert.ToInt32(mjd1a) < 0) mjd1a = "0";
            if (Convert.ToInt32(mjd1b) < 0) mjd1b = "0";
            if (Convert.ToInt32(mjd2a) < 0) mjd2a = "0";
            if (Convert.ToInt32(mjd2b) < 0) mjd2b = "0";
            if (Convert.ToInt32(mjd3a) < 0) mjd3a = "0";
            if (Convert.ToInt32(mjd3b) < 0) mjd3b = "0";
            modifiedJulianDate = Convert.ToInt64(mjd1a + mjd1b + mjd2a + mjd2b + mjd3a + mjd3b);

            // UTC

            string h = Convert.ToString(packet[19 - 4], 16).PadLeft(2, Convert.ToChar("0"));
            string m = Convert.ToString(packet[20 - 4], 16).PadLeft(2, Convert.ToChar("0"));
            string s = Convert.ToString(packet[21 - 4], 16).PadLeft(2, Convert.ToChar("0"));

            string ht = (Convert.ToInt32(h.Substring(0, 1), 16) - 1).ToString();
            string hu = (Convert.ToInt32(h.Substring(1, 1), 16) - 1).ToString();

            string mt = (Convert.ToInt32(m.Substring(0, 1), 16) - 1).ToString();
            string mu = (Convert.ToInt32(m.Substring(1, 1), 16) - 1).ToString();

            string st = (Convert.ToInt32(s.Substring(0, 1), 16) - 1).ToString();
            string su = (Convert.ToInt32(s.Substring(1, 1), 16) - 1).ToString();

            universalTimeCoordinated = ht.Substring(0, 1) + hu.Substring(0, 1) + ":" + mt.Substring(0, 1) + mu.Substring(0, 1) + "/" + st.Substring(0, 1) + su.Substring(0, 1);

            // Status Display
            byte[] bytStatusDisplay = new byte[20];
            Array.Copy(packet, 26-4, bytStatusDisplay, 0, 20);
            for (int n = 0; n < 20; n++)
                bytStatusDisplay[n] = Convert.ToByte(bytStatusDisplay[n] & 0x7f);           
            statusDisplay = System.Text.Encoding.Default.GetString(bytStatusDisplay);
        }

    }


}
