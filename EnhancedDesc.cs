using System;

namespace TeletextSharedResources
{
    class EnhancedDesc
    {
        public String Description;
        public double PresentationLevel;

        public EnhancedDesc(string desc, double preslevel)
        {
            Description = desc;
            PresentationLevel = preslevel;
        }
    }
}
