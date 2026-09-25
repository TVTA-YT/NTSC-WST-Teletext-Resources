using System;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Collections;
using ExtensionMethods;
using System.Drawing.Drawing2D;
using System.Net;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TeletextSharedResources

{
    public class TeletextRenderer
    {
        private bool zeroBlack = false;
        public bool ZeroBlack
        {
            get
            {
                return zeroBlack;
            }
            set
            {
                zeroBlack = value;
            }
        }

        private bool _doubleHeightAndPageNotErasedStrictInterpretation = false;
        public bool DoubleHeightStrict
        {
            get
            {
                return _doubleHeightAndPageNotErasedStrictInterpretation;
            }
            set
            {
                _doubleHeightAndPageNotErasedStrictInterpretation = value;
            }
        }

        private bool _revealPressed = false;
        public bool RevealPressed
        {
            get
            {
                return _revealPressed;
            }
            set
            {
                _revealPressed = value;
            }
        }

        private bool _flashTextOn = true;
        public bool FlashTextOn
        {
            get
            {
                return _flashTextOn;
            }
            set
            {
                _flashTextOn = value;
            }
        }

        private Double _presentationLevel = 1.5;
        public Double PresentationLevel
        {
            get
            {
                return _presentationLevel;
            }
            set
            {
                _presentationLevel = value;
            }
        }

        private bool _showLevelFour = true;

         public bool ShowLevelFour
        {
            get
            {
                return _showLevelFour;
            }
            set
            {
                _showLevelFour = value;
            }
        }

        private bool _saveX26Anim = false;
        public bool SaveX26Anim
        {
            get
            {
                return _saveX26Anim;
            }
            set
            {
                _saveX26Anim = value;
            }
        }

        private string _nationalOptionBits = "";
        public string NationalOptionSelectionBits
        {
            get
            {
                return _nationalOptionBits;
            }
            set
            {
                _nationalOptionBits = value;
            }
        }

        private string _fontName = "Mullard";
        public string Font
        {
            get
            {
                return _fontName;
            }
            set
            {
                _fontName = value;
                font = ReadDefaultFont(value + "Font.char");
            }
        }

        private bool _mix = false;
        public bool Mix
        {
            get { return _mix; }
            set { _mix = value; }
        }

        private bool _suppress = false;
        public bool Suppress
        {
            get { return _suppress;  }
            set { _suppress = value;  }
        }

        private float _deviceDPI = 96.0f;
        public float DeviceDPI
        {
            get { return _deviceDPI; }
            set { _deviceDPI = value; }
        }

        private Hashtable mullard = new Hashtable();
        private Hashtable font = new Hashtable();
        private Hashtable mullard24 = new Hashtable();

        // Set default Active Position
        Int32 activeX = 0, activeY = 0;

        // Set up default colour definitions
        public Int32[,] ColourMap = new Int32[4, 8] {  { 0x000, 0xf00, 0x0f0, 0xff0, 0x00f, 0xf0f, 0x0ff, 0xfff },
                                            { 0x1000, 0x700, 0x070, 0x770, 0x007, 0x707, 0x077, 0x777 },
                                            { 0xf05, 0xf70, 0x0f7, 0xffb, 0x0ca, 0x500, 0x652, 0xb77 },
                                            { 0x333, 0xf77, 0x7f7, 0xff7, 0x77f, 0xf7f, 0x7ff, 0xddd }
                                         };

        private String colourTableRemapping = "000";

        private Color defaultBackgroundColour = Color.Black;

        String currentlySelectedThumbnail = "";

        private int level1XStart = 100;
        private int level1YStart = 60;
        private bool _borders;

                    // Load Triplet Functions
            IDictionary<string, EnhancedDesc> tripletRowFunc = new Dictionary<string, EnhancedDesc>();
            IDictionary<string, EnhancedDesc> tripletColumnFunc = new Dictionary<string, EnhancedDesc>();

        private int debugTotalTripletLimit = 255;
        private int debugTotalTripletCount = 0;

        private Boolean L4debug = false;

        private float GetDeviceDPI()
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                return g.DpiX;
            }
        }

        private float dpiScale = 1;

        // Default contructor
        public TeletextRenderer(bool Borders = false)
        {
            _deviceDPI = GetDeviceDPI();
            dpiScale = _deviceDPI / 96.0f;
            /*switch (_fontName)
            {
                case "Mullard":
                    {
                        font = ReadDefaultFont("MullardFont.char");
                        break;
                    }
                case "Philips":
                    {
                        font = ReadDefaultFont("PhilipsFont.char");
                        break;
                    }
                default:
                    {
                        font = ReadDefaultFont("MullardFont.char");
                        break;
                    }
            }*/
            font = ReadDefaultFont("MullardFont.char");
            _borders = Borders;

            if (Borders)
            {
                level1XStart = 100;
                level1YStart = 60;
            }
            else
            {
                level1XStart = 0;
                level1YStart = 0;
            }

            tripletRowFunc.Add("00000", new EnhancedDesc("Full Screen Colour", 2));
            tripletRowFunc.Add("00001", new EnhancedDesc("Full Row Colour", 2));
            tripletRowFunc.Add("00010", new EnhancedDesc("Reserved", 2));
            tripletRowFunc.Add("00011", new EnhancedDesc("Reserved", 2));
            tripletRowFunc.Add("00100", new EnhancedDesc("Set Active Position", 1.5));
            tripletRowFunc.Add("00101", new EnhancedDesc("Reserved", 2));
            tripletRowFunc.Add("00110", new EnhancedDesc("Reserved", 2));
            tripletRowFunc.Add("00111", new EnhancedDesc("Address Display Row 0", 1.5));
            tripletRowFunc.Add("01000", new EnhancedDesc("PDC - Country of Origin and Programme Source", 1.5));
            tripletRowFunc.Add("01001", new EnhancedDesc("PDC - Month & Day", 1.5));
            tripletRowFunc.Add("01010", new EnhancedDesc("PDC - Cursor Row & Announced Starting Time Hours", 1.5));
            tripletRowFunc.Add("01011", new EnhancedDesc("PDC - Cursor Row & Announce Finishing Time Hours", 1.5));
            tripletRowFunc.Add("01100", new EnhancedDesc("PDC - Cursor Row & Local Time Offset", 1.5));
            tripletRowFunc.Add("01101", new EnhancedDesc("PDC - Series Identifier and Series Code", 1.5));
            tripletRowFunc.Add("01110", new EnhancedDesc("Reserved", 2));
            tripletRowFunc.Add("01111", new EnhancedDesc("Reserved", 2));
            tripletRowFunc.Add("10000", new EnhancedDesc("Origin Modifier", 2));
            tripletRowFunc.Add("10001", new EnhancedDesc("Active Object Invocation", 2));
            tripletRowFunc.Add("10010", new EnhancedDesc("Adaptive Object Invocation", 2));
            tripletRowFunc.Add("10011", new EnhancedDesc("Passive Object Invocation", 2));
            tripletRowFunc.Add("10100", new EnhancedDesc("Reserved", 2));
            tripletRowFunc.Add("10101", new EnhancedDesc("Active Object Definition", 2));
            tripletRowFunc.Add("10110", new EnhancedDesc("Adaptive Object Definition", 2));
            tripletRowFunc.Add("10111", new EnhancedDesc("Passive Object Definition", 2));
            tripletRowFunc.Add("11000", new EnhancedDesc("DRCS Mode", 2));
            tripletRowFunc.Add("11001", new EnhancedDesc("Reserved", 2));
            tripletRowFunc.Add("11010", new EnhancedDesc("Reserved", 2));
            tripletRowFunc.Add("11011", new EnhancedDesc("Reserved", 2));
            tripletRowFunc.Add("11100", new EnhancedDesc("Reserved", 2));
            tripletRowFunc.Add("11101", new EnhancedDesc("Reserved", 2));
            tripletRowFunc.Add("11110", new EnhancedDesc("Reserved", 2));
            tripletRowFunc.Add("11111", new EnhancedDesc("Termination Marker", 1.5));

            tripletColumnFunc.Add("00000", new EnhancedDesc("Foreground Colour", 2));
            tripletColumnFunc.Add("00001", new EnhancedDesc("Block Mosaic Character from the G1 set", 2));
            tripletColumnFunc.Add("00010", new EnhancedDesc("Line Drawing or Smoothed Mosaic Character from the G3 set (Level 1.5)", 1.5));
            tripletColumnFunc.Add("00011", new EnhancedDesc("Background Colour", 2));
            tripletColumnFunc.Add("00100", new EnhancedDesc("Reserved", 2));
            tripletColumnFunc.Add("00101", new EnhancedDesc("Reserved", 2));
            tripletColumnFunc.Add("00110", new EnhancedDesc("PDC - Cursor Column & Announced Starting & Finishing Time Minutes", 1.5));
            tripletColumnFunc.Add("00111", new EnhancedDesc("Additional Flash Functions", 2));
            tripletColumnFunc.Add("01000", new EnhancedDesc("Modified G0 and G2 Character Set Design.", 2));
            tripletColumnFunc.Add("01001", new EnhancedDesc("Character from the G0 set (Levels 2.5 & 3.5)", 2));
            tripletColumnFunc.Add("01010", new EnhancedDesc("Reserved", 2));
            tripletColumnFunc.Add("01011", new EnhancedDesc("Line Drawing or Smoothed Mosaic Character from the G3 set (Levels 2.5 & 3.5)", 2));
            tripletColumnFunc.Add("01100", new EnhancedDesc("Display Attributes", 2));
            tripletColumnFunc.Add("01101", new EnhancedDesc("DRCS Character Invocation", 2));
            tripletColumnFunc.Add("01110", new EnhancedDesc("Font Style", 3.5));
            tripletColumnFunc.Add("01111", new EnhancedDesc("Character from the G2 set", 1.5));
            tripletColumnFunc.Add("10000", new EnhancedDesc("G0 character without diacritical mark", 1.5));
            tripletColumnFunc.Add("10001", new EnhancedDesc("G0 character with diacritical mark", 1.5));
            tripletColumnFunc.Add("10010", new EnhancedDesc("G0 character with diacritical mark", 1.5));
            tripletColumnFunc.Add("10011", new EnhancedDesc("G0 character with diacritical mark", 1.5));
            tripletColumnFunc.Add("10100", new EnhancedDesc("G0 character with diacritical mark", 1.5));
            tripletColumnFunc.Add("10101", new EnhancedDesc("G0 character with diacritical mark", 1.5));
            tripletColumnFunc.Add("10110", new EnhancedDesc("G0 character with diacritical mark", 1.5));
            tripletColumnFunc.Add("10111", new EnhancedDesc("G0 character with diacritical mark", 1.5));
            tripletColumnFunc.Add("11000", new EnhancedDesc("G0 character with diacritical mark", 1.5));
            tripletColumnFunc.Add("11001", new EnhancedDesc("G0 character with diacritical mark", 1.5));
            tripletColumnFunc.Add("11010", new EnhancedDesc("G0 character with diacritical mark", 1.5));
            tripletColumnFunc.Add("11011", new EnhancedDesc("G0 character with diacritical mark", 1.5));
            tripletColumnFunc.Add("11100", new EnhancedDesc("G0 character with diacritical mark", 1.5));
            tripletColumnFunc.Add("11101", new EnhancedDesc("G0 character with diacritical mark", 1.5));
            tripletColumnFunc.Add("11110", new EnhancedDesc("G0 character with diacritical mark", 1.5));
            tripletColumnFunc.Add("11111", new EnhancedDesc("G0 character with diacritical mark", 1.5));

        }

        private Hashtable ReadDefaultFont(String strContains)
        {
            Hashtable output = new Hashtable();
            Stream imgStream = null;
            Bitmap bmp = null;
            Assembly a = Assembly.GetCallingAssembly();
            String[] resources = typeof(TeletextSharedResources.Service).Assembly.GetManifestResourceNames();

            foreach (string s in resources)
            {
                if (s.EndsWith(".bmp") && s.Contains(strContains))
                {
                    imgStream = a.GetManifestResourceStream(s);
                    imgStream = typeof(TeletextSharedResources.Service).Assembly.GetManifestResourceStream(s);
                    if (imgStream != null)
                    {
                        bmp = Bitmap.FromStream(imgStream) as Bitmap;
                        if (bmp != null)
                        {
                            String index;
                            index = s.Substring(s.IndexOf("char") + 4);
                            index = index.Substring(0, index.Length - 4);
                            output[index] = bmp;
                            //System.Diagnostics.Debug.Write("char" + index);
                        }
                        bmp = null;
                        imgStream.Close();
                        imgStream = null;
                    }
                }
            }

            return output;
        }

        public RenderedLayersNova DrawChar(ref RenderedLayersNova layers, ref Page page, Image chr, Int32 xPos, Int32 yPos, String Options = "", Boolean doubleHeight = false, Boolean doubleWidth = false, bool debug = true)
        //public RenderedLayersNova DrawChar(ref RenderedLayersNova layers, ref Page page, Image chr, Int32 xPos, Int32 yPos, String Options = "", bool debug = true)
        {
            Graphics grForeground = Graphics.FromImage(layers.Foreground);


            if (chr == null)
            {
                System.Diagnostics.Debug.Print("");
            }
            else
            {
                //Get the colour of the char from the L2 modeMap

                chr.Palette = GetColour(page, xPos, yPos, chr.Palette);

                if (debug)
                    System.Diagnostics.Debug.WriteLine("BG Clut: {2}, BG Colour: {3}, BG aRGB: {4}\nFG Clut: {5}, FG Colour: {6}, FG aRGB: {7}\n", xPos, yPos, page.modeMapL2[yPos, xPos].BgndCLUT, page.modeMapL2[yPos, xPos].BgndColourCode, chr.Palette.Entries[0].ToString(), page.modeMapL2[yPos, xPos].ForeCLUT, page.modeMapL2[yPos, xPos].ForeColourCode, chr.Palette.Entries[1].ToString());

                //grForeground.DrawImage(chr, (xPos * 12) + level1XStart, (yPos * 20) + level1YStart, (doubleWidth ? chr.Width * 2 : chr.Width), (doubleHeight ? chr.Height * 2 : chr.Height));
                grForeground.DrawImage(chr, ((xPos * 12) + level1XStart) * dpiScale, ((yPos * 20) + level1YStart) * dpiScale, chr.Width * dpiScale, chr.Height * dpiScale);
            }
            return layers;
        }

        private ColorPalette GetColour(Page page, int xPos, int yPos, ColorPalette pal)
        {

            // Background colour

            if (page.modeMapL2[yPos, xPos].BgndCLUT == 0 && page.modeMapL2[yPos, xPos].BgndColourCode == 0)
                pal.Entries[0] = defaultBackgroundColour;
            else
                pal.Entries[0] = ColourLookup(page.modeMapL2[yPos, xPos].BgndCLUT, page.modeMapL2[yPos, xPos].BgndColourCode, "BG", page.modeMapL2[yPos, xPos].FullRowColour);

            //Foreground colour

            pal.Entries[1] = ColourLookup(page.modeMapL2[yPos, xPos].ForeCLUT, page.modeMapL2[yPos, xPos].ForeColourCode, "FG", page.modeMapL2[yPos, xPos].FullRowColour);

            return pal;
        }

        public RenderedLayersNova Render(Page page, bool overrideSuppressHeader = false, bool overrideInhibitDisplay = false)
        {
            if (page != null)
            {
                //RenderedLayers output = new RenderedLayers();

                // need to chop out each rendered line and add to the layers.  Or somehow superimpose each rendered line layer onto the page layer
                // possibly set black as transparent?

                // Restore defaults
                defaultBackgroundColour = Color.Black;
                ColourMap = new Int32[4, 8] {  { 0x000, 0xf00, 0x0f0, 0xff0, 0x00f, 0xf0f, 0x0ff, 0xfff },
                                            { 0x1000, 0x700, 0x070, 0x770, 0x007, 0x707, 0x077, 0x777 },
                                            { 0xf05, 0xf70, 0x0f7, 0xffb, 0x0ca, 0x500, 0x652, 0xb77 },
                                            { 0x333, 0xf77, 0x7f7, 0xff7, 0x77f, 0xf7f, 0x7ff, 0xddd }
                                         };

                colourTableRemapping = "000";

                RenderedLayersNova layers = new RenderedLayersNova(_deviceDPI, _borders, _mix || page.Lines[0].Flags.C6_Subtitle || page.Lines[0].Flags.C5_Newsflash);

                Graphics grForeground, grBackground;
                Mode mode;
                string nationalOptionHex;
                bool doubleHeightAbove;
                debugTotalTripletCount = 0;

                if (_saveX26Anim)
                {
                    //Clear folder
                    Directory.CreateDirectory(System.IO.Path.GetTempPath() + "\\teletext");
                    Directory.CreateDirectory(System.IO.Path.GetTempPath() + "\\teletext\\triplets");
                    DirectoryInfo di = new DirectoryInfo(Environment.GetEnvironmentVariable("temp") + "\\teletext\\triplets");
                    try
                    {
                        foreach (FileInfo fi in di.GetFiles())
                            fi.Delete();
                    }
                    catch
                    {
                        System.Diagnostics.Debug.WriteLine("Anim directory not found.");
                    }

                }

                //if (zeroBlack || _presentationLevel > 1)
                //    layers.Transparency = Color.FromArgb(255, 1, 1, 1);
                //else
                layers.Transparency = Color.Black;


                //layers.Background.Save(System.IO.Path.GetTempPath() + "\\back.png");
                //layers.Foreground.Save(System.IO.Path.GetTempPath() + "\\fore.png");


                //foreground = layers.Foreground;
                //grForeground = Graphics.FromImage(foreground);
                //grForeground.Clear(transparency);

                //background = layers.Background;
                //grBackground = Graphics.FromImage(background);
                //grBackground.Clear(Color.FromArgb(255, 2, 2, 2));

                layers.Mode = new Mode();
                layers.Mode.Default(defaultBackgroundColour);

                doubleHeightAbove = false;

                // Get pre-load data for L2.5 and up
                if (this.PresentationLevel > 1.5)
                    NonDisplayablePackets(page, layers, "Pre");

                for (Int32 y = 0; y < 25; y++)
                {
                    bool displayLine = false;

                    if (y == 0)
                    {
                        if (!page.Lines[0].Flags.C7_SuppressHeader || overrideSuppressHeader)
                            displayLine = true;
                    }

                    if (y > 0)
                    {
                        if (!page.Lines[0].Flags.C10_InhibitDisplay || overrideInhibitDisplay)
                            displayLine = true;
                    }

                    if (displayLine)
                        layers = RenderLine(page, y, layers);
                    //layers.Foreground.Save(y.ToString() + ".bmp");
                }

                //layers.Background.Save(System.IO.Path.GetTempPath() + "\\back.png");
                //layers.Foreground.Save(System.IO.Path.GetTempPath() + "\\fore.png");

                if (_saveX26Anim)
                {
                    Bitmap b = new Bitmap(layers.Background);

                    using (Graphics g = Graphics.FromImage(b))
                    {
                        g.DrawImage(layers.Foreground, 0, 0);
                    }

                    b.Save(Environment.GetEnvironmentVariable("temp") + "\\teletext\\triplets\\triplet000.png");
                }

                // Level 2.5/3.5 support
                if (this.PresentationLevel >= 1.5 && this.PresentationLevel != 4)
                    NonDisplayablePackets(page, layers, "Post");

                if (this.PresentationLevel == 4)
                    DecodeLevelFour(page, layers);

                if (L4debug)
                {
                    layers.Background.Save(Environment.GetEnvironmentVariable("temp") + "\\teletext\\background.png");
                    layers.Foreground.Save(Environment.GetEnvironmentVariable("temp") + "\\teletext\\foreground.png");
                }

                return layers;
            }
            else
            {
                System.Diagnostics.Debug.Write("Null page");
                return new RenderedLayersNova(_deviceDPI);
            }
        }


        public RenderedLayersNova RenderLine(Page page, Int32 y, RenderedLayersNova layers)
        {
            //layers.Foreground.Save(Environment.GetEnvironmentVariable("temp") + "\\teletext\\renderer1-" + y.ToString() + ".png");
            //
            int charWidth = 12;
            int charHeight = 20;
            // Initialise modeMap
            for (Int32 x = 0; x < 40; x++)
            {
                page.modeMap[y, x] = new Mode();
                page.modeMapL2[y, x] = new Mode();
            }

            layers.Mode = new Mode();
            layers.Mode.Default(defaultBackgroundColour);

            //layers.Foreground.Save(Environment.GetEnvironmentVariable("temp") + "\\teletext\\renderer2-" + y.ToString() + ".png");
            //if (zeroBlack || _presentationLevel > 1 )
            //    layers.Transparency = Color.FromArgb(255, 1, 1, 1);
            //else
            layers.Transparency = Color.Black;

            // Lookup the packet for the incoming row number
            Int32 lineNo = -1;
            lineNo = page.GetPacketIndex(y);

            Boolean doubleHeightAbove = false;
            if (y > 0)
            {
                for (Int32 x = 0; x < 40; x++)
                    if (page.modeMap[y - 1, x].DoubleHeight)
                        doubleHeightAbove = true;
            }

            // Find out what national option is selected
            // Make a binary string out of the flags we have

            // Triplet 1 decoding to go here - placeholder of zeroes for now
            string nationalOptionHex = "";
            if (y == 0)
            {
                if (_nationalOptionBits == "")
                    _nationalOptionBits = "0000" + (page.Lines[0].Flags.C12 == true ? "1" : "0") + (page.Lines[0].Flags.C13 == true ? "1" : "0") + (page.Lines[0].Flags.C14 == true ? "1" : "0");
                nationalOptionHex = Convert.ToString(Convert.ToByte(_nationalOptionBits, 2), 16).PadLeft(2, Convert.ToChar("0"));
            }


            Graphics grForeground = Graphics.FromImage(layers.Foreground);

            Graphics grBackground = Graphics.FromImage(layers.Background);
            //layers.Foreground.Save(Environment.GetEnvironmentVariable("temp") + "\\teletext\\renderer3-" + y.ToString() + ".png");
            //int lineLength = page.Lines[lineNo].Text.Length;

            Boolean doubleHeightSet = false;
            Int32 initialDoubleWidthX = -1;

            if (lineNo != -1)
            {
                if (!doubleHeightAbove)
                {
                    // Clear this line before rendering
                    Brush clearBrush = new SolidBrush(layers.Transparency);
                    grForeground.FillRectangle(clearBrush, (0 + level1XStart) * dpiScale, ((y * charHeight) + level1YStart) * dpiScale, (40 * charWidth) * dpiScale, (charHeight * dpiScale));
                    //layers.Foreground.Save(Environment.GetEnvironmentVariable("temp") + "\\teletext\\renderer4-" + y.ToString() + ".png");
                    if (page.Lines[lineNo].Text.Length > 40)
                    {
                        System.Diagnostics.Debug.WriteLine("P" + page.Lines[0].MagPage + " line " + lineNo + " > 40: " + page.Lines[lineNo].Text);
                        page.Lines[lineNo].Text = page.Lines[lineNo].Text.Substring(0, 40);
                    }


                    //layers.Background.Save(System.IO.Path.GetTempPath() + "\\back.png");
                    //layers.Foreground.Save(System.IO.Path.GetTempPath() + "\\fore.png");

                    for (int x = 0; x < page.Lines[lineNo].Text.Length; x++)
                    {

                        byte bytASCII;
                        Boolean isControlChar = false;

                        char chrASCII = Convert.ToChar(page.Lines[lineNo].Text.Substring(x, 1));
                        bytASCII = Convert.ToByte(chrASCII);
                        layers.Mode.Character = bytASCII;

                        // If mode is "Set At" then change the mode before the character is printed
                        if (bytASCII < 0x20)
                        {
                            isControlChar = true;
                            if (IsSetAt(bytASCII))
                                layers.Mode = ChangeMode(bytASCII, layers.Mode);
                        }

                        if ((bytASCII >= 0x20 && bytASCII <= 0x3f) || (bytASCII >= 0x60))
                        // Blast-through text: codes $4x and $5x are displayed as letters even in graphics mode
                        // so only switch the character to graphics if char is not control code or blast through code
                        {
                            if (layers.Mode.Graphics && !layers.Mode.SeparatedGraphics)
                                bytASCII += 0x60;

                            if (layers.Mode.Graphics && layers.Mode.SeparatedGraphics)
                                bytASCII += 0x80;

                            if (layers.Mode.Graphics)
                            {
                                layers.Mode.HeldGraphicsChar = bytASCII;
                            }
                        }

                        string hexASCII = Convert.ToString(bytASCII, 16);

                        Image chr;
                        Image bgndChr = (Image)font["0080"];
                        //layers.Foreground.Save(Environment.GetEnvironmentVariable("temp") + "\\teletext\\renderer5-" + y.ToString() + ".png");
                        if (!isControlChar)
                        {
                            //

                            // Add in the national option prefix
                            if (font[nationalOptionHex + hexASCII] != null)
                                chr = (Image)font[nationalOptionHex + hexASCII];
                            else
                                chr = (Image)font[hexASCII.PadLeft(4, Convert.ToChar("0"))];
                        }
                        else
                        {
                            if (!layers.Mode.Hold || layers.Mode.HeldGraphicsChar == 0)
                                chr = (Image)font["0020"];
                            else
                                chr = (Image)font[Convert.ToString(layers.Mode.HeldGraphicsChar, 16).PadLeft(4, Convert.ToChar("0"))];
                        }

                        ColorPalette pal = chr.Palette;

                        pal.Entries[0] = layers.Transparency;
                        pal.Entries[1] = layers.Mode.ForeColour;
                        chr.Palette = pal;

                        ColorPalette palBgnd = bgndChr.Palette;
                        palBgnd.Entries[0] = (layers.Mode.Boxed || !(_mix || page.Lines[0].Flags.C6_Subtitle) ? layers.Mode.BgndColour : Color.Transparent);
                        palBgnd.Entries[1] = layers.Transparency;
                        bgndChr.Palette = palBgnd;
                        //layers.Foreground.Save(Environment.GetEnvironmentVariable("temp") + "\\teletext\\renderer6-" + y.ToString() + ".png");
                        if (layers.Mode.DoubleHeight)
                        {
                            chr = ConvertToDoubleHeight(chr, _deviceDPI);
                            bgndChr = ConvertToDoubleHeight(bgndChr, _deviceDPI);
                            doubleHeightSet = true;
                        }

                        if (layers.Mode.DoubleWidth && _presentationLevel > 1.5)
                        {
                            chr = ConvertToDoubleWidth(chr, _deviceDPI);
                            bgndChr = ConvertToDoubleWidth(bgndChr, _deviceDPI);
                            if (initialDoubleWidthX == -1)
                                initialDoubleWidthX = x;
                        }
                        else
                            initialDoubleWidthX = -1;

                        // If we are in double width mode miss every other character in the row out
                        if ((x - initialDoubleWidthX) % 2 != 0 && initialDoubleWidthX != -1)
                            chr = null;

                        if ((!layers.Mode.Conceal || (layers.Mode.Conceal && _revealPressed)) && chr != null && bytASCII != 0x20 & bytASCII != 0x80)
                        {
                            if ((layers.Mode.Flash && _flashTextOn) || !layers.Mode.Flash)
                                grForeground.DrawImage(chr, ((x * charWidth) + level1XStart) * dpiScale, ((y * charHeight) + level1YStart) * dpiScale);
                        }


                        if (chr != null)
                            grBackground.DrawImage(bgndChr, ((x * charWidth) + level1XStart) * dpiScale, ((y * charHeight) + level1YStart) * dpiScale);

                        // Store current state of this character cell in the modeMap
                        //if (layers.Mode.DoubleHeight)
                        //{
                        //    System.Diagnostics.Debug.WriteLine("Double");
                        //}

                        page.modeMap[y, x] = layers.Mode;

                        // If control code is "Set After" then set it for next time
                        if (bytASCII < 0x20)
                        {
                            isControlChar = true;
                            if (!IsSetAt(bytASCII))
                                layers.Mode = ChangeMode(bytASCII, layers.Mode);
                        }
                    }
                }

                // if we have had a double height code on this row, copy the background to the row below
                if (doubleHeightSet)
                {
                    Bitmap thisRowBackground = layers.Background.Clone(new Rectangle((int)(0 + level1XStart * dpiScale), (int)(((y * charHeight) + level1YStart) * dpiScale), (int)(480 * dpiScale), (int)(charHeight * dpiScale)), layers.Background.PixelFormat);
                    grBackground.DrawImage(thisRowBackground, (0 + level1XStart) * dpiScale, (((y + 1) * charHeight) + level1YStart) * dpiScale);
                    doubleHeightAbove = true;
                }

                grBackground.Dispose();
                grForeground.Dispose();

                layers.Foreground.MakeTransparent(layers.Transparency);

                //Copy modeMap into Level 2 modeMap
                for (Int32 x = 0; x < 40; x++)
                {
                    page.modeMapL2[y, x] = page.modeMap[y, x];
                }

            }

            return layers;

        }

        public String RenderLineHTML(long ServiceId, long PageId, Page page, Int32 y, string nationalOptionHex = "0000000", string type = "default")
        {
            RenderedHTML layers = new RenderedHTML();

            // Initialise modeMap
            for (Int32 x = 0; x < 40; x++)
            {
                page.modeMap[y, x] = new Mode();
                page.modeMapL2[y, x] = new Mode();
            }
            layers.Mode.Default();
            // Lookup the packet for the incoming row number
            Int32 lineNo = page.GetPacketIndex(y);

            Boolean doubleHeightAbove = false;
            if (y > 0)
            {
                for (Int32 x = 0; x < 40; x++)
                    if (page.modeMap[y - 1, x].DoubleHeight)
                        doubleHeightAbove = true;
                //if (page.modeMap[y - 1, 0].DoubleHeightFlag)
                //    doubleHeightAbove = true;            
            }


            // Find out what national option is selected
            // Make a binary string out of the flags we have

            // Triplet 1 decoding to go here - placeholder of zeroes for now
            /*if (y == 0)
            {

                if (_nationalOptionBits == "")
                    _nationalOptionBits = "0000" + (page.Lines[0].Flags.C12 == true ? "1" : "0") + (page.Lines[0].Flags.C13 == true ? "1" : "0") + (page.Lines[0].Flags.C14 == true ? "1" : "0");
            }

            string nationalOptionHex = Convert.ToString(Convert.ToByte(_nationalOptionBits, 2), 16).PadLeft(2, Convert.ToChar("0"));*/

            if (nationalOptionHex == null)
                nationalOptionHex = "0000000";

            //nationalOptionHex = "0100100";

            Int32 initialDoubleWidthX = -1;

            Mode lastMode = new Mode();
            string output = "<div class='teletext-" + type + "'>";
            bool firstCode = true;

            bool fastextLinkOpen = false;

            if (!doubleHeightAbove)
            {

                if (page.Lines[lineNo].Text.Length > 40)
                {
                    System.Diagnostics.Debug.WriteLine("P" + page.Lines[0].MagPage + " line " + lineNo + " > 40: " + page.Lines[lineNo].Text);
                    page.Lines[lineNo].Text = page.Lines[lineNo].Text.Substring(0, 40);
                }
                for (int x = 0; x < page.Lines[lineNo].Text.Length; x++)
                {
                    string style = "";

                    byte bytASCII;
                    Boolean isControlChar = false;

                    string strASCII = page.Lines[lineNo].Text.Substring(x, 1);
                    //bytASCII = Convert.ToByte(Convert.ToChar(strASCII));
                    bytASCII = Convert.ToByte(page.Lines[lineNo].Bytes[x + 2] & 0x7f);
                    if (bytASCII == 0x7f && !layers.Mode.Graphics) strASCII = "\u25a0";
                    if (bytASCII == 0x7f && layers.Mode.Graphics) strASCII = "\uee80";
                    strASCII = strASCII.Replace((">"), "&gt;");
                    strASCII = strASCII.Replace(("<"), "&lt;");
                    layers.Mode.Character = bytASCII;

                    // blank out the header hammed data bytes
                    if (y == 0 && x < 8)
                        bytASCII = 0xff;

                    // If mode is "Set At" then change the mode before the character is printed
                    if (bytASCII < 0x20)
                    {
                        isControlChar = true;
                        if (IsSetAt(bytASCII))
                            layers.Mode = ChangeMode(bytASCII, layers.Mode);
                    }

                    if ((bytASCII >= 0x20 && bytASCII <= 0x3f) || (bytASCII >= 0x60))
                    // Blast-through text: codes $4x and $5x are displayed as letters even in graphics mode
                    // so only switch the character to graphics if char is not control code or blast through code
                    {
                        //if (layers.Mode.Graphics && !layers.Mode.SeparatedGraphics)
                        //    bytASCII += 0x60;

                        //if (layers.Mode.Graphics && layers.Mode.SeparatedGraphics)
                        //    bytASCII += 0x80;

                        if (layers.Mode.Graphics)
                        {
                            layers.Mode.HeldGraphicsChar = bytASCII;
                            layers.Mode.HoldIsSeparated = layers.Mode.SeparatedGraphics;
                        }
                    }

                    if (!isControlChar)
                    {

                        // National options
                        if (!layers.Mode.Graphics && ("23,24,40,5b,5c,5d,5e,5f,60,7b,7c,7d,7e".Contains(bytASCII.toHex(2)) || nationalOptionHex == "0100100"))
                        {
                            switch (nationalOptionHex)
                            {
                                case "0000000":
                                    // English
                                    if (bytASCII == 0x23) strASCII = "\u00a3";
                                    if (bytASCII == 0x5b) strASCII = "\u2190";
                                    if (bytASCII == 0x5c) strASCII = "\u00bd";
                                    if (bytASCII == 0x5d) strASCII = "\u2192";
                                    if (bytASCII == 0x5e) strASCII = "\u2191";
                                    if (bytASCII == 0x5f) strASCII = "\u0023";
                                    if (bytASCII == 0x60) strASCII = "\u002d";
                                    if (bytASCII == 0x7b) strASCII = "\u00bc";
                                    if (bytASCII == 0x7c) strASCII = "\u2016";
                                    if (bytASCII == 0x7d) strASCII = "\u00be";
                                    if (bytASCII == 0x7e) strASCII = "\u00f7";

                                    break;

                                case "0000001":
                                    // German
                                    if (bytASCII == 0x40) strASCII = "\u00a7";
                                    if (bytASCII == 0x5b) strASCII = "\u00c4";
                                    if (bytASCII == 0x5c) strASCII = "\u00d6";
                                    if (bytASCII == 0x5d) strASCII = "\u00dc";
                                    if (bytASCII == 0x60) strASCII = "\u00b0";
                                    if (bytASCII == 0x7b) strASCII = "\u00e4";
                                    if (bytASCII == 0x7c) strASCII = "\u00f6";
                                    if (bytASCII == 0x7d) strASCII = "\u00fc";
                                    if (bytASCII == 0x7e) strASCII = "\u00df";
                                    break;

                                case "0001000":
                                    // Polish
                                    if (bytASCII == 0x24) strASCII = "\u0144";
                                    if (bytASCII == 0x40) strASCII = "\u0105";
                                    if (bytASCII == 0x5b) strASCII = "\u0161";
                                    if (bytASCII == 0x5c) strASCII = "\u015a";
                                    if (bytASCII == 0x5d) strASCII = "\u0141";
                                    if (bytASCII == 0x5e) strASCII = "\u0107";
                                    if (bytASCII == 0x5f) strASCII = "\u00d3";
                                    if (bytASCII == 0x60) strASCII = "\u0119";
                                    if (bytASCII == 0x7b) strASCII = "\u017c";
                                    if (bytASCII == 0x7c) strASCII = "\u015b";
                                    if (bytASCII == 0x7d) strASCII = "\u0142";
                                    if (bytASCII == 0x7e) strASCII = "\u017a";
                                    break;

                                case "0000010":
                                    // Swedish/Finnish/Hungarian
                                    if (bytASCII == 0x23) strASCII = "\u0023";
                                    if (bytASCII == 0x24) strASCII = "\u263c";
                                    if (bytASCII == 0x40) strASCII = "\u00c9";
                                    if (bytASCII == 0x5b) strASCII = "\u00c4";
                                    if (bytASCII == 0x5c) strASCII = "\u00d6";
                                    if (bytASCII == 0x5d) strASCII = "\u00c2";
                                    if (bytASCII == 0x5e) strASCII = "\u00dc";
                                    if (bytASCII == 0x60) strASCII = "\u00e9";
                                    if (bytASCII == 0x7b) strASCII = "\u00e4";
                                    if (bytASCII == 0x7c) strASCII = "\u0076";
                                    if (bytASCII == 0x7d) strASCII = "\u00e2";
                                    if (bytASCII == 0x7e) strASCII = "\u00fc";
                                    break;

                                case "0000011":
                                    // Italian
                                    if (bytASCII == 0x23) strASCII = "\u00a3";
                                    if (bytASCII == 0x24) strASCII = "\u0024";
                                    if (bytASCII == 0x40) strASCII = "\u00e9";
                                    if (bytASCII == 0x5b) strASCII = "\u00b0";
                                    if (bytASCII == 0x5c) strASCII = "\u00e7";
                                    if (bytASCII == 0x5d) strASCII = "\u2192";
                                    if (bytASCII == 0x5e) strASCII = "\u2191";
                                    if (bytASCII == 0x5f) strASCII = "\u0023";
                                    if (bytASCII == 0x60) strASCII = "\u00f9";
                                    if (bytASCII == 0x7b) strASCII = "\u00e0";
                                    if (bytASCII == 0x7c) strASCII = "\u00f2";
                                    if (bytASCII == 0x7d) strASCII = "\u00e8";
                                    if (bytASCII == 0x7e) strASCII = "\u00ec";
                                    break;

                                case "0000101":
                                    // Portuguese/Spanish
                                    if (bytASCII == 0x23) strASCII = "\u00e7";
                                    if (bytASCII == 0x24) strASCII = "\u0024";
                                    if (bytASCII == 0x40) strASCII = "\u00a1";
                                    if (bytASCII == 0x5b) strASCII = "\u00e1";
                                    if (bytASCII == 0x5c) strASCII = "\u00e9";
                                    if (bytASCII == 0x5d) strASCII = "\u00ed";
                                    if (bytASCII == 0x5e) strASCII = "\u00f3";
                                    if (bytASCII == 0x5f) strASCII = "\u00fa";
                                    if (bytASCII == 0x60) strASCII = "\u00bf";
                                    if (bytASCII == 0x7b) strASCII = "\u00fc";
                                    if (bytASCII == 0x7c) strASCII = "\u00f1";
                                    if (bytASCII == 0x7d) strASCII = "\u00e8";
                                    if (bytASCII == 0x7e) strASCII = "\u00e0";
                                    break;

                                case "0100100":
                                    // Russian/Bulgarian Cyrillic

                                    if (bytASCII == 0x26) strASCII = "\u044b";
                                    if (bytASCII == 0x40) strASCII = "\u042e";
                                    if (bytASCII == 0x60) strASCII = "\u044e";
                                    if (bytASCII == 0x62) strASCII = "\u0431";
                                    if (bytASCII == 0x63) strASCII = "\u0446";
                                    if (bytASCII == 0x64) strASCII = "\u0434";
                                    if (bytASCII == 0x66) strASCII = "\u0444";
                                    if (bytASCII == 0x67) strASCII = "\u0433";
                                    if (bytASCII == 0x68) strASCII = "\u0445";
                                    if (bytASCII == 0x69) strASCII = "\u0438";
                                    if (bytASCII == 0x6a) strASCII = "\u0439";
                                    if (bytASCII == 0x6b) strASCII = "\u043a";
                                    if (bytASCII == 0x6c) strASCII = "\u043b";
                                    if (bytASCII == 0x6d) strASCII = "\u043c";
                                    if (bytASCII == 0x6e) strASCII = "\u043d";
                                    if (bytASCII == 0x6f) strASCII = "\u043e";

                                    if (bytASCII == 0x70) strASCII = "\u043f";
                                    if (bytASCII == 0x71) strASCII = "\u044f";
                                    if (bytASCII == 0x72) strASCII = "\u0440";
                                    if (bytASCII == 0x73) strASCII = "\u0441";
                                    if (bytASCII == 0x74) strASCII = "\u0442";
                                    if (bytASCII == 0x75) strASCII = "\u0443";
                                    if (bytASCII == 0x76) strASCII = "\u0436";
                                    if (bytASCII == 0x77) strASCII = "\u0432";
                                    if (bytASCII == 0x78) strASCII = "\u044c";
                                    if (bytASCII == 0x79) strASCII = "\u044a";
                                    if (bytASCII == 0x7a) strASCII = "\u0437";
                                    if (bytASCII == 0x7b) strASCII = "\u0448";
                                    if (bytASCII == 0x7c) strASCII = "\u044d";
                                    if (bytASCII == 0x7d) strASCII = "\u0449";
                                    if (bytASCII == 0x7e) strASCII = "\u0447";

                                    break;

                            }

                        }

                        // These characters blast-through
                        if (nationalOptionHex == "0100100")
                        {
                            if (bytASCII == 0x40) strASCII = "\u042e";
                            if (bytASCII == 0x41) strASCII = "\u0410";
                            if (bytASCII == 0x42) strASCII = "\u0411";
                            if (bytASCII == 0x43) strASCII = "\u0426";
                            if (bytASCII == 0x44) strASCII = "\u0414";
                            if (bytASCII == 0x46) strASCII = "\u0424";
                            if (bytASCII == 0x47) strASCII = "\u0413";
                            if (bytASCII == 0x48) strASCII = "\u0425";
                            if (bytASCII == 0x49) strASCII = "\u0418";
                            if (bytASCII == 0x4a) strASCII = "\u0419";
                            if (bytASCII == 0x4c) strASCII = "\u041b";
                            if (bytASCII == 0x4e) strASCII = "\u041d";

                            if (bytASCII == 0x50) strASCII = "\u041f";
                            if (bytASCII == 0x51) strASCII = "\u042f";
                            if (bytASCII == 0x52) strASCII = "\u0420";
                            if (bytASCII == 0x53) strASCII = "\u0421";
                            if (bytASCII == 0x54) strASCII = "\u0422";
                            if (bytASCII == 0x55) strASCII = "\u0423";
                            if (bytASCII == 0x56) strASCII = "\u0416";
                            if (bytASCII == 0x57) strASCII = "\u0412";
                            if (bytASCII == 0x58) strASCII = "\u042c";
                            if (bytASCII == 0x59) strASCII = "\u042a";
                            if (bytASCII == 0x5a) strASCII = "\u0417";
                            if (bytASCII == 0x5b) strASCII = "\u0428";
                            if (bytASCII == 0x5c) strASCII = "\u042d";
                            if (bytASCII == 0x5d) strASCII = "\u0429";
                            if (bytASCII == 0x5e) strASCII = "\u0427";
                            if (bytASCII == 0x5f) strASCII = "\u042b";
                        }

                    }
                    else
                    {
                        if (!layers.Mode.Hold || layers.Mode.HeldGraphicsChar == 0)
                            strASCII = "&nbsp;";
                        else
                            strASCII = Convert.ToString((char)layers.Mode.HeldGraphicsChar).Replace(" ", "&nbsp;");
                    }


                    //if (layers.Mode.DoubleWidth && _presentationLevel > 1.5)
                    //{
                    //    chr = ConvertToDoubleWidth(chr);
                    //    bgndChr = ConvertToDoubleWidth(bgndChr);
                    //    if (initialDoubleWidthX == -1)
                    //        initialDoubleWidthX = x;
                    //}
                    //else
                    //    initialDoubleWidthX = -1;

                    // If we are in double width mode miss every other character in the row out
                    if ((x - initialDoubleWidthX) % 2 != 0 && initialDoubleWidthX != -1)
                        strASCII = "&nbsp;";

                    bool modeChange = false;

                    if ((lastMode.ForeColourCode != layers.Mode.ForeColourCode)
                        || (lastMode.Graphics != layers.Mode.Graphics)
                        || (lastMode.SeparatedGraphics != layers.Mode.SeparatedGraphics)
                        || (lastMode.DoubleHeight != layers.Mode.DoubleHeight)
                        || (lastMode.BgndColourCode != layers.Mode.BgndColourCode)
                        || (lastMode.Conceal != layers.Mode.Conceal)
                        || (lastMode.Flash != layers.Mode.Flash)
                        )
                        modeChange = true;

                    if (modeChange)
                    {
                        if (fastextLinkOpen)
                        {
                            output = output.Substring(0, output.Length - 6) + "</a>&nbsp;";
                            fastextLinkOpen = false;
                        }
                        if (lastMode.ForeColourCode != layers.Mode.ForeColourCode
                            || lastMode.Conceal != layers.Mode.Conceal
                            || (lastMode.Flash != layers.Mode.Flash)
                            || (lastMode.BgndColourCode != layers.Mode.BgndColourCode))
                        {
                            style += " teletext-colour-" + layers.Mode.ForeColourCode + " ";
                        }

                        if (layers.Mode.Graphics)
                        {
                            if (
                                    (!layers.Mode.Hold && !layers.Mode.HoldIsSeparated && !layers.Mode.SeparatedGraphics)
                                || (!layers.Mode.Hold && layers.Mode.HoldIsSeparated && !layers.Mode.SeparatedGraphics)
                                || (!layers.Mode.Hold && layers.Mode.HoldIsSeparated && layers.Mode.SeparatedGraphics)
                                || (layers.Mode.Hold && !layers.Mode.HoldIsSeparated && layers.Mode.SeparatedGraphics)
                                || (layers.Mode.Hold && !layers.Mode.HoldIsSeparated && !layers.Mode.SeparatedGraphics)

                                )

                                style += " teletext-mosaics teletext-colour-" + layers.Mode.ForeColourCode;

                            else
                            {
                                style += " teletext-separated teletext-colour-" + layers.Mode.ForeColourCode;
                            }
                        }
                        else
                        {
                            //style += " teletext-alphanumerics teletext-colour-" + layers.Mode.ForeColourCode;
                            style += " teletext-alphanumerics";
                        }

                        if (layers.Mode.DoubleHeight)
                            style += " teletext-doubleheight ";

                        if (layers.Mode.Conceal)
                            //style += " teletext-conceal ";
                            style += " teletext-conceal teletext-colour-" + layers.Mode.ForeColourCode;

                        if (layers.Mode.Flash)
                            style += " teletext-flash ";
                        //style = lastStyle + " teletext-flash ";

                    }

                    if (style != "")
                    {
                        output += firstCode ? "" : "</span>";
                        output += "<span class='" + style + "' ";
                        if (layers.Mode.BgndColourCode != 0)
                            output += "style='background-color: #" + layers.Mode.BgndColour.Name.PadLeft(8, Convert.ToChar("0")).Substring(2) + "'";
                        if (layers.Mode.Graphics)
                            output += " aria-hidden = 'true'";
                        output += ">";
                        if (y == 24)
                        {
                            if (layers.Mode.ForeColourCode == 1)
                            {
                                output += "<a href=\"/Pages/FastextPressed?sourcePageId=" + PageId + "&amp;colour=Red\">";
                                fastextLinkOpen = true;
                            }
                            if (layers.Mode.ForeColourCode == 2)
                            {
                                output += "<a href=\"/Pages/FastextPressed?sourcePageId=" + PageId + "&amp;colour=Green\">";
                                fastextLinkOpen = true;
                            }
                            if (layers.Mode.ForeColourCode == 3)
                            {
                                output += "<a href=\"/Pages/FastextPressed?sourcePageId=" + PageId + "&amp;colour=Yellow\">";
                                fastextLinkOpen = true;
                            }
                            if (layers.Mode.ForeColourCode == 6)
                            {
                                output += "<a href=\"/Pages/FastextPressed?sourcePageId=" + PageId + "&amp;colour=Blue\">";
                                fastextLinkOpen = true;
                            }
                        }
                        firstCode = false;
                    }


                    // Write character
                    if (!IsSetAt(bytASCII) && isControlChar)
                        output += layers.Mode.Hold ? strASCII.Replace(" ", "&nbsp;") : "&nbsp;";

                    if (!isControlChar)
                        output += strASCII.Replace(" ", "&nbsp;");


                    lastMode = (Mode)layers.Mode;

                    // Store current state of this character cell in the modeMap
                    page.modeMap[y, x] = layers.Mode;

                    // If control code is "Set After" then set it for next time
                    if (isControlChar)
                    {
                        if (!IsSetAt(bytASCII))
                            layers.Mode = ChangeMode(bytASCII, layers.Mode);
                        else
                            output += layers.Mode.Hold ? strASCII : "&nbsp;";
                    }
                }
            }
            else
            {
                if (lineNo == 255)
                {
                    lineNo = page.GetPacketIndex(y - 1);
                }
                else
                    lineNo--;
                // Draw the second line of a double height character
                for (int x = 0; x < page.Lines[lineNo].Text.Length; x++)
                {
                    string strASCII = page.Lines[lineNo].Text.Substring(x, 1);
                    //byte bytASCII = Convert.ToByte(Convert.ToChar(strASCII));
                    byte bytASCII = Convert.ToByte(page.Lines[lineNo].Bytes[x+2] & 0x7f);

                    // If mode is "Set At" then change the mode before the character is printed
                    if (bytASCII < 0x20)
                    {
                        if (IsSetAt(bytASCII))
                            layers.Mode = ChangeMode(bytASCII, layers.Mode);
                    }

                    if (bytASCII == 0x1d)
                    {
                        output += "<span style='background-color: #" + layers.Mode.ForeColour.Name.PadLeft(8, Convert.ToChar("0")).Substring(2) + "'>";
                    }
                    if (bytASCII == 0x1c)
                    {
                        output += "</span>";
                    }
                    output += "&nbsp;";
                    if (bytASCII < 0x20)
                    {
                        if (!IsSetAt(bytASCII))
                            layers.Mode = ChangeMode(bytASCII, layers.Mode);
                    }

                }

                output += "</span>";
            }

            //Copy modeMap into Level 2 modeMap
            for (Int32 x = 0; x < 40; x++)
            {
                page.modeMapL2[y, x] = page.modeMap[y, x];
            }


            if (output == "<div class='teletext-" + type + "'>")
                output += "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;";

            if (fastextLinkOpen)
            {
                output += "</a>&nbsp;";
                fastextLinkOpen = false;
            }

            output = output + "</span></div>";

            // Add html link

            if (y != 0 && y != 24)
            {
            bool pgFound = false;
            int pgCount = 0;                
                do
                {
                    pgFound = false;
                    //output = Regex.Replace(output, "[^0-9][0-9]{3}[^0-9]{6}",
                    output = Regex.Replace(output, "[^0-9]([0-9]|[O]){3}[^0-9]{6}",
                    delegate (Match match)
                    {
                        string matchString = match.ToString();
                        string matchStringO = match.ToString().Replace("O", "0");
                        int pg1 = Convert.ToChar(matchString.Substring(1, 1)) ;
                        int pg2 = Convert.ToChar(matchString.Substring(2, 1)) ;
                        int pg3 = Convert.ToChar(matchString.Substring(3, 1)) ;

                        string htmlServiceId = "";
                        string strServiceId = ServiceId.ToString();
                        for (int n=0; n< strServiceId.Length; n++)
                        {
                            htmlServiceId += "&#" + (Convert.ToInt32(strServiceId.Substring(n, 1)) + 48).ToString();
                        }

                        pgFound = true;
                        pgCount++;
                        return matchString.Substring(0, 1) + "<a href='" + "/Pages/Lookup/?service=" + htmlServiceId + "&page=" + matchStringO.Substring(1, 3) + "'>" + "&#" + pg1 + "&#" + pg2 + "&#" + pg3 + "</a>" + matchString.Substring(4);
                    }
                    );
                } while (pgFound && pgCount < 10);
            }

            return output;

        }


        private Bitmap ConvertToDoubleHeight(Image imgIn, float DeviceDPI = 96.0f, ColorPalette pal = null)
        {
            Bitmap bmpIn = new Bitmap(imgIn);
            //bmpIn.Save(Environment.GetEnvironmentVariable("temp") + "\\input.png", System.Drawing.Imaging.ImageFormat.Png);

            float dpiScale = DeviceDPI / 96.0f;

            // Calculate scaled dimensions  
            int scaledWidth = (int)(imgIn.Width * dpiScale);
            int scaledHeight = (int)(imgIn.Height * 2 * dpiScale);

            Bitmap bmpOut = new Bitmap(scaledWidth, scaledHeight, bmpIn.PixelFormat);

            // Scale the pixel positioning based on DPI - fill all pixels in the scaled areas
            for (int y = 0; y < bmpIn.Height; y++)
            {
                for (int x = 0; x < bmpIn.Width; x++)
                {
                    Color pixel = bmpIn.GetPixel(x, y);

                    // Calculate scaled position ranges
                    int scaledXStart = (int)(x * dpiScale);
                    int scaledXEnd = (int)((x + 1) * dpiScale);
                    int scaledY1Start = (int)(y * 2 * dpiScale);
                    int scaledY1End = (int)((y * 2 + 1) * dpiScale);
                    int scaledY2Start = (int)((y * 2 + 1) * dpiScale);
                    int scaledY2End = (int)((y * 2 + 2) * dpiScale);

                    // Fill all pixels in the scaled area (double height)
                    for (int scaledX = scaledXStart; scaledX < scaledXEnd && scaledX < scaledWidth; scaledX++)
                    {
                        // First row of double height
                        for (int scaledY1 = scaledY1Start; scaledY1 < scaledY1End && scaledY1 < scaledHeight; scaledY1++)
                        {
                            bmpOut.SetPixel(scaledX, scaledY1, pixel);
                        }
                        // Second row of double height
                        for (int scaledY2 = scaledY2Start; scaledY2 < scaledY2End && scaledY2 < scaledHeight; scaledY2++)
                        {
                            bmpOut.SetPixel(scaledX, scaledY2, pixel);
                        }
                    }
                }
            }

            if (pal != null)
                bmpOut.Palette = pal;

            //bmpOut.Save(Environment.GetEnvironmentVariable("temp") + $"\\input-{Guid.NewGuid()}.png", System.Drawing.Imaging.ImageFormat.Png);

            return bmpOut;
        }

        private Bitmap ConvertToDoubleWidth(Image imgIn, float DeviceDPI = 96.0f)
        {
            Bitmap bmpIn = new Bitmap(imgIn);
            float dpiScale = DeviceDPI / 96.0f;

            // Calculate scaled dimensions
            int scaledWidth = (int)(imgIn.Width * 2 * dpiScale);
            int scaledHeight = (int)(imgIn.Height * dpiScale);

            Bitmap bmpOut = new Bitmap(scaledWidth, scaledHeight, bmpIn.PixelFormat);

            // Scale the pixel positioning based on DPI - fill all pixels in the scaled areas
            for (int y = 0; y < bmpIn.Height; y++)
            {
                for (int x = 0; x < bmpIn.Width; x++)
                {
                    Color pixel = bmpIn.GetPixel(x, y);

                    // Calculate scaled position ranges
                    int scaledX1Start = (int)(x * 2 * dpiScale);
                    int scaledX1End = (int)((x * 2 + 1) * dpiScale);
                    int scaledX2Start = (int)((x * 2 + 1) * dpiScale);
                    int scaledX2End = (int)((x * 2 + 2) * dpiScale);
                    int scaledYStart = (int)(y * dpiScale);
                    int scaledYEnd = (int)((y + 1) * dpiScale);

                    // Fill all pixels in the scaled area (double width)
                    for (int scaledY = scaledYStart; scaledY < scaledYEnd && scaledY < scaledHeight; scaledY++)
                    {
                        // First column of double width
                        for (int scaledX1 = scaledX1Start; scaledX1 < scaledX1End && scaledX1 < scaledWidth; scaledX1++)
                        {
                            bmpOut.SetPixel(scaledX1, scaledY, pixel);
                        }
                        // Second column of double width
                        for (int scaledX2 = scaledX2Start; scaledX2 < scaledX2End && scaledX2 < scaledWidth; scaledX2++)
                        {
                            bmpOut.SetPixel(scaledX2, scaledY, pixel);
                        }
                    }
                }
            }

            return bmpOut;
        }
        private Bitmap ConvertToUnderlined(Image imgIn)
        {

            Bitmap bmpIn = new Bitmap(imgIn);
            bmpIn.Palette = imgIn.Palette;
            //Bitmap bmpOut = new Bitmap(imgIn.Width, imgIn.Height, bmpIn.PixelFormat);

            //System.Diagnostics.Debug.WriteLine("ConvertToUnderlined: " + bmpIn.Palette.Entries.Length);

            //Rectangle destRect = new Rectangle(0, 0, bmpIn.Width, bmpIn.Height*2);

            int y = bmpIn.Height - 1;

            for (int x = 0; x < bmpIn.Width; x++)
            {
                bmpIn.SetPixel(x, y, bmpIn.Palette.Entries[1]);
            }

            return bmpIn;
        }

        private Bitmap SuperimposeCharacter(Image BaseChar, Image CharToSuperimpose)
        {
            Bitmap bmpIn = new Bitmap(BaseChar);
            if (BaseChar != null && CharToSuperimpose != null)
            {

                Bitmap bmpSuper = new Bitmap(CharToSuperimpose);

                bmpIn.Palette = BaseChar.Palette;

                for (int y = 0; y < bmpIn.Height; y++)
                {
                    for (int x = 0; x < bmpIn.Width; x++)
                    {
                        //System.Diagnostics.Debug.WriteLine(bmpSuper.GetPixel(x, y).ToString());
                        if (bmpSuper.GetPixel(x, y).Equals(Color.FromArgb(255, 0, 0, 0)))
                        {
                            bmpIn.SetPixel(x, y, bmpIn.Palette.Entries[1]);
                        }
                    }
                }
            }

            return bmpIn;
        }

        private Mode ChangeMode(byte b, Mode mode)
        {
            string bHex = b.toHex(2);

            // Required for held graphics nuances, see below
            Boolean origGraphics = mode.Graphics;
            Boolean origDoubleHeight = mode.DoubleHeight;
            Boolean origDoubleWidth = mode.DoubleWidth;
            Boolean origDoubleSize = mode.DoubleSize;

            switch (bHex)
            {
                case "00":
                    if (zeroBlack || _presentationLevel > 1)
                    {
                        mode.Graphics = false;
                        mode.ForeColour = ColourLookup(0, 0, "FG");
                        mode.ForeColourCode = 0;
                        mode.Conceal = false;
                    }
                    break;
                case "01":
                    if (PresentationLevel >= 0.19760101)
                    {
                        mode.Graphics = false;
                        mode.ForeColour = ColourLookup(0, 1, "FG");
                        mode.ForeColourCode = 1;
                        mode.Conceal = false;
                    }
                    break;
                case "02":
                    if (PresentationLevel >= 0.19760101)
                    {
                        mode.Graphics = false;
                        mode.ForeColour = ColourLookup(0, 2, "FG");
                        mode.ForeColourCode = 2;
                        mode.Conceal = false;
                    }
                    break;
                case "03":
                    if (PresentationLevel >= 0.19760101)
                    {
                        mode.Graphics = false;
                        mode.ForeColour = ColourLookup(0, 3, "FG");
                        mode.ForeColourCode = 3;
                        mode.Conceal = false;
                    }
                    break;
                case "04":
                    if (PresentationLevel >= 0.19760101)
                    {
                        mode.Graphics = false;
                        mode.ForeColour = ColourLookup(0, 4, "FG");
                        mode.ForeColourCode = 4;
                        mode.Conceal = false;
                    }
                    break;
                case "05":
                    if (PresentationLevel >= 0.19760101)
                    {
                        mode.Graphics = false;
                        mode.ForeColour = ColourLookup(0, 5, "FG");
                        mode.ForeColourCode = 5;
                        mode.Conceal = false;
                    }
                    break;
                case "06":
                    if (PresentationLevel >= 0.19760101)
                    {
                        mode.Graphics = false;
                        mode.ForeColour = ColourLookup(0, 6, "FG");
                        mode.ForeColourCode = 6;
                        mode.Conceal = false;
                    }
                    break;
                case "07":
                    if (PresentationLevel >= 0.19760101)
                    {
                        mode.Graphics = false;
                        mode.ForeColour = ColourLookup(0, 7, "FG");
                        mode.ForeColourCode = 7;
                        mode.Conceal = false;
                    }
                    break;
                case "08":
                    if (PresentationLevel >= 0.19760101)
                        mode.Flash = true;
                    break;
                case "09":
                    if (PresentationLevel >= 0.19760101)
                    {
                        mode.Flash = false;
                    }
                    break;
                case "0a":
                    if (PresentationLevel >= 0.19760101)
                    {
                        mode.Boxed = false;
                    }
                    break;
                case "0b":
                    if (PresentationLevel >= 0.19760101)
                    {
                        mode.Boxed = true;
                    }
                    break;
                case "0c":
                    if (PresentationLevel >= 1)
                    {
                        mode.DoubleHeight = false;
                        mode.DoubleWidth = false;
                        mode.DoubleSize = false;
                    }
                    if (PresentationLevel == 0.19750901)
                    {
                        mode.Flash = true;
                    }
                        break;
                case "0d":
                    if (_presentationLevel >= 1)
                    {
                        mode.DoubleHeight = true;
                        mode.DoubleHeight2ndRow = false;
                        mode.DoubleHeightFlag = true;
                        mode.DoubleWidth = false;
                        mode.DoubleSize = false;
                    }
                    if (PresentationLevel == 0.19750901)
                    {
                        mode.Flash = false;
                    }
                    break;
                case "0e":
                    if (_presentationLevel > 1.5)
                    {
                        mode.DoubleWidth = true;
                        mode.DoubleHeight = false;
                        mode.DoubleSize = false;
                    }
                    if (PresentationLevel == 0.19750901)
                    {
                        mode.Boxed = false;
                    }
                    break;
                case "0f":
                    if (_presentationLevel > 1.5)
                    {
                        mode.DoubleHeight = true;
                        mode.DoubleWidth = true;
                        mode.DoubleSize = true;
                    }
                    if (PresentationLevel == 0.19750901)
                    {
                        mode.Boxed = true;
                    }
                    break;
                case "10":
                    if (zeroBlack || _presentationLevel > 1)
                    {
                        mode.Graphics = true;
                        mode.ForeColour = ColourLookup(0, 0, "FG");
                        mode.ForeColourCode = 0;
                        mode.Conceal = false;
                    }
                    break;
                case "11":
                    mode.Graphics = true;
                    mode.ForeColour = ColourLookup(0, 1, "FG");
                    mode.ForeColourCode = 1;
                    mode.Conceal = false;
                    break;
                case "12":
                    mode.Graphics = true;
                    mode.ForeColour = ColourLookup(0, 2, "FG");
                    mode.ForeColourCode = 2;
                    mode.Conceal = false;
                    break;
                case "13":
                    mode.Graphics = true;
                    mode.ForeColour = ColourLookup(0, 3, "FG");
                    mode.ForeColourCode = 3;
                    mode.Conceal = false;
                    break;
                case "14":
                    mode.Graphics = true;
                    mode.ForeColour = ColourLookup(0, 4, "FG");
                    mode.ForeColourCode = 4;
                    mode.Conceal = false;
                    break;
                case "15":
                    mode.Graphics = true;
                    mode.ForeColour = ColourLookup(0, 5, "FG");
                    mode.ForeColourCode = 5;
                    mode.Conceal = false;
                    break;
                case "16":
                    mode.Graphics = true;
                    mode.ForeColour = ColourLookup(0, 06, "FG");
                    mode.ForeColourCode = 6;
                    mode.Conceal = false;
                    break;
                case "17":
                    mode.Graphics = true;
                    mode.ForeColour = ColourLookup(0, 7, "FG");
                    mode.ForeColourCode = 7;
                    mode.Conceal = false;
                    break;
                case "18":
                    if (PresentationLevel >= 0.19760101)
                        mode.Conceal = true;
                    else
                    {
                        mode.Graphics = true;
                        mode.ForeColour = ColourLookup(1, 5, "FG");
                        mode.ForeColourCode = 7;
                        mode.Conceal = false;
                    }
                    break;
                case "19":
                    if (PresentationLevel >= 1)
                        mode.SeparatedGraphics = false;
                    if (PresentationLevel == 0.19750901)
                    {
                        mode.Graphics = false;
                        mode.ForeColour = ColourLookup(0, 1, "FG");
                        mode.ForeColourCode = 1;
                        mode.Conceal = false;
                    }
                    break;
                case "1a":
                    if (PresentationLevel >= 1)
                        mode.SeparatedGraphics = true;
                    if (PresentationLevel == 0.19750901)
                    {
                        mode.Graphics = false;
                        mode.ForeColour = ColourLookup(0, 2, "FG");
                        mode.ForeColourCode = 2;
                        mode.Conceal = false;
                    }
                    break;
                case "1b":
                    if (PresentationLevel == 0.19750901)
                    {
                        mode.Graphics = false;
                        mode.ForeColour = ColourLookup(0, 3, "FG");
                        mode.ForeColourCode = 3;
                        mode.Conceal = false;
                    }
                    break;
                case "1c":
                    if (PresentationLevel >= 1)
                    {
                        mode.BgndColour = defaultBackgroundColour;
                        mode.BgndColourCode = 0;
                    }
                    if (PresentationLevel == 0.19750901)
                    {
                        mode.Graphics = false;
                        mode.ForeColour = ColourLookup(0, 4, "FG");
                        mode.ForeColourCode = 4;
                        mode.Conceal = false;
                    }
                    break;
                case "1d":
                    if (PresentationLevel >= 1)
                    {
                        mode.BgndColour = ColourLookup(0, mode.LastColourCode, "BG");
                        mode.BgndColourCode = mode.LastColourCode;
                    }
                    if (PresentationLevel == 0.19750901)
                    {
                        mode.Graphics = false;
                        mode.ForeColour = ColourLookup(0, 5, "FG");
                        mode.ForeColourCode = 5;
                        mode.Conceal = false;
                    }
                    break;
                case "1e":
                    if (PresentationLevel >= 1)
                    {
                        mode.Hold = true;
                    }
                    if (PresentationLevel == 0.19750901)
                    {
                        mode.Graphics = false;
                        mode.ForeColour = ColourLookup(0, 6, "FG");
                        mode.ForeColourCode = 6;
                        mode.Conceal = false;
                    }
                    break;
                case "1f":
                    if (PresentationLevel >= 1)
                    {
                        mode.Hold = false;
                        mode.HeldGraphicsChar = 0x20;
                    }
                    if (PresentationLevel == 0.19750901)
                    {
                        mode.Graphics = false;
                        mode.ForeColour = ColourLookup(0, 7, "FG");
                        mode.ForeColourCode = 7;
                        mode.Conceal = false;
                    }
                    break;


            }

            mode.LastColour = mode.ForeColour;
            mode.LastColourCode = mode.ForeColourCode;

            //If graphics mode changed back to alpha or vice versa, or height changes, reset held graphics character
            if ((origDoubleHeight != mode.DoubleHeight) || origGraphics != mode.Graphics || origDoubleWidth != mode.DoubleWidth || origDoubleSize != mode.DoubleSize)
            {
                mode.HeldGraphicsChar = 0x20;
                //mode.Hold = false;

            }

            //if (!mode.Graphics && mode.Hold) mode.Hold = false;


            return mode;
        }

        private Boolean IsSetAt(Byte code)
        {

            if (code == 0x09 || code == 0x0c || code == 0x18 || code == 0x19 || code == 0x1a || code == 0x1c
                || code == 0x1d || code == 0x1e)
                return true;
            else
                return false;

        }





        private void NonDisplayablePackets(Page page, RenderedLayersNova layers, String block)
        {
            // Scan binary file for X/26 packets
           
            // ***************************************

            //Initialise and load header info

            Line lineHeader = page.Lines[0];
            activeX = 0;
            activeY = 0;



            //loop the rest of the file for non-header packets
            Int32 debugLineLimit = 256;
            Int32 debugLineCount = 0;

            foreach (Line workingLine in page.Lines)
            {
                //is this a non-displayable packet?
                switch (workingLine.Row)
                {
                    case 26:
                        if (debugLineCount < debugLineLimit)
                        {
                            if (block == "Post")
                            {
                                DecodeX26(workingLine, page, layers);
                                debugLineCount++;

                            }
                            else
                            {

                            }
                        }
                        break;
                    case 27:
                        {
                            System.Diagnostics.Debug.WriteLine("------------------------------");
                            System.Diagnostics.Debug.WriteLine("Packet 27");
                            //Get DC for this packet
                            HammingResults84 hrDC27 = new HammingResults84();
                            hrDC27 = hrDC27.HammingCheck84(workingLine.Bytes[5]);

                            System.Diagnostics.Debug.WriteLine("DC:" + hrDC27.Value + " " + hrDC27.Status);
                        }
                        break;
                    case 28:
                        {
                            if (block == "Pre")
                            {
                                System.Diagnostics.Debug.WriteLine("Packet 28");
                                HammingResults84 hrDC28 = new HammingResults84();
                                hrDC28 = hrDC28.HammingCheck84(workingLine.Bytes[2]);

                                System.Diagnostics.Debug.WriteLine("DC:" + hrDC28.Value + " " + hrDC28.Status);

                                // Force the designation code to 2 if set to Level 2
                                if (PresentationLevel == 2)
                                    hrDC28.Value = 2;
                                if (PresentationLevel == 2.5)
                                    hrDC28.Value = 0;

                                switch (hrDC28.Value)
                                {
                                    case 0:
                                        {
                                            System.Diagnostics.Debug.WriteLine("Format X/28/0");

                                            Boolean terminationMarker = false;
                                            String allDataBits = "";
                                            for (Int32 triplet = 1; triplet < 14 & !terminationMarker; triplet++)
                                            {
                                                Byte byte1 = workingLine.Bytes[(triplet - 1) * 3 + 3];
                                                Byte byte2 = workingLine.Bytes[(triplet - 1) * 3 + 4];
                                                Byte byte3 = workingLine.Bytes[(triplet - 1) * 3 + 5];

                                                System.Diagnostics.Debug.Print("\n Level 2.5 X/28---------------------------\n");

                                                HammingResults2418 hr = new HammingResults2418();
                                                HammingResults2418 hr2 = new HammingResults2418();

                                                hr = hr.HammingCheck2418a(byte1, byte2, byte3);
                                                System.Diagnostics.Debug.Print("\nHR1: Data bits:" + hr.Bits + "\n" + hr.ErrorString + "\nOriginal bytes: " + Convert.ToString(byte1, 16).PadLeft(2, Convert.ToChar("0")) + " " + Convert.ToString(byte2, 16).PadLeft(2, Convert.ToChar("0")) + " " + Convert.ToString(byte3, 16).PadLeft(2, Convert.ToChar("0")));

                                                allDataBits = hr.Bits + allDataBits;
                                            }



                                            String pageFunction = allDataBits.Substring(233 - 4, 4);
                                            System.Diagnostics.Debug.Write("Page Function: ");
                                            switch (pageFunction)
                                            {
                                                case "0000":
                                                    {
                                                        System.Diagnostics.Debug.WriteLine("Basic Level 1 Teletext page (LOP) ({0})", pageFunction);
                                                        break;
                                                    }
                                                default:
                                                    {
                                                        System.Diagnostics.Debug.WriteLine("Not defined in emulator ({0})", pageFunction);
                                                        break;
                                                    }
                                            }

                                            String pageCoding = allDataBits.Substring(234 - 7, 3);
                                            System.Diagnostics.Debug.Write("Page Coding: ");
                                            switch (pageCoding)
                                            {
                                                case "000":
                                                    {
                                                        System.Diagnostics.Debug.WriteLine("All 8-bit bytes, each comprising 7 data bits and 1 odd parity bit: " + pageCoding);
                                                        break;
                                                    }
                                                default:
                                                    {
                                                        System.Diagnostics.Debug.WriteLine("Not defined in emulator ({0})", pageFunction);
                                                        break;
                                                    }
                                            }

                                            String defaultG0G2 = allDataBits.Substring(234 - 14, 7);
                                            System.Diagnostics.Debug.WriteLine("Default G0 and G2 Character Set Designation and National Option Selection: " + defaultG0G2);

                                            String secondG0G2 = allDataBits.Substring(234 - 21, 7);
                                            System.Diagnostics.Debug.Write("Second G0 Set Designation and National Option Selection: " + secondG0G2 + " - ");
                                            switch (secondG0G2)
                                            {
                                                case "1111111":
                                                    {
                                                        System.Diagnostics.Debug.WriteLine("No second G0 set required");
                                                        break;
                                                    }
                                                default:
                                                    {
                                                        System.Diagnostics.Debug.WriteLine("Not defined in emulator");
                                                        break;
                                                    }
                                            }

                                            String leftSidePanel = allDataBits.Substring(234 - 22, 1);
                                            System.Diagnostics.Debug.WriteLine("Left Side Panel: " + leftSidePanel);

                                            String rightSidePanel = allDataBits.Substring(234 - 23, 1);
                                            System.Diagnostics.Debug.WriteLine("Right Side Panel: " + rightSidePanel);

                                            String sidePanelStatusFlag = allDataBits.Substring(234 - 24, 1);
                                            System.Diagnostics.Debug.WriteLine("Panel Status Flag: " + sidePanelStatusFlag);

                                            String sidePanelNumColumns = allDataBits.Substring(234 - 28, 4);
                                            System.Diagnostics.Debug.WriteLine("Number of Columns in Side Panels: " + sidePanelNumColumns + " = " + Convert.ToInt32(sidePanelNumColumns, 2));

                                            for (Int32 clut = 0; clut < 2; clut++)
                                            {
                                                for (Int32 colour = 0; colour < 8; colour++)
                                                {
                                                    String colourString = allDataBits.Substring(234 - 40 - (((clut * 8) + colour) * 12), 12);

                                                    //System.Diagnostics.Debug.WriteLine("colourString.Substring(0, 4) {0}, Convert.ToByte(colourString.Substring(0, 4), 2) {1}", colourString.Substring(0, 4), Convert.ToByte(colourString.Substring(0, 4), 2));
                                                    String red = Convert.ToByte(colourString.Substring(0, 4), 2).toHex(1);
                                                    String green = Convert.ToByte(colourString.Substring(4, 4), 2).toHex(1);
                                                    String blue = Convert.ToByte(colourString.Substring(8, 4), 2).toHex(1);

                                                    ColourMap[clut + 2, colour] = Convert.ToInt32(blue + green + red, 16);
                                                    System.Diagnostics.Debug.WriteLine("Colour Map Entry Coding for CLUTs 2 and 3 (CLUT {0}, Colour {1}: {2} = ${3}", clut + 2, colour, colourString, blue + green + red);
                                                }
                                            }

                                            String defaultScreenColour = allDataBits.Substring(234 - 225, 5);
                                            System.Diagnostics.Debug.WriteLine("Default Screen Colour: " + defaultScreenColour);
                                            defaultBackgroundColour = ColourLookup(Convert.ToByte(defaultScreenColour.Substring(0, 2), 2), Convert.ToByte(defaultScreenColour.Substring(2, 3), 2), "BG");

                                            String defaultRowColour = allDataBits.Substring(234 - 230, 5);
                                            System.Diagnostics.Debug.WriteLine("Default Row Colour: " + defaultRowColour);

                                            String blkBgndColourSubs = allDataBits.Substring(234 - 231, 1);
                                            System.Diagnostics.Debug.WriteLine("Black Background Colour Substitution: " + blkBgndColourSubs);

                                            String colTableRemap = allDataBits.Substring(234 - 234, 3);
                                            System.Diagnostics.Debug.WriteLine("Colour Table Re-mapping for use with Spacing Attributes: " + colTableRemap);
                                            colourTableRemapping = colTableRemap;

                                            break;
                                        }

                                    default:
                                        {
                                            System.Diagnostics.Debug.WriteLine("Format X/28/" + hrDC28.Value);

                                            Boolean terminationMarker = false;
                                            String allDataBits = "";
                                            for (Int32 triplet = 1; triplet < 14 & !terminationMarker; triplet++)
                                            {
                                                Byte byte1 = workingLine.Bytes[(triplet - 1) * 3 + 3];
                                                Byte byte2 = workingLine.Bytes[(triplet - 1) * 3 + 4];
                                                Byte byte3 = workingLine.Bytes[(triplet - 1) * 3 + 5];

                                                System.Diagnostics.Debug.Print("\n---------------------------\nLevel 2 X/28/" + hrDC28.Value + " Triplet " + triplet + "\n");

                                                HammingResults2418 hr = new HammingResults2418();
                                                HammingResults2418 hr2 = new HammingResults2418();

                                                hr = hr.HammingCheck2418a(byte1, byte2, byte3);
                                                System.Diagnostics.Debug.Print("\nHR1: Data bits:" + hr.Bits + "\n" + hr.ErrorString + "\nOriginal bytes: " + Convert.ToString(byte1, 16).PadLeft(2, Convert.ToChar("0")) + " " + Convert.ToString(byte2, 16).PadLeft(2, Convert.ToChar("0")) + " " + Convert.ToString(byte3, 16).PadLeft(2, Convert.ToChar("0")));

                                                allDataBits = hr.Bits + allDataBits;
                                            }




                                            for (Int32 clut = 0; clut < 2; clut++)
                                            {
                                                for (Int32 colour = 0; colour < 8; colour++)
                                                {
                                                    String colourString = allDataBits.Substring(234 - 30 - (((clut * 8) + colour) * 12), 12);

                                                    //System.Diagnostics.Debug.WriteLine("colourString.Substring(0, 4) {0}, Convert.ToByte(colourString.Substring(0, 4), 2) {1}", colourString.Substring(0, 4), Convert.ToByte(colourString.Substring(0, 4), 2));
                                                    String red = Convert.ToByte(colourString.Substring(0, 4), 2).toHex(1);
                                                    String green = Convert.ToByte(colourString.Substring(4, 4), 2).toHex(1);
                                                    String blue = Convert.ToByte(colourString.Substring(8, 4), 2).toHex(1);

                                                    ColourMap[clut + 2, colour] = Convert.ToInt32(blue + green + red, 16);
                                                    System.Diagnostics.Debug.WriteLine("Colour Map Entry Coding for CLUTs 2 and 3 (CLUT {0}, Colour {1}: {2} = ${3}", clut + 2, colour, colourString, blue + green + red);
                                                }
                                            }

                                            break;
                                        }

                                }
                            }
                            break;
                        }
                    case 29:
                        {
                            System.Diagnostics.Debug.WriteLine("Packet 29");
                        }
                        break;
                    case 30:
                        {
                            System.Diagnostics.Debug.WriteLine("Packet 30");
                        }
                        break;
                    case 31:
                        {
                            System.Diagnostics.Debug.WriteLine("Packet 31");
                        }
                        break;
                }

            }


            // ***************************************



        }

        private void DecodeX26(Line workingLine, Page page, RenderedLayersNova layers)
        {
            // Make sure we render stuff with text turned on
            _flashTextOn = true;

            // Get designation code

            // Set defaults
            Int32 initialRowColour = 0;
            Int32 currentRowColour = 0;
            Int32 currentForeColour = 0;
            Int32 currentBackColour = 0;

            Int32 debugLineTripletLimit = 255;
            Int32 debugTripletCount = 0;

            Boolean terminationMarker = false;

            // Loop triplets
            for (Int32 t = 0; t < 13
                && debugTripletCount < debugLineTripletLimit
                && !terminationMarker
                && debugTotalTripletCount < debugTotalTripletLimit;
                t++)
            {
                debugTripletCount++;
                debugTotalTripletCount++;


                Byte byte1 = workingLine.Bytes[t * 3 + 3];
                Byte byte2 = workingLine.Bytes[t * 3 + 4];
                Byte byte3 = workingLine.Bytes[t * 3 + 5];

                /*Byte byte1 = workingLine.Bytes[t * 3 + 6];
                Byte byte2 = workingLine.Bytes[t * 3 + 5];
                Byte byte3 = workingLine.Bytes[t * 3 + 4];*/

                System.Diagnostics.Debug.Print("\n---------------------------\n\nX26 Line Triplet {0}, total triplet {1}\n", t, debugTotalTripletCount);




                HammingResults2418 hr = new HammingResults2418();
                HammingResults2418 hr2 = new HammingResults2418();

                hr = hr.HammingCheck2418a(byte1, byte2, byte3);
                System.Diagnostics.Debug.Print("\nHR1: " + hr.Result + "\n" + hr.ErrorString + "\nOriginal bytes: " + Convert.ToString(byte1, 16).PadLeft(2, Convert.ToChar("0")) + " " + Convert.ToString(byte2, 16).PadLeft(2, Convert.ToChar("0")) + " " + Convert.ToString(byte3, 16).PadLeft(2, Convert.ToChar("0")));


                //hr2 = hr2.HammingCheck2418(byte1, byte2, byte3);
                //System.Diagnostics.Debug.Print("\nHR2: " + hr2.Result + "\n" + hr2.ErrorString + "\nOriginal bytes: " + Convert.ToString(byte1, 16).PadLeft(2, Convert.ToChar("0")) + " " + Convert.ToString(byte2, 16).PadLeft(2, Convert.ToChar("0")) + " " + Convert.ToString(byte3, 16).PadLeft(2, Convert.ToChar("0")) + "\n");


                if (!hr.FatalError)
                {
                    Int32 address = Convert.ToInt32(hr.Address, 2);



                    String mode = (address > 39 ? "Row" : "Column");



                    System.Diagnostics.Debug.Print("Address Type: " + mode);

                    switch (mode)
                    {
                        case "Row":
                            {
                                if (_presentationLevel >= tripletRowFunc[hr.Mode].PresentationLevel)
                                    System.Diagnostics.Debug.Print("Function: " + tripletRowFunc[hr.Mode].Description);
                                switch (hr.Mode)
                                {
                                    case "00000":
                                        {
                                            //Full Screen Colour
                                            if (_presentationLevel >= tripletRowFunc[hr.Mode].PresentationLevel)
                                            {
                                                Byte CLUT = Convert.ToByte(hr.Data.Substring(2, 2), 2);
                                                Byte clutEntry = Convert.ToByte(hr.Data.Substring(4, 3), 2);
                                                Color col = ColourLookup(CLUT, clutEntry, "BG");


// Set the image attribute's color mappings
                                                ColorMap[] colorMap = new ColorMap[1];
                                                colorMap[0] = new ColorMap();
                                                colorMap[0].OldColor = Color.Black;
                                                colorMap[0].NewColor = col;
                                                ImageAttributes attr = new ImageAttributes();
                                                attr.SetRemapTable(colorMap);
                                                // Draw using the color map
                                                Rectangle rect = new Rectangle(0, 0, layers.Background.Width, layers.Background.Height);
                                                using (Graphics g = Graphics.FromImage(layers.Background))
                                                {
                                                    g.FillRectangle(new SolidBrush(col), 0, 0, layers.Background.Width, layers.Background.Height);
                                                }


                                            }
                                            else
                                            {
                                                System.Diagnostics.Debug.Print("Function " + tripletRowFunc[hr.Mode].Description + " is not supported at this presentation level (" + _presentationLevel + ")");
                                            }
                                            break;
                                        }
                                    case "00001":
                                        {
                                            // Full Row Colour
                                            if (_presentationLevel >= tripletRowFunc[hr.Mode].PresentationLevel)
                                            {
                                                if (hr.RowColumn != 0)
                                                {
                                                    activeY = hr.RowColumn;
                                                    System.Diagnostics.Debug.Print("Current Y position changed to " + hr.RowColumn);
                                                    activeX = 0;
                                                    System.Diagnostics.Debug.Print("Current X position changed to 0");
                                                }

                                                //Set colours
                                                System.Diagnostics.Debug.Print(hr.Data.Substring(0, 2) != "11" ? "applies only to this row" : "applies to this position and the rest of the page");
                                                System.Diagnostics.Debug.Print("CLUT: " + hr.Data.Substring(2, 2));

                                                Byte CLUT = Convert.ToByte(hr.Data.Substring(2, 2), 2);
                                                Byte clutEntry = Convert.ToByte(hr.Data.Substring(4, 3), 2);



                                                Boolean fillToEndOfPage = ((hr.Data.Substring(0, 2) == "11") ? true : false);
                                                //Boolean fillToEndOfPage = true;

                                                // Set colour to borders
                                                Color col = ColourLookup(CLUT, clutEntry, "BG");
                                                using (Graphics grBackground = Graphics.FromImage(layers.Background))
                                                {
                                                    grBackground.FillRectangle(new SolidBrush(col), 0, level1YStart + (activeY * 20), level1XStart, fillToEndOfPage ? layers.Background.Height - (level1YStart + (activeY * 20)) : 20);
                                                    grBackground.FillRectangle(new SolidBrush(col), level1XStart + (40 * 12), level1YStart + (activeY * 20), layers.Background.Width - (level1XStart + (40 * 12)), fillToEndOfPage ? layers.Background.Height - (level1YStart + (activeY * 20)) : 20);
                                                }

                                                for (Int32 y = activeY; (y < 25 && fillToEndOfPage) || (y == activeY); y++)
                                                {
                                                    //fillToEndOfPage = ((hr.Data.Substring(0, 2) == "11") ? true : false);

                                                    Int32 initialDoubleWidthX = -1;
                                                    Boolean changeBG = true;

                                                    // Copy the colour along to the next change
                                                    for (Int32 x = 0;
                                                        x < 40

                                                        ; x++)
                                                    {
                                                        if (page.modeMapL2[y, x].Character == 0x1c)
                                                            changeBG = true;

                                                        if (changeBG)
                                                        {
                                                            if (page.modeMapL2[y, x].Character != 0x1d)
                                                            {
                                                                // Set the level 2 colour on the mode map
                                                                page.modeMapL2[y, x].BgndCLUT = Convert.ToByte(hr.Data.Substring(2, 2), 2);
                                                                page.modeMapL2[y, x].BgndColourCode = clutEntry;
                                                                page.modeMapL2[y, x].FullRowColour = true;

                                                                // Redraw the level 1 chars under the level 2 layer in the new FG colour on the level 2 layer
                                                                RenderL2Character(ref page, ref layers, ref initialDoubleWidthX, x, y);
                                                            }
                                                            else
                                                                changeBG = false;
                                                        }
                                                    }
                                                }

                                                // Do fill to end of page
                                                if (fillToEndOfPage)
                                                {
                                                    using (Graphics grBackground = Graphics.FromImage(layers.Background))
                                                    {
                                                        grBackground.FillRectangle(new SolidBrush(col), 0, level1YStart + (activeY * 20), layers.Background.Width, layers.Background.Height - (level1YStart + (activeY * 20)));
                                                        //grBackground.FillRectangle(new SolidBrush(col), level1XStart + (40 * 12), level1YStart + (activeY * 20), layers.Background.Width - (level1XStart + (40 * 12)), fillToEndOfPage ? layers.Background.Height - (level1YStart + (activeY * 20)) : 20);
                                                    }
                                                }

                                                String r = GetColourString(clutEntry);
                                                System.Diagnostics.Debug.Print("CLUT entry: " + clutEntry + ", " + r);
                                            }
                                            else
                                            {
                                                System.Diagnostics.Debug.Print("Function " + tripletRowFunc[hr.Mode].Description + " is not supported at this presentation level (" + _presentationLevel + ")");
                                            }
                                            break;
                                        }
                                    case "00100":
                                        {
                                            if (_presentationLevel >= tripletRowFunc[hr.Mode].PresentationLevel)
                                            {
                                                //activeY = Convert.ToInt32(hr.Address, 2);
                                                System.Diagnostics.Debug.Print("Old position = (" + activeX + ", " + activeY + ")");
                                                activeY = hr.RowColumn;
                                                activeX = Convert.ToInt32(hr.Data, 2);
                                                System.Diagnostics.Debug.Print("New position = (" + activeX + ", " + activeY + ")");
                                            }
                                            else
                                            {
                                                System.Diagnostics.Debug.Print("Function " + tripletRowFunc[hr.Mode].Description + " is not supported at this presentation level (" + _presentationLevel + ")");
                                            }
                                            break;
                                        }
                                    /*case "10000":
                                        {
                                            System.Diagnostics.Debug.Print("Origin Modifier: ");
                                            break;
                                        }
                                    case "10001":
                                        {
                                            System.Diagnostics.Debug.Print("Object Invocation: ");
                                            break;
                                        }*/
                                    case "11111":
                                        {
                                            if (_presentationLevel >= tripletRowFunc[hr.Mode].PresentationLevel)
                                            {
                                                terminationMarker = true;
                                            }
                                            else
                                            {
                                                System.Diagnostics.Debug.Print("Function " + tripletRowFunc[hr.Mode].Description + " is not supported at this presentation level (" + _presentationLevel + ")");
                                            }
                                            break;
                                        }
                                    default:
                                        {
                                            System.Diagnostics.Debug.Print("Function: Unsupported Row Command: " + hr.Mode + " " + tripletRowFunc[hr.Mode].Description);
                                            break;
                                        }
                                }
                                break;
                            }
                        case "Column":
                            {
                                if (_presentationLevel >= tripletColumnFunc[hr.Mode].PresentationLevel)
                                    System.Diagnostics.Debug.Print("Function: " + tripletColumnFunc[hr.Mode].Description);
                                switch (hr.Mode)
                                {
                                    case "00000":
                                        {
                                            //Foreground Colour
                                            if (_presentationLevel >= tripletColumnFunc[hr.Mode].PresentationLevel)
                                            {
                                                //Check that D6 and D5 are both 0
                                                System.Diagnostics.Debug.Print("D6/D5: " + hr.Data.Substring(0, 2));

                                                System.Diagnostics.Debug.Print("Old X, Y: " + activeX + ", " + activeY);
                                                activeX = hr.RowColumn;
                                                System.Diagnostics.Debug.Print("X position changed to " + hr.RowColumn);
                                                System.Diagnostics.Debug.Print("New X, Y: " + activeX + ", " + activeY);

                                                System.Diagnostics.Debug.Print("CLUT: " + hr.Data.Substring(2, 2));

                                                // Store old value of forecolour
                                                Byte oldForeColour = page.modeMapL2[activeY, activeX].ForeColourCode;

                                                Byte clutEntry = Convert.ToByte(hr.Data.Substring(4, 3), 2);
                                                page.modeMapL2[activeY, activeX].ForeColourCode = clutEntry;
                                                page.modeMapL2[activeY, activeX].ForeCLUT = Convert.ToByte(hr.Data.Substring(2, 2), 2);

                                                Int32 initialDoubleWidthX = -1;


                                                // Copy the colour along to the next foreground colour change
                                                Boolean colourChangeLatch = false;
                                                for (Int32 x = activeX; x < 40 && !colourChangeLatch; x++)
                                                {
                                                    //if (x == 19 && activeY == 16)
                                                    //{
                                                    //    System.Diagnostics.Debug.Print("Character: 0x" + Convert.ToString(page.modeMapL2[activeY, x].Character, 16));
                                                    //    System.Diagnostics.Debug.Print("Character + 1: 0x" + Convert.ToString(page.modeMapL2[activeY, x+1].Character, 16));

                                                    //    System.Diagnostics.Debug.Print("CLUT: " + Convert.ToByte(hr.Data.Substring(2, 2), 2));
                                                    //    System.Diagnostics.Debug.Print("CLUT Entry: " + clutEntry);
                                                    //    System.Diagnostics.Debug.Print("IT IS HERE - WE ARE STOPPING SHORT FOR SOME REASON");
                                                    //}

                                                    // Set the level 2 colour on the mode map
                                                    page.modeMapL2[activeY, x].ForeColourCode = clutEntry;
                                                    page.modeMapL2[activeY, x].ForeCLUT = Convert.ToByte(hr.Data.Substring(2, 2), 2);

                                                    // Redraw the level 1 chars under the level 2 layer in the new FG colour on the level 2 layer
                                                    RenderL2Character(ref page, ref layers, ref initialDoubleWidthX, x, activeY);

                                                    // Set the latch if the colour has changed
                                                    if ((page.modeMapL2[activeY, x].Character >= 0 && page.modeMapL2[activeY, x].Character < 0x08)
                                                        || (page.modeMapL2[activeY, x].Character >= 0x10 && page.modeMapL2[activeY, x].Character < 0x18))
                                                        colourChangeLatch = true;
                                                }
                                            }
                                            else
                                            {
                                                System.Diagnostics.Debug.Print("Function " + tripletRowFunc[hr.Mode].Description + " is not supported at this presentation level (" + _presentationLevel + ")");
                                            }
                                            break;
                                        }

                                    case "00001":
                                        {
                                            //Block Mosaic Character from the G1 set
                                            if (_presentationLevel >= tripletColumnFunc[hr.Mode].PresentationLevel)
                                            {
                                                System.Diagnostics.Debug.Print("Old X, Y: " + activeX + ", " + activeY);
                                                activeX = hr.RowColumn;
                                                System.Diagnostics.Debug.Print("X position changed to " + hr.RowColumn);
                                                System.Diagnostics.Debug.Print("New X, Y: " + activeX + ", " + activeY);

                                                System.Diagnostics.Debug.Print("ForeColour: {0}, BackColour: {1}", page.modeMapL2[activeY, activeX].ForeColourCode, page.modeMapL2[activeY, activeX].BgndColourCode);

                                                Byte c = Convert.ToByte(hr.Data, 2);
                                                page.modeMapL2[activeY, activeX].Character = c;
                                                page.modeMapL2[activeY, activeX].CharacterSet = "G1";

                                                Int32 initialDoubleWidthX = -1;

                                                if (c >= 0x20)
                                                {
                                                    //     Bitmap chr = (Bitmap)font["00" + (c + 0x60).toHex(2)];
                                                    page.modeMapL2[activeY, activeX].Character = (Byte)c;
                                                    page.modeMapL2[activeY, activeX].CharacterSet = "G1";
                                                    page.modeMapL2[activeY, activeX].FullRowColour = true;
                                                    System.Diagnostics.Debug.Print("(C00001) Selected char: " + (c).toHex(2).ToString() + "(filename char00" + (c).toHex(2).ToString() + ".bmp) ForeColour: " + currentForeColour + ", BackColour: " + currentBackColour);
                                                    //    if (chr != null)
                                                    //        layers = DrawChar(ref layers, ref page, chr, activeX, activeY);
                                                    RenderL2Character(ref page, ref layers, ref initialDoubleWidthX, activeX, activeY);
                                                }


                                            }
                                            else
                                            {
                                                System.Diagnostics.Debug.Print("Function " + tripletRowFunc[hr.Mode].Description + " is not supported at this presentation level (" + _presentationLevel + ")");
                                            }
                                            break;
                                        }

                                    case "00011":
                                        {
                                            //Background Colour
                                            if (_presentationLevel >= tripletColumnFunc[hr.Mode].PresentationLevel)
                                            {
                                                //Check that D6 and D5 are both 0
                                                System.Diagnostics.Debug.Print("D6/D5: " + hr.Data.Substring(0, 2));
                                                if (Convert.ToInt32(hr.Data.Substring(0, 2)) == 0)
                                                {

                                                    System.Diagnostics.Debug.Print("Old X, Y: " + activeX + ", " + activeY);
                                                    activeX = hr.RowColumn;
                                                    System.Diagnostics.Debug.Print("X position changed to " + hr.RowColumn);
                                                    System.Diagnostics.Debug.Print("New X, Y: " + activeX + ", " + activeY);

                                                    System.Diagnostics.Debug.Print("CLUT: " + hr.Data.Substring(2, 2));
                                                    Byte clutEntry = Convert.ToByte(hr.Data.Substring(4, 3), 2);
                                                    page.modeMapL2[activeY, activeX].BgndColourCode = clutEntry;
                                                    page.modeMapL2[activeY, activeX].BgndCLUT = Convert.ToByte(hr.Data.Substring(2, 2), 2);

                                                    System.Diagnostics.Debug.Print("Colour map: {0}, CLUT entry: {1}\n", Convert.ToByte(hr.Data.Substring(2, 2), 2), clutEntry);




                                                    // Store old value of BGcolour
                                                    Byte oldBgndColour = page.modeMapL2[activeY, activeX].BgndColourCode;
                                                    //Boolean useL2Colour = true;

                                                    Int32 initialDoubleWidthX = -1;
                                                    Int32 initialDoubleHeightY = -1;

                                                    // Copy the colour along to the next change
                                                    for (Int32 x = activeX;
                                                        x < 40
                                                        && page.modeMapL2[activeY, x].Character != 0x1c
                                                        && page.modeMapL2[activeY, x].Character != 0x1d
                                                        ; x++)
                                                    {
                                                        // Set the level 2 colour on the mode map
                                                        //if (useL2Colour)

                                                        //else
                                                        //    page.modeMapL2[x, activeY].BgndColourCode = page.modeMap[x, activeY].BgndColourCode;



                                                        page.modeMapL2[activeY, x].BgndCLUT = Convert.ToByte(hr.Data.Substring(2, 2), 2);
                                                        page.modeMapL2[activeY, x].BgndColourCode = clutEntry;

                                                        // Redraw the level 1 chars under the level 2 layer in the new FG colour on the level 2 layer
                                                        RenderL2Character(ref page, ref layers, ref initialDoubleWidthX, x, activeY);

                                                    }
                                                }
                                            }
                                            else
                                            {
                                                System.Diagnostics.Debug.Print("Function " + tripletRowFunc[hr.Mode].Description + " is not supported at this presentation level (" + _presentationLevel + ")");
                                            }
                                            break;
                                        }
                                    case "00010":
                                        {
                                            //Line Drawing or Smoothed Mosaic Character from the G3 set (Level 1.5)
                                            if (_presentationLevel >= tripletColumnFunc[hr.Mode].PresentationLevel)
                                            {
                                                System.Diagnostics.Debug.Print("Old X, Y: " + activeX + ", " + activeY);
                                                activeX = hr.RowColumn;
                                                System.Diagnostics.Debug.Print("X position changed to " + hr.RowColumn);
                                                System.Diagnostics.Debug.Print("New X, Y: " + activeX + ", " + activeY);

                                                System.Diagnostics.Debug.Print("ForeColour: {0}, BackColour: {1}", page.modeMapL2[activeY, activeX].ForeColourCode, page.modeMapL2[activeY, activeX].BgndColourCode);

                                                Int32 initialDoubleWidthX = -1;

                                                Byte c = Convert.ToByte(hr.Data, 2);
                                                page.modeMapL2[activeY, activeX].Character = c;
                                                page.modeMapL2[activeY, activeX].CharacterSet = "G3";

                                                if (c >= 0x20)
                                                {
                                                    //    Bitmap chr = (Bitmap)font["G3" + c.toHex(2)];
                                                    //    if (chr != null)
                                                    //    {
                                                    //        System.Diagnostics.Debug.Print("(C01010) Selected char: " + (c).toHex(2).ToString() + "(filename charG3" + (c).toHex(2).ToString() + ".bmp) ForeColour: " + currentForeColour + ", BackColour: " + currentBackColour);
                                                    //        layers = DrawChar(ref layers, ref page, chr, activeX, activeY);
                                                    //    }
                                                }

                                            }
                                            else
                                            {
                                                System.Diagnostics.Debug.Print("Function " + tripletRowFunc[hr.Mode].Description + " is not supported at this presentation level (" + _presentationLevel + ")");
                                            }
                                            break;
                                        }

                                    case "00111":
                                        {
                                            /* Additional Flash Functions */
                                            if (_presentationLevel >= tripletColumnFunc[hr.Mode].PresentationLevel)
                                            {
                                                System.Diagnostics.Debug.Print("Old X, Y: " + activeX + ", " + activeY);
                                                activeX = hr.RowColumn;
                                                System.Diagnostics.Debug.Print("X position changed to " + hr.RowColumn);
                                                System.Diagnostics.Debug.Print("New X, Y: " + activeX + ", " + activeY);

                                                string flashMode = hr.Data.Substring(5, 2);
                                                string flashRateAndPhase = hr.Data.Substring(2, 3);

                                                Int32 initialDoubleWidthX = -1;

                                                // Copy to the end of the row
                                                for (int x = activeX; x < 40; x++)
                                                {
                                                    page.modeMapL2[activeY, x].Flash = true;
                                                    page.modeMapL2[activeY, x].FlashMode = flashMode;
                                                    page.modeMapL2[activeY, x].FlashRateAndPhase = flashRateAndPhase;
                                                    //int currentPhase = Convert.ToInt32(flashRateAndPhase.Substring(1, 2), 2);
                                                    //page.modeMapL2[activeY, x].FlashCurrentPhase =  currentPhase;

                                                    // This bit is for three-phase flash only
                                                    if (flashRateAndPhase != "000" && flashRateAndPhase.Substring(0, 1) != "1" && false)
                                                    {
                                                        int currentCLUT = page.modeMapL2[activeY, x].ForeCLUT;
                                                        switch (currentCLUT)
                                                        {
                                                            case 0:
                                                                {
                                                                    page.modeMapL2[activeY, x].ForeCLUT = 1;
                                                                    break;
                                                                }
                                                            case 1:
                                                                {
                                                                    page.modeMapL2[activeY, x].ForeCLUT = 0;
                                                                    break;
                                                                }
                                                            case 2:
                                                                {
                                                                    page.modeMapL2[activeY, x].ForeCLUT = 3;
                                                                    break;
                                                                }
                                                            case 3:
                                                                {
                                                                    page.modeMapL2[activeY, x].ForeCLUT = 2;
                                                                    break;
                                                                }
                                                        }

                                                        RenderL2Character(ref page, ref layers, ref initialDoubleWidthX, x, activeY);
                                                    }

                                                }

                                                System.Diagnostics.Debug.Write("Flash Mode: ");

                                                switch (flashMode)
                                                {
                                                    case "00":
                                                        {

                                                            System.Diagnostics.Debug.WriteLine("Steady");
                                                            break;
                                                        }
                                                    case "01":
                                                        {
                                                            System.Diagnostics.Debug.WriteLine("Normal Flash to Background Colour (not yet implemented)");
                                                            break;
                                                        }
                                                    case "10":
                                                        {
                                                            System.Diagnostics.Debug.WriteLine("Invert phase of flash to background colour (not yet implemented)");
                                                            break;
                                                        }
                                                    case "11":
                                                        {
                                                            System.Diagnostics.Debug.WriteLine("Flash to the corresponding colour in an adjacent CLUT");

                                                            break;
                                                        }
                                                }

                                                System.Diagnostics.Debug.Write("Flash Rate and Phase: ");


                                                switch (flashRateAndPhase)
                                                {
                                                    case "000":
                                                        {
                                                            System.Diagnostics.Debug.Write("Slow rate (1Hz) (unsupported)");
                                                            break;
                                                        }
                                                    case "001":
                                                        {
                                                            System.Diagnostics.Debug.Write("Fast rate (2Hz), phase 1");
                                                            break;
                                                        }
                                                    case "010":
                                                        {
                                                            System.Diagnostics.Debug.Write("Fast rate (2Hz), phase 2");
                                                            break;
                                                        }
                                                    case "011":
                                                        {
                                                            System.Diagnostics.Debug.Write("Fast rate (2Hz), phase 3");
                                                            break;
                                                        }
                                                    case "100":
                                                        {
                                                            System.Diagnostics.Debug.Write("Fast rate (2Hz), incremental flash, apparent (unsupported)");
                                                            break;
                                                        }
                                                    case "101":
                                                        {
                                                            System.Diagnostics.Debug.Write("Fast rate (2Hz), decremental flash (unsupported)");
                                                            break;
                                                        }
                                                    case "110":
                                                        {
                                                            System.Diagnostics.Debug.Write("Reserved");
                                                            break;
                                                        }
                                                    case "111":
                                                        {
                                                            System.Diagnostics.Debug.Write("Reserved");
                                                            break;
                                                        }
                                                }

                                            }

                                            break;
                                        }

                                    case "01011":
                                        {
                                            /*Line Drawing and Smoothed Mosaic Character from the G3 Set at
                                            Levels 2.5 and 3.5
                                            The 7 data field bits select a line drawing or smoothed mosaic character from the
                                            G3 set, table 48.
                                            Data field values < 20 hex are reserved but decoders should still set the column
                                            co-ordinate of the Active Position to the value of the address field.
                                            NOTE 3: This command is intended for use at Levels 2.5 and 3.5 only to
                                            ensure existing decoders remain compatible with Level 2.5 and 3.5
                                            transmissions. Level 1.5 decoders should ignore this command.*/

                                            if (_presentationLevel >= tripletColumnFunc[hr.Mode].PresentationLevel)
                                            {
                                                System.Diagnostics.Debug.Print("Old X, Y: " + activeX + ", " + activeY);
                                                activeX = hr.RowColumn;
                                                System.Diagnostics.Debug.Print("X position changed to " + hr.RowColumn);
                                                System.Diagnostics.Debug.Print("New X, Y: " + activeX + ", " + activeY);

                                                System.Diagnostics.Debug.Print("ForeColour: {0}, BackColour: {1}", page.modeMapL2[activeY, activeX].ForeColourCode, page.modeMapL2[activeY, activeX].BgndColourCode);

                                                Byte c = Convert.ToByte(hr.Data, 2);
                                                page.modeMapL2[activeY, activeX].Character = c;
                                                page.modeMapL2[activeY, activeX].CharacterSet = "G3";

                                                Int32 initialDoubleWidthX = -1;

                                                if (c >= 0x20)
                                                {
                                                    //    Bitmap chr = (Bitmap)font["G3" + c.toHex(2)];
                                                    //    if (chr != null)
                                                    //    {
                                                    //        System.Diagnostics.Debug.Print("(C01011) Selected char: " + (c).toHex(2).ToString() + "(filename charG3" + (c).toHex(2).ToString() + ".bmp) ForeColour: " + currentForeColour + ", BackColour: " + currentBackColour);
                                                    //        layers = DrawChar(ref layers, ref page, chr, activeX, activeY);
                                                    //    }
                                                    //    else
                                                    //    {
                                                    //        System.Diagnostics.Debug.Print("*** Character {0} not found! ***", "G3" + c.toHex(2));
                                                    //    }
                                                    System.Diagnostics.Debug.Print("(C01011) Selected char: " + (c).toHex(2).ToString() + "(filename char" + page.modeMapL2[activeY, activeX].CharacterSet + (c).toHex(2).ToString() + ".bmp) ForeColour: " + currentForeColour + ", BackColour: " + currentBackColour);
                                                    RenderL2Character(ref page, ref layers, ref initialDoubleWidthX, activeX, activeY);
                                                }
                                            }
                                            else
                                            {
                                                System.Diagnostics.Debug.Print("Function " + tripletRowFunc[hr.Mode].Description + " is not supported at this presentation level (" + _presentationLevel + ")");
                                            }
                                            break;
                                        }

                                    case "01100":
                                        {
                                            //Display Attributes
                                            if (_presentationLevel >= tripletColumnFunc[hr.Mode].PresentationLevel)
                                            {
                                                // Get a snapshot of what the display looks like now
                                                Bitmap flattened = new Bitmap(layers.Background.Width, layers.Background.Height, layers.Background.PixelFormat);
                                                using (Graphics g = Graphics.FromImage(flattened))
                                                {
                                                    g.CompositingMode = CompositingMode.SourceOver; // this is the default, but just to be clear

                                                    g.DrawImage(layers.Background, 0, 0);
                                                    //g.DrawImage(layers.L25Background, 0, 0);
                                                    g.DrawImage(layers.Foreground, 0, 0);
                                                }

                                                if (((Convert.ToByte(hr.Data, 2) & (Byte)Math.Pow(2, 6)) == Math.Pow(2, 6)) && !((Convert.ToByte(hr.Data, 2) & (Byte)Math.Pow(2, 0)) == Math.Pow(2, 0)))
                                                {
                                                    System.Diagnostics.Debug.Write("Double Width; ");

                                                    Int32 initialX = activeX;
                                                    Int32 initialY = activeY;

                                                    //System.Diagnostics.Debug.Write("Character @ active pos: " + page.modeMapL2[activeX, activeY].Character.toHex(2));
                                                    for (Int32 x = activeX;
                                                        x < 39 && page.modeMapL2[activeY, x + 1].Character != 0x0c && page.modeMapL2[activeY, x + 1].Character != 0x0d;
                                                        x++)
                                                    {
                                                        System.Diagnostics.Debug.Write(Convert.ToChar(page.modeMapL2[activeY, x].Character) + " ");

                                                        if ((x - initialX) % 2 == 0 && (activeY - initialY) % 2 == 0)
                                                        {
                                                            // Get area to enlarge
                                                            Bitmap orig = new Bitmap(12, 20, layers.Background.PixelFormat);
                                                            Rectangle sourceRect = new Rectangle(x * 12, activeY * 20, 12, 20);
                                                            using (Graphics g = Graphics.FromImage(orig))
                                                            {
                                                                g.DrawImage(flattened, 0, 0, sourceRect, GraphicsUnit.Pixel);
                                                            }

                                                            //Enlarge it
                                                            Bitmap widened = ConvertToDoubleWidth(orig);

                                                            //widened.Save("widened.png", ImageFormat.Png);
                                                            //orig.Save("orig.png", ImageFormat.Png);
                                                            // flattened.Save("flattened.png", ImageFormat.Png);

                                                            // Paste onto foreground
                                                            using (Graphics g = Graphics.FromImage(layers.Foreground))
                                                            {
                                                                g.DrawImage(widened, x * 12, activeY * 20);
                                                            }
                                                        }
                                                    }
                                                }

                                                if ((Convert.ToByte(hr.Data, 2) & (Byte)Math.Pow(2, 5)) == Math.Pow(2, 5))
                                                {
                                                    System.Diagnostics.Debug.Write("Underline or Separated Mosaics; ");

                                                    Int32 initialDoubleWidthX = -1;

                                                    for (Int32 x = activeX;
                                                        x < 40;
                                                        x++)
                                                    {
                                                        if ((!page.modeMapL2[activeY, x].Graphics && page.modeMapL2[activeY, x].Character >= 0x40 && page.modeMapL2[activeY, x].Character <= 0x5f) ||
                                                            (page.modeMapL2[activeY, x].Graphics && page.modeMapL2[activeY, x].Character >= 0x20 && page.modeMapL2[activeY, x].Character <= 0x7f))
                                                        {
                                                            // Mark in the mode map
                                                            if (!page.modeMapL2[activeY, x].Graphics)
                                                                page.modeMapL2[activeY, x].Underlined = true;

                                                            if (page.modeMapL2[activeY, x].Graphics)
                                                            {
                                                                page.modeMap[activeY, x].SeparatedGraphics = true;
                                                                page.modeMapL2[activeY, x].SeparatedGraphics = true;
                                                            }

                                                            RenderL2Character(ref page, ref layers, ref initialDoubleWidthX, x, activeY);

                                                            System.Diagnostics.Debug.Write("(" + x + ", " + activeY + ") ");

                                                            // How big are the chars here?
                                                            //Int32 charWidth = (page.modeMapL2[x, activeY].DoubleWidth ? 24 : 12);
                                                            //Int32 charHeight = (page.modeMapL2[x, activeY].DoubleHeight ? 40 : 20);

                                                            // Get area to underline
                                                            //Bitmap orig = new Bitmap(12, 20, layers.Background.PixelFormat);
                                                            //Rectangle sourceRect = new Rectangle(x * 12, activeY * 20, charWidth, charHeight);
                                                            //using (Graphics g = Graphics.FromImage(orig))
                                                            //{
                                                            //    g.DrawImage(flattened, 0, 0, sourceRect, GraphicsUnit.Pixel);
                                                            //    //ColorPalette pal = GetColour(page, x, activeY, chr.Palette);

                                                            //    //Pen p = new Pen(orig.Palette.Entries[1]);
                                                            //    //g.DrawRectangle(p, 0, 19, 20, 1);
                                                            //    orig.Save("underlined.png", ImageFormat.Png);
                                                            //}

                                                            //Enlarge it
                                                            //Bitmap underlined = ConvertToUnderlined(orig);

                                                            //widened.Save("widened.png", ImageFormat.Png);
                                                            //orig.Save("underlined2.png", ImageFormat.Png);
                                                            // flattened.Save("flattened.png", ImageFormat.Png);


                                                        }


                                                    }
                                                }

                                                if ((Convert.ToByte(hr.Data, 2) & (Byte)Math.Pow(2, 4)) == Math.Pow(2, 4))
                                                {
                                                    System.Diagnostics.Debug.Write("Unsupported Display Attribute: Invert Colour; ");
                                                }

                                                if ((Convert.ToByte(hr.Data, 2) & (Byte)Math.Pow(2, 3)) == Math.Pow(2, 3))
                                                {
                                                    System.Diagnostics.Debug.Write("Unsupported Display Attribute: Reserved; ");
                                                }

                                                if ((Convert.ToByte(hr.Data, 2) & (Byte)Math.Pow(2, 2)) == Math.Pow(2, 2))
                                                {
                                                    System.Diagnostics.Debug.Write("Unsupported Display Attribute: Conceal; ");
                                                }

                                                if ((Convert.ToByte(hr.Data, 2) & (Byte)Math.Pow(2, 1)) == Math.Pow(2, 1))
                                                {
                                                    System.Diagnostics.Debug.Write("Unsupported Display Attribute: Boxing / Window; ");
                                                }

                                                if (((Convert.ToByte(hr.Data, 2) & (Byte)Math.Pow(2, 0)) == Math.Pow(2, 0)) && !((Convert.ToByte(hr.Data, 2) & (Byte)Math.Pow(2, 6)) == Math.Pow(2, 6)))
                                                {
                                                    System.Diagnostics.Debug.Write("Double Height");

                                                    //System.Diagnostics.Debug.Write("Character @ active pos: " + page.modeMapL2[activeX, activeY].Character.toHex(2));
                                                    for (Int32 x = activeX;
                                                        x < 39 && page.modeMapL2[activeY, x + 1].Character != 0x0c && page.modeMapL2[activeY, x + 1].Character != 0x0d;
                                                        x++)

                                                    {
                                                        System.Diagnostics.Debug.Write(Convert.ToChar(page.modeMapL2[activeY, x].Character) + " ");

                                                        //if (page.modeMapL2[x, activeY].Character > 0x20)
                                                        //{
                                                        // Get area to enlarge
                                                        Bitmap orig = new Bitmap(12, 20, layers.Background.PixelFormat);
                                                        Rectangle sourceRect = new Rectangle(x * 12, activeY * 20, 12, 20);
                                                        using (Graphics g = Graphics.FromImage(orig))
                                                        {
                                                            g.DrawImage(flattened, 0, 0, sourceRect, GraphicsUnit.Pixel);
                                                        }

                                                        //Enlarge it
                                                        Bitmap stretched = ConvertToDoubleHeight(orig);

                                                        //stretched.Save("stretched.png", ImageFormat.Png);
                                                        //orig.Save("orig.png", ImageFormat.Png);
                                                        //flattened.Save("flattened.png", ImageFormat.Png);

                                                        // Paste onto foreground
                                                        using (Graphics g = Graphics.FromImage(layers.Foreground))
                                                        {
                                                            g.DrawImage(stretched, x * 12, activeY * 20);
                                                        }
                                                        //}

                                                    }
                                                }

                                                if (((Convert.ToByte(hr.Data, 2) & (Byte)Math.Pow(2, 6)) == Math.Pow(2, 6)) && ((Convert.ToByte(hr.Data, 2) & (Byte)Math.Pow(2, 0)) == Math.Pow(2, 0)))
                                                {
                                                    System.Diagnostics.Debug.Write("Double Width and Double height; ");
                                                    Int32 initialX = activeX;
                                                    Int32 initialY = activeY;
                                                    Int32 initialDoubleWidthX = -1;

                                                    //System.Diagnostics.Debug.Write("Character @ active pos: " + page.modeMapL2[activeX, activeY].Character.toHex(2));
                                                    for (Int32 x = activeX;
                                                        x < 40 && page.modeMapL2[activeY, x + 1].Character != 0x0c && page.modeMapL2[activeY, x + 1].Character != 0x0d;
                                                        x++)
                                                    {
                                                        System.Diagnostics.Debug.Write(Convert.ToChar(page.modeMapL2[activeY, x].Character) + " ");

                                                        page.modeMapL2[activeY, x].DoubleSize = true;
                                                        page.modeMapL2[activeY, x].DoubleHeight = true;
                                                        page.modeMapL2[activeY, x].DoubleWidth = true;

                                                        RenderL2Character(ref page, ref layers, ref initialDoubleWidthX, x, activeY);

                                                    }
                                                }

                                            }
                                            else
                                            {
                                                System.Diagnostics.Debug.Print("Function " + tripletRowFunc[hr.Mode].Description + " is not supported at this presentation level (" + _presentationLevel + ")");
                                            }
                                            break;
                                        }

                                    case "01111":
                                        {
                                            // Character from the G2 Supplementary set
                                            if (_presentationLevel >= tripletColumnFunc[hr.Mode].PresentationLevel)
                                            {
                                                System.Diagnostics.Debug.Print("Old X, Y: " + activeX + ", " + activeY);
                                                activeX = hr.RowColumn;
                                                System.Diagnostics.Debug.Print("X position changed to " + hr.RowColumn);
                                                System.Diagnostics.Debug.Print("New X, Y: " + activeX + ", " + activeY);

                                                System.Diagnostics.Debug.Print("ForeColour: {0}, BackColour: {1}", page.modeMapL2[activeY, activeX].ForeColourCode, page.modeMapL2[activeY, activeX].BgndColourCode);

                                                Int32 initialDoubleWidthX = -1;

                                                Byte c = Convert.ToByte(hr.Data, 2);
                                                page.modeMapL2[activeY, activeX].Character = c;
                                                page.modeMapL2[activeY, activeX].CharacterSet = "G2S";

                                                if (c >= 0x20)
                                                {
                                                    //Bitmap chr = (Bitmap)font["G3" + c.toHex(2)];
                                                    //if (chr != null)
                                                    //{
                                                    System.Diagnostics.Debug.Print("(C01111) Selected char: " + (c).toHex(2).ToString() + "(filename charG2S" + (c).toHex(2).ToString() + ".bmp) ForeColour: " + currentForeColour + ", BackColour: " + currentBackColour);
                                                    RenderL2Character(ref page, ref layers, ref initialDoubleWidthX, activeX, activeY);
                                                    //  layers = DrawChar(ref layers, ref page, chr, activeX, activeY);
                                                    //}
                                                }

                                            }
                                            else
                                            {
                                                System.Diagnostics.Debug.Print("Function " + tripletRowFunc[hr.Mode].Description + " is not supported at this presentation level (" + _presentationLevel + ")");
                                            }
                                            break;
                                        }

                                    default:
                                        {
                                            if (hr.Mode.Substring(0, 1) != "1")
                                                System.Diagnostics.Debug.Print("Function: Unsupported Column Command: " + hr.Mode + " - " + tripletColumnFunc[hr.Mode].Description);
                                            break;
                                        }
                                }

                                // Support for column modes 10000 - 11111 (see p95 of ETS)

                                if (hr.Mode.Substring(0, 1) == "1")
                                {
                                    Int32 initialDoubleWidthX = -1;

                                    byte G0Code = Convert.ToByte(hr.Data, 2);
                                    byte G2SuppCode = Convert.ToByte(hr.Mode.Substring(1, 4), 2);
                                    activeX = hr.RowColumn;
                                    System.Diagnostics.Debug.WriteLine("G0 Character: {0}; G2 Supplementary Diacritical Mark Code: {1}", G0Code, G2SuppCode);
                                    System.Diagnostics.Debug.WriteLine("Active X changed to {0}", activeX);


                                    page.modeMapL2[activeY, activeX].Character = G0Code;
                                    page.modeMapL2[activeY, activeX].Diacritical = G2SuppCode;
                                    RenderL2Character(ref page, ref layers, ref initialDoubleWidthX, activeX, activeY);
                                }

                                break;

                            }

                    }
                }

                if (_saveX26Anim)
                {
                    Bitmap b = new Bitmap(layers.Background);

                    using (Graphics g = Graphics.FromImage(b))
                    {
                        g.DrawImage(layers.Foreground, 0, 0);
                    }

                    b.Save(Environment.GetEnvironmentVariable("temp") + "\\teletext\\triplets\\triplet" + debugTotalTripletCount.ToString().PadLeft(3, Convert.ToChar("0")) + ".png");
                }
            }

            if (_saveX26Anim)
            {
                int framerate = 3;
                System.Diagnostics.Debug.WriteLine("ffmpeg.exe -framerate " + framerate + " -i " + Environment.GetEnvironmentVariable("temp") + "\\teletext\\triplets\\triplet%03d.png " + System.IO.Path.GetTempPath() + "\\teletext\\triplets\\triplets.gif");
                System.Diagnostics.ProcessStartInfo procStartInfo = new System.Diagnostics.ProcessStartInfo("cmd", "/c ffmpeg.exe -framerate " + framerate + " -i " + Environment.GetEnvironmentVariable("temp") + "\\teletext\\triplets\\triplet%03d.png " + System.IO.Path.GetTempPath() + "triplets\\triplets.gif");

            }

        }

        public void RenderL2Character(ref Page page, ref RenderedLayersNova layers, ref int initialDoubleWidthX, int x, int y, Boolean debug = true)
        {
            if (y != 0)
            {

                if (debug)
                    System.Diagnostics.Debug.WriteLine("Character Set: " + page.modeMapL2[y, x].CharacterSet + ", Character on L2 mode map: " + page.modeMapL2[y, x].Character.toHex(2));

                Byte bytGraphicsOffset = (Byte)((page.modeMap[y, x].Graphics && page.modeMap[y, x].CharacterSet == "G0") || page.modeMap[y, x].CharacterSet == "G1" ? 0x60 : 0x00);
                String glyphPrefix = "00";

                //if (page.modeMap[x, y].CharacterSet == "G1")
                //{
                //    bytGraphicsOffset = 0;
                //    glyphPrefix = "00";
                //}

                // Support for blast-through alphanumerics
                if (page.modeMap[y, x].Character >= 0x40 && page.modeMap[y, x].Character <= 0x5f)
                    bytGraphicsOffset = 0;

                String strCharToDraw = "20";
                if (page.modeMap[y, x].Character >= 0x20)
                    strCharToDraw = Convert.ToByte(page.modeMapL2[y, x].Character + bytGraphicsOffset).toHex(2);

                if (page.modeMapL2[y, x].CharacterSet != "G0" && page.modeMapL2[y, x].CharacterSet != "G1" && page.modeMapL2[y, x].CharacterSet != null)
                {
                    strCharToDraw = page.modeMapL2[y, x].Character.toHex(2);
                    glyphPrefix = page.modeMapL2[y, x].CharacterSet;
                }

                if (page.modeMapL2[y, x].CharacterSet == "G1")
                {
                    byte bytASCII = page.modeMapL2[y, x].Character;

                    if (page.modeMapL2[y, x].Graphics && !page.modeMapL2[y, x].SeparatedGraphics)
                        bytASCII += 0x60;

                    if (page.modeMapL2[y, x].Graphics && page.modeMapL2[y, x].SeparatedGraphics)
                        bytASCII += 0x80;

                    strCharToDraw = bytASCII.toHex(2);
                    glyphPrefix = "00";
                }

                if (page.modeMap[y, x].Character < 0x20 && page.modeMap[y, x].Hold)
                    strCharToDraw = page.modeMapL2[y, x].HeldGraphicsChar.toHex(2);

                //if (Convert.ToByte(strCharToDraw, 16) < 0x20 || (glyphPrefix == "G3" && (Convert.ToByte(strCharToDraw, 16) > 0x7d || Convert.ToByte(strCharToDraw,16) > 0x6d || Convert.ToByte(strCharToDraw, 16) == 0x5f)))
                if (Convert.ToByte(strCharToDraw, 16) < 0x20 || (glyphPrefix == "G3" && Convert.ToByte(strCharToDraw, 16) >= 0x7e) ||
                    (glyphPrefix == "G3" && Convert.ToByte(strCharToDraw, 16) >= 0x6e && glyphPrefix == "G3" && Convert.ToByte(strCharToDraw, 16) <= 0x6f))
                {
                    if (debug)
                        System.Diagnostics.Debug.WriteLine("Invalid character 0x" + strCharToDraw + ", value forced to 0x20");
                    strCharToDraw = "20";
                }

                if (debug)
                    System.Diagnostics.Debug.WriteLine("Glyph id: " + glyphPrefix + strCharToDraw);

                Image chr;

                if (_flashTextOn)
                    chr = (Image)font[glyphPrefix + strCharToDraw];
                else
                {
                    strCharToDraw = "20";
                    chr = (Image)font["0020"];
                }

                ColorPalette pal = GetColour(page, x, y, chr.Palette);
                chr.Palette = pal;

                // Support for superimposing diacritical marks
                if (page.modeMapL2[y, x].Diacritical != 0 && chr != null && _flashTextOn)
                {
                    Bitmap tmp = new Bitmap(chr, chr.Width, chr.Height);
                    Graphics g = Graphics.FromImage(tmp);
                    g.CompositingMode = CompositingMode.SourceOver;
                    if (debug)
                        System.Diagnostics.Debug.WriteLine("charG2S4" + Convert.ToString(page.modeMapL2[y, x].Diacritical, 16));
                    Bitmap dia = (Bitmap)font["G2S4" + Convert.ToString(page.modeMapL2[y, x].Diacritical, 16)];

                    //ColorPalette pal = GetColour(page, x, y, chr.Palette);
                    //chr.Palette = pal;
                    chr = SuperimposeCharacter(chr, dia);
                    chr.Palette = pal;
                }

                //chr.Save("Justcreated.png");

                // Support for underline
                if (page.modeMapL2[y, x].Underlined && _flashTextOn)
                {
                    //ColorPalette pal = GetColour(page, x, y, chr.Palette); 
                    //chr.Palette = pal;
                    chr = ConvertToUnderlined(chr);
                    chr.Palette = pal;
                }

                // Support for double size/width/height
                if (page.modeMapL2[y, x].DoubleWidth || page.modeMapL2[y, x].DoubleHeight || page.modeMapL2[y, x].DoubleSize)
                {
                    if (initialDoubleWidthX == -1 && (page.modeMapL2[y, x].DoubleWidth || page.modeMapL2[y, x].DoubleSize))
                    {
                        initialDoubleWidthX = x;
                        //initialDoubleHeightY = y;
                    }

                    if ((x - initialDoubleWidthX) % 2 == 0 && initialDoubleWidthX != -1)
                    {
                        //ColorPalette pal = GetColour(page, x, y, chr.Palette);
                        //chr.Palette = pal;

                        chr = ConvertToDoubleWidth(chr, _deviceDPI);
                        chr.Palette = pal;

                        if (page.modeMapL2[y, x].DoubleHeight)
                        {
                            chr = ConvertToDoubleHeight(chr, _deviceDPI);
                            chr.Palette = pal;
                        }

                    }

                    // If only in double height mode do the double height
                    if (initialDoubleWidthX == -1)
                    {
                        //ColorPalette pal = GetColour(page, x, y, chr.Palette);
                        //chr.Palette = pal;

                        chr = ConvertToDoubleHeight(chr, _deviceDPI);
                        chr.Palette = pal;
                    }

                    // If we are in double width mode miss every other character in the row out
                    if ((x - initialDoubleWidthX) % 2 != 0 && initialDoubleWidthX != -1)
                        chr = null;
                }
                else
                {
                    initialDoubleWidthX = -1;
                    //initialDoubleHeightY = -1;
                }




                if (chr != null)
                {
                    if (debug)
                        System.Diagnostics.Debug.Print("(C00000) Char code: {2}, Selected char: " + strCharToDraw + "(filename char" + glyphPrefix + strCharToDraw + ".bmp) @ ({0}, {1})", x, y, page.modeMap[y, x].Character.toHex(2));
                    layers = DrawChar(ref layers, ref page, chr, x, y, "", debug);
                }
            }
            else
            {
                if (debug)
                    System.Diagnostics.Debug.Print("Not printed - rendering to row 0 is not allowed.");
            }
        }

        private static string GetColourString(Int32 clutEntry)
        {
            String r = "";
            switch (Convert.ToByte(clutEntry).toHex(2))
            {
                case "00":
                    r = "Black";
                    break;
                case "01":
                    r = "Red";
                    break;
                case "02":
                    r = "Green";
                    break;
                case "03":
                    r = "Yellow";
                    break;
                case "04":
                    r = "Blue";
                    break;
                case "05":
                    r = "Magenta";
                    break;
                case "06":
                    r = "Cyan";
                    break;
                case "07":
                    r = "White";
                    break;
            }
            return r;
        }

        private Color ColourLookup(Byte CLUT, Int32 clutEntry, String fgBg, Boolean FullRowColour = false)
        {
            Color r = Color.Black;

            int alpha = 255;

            //Colour table remapping as per X/28 Format 1
            switch (colourTableRemapping)
            {
                case "001":
                    if (fgBg == "FG")
                        CLUT = 0;
                    if (fgBg == "BG" && !FullRowColour)
                        CLUT = 1;
                    break;
                case "010":
                    if (fgBg == "FG")
                        CLUT = 0;
                    if (fgBg == "BG" && !FullRowColour)
                        CLUT = 2;
                    break;
                case "011":
                    if ((fgBg == "BG" && !FullRowColour) || fgBg == "FG")
                        CLUT = 1;
                    break;
                case "100":
                    if (fgBg == "FG")
                        CLUT = 1;
                    if (fgBg == "BG" && !FullRowColour)
                        CLUT = 2;
                    break;
                case "101":
                    if (fgBg == "FG")
                        CLUT = 2;
                    if (fgBg == "BG" && !FullRowColour)
                        CLUT = 1;
                    break;
                case "110":
                    if ((fgBg == "BG" && !FullRowColour) || fgBg == "FG")
                        CLUT = 2;
                    break;
                case "111":
                    if (fgBg == "FG")
                        CLUT = 2;
                    if (fgBg == "BG" && !FullRowColour)
                        CLUT = 3;
                    break;
            }

            Byte red = Convert.ToByte(Convert.ToString(ColourMap[CLUT, clutEntry], 16).PadLeft(3, Convert.ToChar("0")).Substring(0, 1), 16);
            Byte green = Convert.ToByte(Convert.ToString(ColourMap[CLUT, clutEntry], 16).PadLeft(3, Convert.ToChar("0")).Substring(1, 1), 16);
            Byte blue = Convert.ToByte(Convert.ToString(ColourMap[CLUT, clutEntry], 16).PadLeft(3, Convert.ToChar("0")).Substring(2, 1), 16);

            String strRed = Convert.ToString(ColourMap[CLUT, clutEntry], 16).PadLeft(3, Convert.ToChar("0")).Substring(0, 1);
            String strGreen = Convert.ToString(ColourMap[CLUT, clutEntry], 16).PadLeft(3, Convert.ToChar("0")).Substring(1, 1);
            String strBlue = Convert.ToString(ColourMap[CLUT, clutEntry], 16).PadLeft(3, Convert.ToChar("0")).Substring(2, 1);

            /*String strRed = Convert.ToString(ColourMap[CLUT, clutEntry], 16).Substring(0, 1).PadLeft(2, Convert.ToChar(Convert.ToString(ColourMap[CLUT, clutEntry], 16).Substring(0, 1)));
            String strGreen = Convert.ToString(ColourMap[CLUT, clutEntry], 16).Substring(0, 1).PadLeft(2, Convert.ToChar(Convert.ToString(ColourMap[CLUT, clutEntry], 16).Substring(0, 1)));
            String strBlue = Convert.ToString(ColourMap[CLUT, clutEntry], 16).Substring(0, 1).PadLeft(2, Convert.ToChar(Convert.ToString(ColourMap[CLUT, clutEntry], 16).Substring(0, 1)));*/

            if (CLUT == 1 && clutEntry == 0)
                alpha = 0;

            red = Convert.ToByte(strRed + strRed, 16);
            green = Convert.ToByte(strGreen + strGreen, 16);
            blue = Convert.ToByte(strBlue + strBlue, 16);

            r = Color.FromArgb(alpha, red, green, blue);

            return r;
        }

        private void DecodeLevelFour(Page page, RenderedLayersNova layers)
        {
            // Is this a Level 4 page?
            Boolean pageIsLevel4 = false;
            Boolean doneChecking = false;
            foreach (Line workingLine in page.Lines)
            {
                if (workingLine.Row == 1 && (workingLine.Bytes[2] & Convert.ToByte(0x7f)) == 0x71 && (workingLine.Bytes[3] & Convert.ToByte(0x7f)) == 0x5A && !doneChecking)
                {

                    //String bum = Convert.ToString(workingLine.Bytes[5], 2).PadLeft(8, Convert.ToChar("0"));
                    //bum = bum.Substring(4, 4);
                    Int32 pageNumber = Convert.ToInt32(Convert.ToString(workingLine.Bytes[4], 2).PadLeft(8, Convert.ToChar("0")).Substring(1, 3), 2);
                    Int32 totalNumberOfPages = Convert.ToInt32(Convert.ToString(workingLine.Bytes[4], 2).PadLeft(8, Convert.ToChar("0")).Substring(4, 4), 2);




                    doneChecking = true;
                    pageIsLevel4 = true;

                }
            }

            if (pageIsLevel4)
            {
                // now go through the page packet-by-packet and decode
                for (Int32 pkt = 1; pkt < 25; pkt++)
                {
                    Line workingLine = new Line();

                    // Find next line
                    foreach (Line loopLine in page.Lines)
                    {
                        if (loopLine.Row == pkt)
                            workingLine = loopLine;
                    }

                    //Now parse the line
                    if (workingLine.Bytes != null)
                    {

                        // Set the starting byte in the packet
                        Int32 currentBytePosn = 2;

                        // If the first row, bypass the L4 identifier qZ
                        if (workingLine.Row == 1)
                            currentBytePosn = 4;

                        // Convert the whole line to a bit string; leave out the CRC at the last byte
                        String lineBin = "";
                        string lineBinRev = "";
                        String lineBinWithParity = "";
                        String lineBinWithParityRev = "";
                        for (int n = currentBytePosn; n < workingLine.Bytes.Length - 1; n++)
                        {
                            // Get byte, MSB first, strip the parity bit
                            String bin = Convert.ToString(Convert.ToString(workingLine.Bytes[n], 2).PadLeft(8, Convert.ToChar("0"))).Substring(1, 7);
                            lineBin += bin;
                            lineBinRev += bin.Reverse();
                            String binParity = Convert.ToString(Convert.ToString(workingLine.Bytes[n], 2).PadLeft(8, Convert.ToChar("0")));
                            lineBinWithParity += binParity;
                            lineBinWithParityRev += binParity.Reverse();

                        }

                        System.Diagnostics.Debug.Print("\nLine " + workingLine.Row);

                        do
                        {
                            // This byte should be the HRG word lead-in, 101010
                            String leadIn = Convert.ToString(workingLine.Bytes[currentBytePosn], 2).PadLeft(8, Convert.ToChar("0")).Substring(1, 5);
                            System.Diagnostics.Debug.WriteLine("Lead-in (expecting 101010): " + leadIn);

                            // Init the command array and stuff
                            String strCommandCode = Convert.ToString(workingLine.Bytes[currentBytePosn], 2).PadLeft(8, Convert.ToChar("0"));
                            Byte commandCode = Convert.ToByte(strCommandCode.Substring(1, 5), 2);
                            String commandName = "";
                            Int32 commandLength = 0;

                            switch (Convert.ToInt32(commandCode))
                            {
                                case 0:
                                    {
                                        commandName = "0: Line / Line";
                                        //commandLength = 
                                        break;
                                    }
                                case 1:
                                    {
                                        commandName = "1: Polygon / Circle";
                                        //commandLength = 
                                        break;
                                    }
                                case 2:
                                    {
                                        commandName = "2: Circle / Boundary";
                                        //commandLength = 
                                        break;
                                    }
                                case 3:
                                    {
                                        commandName = "3: Major Arc / Infill";
                                        //commandLength = 
                                        break;
                                    }
                                case 4:
                                    {
                                        commandName = "4: Minor Arc / Polygon";
                                        //commandLength = 
                                        break;
                                    }
                                case 5:
                                    {
                                        commandName = "5: Chain / Erase";
                                        //commandLength = 
                                        break;
                                    }
                                case 6:
                                    {
                                        commandName = "6: Boundary Infill / Delay";
                                        //commandLength = 
                                        break;
                                    }
                                case 7:
                                    {
                                        commandName = "7: Infill / Write";
                                        //commandLength = 
                                        break;
                                    }
                                case 8:
                                    {
                                        commandName = "8: Write / Double Height Write";
                                        //commandLength = 
                                        break;
                                    }
                                case 9:
                                    {
                                        commandName = "9: Erase / Chain";
                                        //commandLength = 
                                        break;
                                    }
                                case 0xa:
                                    {
                                        commandName = "a: Delay / Character Definition";
                                        //commandLength = 
                                        break;
                                    }
                                case 0xb:
                                    {
                                        commandName = "b: Flash On / Double Height and Width Write";
                                        //commandLength = 
                                        break;
                                    }
                                default:
                                    {
                                        commandName = "No command found.";
                                        break;
                                    }

                            }



                            System.Diagnostics.Debug.WriteLine("Command " + commandCode + " : " + commandName);



                            //process command
                            currentBytePosn++;

                        } while (currentBytePosn < workingLine.Bytes.Length);


                        /*
                        // Make a bloody great big string of the binary of the current working line
                        String packetBinary = "";
                        for (Byte b = 2; b < 41; b++)
                        {
                            packetBinary += Convert.ToString(workingLine.Bytes[b], 2).PadLeft(8, Convert.ToChar("0")).Substring(1, 7);
                        }

                        Boolean leadInFound = false;
                        Int32 leadInNext = 0;
                        Int32 leadInStart = packetBinary.IndexOf("10101");
                        do
                        {
                            //find next lead in
                            leadInStart = leadInNext;
                            leadInNext = packetBinary.IndexOf("10101", leadInStart + 5);

                            String hrgInstruction = packetBinary.Substring(leadInStart, leadInStart + leadInNext).Substring(5);

                            String hrgCommandCode = Convert.ToString(Convert.ToByte(hrgInstruction.Substring(0, 5), 2), 16);

                            String hrgCommandName = "";

                            switch (hrgCommandCode)
                            {
                                case "1":
                                    {
                                        hrgCommandName = "Circle";
                                        Int32 hrgColour = Convert.ToInt32(hrgInstruction.Substring(5, 4), 2);
                                        Int32 x = Convert.ToInt32(hrgInstruction.Substring(9, 11), 2);
                                        Int32 y = Convert.ToInt32(hrgInstruction.Substring(20, 10), 2);
                                        Int32 radius = Convert.ToInt32(hrgInstruction.Substring(30, 11));
                                        break;
                                    }
                                case "13":
                                    {
                                        hrgCommandName = "Display Neat Plane";
                                        //commandLength = 
                                        break;
                                    }
                                default:
                                    {
                                        hrgCommandName = "No command found.";
                                        break;
                                    }

                            }

                            System.Diagnostics.Debug.WriteLine("Command Code: {0}; Name: {1}", hrgCommandCode, hrgCommandName);

                            if (leadInNext == 0)
                                leadInFound = false;
                            else
                                leadInFound = true;



                        } while (leadInFound);
                        */

                        //
                    }
                }
            }
        }

        private String Translate(String txt)
        {
            String result = "";
            String noControlChars = "";

            //Get any text out so that there are no control characters

            // Loop text, looking for alphanumeric text
            Boolean blnGraphicsMode = false;
            for (Int32 n = 0; n < txt.Length; n++)
            {
                Byte b = Convert.ToByte(Convert.ToChar(txt.Substring(n, 1)));
                if ((b >= 0x10 && b <= 0x17) || b == 0x19 || b == 0x1a || b == 0x1e || b == 0x1f)
                    blnGraphicsMode = true;
                else
                    if (b >= 0x00 && b <= 0x07)
                        blnGraphicsMode = false;

                if (!blnGraphicsMode && b >= 0x20 && b <= 0x7a)
                    noControlChars += txt.Substring(n, 1);
                else
                    noControlChars += " ";
            }

            // Send clean string to translation service

            result = TranslateGoogle(noControlChars, "de-de", "en-gb");

            return result;
        }

        public string TranslateGoogle(string text, string fromCulture, string toCulture)
        {
            fromCulture = fromCulture.ToLower();
            toCulture = toCulture.ToLower();

            String error = "";

            // normalize the culture in case something like en-us was passed 
            // retrieve only en since Google doesn't support sub-locales
            string[] tokens = fromCulture.Split('-');
            if (tokens.Length > 1)
                fromCulture = tokens[0];

            // normalize ToCulture
            tokens = toCulture.Split('-');
            if (tokens.Length > 1)
                toCulture = tokens[0];

            string url = string.Format(@"http://translate.google.com/translate_a/t?client=j&text={0}&hl=en&sl={1}&tl={2}",
                                       text, fromCulture, toCulture);

            // Retrieve Translation with HTTP GET call
            string html = null;
            try
            {
                WebClient web = new WebClient();

                // MUST add a known browser user agent or else response encoding doen't return UTF-8 (WTF Google?)
                web.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0");
                web.Headers.Add(HttpRequestHeader.AcceptCharset, "UTF-8");

                // Make sure we have response encoding to UTF-8
                web.Encoding = Encoding.UTF8;
                html = web.DownloadString(url);
            }
            catch (Exception ex)
            {
                error = ex.GetBaseException().Message;
                return null;
            }

            // Extract out trans":"...[Extracted]...","from the JSON string
            //string result = Regex.Match(html, "trans\":(\".*?\"),\"", RegexOptions.IgnoreCase).Groups[1].Value;
            String result = html.Substring(1, html.Length - 2);

            //return WebUtils.DecodeJsString(result);

            // Result is a JavaScript string so we need to deserialize it properly

            return result;
        }
    }


}
