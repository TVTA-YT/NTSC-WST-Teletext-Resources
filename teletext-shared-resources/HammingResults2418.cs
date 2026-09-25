using System;
using ExtensionMethods;
using System.Linq;

namespace TeletextSharedResources
{
    public class HammingResults2418
    {
        public String Address, Mode, Data, Bits;
        public String Result, ErrorString;
        public Int32 RowColumn;
        public Boolean FatalError;
        public HammingResults2418 HammingCheck2418(byte byte1, byte byte2, byte byte3)
        {
            //Convert bytes to binary strings
            string bin1 = Convert.ToString(byte1, 2).PadLeft(8, Convert.ToChar("0"));
            string bin2 = Convert.ToString(byte2, 2).PadLeft(8, Convert.ToChar("0"));
            string bin3 = Convert.ToString(byte3, 2).PadLeft(8, Convert.ToChar("0"));

            //string binRev1 = "", binRev2 = "", binRev3 = "";
            string hammingStatus = "OK";

            // reverse strings as spec uses LSB first
            bin1 = bin1.Reverse();
            bin2 = bin2.Reverse();
            bin3 = bin3.Reverse();

            // apply Hamming error detection - let's see where the errors are
            // note: Parity checks A, B, C, D and F are the same for all bytes

            //Byte 1
            Boolean parityA1, parityB1, parityC1, parityD1, parityE1, parityF1;

            String binA1 = Convert.ToString(byte1 & Convert.ToByte("10101010", 2), 2);
            parityA1 = (binA1.Replace("0", "").Length % 2 == 0);

            String binB1 = Convert.ToString(byte1 & Convert.ToByte("01100110", 2), 2);
            parityB1 = (binB1.Replace("0", "").Length % 2 == 0);

            String binC1 = Convert.ToString(byte1 & Convert.ToByte("00011110", 2), 2);
            parityC1 = (binC1.Replace("0", "").Length % 2 == 0);

            String binD1 = Convert.ToString(byte1 & Convert.ToByte("00000001", 2), 2);
            parityD1 = (binD1.Replace("0", "").Length % 2 == 0);

            String binE1 = Convert.ToString(byte1 & Convert.ToByte("00000000", 2), 2);
            parityE1 = (binE1.Replace("0", "").Length % 2 == 0);

            parityF1 = (Convert.ToString(byte1, 2).Replace("0", "").Length % 2 == 0);

            //Byte 2
            Boolean parityA2, parityB2, parityC2, parityD2, parityE2, parityF2;

            String binA2 = Convert.ToString(byte2 & Convert.ToByte("10101010", 2), 2);
            parityA2 = (binA2.Replace("0", "").Length % 2 == 0);

            String binB2 = Convert.ToString(byte2 & Convert.ToByte("01100110", 2), 2);
            parityB2 = (binB2.Replace("0", "").Length % 2 == 0);

            String binC2 = Convert.ToString(byte2 & Convert.ToByte("00011110", 2), 2);
            parityC2 = (binC2.Replace("0", "").Length % 2 == 0);

            String binD2 = Convert.ToString(byte2 & Convert.ToByte("11111110", 2), 2);
            parityD2 = (binD2.Replace("0", "").Length % 2 == 0);

            String binE2 = Convert.ToString(byte2 & Convert.ToByte("00000001", 2), 2);
            parityE2 = (binE2.Replace("0", "").Length % 2 == 0);

            parityF2 = (Convert.ToString(byte2, 2).Replace("0", "").Length % 2 == 0);

            //Byte 3
            Boolean parityA3, parityB3, parityC3, parityD3, parityE3, parityF3;

            String binA3 = Convert.ToString(byte3 & Convert.ToByte("10101010", 2), 2);
            parityA3 = (binA3.Replace("0", "").Length % 2 == 0);

            String binB3 = Convert.ToString(byte3 & Convert.ToByte("01100110", 2), 2);
            parityB3 = (binB3.Replace("0", "").Length % 2 == 0);

            String binC3 = Convert.ToString(byte3 & Convert.ToByte("00011110", 2), 2);
            parityC3 = (binC3.Replace("0", "").Length % 2 == 0);

            String binD3 = Convert.ToString(byte3 & Convert.ToByte("00000000", 2), 2);
            parityD3 = (binD3.Replace("0", "").Length % 2 == 0);

            String binE3 = Convert.ToString(byte3 & Convert.ToByte("11111110", 2), 2);
            parityE3 = (binE3.Replace("0", "").Length % 2 == 0);

            parityF3 = (Convert.ToString(byte1, 2).Replace("0", "").Length % 2 == 0);


            HammingResults2418 hr = new HammingResults2418();
            hr.FatalError = false;

            if (parityA1 && parityB1 && parityC1 && parityD1 && parityE1 &&
                parityA2 && parityB2 && parityC2 && parityD2 && parityE2 &&
                parityA3 && parityB3 && parityC3 && parityD3 && parityE3 &&
                (!parityF1 || !parityF2 || !parityF3)
                )
                hammingStatus = "Error in P6, but accept data";

            if (parityF1 && parityF2 && parityF3 &&
                (
                !parityA1 || !parityB1 || !parityC1 || !parityD1 || !parityE1 ||
                !parityA2 || !parityB2 || !parityC2 || !parityD2 || !parityE2 ||
                !parityA3 || !parityB3 || !parityC3 || !parityD3 || !parityE3)
                )
            {
                hammingStatus = "Double error, reject data bits";
                hr.FatalError = true;
            }

            if (
                (!parityF1 || !parityF2 || !parityF3) &&
                (
                !parityA1 || !parityB1 || !parityC1 || !parityD1 || !parityE1 ||
                !parityA2 || !parityB2 || !parityC2 || !parityD2 || !parityE2 ||
                !parityA3 || !parityB3 || !parityC3 || !parityD3 || !parityE3)
                )
            {
                hammingStatus = "Single Error, ";

                //Get results of tests across all three bytes
                Boolean resultA = ((binA1 + binA2 + binA3).Replace("0", "").Length % 2 == 0);
                Boolean resultB = ((binB1 + binB2 + binB3).Replace("0", "").Length % 2 == 0);
                Boolean resultC = ((binC1 + binC2 + binC3).Replace("0", "").Length % 2 == 0);
                Boolean resultD = ((binD1 + binD2 + binD3).Replace("0", "").Length % 2 == 0);
                Boolean resultE = ((binE1 + binE2 + binE3).Replace("0", "").Length % 2 == 0);

                Int32 errorPosition = 2 ^ 4 * (resultE == true ? 0 : 1) + 2 ^ 3 * (resultD == true ? 0 : 1) + 2 ^ 2 * (resultC == true ? 0 : 1) + 2 ^ 1 * (resultB == true ? 0 : 1) + 2 ^ 0 * (resultA == true ? 0 : 1);
                hammingStatus += "Error Position: " + errorPosition.ToString();
                if (errorPosition == 3 || errorPosition == 5 || errorPosition == 6 || errorPosition == 7 || (errorPosition >= 9 && errorPosition <= 15) || (errorPosition >= 17 && errorPosition <= 23))
                {
                    hammingStatus += " - this is in a data bit.";
                }

            }



            //Convert back to MSB first.  EBU TTX document has them this way round in section 12.3.1.
            String binRev1 = (Convert.ToString(byte1, 2).PadLeft(8, Convert.ToChar("0"))).Reverse();
            String binRev2 = (Convert.ToString(byte2, 2).PadLeft(8, Convert.ToChar("0"))).Reverse();
            String binRev3 = (Convert.ToString(byte3, 2).PadLeft(8, Convert.ToChar("0"))).Reverse();

            // Or, in reverse:
            /*String binRev1 = Reverse(Convert.ToString(byte1, 2).PadLeft(8, Convert.ToChar("0")));
            String binRev2 = Reverse(Convert.ToString(byte2, 2).PadLeft(8, Convert.ToChar("0")));
            String binRev3 = Reverse(Convert.ToString(byte3, 2).PadLeft(8, Convert.ToChar("0")));*/



            String binDataBitsRev = binRev1.Substring(2, 1) + binRev1.Substring(4, 3);
            binDataBitsRev += binRev2.Substring(0, 7);
            binDataBitsRev += binRev3.Substring(0, 7);

            hr.Bits = binDataBitsRev;

            hr.Address = binDataBitsRev.Substring(0, 6).Reverse();
            hr.Mode = binDataBitsRev.Substring(6, 5).Reverse();
            hr.Data = binDataBitsRev.Substring(11, 7).Reverse();


            Int32 decAddr = Convert.ToInt32(hr.Address, 2);
            String addrType = decAddr < 40 ? "Col: " + decAddr.ToString() : "Row: " + (decAddr - 40).ToString();
            if (decAddr == 40)
                addrType = "Row 24";

            hr.RowColumn = decAddr < 40 ? decAddr : (decAddr - 40);
            if (decAddr == 40)
                hr.RowColumn = 24;


            hr.Result = "Addr: " + hr.Address + "(" + decAddr + ") " + addrType + ", Mode: " + hr.Mode + ", Data: " + hr.Data;
            hr.ErrorString = hammingStatus;
            return hr;
        }
        public HammingResults2418 HammingCheck2418a(byte byte1, byte byte2, byte byte3)
        {
            //Adapted from http://pdc.ro.nu/hamming.html

            //Convert bytes to binary strings
            //Convert bytes to binary strings
            string bin1 = Convert.ToString(byte1, 2).PadLeft(8, Convert.ToChar("0"));
            string bin2 = Convert.ToString(byte2, 2).PadLeft(8, Convert.ToChar("0"));
            string bin3 = Convert.ToString(byte3, 2).PadLeft(8, Convert.ToChar("0"));

            string hammingStatus = "OK";

            Boolean[] h = new Boolean[24];
            //Load Byte 1 into array
            for (Int32 n = 0; n < 8; n++)
            {
                h[n] = bin1.Substring(7 - n, 1) == "1" ? true : false;
            }
            //Load Byte 2 into array
            for (Int32 n = 0; n < 8; n++)
            {
                h[n + 8] = bin2.Substring(7 - n, 1) == "1" ? true : false;
            }
            //Load Byte 3 into array
            for (Int32 n = 0; n < 8; n++)
            {
                h[n + 16] = bin3.Substring(7 - n, 1) == "1" ? true : false;
            }

            //Parity Tests
            Boolean p = h[23];
            for (Int32 n = 22; n > 0; n--)
                p = p ^ h[n];

            Boolean c0 = h[0] ^ h[2] ^ h[4] ^ h[6] ^ h[8] ^ h[10] ^ h[12] ^ h[14] ^ h[16] ^ h[18] ^ h[20] ^ h[22];
            Boolean c1 = h[1] ^ h[2] ^ h[5] ^ h[6] ^ h[9] ^ h[10] ^ h[13] ^ h[14] ^ h[17] ^ h[18] ^ h[21] ^ h[22];
            Boolean c2 = h[3] ^ h[4] ^ h[5] ^ h[6] ^ h[11] ^ h[12] ^ h[13] ^ h[14] ^ h[19] ^ h[20] ^ h[21] ^ h[22];
            Boolean c3 = h[7] ^ h[8] ^ h[9] ^ h[10] ^ h[11] ^ h[12] ^ h[13] ^ h[14];
            Boolean c4 = h[15] ^ h[16] ^ h[17] ^ h[18] ^ h[19] ^ h[20] ^ h[21] ^ h[22];

            if (p && c0 && c1 && c2 && c3 && c4)
            {
                hammingStatus = "OK";
            }

            if (!p && (!c0 || !c1 || !c2 || !c3 || !c4))
            {
                hammingStatus = "Damaged beyond repair.";
            }

            if (!p && c0 && c1 && c2 && c3 && c4)
            {
                Int32 bitInError = 0;
                if (!c0)
                    bitInError += 1;
                if (!c1)
                    bitInError += 2;
                if (!c2)
                    bitInError += 4;
                if (!c3)
                    bitInError += 8;
                if (!c4)
                    bitInError += 16;

                hammingStatus = "Single bit error in bit " + bitInError.ToString() + ", corrected.  ";
                h[bitInError] = !h[bitInError];
            }

            String dataBits = "";
            dataBits += h[22] == true ? "1" : "0";
            dataBits += h[21] == true ? "1" : "0";
            dataBits += h[20] == true ? "1" : "0";
            dataBits += h[19] == true ? "1" : "0";
            dataBits += h[18] == true ? "1" : "0";
            dataBits += h[17] == true ? "1" : "0";
            dataBits += h[16] == true ? "1" : "0";
            dataBits += h[14] == true ? "1" : "0";
            dataBits += h[13] == true ? "1" : "0";
            dataBits += h[12] == true ? "1" : "0";
            dataBits += h[11] == true ? "1" : "0";
            dataBits += h[10] == true ? "1" : "0";
            dataBits += h[9] == true ? "1" : "0";
            dataBits += h[8] == true ? "1" : "0";
            dataBits += h[6] == true ? "1" : "0";
            dataBits += h[5] == true ? "1" : "0";
            dataBits += h[4] == true ? "1" : "0";
            dataBits += h[2] == true ? "1" : "0";


            HammingResults2418 hr = new HammingResults2418();
            hr.Bits = dataBits;

            hr.Data = dataBits.Substring(0, 7);

            hr.Mode = dataBits.Substring(7, 5);

            hr.Address = dataBits.Substring(12, 6);

            Int32 decAddr = Convert.ToInt32(hr.Address, 2);
            String addrType = decAddr < 40 ? "Col: " + decAddr.ToString() : "Row: " + (decAddr - 40).ToString();
            if (decAddr == 40)
                addrType = "Row 24";

            hr.RowColumn = decAddr < 40 ? decAddr : (decAddr - 40);
            if (decAddr == 40)
                hr.RowColumn = 24;

            hr.Result = "Addr: " + hr.Address + "(" + decAddr + ") " + addrType + ", Mode: " + hr.Mode + ", Data: " + hr.Data;
            hr.ErrorString = hammingStatus;

            System.Diagnostics.Debug.Print("Hamming Status: " + hammingStatus);

            return hr;
        }


    }


}