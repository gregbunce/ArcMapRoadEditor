using Google.GData.Spreadsheets;
using Google.GData.Client;
using Google.GData.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;
//using NLog;

namespace UtransEditorAGRC
{
    class clsUtransEditorStaticClass
    {
        //set up nlogger for catching errors
        //private static Logger logger = LogManager.GetCurrentClassLogger();

        public static SpreadsheetsService service;
        public static string accessToken;
        public static OAuth2Parameters parameters;
        public static string strAccessCode = "4/n-puZ4O2y3rRKmE_lhQfgaz12LQnuRdpe_nWLg_8bBc"; //i got this access code 6/21/2016 - not being used right now

        // this method authorizes the google api with client id and secret, etc..
        public static void AuthorizeRequestGoogleSheetsAPI()
        {
            try
            {
                //also check for expiration time parameters.TokenExpiry {6/22/2016 12:12:08 PM}  seems to be about one hour before it expires - but, it also has a value called parameters.RefreshToken maybe I can use that to refresh it (?)
                if (parameters == null)
                {
                    clsGlobals.boolGoogleHasAccessCode = false;
                    ////////////////////////////////////////////////////////////////////////////
                    // STEP 1: Configure how to perform OAuth 2.0
                    ////////////////////////////////////////////////////////////////////////////

                    // TODO: Update the following information with that obtained from
                    // https://code.google.com/apis/console. After registering
                    // your application, these will be provided for you.

                    string CLIENT_ID = "378108272379-90bdt65runbo7im4hial72f8otk3t4d2.apps.googleusercontent.com";

                    // This is the OAuth 2.0 Client Secret retrieved
                    // above.  Be sure to store this value securely.  Leaking this
                    // value would enable others to act on behalf of your application!
                    string CLIENT_SECRET = "WIbwPxkAYwrg-4SVhhmGZNIf";

                    // Space separated list of scopes for which to request access.
                    //string SCOPE = "https://spreadsheets.google.com/feeds https://docs.google.com/feeds";
                    string SCOPE = "https://spreadsheets.google.com/feeds";

                    // This is the Redirect URI for installed applications.
                    // If you are building a web application, you have to set your
                    // Redirect URI at https://code.google.com/apis/console.
                    string REDIRECT_URI = "urn:ietf:wg:oauth:2.0:oob";

                    ////////////////////////////////////////////////////////////////////////////
                    // STEP 2: Set up the OAuth 2.0 object
                    ////////////////////////////////////////////////////////////////////////////

                    // OAuth2Parameters holds all the parameters related to OAuth 2.0.
                    parameters = new OAuth2Parameters();

                    // Set your OAuth 2.0 Client Id (which you can register at
                    // https://code.google.com/apis/console).
                    parameters.ClientId = CLIENT_ID;

                    // Set your OAuth 2.0 Client Secret, which can be obtained at
                    // https://code.google.com/apis/console.
                    parameters.ClientSecret = CLIENT_SECRET;

                    // Set your Redirect URI, which can be registered at
                    // https://code.google.com/apis/console.
                    parameters.RedirectUri = REDIRECT_URI;

                    ////////////////////////////////////////////////////////////////////////////
                    // STEP 3: Get the Authorization URL
                    ////////////////////////////////////////////////////////////////////////////

                    // Set the scope for this particular service.
                    parameters.Scope = SCOPE;

                    // Get the authorization url.  The user of your application must visit
                    // this url in order to authorize with Google.  If you are building a
                    // browser-based application, you can redirect the user to the authorization
                    // url.
                    string authorizationUrl = OAuthUtil.CreateOAuth2AuthorizationUrl(parameters);
                    //MessageBox.Show(authorizationUrl);
                    System.Diagnostics.Process.Start(authorizationUrl);
                    //Console.WriteLine(authorizationUrl);
                    //Console.WriteLine("Please visit the URL above to authorize your OAuth "
                    //  + "request token.  Once that is complete, type in your access code to "
                    //  + "continue...");
                    //parameters.AccessCode = Console.ReadLine();
                    //parameters.AccessCode = Microsoft.VisualBasic.Interaction.InputBox("Title", "Prompt", "Default", 0, 0);
                
                    //open up the user input from - use ShowDialog b/c is waits for user to click ok button before continue - aka: async
                    clsGlobals.UserInputNotes = new frmUserInputNotes();
                    clsGlobals.UserInputNotes.ShowDialog();

                    parameters.AccessCode = clsGlobals.strUserInputGoogleAccessCode.ToString().Trim();
                    ////////////////////////////////////////////////////////////////////////////
                    // STEP 4: Get the Access Token
                    ////////////////////////////////////////////////////////////////////////////

                    // Once the user authorizes with Google, the request token can be exchanged
                    // for a long-lived access token.  If you are building a browser-based
                    // application, you should parse the incoming request token from the url and
                    // set it in OAuthParameters before calling GetAccessToken().
                    OAuthUtil.GetAccessToken(parameters);
                    accessToken = parameters.AccessToken;
                    //Console.WriteLine("OAuth Access Token: " + accessToken);                
                }
                else
                {
                    //set the bool to true so the dialog doesn't show the label and textbox for the access code
                    clsGlobals.boolGoogleHasAccessCode = true;
                    //open up the user input from - use ShowDialog b/c is waits for user to click ok button before continue - aka: async
                    clsGlobals.UserInputNotes = new frmUserInputNotes();
                    clsGlobals.UserInputNotes.ShowDialog();
                }


                ////////////////////////////////////////////////////////////////////////////
                // STEP 5: Make an OAuth authorized request to Google
                ////////////////////////////////////////////////////////////////////////////

                // Initialize the variables needed to make the request
                GOAuth2RequestFactory requestFactory =
                    new GOAuth2RequestFactory(null, "MySpreadsheetIntegration-v1", parameters);
                service = new SpreadsheetsService("MySpreadsheetIntegration-v1");
                service.RequestFactory = requestFactory;

                // Make the request to Google
                // See other portions of this guide for code to put here...
            }
            catch (Exception ex)
            {
                clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }




        // this method updates a row in the google spreadsheet // MySpreadsheetIntegration-v1
        public static void AddRowToGoogleSpreadsheet()
        {
            try
            {
                clsUtransEditorStaticClass.AuthorizeRequestGoogleSheetsAPI();

                SpreadsheetEntry spreadsheet = null;
            

                //string docKey = "1A5be20hhg2fe2AGWe6BpeJmtdI-_ITcTSF1WiIVbyTY";
                //string gDocsURL = "https://docs.google.com/spreadsheet/ccc?key={0}";
                //string docURL = String.Format(gDocsURL, docKey);

                //FeedQuery singleQuery = new FeedQuery();
                //singleQuery.Uri = new Uri(docKey);

                //AtomFeed newFeed = service.Query(singleQuery);
                //AtomEntry retrievedEntry = newFeed.Entries[0];

                //MessageBox.Show(retrievedEntry.Title.Text);


                ////SpreadsheetsService service = new SpreadsheetsService("MySpreadsheetIntegration-v1");

                // TODO: Authorize the service object for a specific user (see other sections)
                // Instantiate a SpreadsheetQuery object to retrieve spreadsheets.
                SpreadsheetQuery query = new SpreadsheetQuery();

                // Make a request to the API and get all spreadsheets.
                SpreadsheetFeed feed = service.Query(query);

                if (feed.Entries.Count == 0)
                {
                    // TODO: There were no spreadsheets, act accordingly.
                    MessageBox.Show("google didn't find any spreadsheets.");
                }

                // TODO: Choose a spreadsheet more intelligently based on your
                // app's needs.
                ////SpreadsheetEntry spreadsheet = (SpreadsheetEntry)feed.Entries[0];
                ////MessageBox.Show(spreadsheet.Title.Text);
                //Console.WriteLine(spreadsheet.Title.Text);

                //loop through the feeds to find the spreadsheet with the correct name
                for (int i = 0; i < feed.Entries.Count; i++)
                {

                    spreadsheet = (SpreadsheetEntry)feed.Entries[i];
                    if (spreadsheet.Title.Text == "UtransEditorCountyNotificationList")
                    {
                        //MessageBox.Show("found it!!!");
                        break;
                    }
                    else
                    {
                        //MessageBox.Show("Didn't find a spreadsheet with the name UtransEditorCountyNotificationList.  The info was not saved to a google spreadsheet.");
                    }

                }

                // Get the first worksheet of the first spreadsheet.
                // TODO: Choose a worksheet more intelligently based on your
                // app's needs.
                WorksheetFeed wsFeed = spreadsheet.Worksheets;
                WorksheetEntry worksheet = (WorksheetEntry)wsFeed.Entries[0];

                // Define the URL to request the list feed of the worksheet.
                AtomLink listFeedLink = worksheet.Links.FindService(GDataSpreadsheetsNameTable.ListRel, null);

                // Fetch the list feed of the worksheet.
                ListQuery listQuery = new ListQuery(listFeedLink.HRef.ToString());
                ListFeed listFeed = service.Query(listQuery);

                // Create a local representation of the new row.
                ListEntry row = new ListEntry();

                row.Elements.Add(new ListEntry.Custom() { LocalName = "logdate", Value = DateTime.Now.ToString("d") });
                row.Elements.Add(new ListEntry.Custom() { LocalName = "countyid", Value = clsGlobals.strCountyID });
                row.Elements.Add(new ListEntry.Custom() { LocalName = "agrcnotes", Value = clsGlobals.strUserInputForSpreadsheet.ToString().Trim() });
                row.Elements.Add(new ListEntry.Custom() { LocalName = "agrcsegment", Value = clsGlobals.strAgrcSegment.ToString().Trim() });
                row.Elements.Add(new ListEntry.Custom() { LocalName = "cntysegment", Value = clsGlobals.strCountySegmentTrimed });
                row.Elements.Add(new ListEntry.Custom() { LocalName = "leftfrom", Value = clsGlobals.strCountyFROMADDR_L });
                row.Elements.Add(new ListEntry.Custom() { LocalName = "leftto", Value = clsGlobals.strCountyTOADDR_L });
                row.Elements.Add(new ListEntry.Custom() { LocalName = "rightfrom", Value = clsGlobals.strCountyFROMADDR_R });
                row.Elements.Add(new ListEntry.Custom() { LocalName = "rightto", Value = clsGlobals.strCountyTOADDR_R });
                row.Elements.Add(new ListEntry.Custom() { LocalName = "city", Value = clsGlobals.strGoogleSpreadsheetCityField });
                // Send the new row to the API for insertion.
                service.Insert(listFeed, row);
            }
            catch (Exception ex)
            {
                clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }


    }
}
