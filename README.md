# Centrify.Samples.DotNet.ConnectorHealth

Notes: This package contains the source code for a sample Connector Health Utility for the Centrify Identity Service Platform API's written in C#. The solution Centrify.Samples.DotNet.ConnectorHealth.sln (VS 2015) contains two projects:

1. Centrify.Samples.DotNet.ApiLib - Includes a general REST client for communicating with the CIS Platform, as well as a class, ApiClient, which uses the REST client to make calls to specific platform API's.
2. Centrify.Samples.DotNet.ConnectorHealth - A sample console application utility which utilizes the ApiLib project to authenticate a user, query the platform API's for a list of Centrify Cloud Connectors registered to a tenant, check the health of health connector, and export the health results to individual CSV files for further use (i.e. sending reports via email, ingesting into other database systems, use in data warehouse solutions and ETL's such as SSIS, etc). The utility will also back up old output files when it notices that the day has changed since its last run.

  This utility can be ran manually to check a list of connectors or it can be automated using a system like Windows Task Scheduler.

Installation and use Instructions:

1. First compile the solution in Release
2. Copy the contents of the Release folder to a location of your choice
3. Open the App.config file and customize your Centrify tenant url, admin username, and admin password. Make sure the admin account is set to not use MFA or the utility will fail.
4. Run utility from command line or by double click
5. Results will be located in the Output folder in the root of the utility directory.
6. Use a scheduling tool, such as Windows Task Scheduler, to run the utility on a scheduled basis. The utility can be ran as often as desired as long as it has a chance to finish running before it is ran again.
7. Make use of exported CSV files in desired use case, or add functionality to the sample project to meet the needs of desired use case.
