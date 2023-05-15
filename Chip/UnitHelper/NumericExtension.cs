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
        public static double FromMeter2(this double length)
        {
            return length * (GetConversionFactor()*GetConversionFactor());
        }
        public static double ToMeter2(this double length)
        {
            return length / (GetConversionFactor() * GetConversionFactor());
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

        public static double ToRadians(this double degree)
        {
            return (Math.PI / 180) * degree;
        }
        public static double ToDegrees(this double radians)
        {
            return (180 / Math.PI) * radians;
        }
    }
    
}
