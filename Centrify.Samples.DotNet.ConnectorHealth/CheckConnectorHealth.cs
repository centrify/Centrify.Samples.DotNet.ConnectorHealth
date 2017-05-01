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

namespace Centrify.Samples.DotNet.ConnectorHealth
{
    class CheckConnectorHealth
    {
        static string Version = "Version 1.0_05_01_17";

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

                Newtonsoft.Json.Linq.JObject results = apiClient.CheckProxyHealth(dicConnector["ID"]);

                Dictionary<string, dynamic> dicConnResults = results["Connectors"][0]["ConnectorInfo"].ToObject<Dictionary<string, dynamic>>();

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
                using (var writer = new StreamWriter(@"Output\" + fileName + ".csv"))
                {
                    string connectorHealth = string.Join(";", results.Select(x => x.Key + "=" + x.Value).ToArray());

                    writer.WriteLine(connectorHealth);
                    writer.Flush();
                    
                    Console.WriteLine("Connector health successfully written to CSV...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was an error processing connector health to CSV: " + ex.InnerException);
            }
        }
    }
}

