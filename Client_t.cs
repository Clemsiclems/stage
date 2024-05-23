using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Caisse.Models
{
    public class Clients_t
    {
        public int IDClient { get; set; }
        public string cliNumGRC { get; set; }
        public string cliNom { get; set; }
        public string cliPrenom { get; set; }
        public string cliCP { get; set; }
        public string cliCommune { get; set; }
        public string cliPays { get; set; }
        public string cliIDAgence { get; set; }
        public bool cliPart { get; set; }
        public string cliTotContratIARD { get; set; }
        public string cliManuel { get; set; }
        public List<string> Sigma { get; set; }

        public List<SelectListItem> ListeCommunes { get; set; }
        public string SelectedCommune { get; set; }
    }

}