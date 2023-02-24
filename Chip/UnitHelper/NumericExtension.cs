using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip.UnitHelper
{

    public static class NumericExtensions
    {
        public static double FromMeter(this double length)
        {
            return length * GetConversionFactor();
        }


        public static double GetConversionFactor()
        {

            switch (Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem)
            {
                case Rhino.UnitSystem.Meters:
                    return 1.0;

                case Rhino.UnitSystem.Millimeters:
                    return 1000.0;

                case Rhino.UnitSystem.Centimeters:
                    return 100.0;

                case Rhino.UnitSystem.Feet:
                    return 304.8 / 1000.0;

                default:
                    throw new Exception("unknown units");
            }
        }
    }
    
}
