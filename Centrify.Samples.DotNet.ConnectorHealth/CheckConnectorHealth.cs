/**
 * Copyright 2016 Centrify Corporation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *  http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 **/

/*This version of Centrify.Samples.DotNet.ApiLib has been modified from its origional version found here 
* https://github.com/centrify/centrify-samples-dotnet-cs/tree/master/Centrify.Samples.DotNet.ApiLib to better 
* fit the needs of the SIEM Utitlity that this version is bundled with. */

using System;
using System.Collections.Generic;
using Centrify.Samples.DotNet.ApiLib;
using System.IO;
using System.Configuration;
using Newtonsoft.Json;
using System.Linq;
using System.Web.Script;

namespace Centrify.Samples.DotNet.ConnectorHealth
{
    class CheckConnectorHealth
    {
        static string Version = "Version 1.1_05_02_17";

        static void Main(string[] args)
        {
            DateTime lastRunTime = DateTime.Now;

            if (File.Exists("LastRun.txt"))
            {
                lastRunTime = Convert.ToDateTime(File.ReadAllText("LastRun.txt"));
            }

            //Output all console logs to log file 
            FileStream filestream = new FileStream("log.txt", FileMode.Create);
            var streamwriter = new StreamWriter(filestream);
            streamwriter.AutoFlush = true;
            Console.SetOut(streamwriter);
            Console.SetError(streamwriter);

            Console.WriteLine("Starting Connector Health Utility... \n");
            Console.WriteLine("Application Version: " + Version);
            Console.WriteLine("Current System Time: " + DateTime.Now + Environment.NewLine);

            Console.WriteLine("Checking last run time and backing up files if needed...");

            if (lastRunTime < DateTime.Today)
            {
                var files = Directory.GetFiles(@".\Output\", "*.csv");
                string prefix = lastRunTime.Month.ToString() + "_" + lastRunTime.Day.ToString() + "_" + lastRunTime.Year.ToString() + "_";
                foreach (var file in files)
                {
                    string newFileName = Path.Combine(Path.GetDirectoryName(file) + "\\Archived\\", (prefix + Path.GetFileName(file)));
                    File.Move(file, newFileName);
                }
            }

            Console.WriteLine("Connecting to Centrify API...");

            //Authenticate to Centrify with no MFA service account
            RestClient authenticatedRestClient = InteractiveLogin.Authenticate(ConfigurationManager.AppSettings["CentrifyEndpointUrl"], ConfigurationManager.AppSettings["AdminUserName"], ConfigurationManager.AppSettings["AdminPassword"]);
            ApiClient apiClient = new ApiClient(authenticatedRestClient);

            Console.WriteLine("Getting list of Centrify Cloud Connectors");

            //Get list of Cloud Connectors for tenant
            Newtonsoft.Json.Linq.JObject connectorList = apiClient.Query("select MachineName, ID from proxy");

            //Check health of each connector in tenant and write health status to CSV
            foreach (var connector in connectorList["Results"])
            {
                Dictionary<string, dynamic> dicConnector = connector["Row"].ToObject<Dictionary<string, dynamic>>();
                Console.WriteLine("Checking health of cloud connector. Name: {0}, ID: {1}", dicConnector["MachineName"], dicConnector["ID"]);

                Newtonsoft.Json.Linq.JArray results = apiClient.CheckProxyHealth(dicConnector["ID"]);

                Dictionary<string, dynamic> dicConnResults = results[0].ToObject<Dictionary<string, dynamic>>();

                ProcessQueryResults(dicConnector["MachineName"].ToString(), dicConnResults);
            }

            //Log last run time at every successful run.
            try
            {
                System.IO.File.WriteAllText(@".\LastRun.txt", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine("There Was An Error When Writing LastRun.txt");
                Console.WriteLine(ex.InnerException);
            }
        }

        //Process connector health results to individual CSV files
        static void ProcessQueryResults(string fileName, Dictionary<string, dynamic> results)
        {
            try
            {
                using (var writer = new StreamWriter(@"Output\" + fileName + ".csv", true))
                {
                    Console.WriteLine("Processing connector health results...");

                    //Conenctor info will be present in every result for checkproxyhealth
                    Dictionary < string, dynamic > dicConnInfo = results["ConnectorInfo"].ToObject<Dictionary<string, dynamic>>();
                    //adInfo will not be in the result if the connector is not connected to an AD or LADP environment. We must check if adInfo is present
                    Dictionary<string, dynamic> dicAdInfo = null;
                    Dictionary<string, dynamic> dicFullResult = null;

                    //Check if connector has an AD or LDAP environment and populate dicAdInfo if it is
                    Console.WriteLine("Checking Active Directory environment...");

                    if (results.ContainsKey("AdInfo"))
                    {
                        dicAdInfo = results["AdInfo"].ToObject<Dictionary<string, dynamic>>();
                        Console.WriteLine("Active Directory found...");
                    }
                    else
                    {
                        Console.WriteLine("No Active Directory environments were found...");
                    }

                    //Combine dictionaries if adInfo is found. Otherwise just use the Connector Info dictionary
                    if (dicAdInfo != null)
                    {
                        //CheckProxyHealth returns status for both the connector and AD. Combining dictionaries with Union to preserve if one status is different and joining them if the same. 
                        dicFullResult = dicConnInfo.Union(dicAdInfo).ToDictionary(k => k.Key, v => v.Value);
                    }
                    else
                    {
                        dicFullResult = dicConnInfo;
                    }       

                    //Create string for CSV. This sample uses a log format not column row format. i.e. results will be in columnName=value;column2Name=vale;etc. If different formatting is desired, the logic will need to be changed. 
                    string connectorHealth = string.Join(ConfigurationManager.AppSettings["CSVDelimiter"], dicFullResult.Select(x => x.Key + "=" + x.Value).ToArray());

                    //Write to CSV
                    writer.WriteLine(connectorHealth);
                    writer.Flush();
                    
                    Console.WriteLine("Connector health successfully written to CSV...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was an error processing connector health to CSV: " + ex.Message);
            }
        }
    }
}

