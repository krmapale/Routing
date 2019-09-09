using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace reititys1t4
{
    class Program
    {
        static void Main(string[] args)
        {
            //Random rnd = new Random();
            //string iposoite = "135.46.56." + Convert.ToString(rnd.Next(1, 255));
            string iposoite = "192.168.1.105";
            string reititystaulu = @"0.0.0.0/0        192.168.100.18";

            (string ip, string net, string hop) = forwardoi(reititystaulu, iposoite);
            System.Console.WriteLine(@"Syöte: iposoite = {1} reititystaulu = {0} 
                                     palautti arvot: {2} --> {3} via {4}", 
                                     reititystaulu, iposoite, ip, net, hop);
        }

        
        /// <summary>
        /// Funktio palauttaa IP-osoitteen ja sen mihin verkkoon (kohdeverkko/maski)
        /// paketti tulee välittää sekä seuraavan laitteen osoitteen,
        /// jota kautta paketti tulee välittää kohdeverkkoon.
        /// </summary>
        /// <param name="reititystaulu">reititystaulu, string, muodossa esim.
        /// 135.46.56.0/22   192.168.0.2
        /// 135.46.56.0/21   192.168.0.6
        /// 192.53.40.0/23   192.168.0.10
        /// 0.0.0.0/0        192.168.0.14
        /// eli rivit päättyvät rivinvaihtoon ja rivillä erottimena voi olla
        /// yksi tai useampi välilyönti</param>
        /// <param name="iposoite">IP-osoite, string, jonka kohde määritetään</param>
        /// <returns>parametreista muodostetut informaatiot string:inä
        /// iposoite, string, sama mikä tuli parametrina
        /// kohdeverkko, string, kohdeverkko ja mask, muodossa x.x.x.x/y
        /// nexthop, string, reititystaulun nex_hop IP osoite
        /// </returns>
        public static (string, string, string) forwardoi(string reititystaulu,
                                        string iposoite)
        {
            string kohdeverkko = ""; // kohdeverkko/maski
            string nexthop = ""; // seuraavan laitteen osoitte

            //Jaetaan reititystaulu paloihin (rivinvaihtojen mukaan)
            string[] tauluRivit = reititystaulu.Split('\r');
            string[] maskiluvut = new string[tauluRivit.Length];
            string[] andTulokset = new string[tauluRivit.Length];
            for (int i = 0; i < tauluRivit.Length; i++)
            {
                tauluRivit[i] = tauluRivit[i].Trim();
                string[] temp = tauluRivit[i].Split(new char[] {'/', ' '});
                maskiluvut[i] = temp[1];            
            }

            int isoinMaskiluku = 0; // tallennetaan tieto silmukassa löydetystä isoimmasta maskiluvusta
            int indeksi = 0; //isoimman maskiluvun indeksi tauluRivit taulukossa
            bool b = false;

            for(int i = 0; i < tauluRivit.Length; i++)
            {
                //tallennetaan iposoitteen ja maskin AND-operaatio andTulokseen
                andTulokset[i] = andFromIPandMask(iposoite + "/" + maskiluvut[i]);
                //jaetaan taulurivi kolmeen osaan, aliverkko, maski ja next hop
                string[] tempjako = tauluRivit[i].Split(new char[] { '/', ' ' });

                string aliverkko = tempjako[0];
                if(!xorOfSubnets(aliverkko, andTulokset[i]).Contains("1")) //jos (aliverkko XOR (IP AND maski)) palauttaa pelkkiä nollia
                {
                    if (isoinMaskiluku <= Convert.ToInt32(tempjako[1]))
                    {
                        isoinMaskiluku = Convert.ToInt32(tempjako[1]); //muutetaan maski kokonaisluvuksi ja tallennetaan vertailua varten
                        indeksi = i; //tallennetaan for silmukan indeksi, jotta voidaan myöhemmin tarvittaessa löytää tauluRivistä oikea rivi
                        b = true;
                    }
                    
                }
            }
            if(b == true)
            {
                string[] tauluTmp = tauluRivit[indeksi].Split(' ');
                kohdeverkko = tauluTmp[0];
                nexthop = tauluTmp[tauluTmp.GetUpperBound(0)];
            }
            else
            {
                kohdeverkko = "0.0.0.0/0";
                nexthop = "192.168.100.18";

            }
            if (tauluRivit.Length == 1)
            {
                (iposoite, kohdeverkko, nexthop) = forwardoi(reititystaulu, iposoite);
            }

            return (iposoite, kohdeverkko, nexthop);
        }


        // ------ tehtava 2 osa A -------------------
        /// <summary>
        /// Funktio palauttaa aliverkkomaskia /x vastaaavan
        /// binääriluvun string-muodossa
        /// <param name="maski">string, joka on luku väliltä 2-30</param>
        /// <returns>binääriluku string-muodossa</returns>
        public static string binmask(string maski)
        {
            string binmaski = "";
            int intMask = 0;
            try
            {
                Int32.TryParse(maski, out intMask);
                //Maskissa on 4x8 (32) bittiä
                for (int i = 0; i < 32; i++)
                {
                    if (i <= intMask - 1)
                    {
                        binmaski = binmaski + 1;
                    }
                    else
                    {
                        binmaski = binmaski + 0;
                    }

                }
            }
            catch (FormatException e)
            {
                System.Console.WriteLine("Unable to parse " + e);
            }

            return binmaski;
        }

        /// <summary>
        /// Funktio palauttaa aliverkon, eli AND operaation tuloksen, IP-osoitteesta
        /// ja sitä vastaavasta aliverkkomaskista pistedesimaalimuodossa
        /// <param name="ipJaMaski">string, muodossa x.x.x.x/y</param>
        /// <returns>aliverkko pistedesimaalimuodossa</returns>
        public static string andFromIPandMask(string ipJaMaski)
        {

            string andIP = "";

            //erotetaan maski annetusta string-arvosta ja muutetaan se binääriseen muotoon
            string[] jono = ipJaMaski.Split('/');
            string maskiLuku = "";
            if (jono.Length > 1) maskiLuku = jono[1];
            string binmaski = binmask(maskiLuku); //binaarinen maski

            //muutetaan IP-osoite binaariseski
            char[] merkit = { '.', '/' };
            string[] tempJono = ipJaMaski.Split(merkit); //luodaan väliaikainen jono, joka sisältää kaikki merkkijonon osat
            byte[] jono2 = new byte[4]; //kopioidaan tähän jonoon vain IP-osoite tempJonosta
            for (int i = 0; i < 4; i++)
            {
                //tempJono sisältää "inttejä" string muodossa. Muutetaan tavuiksi.
                string temp = Convert.ToString(Convert.ToByte(tempJono[i]), 2);
                if (temp.Length < 8)
                {
                    temp = temp.PadLeft(8, '0');
                    jono2[i] = Convert.ToByte(temp, 2);
                }
                else jono2[i] = Convert.ToByte(temp, 2);
            }


            //jaetaan binmaski neljään osaan, jotta voidaan suorittaa AND operaatio
            byte[] splitMaski = new byte[4];
            splitMaski[0] = Convert.ToByte(binmaski.Substring(0, 8), 2);
            splitMaski[1] = Convert.ToByte(binmaski.Substring(8, 8), 2);
            splitMaski[2] = Convert.ToByte(binmaski.Substring(16, 8), 2);
            splitMaski[3] = Convert.ToByte(binmaski.Substring(24, 8), 2);

            int[] ipJono = new int[4];
            for (int i = 0; i < 4; i++)
            {

                ipJono[i] = Convert.ToInt32(splitMaski[i]) & Convert.ToInt32(jono2[i]);
                if (andIP != "") andIP = andIP + "." + ipJono[i];
                else andIP = ipJono[i].ToString();
            }

            return andIP;
        }

        /// <summary>
        /// Funktio kahden aliverkon XOR operaation tuloksen binäärilukuna string-muodossa
        /// <param name="subnet">aliverkko, muodossa x.x.x.x</param>
        /// <param name="andresult">aliverkko, tulos AND-operaatiosta, muodossa x.x.x.x</param>
        /// <returns>binääriluku string-muodossa</returns>
        public static string xorOfSubnets(string subnet, string andresult)
        {
            string result = "";
            byte[] subnetByte = new byte[4];
            byte[] andresultByte = new byte[4];

            string[] splitSubnet = subnet.Split('.');
            string[] splitAndresult = andresult.Split('.');


            for (int i = 0; i < 4; i++)
            {
                byte testi = Convert.ToByte(splitSubnet[i]);
                splitSubnet[i] = Convert.ToString(testi, 2).PadLeft(8, '0');
                subnetByte[i] = Convert.ToByte(splitSubnet[i], 2);

                int temp2 = Convert.ToInt32(splitAndresult[i]);
                splitAndresult[i] = Convert.ToString(Convert.ToByte(temp2), 2).PadLeft(8, '0');
                andresultByte[i] = Convert.ToByte(splitAndresult[i], 2);

                result = result + Convert.ToString(subnetByte[i] ^ andresultByte[i], 2).PadLeft(8, '0');
            }

            return result;
        }
    }
}
