// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class EchoBot : ActivityHandler
    {
        Cookie cookie;

        public  EchoBot(){
            cookie = getCookie();
            //System.Console.WriteLine(cookie);
        }
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            string input = turnContext.Activity.Text;
            string output="";
            int workItemId= int.Parse(input);
            output=$"Input: {input} Work Item : {workItemId}";
            //run_cmd("test.py", "1");
            string url = getWorkItemUrl(workItemId);
            string response = Get(url,cookie);
            //System.Console.WriteLine(response);
            string workitem=parseWorkItem(response,cookie);
            System.Console.WriteLine("WorkItem data : "+workitem);
            await turnContext.SendActivityAsync(MessageFactory.Text(output), cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Hello and welcome! Please give me your Work Item ID"), cancellationToken);
                }
            }
        }

        public string Get(string uri,Cookie cookie)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.CookieContainer=new CookieContainer();
            Uri target = new Uri(uri);
            cookie.Domain=target.Host;
            request.CookieContainer.Add(cookie);

            using(HttpWebResponse response = (HttpWebResponse)request.GetResponse()){
                HttpStatusCode statusCode =  (HttpStatusCode)response.StatusCode;
                //System.Console.WriteLine("Status :",statusCode);
                using(Stream stream = response.GetResponseStream())
                using(StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
            
        
        }

        public Cookie getCookie()
        {
            string value = System.IO.File.ReadAllText("cookiefile.txt");
            return new Cookie("SpsAuthenticatedUser",value);
        }

        public string getWorkItemUrl(int workItemId){
            string url_first_part="https://office.visualstudio.com/DefaultCollection/Outlook%20Mobile/";
            string url=url_first_part+"_apis/wit/workitems?ids="+workItemId+"&$expand=all&api-version=5.1";
            return url;
        }

        public string parseWorkItem(string data,Cookie cookie){
            dynamic dataJson = JObject.Parse(data);
            dataJson=dataJson.value[0];
            string outputString="";
            //System.Console.WriteLine(dataJson);
            List<string> fields_keys= new List<string> {"System.TeamProject","System.WorkItemType","System.Title","System.Description","Microsoft.VSTS.TCM.ReproSteps","Office.Common.ExpectedOutcome","Office.Common.ActualOutcome","System.Tags","System.WorkItemType"};
            dynamic fields=dataJson.fields;
            foreach(string k in fields_keys){
                if (fields.ContainsKey(k)) { 
                    System.Console.WriteLine(k+" "+fields[k]); 
                    string fields_k=(string) fields[k];
                    outputString=outputString + " "+cleanString(fields_k);
                }
            }
            dynamic links = dataJson._links;
            if(links.ContainsKey("workItemComments")){
                string commentsUrl=links.workItemComments.href;
                string commentsData = Get(commentsUrl,cookie);
                commentsData=parseComments(commentsData);
                outputString=outputString+" "+commentsData;
            }
            
            //System.Console.WriteLine(outputString);       
            return outputString;
        }

        public string parseComments(string data){
            dynamic dataJson = JObject.Parse(data);
            dynamic commentsData=dataJson.comments;
            string op="";
            foreach(dynamic com in commentsData){
                op=op+" "+cleanString(com.ToString());
            }
            return op;
        }

        public string cleanString(string ip)
        {
            ip = Regex.Replace(ip, @"<.*?>", "");
            ip = Regex.Replace(ip, @" +", "");
            ip=ip.Replace(",","").Replace("\n","").Replace("\r","").Replace("\t","");
            return ip;
        }
    }

    


}
