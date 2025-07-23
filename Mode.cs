using System;
using System.Drawing;

namespace TeletextSharedResources
{
    public struct Mode
    {
        public Color ForeColour;
        public Byte ForeColourCode;
        public Color BgndColour;
        public Byte BgndColourCode;
        public Boolean Graphics;
        public Boolean Flash;
        public Boolean Boxed;
        public Boolean DoubleHeight;
        public Boolean DoubleHeightFlag;
        public Boolean DoubleHeight2ndRow;
        public Boolean SeparatedGraphics;
        public Boolean Hold;
        public Boolean NewBgnd;
        public Color LastColour;
        public Byte LastColourCode;
        public Byte LastBit6;
        public Byte HeldGraphicsChar;
        public Boolean HoldIsSeparated;
        public Boolean Conceal;
        public Byte Character;
        public Byte BgndCLUT;
        public Byte ForeCLUT;
        public Boolean DoubleWidth;
        public Boolean DoubleSize;
        public Boolean Underlined;
        public String CharacterSet;
        public Boolean FullRowColour;
        public Byte Diacritical;
        public String FlashMode;
        public String FlashRateAndPhase;
        //public int FlashCurrentPhase;

        public void Default(Color? defaultBgnd = null)
        {
            ForeColour = Color.White;
            ForeColourCode = 7;
            BgndColour = defaultBgnd ?? Color.Black;
            BgndColourCode = 0;
            Graphics = false;
            Flash = false;
            Boxed = false;
            DoubleHeight = false;
            DoubleHeightFlag = false;
            SeparatedGraphics = false;
            NewBgnd = false;
            Hold = false;
            HeldGraphicsChar = 0x20;
            HoldIsSeparated = false;
            LastColour = Color.White;
            LastColourCode = 7;
            LastBit6 = 0;
            Conceal = false;
            BgndCLUT = 0;
            ForeCLUT = 0;
            DoubleWidth = false;
            DoubleSize = false;
            Underlined = false;
            CharacterSet = "G0";
            FullRowColour = false;
            Diacritical = 0;
        }
    }

}
