using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;



namespace MonetraUtility
{
    class Program
    {
        static void log(string message)
        {
            Console.WriteLine(message);
        }

        static void Main(string[] args)
        {
            string server = "db5";
            ushort port = 8666;
            string file = null;
            string industryCode = "rs";

            string user = null;
            string pass = null;

            bool printUsage = false;

            string action = "setindustrycode";

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-port":
                        port = ushort.Parse(args[i + 1]);

                        i++; // increment arg since the immediate next is the value.
                        break;

                    case "-server":
                        server = args[i + 1];

                        i++; // increment arg since the immediate next is the value.
                        break;

                    case "-industrycode":
                        industryCode = args[i + 1];

                        i++; // increment arg since the immediate next is the value.
                        break;

                    case "-file":
                        file = args[i + 1];

                        i++; // increment arg since the immediate next is the value.
                        break;

                    case "-user":
                        user = args[i + 1];

                        i++; // increment arg since the immediate next is the value.
                        break;

                    case "-password":
                        pass = args[i + 1];

                        i++; // increment arg since the immediate next is the value.
                        break;

                    case "-action":
                        action = args[i + 1];

                        i++; // increment arg since the immediate next is the value.
                        break;

                    case "-?":
                        // Fall through on purpose.
                    default:
                        printUsage = true;
                        break;
                }
            }

            if ((null == user)
                || (null == pass))
            {
                // Invalid arguments, display the usage.
                printUsage = true;
            }

            if (printUsage)
            {
                Console.WriteLine("Monetra Utility\n\n" +
                    "    Parameters:\n" +
                    "    -server [server name]\n" +
                    "    -port [port]\n" +
                    "    -user [user name]\n" +
                    "    -password [password]\n" +
                    "    -file [file name]\n" +
                    "    -action [action to take]: valid values are:\n" +
                    "        setindustrycode\n" +
                    "        listusers\n" +
                    "    -industrycode [value]: Code to set for setindustrycode action.\n"
                );
            }
            else
            {
                try
                {
                    MonetraUtility mu = new MonetraUtility(server, port, user, pass);

                    string[] merchants = null;

                    if (null != file)
                    {
                        merchants = File.ReadAllLines(file);
                    }

                    switch (action.ToLower())
                    {
                        case "listusers":
                            if (null != merchants)
                            {
                                mu.ListUsers(merchants);
                            }
                            else
                            {
                                mu.ListUsers();
                            }
                            break;
                        case "setindustrycode":
                        default:
                            mu.ProcessIndustryCode(merchants, industryCode);                           
                            break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unknown problem encountered: {0}", e.Message);
                    //throw;
                }

            }
        }
    }
}
