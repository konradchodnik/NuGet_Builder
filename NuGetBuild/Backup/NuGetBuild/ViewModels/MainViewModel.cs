using NuGetBuild.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml;
using System.Xml.XPath;
using GalaSoft.MvvmLight.Helpers;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;

namespace NuGetBuild.ViewModels
{
    
    public class MainViewModel : ViewModelBase
    {   
        //1 przerobic na linq to xml
        //2 build - elsa
        //3 rejestr ?
        //4 nuget


        public ICommand NowaLokalizacja { get; set; }
        public ICommand Kopiowanie { get; set; }
        public ICommand BudujNuget { get; set; }
        public ICommand EdytujXML { get; set; }
        public ICommand Aktualizacja { get; set; }

        //wersja visual studio 2015
        const string ACTIVE_VERSION = "VisualStudio.DTE.14.0";
        EnvDTE80.DTE2 MyDte = (EnvDTE80.DTE2)System.Runtime.InteropServices.Marshal.GetActiveObject(ACTIVE_VERSION);

        //kolekcje do przeszukania folderow
        public ObservableCollection<Dane> WczytajNuspec { get; private set; }

        //Combobox - wybierz .nuspec
        private Dane combobox;
        public Dane Combobox
        {
            get { return combobox; }
            set
            {
                combobox = value;
                DaneXML = LoadNuspec(Combobox.Path);
            }
        }

        //Dane xml - wczytywanie pol
        private DaneXml daneXml;
        public DaneXml DaneXML
        {
            get { return daneXml; }
            set { daneXml = value; RaisePropertyChanged(); }
        }

        //wybrany folder do przekopiowania
        private String wybranyFolder;
        public String WybranyFolder
        {
            get
            {
                return wybranyFolder;
            }
            set
            {
                wybranyFolder = value;
                RaisePropertyChanged("WybranyFolder");
                AppSettings.Default.WybranyFolder = wybranyFolder;
                AppSettings.Default.Save();
            }
        }

        public MainViewModel()
        {
            NowaLokalizacja = new RelayCommand(NowaLokalizacjaButton);
            Kopiowanie = new RelayCommand(KopiowanieButton);
            BudujNuget = new RelayCommand(BudujNugetButton);
            EdytujXML = new RelayCommand(EdytujXMLButton);
            Aktualizacja = new RelayCommand(AktualizacjaButton);
            
            #region Dokumentacja : objaśnienie nazw zmiennych
            /* Dokumentacja:
             * 
             * 1: SolutionPath  - sciezka z projektem plik .sln -> np: C:\Users\Konrad\Documents\Visual Studio 2015\Projects\NuGetBuild\NuGetBuild.sln
             * 2: SolutionPathFolder - sciezka z projektem folder glowny -> np: C:\Users\Konrad\Documents\Visual Studio 2015\Projects\NuGetBuild
             * 3: SingleFile_sln - Pojedynczy plik .sln z nazwy projektu
             * 4: SingleFile_nuspec - Plik .nuspec - nazwa kombinowana
             * 5: nuspec_Extension - sama końcówka .nupkg - do wyszukiwania
             * 
             */
            #endregion  Dokumentacja 

            string SolutionPath = MyDte.Solution.FullName;
            string SolutionPathFolder = Path.GetDirectoryName(SolutionPath);
            string SingleFile_sln = Path.GetFileName(SolutionPath);
            string SingleFile_nuspec = Path.ChangeExtension(SingleFile_sln, ".nuspec");
            string nuspec_Extension = Path.GetExtension(SingleFile_nuspec);
            
            #region Wypelnienie List Combobox
            try
            {
                DirectoryInfo PrzeszukajFolder = new DirectoryInfo(SolutionPathFolder);
                //nuscepfiles
                WczytajNuspec = new ObservableCollection<Dane>();
                foreach (var element in PrzeszukajFolder.GetFiles("*", SearchOption.AllDirectories))
                {
                    if (Path.GetExtension(element.Name) == nuspec_Extension)
                    {
                        WczytajNuspec.Add(new Dane() { Name = element.Name, Path = element.FullName });
                    }
                }
            }
            catch (Exception)
            {
                WczytajNuspec = new ObservableCollection<Dane>()
                {
                    new Dane() {Name = "Nie istnieje jeszcze plik .nuspec" }
                };
            }
            #endregion 
        }
        private DaneXml LoadNuspec(string PliknNspec)
        {
            //xml czytanie danych 
            XmlDocument document = new XmlDocument();
            document.Load(PliknNspec);
            XmlNamespaceManager manager = new XmlNamespaceManager(document.NameTable);
            manager.AddNamespace("NamespaceManager", "http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd");
            XmlNode nodeId = document.SelectSingleNode("//NamespaceManager:id", manager);
            XmlNode nodeAuthors = document.SelectSingleNode("//NamespaceManager:authors", manager);
            XmlNode nodeVersion = document.SelectSingleNode("//NamespaceManager:version", manager);
            XmlNode nodeOwners = document.SelectSingleNode("//NamespaceManager:owners", manager);
            XmlNode nodeDesc = document.SelectSingleNode("//NamespaceManager:description", manager);
            XmlNode nodenote = document.SelectSingleNode("//NamespaceManager:releaseNotes", manager);
            XmlNode nodeTitl = document.SelectSingleNode("//NamespaceManager:title", manager);

            daneXml = new DaneXml(nodeId.InnerXml,
                                  nodeAuthors.InnerXml,
                                  nodeVersion.InnerXml,
                                  nodeDesc.InnerXml,
                                  nodeOwners.InnerXml,
                                  nodenote.InnerXml,
                                  nodeTitl.InnerXml
                                 );
            return daneXml;
        }
        private void NowaLokalizacjaButton()
        {
            var wybierz = new FolderBrowserDialog();
            DialogResult wynikWyszukania = wybierz.ShowDialog();
            WybranyFolder = wybierz.SelectedPath;
        }
        private void KopiowanieButton()
        {
            try
            {
                #region Dokumentacja Ścieżek i zmiennych 
                /*
                 * 
                 * 1: PathFromCombobox - Scieżka do projektu z zaznaczonego pliku .nuspec z comboboxa -> np: C:\Users\Konrad\Documents\Visual Studio 2015\Projects\NuGetBuild\NuGetBuild\NuGetBuild.nuspec
                 * 2: ProjectPath - Ścieżka do projektu -> np: C:\Users\Konrad\Documents\Visual Studio 2015\Projects\NuGetBuild\NuGetBuild
                 * 3: SingleFile_nuspec
                 * 4: nuspec_Version - odczytana wartość z pliku .nuspec z wezła version
                 * 5: nuspec_Id - odczytana wartość z pliku .nuspec z wezła id
                 * 6: NameNewNupkg - sklejone : nowa nazwa nupkg
                 * 7: Path_NewNupkg - Ścieżka do nugeta -> np: C:\Users\Konrad\Documents\Visual Studio 2015\Projects\NuGetBuild\NuGetBuild\nuget.1.0.0.nupkg
                 *
                 */
                #endregion

                string PathFromCombobox = combobox.Path.ToString();
                string ProjectPath = Path.GetDirectoryName(PathFromCombobox);
                string SingleFile_nuspec = Path.GetFileName(PathFromCombobox);
                
                #region Dokumentacja XML

                #endregion

                XmlDocument document = new XmlDocument();
                document.Load(PathFromCombobox);
                XmlNamespaceManager manager = new XmlNamespaceManager(document.NameTable);
                manager.AddNamespace("NamespaceManager", "http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd");
                XmlNode nodeVersion = document.SelectSingleNode("//NamespaceManager:version", manager);
                XmlNode nodeId = document.SelectSingleNode("//NamespaceManager:id", manager);
                
                var nuspec_Version = nodeVersion.InnerXml;
                var nuspec_Id = nodeId.InnerXml;
                string NameNewNupkg = nuspec_Id + "." + nuspec_Version + ".nupkg";
                string Path_NewNupkg = ProjectPath + "\\" + NameNewNupkg;

                try
                {
                    //dopytać czy zrobić delete z miejsca docelowego
                    File.Copy(Path_NewNupkg, WybranyFolder + "\\" + Path.GetFileName(NameNewNupkg));
                    System.Windows.MessageBox.Show("Plik zostal przekopiowany.");

                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Wybierz wlasciwy folder. Problem z kopiowaniem pliku, {0}", ex.Message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Wybierz plik .nuspec z comboboxa oraz wybierz lokalizacje, {0}", ex.Message);
            }

           
        }
        private void BudujNugetButton()
        {
            #region Dokumentacja Ścieżek i zmiennych

            /*
             * 
             * 1: PathFromCombobox - Scieżka do projektu z zaznaczonego pliku.nuspec z comboboxa->np: C: \Users\Konrad\Documents\Visual Studio 2015\Projects\NuGetBuild\NuGetBuild\NuGetBuild.nuspec
             * 2: SingleFile_nuspec - pojedynczy plik .nuspec z projektu zaznaczonego -> np: NugetBuildFile.nuspec
             * 3: ProjectPath - ścieżka solucji z projektu -> np: C:\Users\Konrad\Documents\Visual Studio 2015\Projects\NuGetBuild\NuGetBuild
             * 4: NuGetFolder - \NuGet\NuGet.exe - folder z aktualizowanym nugetem
             * 5: Path_NuGetFile_exe - ścieżka do pliku nuget.exe -> np: C:\Users\Konrad\Documents\Visual Studio 2015\Projects\NuGetBuild\NuGetBuild\NuGet.exe 
             * 6: Path_NuGet_Folder - sciezka do nugeta, ktory jest aktualizowany -> np: C:\Users\Konrad\Documents\Visual Studio 2015\Projects\NuGetBuild\NuGetBuild\NuGet\NuGet.exe
             * 
             */
            #endregion

            string PathFromCombobox = combobox.Path.ToString();
            string SingleFile_nuspec = Path.GetFileName(PathFromCombobox);
            string ProjectPath = Path.GetDirectoryName(PathFromCombobox);
            string NuGetFolder = @"\NuGet\NuGet.exe";
            string Path_NuGetFile_exe = ProjectPath + @"\NuGet.exe";
            string Path_NuGet_Folder = ProjectPath + NuGetFolder;

            //zmienic elsa
            if (! File.Exists(Path_NuGetFile_exe))
            {
                //Zrobienie kopi Z folderu NuGet(aktualny nuget) DO miejsca budowania nugeta
                File.Copy(Path_NuGet_Folder, ProjectPath + "\\" + "NuGet.exe");
                try
                {
                    string displayName;
                    RegistryKey key;

                    key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\14.0");
                    foreach (String keyName in key.GetSubKeyNames())
                    {
                        RegistryKey subkey = key.OpenSubKey(keyName);
                        displayName = subkey.GetValue("VSPerfReportPath") as string;

                        if (Path.GetFileNameWithoutExtension(displayName) == "VSPerfReport")
                        {
                            #region Ścieżki z Rejestru
                            /*
                             * 1: Path_SubKey - wyswietla sciezke jaka na jaka aktualnie wskazuje displayName -> np: C:\Program Files (x86)\Microsoft Visual Studio 14.0\Team Tools\Performance Tools
                             * 2: Path_SubKey_Oneline - C:\Program Files (x86)\Microsoft Visual Studio 14.0
                             * 3: NewSearchPath - C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools - skelejona wartosc
                             * 
                             */
                            #endregion

                            var Path_SubKey = Path.GetDirectoryName(displayName);
                            var Path_SubKey_Oneline = Path.GetDirectoryName(Path.GetDirectoryName(Path_SubKey));
                            var addString = @"\Common7\Tools";
                            var NewSearchPath = Path_SubKey_Oneline + addString;

                            //przeszukanie wewnatrz, juz w docelowym pliku
                            DirectoryInfo Search = new DirectoryInfo(NewSearchPath);
                            foreach (var file in Search.GetFiles("*", SearchOption.AllDirectories))
                            {
                                if (file.Name == "VsMSBuildCmd.bat")
                                {
                                    UseProcess(SingleFile_nuspec, ProjectPath, NewSearchPath);
                                    //GetProcess(NewSearchPath);
                                    
                                    /*
                                    Process p = new Process();
                                    ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo("cmd", NewSearchPath);
                                    startInfo.RedirectStandardInput = true;
                                    startInfo.UseShellExecute = false;
                                    p.StartInfo = startInfo;
                                    p.Start();
                                    using (StreamWriter sw = p.StandardInput)
                                    {
                                        if (sw.BaseStream.CanWrite)
                                        {
                                            /* wersja na sztywno dzialajaca 
                                             sw.WriteLine(@"cd\");
                                             sw.WriteLine(@"cd C:\Users\Konrad\Documents\Visual Studio 2015\Projects\BudujNugetaVSIXProject1\BudujNugetaVSIXProject1");
                                             sw.WriteLine("nuget pack BudujNugetaVSIXProject1.nuspec");
                                             
                                            sw.WriteLine(@"cd\");
                                            sw.WriteLine(@"cd {0}", ProjectPath);
                                            sw.WriteLine("nuget pack {0}", SingleFile_nuspec);
                                        }
                                    }
                                    p.Close();
                                    */
                                }
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Nastapil problem ze zbudowaniem .nuspec, {0}", ex.Message);
                }
            }

            else
            {
                try
                {
                    string displayName;
                    RegistryKey key;
                    // key - klucz glowny z rejestru - jeden z 6 - localMachine
                    //diplayName - wypisuje aktualnie przekazywane podklucze 
                    key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\14.0");
                    foreach (String keyName in key.GetSubKeyNames())
                    {
                        RegistryKey subkey = key.OpenSubKey(keyName);
                        displayName = subkey.GetValue("VSPerfReportPath") as string;

                        //jesli natrafi na podklucz VSPerfReport to wykona 
                        if (Path.GetFileNameWithoutExtension(displayName) == "VSPerfReport")
                        {
                            #region Ścieżki z Rejestru
                            /*
                             * 1: Path_SubKey - wyswietla sciezke jaka na jaka aktualnie wskazuje displayName -> np: C:\Program Files (x86)\Microsoft Visual Studio 14.0\Team Tools\Performance Tools
                             * 2: Path_SubKey_Oneline - C:\Program Files (x86)\Microsoft Visual Studio 14.0
                             * 3: NewSearchPath - C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools - skelejona wartosc
                             * 
                             */
                            #endregion

                            PlikZRejestru(SingleFile_nuspec, ProjectPath, displayName);
                        }
                    }
           
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Nastapil problem ze zbudowaniem .nuspec, {0}", ex.Message);
                }
            }
      
        }

        private static void PlikZRejestru(string SingleFile_nuspec, string ProjectPath, string displayName)
        {
            var Path_SubKey = Path.GetDirectoryName(displayName);
            var Path_SubKey_Oneline = Path.GetDirectoryName(Path.GetDirectoryName(Path_SubKey));
            var addString = @"\Common7\Tools";
            var NewSearchPath = Path_SubKey_Oneline + addString;

            //przeszukanie wewnatrz, juz w docelowym pliku
            DirectoryInfo Search = new DirectoryInfo(NewSearchPath);
            foreach (var file in Search.GetFiles("*", SearchOption.AllDirectories))
            {
                if (file.Name == "VsMSBuildCmd.bat")
                {
                    UseProcess(SingleFile_nuspec, ProjectPath, NewSearchPath);
                }
            }
        }

        private static void UseProcess(string SingleFile_nuspec, string ProjectPath, string NewSearchPath)
        {
            Process p = GetProcess(NewSearchPath);
            p.Start();
            using (StreamWriter sw = p.StandardInput)
            {
                if (sw.BaseStream.CanWrite)
                {
                    /* wersja na sztywno dzialajaca 
                     sw.WriteLine(@"cd\");
                     sw.WriteLine(@"cd C:\Users\Konrad\Documents\Visual Studio 2015\Projects\BudujNugetaVSIXProject1\BudujNugetaVSIXProject1");
                     sw.WriteLine("nuget pack BudujNugetaVSIXProject1.nuspec");
                     */
                    sw.WriteLine(@"cd\");
                    sw.WriteLine(@"cd {0}", ProjectPath);
                    sw.WriteLine("nuget pack {0}", SingleFile_nuspec);
                }
            }
            p.Close();
        }

        private static Process GetProcess(string NewSearchPath)
        {
            Process p = new Process();
            ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo("cmd", NewSearchPath);
            startInfo.RedirectStandardInput = true;
            startInfo.UseShellExecute = false;
            p.StartInfo = startInfo;
            return p;
        }

        private void EdytujXMLButton()
        {
            try
            {
                //  1: PathFromCombobox - Scieżka do projektu z zaznaczonego pliku .nuspec z comboboxa -> np: C:\Users\Konrad\Documents\Visual Studio 2015\Projects\NuGetBuild\NuGetBuild\NuGetBuild.nuspec
                string PathFromCombobox = combobox.Path.ToString();

                XmlDocument document = new XmlDocument();
                document.Load(PathFromCombobox);
                XmlNamespaceManager manager = new XmlNamespaceManager(document.NameTable);
                manager.AddNamespace("NamespaceManager","http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd");
                XPathNavigator navigator = document.CreateNavigator();
                foreach (XPathNavigator nav in navigator.Select("//NamespaceManager:id", manager))
                {
                    nav.SetValue(DaneXML.Id.ToString());
                }
                foreach (XPathNavigator nav in navigator.Select("//NamespaceManager:title", manager))
                {
                    nav.SetValue(DaneXML.Title.ToString());
                }
                foreach (XPathNavigator nav in navigator.Select("//NamespaceManager:authors", manager))
                {
                    nav.SetValue(DaneXML.Authors.ToString());
                    /*if (nav.Value == "konrad")
                    {
                        nav.SetValue(DaneXML.Authors.ToString());
                    }*/
                }
                foreach (XPathNavigator nav in navigator.Select("//NamespaceManager:owners", manager))
                {
                    nav.SetValue(DaneXML.Owners.ToString());
                }
                foreach (XPathNavigator nav in navigator.Select("//NamespaceManager:version", manager))
                {
                    /* sprawdzenie liczby
                    string spr = DaneXML.Version.ToString();
                    bool sprawdzenieLiczby = Char.IsNumber(spr, 3);
                    if (sprawdzenieLiczby == true)
                    {
                        nav.SetValue(DaneXML.Version.ToString());
                    }
                    if(sprawdzenieLiczby == false)
                    {
                        System.Windows.MessageBox.Show("Aby poprawnie zapisać plik, version musi być liczba, np: 1.0.0");
                    }
                    */
                    nav.SetValue(DaneXML.Version.ToString());
                }
                foreach (XPathNavigator nav in navigator.Select("//NamespaceManager:description", manager))
                {
                    nav.SetValue(DaneXML.Description.ToString());
                }
                foreach (XPathNavigator nav in navigator.Select("//NamespaceManager:releaseNotes", manager))
                {
                    nav.SetValue(DaneXML.ReleaseNotes.ToString());
                }
                document.Save(PathFromCombobox);
                System.Windows.MessageBox.Show("Zapisano dane do pliku.");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Wypełnij wszystkie pola, {0}", ex.Message);
            }
        }
        private void AktualizacjaButton()
        {
            #region Dokumentacja ścieżek i zmiennych
            /*
             * 
             * 1: PathFromCombobox - Scieżka do projektu z zaznaczonego pliku.nuspec z comboboxa->np: C: \Users\Konrad\Documents\Visual Studio 2015\Projects\NuGetBuild\NuGetBuild\NuGetBuild.nuspec
             * 2: ProjectPath - C:\Users\Konrad\Documents\Visual Studio 2015\Projects\NuGetBuild\NuGetBuild
             * 3: Path_OldNuget - C:\Users\Konrad\Documents\Visual Studio 2015\Projects\NuGetBuild\NuGetBuild\NuGet
             * 4: Path_NewNuget - C:\Users\Konrad\Documents\Visual Studio 2015\Projects\NuGetBuild\NuGetBuild\bin\Debug
             * 
             */
            #endregion

            try
            {
                string PathFromCombobox = combobox.Path.ToString();
                string ProjectPath = Path.GetDirectoryName(PathFromCombobox);
                string Path_OldNuget = ProjectPath + @"\NuGet";
                string Path_NewNuget = ProjectPath + @"\bin\Debug";

                string uri = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe";
                string LokalizacjaUri = Path_NewNuget + @"\nuget.exe";
                try
                {
                    WebClient download = new WebClient();
                    download.DownloadFileAsync(new Uri(uri), LokalizacjaUri);
                    while (download.IsBusy) { }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Problem przy ściągnięciu pliku. " + ex.Message);
                }

                DirectoryInfo SearchFirstNuGet = new DirectoryInfo(Path_OldNuget);
                foreach (var item in SearchFirstNuGet.GetFiles("*", SearchOption.AllDirectories))
                {

                    if (Path.GetFileNameWithoutExtension(item.ToString().ToUpper()) == "NUGET")
                    {
                        //Sprawdzenie wersji - Informacje o pliku
                        var NugetInfo_old = FileVersionInfo.GetVersionInfo(item.FullName);
                        var version_old = NugetInfo_old.ProductVersion;
                        string versionConvert_old = version_old.Replace(".", "");
                        double OldNuget = Convert.ToDouble(versionConvert_old);

                        DirectoryInfo SearchSecondNuGet = new DirectoryInfo(Path_NewNuget);
                        foreach (var file in SearchSecondNuGet.GetFiles("*", SearchOption.AllDirectories))
                        {
                            if (Path.GetFileNameWithoutExtension(file.ToString().ToUpper()) == "NUGET")
                            {
                                NowyNuget(item, OldNuget, file);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Wybierz plik .nuspec z combobox" + ex.Message);
            }      
        }

        private static void NowyNuget(FileInfo item, double OldNuget, FileInfo file)
        {
            //Sprawdzenie wersji - Informacje o pliku
            var NugetInfo_new = FileVersionInfo.GetVersionInfo(file.FullName);
            var version_new = NugetInfo_new.ProductVersion;
            string versionConvert_new = version_new.Replace(".", "");
            double NewNuget = Convert.ToDouble(versionConvert_new);

            var Path_Nuget_Download_Pleace = Path.GetFullPath(file.FullName);
            var Path_NuGet_Folder = Path.GetDirectoryName(Path.GetFullPath(item.FullName)) + @"\NuGet.exe";

            //Path_Nuget_Download_Pleace - C:\Users\Konrad\Documents\Visual Studio 2015\Projects\NuGetBuild\NuGetBuild\bin\Debug\nuget.exe
            //Path_NuGet_Folder - C:\Users\Konrad\Documents\Visual Studio 2015\Projects\NuGetBuild\NuGetBuild\NuGet\NuGet.exe

            if (OldNuget >= NewNuget)
            {
                MessageBox.Show("Posiadasz najświeższą wersje NuGet");
            }
            else
            {
                //File.Delete(Path.GetFullPath(item.FullName));
                File.Copy(Path_Nuget_Download_Pleace, Path_NuGet_Folder, true);
                File.Delete(Path.GetFullPath(file.FullName));
                MessageBox.Show("Aktualizacja zakonczona sukcesem ");
            }
        }
    }
}

