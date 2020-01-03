///////////////////////////////////////////////////////////////////////
// NavigatorServer.cs -  handles all incoming and outgoing requests  //
// ver 1.0                                                           //
// Language:    C#, VS 2017                                          //
// Platform:    HP Envy Notebook                                     //
// Application: Demonstration for CSE681, Project #4, Fall 2018      //
// Author:      Rucha Guntoorkar, SUID 453497450                     //
// Reference:   Helper code for Project 4 by Dr. Jim Fawcett         //  
///////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package defines a single NavigatorServer class that returns file
 * and directory information about its rootDirectory subtree, dependency analysis
 * and strong component.  It uses
 * a message dispatcher that handles processing of all incoming and outgoing
 * messages.
 * 
 * Public Interface:
 * -------------------
 * NavigatorServer server=new NavigatorServer()  //constructs the NavigatorServer object
 * server.initializeEnvironment()     //sets environment properties
 * server.ProcessFiles(args)          //processes input file
 * server.initializeDispatcher()      //defines how each message will be processed
 * server.getStrongComponent()        //adds getStrongComponent to messageDispatcher
 * server.getDependencyAnalysis()     //adds getDependencyAnalysis to messageDispatcher
 * server.getTopFiles()               //adds getTopFiles to messageDispatcher dictionary
 * server.getTopDirs()                //adds getTopDirs to messageDispatcher dictionary
 * server.moveIntoFolderFiles()       //adds moveIntoFolderFiles to messageDispatcher dictionary
 * server.moveIntoFolderDirs()        //adds moveIntoFolderDirs to messageDispatcher dictionary
 * 
 * Required Files:
 * -------------------
 * NavigatorServer.cs, FileMgr.cs, Executive.cs,
 * RulesAndActions.cs, DependencyTable.cs, CSGraph.cs,
 * MPCommService.cs
 * 
 * Maintanence History:
 * --------------------
 * ver 3.0 - 12 Dec 2017
 * - added new message commands in message dispatcher
 * ver 2.0 - 24 Oct 2017
 * - added message dispatcher which works very well - see below
 * - added these comments
 * ver 1.0 - 22 Oct 2017
 * - first release
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeAnalysis;
using CsGraph;
using MessagePassingComm;

namespace Navigator
{
    public class NavigatorServer
    {
        IFileMgr localFileMgr { get; set; } = null;
        Comm comm { get; set; } = null;

        Dictionary<string, Func<CommMessage, CommMessage>> messageDispatcher =
          new Dictionary<string, Func<CommMessage, CommMessage>>();

        /*----< initialize server processing >-------------------------*/

        public NavigatorServer()
        {
            initializeEnvironment();
            Console.Title = "Server";
            localFileMgr = FileMgrFactory.create(FileMgrType.Local);
        }
        /*----< set Environment properties needed by server >----------*/

        void initializeEnvironment()
        {
            Environment.root = ServerEnvironment.root;
            Environment.address = ServerEnvironment.address;
            Environment.port = ServerEnvironment.port;
            Environment.endPoint = ServerEnvironment.endPoint;
        }

        //---------< Processes the input files >------------------
        public static List<string> ProcessFiles(string[] args)
        {
            List<string> files = new List<string>();
            if (args.Length == 0)
            {
                Console.Write("\n  Please enter file(s) to analyze\n\n");
                return files;
            }
            //changing to point to the serverfileFolder
            string path = "../../../ServerFiles";
            path = Path.GetFullPath(path);
            for (int i = 0; i < args.Length; ++i)
            {
                string filename = Path.GetFileName(args[i]);
                files.AddRange(Directory.GetFiles(path, filename));
            }
            return files;
        }
        /*----< define how each message will be processed >------------*/
        void initializeDispatcher()
        {
            getStrongComponent();
            getDependencyAnalysis();
            getTopFiles();
            getTopDirs();
            moveIntoFolderFiles();
            moveIntoFolderDirs();   
        }

        //---------< adds getStrongComponent to messageDispatcher dictionary >------------------
        void getStrongComponent()
        {
            Func<CommMessage, CommMessage> getStrongComponent = (CommMessage msg) =>
            {
                localFileMgr.currentPath = "";
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "getStrongComponent";
                string[] args = new string[msg.arguments.Count];
                for (var i = 0; i < args.Length; ++i)
                    args[i] = msg.arguments[i].ToString();
                List<string> files = ProcessFiles(args);
                Executive executive = new Executive();
                executive.typeAnalysis(files);
                Repository repo = Repository.getInstance();
                executive.dependencyAnalysis(files);
                repo.dependencyTable.show();
                CsGraph<string, string> graph = executive.buildDependencyGraph();
                graph.strongComponents();
                foreach (var item in graph.strongComp)
                {
                    reply.arguments.Add("Strong Component " + (item.Key +1).ToString() + ":");
                    foreach (var elem in item.Value)
                    {
                            reply.arguments.Add(elem.name);
                    }
                }
                return reply;
            };
            messageDispatcher["getStrongComponent"] = getStrongComponent;
        }

        //---------< adds getDependencyAnalysis to messageDispatcher dictionary >------------------
        void getDependencyAnalysis()
        {
            Func<CommMessage, CommMessage> getDependencyAnalysis = (CommMessage msg) =>
            {
                localFileMgr.currentPath = "";
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "getDependencyAnalysis";
                string[] args = new string[msg.arguments.Count];
                for (var i = 0; i < args.Length; ++i)
                    args[i] = msg.arguments[i].ToString();
                List<string> files = ProcessFiles(args);
                Executive executive = new Executive();
                executive.typeAnalysis(files);
                Repository repo = Repository.getInstance();
                executive.dependencyAnalysis(files);
                repo.dependencyTable.show();
                Dictionary<string, List<string>> dep = repo.dependencyTable.dependencies;
                string delimiter = "\n\t";
                string val;
                foreach (var item in dep)
                {
                    string key = Path.GetFileName(item.Key);
                    if (item.Value.Count >= 1)
                    {
                        if (item.Value.Count > 1)
                        {
                            val = (item.Value).Aggregate((i, j) => Path.GetFileName(i) + delimiter + Path.GetFileName(j)).ToString();
                            reply.arguments.Add(key + delimiter + val);
                        }
                        else if (item.Value.Count == 1)
                        {
                            val = Path.GetFileName(item.Value[0]);
                            reply.arguments.Add(key + delimiter + val);
                        }
                    }
                    else
                        reply.arguments.Add(key);
                }
                return reply;
            };
            messageDispatcher["getDependencyAnalysis"] = getDependencyAnalysis;
        }

        //---------< adds getTopFiles to messageDispatcher dictionary >------------------
        void getTopFiles()
        {
            Func<CommMessage, CommMessage> getTopFiles = (CommMessage msg) =>
            {
                localFileMgr.currentPath = "";
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "getTopFiles";
                reply.arguments = localFileMgr.getFiles().ToList<string>();
                return reply;
            };
            messageDispatcher["getTopFiles"] = getTopFiles;
        }

        //---------< adds getTopDirs to messageDispatcher dictionary >------------------
        void getTopDirs()
        {
            Func<CommMessage, CommMessage> getTopDirs = (CommMessage msg) =>
            {
                localFileMgr.currentPath = "";
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "getTopDirs";
                reply.arguments = localFileMgr.getDirs(msg.arguments[0]).ToList<string>();
                return reply;
            };
            messageDispatcher["getTopDirs"] = getTopDirs;
        }

        //---------< adds moveIntoFolderFiles to messageDispatcher dictionary >------------------
        void moveIntoFolderFiles()
        {
            Func<CommMessage, CommMessage> moveIntoFolderFiles = (CommMessage msg) =>
            {
                if (msg.arguments.Count() == 1)
                    localFileMgr.currentPath = msg.arguments[0];
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "moveIntoFolderFiles";
                reply.arguments = localFileMgr.getFiles().ToList<string>();
                return reply;
            };
            messageDispatcher["moveIntoFolderFiles"] = moveIntoFolderFiles;
        }

        //---------< adds moveIntoFolderDirs to messageDispatcher dictionary >------------------
        void moveIntoFolderDirs()
        {
            Func<CommMessage, CommMessage> moveIntoFolderDirs = (CommMessage msg) =>
            {
                if (msg.arguments.Count() == 1)
                    localFileMgr.currentPath = msg.arguments[0];
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "moveIntoFolderDirs";
                reply.arguments = localFileMgr.getDirs(msg.arguments[0]).ToList<string>();
                return reply;
            };
            messageDispatcher["moveIntoFolderDirs"] = moveIntoFolderDirs;
        }

        /*----< Server processing >------------------------------------*/
        /*
         * - all server processing is implemented with the simple loop, below,
         *   and the message dispatcher lambdas defined above.
         */
        static void Main(string[] args)
        {
            TestUtilities.title("Starting Server", '=');
            try
            {
                NavigatorServer server = new NavigatorServer();
                server.initializeDispatcher();
                server.comm = new MessagePassingComm.Comm(ServerEnvironment.address, ServerEnvironment.port);

                while (true)
                {
                    CommMessage msg = server.comm.getMessage();
                    if (msg.type == CommMessage.MessageType.closeReceiver)
                        break;
                    msg.show();
                    if (msg.command == null)
                        continue;
                    CommMessage reply = server.messageDispatcher[msg.command](msg);
                    reply.show();
                    server.comm.postMessage(reply);
                }
            }
            catch (Exception ex)
            {
                Console.Write("\n  exception thrown:\n{0}\n\n", ex.Message);
            }
        }
    }
}
