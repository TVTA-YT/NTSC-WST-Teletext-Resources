using System;

namespace TeletextSharedResources
{
    public class IndependentDataLine
    {
        public string IdlDesignationCode;
        public string EnhDesignationCode;
        public byte DataChannel;
        public string FormatType;
        public string FormatDescription;
        public int IdlBytesRequired;
        public int IdlDataPosition = 4;
        public string DecodedText;
        public string ServicePacketAddress;

        public void DecodeIDL(Line localline)
        {
            var t = new TeletextTools();
            // In the case of an IDL, the Designation Code is the second byte of the MRAG and the Data Channel is the first byte of the MRAG

            // Get designation code
            this.IdlDesignationCode = Convert.ToString(t.Decode84HammedValue(localline.Bytes[1]), 2);

            //Get Data Channel
            this.DataChannel = t.Decode84HammedValue(localline.Bytes[0]);
            //this.DataChannel = Convert.ToString(bytDataChannel, 2);

            // Process format type for IDL Format A or B

            switch (this.DataChannel)
            {
                case 0:
                    this.FormatDescription = "Packet 8/30";
                            break;
                case 1:
                    this.FormatDescription = "Packet 1/30";
                    break;
                case 2:
                    this.FormatDescription = "Packet 2/30";
                    break;
                case 3:
                    this.FormatDescription = "Packet 3/30";
                    break;
                case 4:
                    this.FormatDescription = "Low bit rate audio";
                    break;
                case 5:
                    this.FormatDescription = "Datavideo";
                    break;
                case 6:
                    this.FormatDescription = "Datavideo";
                    break;
                case 7:
                    this.FormatDescription = "Packet 7/30";
                    break;
                case 8:
                    this.FormatDescription = "IDL Format A or B";
                    break;
                case 9:
                    this.FormatDescription = "IDL Format A or B";
                    break;
                case 10:
                    this.FormatDescription = "IDL Format A or B";
                    break;
                case 11:
                    this.FormatDescription = "IDL Format A or B";
                    break;
                case 12:
                    this.FormatDescription = "Low bit rate audio";
                    break;
                case 13:
                    this.FormatDescription = "Datavideo";
                    break;
                case 14:
                    this.FormatDescription = "Datavideo";
                    break;
                case 15:
                    this.FormatDescription = "IDL Format B";
                    break;
            }

            // Placeholder
            if (this.FormatDescription == "IDL Format B")
                this.FormatType = "";

            if (this.FormatDescription == "IDL Format A or B")
            {
                //Get Format Type
                byte bytFormatType = t.Decode84HammedValue(localline.Bytes[2]);
                this.FormatType = Convert.ToString(bytFormatType, 2).PadLeft(4, Convert.ToChar("0"));


                if ((bytFormatType & 1) == 0)
                {
                    this.FormatDescription = "IDL Format A: ";

                    if ((bytFormatType & 2) == 0)
                        this.FormatDescription += "No repeat facility / ";
                    else
                    {
                        this.FormatDescription += "Repeat packet facility applies / ";
                        this.IdlDataPosition++;
                    }
                    if ((bytFormatType & 4) == 0)
                        this.FormatDescription += "Continuity indicator is implicit / ";
                    else
                    {
                        this.FormatDescription += "Explicit continuity indicator included / ";
                        this.IdlDataPosition++;
                    }
                    if ((bytFormatType & 8) == 0)
                        this.FormatDescription += "Data Length Byte not in use. ";
                    else
                    {
                        this.FormatDescription += "Data Length Byte in use. ";
                        this.IdlDataPosition++;
                    }

                    // I'm not interested in the positions of the CI, DL, RI or SPA data at present - do later if relevant

                    //Get Interpretation and Address Length
                    //HammingResults84 hrIal = new HammingResults84();
                    //hrIal = hrIal.HammingCheck84(localline.Bytes[2]);
                    //strDataBits = Convert.ToString(t.Decode84HammedValue(localline.Bytes[2]), 2);
                    //String strIal = strDataBits.Substring(7, 1) + strDataBits.Substring(5, 1) + strDataBits.Substring(3, 1) + strDataBits.Substring(1, 1);
                    Byte bytIal = t.Decode84HammedValue(localline.Bytes[3]);
                    int intSpaBytesRequired = bytIal;

                    this.FormatDescription += "SPA Bits Reqd: " + (bytIal * 4).ToString() + ". ";
                    this.IdlDataPosition += bytIal;

                    this.ServicePacketAddress = "";
                    for (int n = 0; n < intSpaBytesRequired; n++)
                    {
                        this.ServicePacketAddress += Convert.ToString(t.Decode84HammedValue(localline.Bytes[4 + n]), 2).PadLeft(4, Convert.ToChar("0")) + " ";
                    }
                    this.ServicePacketAddress = this.ServicePacketAddress.TrimEnd((Char)0x20);


                    this.DecodedText = localline.Text.Substring(IdlDataPosition-2);
                    this.DecodedText = this.DecodedText.Substring(0, this.DecodedText.Length - 2);
                    //intTelfaxByteStartPosn = IdlDataPosn;

                    //return localline.Text.Substring(intTelfaxByteStartPosn, localline.Text.Length - IdlDataPosn);

                }

            }
            if (this.FormatDescription == "Packet 8/30")
            {
                this.EnhDesignationCode = Convert.ToString(t.Decode84HammedValue(localline.Bytes[2]), 2).PadLeft(4, Convert.ToChar("0"));
                switch (this.EnhDesignationCode)
                {
                    case "0000":
                        this.FormatDescription += " Format 1";
                        var decoder0 = new Decode8_30_1(localline.Bytes);

                        DateTime dt0 = new DateTime(1858, 11, 16);
                        try
                        {
                            dt0 = dt0.AddDays(decoder0.ModifiedJulianDate);
                        }

                        catch { }

                        this.DecodedText = "P" +decoder0.InitialPage + " " + decoder0.NetworkIdent + " " + dt0.ToShortDateString() + " " + decoder0.UniversalTimeCoordinated + " " + decoder0.StatusDisplay;
                        break;
                    case "0001":
                        this.FormatDescription += " Format 1";
                        var decoder1 = new Decode8_30_1(localline.Bytes);

                        DateTime dt1 = new DateTime(1858, 11, 16);
                        try
                        {
                            dt1 = dt1.AddDays(decoder1.ModifiedJulianDate);
                        }
                        catch { }

                        this.DecodedText = "P" + decoder1.InitialPage + " " + decoder1.NetworkIdent + " " + dt1.ToShortDateString() + " " + decoder1.UniversalTimeCoordinated + " " + decoder1.StatusDisplay;
                        break;
                    case "0010":
                        this.FormatDescription += " Format 2";
                        var decoder21 = new Decode8_30_2(localline.Bytes);

                        this.DecodedText = "P" + decoder21.InitialPage + " " + decoder21.ProgrammeIdentificationData + " " + decoder21.StatusDisplay;
                        break;
                    case "0011":
                        this.FormatDescription += " Format 2";
                        var decoder22 = new Decode8_30_2(localline.Bytes);

                        this.DecodedText = "P" + decoder22.InitialPage + " " + decoder22.ProgrammeIdentificationData + " " + decoder22.StatusDisplay;
                        break;
                    default:
                        this.FormatDescription += " Page-related enhancement packet";
                        break;
                }
            }
            if (this.FormatDescription == "Low bit rate audio")
            {
                Byte bytDataChannel = t.Decode84HammedValue(localline.Bytes[2]);
                this.DataChannel = bytDataChannel;

            }
        }

    }
}