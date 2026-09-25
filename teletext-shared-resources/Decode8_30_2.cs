using ExtensionMethods;
using System;

namespace TeletextSharedResources
{
    public class Decode8_30_2
    {
        private String initialPage;
        private String programmeIdentificationData;
        private String statusDisplay;

        public string InitialPage { get => initialPage; set => initialPage = value; }
        public string ProgrammeIdentificationData { get => programmeIdentificationData; set => programmeIdentificationData = value; }
        public string StatusDisplay { get => statusDisplay; set => statusDisplay = value; }

        public Decode8_30_2(byte[] packet)
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

            // Programme Identification Data
            programmeIdentificationData = "";
            for (int i = 13 - 4; i < 26 - 4; i++)
                programmeIdentificationData += packet[i].toHex(2);

            // Status Display
            byte[] bytStatusDisplay = new byte[20];
            Array.Copy(packet, 26 - 4, bytStatusDisplay, 0, 20);
            for (int n = 0; n < 20; n++)
                bytStatusDisplay[n] = Convert.ToByte(bytStatusDisplay[n] & 0x7f);
            statusDisplay = System.Text.Encoding.Default.GetString(bytStatusDisplay);
        }

    }
}
