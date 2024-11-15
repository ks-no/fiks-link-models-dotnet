namespace KS.Fiks.Link.Models.V1.Meldingstyper
{
    public class FiksLinkMeldingtyperV1
    {
        // Forespørsler hent
        public const string HentKartutsnitt = "no.ks.fiks.link.v1.kartutsnitt.hent";
        public const string HentNaboliste = "no.ks.fiks.link.v1.naboliste.hent";
        public const string HentOmraade = "no.ks.fiks.link.v1.omraade.hent";
        public const string HentPunkt = "no.ks.fiks.link.v1.punkt.hent";

        // Resultat på forespørsler-innsyn
        public const string ResultatHentKartutsnitt = "no.ks.fiks.link.v1.kartutsnitt.hent.resultat";
        public const string ResultatHentNaboliste = "no.ks.fiks.link.v1.naboliste.hent.resultat";
        public const string ResultatHentOmraade = "no.ks.fiks.link.v1.omraade.hent.resultat";
        public const string ResultatHentPunkt = "no.ks.fiks.link.v1.punkt.hent.resultat";
        
        // Feilmeldinger
        public const string Ugyldigforespoersel = "no.ks.fiks.link.v1.feilmelding.ugyldigforespoersel";
        public const string Serverfeil = "no.ks.fiks.link.v1.feilmelding.serverfeil";
        public const string Ikkefunnet = "no.ks.fiks.link.v1.feilmelding.ikkefunnet";
        
        private static Dictionary<string, string> Skjemanavn;

        public static string GetSkjemanavn(string meldingstypeNavn)
        {
            if (Skjemanavn == null)
            {
                initSkjemanavn();
            }
            return Skjemanavn[meldingstypeNavn];
        }

        public static readonly List<string> HentTyper = new List<string>()
        {
            HentKartutsnitt,
            HentNaboliste,
            HentOmraade,
            HentPunkt,
            ResultatHentKartutsnitt,
            ResultatHentNaboliste,
            ResultatHentOmraade,
            ResultatHentPunkt
        };

        public static readonly List<string> FeilmeldingTyper = new List<string>()
        {
            Ugyldigforespoersel,
            Serverfeil,
            Ikkefunnet
        };

        public static bool IsHentType(string meldingstype)
        {
            return HentTyper.Contains(meldingstype);
        }

        public static bool IsFeilmelding(string meldingstype)
        {
            return FeilmeldingTyper.Contains(meldingstype);
        }

        public static bool IsGyldigProtokollType(string meldingstype)
        {
            return IsHentType(meldingstype) || IsFeilmelding(meldingstype);
        }

        private static void initSkjemanavn()
        {
            Skjemanavn = new Dictionary<string, string>();
            foreach (var meldingstype in HentTyper)
            {
                AddSkjemanavnTilDictionary(meldingstype);    
            }
            foreach (var meldingstype in FeilmeldingTyper)
            {
                AddSkjemanavnTilDictionary(meldingstype);    
            }
        }

        private static void AddSkjemanavnTilDictionary(string meldingstype)
        {
            Skjemanavn.Add(meldingstype, $"{meldingstype}.schema.json");
        }

    }
}
