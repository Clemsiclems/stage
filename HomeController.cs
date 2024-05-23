using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Mail;
using System.Web.Mvc;
using Caisse.Models;
using Caisse.ViewModels;

namespace Caisse.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Validation()
        {
            return View(Get_listes_validation());
        }
        [HttpPost]
        public ActionResult Validation(List<string> valeur)
        {
            if (valeur == null) {
                return RedirectToAction("Index", "Home");
            }
            MailMessage msg = new MailMessage();
            MailAddress MailFrom = new MailAddress("GroupamaGrandEST@groupama-ge.fr", "Caisse");
            msg.From = MailFrom;
            string[] listeAdresseEmail = System.Configuration.ConfigurationManager.AppSettings["ListeAdresseEmail"].Split(',');
            foreach (var adr in listeAdresseEmail) {
                msg.To.Add(new MailAddress(adr));
            }   
            msg.Subject = "Encaissements effectués";
            msg.Body = "<table><thead><tr>";
            msg.Body += "<td style=\"text-align:center\"><b>Conseiller</b></td>";
            msg.Body += "<td style=\"text-align:center\"><b>Date de paiement</b></td>";
            msg.Body += "<td style=\"text-align:center\"><b>N° GRC</b></td>";
            msg.Body += "<td style=\"text-align:center\"><b>N° Sigma</b></td>";
            msg.Body += "<td style=\"text-align:center\"><b>N° Cpt Tiers</b></td>";
            msg.Body += "<td style=\"text-align:center\"><b>Nom prénom</b></td>";
            msg.Body += "<td style=\"text-align:center\"><b>Commune</></b></td>";
            msg.Body += "<td style=\"text-align:center\"><b>Sonia/Smart</></b></td>";
            msg.Body += "<td style=\"text-align:center\"><b>Montant Encaissé</b></td>";
            msg.Body += "</tr></thead><tbody>";
            List<Ventilation> info_to_send = Get_listes_validation();
            double total = 0;
            foreach (var info in info_to_send) {
                msg.Body += "<tr style=\"text-align:center\">";
                msg.Body += "<td style=\"text-align:center\">" + info.conseiller+"</td>";
                msg.Body += "<td style=\"text-align:center\">" + info.encDateEncaissement.Split(' ')[0]+"</td>";
                msg.Body += "<td style=\"text-align:center\">" + info.NumGrc+"</td>";
                msg.Body += "<td style=\"text-align:center\">" + info.venNumBackOffice+"</td>";
                msg.Body += "<td style=\"text-align:center\">" + info.venNumCompteTiers+"</td>";
                msg.Body += "<td style=\"text-align:center\">" + info.Nom +" "+info.Prenom+"</td>";
                msg.Body += "<td style=\"text-align:center\">" + info.Commune+"</td>";
                if (info.encSonia == "True") {
                    msg.Body += "<td style=\"text-align:center\">Oui</td>";
                }
                else {
                    msg.Body += "<td style=\"text-align:center\">Non</td>";
                }
                msg.Body += "<td style=\"text-align:center\">"+info.venMontantEncaisseEuro+"</td>";
                msg.Body += "</tr>";
                total += Convert.ToDouble(info.venMontantEncaisseEuro);
            }
            msg.Body += "</tbody></table>";
            msg.Body += "<b> TOTAL : " + total + " euros</b>";
            msg.IsBodyHtml = true;
            msg.BodyEncoding = System.Text.Encoding.UTF32;
            SmtpClient smtp = new SmtpClient();
            smtp.Send(msg);
            DataAccess db = new DataAccess();
            var ut = new Utility();
            foreach (var id in valeur) {
                var updatequery = "UPDATE Encaissements SET";
                updatequery += " [encDateValidation] = '" + DateTime.Now.ToString("yyyy-MM-dd") + "'";
                updatequery += ",[encIdConseillerValideur] = '" + ut.Conversion_id_user(User.Identity.Name) + "'";
                updatequery += " WHERE IDEncaissement = " + id;
                db.ExecuteQuery(updatequery);
            }

            return RedirectToAction("Index", "Home");
        }

        public ActionResult Liste_Passage()
        {
            return View(get_info_client());
        }
        public ActionResult Encaissements()
        {
            ClientViewModel model = get_info_client();
            model.link_cpt_tier_sigma = new Dictionary<string, List<string>>();
            DataAccess db = new DataAccess();
            foreach (var num_sigma in model.Recherche.Sigma) {
                string selectquery = "SELECT * FROM [CompteTiers] left join NumBackOffice on CompteTiers.cptNumBackOffice = NumBackOffice.nboNumBackOffice " +
                                  "left join TypeCompteTiers on CompteTiers.cptIdTypeCpteTiers = TypeCompteTiers.IDTypeCompteTiers " +
                                  "where cptNumBackOffice = " + num_sigma;
                var result = db.ExecuteQuery(selectquery);
                List<string> all_cpt_tiers = new List<string>();
                for (int i = 0; i < result.Rows.Count; i++) {

                    string cpt_tiers = Convert.ToString(result.Rows[i]["cptNumCompteTiers"]);
                    cpt_tiers += " / " + Convert.ToString(result.Rows[i]["IDTypeCompteTiers"]);
                    cpt_tiers += "-" + Convert.ToString(result.Rows[i]["LibTypeCompteTiers"]);
                    all_cpt_tiers.Add(cpt_tiers);
                }
                model.link_cpt_tier_sigma.Add(num_sigma, all_cpt_tiers);
            }
            return View(model);
        }
        [HttpPost]
        public ActionResult Encaissements(ClientViewModel cl, List<string> valeurC1, List<string> valeurC2, List<string> valeurC3)
        {
            if (ModelState.IsValid) {
                cl.Recherche = get_info_client().Recherche;
                var ut = new Utility();
                DataAccess db = new DataAccess();
                var insertquery = "INSERT INTO [dbo].[Encaissements] ([encIDClient] " +
               ",[encNumGRC],[encMontantDuEuro],[encMontantEncaisseEuro],[encMontantARendreEuro],[encDateEncaissement]" +
               ",[encDateValidation],[encModePaiement],[encCommentaires],[encIDConseiller],[encIDAgence],[encRemise],[encIDBanque]" +
               ",[encImprimerEncaissement],[encSonia],[encEstParticulier],[encIdConseillerValideur],[encMontantCot],[encClientManuel]" +
               ",[encAnnule],[encIdConseillerAnnuleur],[encNumTansactionPayBox],[encEchecPayBox],[encDateExtrait],[bug],[encNumMandat],[encNumRemise])";
                insertquery += "VALUES (";
                insertquery += "'" + cl.Recherche.IDClient + "',";
                insertquery += "'" + cl.Recherche.cliNumGRC + "',";
                insertquery += cl.montant_du + ",";
                insertquery += cl.montant_encaisse + ",";
                insertquery += cl.montant_a_rendre + ",";
                insertquery += "'" + cl.date_de_passage + "',";
                insertquery += "NULL,";
                insertquery += "3,";
                insertquery += "NULL,";
                insertquery += "'" + ut.Conversion_id_user(User.Identity.Name) + "',";
                insertquery += "'" + ut.Get_Agence() + "',";
                insertquery += "NULL,";
                insertquery += "NULL,";
                insertquery += "0,";
                insertquery += Convert.ToInt32(cl.encSonia) + ",";
                insertquery += Convert.ToInt32(Convert.ToBoolean(cl.Recherche.cliPart)) + ",";
                insertquery += "NULL,";
                insertquery += "NULL,";
                insertquery += Convert.ToInt32(Convert.ToBoolean(cl.Recherche.cliManuel)) + ",";
                insertquery += "0,NULL,NULL,NULL,NULL,NULL,NULL,NULL);";
                insertquery += "SELECT SCOPE_IDENTITY() as id";
                var result = db.ExecuteQuery(insertquery);
                string id = Convert.ToString(result.Rows[0]["id"]);
                for (int i = 0; i < valeurC1.Count; i++) {
                    insertquery = "INSERT INTO [dbo].[Ventilations] ([venIDEncaissement],[venIDBackOffice]" +
                                  ",[venNumCompteTiers],[venMontantEncaisseEuro] ,[venNumBackOffice] ,[venCompta]" +
                                  ",[venImprimerCompta]) ";
                    insertquery += "VALUES (";
                    insertquery += id + ",";
                    insertquery += "'S',";
                    insertquery += "'" + valeurC1[i] + "',";
                    insertquery += "'" + valeurC3[i] + "',";
                    insertquery += "'" + valeurC2[i] + "',";
                    insertquery += "0,0)";
                    db.ExecuteQuery(insertquery);
                };
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        /* début liste communes */

        [HttpGet]
        public ActionResult Index()
        {
            var selectquery = "SELECT CONCAT(comCP,' - ',comCommune) as commune FROM[CAISSE].[dbo].[Communes] order by comCommune";
            DataAccess db = new DataAccess();
            var result = db.ExecuteQuery(selectquery);
            //var all_commune = "";
            //for (int i = 0; i < result.Rows.Count; i++) {
            //all_commune += Convert.ToString(result.Rows[i]["commune"]) + "/";
            //}
             
            var all_communes = new List<SelectListItem>();
            all_communes.Insert(0, new SelectListItem { Text = "", Value = "" });
            foreach (DataRow r in result.Rows)
            {
                all_communes.Add(new SelectListItem { Text = Convert.ToString(r["commune"]), Value = Convert.ToString(r["commune"]), Selected = false });
            }

            ClientViewModel clientViewModel = new ClientViewModel();
            clientViewModel.Recherche = new Clients_t();
            //clientViewModel.Recherche.SelectedCommune = all_commune;

            clientViewModel.Recherche.ListeCommunes = all_communes;

            clientViewModel.Recherche.Sigma = new List<string> { "" };

            return View(clientViewModel);
        }
        [HttpPost]
        public ActionResult Index(ClientViewModel cl)
        {
            if (ModelState.IsValid) {
                List<string> dates = new List<string>();
                dates.Add(cl.Date_debut);
                dates.Add(cl.Date_fin);
                List<Clients_t> model = Select_clients(cl.Recherche, cl.Date_active, dates);

                ClientViewModel new_model = new ClientViewModel {

                    Recherche = cl.Recherche,
                    Clients_aff = model,
                    Date_debut = cl.Date_debut,
                    Date_fin = cl.Date_fin
                };

                var selectquery = "SELECT CONCAT(comCP,' - ',comCommune) as commune FROM[CAISSE].[dbo].[Communes] order by comCommune";
                DataAccess db = new DataAccess();
                var result = db.ExecuteQuery(selectquery);
                //var all_commune = "";
                //for (int i = 0; i < result.Rows.Count; i++) {
                //all_commune += Convert.ToString(result.Rows[i]["commune"]) + "/";
                //}

                var all_communes = new List<SelectListItem>();
                all_communes.Insert(0, new SelectListItem { Text = "", Value = "" });
                foreach (DataRow r in result.Rows)
                {
                    all_communes.Add(new SelectListItem { Text = Convert.ToString(r["commune"]), Value = Convert.ToString(r["commune"]), Selected = false });
                }
                new_model.Recherche.ListeCommunes = all_communes;
                return View(new_model);
            }
            return View();

        }   /* fin liste communes */

        /* chargement liste client */

        public List<Clients_t> Select_clients(Clients_t cl, bool date_active, List<string> dates)
        {
            DataAccess db = new DataAccess();
            string selectquery = "SELECT * FROM Clients ";
            selectquery += "left join NumBackOffice as cl1 on Clients.IDClient = cl1.nboIDClient ";
            selectquery += "left join NumBackOffice as cl2 on Clients.IDClient = cl2.nboIDClient ";
            selectquery += "left join Encaissements on  Clients.cliNumGRC = Encaissements.encNumGRC";
            selectquery += " WHERE 1 =1 ";
            selectquery += "AND cliNumGRC like '" + cl.cliNumGRC + "%' ";
            selectquery += "AND cliNom like '" + cl.cliNom + "%' ";
            selectquery += "AND CLIPrenom like '" + cl.cliPrenom + "%' ";
            //selectquery += "AND cliCP like '" + cl.cliCP + "%' ";
            selectquery += "AND concat(trim(clicp),' - ',cliCommune) like '" + cl.SelectedCommune + "%' ";
            if (cl.Sigma[0] != "") {
                selectquery += "AND cl1.nboNumBackOffice like '" + cl.Sigma[0] + "%'  and cl1.nboIDBackOffice ='S' ";
            }
            if (date_active) {
                selectquery += "AND (encDateEncaissement >= '" + dates[0] + "' and encDateEncaissement <= '" + dates[1] + "')";
            }
            selectquery += " ORDER BY cliNom,cliPrenom,cliCP";
            var result = db.ExecuteQuery(selectquery);


            Utility ut = new Utility();
            List<Clients_t> model = ut.conversion(result);
            model = ut.add_Sigma_vie(model);
            return model;
        }

        /* fin chargement */

        private List<Passage> GetPassages(string condition)
        {
            DataAccess db = new DataAccess();
            var command = "Select "
                + "Encaissements.encDateEncaissement as [Date de paiement],"
                + "ModePaiements.modLibelle as [Mode de paiement],"
                + "Encaissements.encMontantDuEuro as Montant,"
                + "Agences.AgeAgence as [Agence],"
                + "ut2.utiNom as valideur,"
                + "Encaissements.encIDConseiller as IDUtilisateur,"
                + "Encaissements.encDateValidation as Validé,"
                + "ut.utiNom as Réalisateur,"
                + "NumBackOffice.nboIDBackOffice as [Sigma/vie],"
                + "NumBackOffice.nboNumBackOffice as [valeurSV],"
                + "ut3.utiNom as [Annulé par],"
                + " Encaissements.encAnnule as [est annulé],"
                + "Clients.IDClient, "
                + "Encaissements.IDEncaissement "
                + "FROM [CAISSE].[dbo].[Encaissements]"
                + "left join Clients on Encaissements.encNumGRC = Clients.cliNumGRC "
                + "left join ModePaiements on Encaissements.encModePaiement = ModePaiements.IDPaiement "
                + "left join Agences on Encaissements.encIDAgence = Agences.IDAgence "
                + "left join Utilisateurs as ut on Encaissements.encIDConseiller = ut.IDUtilisateur "
                + "left join Utilisateurs as ut2 on Encaissements.encIdConseillerValideur = ut2.IDUtilisateur "
                + "left join Utilisateurs as ut3 on Encaissements.encIdConseillerAnnuleur = ut3.IDUtilisateur "
                + "left join NumBackOffice on Clients.IDClient = NumBackOffice.[nboIDClient] "
                + condition
                + " order by Encaissements.encDateEncaissement desc,Encaissements.encMontantDuEuro desc,ut.utiNom ";
            var result = db.ExecuteQuery(command);
            Utility ut = new Utility();
            return ut.conversion_passage(result);
        }
        private ClientViewModel get_info_client()
        {
            string path = Request.Url.LocalPath;
            string[] segment = path.Split('/');
            string id = segment[segment.Length - 1];

            DataAccess db = new DataAccess();
            var result = db.ExecuteQuery("Select * FROM Clients where IDClient=" + id);
            Utility ut = new Utility();
            List<Clients_t> conv = ut.conversion(result);
            conv = ut.add_Sigma_vie(conv);
            ClientViewModel new_model = new ClientViewModel {
                Recherche = conv[0],
                Passages = GetPassages("Where Clients.IdClient =" + id)
            };
            return new_model;
        }
        private List<Ventilation> Get_listes_validation()
        {
            DataAccess db = new DataAccess();
            var selectquery = "SELECT utiNom,encDateEncaissement,encNumGRC,venNumBackOffice,venNumCompteTiers,cliNom,cliPrenom,SelectedCommune,venMontantEncaisseEuro,IDEncaissement,encSonia " +
                               "FROM [CAISSE].[dbo].[Ventilations]" +
                               " left join [dbo].[Encaissements] on venIDEncaissement = Encaissements.IDEncaissement " +
                               " left join [dbo].[Utilisateurs] on encIDConseiller = Utilisateurs.IDUtilisateur" +
                               " left join [dbo].[Clients] on encNumGRC = Clients.cliNumGRC" +
                               " where Year(encDateEncaissement) +1 >= " + DateTime.Now.Year + " and encAnnule = 0 and encIdConseillerValideur is NULL";
            selectquery += " order by encDateEncaissement";
            var result = db.ExecuteQuery(selectquery);
            List<Ventilation> model = new List<Ventilation>();
            for (int i = 0; i < result.Rows.Count; i++) {
                Ventilation ventilation = new Ventilation();
                ventilation.venNumCompteTiers = Convert.ToString(result.Rows[i]["venNumCompteTiers"]);
                ventilation.venMontantEncaisseEuro = Convert.ToString(result.Rows[i]["venMontantEncaisseEuro"]);
                ventilation.venNumBackOffice = Convert.ToString(result.Rows[i]["venNumBackOffice"]);
                ventilation.encDateEncaissement = Convert.ToString(result.Rows[i]["encDateEncaissement"]);
                ventilation.encSonia = Convert.ToString(result.Rows[i]["encSonia"]);

                ventilation.conseiller = Convert.ToString(result.Rows[i]["utiNom"]);
                ventilation.NumGrc = Convert.ToString(result.Rows[i]["encNumGRC"]);
                ventilation.Nom = Convert.ToString(result.Rows[i]["cliNom"]);
                ventilation.Prenom = Convert.ToString(result.Rows[i]["cliPrenom"]);
                ventilation.Commune = Convert.ToString(result.Rows[i]["SelectedCommune"]);
                ventilation.IdEncaissement = Convert.ToString(result.Rows[i]["IDEncaissement"]);
                model.Add(ventilation);
            }
            return model;
        }
    }
}