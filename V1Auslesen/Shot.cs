using System;
using System.Collections.Generic;
using System.Text;

namespace V1Auslesen
{
    class Shot
    {
        public double Ringe { get; set; }
        public double Teiler { get; set; }
        public double XAbweichung { get; set; }
        public double YAbweichung { get; set; }
        public string Markierung { get; set; }

        public Shot()
        {

        }

        public Shot(int ringe)
        {
            Ringe = ringe;
        }

        public override string ToString()
        {
            return Ringe.ToString();
        }
    }
}
