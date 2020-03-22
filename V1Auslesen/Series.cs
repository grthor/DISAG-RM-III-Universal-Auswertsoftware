using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace V1Auslesen
{
    class Series
    {
        public int Serie { get; set; }
        public Shot Schuss1 { get; set; }
        public Shot Schuss2 { get; set; }
        public Shot Schuss3 { get; set; }
        public Shot Schuss4 { get; set; }
        public Shot Schuss5 { get; set; }

        public Series()
        {
            Serie = 0;
            Schuss1 = new Shot();
            Schuss2 = new Shot();
            Schuss3 = new Shot();
            Schuss4 = new Shot();
            Schuss5 = new Shot();
        }

        public override string ToString()
        {
            return (Schuss1.Ringe + Schuss2.Ringe + Schuss3.Ringe + Schuss4.Ringe + Schuss5.Ringe).ToString();
        }

    }
}
