using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace TeletextSharedResources
{

    public struct Header
    {
        public byte mrag1;
        public byte mrag2;
        public byte pu;
        public byte pt;
        public byte mu;
        public byte mt;
        public byte hu;
        public byte ht;
        public byte ca;
        public byte cb;
    }


    //public class RenderedLayers
    //{
    //    //public Bitmap L25Background = new Bitmap(480, 500, PixelFormat.Format32bppArgb);
    //    public Bitmap Background = new Bitmap(480, 500, PixelFormat.Format32bppArgb);
    //    public Bitmap Foreground = new Bitmap(480, 500, PixelFormat.Format32bppArgb);
    //    //public Bitmap Flash;
    //    //public Bitmap Conceal;
    //    //public Bitmap FlashConceal;


    //    public Boolean[] doubleHeight2ndRows = new Boolean[31];

    //    public void ResetDoubleHeights()
    //    {
    //        for (int n = doubleHeight2ndRows.GetLowerBound(0); n <= doubleHeight2ndRows.GetUpperBound(0); n++)
    //            doubleHeight2ndRows[n] = false;
    //    }

    //}





}