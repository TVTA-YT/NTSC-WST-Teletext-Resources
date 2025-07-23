using System;
using System.Drawing;

namespace TeletextSharedResources
{
    public class RenderedHTML
    {
        public int Level1XStart = 50;
        public int Level1YStart = 30;


        public Boolean[] doubleHeight2ndRows = new Boolean[31];

        public Color Transparency;
        public Mode Mode = new Mode();

        public void ClearAll()
        {
        }

        public void ResetDoubleHeights()
        {
            for (int n = doubleHeight2ndRows.GetLowerBound(0); n <= doubleHeight2ndRows.GetUpperBound(0); n++)
                doubleHeight2ndRows[n] = false;
        }

        public RenderedHTML()
        {
            ClearAll();
        }


    }

}
