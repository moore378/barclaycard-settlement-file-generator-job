using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;

using MjhGeneral;
using AuthorizationClientPlatforms;

namespace MonetraUtility
{
    /// <summary>
    /// Subset of the getuserinfo details.
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        /// Merchant account name.
        /// </summary>
        public string UserName;

        /// <summary>
        /// Industry code value as defined by Monetra.
        /// </summary>
        public string IndustryCode;

        /// <summary>
        /// Internal processor platform enumeration name.
        /// </summary>
        public string Processor;
    }

    /// <summary>
    /// Interacts with Monetra server... ideally in admin mode.
    /// </summary>
    /// <remarks>Interfacing directly with libmonetra instead of building
    /// on top of AuthorizationClientPlatform's monetra class in order
    /// to code things quickly without framework restrictions.</remarks>
    public class MonetraUtility
    {
        libmonetra.Monetra monetra;

        private string admin;
        private string pass;

        private Dictionary<string, bool> processorSelfServ;

        public MonetraUtility(string server, ushort port, string admin, string pass)
        {
            // Save off the admin credentials.
            this.admin = admin;
            this.pass = pass;

            Connect(server, port);

            //GenerateManualSelfServSupportList();
            GenerateSelfServSupportList();
        }

        private void Connect(string server, ushort port)
        {
            bool rc = true;

            // Imitate what MonetraDotNetNativeClient would have done with connecting to the server.
            monetra = new libmonetra.Monetra();

            // First try with SSL.
            rc = monetra.SetSSL(server, port);

            if (rc)
            {
                rc = monetra.SetBlocking(true);
            }

            if (rc)
            {
                rc = monetra.VerifySSLCert(false);
            }

            if (rc)
            {
                rc = monetra.Connect();

                if (!rc)
                {
                    // Failure, so try with standard TCP/IP.
                    rc = monetra.SetIP(server, port);

                    if (rc)
                    {
                        rc = monetra.Connect();
                    }
                }
            }

            if (rc)
            {
                // Connection successful. Set the timeout.
                rc = monetra.SetTimeout(30);
            }

            if (!rc)
            {
                throw new Exception("Unable to connect to Monetra");
            }
        }

        /// <summary>
        /// Dynamically figures out for all processors if they support Retail/Self Serv mode.
        /// </summary>
        private void GenerateSelfServSupportList()
        {
            bool rc = true;

            int transactionID = 0;

            int supportedIndustriesIndex = -1;
            int procIndex = -1;

            try
            {
                transactionID = monetra.TransNew();

                if (rc)
                {
                    rc = monetra.TransKeyVal(transactionID, "username", admin);
                }

                if (rc)
                {
                    rc = monetra.TransKeyVal(transactionID, "password", pass);
                }

                if (rc)
                {
                    rc = monetra.TransKeyVal(transactionID, "action", "proclist");
                }

                // Send the request. 
                if (rc)
                {
                    rc = monetra.TransSend(transactionID);
                    if ((!rc)
                        || (libmonetra.Monetra.M_SUCCESS != monetra.ReturnStatus(transactionID)))
                    {
                        throw new Exception("Unable to get the proc list. Must be admin user and on admin port.");
                    }
                }

                if (rc)
                {
                    // Parse out the CSV
                    rc = monetra.ParseCommaDelimited(transactionID);
                }

                if (rc)
                {
                    // Find the proc.
                    int cols = monetra.NumColumns(transactionID);

                    for (int i = 0; (i < cols) && ((-1 == supportedIndustriesIndex) || (-1 == procIndex)); i++)
                    {
                        //Console.WriteLine(monetra.GetHeader(transactionID, i));
                        switch (monetra.GetHeader(transactionID, i))
                        {
                            // NOTE: Iteratively testing this since technically 
                            // the column is not documented in the API.
                            case "supported_industries":
                                supportedIndustriesIndex = i;
                                break;
                            case "proc":
                                procIndex = i;
                                break;
                            default:
                                // Do nothing.
                                break;
                        }
                    }

                    if (-1 == supportedIndustriesIndex)
                    {
                        throw new Exception("Unable to find the supported industries column.");
                    }

                    if (-1 == procIndex)
                    {
                        throw new Exception("Unable to find the proc column");
                    }
                }

                if (rc)
                {
                    processorSelfServ = new Dictionary<string, bool>(StringComparer.CurrentCultureIgnoreCase);

                    int rows = monetra.NumRows(transactionID);

                    for (int i = 0; i < rows; i++)
                    {
                        string data = monetra.GetCellByNum(transactionID, supportedIndustriesIndex, i);

                        string proc = monetra.GetCellByNum(transactionID, procIndex, i);

                        // See if it supports the Retail Self Serv
                        bool supportsSelfServ = data.Split(new char[] { '|' }).Contains("RS");

                        //Console.WriteLine("{0} {1} {2}", proc, data, supportsSelfServ);

                        processorSelfServ[proc] = supportsSelfServ;
                    }
                }
            }
            finally
            {
                if (0 != transactionID)
                {
                    // Try to delete the transaction for completeness sake.
                    monetra.DeleteTrans(transactionID);
                }
            }
        }

        /// <summary>
        /// Generates the self serve processor list.
        /// </summary>
        /// <remarks>This was a manually generated list from looking at a 
        /// Monetra 8.3.1 system before we knew about using the proclist action.</remarks>
        private void GenerateManualSelfServSupportList()
        {
            processorSelfServ = new Dictionary<string, bool>(StringComparer.CurrentCultureIgnoreCase);

            processorSelfServ["amexauth"] = false;
            processorSelfServ["amexpip"] = false;
            processorSelfServ["buypass"] = false;
            processorSelfServ["cardnet"] = true;
            processorSelfServ["certegyecc"] = false;
            processorSelfServ["certegyimgrepo"] = false;
            processorSelfServ["certegy"] = false;
            processorSelfServ["chockstone"] = false;
            processorSelfServ["compass_auth"] = true;
            processorSelfServ["compass_settle"] = true;
            processorSelfServ["echk"] = false;
            processorSelfServ["echoimgrepo"] = false;
            processorSelfServ["elavoncf"] = false;
            processorSelfServ["fdgift"] = true;
            processorSelfServ["fifththird510"] = true;
            processorSelfServ["fifththird610"] = true;
            processorSelfServ["gers"] = false;
            processorSelfServ["givex"] = true;
            processorSelfServ["globalpaybb"] = true;
            processorSelfServ["globalpay"] = true;
            processorSelfServ["heartland"] = true;
            processorSelfServ["litle"] = false;
            processorSelfServ["loopback"] = true;
            processorSelfServ["mbba"] = true;
            processorSelfServ["mercury"] = true;
            processorSelfServ["monetra_gift"] = true;
            processorSelfServ["nabanco"] = false;
            processorSelfServ["nashnorth"] = true;
            processorSelfServ["nashville"] = false;
            processorSelfServ["nova"] = false;
            processorSelfServ["omaha"] = true;
            processorSelfServ["opticard"] = false;
            processorSelfServ["paymentech"] = true;
            processorSelfServ["paypal"] = false;
            processorSelfServ["paytronix"] = false;
            processorSelfServ["rbslynk"] = true;
            processorSelfServ["salem"] = true;
            processorSelfServ["securenet"] = true;
            processorSelfServ["spdh"] = true;
            processorSelfServ["svs"] = true;
            processorSelfServ["telecheckimgrepo"] = false;
            processorSelfServ["telecheck"] = false;
            processorSelfServ["valutec"] = true;
            processorSelfServ["vital"] = true;
            processorSelfServ["worldpay"] = false;
        }

        public void ProcessIndustryCode(string[] users, string industryCode)
        {
            int changed = 0;            // Keep track of how many were successfully changed.
            int skippedUnsupported = 0; // Keep track of ones that cannot be changed.
            int skippedAlreadySet = 0;  // Keep track of ones that don't need to be changed.
            int error = 0;              // Keep track of processing errors.

            bool processingAllowed = true;

            // Loop through each user.
            foreach(string entry in users)
            {
                UserInfo info = null;
                string message = null;

                try
                {
                    // Get the user details to find the processor that it's
                    // using.
                    info = QueryUserInfo(entry);

                    // See if we need to restrict things for self serv.
                    if (String.Equals(industryCode, "RS", StringComparison.CurrentCultureIgnoreCase))
                    {
                        // Check and see if the processor supports self serve.
                        // If it does, then it can continue on to change
                        // the industry code.
                        processingAllowed = processorSelfServ[info.Processor];
                    }

                    if (processingAllowed)
                    {
                        // Process the individual user.
                        if (!String.Equals(info.IndustryCode, industryCode, StringComparison.CurrentCultureIgnoreCase))
                        {
                            ChangeIndustryCode(entry, industryCode);

                            // If no exception's thrown by now, it's a success.
                            message = "Success.";
                            changed++;
                        }
                        else
                        {
                            message = "Already using industry code value.";
                            skippedAlreadySet++;
                        }
                    }
                    else
                    {
                        message = String.Format( "Industry code {0} not supported for processor {1}.", industryCode, info.Processor);
                        skippedUnsupported++;
                    }
                }
                catch (Exception e)
                {
                    message = String.Format("Error with {0}", e.Message);
                    error++;
                }

                Console.WriteLine("User {0}. {1}", entry, message);
            }

            Console.WriteLine("\n\n{0:000} entries to change.\n---", users.Length);

            if (0 != changed)
            {
                Console.WriteLine("{0:000} successfully changed industry code.", changed);
            }

            if (0 != skippedAlreadySet)
            {
                Console.WriteLine("{0:000} skipped since the value was already set.", skippedAlreadySet);
            }

            if (0 != skippedUnsupported)
            {
                Console.WriteLine("{0:000} skipped due to unsupported mode.", skippedUnsupported);
            }

            if (0 != error)
            {
                Console.WriteLine("{0:000} failed to process.", error);
            }   
        }

        /// <summary>
        /// Communicates with Monetra and change the industry code for the merchant user account.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="industryCode"></param>
        public void ChangeIndustryCode(string user, string industryCode)
        {
            bool rc = true;

            int transactionID = 0;

            try
            {
                transactionID = monetra.TransNew();

                rc = monetra.TransKeyVal(transactionID, "username", admin);

                if (rc)
                {
                    rc = monetra.TransKeyVal(transactionID, "password", pass);
                }

                if (rc)
                {
                    rc = monetra.TransKeyVal(transactionID, "action", "edituser");
                }

                if (rc)
                {
                    rc = monetra.TransKeyVal(transactionID, "user", user);
                }

                if (rc)
                {
                    monetra.TransKeyVal(transactionID, "indcode", industryCode);
                }

                if (rc)
                {
                    // Send the request. 
                    rc = monetra.TransSend(transactionID);
                    if ((!rc)
                        || (libmonetra.Monetra.M_SUCCESS != monetra.ReturnStatus(transactionID)))
                    {
                        string verbiage = monetra.ResponseParam(transactionID, "verbiage");

                        throw new Exception(String.Format("Unable to change the industry code due to {0}.", verbiage));
                    }
                }

                if (!rc)
                {
                    throw new Exception("Unable to change the industry code");
                }
            }
            finally
            {
                if (0 != transactionID)
                {
                    // Try to delete the transaction for completeness sake.
                    monetra.DeleteTrans(transactionID);
                }
            }
        }

        /// <summary>
        /// Communicates with Monetra and return back relevant user details 
        /// such as processor and industry code.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private UserInfo QueryUserInfo(string user)
        {
            UserInfo info = null;

            bool rc = true;

            int transactionID = 0;

            try
            {
                transactionID = monetra.TransNew();

                if (rc)
                {
                    rc = monetra.TransKeyVal(transactionID, "username", admin);
                }

                if (rc)
                {
                    rc = monetra.TransKeyVal(transactionID, "password", pass);
                }

                if (rc)
                {
                    rc = monetra.TransKeyVal(transactionID, "action", "getuserinfo");
                }

                if (rc)
                {
                    rc = monetra.TransKeyVal(transactionID, "user", user);
                }

                // Send the request. 
                if (rc)
                {
                    rc = monetra.TransSend(transactionID);
                    if ((!rc)
                        || (libmonetra.Monetra.M_SUCCESS != monetra.ReturnStatus(transactionID)))
                    {
                        throw new Exception(String.Format("Unable to get the industry code for {0}", user));
                    }
                }

                if (rc)
                {
                    try
                    {
                        info = new UserInfo()
                        {
                            UserName = user,
                            IndustryCode = monetra.ResponseParam(transactionID, "INDCODE"),
                            Processor = monetra.ResponseParam(transactionID, "proc")
                        };
                    }
                    catch (Exception)
                    {
                        throw new Exception(String.Format("Unable to parse the response for getuserinfo for {0}", user));
                    }
                }
            }
            finally
            {
                if (0 != transactionID)
                {
                    // Try to delete the transaction for completeness sake.
                    monetra.DeleteTrans(transactionID);
                }
            }

            return info;
        }

        /// <summary>
        /// List all users defined in Monetra, display their info, and also 
        /// validate if they are correctly set as Retail/SelfServ.
        /// </summary>
        public void ListUsers()
        {
            bool rc = true;

            int transactionID = 0;

            int userIndex = 0;

            try
            {
                transactionID = monetra.TransNew();

                rc = monetra.TransKeyVal(transactionID, "username", admin);

                if (rc)
                {
                    rc = monetra.TransKeyVal(transactionID, "password", pass);
                }

                if (rc)
                {
                    rc = monetra.TransKeyVal(transactionID, "action", "listusers");
                }

                // Send the request. 
                if (rc)
                {
                    rc = monetra.TransSend(transactionID);
                    if ((!rc)
                        || (libmonetra.Monetra.M_SUCCESS != monetra.ReturnStatus(transactionID)))
                    {
                        throw new Exception("Unable to get the listusers");
                    }
                }

                if (rc)
                {
                    rc = monetra.ParseCommaDelimited(transactionID);
                }

                if (rc)
                {
                    int rows = monetra.NumRows(transactionID);

                    string[] users = new string[rows];

                    // Get the users in an array format.
                    for (int i = 0; i < rows; i++)
                    {
                        users[i] = monetra.GetCellByNum(transactionID, userIndex, i);
                    }

                    // Call helper function for displaying the user details.
                    ListUsers(users);
                }
            }
            finally
            {
                if (0 != transactionID)
                {
                    // Try to delete the transaction for completeness sake.
                    monetra.DeleteTrans(transactionID);
                }
            }
        }

        /// <summary>
        /// Display user info and also validate if they are correctly
        /// set as Retail/Self Serv.
        /// </summary>
        /// <param name="users"></param>
        public void ListUsers(string[] users)
        {
            int wrongCode = 0;

            for (int i = 0; i < users.Length; i++)
            {
                string user = users[i];

                UserInfo info = QueryUserInfo(user);

                StringBuilder sb = new StringBuilder();

                sb.AppendFormat("User: {0}, Processor: {1}. Industry Code {2}.", info.UserName, info.Processor, info.IndustryCode);

                if ((processorSelfServ[info.Processor])
                    && (!String.Equals(info.IndustryCode, "RS", StringComparison.CurrentCultureIgnoreCase)))
                {
                    sb.Append(" INVALID!");

                    wrongCode++;
                }

                Console.WriteLine(sb.ToString());
            }

            if (0 != wrongCode)
            {
                Console.WriteLine("\n\n{0} of {1} users have the wrong industry codes.", wrongCode, users.Length);
            }
            else
            {
                Console.WriteLine("\n\nAll entries seem to have valid industry codes.");
            }
        }
    }
}
