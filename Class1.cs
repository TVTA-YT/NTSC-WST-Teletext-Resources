using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Teletext
{
    class Decode8_30_1
    {
        public Decode8_30_1(byte[] packet)
        {
            var decoded = new Decoded8_30_1();
            byte[] statusDisplay = new byte[19];
            Array.Copy(packet, 22, statusDisplay, 0, 19);
            decoded.StatusDisplay = System.Text.Encoding.Default.GetString(statusDisplay);
        }
        private struct Decoded8_30_1
        {
            public String InitialPage;
            public String NetworkIdent;
            public Int32 TimeOffset;
            public Int32 TimeOffsetPolarity;
            public Int32 ModifiedJulianDate;
            public DateTime UniversalTimeCoordinated;
            public String StatusDisplay;
        }

    }


}
