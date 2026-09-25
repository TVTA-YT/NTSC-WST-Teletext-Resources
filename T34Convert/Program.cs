using System;
using System.IO;
using System.Text;
using TeletextSharedResources;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: dotnet run -- <capture.t34>");
            return 1;
        }

        // Get input filename
        string inputPath = args[0];

        // Read all T34 bytes, then convert to T42 bytes
        byte[] t34Data = File.ReadAllBytes(inputPath);
        byte[] t42Data = T34Converter.ToT42(t34Data);

        // On output, change file extension to T42; write all bytes to this new format
        string outputPath = Path.ChangeExtension(inputPath, ".t42");
        File.WriteAllBytes(outputPath, t42Data);

        Console.WriteLine($"Read {t34Data.Length / 34} T34 packets, wrote {t42Data.Length / 42} T42 packets to {outputPath}");
        Console.WriteLine();

        // Print every packet as text: headers start a new page.
        for (int start = 0; start + 42 <= t42Data.Length; start += 42)
        {
            // Decode magazine and row numbers
            int magazine = DecodeNibble(t42Data[start]) & 0x7;
            int row = ((DecodeNibble(t42Data[start]) >> 3) & 1) | (DecodeNibble(t42Data[start + 1]) << 1);

            // Magazine 0 converts to magazine 8
            if (magazine == 0) magazine = 8;

            // Row 0 is the page header
            if (row == 0)
            {
                // Extract 2 digits used for page number
                int units = DecodeNibble(t42Data[start + 2]);
                int tens = DecodeNibble(t42Data[start + 3]);

                // Console output
                Console.WriteLine();
                Console.WriteLine($"=== Page {magazine}{tens:X}{units:X} ===");
                Console.WriteLine($"   | {TextOf(t42Data, start + 10, 32)}"); // Header text is columns 8-39
            }
            else
            {
                // Print row number and text
                Console.WriteLine($"{row,2} | {TextOf(t42Data, start + 2, 40)}");
            }
        }
        return 0;
    }

    // Convert bytes to printable text; show control codes for colors, graphics, etc. as spaces
    static string TextOf(byte[] data, int start, int length)
    {
        var text = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            // Remove parity bit, then decide if output is printable
            int character = data[start + i] & 0x7F;
            text.Append(character < 0x20 || character == 0x7F ? ' ' : (char)character);
        }
        return text.ToString();
    }

    // Plain Hamming 8/4 data bits with no error correction since the converter already filters bad addresses
    static int DecodeNibble(byte encoded) =>
        ((encoded >> 1) & 1) | (((encoded >> 3) & 1) << 1) | (((encoded >> 5) & 1) << 2) | (((encoded >> 7) & 1) << 3);
}