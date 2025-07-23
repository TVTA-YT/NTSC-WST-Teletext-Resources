using System;
using System.Collections.Generic;
using System.Linq;

namespace TeletextSharedResources
{
    public class MagazineCycleList
    {
        // An array of cycle page lists, one for each magazine
        // Each list is a list of pages of cycled (rather than timed) pages, listing them in order of transmission

        List<CycledPage> magCycleList;
        Int32 magPointer;
        public Int32 CurrentPacket;
        public Int32 Count = 0;

        public MagazineCycleList()
        {
            magCycleList = new List<CycledPage>();

            // This is an array of pointers to the current item in the magCycleList for each magazine
            //magPointer = new Int32[] { 0, 0, 0, 0, 0, 0, 0, 0 };
            magPointer = 0;
            CurrentPacket = 0;

        }



        public void AddItem(String magPage, String subPage = "")
        {
            Int32 magazine = Convert.ToInt32(magPage.Substring(0, 1));

            CycledPage cp = new CycledPage();
            cp.magPage = magPage;
            cp.page = magPage.Substring(1, 2);
            cp.subPage = subPage;

            magCycleList.Add(cp);
            Count++;
        }

        public Boolean Exists(String magPage, String subPage)
        {
            bool result = false;

            /*CycledPage cp = new CycledPage();
            cp.magPage = magPage;
            cp.page = magPage.Substring(1, 2);
            cp.subPage = "";

            if (magCycleList != null)
                result = result || magCycleList.Contains<CycledPage>(cp);*/

            foreach (CycledPage cp in magCycleList)
            {
                if (cp.magPage == magPage && cp.subPage == subPage)
                    result = true;
            }

            return result;
        }

        public String GetNext()
        {

            // Are we counting subpages in base 10 or 16?
            Int32 subPageCountBase = 10;

            // Get page currently being pointed at (i.e. the one from last time)
            String lastPage = magCycleList.ElementAt<CycledPage>(magPointer).magPage;
            //if (lastPage == "700")
            //   System.Diagnostics.Debug.WriteLine("100 found!");

            // Get the next page in sequence
            do
            {
                magPointer++;
                if (magPointer >= magCycleList.Count)
                {
                    magPointer = 0;
                }
            } while (magCycleList.ElementAt<CycledPage>(magPointer).magPage == lastPage && magCycleList.Count > 1);




            // Get the next subpage in sequence
            String lastSubpage = magCycleList.ElementAt<CycledPage>(magPointer).currentSubPage;
            if (lastSubpage == "")
                lastSubpage = magCycleList.ElementAt<CycledPage>(magPointer).subPage;

            //if (lastSubpage == "00:09" && lastPage.ToUpper() == "1DF")
            //    System.Diagnostics.Debug.Write(".");

            String nextSubPage = "";
            if (lastSubpage != "")
            {
                // Get hours and minutes of timecode
                String subpageMinutes = lastSubpage.Substring(3, 2);
                String subpageHours = lastSubpage.Substring(0, 2);

                // Increment timecode and deal with boundaries
                Int32 subpageMinutes10 = Convert.ToInt32(subpageMinutes, subPageCountBase);
                Int32 subpageHours10 = Convert.ToInt32(subpageHours, subPageCountBase);

                subpageMinutes10++;
                subpageMinutes = Convert.ToString(subpageMinutes10, subPageCountBase).PadLeft(2, Convert.ToChar("0"));
                if (subpageMinutes10 > 0x7f)
                {
                    subpageHours10++;
                    subpageMinutes10 = 0;
                    subpageMinutes = "00";
                }

                subpageHours = Convert.ToString(subpageHours10, subPageCountBase).PadLeft(2, Convert.ToChar("0"));
                if (subpageHours10 > 0x3f)
                {
                    subpageHours10 = 0;
                    subpageHours = "00";
                    System.Diagnostics.Debug.WriteLine("Timecode overflow");
                }

                // Does subpage exist?

                if (!this.Exists(this.GetCurrentPageNumber(), subpageHours + ":" + subpageMinutes))
                {
                    subpageHours = "00";
                    subpageMinutes = "00";
                    subpageHours10 = 0;
                    subpageMinutes10 = 0;

                    // Does zeroth subpage exist?  If not, increment until one is found (from 00:00 to 00:7f, anyway)
                    Boolean exit = false;
                    do
                    {
                        //System.Diagnostics.Debug.WriteLine("CurrentPageNumber: " + this.GetCurrentPageNumber());
                        if (this.Exists(this.GetCurrentPageNumber(), subpageHours + ":" + subpageMinutes))
                        {
                            exit = true;
                        }
                        else
                        {
                            subpageMinutes10++;
                            subpageMinutes = Convert.ToString(subpageMinutes10, subPageCountBase).PadLeft(2, Convert.ToChar("0"));
                        }
                    } while (!exit && subpageMinutes.ToUpper() != "7F" && subpageMinutes10 < 0xFF);

                    if (subpageMinutes.ToUpper() == "7F")
                    {
                        subpageMinutes = "00";
                        subpageMinutes10 = 0;
                    }
                }
                //else
                //{
                //    System.Diagnostics.Debug.Print("ooo, hello");
                //}


                // Set subpage
                //magCycleList.ElementAt<CycledPage>(magPointer).subPage = subpageHours + ":" + subpageMinutes;
                nextSubPage = subpageHours + ":" + subpageMinutes;
            }

            //magCycleList.ElementAt<CycledPage>(magPointer).currentSubPage = magCycleList.ElementAt<CycledPage>(magPointer).subPage;
            magCycleList.ElementAt<CycledPage>(magPointer).currentSubPage = nextSubPage;
            String result = magCycleList.ElementAt<CycledPage>(magPointer).magPage + "s" + magCycleList.ElementAt<CycledPage>(magPointer).currentSubPage;

            // if (magCycleList.ElementAt<CycledPage>(magPointer).magPage.Substring(0, 1) == "7")
            //     System.Diagnostics.Debug.WriteLine("GetNext() returned: " + result + " with magpointer: " + magPointer);

            return result;

        }

        public String GetCurrentPageNumber()
        {
            if (magCycleList.Count > 0)
                return magCycleList.ElementAt<CycledPage>(magPointer).magPage;
            else
                return "";
        }

        public String GetCurrentSubPage()
        {
            if (magCycleList.Count > 0)
                return magCycleList.ElementAt<CycledPage>(magPointer).currentSubPage;
            else
                return "";
        }

        public class CycledPage
        {
            // Page as in 00-FF as part of a magazine
            public String page;

            // Page as in 100, 2FF, etc.
            public String magPage;

            // Subpage in format 3F7F
            public String subPage;

            // Current subpage pointer
            public String currentSubPage;

            public CycledPage()
            {
                page = "";
                magPage = "";
                subPage = "";
                currentSubPage = "";
            }
        }
    }

}
