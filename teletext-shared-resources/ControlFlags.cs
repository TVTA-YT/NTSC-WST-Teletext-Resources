using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeletextSharedResources
{
    public class ControlFlags
    {
        public Boolean C4_Erase;
        public Boolean C5_Newsflash;
        public Boolean C6_Subtitle;
        public Boolean C7_SuppressHeader;
        public Boolean C8_Update;
        public Boolean C9_InterruptedSequence;
        public Boolean C10_InhibitDisplay;
        public Boolean C11_MagazineSerial;
        public Boolean C12;
        public Boolean C13;
        public Boolean C14;

        public ControlFlags()
        {
            C4_Erase = false;
            C5_Newsflash = false;
            C6_Subtitle = false;
            C7_SuppressHeader = false;
            C8_Update = false;
            C9_InterruptedSequence = false;
            C10_InhibitDisplay = false;
            C11_MagazineSerial = false;
            C12 = false;
            C13 = false;
            C14 = false;
        }
    }

}
