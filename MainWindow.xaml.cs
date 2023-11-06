using Microsoft.VisualBasic;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfApp2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<StarWarsPeople> StarWarsPeoples { get; set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtName.Text))
            {
                lstNames.Items.Add(txtName.Text);
                txtName.Clear();
            }
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Browse for JSON file";
            ofd.DefaultExt = ".json";
            ofd.Filter = "Json files | *.json";
            ofd.Multiselect = false;

            string dir = Directory.GetCurrentDirectory();
            ofd.InitialDirectory = dir;
            bool? result = ofd.ShowDialog();

            if (result != null && result.Value == true)
            {
                string file = ofd.FileName;
                string jason = File.ReadAllText(file);
                List<String>? list = JsonConvert.DeserializeObject<List<String>>(jason);
                if (list != null)
                {
                    lstNames.Items.Clear();
                    foreach (String s in list)
                    {
                        lstNames.Items.Add(s);
                    }
                }
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Wollen sie wirklich speichern?", "Save", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                string jason = JsonConvert.SerializeObject(lstNames.Items);
                File.WriteAllText("namelist.json", jason);
            }
        }

        private void radioButtonName_Checked(object sender, RoutedEventArgs e)
        {
            txtName.IsEnabled = true;
            btnAdd.IsEnabled = true;

        }

        private void radioButtonWebsite_Checked(object sender, RoutedEventArgs e)
        {
            txtName.IsEnabled = false;
            btnAdd.IsEnabled = false;
        }

        private void btnWebsite_Click(object sender, RoutedEventArgs e)
        {
            lstNames.Foreground = new SolidColorBrush(Colors.DarkBlue);
            lstNames.Background = new SolidColorBrush(Colors.LightGreen);

            // https://en.wikipedia.org/wiki/List_of_programmers
            string sUrl = txtWebsite.Text;
            if (String.IsNullOrEmpty(sUrl))
            {
                MessageBox.Show("Url must not be empty", "Invalid Url", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!sUrl.StartsWith("http"))
            {
                sUrl = "https://" + sUrl;
            }
            Uri url = new Uri(sUrl);
            if(!Uri.IsWellFormedUriString(sUrl, UriKind.Absolute))
            {
                MessageBox.Show("Url not well-formed", "Invalid Url", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            HttpClient httpClient = new HttpClient();
            Task<string> task = httpClient.GetStringAsync(url);
            string data = "";
            try
            {
                data = task.Result;
            }
            catch (Exception ex)
            {
                string sText = ex.Message;
                if (ex.InnerException is not null)
                {
                    sText += "\n";
                    sText += ex.InnerException.Message;
                }
                MessageBox.Show("Exception: " + sText, "Invalid Url", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string expr = @"<a [^\<\>]*href\=\""([^\#][^\""]*?)\"""; // TODO: Find all HTML - links
            Regex regEx = new Regex(expr);
            MatchCollection matchedLinks = regEx.Matches(data);
            foreach (Match match in matchedLinks)
            {
                string s = match.Groups[1].Value;
                if (s.StartsWith("http"))
                {
                    lstNames.Items.Add(s);
                }
            }
        }

        private void btnApi_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            // Ein "people" - Datensatz (JSON), repräsentiert durch die Klasse "StarWarsPeople" 
            //string people = getRequest("/api/people/1");
            //StarWarsPeople? starWarsPeople = System.Text.Json.JsonSerializer.Deserialize<StarWarsPeople>(people);
            //lstNames.Items.Add(starWarsPeople.Name);

            //Mehrere "people" - Datensätze, repräsentiert durch die Klasse "StarWarsPeopleList"
            string people = getRequest("/api/people");
            StarWarsPeopleList? peopleList = System.Text.Json.JsonSerializer.Deserialize<StarWarsPeopleList>(people);
            while (peopleList.Next != null)
            {
                // Wie verkettete Liste; solange es ein "next" gibt, machen wir weiter
                people = getRequest(peopleList.Next);
                StarWarsPeopleList? tempList = System.Text.Json.JsonSerializer.Deserialize<StarWarsPeopleList>(people);
                // Füge die neue Liste zur alten hinzu
                peopleList.Peoples.AddRange(tempList.Peoples);
                // ...und setze den neuen Next-Zeiger
                peopleList.Next = tempList.Next;
                foreach (StarWarsPeople p in tempList.Peoples)
                {
                    lstNames.Items.Add(p.Name);
                }
            }
            //// Alle "people" zur ListBox hinzufügen
            //foreach (StarWarsPeople p in peopleList.Peoples)
            //{
            //    lstNames.Items.Add(p.Name);
            //}

            Mouse.OverrideCursor = null;
        }

        private async void btnApi2_Click(object sender, RoutedEventArgs e)
        {
            //Mouse.OverrideCursor = Cursors.Wait;
            //StarWarsPeoples = new();

            string people = await getRequestAsynch("/api/people");
            StarWarsPeopleList? peopleList = System.Text.Json.JsonSerializer.Deserialize<StarWarsPeopleList>(people);
            while (peopleList?.Next is not null)
            {
                // "Erwarten" der asynchronen Ergebnisse, nicht blockierend
                people = await getRequestAsynch(peopleList.Next);
                StarWarsPeopleList? tempList = System.Text.Json.JsonSerializer.Deserialize<StarWarsPeopleList>(people);
                // Füge die neue Liste zur alten hinzu
                if (peopleList.Peoples is not null && tempList?.Peoples is not null)
                {
                    foreach (StarWarsPeople p in tempList.Peoples)
                    {
                        lstNames.Items.Add(p.Name);
                    }
                    peopleList.Peoples?.AddRange(tempList.Peoples);
                }
                // ...und setze den neuen Next-Zeiger
                peopleList.Next = tempList?.Next;
            }
            StarWarsPeoples = new();
            // Alle "people" zur ListBox hinzufügen
            if (peopleList?.Peoples is not null)
            {
                foreach (StarWarsPeople p in peopleList.Peoples)
                {
                    //lstNames.Items.Add(p.Name);
                    StarWarsPeoples.Add(p);
                }
            }
            dataGrid.ItemsSource = StarWarsPeoples;

            Mouse.OverrideCursor = null;
        }

        private string getRequest(string url)
        {
            HttpClient client = new HttpClient();
            Uri baseUri = new Uri("https://swapi.dev");
            client.BaseAddress = baseUri;
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            //make the request
            var task = client.SendAsync(requestMessage);
            var response = task.Result;
            HttpResponseMessage msg = response.EnsureSuccessStatusCode();
            string responseBody = response.Content.ReadAsStringAsync().Result;
            //MessageBox.Show(responseBody);
            return responseBody;
        }

        private async Task<string> getRequestAsynch(string url)
        {
            HttpClient client = new HttpClient();
            Uri baseUri = new Uri("https://swapi.dev");
            client.BaseAddress = baseUri;
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            //make the request
            var task = await client.SendAsync(requestMessage);
            HttpResponseMessage msg = task.EnsureSuccessStatusCode();
            string responseBody = await task.Content.ReadAsStringAsync();
            return responseBody;
        }

        private void txtWebsite_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnWebsite_Click(sender, e);
            }
        }

        private void lstNames_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string txt = "---";
            ListBox? listBox = sender as ListBox;
            if (listBox is not null)
            {
                txt = listBox.SelectedItem as string;
            }
            //MessageBox.Show(txt);

            Uri uri;
            if (Uri.TryCreate(txt, UriKind.Absolute, out uri))
            {
                try
                {
                    ProcessStartInfo processStartInfo = new ProcessStartInfo();
                    processStartInfo.FileName = uri.AbsoluteUri;
                    processStartInfo.UseShellExecute = true;
                    Process.Start(processStartInfo);
                }
                catch (System.ComponentModel.Win32Exception noBrowser)
                {
                    if (noBrowser.ErrorCode == -2147467259)
                        MessageBox.Show(noBrowser.Message);
                }
                catch (System.Exception other)
                {
                    MessageBox.Show(other.Message);
                }
            }
        }

        private void txtWebsite_KeyUp(object sender, KeyEventArgs e)
        {

        }
    }
}
