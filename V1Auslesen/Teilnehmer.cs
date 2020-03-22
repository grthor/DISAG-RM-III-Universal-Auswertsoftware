using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace V1Auslesen
{

    public enum Geschl { m = 1, w = 2 };
    class Teilnehmer
    {
        public int Startnummer { get; set; }
        public string Vorname { get; set; }
        public string Nachname { get; set; }
        public string Mannschaft { get; set; }
        public Geschl Geschlecht { get; set; }
        public MyObservableCollection<Series> Ringe { get; set; }

        public Teilnehmer()
        {
            Vorname = "";
            Nachname = "";
            Mannschaft = "";
            Ringe = new MyObservableCollection<Series>();
        }


    }
}
