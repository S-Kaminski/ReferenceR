using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ReferenceR
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public string JsonFile { get; set; }
        public string MapFile { get; set; }
        public string JPGWPath { get; set; }
        public JsonClass JsonContent { get; set; }
        public List<string> sBounds { get; set; }
        public List<double> Bounds { get; set; }
        public string StructureValue { get; set; }

        public double BoundX { get; set; }
        public double BoundY { get; set; }

        public string Xpuwg { get; set; }
        public string Ypuwg { get; set; }


        public MainWindow()
        {
            InitializeComponent();
        }

        private string StructureValues(string value)
        {
            string s = String.Empty;
            switch (value)
            {
                case "Pulkovo 1995 / Gauss-Kruger zone 4":
                    s = "0.38205833372368875";
                    break;
                case "PUWG 92":
                    s = "0.3820583337230174";
                    break;
                case "WGS 84":
                    s = "0.00000778733705265";
                    break;
                default:
                    s = "0";
                    break;
            }
            return s;
        } //return value of chosen structure

        private void AppendLog(string s)
        {
            LOGS.AppendText(s + "\n");
            LOGS.ScrollToEnd();
        }

        private void AssignValuesFromJson(string jsonPath)
        {
            string json = File.ReadAllText(jsonPath);
            JsonClass jc = JsonConvert.DeserializeObject<JsonClass>(json);
            //JsonContent.MaxZoom = jc.MaxZoom;
            //JsonContent.MinZoom = jc.MinZoom;
            //JsonContent.Bounds = jc.Bounds;
            //JsonContent.Name = jc.Name;
            //JsonContent.Format = jc.Format;
            sBounds = jc.Bounds.Split(',').ToList<string>();

            BoundX = Convert.ToDouble(sBounds[3], CultureInfo.InvariantCulture);
            BoundY = Convert.ToDouble(sBounds[0], CultureInfo.InvariantCulture);
        }

        private void JPGWFile(string mapFile)
        {
            FileInfo fi = new FileInfo(mapFile);
            if (fi.Exists)
            {
                string temp = fi.DirectoryName + "\\";
                string fileName = ((fi.Name).ToString()).Replace(fi.Extension, "");
                JPGWPath = temp + fileName + ".jgw";
            }
        }

        private int wgs84_to_pugw92(double B_stopnie, double L_stopnie) /*, double *Xpuwg, double *Ypuwg*/
        {
            double e = 0.0818191910428;     //pierwszymimo¶ród elipsoidy
            double R0 = 6367449.14577;      //promień sfery Lagrange.a
            double Snorm = 2.0E-6;          //parametr normuj±cy
            double xo = 5760000.0;

            //Wspolczynniki wielomianu
            double a0 = 5765181.11148097;
            double a1 = 499800.81713800;
            double a2 = -63.81145283;
            double a3 = 0.83537915;
            double a4 = 0.13046891;
            double a5 = -0.00111138;
            double a6 = -0.00010504;

            // Parametry odwzorowania Gaussa-Kruegera dla układu PUWG92
            double L0_stopnie = 19.0;       //Pocz±tek układu wsp. PUWG92 (długo¶ć)
            double m0 = 0.9993;
            double x0 = -5300000.0;
            double y0 = 500000.0;

            // Zakres stosowalnosci metody
            double Bmin = 48.0 * Math.PI / 180.0;
            double Bmax = 56.0 * Math.PI / 180.0;
            double dLmin = -6.0 * Math.PI / 180.0;
            double dLmax = 6.0 * Math.PI / 180.0;

            // Weryfikacja danych wejsciowych
            double B = B_stopnie * Math.PI / 180.0;
            double dL_stopnie = L_stopnie - L0_stopnie;
            double dL = dL_stopnie * Math.PI / 180.0;

            if ((B < Bmin) || (B > Bmax))
                return 1;

            if ((dL < dLmin) || (dL > dLmax))
                return 2;

            //etap I - elipsoida na kulę
            double U = 1.0 - e * Math.Sin(B);
            double V = 1.0 + e * Math.Sin(B);
            double K = Math.Pow((U / V), (e / 2.0));
            double C = K * Math.Tan(B / 2.0 + Math.PI / 4.0);
            double fi = 2.0 * Math.Atan(C) - Math.PI / 2.0;
            double d_lambda = dL;

            // etap II - kula na walec
            double p = Math.Sin(fi);
            double q = Math.Cos(fi) * Math.Cos(d_lambda);
            double r = 1.0 + Math.Cos(fi) * Math.Sin(d_lambda);
            double s = 1.0 - Math.Cos(fi) * Math.Sin(d_lambda);
            double XMERC = R0 * Math.Atan(p / q);
            double YMERC = 0.5 * R0 * Math.Log(r / s);

            //etap III - walec na płaszczyznę
            //double Z = ((XMERC-xo)*Snorm + YMERC* Snorm);
            Complex Z = new Complex((XMERC - xo) * Snorm, YMERC * Snorm);
            Complex Zgk;

            Zgk = a0 + Z * (a1 + Z * (a2 + Z * (a3 + Z * (a4 + Z * (a5 + Z * a6)))));
            double Xgk = Zgk.Real;
            double Ygk = Zgk.Imaginary;
            //Przej¶cie do układu aplikacyjnego
            Xpuwg = (m0 * Xgk + x0).ToString();
            Ypuwg = (m0 * Ygk + y0).ToString();

            return 0;
        }

        private void CreateWorldFile()
        {
            using (StreamWriter sw = new StreamWriter(JPGWPath))
            {
                if (wgs84_to_pugw92(BoundX, BoundY) != 0)
                {
                    AppendLog("Dane wykroczyły poza zakres");
                }
                else
                {
                    AppendLog("Przeliczanie z układu WGS84 na PUWG92...");
                    wgs84_to_pugw92(BoundX, BoundY);
                    AppendLog("Przekonwertowano układ WGS84 na PUWG92");
                    AppendLog("Tworzenie pliku world...");
                    sw.WriteLine(StructureValue);
                    sw.WriteLine(0);
                    sw.WriteLine(0);
                    sw.WriteLine("-" + StructureValue);
                    sw.WriteLine(Ypuwg.Replace(',', '.'));
                    sw.WriteLine(Xpuwg.Replace(',', '.'));
                    AppendLog("Utworzono plik world");
                    MAINPB.Content = "GOTOWE";
                }
            }
        }


        private void JsonFilePath_Click(object sender, RoutedEventArgs e)
        {
            Stream myStream = null;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            //openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog1.Filter = "JSON File (*.json)|*.json";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.ShowDialog();

            try
            {
                if ((myStream = openFileDialog1.OpenFile()) != null)
                {
                    using (myStream)
                    {
                        //JsonFile = openFileDialog1.FileName; THIS GOES AFTER START BTN EVENT ON CLICK
                        JsonPath.Text = openFileDialog1.FileName;
                        
                    }
                }
            }
            catch (Exception)
            {
            }

        } //open json file

        private void MapFilePath_Click(object sender, RoutedEventArgs e)
        {
            Stream myStream = null;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

           // openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog1.Filter = "Image files (*.png *.jpg *.jpeg) | *.png; *.jpg; *.jpeg";

            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.ShowDialog();

            try
            {
                if ((myStream = openFileDialog1.OpenFile()) != null)
                {
                    using (myStream)
                    {
                        MapPath.Text = openFileDialog1.FileName;
                    }
                }
            }
            catch (Exception)
            {
            }
        } //open map file

        private void START_Click(object sender, RoutedEventArgs e)
        {
            LOGS.Clear();
            //try
            //{
                ComboBoxItem typeItem = (ComboBoxItem)ComboBoxMW.SelectedItem;
                try
                {
                    StructureValue = StructureValues(typeItem.Content.ToString());
                    AppendLog("Wybrano układ " + typeItem.Content.ToString() + " [" + StructureValue + "]");
                }
                catch (Exception)
                {
                    //System.Windows.MessageBox.Show("Układ nie został wybrany", "BŁĄD");
                }

                JsonFile = JsonPath.Text;
                MapFile = MapPath.Text;

                AssignValuesFromJson(JsonFile);
                JPGWFile(MapFile);

                AppendLog("Ścieżka pliku world: " + JPGWPath);

                CreateWorldFile();
            //}
            //catch (Exception excp) { System.Windows.MessageBox.Show("Wprowadź poprawnie wszystkie dane: " + excp, "Wystąpił błąd"); }
        }
    }
}
