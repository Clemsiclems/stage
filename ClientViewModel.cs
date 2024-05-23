using System.Collections.Generic;
using Caisse.Models;

namespace Caisse.ViewModels
{
    public class ClientViewModel
    {
        //recherche
        public List<Clients_t> Clients_aff { get; set; }

        public Clients_t Recherche { get; set; }
        public bool Date_active { get; set; }
        public string Date_debut { get; set; }
        public string Date_fin { get; set; }

        //liste passage
        public List<Passage> Passages { get; set; }


        //encaissement
        public string montant_du { get; set; }
        public string montant_encaisse { get; set; }
        public string montant_a_rendre { get; set; }
        public string date_de_passage { get; set; }
        public Dictionary<string, List<string>> link_cpt_tier_sigma { get; set; }
        public bool encSonia { get; set; }
    }
}