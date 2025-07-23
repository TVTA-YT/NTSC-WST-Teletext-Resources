using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace TeletextSharedResources
{
    public class Service
    {
        private BinaryReader reader = null;
        private byte b;
        private Int32 offset = 0;
        public Int32 Revolutions = 0;

        private Byte[] byteReader;

        private long localPos = 0;
        public long Position
        {
            get
            {
                return localPos;
            }
            set
            {
                localPos = value;

                if (localPos < 0)
                    localPos = 0;

                if (reader != null)
                {
                    if (reader.BaseStream != null)
                    {
                        reader.BaseStream.Seek(0, SeekOrigin.Begin);
                        this.Reset();
                        reader.ReadBytes((Int32)value);
                    }
                }
            }
        }

        private Byte packetSize = 42;
        private Int32 frameSize = 860;

        public Byte PacketSize
        {
            get
            {
                return packetSize;
            }
            set
            {
                packetSize = value;
                frameSize = packetSize * 32;
            }
        }

        public Int32 RowLength = 42;

        public Int64 Length = 0;
        public Int32 FrameNo = 0;
        public byte FrameLine = 21;
        //public Frame currentFrame = new Frame();

        private long intCheckHorizon = 5242880;
        public long CheckHorizon
        {
            get
            {
                return intCheckHorizon;
            }

            set
            {
                intCheckHorizon = value;
            }
        }

        private Int32 _linesPerFrame;
        public Int32 LinesPerFrame
        {
            get
            {
                return _linesPerFrame;
            }
        }

        private Boolean _useHamming = true;
        public Boolean UseHamming
        {
            get
            {
                return _useHamming;
            }
            set
            {
                _useHamming = value;
            }
        }

        private Boolean _loaded = false;
        public Boolean Loaded
        {
            get
            {
                return _loaded;
            }
        }

        private String _loadError = "";
        public String LoadError
        {
            get
            {
                return _loadError;
            }
        }

        private Int32 _pcLoaded = 0;
        public Int32 PercentLoaded
        {
            get
            {
                return _pcLoaded;
            }
        }

        private String _fileType;
        public String FileType
        {
            get
            {
                return _fileType;
            }
        }

        private ServiceType _serviceType;
        public ServiceType ServiceType  //Either T42, Remote or Carousel
        {
            get
            {
                return _serviceType;
            }
            set { _serviceType = value; }
        }

        public String Status;

        private string _filename = "";
        public string Filename
        {
            get
            {
                return _filename;
            }
        }

        private List<CycleParameters> lService = new List<CycleParameters>();
        MagazineCycleList[] mclMagCycleList = new MagazineCycleList[8];

        private String _urlWeightingMatrix = "1231561701231561704";
        public String WeightingMatrix
        {
            get
            {
                return _urlWeightingMatrix;
            }
            set
            {
                _urlWeightingMatrix = value;
            }
        }
        Int32 lastWeightingMatrixPosn = 0;

        private String _headerTemplate = "12345678TEEFAX mpp  DaY !d MtH \u0003hH:mM/sS";
        public String HeaderTemplate
        {
            get
            {
                return _headerTemplate;
            }
            set
            {
                _headerTemplate = value;
            }
        }

        private Boolean _overrideHeaders = true;
        public Boolean OverrideHeaders
        {
            get
            {
                return _overrideHeaders;
            }
            set
            {
                _overrideHeaders = value;
            }
        }

        private Boolean _doParityCheck = false;
        public Boolean DoParityCheck
        {
            get
            {
                return _doParityCheck;
            }
            set
            {
                _doParityCheck = value;
            }

        }


        public string OpenService(string filename, string OverrideFileType = "")
        {
            _filename = filename;   
            if (OverrideFileType == "")
                _fileType = filename.Substring(filename.LastIndexOf(".") + 1, filename.Length - filename.LastIndexOf(".") - 1).ToUpper();
            else
            {
                _fileType = OverrideFileType;
            }

            string returnvalue = "";

            try
            {
                reader = new BinaryReader(File.Open(filename, FileMode.Open));
                this.Length = reader.BaseStream.Length;
                if (this.Length == 0)
                    throw new Exception("File has zero length");
            }
            catch (Exception e)
            {
                returnvalue = "Error opening file" + filename  + " : " + e.Message;
                if (reader != null)
                    reader.Close();
            };
            

            if (Length > 0 && returnvalue == "")
            {

                /*if (Convert.ToDouble(this.Length) / 20 != this.Length)
                {
                    this.Length = ((this.Length / frameSize) + 1) * frameSize;
                }*/

                byteReader = new Byte[Length];
                byte[] byteReaderTemp = new Byte[1];

                try
                {
                    byteReaderTemp = reader.ReadBytes(Convert.ToInt32(reader.BaseStream.Length));
                }
                catch (Exception e)
                {
                    returnvalue = "Error opening file: " + e.Message;
                    if (reader != null)
                        reader.Close();
                }
                Array.Copy(byteReaderTemp, 0, byteReader, 0, byteReaderTemp.LongLength);



                //_linesPerFrame = NumLinesPerFrame();

                //Revolutions = 0;

                reader.Close();
                reader.Dispose();
                reader = null;
                //_serviceType = "Service";

                // List the carousels to find out if it's a carousel or service we've just opened
                if (this.ServiceType == ServiceType.Unknown)
                    this.ServiceType = this.DetermineServiceType();

                Reset();

                }
            else
            {
                if (returnvalue != "")
                {
                    reader.Close();
                    reader.Dispose();
                    reader = null;
                    _serviceType = ServiceType.Unknown;
                    return returnvalue;
                }
            }

            // Filetype definitions

            if (_fileType.EndsWith("43BP"))
            {
                this.RowLength = 43;
                packetSize = 43;
                _serviceType = ServiceType.Carousel;
            }

            if (_fileType.EndsWith("DAT"))
            {
                this.RowLength = 43;
                packetSize = 43;
                _serviceType = ServiceType.Service;
            }

            if (_fileType.EndsWith("T43"))
            {
                this.RowLength = 43;
                packetSize = 43;
                _serviceType = ServiceType.Service;
            }

            if (_fileType.EndsWith("T42"))
            {
                this.RowLength = 42;
                packetSize = 42;
                _serviceType = ServiceType.Service;
            }

            if (_fileType.EndsWith("BIN"))
            {
                this.RowLength = 42;
                _serviceType = ServiceType.Carousel;
            }

            if (_fileType.EndsWith("TTX"))
            {
                this.RowLength = 42;
                _serviceType = ServiceType.Carousel;
            }

            if (_fileType.EndsWith("EP1"))
            {
                this.RowLength = 40;
                _serviceType = ServiceType.Carousel;
            }

            if (_fileType.EndsWith("TTI"))
            {
                this.RowLength = 40;
                _serviceType = ServiceType.Carousel;
            }

            if (_fileType.EndsWith("TTIX"))
            {
                this.RowLength = 40;
                _serviceType = ServiceType.Carousel;
            }

            if (_fileType.EndsWith("PRG"))
            {
                this.RowLength = 40;
                _serviceType = ServiceType.Carousel;
            }

            if (_fileType.EndsWith("VTP"))
            {
                this.RowLength = 40;
                _serviceType = ServiceType.Carousel;
            }
            if (_fileType.EndsWith("VTX"))
            {
                this.RowLength = 40;
                packetSize = 40;
                _serviceType = ServiceType.Service;
            }


            if (packetSize != 42 && returnvalue == "")
            {
                // Convert to 42 byte packet

                // Byte array to copy into
                Byte[] byteReaderTemp = new Byte[Length / 40 * 42];

                //MRAGless, so add one to each line 
                Byte row;
                if (FileType == "TTX")
                {
                    for (Int64 line = 0; line < this.Length / 40; line++)
                    {
                        Buffer.BlockCopy(byteReader, (Int32)(line * 40), byteReaderTemp, (Int32)(line * 42) + 2, 40);
                        String text = System.Text.Encoding.ASCII.GetString(byteReader, (Int32)line * 40, 40);

                        row = (Byte)(line % 24);
                        Line tempLine = CreatePacket(1, "00", "00:00", row, row, text);
                        byteReaderTemp[line * 42] = tempLine.MRAG1;
                        byteReaderTemp[(line * 42) + 1] = tempLine.MRAG2;
                    }

                }

                if (FileType == "DAT" || FileType == "T43")
                {
                    Int64 startOffset = 0;
                    for (Int64 line = 0; line < (this.Length - startOffset) / packetSize; line++)
                    {
                        Buffer.BlockCopy(byteReader, (Int32)(line * packetSize) + (Int32)startOffset, byteReaderTemp, (Int32)(line * 42), 42);
                        //String text = System.Text.Encoding.ASCII.GetString(byteReader, (Int32)line * packetSize, 42);

                        //row = (Byte)(line % 24);
                        //Line tempLine = CreatePacket(1, "00", "00:00", row, row, text);
                        //byteReaderTemp[line * 42] = tempLine.MRAG1;
                        //byteReaderTemp[(line * 42) + 1] = tempLine.MRAG2;
                    }
                    byteReader = byteReaderTemp;
                    
                }

                if (FileType == "PRG")
                {
                    for (Int64 line = 0; line < 24; line++)
                    {
                        Buffer.BlockCopy(byteReader, (Int32)(line * 40), byteReaderTemp, (Int32)(line * 42), 40);
                        String text = System.Text.Encoding.ASCII.GetString(byteReader, (Int32)line * 40, 40);

                        row = (Byte)(line);
                        Line tempLine = CreatePacket(1, "00", "00:00", row, row, text);
                        byteReaderTemp[line * 42] = tempLine.MRAG1;
                        byteReaderTemp[(line * 42) + 1] = tempLine.MRAG2;
                    }
                }

                if (FileType == "VTP")
                {
                    Int32 headerLength = 0x76;
                    Int32 counter = headerLength;
                    Int32 line = 0;

                    do
                    {
                        // Read 24x40 Lines
                        for (row = 0; row < 24; row++)
                        {
                            if (counter < this.Length - 42)
                            {
                                Buffer.BlockCopy(byteReader, counter, byteReaderTemp, (line * 42) + 2, 40);
                                String text = System.Text.Encoding.ASCII.GetString(byteReaderTemp, (Int32)line * 40, 40);

                                Line tempLine = CreatePacket(1, "00", "00:00", row, row, text);
                                byteReaderTemp[line * 42] = tempLine.MRAG1;
                                byteReaderTemp[(line * 42) + 1] = tempLine.MRAG2;

                                counter += 40;
                                line++;
                            }
                        }
                        counter += 10;
                    } while (counter < this.Length);

                }

                if (FileType == "EP1")
                {
                    for (Int64 line = 0; line < (this.Length - 6) / 40; line++)
                    {
                        Buffer.BlockCopy(byteReader, (Int32)(line * 40) + 6, byteReaderTemp, (Int32)(line * 42) + 2, 40);
                        String text = System.Text.Encoding.ASCII.GetString(byteReader, (Int32)line * 40, 40);

                        row = (Byte)(line % 24);
                        Line tempLine = CreatePacket(1, "00", "00:00", row, row, text);
                        byteReaderTemp[line * 42] = tempLine.MRAG1;
                        byteReaderTemp[(line * 42) + 1] = tempLine.MRAG2;
                    }
                }

                if (FileType == "VTX")
                {
                    // Service file
                    // 40 byte service VTX header
                    // 7 byte first page VTX header
                    // then 23 rows of 40 characters
                    //subsequently each subpage:
                    // 10 byte page VTX header, an 8 byte VTX header, then a 32 byte teletext header row,
                    // then 23 rows of 40 characters
                    //
                    // mew page has a 18 byte VTX header 
                    // then 23 rows of 40 characters

                    // 18 byte new page header: zero based, byte 2 and 10 are the page; 3 and 11 are the magazine 

                    long currentIndex = 0x10;
                    int totalLineNo = 0;

                    do
	                {
                        // Get num of subpages in page
                        int indexNumSubLo = byteReader[currentIndex + 0x04];
                        int indexNumSubHi = byteReader[currentIndex + 0x05];
                        int numSubs = (indexNumSubHi * 0x40) + (indexNumSubLo);

                        // Get page and magazine
                        string page = Convert.ToString(byteReader[currentIndex + 0x02], 16).PadLeft(2, Convert.ToChar("0"));
                        byte magazine = (byte)byteReader[currentIndex + 0x03];

                        // Start of the header row text
                        if (currentIndex == 0x10)
                            currentIndex += 0x27;
                        else
                            currentIndex += 0x18;
                    


                        for (int subpage = 0; subpage < numSubs; subpage++)
                        {
                            for (int lineNo = 0; lineNo < 25; lineNo++)
                            {
                                Line tempLine = new Line();
                                if (lineNo == 0)
                                {
                                    String headerText = "        " + System.Text.Encoding.ASCII.GetString(byteReader, (Int32)currentIndex, 32);
                                    tempLine = CreatePacket(magazine, page, Convert.ToString(subpage, 16).PadLeft(4, Convert.ToChar("0")), 0, 0, headerText);
                                    currentIndex += 32;
                                }
                                else
                                {
                                    String lineText = System.Text.Encoding.ASCII.GetString(byteReader, (Int32)currentIndex, 40);
                                    tempLine = CreatePacket(magazine, page, Convert.ToString(subpage, 16).PadLeft(4, Convert.ToChar("0")), (byte)lineNo, (byte)lineNo, lineText);
                                    currentIndex += 40;
                                }


                                tempLine.CalcHammingCodes();

                                // insert line into byte array

                                byteReaderTemp[totalLineNo * 42] = tempLine.MRAG1;
                                byteReaderTemp[(totalLineNo * 42) + 1] = tempLine.MRAG2;

                                for (Int32 i = 0; i < 40; i++)
                                {
                                    if (i < tempLine.Text.Length)
                                        byteReaderTemp[totalLineNo * 42 + i + 2] = Convert.ToByte(Convert.ToChar(tempLine.Text.Substring(i, 1)));

                                    else
                                        byteReaderTemp[totalLineNo * 42 + i + 2] = 0x20;

                                    /*if (i < rowText.Length)
                                    {
                                        if (Convert.ToByte(Convert.ToChar(rowText.Substring(i, 1))) == 0x0d)
                                            System.Diagnostics.Debug.WriteLine("0x0d: " + " " + i);
                                    }*/
                                }

                                if (tempLine.Row == 0)
                                {
                                    byteReaderTemp[(totalLineNo * 42) + 2] = tempLine.PU;
                                    byteReaderTemp[(totalLineNo * 42) + 3] = tempLine.PT;
                                    byteReaderTemp[(totalLineNo * 42) + 4] = tempLine.MU;
                                    byteReaderTemp[(totalLineNo * 42) + 5] = tempLine.MT;
                                    byteReaderTemp[(totalLineNo * 42) + 6] = tempLine.HU;
                                    byteReaderTemp[(totalLineNo * 42) + 7] = tempLine.HT;
                                    byteReaderTemp[(totalLineNo * 42) + 8] = tempLine.CA;
                                    byteReaderTemp[(totalLineNo * 42) + 9] = tempLine.CB;

                                }

                                totalLineNo++;

                            }
                            if (subpage + 1 < numSubs)
                                // More subpages left, so skip forward to the nexr subpage
                                currentIndex += 18;
                            else
                                // Next page so skip forward
                                currentIndex -= 0;
                        }
                        BinaryWriter writer2 = new BinaryWriter(new FileStream(Environment.GetEnvironmentVariable("temp") + "\\conv.t42", FileMode.Create));
                        writer2.Write(byteReaderTemp);
                        writer2.Close();
                        writer2 = null;

                    } while (currentIndex < byteReader.Length);
                    //} while (currentIndex < 10000);
                    byteReader = byteReaderTemp;

                }

                if (FileType == "TTI" || FileType == "TTIX")
                {

                    // Assumptions:
                    // File is in Windows format (i.e. CRLF line terminator)

                    // If any codes are < 0x80, they are control codes - recode them as control codes <0x20
                    Int32 n = 0;
                    foreach (Byte b in byteReader)
                    {
                        if (b > 0x7f && b != 0x8A && b != 0x8D)
                            byteReader[n] = (Byte)((Int32)b & 0x7f);
                        n++;
                    }

                    Char[] chrBytereader = new Char[byteReader.Length];
                    byteReader.CopyTo(chrBytereader, 0);
                    String ttiFile = new String(chrBytereader);
                    Int32 numPackets = 0;

                    //Find out how many packets are in the file so that we can make enough room in the byte array
                    //Int32 ttiFileOriginalLength = ttiFile.Length;

                    /*// Count the subpages in the TTI file
                    ttiFile = ttiFile.Replace("PN,", "1234");
                    numPackets = ttiFile.Length - ttiFileOriginalLength;

                    // Disregard the OL,0 header lines as the amount of pages has already been counted
                    ttiFile = ttiFile.Replace("OL,0", "12345");
                    //numPackets = ttiFile.Length - ttiFileOriginalLength;
                    //Int32 numOL0 = numPackets;
                    ttiFileOriginalLength = ttiFile.Length;

                    // Count other packets
                    ttiFile = ttiFile.Replace("OL,", "1234");
                    numPackets += ttiFile.Length - ttiFileOriginalLength;

                    // See if there is a Fastext packet
                    ttiFileOriginalLength = ttiFile.Length;
                    ttiFile = ttiFile.Replace("FL,", "1234");
                    numPackets += ttiFile.Length - ttiFileOriginalLength;*/

                    //ttiFile = ttiFile.Replace("OL,", "1234");

                    //ttiFile = ttiFile.Replace("FL,", "1234");
                    //numPackets += ttiFile.Length - ttiFileOriginalLength;




                    // Reset ttiFile
                    ttiFile = new String(chrBytereader);

                    ttiFile.Replace(Convert.ToString((Char)13), "");

                    do
                    {
                        Int32 x = ttiFile.LastIndexOf((Char)0x1B);
                        if (x != -1 && x < ttiFile.Length)
                        {
                            String ttiFileTemp = ttiFile.Substring(0, x);
                            Byte bytTemp = Convert.ToByte(Convert.ToChar(ttiFile.Substring(x + 1, 1)));
                            bytTemp -= 0x40;
                            ttiFileTemp += Convert.ToChar(bytTemp).ToString();
                            String strTemp = ttiFile.Substring(x + 2);
                            if ((x + 2) < ttiFile.Length)
                                ttiFileTemp += ttiFile.Substring(x + 2);
                            ttiFile = ttiFileTemp;
                        }


                    } while (ttiFile.LastIndexOf((Char)0x1B) != -1);

                    String[] ttiLines = ttiFile.Split(Convert.ToChar(10));

                    // Work out how many packets there are in total
                    // How many OLs, PNs and FLs?
                    Int32 intOL = 0;
                    Int32 intPN = 0;
                    Int32 intFL = 0;
                    foreach (String lineType in ttiLines)
                    {
                        if (lineType.Length > 2)
                        {
                            lineType.Replace(" ", "");
                            switch (lineType.Substring(0, 2))
                            {
                                case "OL":
                                    intOL++;
                                    break;
                                case "PN":
                                    intPN++;
                                    break;
                                case "FL":
                                    intFL++;
                                    break;
                                default:
                                    break;


                            }                           
                        }
                    }

                    numPackets = intOL + intFL + intPN;
                    byteReaderTemp = new Byte[numPackets * 42];

                    n = 0;
                    Byte magazine = 1;
                    String page = "00";
                    String subpage = "00";


                    Boolean flagErase = false;
                    Boolean flagNewsflash = false;
                    Boolean flagSubtitle = false;
                    Boolean flagSuppressHeader = false;
                    Boolean flagUpdate = false;
                    Boolean flagInterruptedSequence = false;
                    Boolean flagInhibitDisplay = false;
                    Boolean flagMagazineSerial = false;



                    Int32 intLineNumber = 0;
                    bool headerFound =false;

                    foreach (String ss in ttiLines)
                    {
                        String s = ss;
                        //System.Diagnostics.Debug.WriteLine(s);
                        intLineNumber++;
                        if (s.Length > 2)
                        {

                            // Get rid of the LF code 0x0d if present at EOL
                            if (Convert.ToChar(s.Substring(s.Length - 1)) == (Char)0x0d)
                                s = s.Substring(0, s.Length - 1);
                            // Get rid of the CR code 0x0b if present at EOL
                            if (Convert.ToChar(s.Substring(s.Length - 1)) == (Char)0x0b)
                                s = s.Substring(0, s.Length - 1);
                      
                            

                            // Replace the shifted control codes back from text-printable to control code values
                            s = s.Replace((Char)0x8A, (Char)0x0A);
                            s = s.Replace((Char)0x8D, (Char)0x0D);



                            String ttiLineType = s.Substring(0, 2);

                            String headerBits = "1100000000000000";

                            Line tempLine = new Line();

                            switch (ttiLineType)
                            {
                                case "PN":
                                    String value = s.Substring(s.IndexOf(",") + 1);
                                    magazine = Convert.ToByte(value.Substring(0, 1));
                                    page = value.Substring(1, 2);
                                    if (subpage == "00")
                                        subpage = "00:" + value.Substring(3, 2);

                                    // Horrific code needed to support TTI file format crapness, where OL,0 doesn't have to specified for a new page/subpage
                                    // if we get a PN, check to see if there is an OL,0 before the next PN or end of file.  If not we need to add one.
                                    Boolean quitFor = false;
                                    Boolean headerNeeded = true;
                                    
                                    for (Int32 l = intLineNumber; l < ttiLines.Length && !quitFor; l++)
                                    {
                                        //System.Diagnostics.Debug.WriteLine(ttiLines[l].Substring(0, 4));
                                        if (ttiLines[l].ToUpper().StartsWith("PN"))
                                        {
                                                quitFor = true;
                                        }
                                        if (ttiLines[l].Length > 2)
                                            if (ttiLines[l].Substring(0, 4) == "OL,0")
                                                headerNeeded = false;
                                    }

                                    if (headerNeeded)
                                    {
                                        tempLine = CreatePacket(magazine, page, "00:" + value.Substring(3, 2), 0, 0, _headerTemplate.Replace("mpp", (magazine.ToString() == "0" ? "8" + page : magazine.ToString()) + page));
                                    }

                                    break;
                                case "OL":
                                    Int32 firstCommaPos = s.IndexOf(",", 0);
                                    Int32 secondCommaPos = s.IndexOf(",", firstCommaPos + 1);

                                    if (s.Length < 7)
                                        s = s.PadRight(40, Convert.ToChar(" "));

                                    if (firstCommaPos != -1 && secondCommaPos != -1)
                                    {
                                        String rowValue = s.Substring(firstCommaPos + 1, secondCommaPos - firstCommaPos - 1);
                                        String rowText = s.Substring(secondCommaPos + 1);

                                        // no Ol,0 line required stuff
                                       /* if (newHeaderExpected && rowValue != "0")
                                        {
                                            // reset the flag so we don't get lots of headers
                                            newHeaderExpected = false;

                                            // insert the last header

                                            // insert line into byte array



                                            for (Int32 i = 0; i <= 40; i++)
                                            {
                                                if (i < rowText.Length - 1)
                                                {
                                                    byteReaderTemp[n * 42 + i + 2] = Convert.ToByte(Convert.ToChar(lastHeader.Text.Substring(i, 1)));
                                                }
                                                else
                                                    byteReaderTemp[n * 42 + i + 2] = 0x20;
                                            }

                                            byteReaderTemp[n * 42] = lastHeader.MRAG1;
                                            byteReaderTemp[(n * 42) + 1] = lastHeader.MRAG2;
                                            byteReaderTemp[(n * 42) + 2] = lastHeader.PU;
                                            byteReaderTemp[(n * 42) + 3] = lastHeader.PT;
                                            byteReaderTemp[(n * 42) + 4] = lastHeader.MU;
                                            byteReaderTemp[(n * 42) + 5] = lastHeader.MT;
                                            byteReaderTemp[(n * 42) + 6] = lastHeader.HU;
                                            byteReaderTemp[(n * 42) + 7] = lastHeader.HT;
                                            byteReaderTemp[(n * 42) + 8] = lastHeader.CA;
                                            byteReaderTemp[(n * 42) + 9] = lastHeader.CB;

                                            n++;

                                        }*/



                                        //if (magazine == 1 && page == "00" && rowValue == "0")
                                        //    System.Diagnostics.Debug.Print("Arse");

                                        // Substitute real-time values in header
                                        if (rowValue == "0")
                                        {
                                            if (_overrideHeaders)
                                                rowText = _headerTemplate;

                                            rowText.Replace("mpp", (magazine.ToString() == "0" ? "8" + page: magazine.ToString()) + page);
                                        }

 
                                        //if (rowValue != "0" || (rowValue == "0" && !headerFound))
                                        tempLine = CreatePacket(magazine, page, subpage, Convert.ToByte(rowValue), Convert.ToByte(rowValue), rowText);

                                        //if (rowValue == "0")
                                        //    headerFound = true;
                                        n++;
                                    }
                                    break;
                                case "PS":

                                    firstCommaPos = s.IndexOf(",", 0);
                                    try
                                    {
                                        headerBits = Convert.ToString(Convert.ToInt32(s.Substring(firstCommaPos + 1), 16), 2);
                                    }
                                    catch
                                    {
                                        headerBits = Convert.ToString(Convert.ToInt32("8000", 16), 2);
                                    }


                                    // This only works if the PS is before the header OL line - is this always the case?
                                    String strFlags = s.Substring(s.IndexOf(",") + 1).Replace("\r", "");

                                    Byte bytFlags1;
                                    Byte bytFlags2;
                                    try
                                    {
                                        bytFlags1 = Convert.ToByte(strFlags.Substring(0, 2), 16);
                                        bytFlags2 = Convert.ToByte(strFlags.Substring(2, 2), 16);
                                    }
                                    catch
                                    {
                                        bytFlags1 = Convert.ToByte("80", 16);
                                        bytFlags2 = Convert.ToByte("00", 16);
                                    }

                                     String binFlags1 = Convert.ToString(bytFlags1, 2).PadLeft(8, Convert.ToChar("0"));
                                    String binFlags2 = Convert.ToString(bytFlags2, 2).PadLeft(8, Convert.ToChar("0"));

                                    Byte ControlGroupA = byteReaderTemp[(n * 42) + 8];
                                    Byte ControlGroupB = byteReaderTemp[(n * 42) + 9];

                                    // Read control flags
                                    if ((bytFlags1 & 64) == 64)
                                        flagErase = true;
                                    else
                                        flagErase = false;

                                    if ((bytFlags2 & 1) == 1)
                                        flagNewsflash = true;
                                    else
                                        flagNewsflash = false;

                                    if ((bytFlags2 & 2) == 2)
                                        flagSubtitle = true;
                                    else
                                        flagSubtitle = false;

                                    if ((bytFlags2 & 4) == 4)
                                        flagSuppressHeader = true;
                                    else
                                        flagSuppressHeader = false;

                                    if ((bytFlags2 & 8) == 8)
                                        flagUpdate = true;
                                    else
                                        flagUpdate = false;

                                    if ((bytFlags2 & 16) == 16)
                                        flagInterruptedSequence = true;
                                    else
                                        flagInterruptedSequence = false;

                                    if ((bytFlags2 & 32) == 32)
                                        flagInhibitDisplay = true;
                                    else
                                        flagInhibitDisplay = false;

                                    if ((bytFlags1 & 4) == 4)
                                        flagMagazineSerial = true;
                                    else
                                        flagMagazineSerial = false;
                                    break;

                                case "FL":
                                    String[] links = s.Split(',');

                                    // Create Fastext data line with Hamming etc.
                                    Line fastextLine = CreatePacket(magazine, 27, links);

                                    // Copy data from new line into main data array
                                    /*BinaryWriter writer1 = new BinaryWriter(new FileStream("c:\\beforefastext.bin", FileMode.Create));
                                    writer1.Write(byteReaderTemp);
                                    writer1.Close();
                                    writer1 = null;*/

                                    Array.Copy(fastextLine.Bytes, 0, byteReaderTemp, (n * 42), 42);
                                    n++;

                                    /*BinaryWriter writer2 = new BinaryWriter(new FileStream("c:\\afterfastext.bin", FileMode.Create));
                                    writer2.Write(byteReaderTemp);
                                    writer2.Close();
                                    writer2 = null;*/

                                    break;

                                case "SC":
                                    String[] subpages = s.Split(',');
                                    subpage = subpages[subpages.Length - 1].PadRight(4, Convert.ToChar("0"));
                                    subpage = subpage.Substring(0,2) + ":" + subpage.Substring(2,2);
                                    tempLine.TimeCode = subpage;
                                    break;

                            }

                            // If tempLine has a value, add it
                            if (tempLine.Bytes != null)
                            {

                                // This came from the TeletextStreamCreator code
                                //START
                                //---------------------------------------------------------
                                switch (tempLine.Row)
                                {
                                    case 26:
                                        Byte[] bytes = Encoding.ASCII.GetBytes(tempLine.Text);
                                        Byte DC = Convert.ToByte(bytes[0] & 0x0F);
                                        for (Int32 t = 0; t <= 12; t++)
                                        {

                                            Int32 triplet;
                                            triplet = bytes[t * 3 + 1] & 0x3f;
                                            triplet |= (bytes[(t * 3) + 2] & 0x3F) << 6;
                                            triplet |= (bytes[(t * 3) + 3] & 0x3F) << 12;

                                            //triplet = bytes[t * 3 + 3] & 0x3F;
                                            //triplet |= ((bytes[t * 3 + 4]) & 0x3F) << 6;
                                            //triplet |= ((bytes[t * 3 + 5]) & 0x3F) << 12;

                                            System.Diagnostics.Debug.WriteLine("Original TTI Text: " + tempLine.Text);
                                            System.Diagnostics.Debug.WriteLine("Triplet content  : " + Convert.ToString(triplet, 2).PadLeft(18, Convert.ToChar("0")));


                                        }
                                        break;

                                    default:
                                        Int32 sourceCharPosn = (tempLine.Type == LineTypes.Header ? 8 : 0);
                                        Int32 destCharPosn = (tempLine.Type == LineTypes.Header ? 10 : 2);
                                        do
                                        {

                                            Byte b = Convert.ToByte(Convert.ToChar(tempLine.Text.Substring(sourceCharPosn, 1)));
                                            if (b > 0x80)
                                            {
                                                b = Convert.ToByte((Int32)b - 0x80);
                                            }

                                            if (b == 27)
                                            {
                                                sourceCharPosn++;
                                                b = Convert.ToByte(Convert.ToChar(tempLine.Text.Substring(sourceCharPosn, 1)));
                                                b = Convert.ToByte((Int32)b - 0x40);
                                            }
                                            // else
                                            byteReaderTemp[n * 42 + destCharPosn] = b;

                                            sourceCharPosn++;
                                            destCharPosn++;



                                        } while (sourceCharPosn < tempLine.Text.Length);

                                        break;
                                }

                                // -------------------------------------------------------------------------
                                // END

                                // Set control flags
                                if (tempLine.Type == LineTypes.Header)
                                {
                                    tempLine.Flags.C4_Erase = flagErase;
                                    flagErase = true;

                                    tempLine.Flags.C5_Newsflash = flagNewsflash;
                                    flagNewsflash = false;

                                    tempLine.Flags.C6_Subtitle = flagSubtitle;
                                    flagSubtitle = false;

                                    tempLine.Flags.C7_SuppressHeader = flagSuppressHeader;
                                    flagSuppressHeader = false;

                                    tempLine.Flags.C8_Update = flagUpdate;
                                    flagUpdate = false;

                                    tempLine.Flags.C9_InterruptedSequence = flagInterruptedSequence;
                                    flagInterruptedSequence = false;

                                    tempLine.Flags.C10_InhibitDisplay = flagInhibitDisplay;
                                    flagInhibitDisplay = false;

                                    tempLine.Flags.C11_MagazineSerial = flagMagazineSerial;
                                    flagMagazineSerial = false;
                                }

                                //File.WriteAllBytes(Environment.GetEnvironmentVariable("temp") + "\\bytereadertemp.t42", byteReaderTemp);

                                tempLine.CalcHammingCodes();

                                // insert line into byte array

                                byteReaderTemp[n * 42] = tempLine.MRAG1;
                                byteReaderTemp[(n * 42) + 1] = tempLine.MRAG2;





                                for (Int32 i = 0; i < 40; i++)
                                {
                                    if (i < tempLine.Text.Length)
                                        byteReaderTemp[n * 42 + i + 2] = Convert.ToByte(Convert.ToChar(tempLine.Text.Substring(i, 1)));

                                    else
                                        byteReaderTemp[n * 42 + i + 2] = 0x20;

                                    /*if (i < rowText.Length)
                                    {
                                        if (Convert.ToByte(Convert.ToChar(rowText.Substring(i, 1))) == 0x0d)
                                            System.Diagnostics.Debug.WriteLine("0x0d: " + " " + i);
                                    }*/
                                }


                                if (tempLine.Row == 0)
                                {
                                    byteReaderTemp[(n * 42) + 2] = tempLine.PU;
                                    byteReaderTemp[(n * 42) + 3] = tempLine.PT;
                                    byteReaderTemp[(n * 42) + 4] = tempLine.MU;
                                    byteReaderTemp[(n * 42) + 5] = tempLine.MT;
                                    byteReaderTemp[(n * 42) + 6] = tempLine.HU;
                                    byteReaderTemp[(n * 42) + 7] = tempLine.HT;
                                    byteReaderTemp[(n * 42) + 8] = tempLine.CA;
                                    byteReaderTemp[(n * 42) + 9] = tempLine.CB;

                                }

                                //n++;

                            }

                        }

                        if (ttiFile.Contains("PN,100"))
                            File.WriteAllBytes(Environment.GetEnvironmentVariable("temp") + "\\100.t42", byteReaderTemp);
                    }
                    // Remove trailing zeroes from array 
                    /*
                    //Int32 lastNonZero = Array.FindLastIndex(byteReaderTemp, b => b != 0) + 1;
                    Int32 lastNonZero = Array.FindLastIndex(byteReaderTemp, NonZero) + 1;
                    Double numT42sInOutFile = lastNonZero / 42.0D;

                    Int32 extraBytesInOutFile = Convert.ToInt32((numT42sInOutFile - Math.Truncate(numT42sInOutFile)) * 42D);
                    lastNonZero += (Int32)extraBytesInOutFile;
                    if (lastNonZero > byteReaderTemp.Length)
                        lastNonZero = byteReaderTemp.Length;

                    Byte[] byteReaderTempTrunc = new Byte[lastNonZero]; 
                    Array.Copy(byteReaderTemp, byteReaderTempTrunc, lastNonZero);

                    if (ttiFile.Contains("PN,100"))
                        File.WriteAllBytes(Environment.GetEnvironmentVariable("temp") + "\\790-pretrunc.t42", byteReaderTemp);

                    byteReader = byteReaderTempTrunc; */


                    byteReader = byteReaderTemp;
                    //byteReaderTemp.CopyTo(byteReader, 0);
                    byteReaderTemp = null;

                    //if (ttiFile.Contains("PN,401") && !System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
                    //   File.WriteAllBytes(Environment.GetEnvironmentVariable("temp") + "\\No_OLs.t42", byteReader);
                }


                //if (ttiFile.Contains("PN,100"))
                   // File.WriteAllBytes(Environment.GetEnvironmentVariable("temp") + "\\100.t42", byteReaderTemp);


                Reset();

                if (byteReader.Length > 32 * 42)
                    _linesPerFrame = NumLinesPerFrame();
                else
                    _linesPerFrame = 32;

                Revolutions = 0;
                RowLength = 42;

                //For debugging

                //BinaryWriter writer = new BinaryWriter(new FileStream(Environment.GetEnvironmentVariable("temp") + "\\conv.bin", FileMode.Create));
                //writer.Write(byteReader);
                //writer.Close();
                //writer = null;

                //byteReaderTemp = null;
                


            }

            return returnvalue;

        }

        public string OpenService(Uri[] url, Boolean offlineMode)
        {
            _filename = null;   
            // this method fills lService, a complete list of cycle parameters.  Cycle parameters tell
            // us when to serve the page, but also contain the pages themselves, including all subpages.

            _loaded = false;
            _pcLoaded = 0;

            lService.Clear();

            String strTempFolder = System.IO.Path.GetTempPath() + "teefax";
            String strIndexPath = System.IO.Path.GetTempPath() + "teefax\\index.html";

            String returnMessage = "OK";

            this.RowLength = 40;
            this.packetSize = 40;
            _fileType = "TTI";

            WebClient wcTeefax = new WebClient();



            if (!offlineMode)
            {
                // delete folder contents
                try
                {
                    System.IO.DirectoryInfo tempdir = new DirectoryInfo(strTempFolder);

                    foreach (FileInfo file in tempdir.GetFiles())
                    {
                        file.Delete();
                    }
                }
                catch 
                {
                    Directory.CreateDirectory(strTempFolder);
                }
                finally
                { }

                for (int u = 0; u < url.Length; u++)
                {
                    // Load index file from remote site
                    try
                    {
                        wcTeefax.DownloadFile(url[u], strIndexPath+u.ToString());
                    }
                    catch (WebException err)
                    {
                        _loadError = "Remote Server Error" + err.Message.ToString();
                    }
                }

                // Concatenate files
                using (var outputFile = File.Create(strIndexPath))
                {
                    for (int u = 0; u < url.Length; u++)
                    {
                        using (var inputFile = File.OpenRead(strIndexPath+u.ToString()))
                        {
                            inputFile.CopyTo(outputFile);
                        }
                    }
                }
            }

            for(int u = 0; u < url.Length; u++)
            { 
                if (File.Exists(strIndexPath+u.ToString()))
                {
                    // Load file into string
                    String strIndex = File.ReadAllText(strIndexPath+u.ToString());

                    // Get all links into a list
                    MatchCollection mcLinksDupes = Regex.Matches(strIndex, "(\".*?tti.*?\")");
                    var mcLinks = mcLinksDupes
                             .OfType<Match>()
                            .Select(m => m.Groups[0].Value)
                            .Distinct();
;

                    Int32 fileCounter = 0;
                    foreach (string mLink in mcLinks)
                    {
                        CycleParameters param = new CycleParameters();

                        param.Filename = mLink.Trim((Char)34);
                        param.Url = url[u] + param.Filename;
                        param.FilePath = strTempFolder + "\\" + param.Filename;

                        if (!offlineMode)
                            wcTeefax.DownloadFile(param.Url, param.FilePath);


                        if (File.Exists(param.FilePath))
                        {
                            this.RowLength = 40;
                            this.packetSize = 40;
                            this.OpenService(param.FilePath);
                            this.Revolutions = 0;
                            int biggestSubPage = 0;
                            int intSubPage = -1;
                            if (this.ServiceType != ServiceType.Unknown)
                            {
                                do
                                {
                                    biggestSubPage = intSubPage;

                                    // Wind back a line so that we can read the first line of the next page instead of skipping one
                                    Position = Position > RowLength ? Position - RowLength : Position;

                                    // Get next page from the Service object
                                    param.Page = this.GetPage();
                                    intSubPage = Convert.ToInt32(param.Page.Lines[0].TimeCode.Replace(":", ""));
                                    System.Diagnostics.Debug.Print("OpenService URI - " + param.Page.Lines[0].MagPage + " " + param.Page.Lines[0].TimeCode);

                                    //if (param.Page.Lines[0].MagPage + " " + param.Page.Lines[0].TimeCode == "335 00:00")
                                    //    System.Diagnostics.Debug.WriteLine("oof");

                                    // Only process the page if we haven't already been around once
                                    if (intSubPage > biggestSubPage)
                                    {
                                        // Get the Cycle Time parameters for the page
                                        StreamReader reader = File.OpenText(param.FilePath);

                                        long lastPosition = 0;
                                        while (reader.Peek() >= 0 && reader.BaseStream.Position >= lastPosition && param.TypeValue == null)
                                        {
                                            String s = reader.ReadLine();
                                            lastPosition = reader.BaseStream.Position;
                                            if (s.StartsWith("CT,"))
                                            {
                                                String value = s.Substring(s.IndexOf(",") + 1);
                                                String[] ctParams = value.Split(Convert.ToChar(","));
                                                if (ctParams.Length == 2)
                                                {
                                                    param.TypeValue = ctParams[0];
                                                    param.CycleType = ctParams[1];

                                                    //if (param.Page.Lines[0].Magazine == 8)
                                                    //System.Diagnostics.Debug.WriteLine("param.Magazine:" + param.Magazine + " magpage:" + param.Page.Lines[0].MagPage);

                                                }
                                            }
                                        }

                                        this.Revolutions++;

                                        param.Magazine = param.Page.Lines[0].Magazine;
                                        lService.Add(param);
                                        reader.Close();
                                    }
                                } while (intSubPage > biggestSubPage);
                            }
                        }
                        else
                            System.Diagnostics.Debug.Print("File not found:" + param.FilePath);

                        fileCounter++;
                        _pcLoaded = Convert.ToInt32(Convert.ToDouble(fileCounter) / Convert.ToDouble(mcLinksDupes.Count / 2) * 100);

                    }

                    //Dump the lot to a binary file for debug
                    /*File.Delete(Environment.GetEnvironmentVariable("temp") + "\\filestream.t42");
                    FileStream fs = new FileStream(Environment.GetEnvironmentVariable("temp") + "\\filestream.t42", FileMode.Append);

                    foreach (CycleParameters p in lService)
                    {
                        foreach (Line l in p.Page.Lines)
                        {
                            if (l.Bytes != null)
                            {
                                fs.Write(l.Bytes, 0, l.Bytes.Length);
                            }
                        }
                    }
                

                    fs.Dispose();
                    fs.Close();*/

                    // This is the complete service, sorted - each subpage is an element in the list.
                    //List<CycleParameters> lServiceSorted = lService.OrderBy(o => o.Page.Lines[0].MagPage).ToList();
                    //List<CycleParameters> lServiceSorted = lService;

                    //Dump the lot to a binary file for debug
                    //File.Delete(Environment.GetEnvironmentVariable("temp") + "\\filestream_sorted.t42");
                    //fs = new FileStream(Environment.GetEnvironmentVariable("temp") + "\\filestream_sorted.t42", FileMode.Append);

                    /*foreach (CycleParameters p in lService)
                    {
                        foreach (Line l in p.Page.Lines)
                        {
                            if (l.Bytes != null)
                            {
                                fs.Write(l.Bytes, 0, l.Bytes.Length);
                            }

                        }
                    }

                    fs.Dispose();
                    fs.Close();*/


                    // For each magazine, create a list of pages and subpages
                    // When the timer is running, the pages will be fetched in the order specified in mclMagCycleList
                    // MagCycleList only contains the first subpage

                    for (Int32 n = 0; n < 8; n++)
                    {
                        mclMagCycleList[n] = new MagazineCycleList();
                    }

                    foreach (CycleParameters p in lService)
                    {
                        /*if (p.Filename.Contains("401"))
                        { 
                            String pageNo = p.Page.Lines[0].MagPage +"s"+ p.Page.Lines[0].TimeCode.Replace(":","");
                            System.Diagnostics.Debug.WriteLine("Page: " + pageNo);
                            BinaryWriter writer = new BinaryWriter(new FileStream(Environment.GetEnvironmentVariable("temp") + "\\" + pageNo + ".bin", FileMode.Create));
                            for (int n=0; n < 24; n++)  
                                writer.Write(p.Page.Lines[n].Bytes);
                            writer.Close();
                            writer = null;
                        }*/
                        if (p.Page.Lines[0].Page != "")
                        {
                            string mp = p.Magazine.ToString() + p.Page.Lines[0].Page;
                            string subpage = p.Page.Lines[0].TimeCode;

                            mclMagCycleList[p.Magazine].AddItem(mp, subpage);
                        }
                        else
                            System.Diagnostics.Debug.WriteLine("Blank .Page: " + p.Filename);
                    }

                }

            }

            _fileType = "URL";

            //For debugging
            // Saves only the last page
            //BinaryWriter writer = new BinaryWriter(new FileStream(Environment.GetEnvironmentVariable("temp") + "\\conv.bin", FileMode.Create));
            //writer.Write(byteReader);
            //writer.Close();
            //writer = null;

            _linesPerFrame = 32;

            _loaded = true;

            if (byteReader == null)
                returnMessage = "There was an error fetching the remote service and no pages were downloaded.";

            this.Status = returnMessage;
            return returnMessage;
        }

        public string OpenService(byte[] readerTemp)
        {
            _filename = null;

            byteReader = new byte[readerTemp.Length];
            byteReader = readerTemp;

            this.Length = byteReader.Length;
            Reset();
            Revolutions = 0;
            RowLength = 42;
            _serviceType = ServiceType.Service;

            return "";
        }
        private Int32 NumLinesPerFrame()
        {
            // Get line to force the population of currentFrame object
            Line dummy = new Line();
            do
            {
                dummy = GetNextLine();
            }
            while (dummy.Type == LineTypes.Blank);

        Int32 linesPerField = 0;

            // Count the rows that don't start with 00 00 or FF FF
            //Is the below for .bin files?  Why 21?
            // for (Int32 n = 1; n < 21; n++)

            for (Int32 n = 0; n < 32; n++)
            {
                Byte b0 = dummy.Bytes[1];
                Byte b1 = dummy.Bytes[2];

                if ((b0 != 0 || b1 != 0) && (b0 != 0xff || b1 != 0xff))
                    linesPerField++;

                GetNextLine();
            }

            // Reset Service to beginning
            this.Reset();


            return linesPerField;
        }

        public Line GetNextLine()
        {
            if (this.FileType != "URL")
                return internalGetNextLine(-1);
            else
                return internalGetNextLineURL();
        }

        public Line GetNextLine(String magPage = "")
        {
            if (magPage != "")
            {
                Int32 magazine = Convert.ToInt32(magPage.Substring(0, 1));
                String page = magPage.Substring(1, 2);
                return internalGetNextLine(magazine);
            }
            else
                return internalGetNextLine();

            ;
        }

        public Line GetNextLine(int magazine = -1)
        {
            if (magazine != -1)
            {
                return internalGetNextLine(magazine);
            }
            else
                return internalGetNextLine();
            ;
        }

        private Line internalGetNextLine(int magazine = -1)
        {
            Boolean escape = false;
            Line workingLine = new Line();
            //packetSize = 42;

            if (magazine == 8)
                magazine = 0;

            if (byteReader != null)
            {
                while (!escape && byteReader.LongLength != 0)
                {
                    workingLine.Clear();

                    // Always return the first line if MRAG not filtered
                    if (magazine == -1)
                        escape = true;

                    //At end of file?  Reset pointer and let's go round again!
                    if (localPos > byteReader.LongLength - packetSize)
                    {
                        Revolutions++;
                        this.Position = 0;
                    }

                    //FrameLine++;

                    //Read next Line
                    Byte[] lineBytes = new Byte[42];
                    String lineConverted = "";
                    //String lineConvertedTemp = "";
                    //currentFrame.LineConverted[line] = "";
                    //File.WriteAllBytes(Environment.GetEnvironmentVariable("temp") + "\\bytereader.t42", byteReader);
                    for (byte byt = 0; byt < 42; byt++)
                    {
                        b = byteReader[localPos];
                        localPos++;
                        lineBytes[byt] = b;
                        Char decodedChar = DecodeChar(b);
                        lineConverted += decodedChar.ToString();
                        //lineConvertedTemp += Convert.ToChar(b & 0x7f);
                        if (Convert.ToByte(decodedChar) == 0xff && b != 0xff)
                            lineBytes[byt] = 0xff;
                    }

                    //FrameLine = 0;


                    //Set line and frame properties
                    workingLine.LineNo = FrameLine;
                    workingLine.Frame = Convert.ToInt32(localPos / frameSize);

                    //Mark the start of the line in the Service file
                    //workingLine.StartPos = localPos - frameSize + ((FrameLine) * packetSize);
                    //workingLine.EndPos = localPos - frameSize + (FrameLine * packetSize) - 1;
                    workingLine.StartPos = this.Position - packetSize;
                    workingLine.EndPos = this.Position - 1;

                    if (workingLine.StartPos < 0)
                        workingLine.StartPos += frameSize;

                    if (workingLine.EndPos < 0)
                        workingLine.EndPos += frameSize;

                    workingLine.Bytes = lineBytes;
                    workingLine.Type = ((workingLine.Bytes[0] != 0 && workingLine.Bytes[1] != 0) && (workingLine.Bytes[0] != 0xff && workingLine.Bytes[1] != 0xff)) ? LineTypes.Unknown : LineTypes.Blank;

                    if (workingLine.Type != LineTypes.Blank)
                    {


                        //Get MRAG
                        workingLine.MRAG1 = lineBytes[0];
                        workingLine.MRAG2 = lineBytes[1];
                        workingLine.PU = lineBytes[2];
                        workingLine.PT = lineBytes[3];
                        workingLine.MU = lineBytes[4];
                        workingLine.MT = lineBytes[5];
                        workingLine.HU = lineBytes[6];
                        workingLine.HT = lineBytes[7];
                        workingLine.CA = lineBytes[8];
                        workingLine.CB = lineBytes[9];


                        workingLine.Magazine = GetMagazine(workingLine.MRAG1);
                        workingLine.Row = GetRow(workingLine.MRAG1, workingLine.MRAG2);



                        // Check to see if the Magazine matches the filter if a filter's set
                        if (!escape && magazine == workingLine.Magazine)
                            escape = true;

                        if (escape)
                        // next line is found so populate it with the rest of the data
                        {
                            Byte lineLength = 40;

                            switch (workingLine.Row)
                            {
                                case 24:
                                    //Fastext line
                                    workingLine.Type = LineTypes.FastextDisplay;
                                    workingLine.Text = lineConverted.Substring(2, lineLength);
                                    break;

                                case 0:
                                    //Header line
                                    workingLine.Type = LineTypes.Header;
                                    //offset += 8;

                                    workingLine.Page = GetPageNumber(workingLine.PU, workingLine.PT);
                                    workingLine.TimeCode = GetTimeCode(workingLine.MU, workingLine.MT, workingLine.HU, workingLine.HT);
                                    workingLine.Flags.C4_Erase = GetBit(8, workingLine.MT);
                                    workingLine.Flags.C5_Newsflash = GetBit(6, workingLine.HT);
                                    workingLine.Flags.C6_Subtitle = GetBit(8, workingLine.HT);
                                    workingLine.Flags.C7_SuppressHeader = GetBit(2, workingLine.CA);
                                    workingLine.Flags.C8_Update = GetBit(4, workingLine.CA);
                                    workingLine.Flags.C9_InterruptedSequence = GetBit(6, workingLine.CA);
                                    workingLine.Flags.C10_InhibitDisplay = GetBit(8, workingLine.CA);
                                    workingLine.Flags.C11_MagazineSerial = GetBit(2, workingLine.CB);
                                    workingLine.Flags.C12 = GetBit(4, workingLine.CB);
                                    workingLine.Flags.C13 = GetBit(6, workingLine.CB);
                                    workingLine.Flags.C14 = GetBit(8, workingLine.CB);
                                    workingLine.Text = lineConverted.Substring(10, lineLength - 8);
                                    workingLine.MagPage = workingLine.Magazine.ToString() + workingLine.Page;
                                    break;
                                case 25:
                                    workingLine.Type = LineTypes.FastextDisplay;
                                    workingLine.Text = lineConverted.Substring(2, lineLength);
                                    break;
                                case 26:
                                    workingLine.Type = LineTypes.Enhanced;
                                    workingLine.Text = lineConverted.Substring(2, lineLength);
                                    break;
                                case 27:
                                    workingLine.Type = LineTypes.FastextLinks;
                                    workingLine.Text = lineConverted.Substring(2, lineLength);
                                    break;
                                case 28:
                                    workingLine.Type = LineTypes.Enhanced;
                                    workingLine.Text = lineConverted.Substring(2, lineLength);
                                    break;
                                case 29:
                                    workingLine.Type = LineTypes.Enhanced;
                                    workingLine.Text = lineConverted.Substring(2, lineLength);
                                    break;
                                case 30:
                                    workingLine.Type = LineTypes.DataServices;
                                    workingLine.Text = lineConverted.Substring(2, lineLength);
                                    break;
                                case 31:
                                    workingLine.Type = LineTypes.DataServices;
                                    workingLine.Text = lineConverted.Substring(2, lineLength);
                                    break;
                                default:
                                    workingLine.Type = LineTypes.Line;
                                    workingLine.Text = lineConverted.Substring(2, lineLength);
                                    break;
                            }
                            try
                            {
                                if (workingLine.Type == LineTypes.Header)
                                {
                                    workingLine.Text = lineConverted.Substring(10, lineLength - 8);
                                }
                                else
                                {
                                    workingLine.Text = lineConverted.Substring(2, lineLength);
                                }
                            }
                            catch { }
                        }
                    }

                }
            }

            return workingLine;
        }

        public string ExtractValue(byte field)
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

        private char DecodeChar(byte b)
        {
            // Returns a decoded char for a received byte
            char c = Convert.ToChar(b);

            Int32 bAnded = 0xff;

            if (_doParityCheck)
            {
                Boolean calculatedOddParity = false;
                String strB = Convert.ToString(b, 2).PadLeft(8,Convert.ToChar("0"));
                Int32 count = 0;
                for (Int32 n=1; n < strB.Length; n++)
                {
                    if (strB.Substring(n, 1) == "1")
                        count++;
                }
                if (count % 2 == 0)
                    calculatedOddParity = true;

                Boolean originalOddParityBit = strB.Substring(0, 1) == "1" ? true : false;

                if (calculatedOddParity != originalOddParityBit)
                    bAnded = 0xFF;
                else
                    bAnded = (Convert.ToInt32(b) & 127);
            }
            else
            {
                bAnded = (Convert.ToInt32(b) & 127);
            }
            

            char converted = Convert.ToChar(bAnded);

            return converted;
        }

        private Int32 GetMagazine(byte mrag1)
        {
            string binRev = ExtractValue(mrag1);

            /*_useHamming = false;
            String mnh = ExtractValue(mrag1);
            _useHamming = true;
            String mh = ExtractValue(mrag1);
            if (mh != mnh)
                System.Diagnostics.Debug.WriteLine(mh + "  " + mnh);
            */

            //Construct return binary as MSB first
            string magBin = binRev.Substring(5, 1) + binRev.Substring(3, 1) + binRev.Substring(1, 1);

            byte magazine = Convert.ToByte(magBin, 2);

            return Convert.ToInt32(magazine);
        }

        public Int32 GetRow(byte mrag1, byte mrag2)
        {
            //Convert to binary string
            string binRev1 = ExtractValue(mrag1);
            string binRev2 = ExtractValue(mrag2);

            // Construct return binary as MSB first
            string rowBin = binRev2.Substring(7, 1) + binRev2.Substring(5, 1) + binRev2.Substring(3, 1) + binRev2.Substring(1, 1) + binRev1.Substring(7, 1);

            byte row = Convert.ToByte(rowBin, 2);
            return row;
        }

        private string GetPageNumber(byte pu, byte pt)
        {
            //Removing hamming and convert to LSB first
            string binRevUnits = ExtractValue(pu);
            string binRevTens = ExtractValue(pt);

            // Construct return binary as MSB first
            string binUnits = binRevUnits.Substring(7, 1) + binRevUnits.Substring(5, 1) + binRevUnits.Substring(3, 1) + binRevUnits.Substring(1, 1);
            string binTens = binRevTens.Substring(7, 1) + binRevTens.Substring(5, 1) + binRevTens.Substring(3, 1) + binRevTens.Substring(1, 1);

            //Get page units and tens
            int pageUnits = Convert.ToInt32(binUnits, 2);
            int pageTens = Convert.ToInt32(binTens, 2);

            return Convert.ToString(pageTens, 16).ToUpper() + Convert.ToString(pageUnits, 16).ToUpper();
        }

        private string GetTimeCode(byte mu, byte mt, byte hu, byte ht)
        {
            //Removing hamming and convert to LSB first
            string binRevMinUnits = ExtractValue(mu);
            string binRevMinTens = ExtractValue(mt);
            string binRevHourUnits = ExtractValue(hu);
            string binRevHourTens = ExtractValue(ht);

            // Construct return binary as MSB first
            string binMinUnits = binRevMinUnits.Substring(7, 1) + binRevMinUnits.Substring(5, 1) + binRevMinUnits.Substring(3, 1) + binRevMinUnits.Substring(1, 1);
            string binMinTens = binRevMinTens.Substring(5, 1) + binRevMinTens.Substring(3, 1) + binRevMinTens.Substring(1, 1);
            string binHourUnits = binRevHourUnits.Substring(7, 1) + binRevHourUnits.Substring(5, 1) + binRevHourUnits.Substring(3, 1) + binRevHourUnits.Substring(1, 1);
            string binHourTens = binRevHourTens.Substring(3, 1) + binRevHourTens.Substring(1, 1);

            int intMins = (Convert.ToInt32(binMinTens, 2) * 10) + Convert.ToInt32(binMinUnits, 2);
            int intHour = (Convert.ToInt32(binHourTens, 2) * 10) + Convert.ToInt32(binHourUnits, 2);
            //Console.Write("hours:{0}, mins{1}", intHour.ToString(), intMins.ToString());

            string strMins = "00".Substring(0, 2 - intMins.ToString().Length) + intMins.ToString();
            string strHour = "00".Substring(0, 2 - intHour.ToString().Length) + intHour.ToString();

            return strHour + ":" + strMins;
        }

        private Boolean GetBit(Int32 bit, byte inByte)
        {
            // Note: this uses the teletext spec bit notation method, ie LSB first.

            // Check bit is in range
            if (bit < 1 || bit > 8)
                throw new ArgumentException("GetBit: Bit reference is out of range.");

            String bin = ExtractValue(inByte);

            if (bin.Substring(bit - 1, 1) == "1")
                return true;
            else
                return false;

        }

        public void Reset()
        {
            //reader.BaseStream.Seek(0, SeekOrigin.Begin);
            localPos = 0;
            //FrameLine set too high so that the Frame is read again from the Service's new position
            //FrameLine = 99;
            Revolutions = 0;
        }

        public Line FindHeader(string magpage, bool reverse=false)
        {
            Boolean escape = false;
            string page = "";
            int magazine = 0;

            if (magpage == null)
                escape = true;

            if (!escape && magpage.Length == 3)
            {
                page = magpage.Substring(1, 2);
                magazine = Convert.ToInt32(magpage.Substring(0, 1));
            }
            else
                escape = true;

            Line line = new Line();
            Line nextLine = new Line();

            long initPosition = localPos;
            Int32 revs = Revolutions;

            while (!escape)
            {   
                //line = nextLine;     
                
                //if (line.Type == null) 
                line = reverse ? GetPreviousLine(magazine, line.StartPos == 0 ? this.Position : line.StartPos) : GetNextLine(magazine);

                //nextLine = reverse ? GetPreviousLine(magazine) : GetNextLine(magazine);


                //if (reverse)
                    //this.Position -= packetSize;
                    //nextLine = reverse ? GetPreviousLine(magazine) : GetNextLine(magazine);
                //else
                //    this.Position -= packetSize;

                if (line.Page == page && line.Magazine == magazine)
                    // if the next line to fetch (either forwards or reverse) is a header then continue searching until we come
                    // to the actual page (this is to skip past header padding)
                    //if (line.Magazine == nextLine.Magazine && nextLine.Type != LineTypes.Header)

                        escape = true;

                

                if (reverse ? localPos < initPosition - intCheckHorizon : localPos > initPosition + intCheckHorizon)
                {
                    escape = true;
                    line.Text = "Line not found (searched past CheckHorizon).";
                    line.Magazine = -1;
                }

                if (Revolutions > revs + 1)
                {
                    escape = true;
                    line.Text = "Line not found (too many Revolutions).";
                    line.Magazine = -1;
                }
            }

            return line;
        }

        private Line GetPreviousLine(int magazine = -1, long startPos = -1)
        {
            if (magazine != -1)
            {
                return internalGetPreviousLine(magazine, startPos);
            }
            else
                return internalGetPreviousLine();
        }

        private Line internalGetPreviousLine(int magazine = -1, long startPos = -1)
        {

            Boolean escape = false;
            Line workingLine = new Line();
            if (startPos != -1)
                this.Position = startPos;

            if (byteReader != null)
            {
                while (!escape && byteReader.LongLength != 0)
                {
                    workingLine.Clear();

                    // Always return the first line if MRAG not filtered
                    if (magazine == -1)
                        escape = true;

                    //At end of file?  Reset pointer and let's go round again!
                    if (localPos - packetSize < 0)
                    {
                        Revolutions++;
                        this.Position = byteReader.LongLength;
                    }

                    //Read previous Line
                    Byte[] lineBytes = new Byte[42];
                    String lineConverted = "";

                    for (byte byt = 0; byt < 42; byt++)
                    {
                        b = byteReader[localPos - 42 + byt];
                        //localPos++;
                        lineBytes[byt] = b;
                        Char decodedChar = DecodeChar(b);
                        lineConverted += decodedChar.ToString();
                        //lineConvertedTemp += Convert.ToChar(b & 0x7f);
                        if (Convert.ToByte(decodedChar) == 0xff && b != 0xff)
                            lineBytes[byt] = 0xff;
                    }

                    this.Position = localPos - 42;

                    //Set line and frame properties
                    workingLine.LineNo = FrameLine;
                    workingLine.Frame = Convert.ToInt32(localPos / frameSize);

                    //Mark the start of the line in the Service file
                    //workingLine.StartPos = this.Position - packetSize;
                    workingLine.StartPos = this.Position;
                    workingLine.EndPos = this.Position + packetSize;

                    if (workingLine.StartPos < 0)
                        workingLine.StartPos += frameSize;

                    if (workingLine.EndPos < 0)
                        workingLine.EndPos += frameSize;

                    workingLine.Bytes = lineBytes;
                    workingLine.Type = ((workingLine.Bytes[0] != 0 && workingLine.Bytes[1] != 0) && (workingLine.Bytes[0] != 0xff && workingLine.Bytes[1] != 0xff)) ? LineTypes.Unknown : LineTypes.Blank;

                    if (workingLine.Type != LineTypes.Blank)
                    {


                        //Get MRAG
                        workingLine.MRAG1 = lineBytes[0];
                        workingLine.MRAG2 = lineBytes[1];
                        workingLine.PU = lineBytes[2];
                        workingLine.PT = lineBytes[3];
                        workingLine.MU = lineBytes[4];
                        workingLine.MT = lineBytes[5];
                        workingLine.HU = lineBytes[6];
                        workingLine.HT = lineBytes[7];
                        workingLine.CA = lineBytes[8];
                        workingLine.CB = lineBytes[9];


                        workingLine.Magazine = GetMagazine(workingLine.MRAG1);
                        workingLine.Row = GetRow(workingLine.MRAG1, workingLine.MRAG2);



                        // Check to see if the Magazine matches the filter if a filter's set
                        if (!escape && magazine == workingLine.Magazine)
                            escape = true;

                        //If row is out of bounds get another row
                        //if (workingLine.Row > 31)
                        //escape = false;

                        // If row is a non-data row get the next row
                        //if ((lineBytes[0] == 0 && lineBytes[1] == 0) || (lineBytes[0] == 0xFF && lineBytes[1] == 0xFF))
                        //    escape = false;

                        if (escape)
                        {
                            Byte lineLength = 40;

                            switch (workingLine.Row)
                            {
                                case 24:
                                    //Fastext line
                                    workingLine.Type = LineTypes.FastextDisplay;
                                    workingLine.Text = lineConverted.Substring(2, lineLength);
                                    break;

                                case 0:
                                    //Header line
                                    workingLine.Type = LineTypes.Header;
                                    //offset += 8;

                                    workingLine.Page = GetPageNumber(workingLine.PU, workingLine.PT);
                                    workingLine.TimeCode = GetTimeCode(workingLine.MU, workingLine.MT, workingLine.HU, workingLine.HT);
                                    workingLine.Flags.C4_Erase = GetBit(8, workingLine.MT);
                                    workingLine.Flags.C5_Newsflash = GetBit(6, workingLine.HT);
                                    workingLine.Flags.C6_Subtitle = GetBit(8, workingLine.HT);
                                    workingLine.Flags.C7_SuppressHeader = GetBit(2, workingLine.CA);
                                    workingLine.Flags.C8_Update = GetBit(4, workingLine.CA);
                                    workingLine.Flags.C9_InterruptedSequence = GetBit(6, workingLine.CA);
                                    workingLine.Flags.C10_InhibitDisplay = GetBit(8, workingLine.CA);
                                    workingLine.Flags.C11_MagazineSerial = GetBit(2, workingLine.CB);
                                    workingLine.Flags.C12 = GetBit(4, workingLine.CB);
                                    workingLine.Flags.C13 = GetBit(6, workingLine.CB);
                                    workingLine.Flags.C14 = GetBit(8, workingLine.CB);
                                    workingLine.Text = lineConverted.Substring(10, lineLength - 8);
                                    workingLine.MagPage = workingLine.Magazine.ToString() + workingLine.Page;
                                    break;
                                case 27:

                                    break;
                                default:
                                    workingLine.Type = LineTypes.Line;
                                    workingLine.Text = lineConverted.Substring(2, lineLength);
                                    break;
                            }
                            try
                            {
                                if (workingLine.Type == LineTypes.Header)
                                {
                                    workingLine.Text = lineConverted.Substring(10, lineLength - 8);
                                }
                                else
                                {
                                    workingLine.Text = lineConverted.Substring(2, lineLength);
                                }
                            }
                            catch { }
                        }
                    }

                }
            }

            //this.Position = this.Position - packetSize;

            return workingLine;


        }

        public Line GetNextHeader(int magazine = -1)
        {
            Line localline = new Line();
            Boolean escape = false;
            Int32 startRevs = Revolutions;

            if (magazine == 8)
                magazine = 0;

            while (!escape)
            {
                if (magazine == -1)
                    localline = GetNextLine();
                else
                    localline = GetNextLine(magazine);

                if (localline.Type == LineTypes.Header)
                {

                    // Is the next line in this magazine a header too?
                    long headerPosn = this.Position;
                    //long headerPosn = localline.StartPos;
                    Line l = GetNextLine(localline.Magazine);

                    if (l.Row != 0)
                    {
                        escape = true;

                    }
                    this.Position = headerPosn;

                    // if we are looking for a particular magazine, and this isn't it, keep looking
                    if (magazine != -1)
                        if (localline.Magazine != magazine)
                            escape = false;

                    //else this.Position = this.Position - this.PacketSize - 2;
                    //else this.Position = headerPosn;
                }


                   
                if (startRevs < Revolutions)
                {
                    //Revolutions++;
                    escape = true;
                }
            }
            return localline;
        }

        public Page GetPage(string magPage = "", string timeCode = "", bool reverse = false)
        {
            Page localPage = new Page();
            Line nextLine = new Line();

            long initPos = this.Position;

            // Move Service to the header for required page
            Line header;
            if (magPage == "")
            {
                header = GetNextHeader();
            }
            else
            {
                do
                {
                    header = FindHeader(magPage, reverse);
                    if (reverse)
                    {
                        // Find the start of the current page
                        header = FindHeader(magPage, reverse);
                        // Wind back 1 packet so we don't read it again
                        this.Position = header.StartPos - packetSize;
                        // Find previous header
                        long loopInitPos = this.Position;
                        // Find previous header
                        header = FindHeader(magPage);
                    }
                } 
                while (!(timeCode == "" || header.TimeCode == timeCode));
            }


            localPage.Lines[0].Text = header.Text;
            localPage.Lines[0].LineNo = header.LineNo;
            localPage.Lines[0].Frame = header.Frame;
            localPage.Lines[0].EndPos = header.EndPos;
            localPage.Lines[0].Magazine = header.Magazine % 8;
            localPage.Lines[0].Page = header.Page;
            localPage.Lines[0].MagPage = header.Magazine.ToString() + header.Page;
            localPage.Lines[0].Row = header.Row;
            localPage.Lines[0].StartPos = header.StartPos;
            localPage.Lines[0].TimeCode = header.TimeCode;
            localPage.Lines[0].Type = header.Type;
            localPage.Lines[0].Flags = header.Flags;
            localPage.Lines[0].MRAG1 = header.MRAG1;
            localPage.Lines[0].MRAG2 = header.MRAG2;
            localPage.Lines[0].PU = header.PU;
            localPage.Lines[0].PT = header.PT;
            localPage.Lines[0].MU = header.MU;
            localPage.Lines[0].MT = header.MT;
            localPage.Lines[0].HU = header.HU;
            localPage.Lines[0].HT = header.HT;
            localPage.Lines[0].CA = header.CA;
            localPage.Lines[0].CB = header.CB;
            localPage.Lines[0].Bytes = header.Bytes;

            //this.Position = header.StartPos;

            // We have the header, get the rest of the rows
            if (header.Magazine != -1)
            {
                Int32 row = 1;

                // Get next line
                nextLine = GetNextLine(magPage);
                localPage.Lines[row].Text = nextLine.Text;

                //while (nextLine.Type != LineTypes.Header  || (nextLine.Type == LineTypes.Header && nextLine.MagPage == magPage && _serviceType != ServiceType.Carousel))
                while (nextLine.Type != LineTypes.Header && row < 256)
                    //while (nextLine.Type != LineTypes.Header && row < 256)
                    {
                    // We already have a header, so don't write any more from the same page
                    if (nextLine.Type != LineTypes.Header && row < 256 && nextLine.Row < 30)
                    {
                        localPage.Lines[row].Text = nextLine.Text;
                        localPage.Lines[row].LineNo = nextLine.LineNo;
                        localPage.Lines[row].Frame = nextLine.Frame;
                        localPage.Lines[row].EndPos = nextLine.EndPos;
                        localPage.Lines[row].Magazine = nextLine.Magazine;
                        localPage.Lines[row].Page = nextLine.Page;
                        localPage.Lines[row].MagPage = nextLine.Magazine.ToString() + nextLine.Page;
                        localPage.Lines[row].Row = nextLine.Row;
                        localPage.Lines[row].StartPos = nextLine.StartPos;
                        localPage.Lines[row].TimeCode = nextLine.TimeCode;
                        localPage.Lines[row].Type = nextLine.Type;
                        localPage.Lines[row].MRAG1 = nextLine.MRAG1;
                        localPage.Lines[row].MRAG2 = nextLine.MRAG2;
                        localPage.Lines[row].Bytes = nextLine.Bytes;
                        row++;
                    }


                    nextLine = GetNextLine(magPage);

                }
            }
            else
            {
                localPage.Lines[0].Magazine = -1;
            }

            // Since the end of the page is governed by a header row of the same magazine, decrement the frame line pointer
            // so that the header row is read next time (or entire pages are skipped)
            this.Position = this.Position - this.RowLength;

            //if (reverse)
            //    this.Position = initPos;

            return localPage;
        }

        public Hashtable ConvertMRAG(Byte mrag1, Byte mrag2)
        {
            Hashtable convertedMRAG = new Hashtable();
            convertedMRAG["Magazine"] = GetMagazine(mrag1);
            convertedMRAG["Row"] = GetRow(mrag1, mrag2);

            return convertedMRAG;
        }

        public String ConvertPageUnitsTens(Byte pu, Byte pt)
        {
            return GetPageNumber(pu, pt);
        }

        public Line CreatePacket(Int32 Magazine, String Page, String Subpage, byte PacketNo, byte RowNo, string Text, String flags = "10000000000")
        {
            String textForByte = Text.PadRight(40, (Char)0x00);
            textForByte = Text.PadLeft(PacketSize, (Char)0x00);
            flags = flags.PadLeft(16);

            Line newline = new Line();
            newline.Clear();
            newline.EndPos = 0;
            newline.Flags = new ControlFlags();
            newline.Frame = 0;
            newline.LineNo = PacketNo;
            newline.Magazine = Magazine % 8;
            newline.Page = Page;
            newline.MagPage = newline.Magazine.ToString() + newline.Page;
            newline.Row = RowNo;
            newline.StartPos = 0;
            newline.Text = Text;
            newline.Bytes = Encoding.ASCII.GetBytes(textForByte);
            newline.TimeCode = Subpage;
            newline.Flags = new ControlFlags();
            newline.Flags.C4_Erase = (flags.Substring(1, 1) == "1" ? true : false);
            newline.Flags.C5_Newsflash = (flags.Substring(2, 1) == "1" ? true : false);
            newline.Flags.C6_Subtitle = (flags.Substring(3, 1) == "1" ? true : false);
            newline.Flags.C7_SuppressHeader = (flags.Substring(4, 1) == "1" ? true : false);
            newline.Flags.C8_Update = (flags.Substring(5, 1) == "1" ? true : false);
            newline.Flags.C9_InterruptedSequence = (flags.Substring(6, 1) == "1" ? true : false);
            newline.Flags.C10_InhibitDisplay = (flags.Substring(7, 1) == "1" ? true : false);
            newline.Flags.C11_MagazineSerial = (flags.Substring(8, 1) == "1" ? true : false);
            newline.Flags.C12 = (flags.Substring(9, 1) == "1" ? true : false);
            newline.Flags.C13 = (flags.Substring(10, 1) == "1" ? true : false);
            newline.Flags.C14 = (flags.Substring(11, 1) == "1" ? true : false);

            switch (PacketNo)
            {
                case 0:
                    newline.Type = LineTypes.Header;
                    break;
                case 25:
                    newline.Type = LineTypes.FastextDisplay;
                    break;
                default:
                    newline.Type = LineTypes.Line;
                    break;
            }

            return newline;
        }

        public Line CreatePacket(Int32 Magazine, String Page, String Subpage, byte PacketNo, byte RowNo, byte[] Bytes, String flags = "10000000000")
        {

            byte[] Bytes7f= new byte[Bytes.Length - 2];

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
                text = "   P" + Magazine.ToString() +  Page + " " + text.Substring(8);
            }

            flags = flags.PadLeft(16);

            Line newline = new Line();
            newline.Clear();
            newline.EndPos = 0;
            newline.Flags = new ControlFlags();
            newline.Frame = 0;
            newline.LineNo = PacketNo;
            newline.Magazine = Magazine % 8;
            newline.Page = Page;
            newline.MagPage = newline.Magazine.ToString() + newline.Page;
            newline.Row = RowNo;
            newline.StartPos = 0;
            newline.Text = text;
            newline.Bytes = Bytes;
            newline.TimeCode = Subpage;
            newline.Flags = new ControlFlags();
            newline.Flags.C4_Erase = (flags.Substring(1, 1) == "1" ? true : false);
            newline.Flags.C5_Newsflash = (flags.Substring(2, 1) == "1" ? true : false);
            newline.Flags.C6_Subtitle = (flags.Substring(3, 1) == "1" ? true : false);
            newline.Flags.C7_SuppressHeader = (flags.Substring(4, 1) == "1" ? true : false);
            newline.Flags.C8_Update = (flags.Substring(5, 1) == "1" ? true : false);
            newline.Flags.C9_InterruptedSequence = (flags.Substring(6, 1) == "1" ? true : false);
            newline.Flags.C10_InhibitDisplay = (flags.Substring(7, 1) == "1" ? true : false);
            newline.Flags.C11_MagazineSerial = (flags.Substring(8, 1) == "1" ? true : false);
            newline.Flags.C12 = (flags.Substring(9, 1) == "1" ? true : false);
            newline.Flags.C13 = (flags.Substring(10, 1) == "1" ? true : false);
            newline.Flags.C14 = (flags.Substring(11, 1) == "1" ? true : false);

            switch (PacketNo)
            {
                case 0:
                    newline.Type = LineTypes.Header;
                    break;
                case 25:
                    newline.Type = LineTypes.FastextDisplay;
                    break;
                default:
                    newline.Type = LineTypes.Line;
                    break;
            }

            return newline;
        }


        public Line CreatePacket(Int32 Magazine, Byte Packet, String[] FastextLinks)
        {
            Line newline = new Line();

            newline.EndPos = 0;
            newline.Flags = new ControlFlags();
            newline.Frame = 0;
            newline.LineNo = Packet;
            newline.Magazine = Magazine % 8;
            newline.Row = Packet;
            newline.StartPos = 0;
            newline.FastextLinks = FastextLinks;
            newline.Bytes = new Byte[42];
            newline.TimeCode = "00:00";

            newline.CalcHammingCodes();
            newline.Bytes[0] = newline.MRAG1;
            newline.Bytes[1] = newline.MRAG2;

            switch (Packet)
            {
                case 0:
                    newline.Type = LineTypes.Header;
                    break;
                default:
                    newline.Type = LineTypes.Line;
                    break;
            }


            return newline;
        }

        public Line internalGetNextLineURL()
        {
            Int32 mag;

            // Bump the magazine depending on the weighting matrix
            do
            {
                lastWeightingMatrixPosn++;

                if (lastWeightingMatrixPosn >= _urlWeightingMatrix.Length)
                    lastWeightingMatrixPosn = 0;

                mag = Convert.ToInt32(_urlWeightingMatrix.Substring(lastWeightingMatrixPosn, 1));

                // If there are no pages in the magazine, skip to the next
            } while (mclMagCycleList[mag].Count == 0);

            Line nextLine = new Line();

            String magPage = mclMagCycleList[mag].GetCurrentPageNumber();

            String subPage = mclMagCycleList[mag].GetCurrentSubPage();

            CycleParameters cpTest = lService.Find(o => o.Page.Lines[0].MagPage == magPage && o.Page.Lines[0].TimeCode == subPage);
            if (cpTest != null)
            {
                nextLine = cpTest.Page.Lines[mclMagCycleList[mag].CurrentPacket];
                nextLine.MagPage = magPage;
                nextLine.TimeCode = subPage;
                nextLine.Magazine = mag;
                if (nextLine.Bytes == null)
                    nextLine.Type = LineTypes.Blank;
                //nextLine.Page = magPage.Substring(1, 2);
            }
            else
            {
                String result = mclMagCycleList[mag].GetNext();
            }

            //if (magPage == "700")
            //    System.Diagnostics.Debug.WriteLine(magPage + " Packet: " + mclMagCycleList[mag].CurrentPacket + " " + nextLine.Text);

            //if (magPage == "0FE")
              //  System.Diagnostics.Debug.Write("mclMagCycleList[mag].CurrentPacket: "+ mclMagCycleList[mag].CurrentPacket);
            //if (nextLine.Type == null && magPage == "104")
            //    System.Diagnostics.Debug.WriteLine("null-me-do - current packet:" + mclMagCycleList[mag].CurrentPacket);


            mclMagCycleList[mag].CurrentPacket++;
            if (mclMagCycleList[mag].CurrentPacket > 31)
            {
                mclMagCycleList[mag].CurrentPacket = 0;
                String result = mclMagCycleList[mag].GetNext();
            }

            // If line is a header, substitute the page number and time into the relevant template characters
            if (nextLine.Row == 0 && nextLine.Bytes != null && nextLine.Type != LineTypes.Blank)
            {
                nextLine.Text = nextLine.Text.Replace("mpp", (nextLine.MagPage.Substring(0, 1) == "0" ? "8" + nextLine.MagPage.Substring(1, 2) : nextLine.MagPage));
                Array.Copy(Encoding.ASCII.GetBytes(nextLine.Text), 0, nextLine.Bytes, 0, nextLine.Text.Length);
                //System.Diagnostics.Debug.WriteLine(nextLine.Text);
            }

            return nextLine;
        }

        public void WriteLine(byte[] Packet, long InsertPosition)
        {
            if (InsertPosition > this.Length - 42)
                throw new Exception("Service.WriteLine: Insert position would insert past the end of the file.");

            if (Packet.Length != 42)
                throw new Exception("Service.WriteLine: Packet length must be 42 bytes.");


            Array.Copy(Packet, 0, byteReader, InsertPosition, Packet.Length);

        }

        public string SaveService(string Filename)
        {
            string error = "";

            // Ensure all lines have parity bit set correctly on all bytes
            //byte[] bytes = new byte[byteReader.Length];
            //for (int i = 0; i < this.byteReader.Length / 42; i++)
            //{
            //    Line line = new Line();
            //    line.Clear();
            //    Array.Copy(byteReader, i * 42, line.Bytes, 0, 42);
            //    TeletextTools tools = new TeletextTools();
            //    line.Magazine = tools.DecodeMagazineNoFromMRAG(line.Bytes[0]);
            //    line.Row = tools.DecodePacketNoFromMRAG(line.Bytes[0], line.Bytes[1]);
            //    if (line.Magazine >= 0 && line.Magazine < 25 && line.Bytes[0] != 0 && line.Bytes[1] != 0)
            //        line.CalcParity();

            //    Array.Copy(line.Bytes, 0,  bytes, i * 42, 42);
            //}

            

            try
            {
                //File.WriteAllBytes(Filename, bytes);
                File.WriteAllBytes(Filename, byteReader);
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return error;

        }

        public List<string> ListCarousels()
        {
            List<string> carousels = new List<string>();

            Int32 initialRevs = this.Revolutions;

            Boolean escape = false;
            long nextCheck = this.CheckHorizon;

            while (this.Revolutions == initialRevs && !escape)
            {
                Line localline = this.GetNextHeader();
                string header = localline.Text; //.Substring(9, 3);

                string pageNo = "";

                for (int start = 0; start < localline.Text.Length - 5; start++)
                    pageNo = localline.Magazine.ToString() + localline.Page;

                if (this.Position > nextCheck)
                    escape = true;


                if (!carousels.Contains(pageNo) && pageNo != "")
                    carousels.Add(pageNo);
            }

            if (carousels.Count > 1)
                this.ServiceType = ServiceType.Service;
            else
                this.ServiceType = ServiceType.Carousel;

            return carousels;
        }

        public ServiceType DetermineServiceType()
        {
            ServiceType returnType = ServiceType.Carousel;

            Int32 initialRevs = this.Revolutions;

            Boolean escape = false;
            long nextCheck = this.CheckHorizon;

            string pageNo = "";

            while (this.Revolutions == initialRevs && !escape)
            {
                Line localline = this.GetNextHeader();

                string newPageNo = localline.MagPage;

                if (newPageNo != pageNo && pageNo != "")
                {
                    returnType = ServiceType.Service;
                    escape = true;
                }

                if (this.Position > nextCheck)
                    escape = true;

                if (!escape)
                    pageNo = newPageNo;
            }

            return returnType;
        }
        
    }
    public enum ServiceType
    {
        Unknown,
        Service,
        Carousel
    }

}
