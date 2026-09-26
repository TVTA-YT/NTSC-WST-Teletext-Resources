using System;
using System.Collections.Generic;

namespace TeletextSharedResources
{
    /// <summary>
    /// This script converts a 34-byte NTSC (525-line) T34 WST teletext file to a 42-byte PAL (625-line) T42 WST teletext
    /// file that can be used with existing WST editors, all based on processing T42 files.
    ///
    /// In a T34 packet, there are 2 address bytes plus 32 display bites, so 34 bytes total (columns 0-31).
    /// NTSC WST services sent the remaining columns, 32-39, as separate row-extension packets. To adapt T34 streams
    /// for use with T42 editors, this involves keeping the original 34-byte packet and adding 8 additional bytes to
    /// the end for columns 32-39. These columns are filled from the row-extension packets (or spaces if no packets arrived).
    /// </summary>
    ///
    public static class T34Converter
    {
        // Packet sizes for T34 and T42 (PAL) and address byte size
        public const int T34PacketSize = 34;
        public const int T42PacketSize = 42;
        private const int AddressBytes = 2;

        // 32 columns in 525 WST, 40 in 625 WST
        private const int ColumnsPerT34Packet = 32;
        private const int ExtraColumns = 8;

        // Each extension packet covers 4 rows
        private const int RowsPerExtensionPacket = 4;

        // 8 possible magazines
        private const int MagazineCount = 8;
        private const int ExtensionMagazineOffset = 4;

        // 24 rows for display, 25 total rows
        private const int LastDisplayRow = 24;
        private const int RowsPerPage = 25;

        // Space byte
        private const byte Space = 0x20;

        // 16 possible 4-bit values
        private static readonly byte[] HammingCodeWords =
        {
            0x15, 0x02, 0x49, 0x5E, 0x64, 0x73, 0x38, 0x2F,
            0xD0, 0xC7, 0x8C, 0x9B, 0xA1, 0xB6, 0xFD, 0xEA
        };

        // Take 1 encoded byte and determine which 4-bit value it represents
        private static int DecodeHamming84(byte encodedByte, out int bitErrors)
        {
            int closestValue = -1;             // No value yet
            int closestDistance = 9;           // A byte has 8 bytes; 9 would be larger than any possible distance
            int otherValuesAtSameDistance = 0; // Tracks whether multiple values are equally close

            // 16 possible 4-bit values
            for (int value = 0; value < 16; value++)
            {
                // Calculate difference in bits between received byte and one of the known Hamming codewords
                int distance = CountSetBits(encodedByte ^ HammingCodeWords[value]);

                // If codeword is closer than anything previously seen, remember it
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestValue = value;
                    otherValuesAtSameDistance = 0;
                }

                // If equally close, increment "tie" counter; do not accept ambiguous results
                else if (distance == closestDistance)
                {
                    otherValuesAtSameDistance++;
                }
            }

            // Return error count
            bitErrors = closestDistance;

            // Determine if correctable
            bool correctable = closestDistance <= 1 && otherValuesAtSameDistance == 0;
            return correctable ? closestValue : -1;
        }

        // Count how many bits are in an integer
        private static int CountSetBits(int value)
        {
            int count = 0;

            // Check lowest bit, shift everything right by one bit
            while (value != 0)
            {
                count += value & 1;
                value >>= 1;
            };
            return count;
        }

        // Determine whether a packet number represents one of the unique extension packets
        private static bool IsRowExtensionPacketNumber(int packetNumber) =>
        packetNumber == 1 || (packetNumber >= 4 && packetNumber <= 20 && packetNumber % 4 == 0);

        // Read the first 2 bytes of a packet
        private static bool TryReadPacketAddress(byte[] data, int packetStart, out int magazine, out int rowNumber, out bool addressArrivedIntact)
        {
            // Decode first two address bytes
            int lowNibble = DecodeHamming84(data[packetStart], out int lowErrors);
            int highNibble = DecodeHamming84(data[packetStart + 1], out int highErrors);

            magazine = 0;
            rowNumber = 0;
            addressArrivedIntact = false;

            // Reject addresses that cannot be decoded
            if (lowNibble < 0 || highNibble < 0) return false;

            // Extract magazine
            magazine = lowNibble & 0x7;

            // Calculate row number
            rowNumber = ((lowNibble >> 3) & 0x1) | (highNibble << 1);

            // This is true only if both bytes arrived with no errors
            addressArrivedIntact = lowErrors == 0 && highErrors == 0;
            return true;
        }

        // Temporarily store a page
        private sealed class PageInProgress
        {
            // Store T42 packets belonging to a page
            public readonly List<byte[]> RowPackets = new List<byte[]>();
            public readonly byte[][] ExtraColumnsByRow = new byte[RowsPerPage][];
        }

        // Write collected page to final output
        private static void WritePageToOutput(PageInProgress[] pagesInProgress, int magazine, List<byte> output)
        {
            // Get page. If no page is being assembled, stop
            var page = pagesInProgress[magazine];
            if (page == null) return;

            // Process each row
            foreach (var rowPacket in page.RowPackets)
            {
                // Find row number
                TryReadPacketAddress(rowPacket, 0, out _, out int rowNumber, out _);
                    var extraColumns = page.ExtraColumnsByRow[rowNumber];

                // If extra columns exist, insert them
                if (extraColumns != null)
                {
                    Buffer.BlockCopy(extraColumns, 0, rowPacket, AddressBytes + ColumnsPerT34Packet, ExtraColumns);
                }
                output.AddRange(rowPacket);
            }
            pagesInProgress[magazine] = null;
        }

        // Convert T34 to T42
        public static byte[] ToT42(byte[] t34Data)
        {
            // Calculate packet count and create counters
            int packetCount = t34Data.Length / T34PacketSize;
            var intactHeaderCount = new int[MagazineCount];
            var extensionNumberedCount = new int[MagazineCount];
            var otherPacketCount = new int[MagazineCount];

            // ! T34 data passthrough - first pass -- figures out what stream contains
            for (int i = 0; i < packetCount; i++)
            {
                // Skip packet if address cannot be decoded
                if (!TryReadPacketAddress(t34Data, i * T34PacketSize, out int magazine, out int rowNumber, out bool intact)) continue;

                // Count intact headers
                if (rowNumber == 0)
                {
                    if (intact) intactHeaderCount[magazine]++;
                }

                // Count extension packets
                else if (IsRowExtensionPacketNumber(rowNumber))
                {
                    extensionNumberedCount[magazine]++;
                }

                // Count ordinary packets
                else
                {
                    otherPacketCount[magazine]++;
                }
            }

            var carriesRowExtensions = new bool[MagazineCount];

            // Check if magazine carries row extensions
            for (int magazine = ExtensionMagazineOffset; magazine < MagazineCount; magazine++)
            {
                // No intact headers & at least 6 extension packets & very few other packets
                carriesRowExtensions[magazine] = intactHeaderCount[magazine] == 0 && extensionNumberedCount[magazine] >= 6 && otherPacketCount[magazine] <= extensionNumberedCount[magazine] / 20;
            }

            // Store T42 packets
            var output = new List<byte>(packetCount * T42PacketSize);

            // Create page storage
            var pagesInProgress = new PageInProgress[MagazineCount];

            // ! T34 data passthrough - second pass -- conversion pass
            for (int i = 0; i < packetCount; i++)
            {
                // Get packet start
                int packetStart = i * T34PacketSize;

                // Try reading address again
                if (!TryReadPacketAddress(t34Data, packetStart, out int magazine, out int rowNumber, out bool intact)) continue;

                // If extension-carrying magazine, do not convert to T42 packet; save 8-byte extension data
                if (carriesRowExtensions[magazine])
                {
                    StoreRowExtensions(t34Data, packetStart, magazine, rowNumber, intact, pagesInProgress);
                    continue;
                }

                // Ignore rows beyond row 24
                if (rowNumber > LastDisplayRow) continue;

                // Skip rows whose address needs correcting because it may point at the wrong row or page; the headers are kept
                if (rowNumber != 0 && !intact) continue;

                // Create 42-byte packet, then copy the 34 T34 bytes into it
                var t42Packet = new byte[T42PacketSize];
                Buffer.BlockCopy(t34Data, packetStart, t42Packet, 0, T34PacketSize);

                // Fill extra bytes with spaces
                for (int column = AddressBytes + ColumnsPerT34Packet; column < T42PacketSize; column++)
                {
                    t42Packet[column] = Space;
                }

                // If row 0, write previous page, then start new page
                if (rowNumber == 0)
                {
                    WritePageToOutput(pagesInProgress, magazine, output);
                    pagesInProgress[magazine] = new PageInProgress();
                }

                // Skip rows that arrive before any header in their magazine
                if (pagesInProgress[magazine] == null) continue;

                // Save T42 packet
                pagesInProgress[magazine].RowPackets.Add(t42Packet);
            }

            // Flush pages that are still being assembled; return all converted data as a byte array
            for (int magazine = 0; magazine < MagazineCount; magazine++)
            {
                WritePageToOutput(pagesInProgress, magazine, output);
            }
            return output.ToArray();
        }

        // Take extension packet and save its 8-byte-per-row data into current page that's being assembled
        private static void StoreRowExtensions(byte[] t34Data, int packetStart, int carrierMagazine, int packetNumber, bool intact, PageInProgress[] pagesInProgress)
        {
            // Don't do anything if the address had errors or the current packet isn't an extension packet
            if (!intact || !IsRowExtensionPacketNumber(packetNumber)) return;

            // Map extension magazine to page magazine, then find page currently being built
            int pageMagazine = carrierMagazine - ExtensionMagazineOffset;
            var page = pagesInProgress[pageMagazine];

            // If corresponding page isn't being built, ignore extension data
            if (page == null) return;

            // Determine first row
            int firstRow = (packetNumber / RowsPerExtensionPacket) * RowsPerExtensionPacket;

            // Process 4 groups, each representing 1 row
            for (int group = 0; group < RowsPerExtensionPacket; group++)
            {
                // Storage for 8 bytes
                var extraColumns = new byte[ExtraColumns];

                // Calculate starting point of group's data, copy the 8 bytes, and associate the bytes with a row
                int groupStart = packetStart + AddressBytes + group * ExtraColumns;
                Buffer.BlockCopy(t34Data, groupStart, extraColumns, 0, ExtraColumns);
                page.ExtraColumnsByRow[firstRow + group] = extraColumns;
            }
        }
    }
}