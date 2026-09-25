using System;

namespace TeletextSharedResources
{
    public class SubPageMap
    {
        public Int32 Key;
        public Int32 Row;
        public Int32 AlsoInKey;
        public Int32 SubPage;

        public SubPageMap(Int32 key, Int32 row, Int32 alsoInKey, Int32 subPage)
        {
            Key = key;
            Row = row;
            AlsoInKey = alsoInKey;
            SubPage = subPage;
        }

    }

}
