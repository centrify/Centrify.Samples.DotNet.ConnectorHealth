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
using System.Net;
using System.Collections;

namespace Centrify.Samples.DotNet.ApiLib
{
    public class ApiClient
    {
        private RestClient m_restClient = null;

        public ApiClient(RestClient authenticatedClient)
        {
            m_restClient = authenticatedClient;
        }

        public ApiClient(string endpointBase, string bearerToken)
        {
            m_restClient = new RestClient(endpointBase);
            m_restClient.BearerToken = bearerToken;
        }

        public string BearerToken
        {
            get
            {
                if (m_restClient.BearerToken != null)
                {
                    return m_restClient.BearerToken;
                }
                else
                {
                    if (m_restClient.Cookies != null)
                    {
                        CookieCollection endpointCookies = m_restClient.Cookies.GetCookies(new Uri(m_restClient.Endpoint));
                        if (endpointCookies != null)
                        {
                            Cookie bearerCookie = endpointCookies[".ASPXAUTH"];
                            if (bearerCookie != null)
                            {
                                return bearerCookie.Value;
                            }
                        }
                    }
                }
                return null;
            }

            set
            {
                m_restClient.BearerToken = value;
            }
        }
        
        // Illustrates usage of /redrock/query to run queries
        public dynamic Query(string sql)
        {
            Dictionary<string, dynamic> args = new Dictionary<string, dynamic>();
            args["Script"] = sql;

            Dictionary<string, dynamic> queryArgs = new Dictionary<string, dynamic>();
            args["Args"] = queryArgs;

            /*queryArgs["PageNumber"] = 1;
            queryArgs["PageSize"] = 10000;
            queryArgs["Limit"] = 10000;
            queryArgs["Caching"] = -1;*/

            var result = m_restClient.CallApi("/redrock/query", args);
            if (result["success"] != true)
            {
                Console.WriteLine("Running query failed: {0}", result["Message"]);
                throw new ApplicationException(result["Message"]);
            }

            return result["Result"];
        }

        // Illustrates usage of /UserMgmt/GetUserRolesAndAdministrativeRights to get user roles
        public dynamic CheckProxyHealth(string ID)
        {
            var result = m_restClient.CallApi("/core/CheckProxyHealth?proxyUuid=" + ID, null);
            if (result["success"] != true)
            {
                Console.WriteLine("CheckProxyHealths failed: {0}", result["Message"]);
                throw new ApplicationException(result["Message"]);
            }

            return result["Result"]["Connectors"];
        }
    }
}
