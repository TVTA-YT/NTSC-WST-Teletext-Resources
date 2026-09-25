using System;
using System.Drawing;
using TeletextSharedResources;

namespace ExtensionMethods
{
    public static class MyExtensions
    {
        public static Boolean isHex(this char digit)
        {
            if (char.IsNumber(digit) || (Convert.ToInt32(digit) >= 65 && Convert.ToInt32(digit) <= 70))
                return true;
            else
                return false;
        }

        public static String toHex(this byte Integer, Int32 Length)
        {
            String outStr = Convert.ToString(Integer, 16);
            String zeroes = "";

            for (Int32 i = 0; i < Length; i++)
            {
                zeroes += "0";
            }

            outStr = zeroes.Substring(0, Length - outStr.Length) + outStr;
            return outStr;
        }

        public static String ZeroPadding(this String inVal, Int32 Length)
        {
            string inString = inVal.ToString();
            String zeroes = "";

            for (Int32 i = 0; i < Length; i++)
            {
                zeroes += "0";
            }

            return zeroes.Substring(0, Length - inString.Length) + inString;
        }

        public static Int32 ThreeDigitHexNoPosition(this Byte[] bytes)
        {
            Int32 position = 0;
            for (int start = 0; start < bytes.Length - 5; start++)
            {
                if ((bytes[start] & 127) < 0x21 && (bytes[start + 4] & 127) < 0x21)
                {
                    char hChar = Convert.ToChar(bytes[start + 1] & 127);
                    char tChar = Convert.ToChar(bytes[start + 2] & 127);
                    char uChar = Convert.ToChar(bytes[start + 3] & 127);

                    if (char.IsNumber(hChar) && tChar.isHex() && uChar.isHex())
                        position = start + 1;
                }
            }

            if (position == 0)
                System.Diagnostics.Debug.WriteLine("No page number found");

            return position;
        }

        public static String Reverse(this string s)
        {
            String outStr = "";

            for (Int32 n = s.Length - 1; n > -1; n--)
                outStr += s.Substring(n, 1);

            return outStr;

        }

        public static String ValidateMagPage(this String inPage)
        {
            String outPage = inPage;

            // if length isn't 3 then it isn't valid so return the 'invalid' page number
            if (inPage.Length != 3)
                 outPage = "8FE";

            // make sure the magazine is a number
            if (!Char.IsNumber(Convert.ToChar(inPage.Substring(0, 1))))
                outPage = "8" + inPage.Substring(1,2);

            // mod the number to make sure it's in the range 0-8
            Int32 magazine = Convert.ToInt32(inPage.Substring(0, 1));
            magazine = magazine % 8;
            outPage = magazine.ToString() + outPage.Substring(1, 2);

            // Convert 0 to 8
            if (magazine == 0)
                outPage = "8" + outPage.Substring(1, 2);

            // Check that the page number digits are both either hex or numbers 0-9
            Char pageTens = Convert.ToChar(inPage.Substring(1, 1));
            if (pageTens.isHex() || char.IsNumber(pageTens))
                outPage = inPage;
            else
                outPage = outPage.Substring(0, 1) + "F" + outPage.Substring(2, 1);

            // Check that the page number digits are both either hex or numbers 0-9
            Char pageUnits = Convert.ToChar(inPage.Substring(1, 1));
            if (pageUnits.isHex() || char.IsNumber(pageUnits))
                outPage = inPage;
            else
                outPage = outPage.Substring(0, 1) + "E" + outPage.Substring(2, 1);

            return outPage;

        }
    }
}