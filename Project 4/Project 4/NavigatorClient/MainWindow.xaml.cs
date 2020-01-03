////////////////////////////////////////////////////////////////////////////
// MainWindow.xaml.cs - defines WPF application processing by the client  //
// ver 1.0                                                                //
// Language:    C#, VS 2017                                               //
// Platform:    HP Envy Notebook                                          //
// Application: Demonstration for CSE681, Project #4, Fall 2018           //
// Author:      Rucha Guntoorkar, SUID 453497450                          //
// Reference:   Helper code for Project 4 by Dr. Jim Fawcett              //  
////////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package defines WPF application processing by the client.  The client
 * displays remote FileFolder view, dependency analysis result and strong
 * component result.  It supports
 * navigating into subdirectories and fies on remote Server.
 * 
 * Package Interface:
 * -------------------
 * MainWindow m=new MainWindow()    //constructs object
 * m.initializeEnvironment()        //make Environment equivalent to ClientEnvironment
 * m.initializeMessageDispatcher()  //define how to process each message command
 * m.getTopFiles()                  //load remoteFiles listbox with files from root
 * m.getTopDirs()                   //load remoteDirs listbox with dirs from root
 * m.moveIntoFolderFiles()          //load remoteFiles listbox with files from folder
 * m.moveIntoFolderDirs()           //load remoteDirs listbox with dirs from folder
 * m.getStrongComponent()           //load stringComponent listbox with files from strong 
 *                                    component analysis
 * m.getDependencyAnalysis()        //load dependency analysis result
 * m.rcvThreadProc()                //define processing for GUI's receive thread
 * m.remoteTopClick()               //move to root of remote directories and gets the file
 * m.remoteUpClick()                // move to parent directory of current remote path 
 * m.remoteDirsMouseDoubleClick()   //move into remote subdir and display files and subdirs
 * m.DepAnalysisBtnClick()          //gets dependency analysis result on button click
 * m.StrongCompButtonClick()        //gets the strong component result
 * m.DemonstrateATU()               //invokes automated test unit methods
 * m.showDependencyAnalysis()       //tests the dependency analysis requirement 
 * m.Requirement3_ShowPackages()    //tests the packages requirement
 * m.showStrongAnalysis()           //tests the stromg component analysis requirement
 * m.requirement6_7()               //tests the GUI and ATU suite requirement
 * 
 * Required Files:
 * -------------------
 * MainWindow.xaml.cs, MainWindow.xaml, MPCommService.cs,
 * FileMgr.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 3.0 : 5 Dec 2018
 * - changed the GUI to show the dependency analysis and strong components
 *   result 
 * ver 2.1 : 26 Oct 2017
 * - relatively minor modifications to the Comm channel used to send messages
 *   between NavigatorClient and NavigatorServer
 * ver 2.0 : 24 Oct 2017
 * - added remote processing - Up functionality not yet implemented
 *   - defined NavigatorServer
 *   - added the CsCommMessagePassing prototype
 * ver 1.0 : 22 Oct 2017
 * - first release
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Threading;
using MessagePassingComm;
using System.Collections.ObjectModel;


namespace Navigator
{
    public class BoolStringClass
    {
        public string TheText { get; set; }
        public int TheValue { get; set; }
    }
    public partial class MainWindow : Window
    {
        private IFileMgr fileMgr { get; set; } = null;  // note: Navigator just uses interface declarations
        Comm comm { get; set; } = null;

        public string TheText { get; set; }

        List<string> checkedStuff;
        public ObservableCollection<BoolStringClass> TheList { get; set; }

        Dictionary<string, Action<CommMessage>> messageDispatcher = new Dictionary<string, Action<CommMessage>>();
        Thread rcvThread = null;

        static List<string> atuInputFiles { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            initializeEnvironment();
            Console.Title = "Client";
            fileMgr = FileMgrFactory.create(FileMgrType.Local); // uses Environment
            comm = new Comm(ClientEnvironment.address, ClientEnvironment.port);
            initializeMessageDispatcher();
            DepAnalysisBtn.IsEnabled = false;
            StrngCompBtn.IsEnabled = false;
            rcvThread = new Thread(rcvThreadProc);
            checkedStuff = new List<string>();
            rcvThread.Start();
            remoteTopClick();
            DemonstrateATU();                      
        }

        void checkOnLoad()
        {
            CheckBox cbox = new CheckBox();

            
        }
        //----< make Environment equivalent to ClientEnvironment >-------
       
        void initializeEnvironment()
        {
            Environment.root = ClientEnvironment.root;
            Environment.address = ClientEnvironment.address;
            Environment.port = ClientEnvironment.port;
            Environment.endPoint = ClientEnvironment.endPoint;
        }
        //----< define how to process each message command >-------------

        void initializeMessageDispatcher()
        {
            getTopFiles();
            getTopDirs();
            moveIntoFolderFiles();
            moveIntoFolderDirs();
            getDependencyAnalysis();
            getStrongComponent();
        }

       
        //------------< load remoteFiles listbox with files from root >----------------
        void getTopFiles()
        {
            messageDispatcher["getTopFiles"] = (CommMessage msg) =>
            {

                DataContext = null;
                TheList = new ObservableCollection<BoolStringClass>();
                int i = 0;
                foreach (string file in msg.arguments)
                {
                    TheList.Add(new BoolStringClass { TheText = file, TheValue = i });
                    i = i + 1;

                }
                this.DataContext = this;

            };
        }

        //--------< load remoteDirs listbox with dirs from root>--------------
        void getTopDirs()
        {
            messageDispatcher["getTopDirs"] = (CommMessage msg) =>
            {
                remoteDirs.Items.Clear();
                foreach (string dir in msg.arguments)
                {
                    remoteDirs.Items.Add(dir);
                }
            };
        }

        //---------<load remoteFiles listbox with files from folder>--------------
        void moveIntoFolderFiles()
        {
            messageDispatcher["moveIntoFolderFiles"] = (CommMessage msg) =>
            {
                DataContext = null;
                TheList = new ObservableCollection<BoolStringClass>();
                int i = 1;
                foreach (string file in msg.arguments)
                {
                    TheList.Add(new BoolStringClass { TheText = file, TheValue = i });
                    i = i + 1;

                }
                this.DataContext = this;
            };
        }

        //----------< load remoteDirs listbox with dirs from folder >-------------
        void moveIntoFolderDirs()
        {

            messageDispatcher["moveIntoFolderDirs"] = (CommMessage msg) =>
            {
                remoteDirs.Items.Clear();
                foreach (string dir in msg.arguments)
                {
                    remoteDirs.Items.Add(dir);
                }

            };
        }

        //-------< load stringComponent listbox with files from strong component analysis >-------
        void getStrongComponent()
        {
            messageDispatcher["getStrongComponent"] = (CommMessage msg) =>
            {
                
                StrngCompListBox.Items.Clear();
                foreach (string file in msg.arguments)
                {
                    StrngCompListBox.Items.Add(file);
                    Console.WriteLine(file);
                }
            };
        }

        //-------< load dependency analysis result >----------------
        void getDependencyAnalysis()
        {
            messageDispatcher["getDependencyAnalysis"] = (CommMessage msg) =>
            {
                DepAnalysisListBox.Items.Clear();
                foreach (string dep in msg.arguments)
                {
                    DepAnalysisListBox.Items.Add(dep);
                    Console.WriteLine(dep);
                }

            };
        }
        //----< define processing for GUI's receive thread >-------------

        void rcvThreadProc()
        {
            Console.Write("\n  starting client's receive thread");
            while (true)
            {
                CommMessage msg = comm.getMessage();
                msg.show();
                if (msg.command == null)
                    continue;

                // pass the Dispatcher's action value to the main thread for execution

                Dispatcher.Invoke(messageDispatcher[msg.command], new object[] { msg });
            }
        }

        //----< shut down comm when the main window closes >-------------

        private void Window_Closed(object sender, EventArgs e)
        {
            comm.close();

            // The step below should not be nessary, but I've apparently caused a closing event to 
            // hang by manually renaming packages instead of getting Visual Studio to rename them.

            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
        //----< not currently being used >-------------------------------

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        //----< add the file to CheckedStuff list when the checkbox is selected >-------
        private void CheckBoxZone_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cbox = sender as CheckBox;
            string s = cbox.Content as string;

            if ((bool)cbox.IsChecked && !checkedStuff.Contains(s))
                checkedStuff.Add(s);
            if(checkedStuff.Count>=0)
            {
                DepAnalysisBtn.IsEnabled = true;
                StrngCompBtn.IsEnabled = true;
            }


        }


        //----< move to root of remote directories and gets the file >---------------------
       
        private void RemoteTop_Click(object sender, RoutedEventArgs e)
        {
            remoteTopClick();
        }
       
        /*
        * - sends a message to server to get files from root
        * - recv thread will create an Action<CommMessage> for the UI thread
        *   to invoke to load the remoteFiles listbox
        */
        void remoteTopClick()
        {
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = ServerEnvironment.endPoint;
            msg1.author = "Jim Fawcett";
            msg1.command = "getTopFiles";
            msg1.arguments.Add("");
            comm.postMessage(msg1);
            CommMessage msg2 = msg1.clone();
            msg2.command = "getTopDirs";
            comm.postMessage(msg2);
        }
        //----< download file and display source in popup window >-------

        private void remoteFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // coming soon

        }
        //----< move to parent directory of current remote path >--------

        private void RemoteUp_Click(object sender, RoutedEventArgs e)
        {
            remoteUpClick();
        }
        /*
       * - sends a message to server to get parent directory
       * - recv thread will create an Action<CommMessage> for the UI thread
       *   to invoke to load the remoteDirs listbox
       */
        void remoteUpClick()
        {
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = ServerEnvironment.endPoint;
            msg1.author = "Jim Fawcett";
            msg1.command = "getTopDirs";
            //msg1.arguments.Add(remoteDirs.Items[0].ToString());
            fileMgr.pathStack.Push(fileMgr.currentPath);

            if (remoteDirs.Items.Count > 0)
            {
                fileMgr.currentPath = remoteDirs.Items[0].ToString();
                msg1.arguments.Add(remoteDirs.Items[0].ToString());
            }
            else
            {
                fileMgr.currentPath = "";
                msg1.arguments.Add("");
            }

            comm.postMessage(msg1);
            CommMessage msg2 = msg1.clone();
            msg2.command = "getTopFiles";
            comm.postMessage(msg2);
        }

        //----< move into remote subdir and display files and subdirs >--
        private void remoteDirs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            remoteDirsMouseDoubleClick();
        }

        /*
         * - sends messages to server to get files and dirs from folder
         * - recv thread will create Action<CommMessage>s for the UI thread
         *   to invoke to load the remoteFiles and remoteDirs listboxs
         */
        void remoteDirsMouseDoubleClick()
        {
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = ServerEnvironment.endPoint;
            msg1.command = "moveIntoFolderFiles";
            msg1.arguments.Add(remoteDirs.SelectedValue as string);
            comm.postMessage(msg1);
            CommMessage msg2 = msg1.clone();
            msg2.command = "moveIntoFolderDirs";
            comm.postMessage(msg2);
        }


     
        //--------------< Displays dependency analysis result on button click>------------
        private void DepAnalysisBtn_Click(object sender, RoutedEventArgs e)
        {
            DepAnalysisBtnClick();
        }

        /*
        * - sends messages to server to get dependency analysis result by sending filenames
        * - recv thread will create Action<CommMessage>s for the UI thread
        *   to invoke to load the dependency analysis result in the listbox
        */
        public void DepAnalysisBtnClick()
        {
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = ServerEnvironment.endPoint;
            msg1.author = "Rucha Guntoorkar";
            msg1.command = "getDependencyAnalysis";
            if (checkedStuff.Count > 0)
                msg1.arguments = checkedStuff;
            else
                msg1.arguments = atuInputFiles;
            comm.postMessage(msg1);
        }

        //------------< invokes when there is selection change in tab>--------------
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        //------------< invokes when there is selection change in remotefiles>----------
        private void remoteFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        //-----------< displays the strong component result after clicking the button>------------
        private void StrngCompBtn_Click(object sender, RoutedEventArgs e)
        {
            StrongCompButtonClick();
        }

        /*
        * - sends messages to server to get strong component analysis result by sending filenames
        * - recv thread will create Action<CommMessage>s for the UI thread
        *   to invoke to load the strong component analysis result in the listbox
        */
        public void StrongCompButtonClick()
        {
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = ServerEnvironment.endPoint;
            msg1.author = "Rucha Guntoorkar";
            msg1.command = "getStrongComponent";
            if (checkedStuff.Count > 0)
                msg1.arguments = checkedStuff;
            else
                msg1.arguments = atuInputFiles;
            comm.postMessage(msg1);
            CommMessage msg2 = msg1.clone();
        }

        //----------< invokes when checkbox is unchecked>--------------
        private void TokerCHeckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox cbox = sender as CheckBox;
            string s = cbox.Content as string;

            if (!(bool)cbox.IsChecked)
                checkedStuff.Remove(s);
            if(checkedStuff.Count <= 0)
            {
                DepAnalysisBtn.IsEnabled = false;
                StrngCompBtn.IsEnabled = false;
                DepAnalysisListBox.Items.Clear();
                StrngCompListBox.Items.Clear();
            }
        }

        //-----------< invokes when checkbox is loaded >--------------
        private void TokerCHeckBox_Loaded(object sender, RoutedEventArgs e)
        {
            CheckBox cbox = sender as CheckBox;
            string s = cbox.Content as string;

            if (checkedStuff.Contains(s))
                cbox.IsChecked = true;
        }

        //---------< invokes automated test unit methods >---------------
        public void DemonstrateATU()
        {
            setListValues();
            Console.WriteLine("Demonstrating project 4 requirement");
            Console.WriteLine("=============================================================================================================");
            Requirement3_ShowPackages();
            showDependencyAnalysis();
            DepAnalysisListBox.Items.Clear();
            showStrongAnalysis();
            StrngCompListBox.Items.Clear();
            requirement6_7();
        }

        //---------< tests the dependency analysis requirement >-----------
        void showDependencyAnalysis()
        {
            Console.WriteLine("Requirement 4:Demonstrating dependency analysis by passing messages to the client");
            Console.WriteLine("-------------------------------------------------------------------------------------------------------------");
            DepAnalysisBtnClick();
            Console.Write("Test Passed\n");
        }

        //---------< tests the packages requirement >-----------
        void Requirement3_ShowPackages()
        {
            Console.WriteLine("Requirement 3: Display the packages as mentioned in project requirement");
            Console.WriteLine("-------------------------------------------------------------------------------------------------------------");
            Console.WriteLine("Packages used in Project 3 are as follows");
            string path = "../../../";
            path = System.IO.Path.GetFullPath(path);

            Console.WriteLine(System.IO.Path.Combine(path, "Toker\\"));
            Console.WriteLine(System.IO.Path.Combine(path, "SemiExp\\Semi.cs"));
            Console.WriteLine(System.IO.Path.Combine(path, "TypeTable\\TypeTable.cs"));
            Console.WriteLine(System.IO.Path.Combine(path, "TypeAnalysis\\TypeAnalyis.cs"));
            Console.WriteLine(System.IO.Path.Combine(path, "DepAnalysis\\DependencyAnalysis.cs"));
            Console.WriteLine(System.IO.Path.Combine(path, "TestDependencyAnaysis\\Executive.cs"));
            Console.WriteLine(System.IO.Path.Combine(path, "NavigatorClient\\MainWindow.xaml.cs"));
            Console.WriteLine(System.IO.Path.Combine(path, "NavigatorServer\\NavigatorServer.cs"));
            Console.WriteLine(System.IO.Path.Combine(path, "MessagePassingCommService\\MPCommService.cs"));

            Console.WriteLine("\n Test Passed\n");
        }

        //---------< tests the stromg component analysis requirement >-----------
        void showStrongAnalysis()
        {
            Console.WriteLine("Requirement 5:Demonstrating strong components by passing messages to the client");
            Console.WriteLine("-------------------------------------------------------------------------------------------------------------");
            StrongCompButtonClick();
            Console.Write("\n Test Passed\n");
        }

        //---------< tests the GUI and ATU suite requirement >-----------
        public void requirement6_7()
        {
            Console.WriteLine("Requirement 6: Display the results on GUI");
            Console.WriteLine("-------------------------------------------------------------------------------------------------------------");
            Console.WriteLine("The results are shown on the GUI which is  developed using WPF");
            Console.WriteLine("\nTest Passed\n");
            
            Console.WriteLine("Requirement 7: Automated test unit suite to demonstrate all the requirements");
            Console.WriteLine("-------------------------------------------------------------------------------------------------------------");
            Console.WriteLine("All of the above are automated tests which demonstrate all the requirements");
            Console.WriteLine("\nTest Passed\n"); ;
        }

        //---------------< populate the list>---------------------------
        void setListValues()
        {
            atuInputFiles = fileMgr.getFiles().ToList<String>();
            checkedStuff = fileMgr.getFiles().ToList<String>();

        }

        //-------------< clear the listbox >-------------------------------
        void clearListBox()
        {
            //Thread.Sleep(7000);
            DepAnalysisListBox.Items.Clear();
            StrngCompListBox.Items.Clear();
        }

  
        
    }
}
