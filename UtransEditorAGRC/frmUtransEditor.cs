using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Editor;
using ESRI.ArcGIS.EditorExt;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.GeoDatabaseUI;
//using NLog;
//using NLog.Config;

namespace UtransEditorAGRC
{

    public partial class frmUtransEditor : Form
    {
        //set up nlogger for catching errors
        //clsGlobals.logger = LogManager.GetCurrentClassLogger();
        
        
        //form-wide variables...
        // create a list of controls that contains address pieces for managing edits
        private List<Control> ctrlList = new List<Control>();

        string txtUtransInitialFROMADDR_L;
        string txtUtransInitialL_TAdd;
        string txtUtransInitialFROMADDR_R;
        string txtUtransInitialTOADDR_R;
        string txtUtransInitialPreDir;
        string txtUtransInitialStName;
        string txtUtransInitialStType;
        string txtUtransInitialPOSTDIR;
        string txtUtransInitialA1_NAME;
        string txtUtransInitialA1_POSTTYPE;
        string txtUtransInitialA2_NAME;
        string txtUtransInitialA2_POSTTYPE;
        string txtUtransInitialA1_PREDIR;
        string txtUtransInitialA1_POSTDIR;
        string txtUtransInitialA2_PREDIR;
        string txtUtransInitialA2_POSTDIR;
        string txtUtransInitialAN_NAME;
        string txtUtransInitialAN_POSTDIR;
        int intUtransInitialCartoCodeIndex;
        string strGoogleLogLeftTo;
        string strGoogleLogLeftFrom;
        string strGoogleLogRightTo;
        string strGoogleLogRightFrom;

        //get the selected feature(s) from the dfc fc
        IFeatureSelection arcFeatureSelection; // = clsGlobals.arcGeoFLayerDfcResult as IFeatureSelection;
        ISelectionSet arcSelSet; // = arcFeatureSelection.SelectionSet;
        IActiveView arcActiveView;
        IFeatureLayerDefinition arcFeatureLayerDef;
        IQueryFilter arcQFilterLabelCount;
        IFeature arcCountyFeature; // i gave this form-scope becuase i need access to this varialbe in the onclick method to pass it into the google spreadsheet get city code method 

        //create an italic font for lables - to use where data does not match
        Font fontLabelHasEdits = new Font("Microsoft Sans Serif", 8.0f, FontStyle.Bold);

        //create an italic font for lables - to use where data does not match
        Font fontLabelRegular = new Font("Microsoft Sans Serif", 8.0f, FontStyle.Regular);

        //get the objectids from dfc layer for selecting on corresponding layer
        string strCountyOID = "";
        string strUtransOID = "";
        string strChangeType = "";
        string strDFC_RESULT_oid = "";
        string strUtransCartoCode = "";
        string strCountyCartoCode = "";

        bool boolVerticesOn = false;

        IMap arcMapp;

        ICompositeGraphicsLayer2 pComGraphicsLayer;
        ICompositeLayer pCompositeLayer;
        ILayer pLayer;

        //initialize the form
        public frmUtransEditor()
        {
            InitializeComponent();
            //timer1.Interval = _blinkFrequency;
        }


        //form load event
        private void frmUtransEditor_Load(object sender, EventArgs e)
        {
            try
            {
                //test if the logger is working
                //LogManager.Configuration = new XmlLoggingConfiguration("c:\\Users\\gbunce\\documents\\visual studio 2013\\Projects\\UtransEditorAGRC\\UtransEditorAGRC\\NLog.config");
                //clsGlobals.logger = LogManager.GetCurrentClassLogger();
                //clsGlobals.logger.Trace("test on load");

                //get the current document
                IMxDocument arcMxDoc = clsGlobals.arcApplication.Document as IMxDocument;

                //get the focus map
                arcMapp = arcMxDoc.FocusMap;

                arcActiveView = arcMapp as IActiveView;
                arcMapp.ClearSelection();

                //setup event handler for when the  map selection changes
                ((IEditEvents_Event)clsGlobals.arcEditor).OnSelectionChanged += new IEditEvents_OnSelectionChangedEventHandler(frmUtransEditor_OnSelectionChanged);

                //get the editor workspace
                IWorkspace arcWspace = clsGlobals.arcEditor.EditWorkspace;

                //set the bool to false so the user imput form will ask the user to provide a google access code
                clsGlobals.boolGoogleHasAccessCode = false;

                //if the workspace is not remote (sde), exit the sub - if it's sde get the version name
                if (arcWspace.Type != esriWorkspaceType.esriRemoteDatabaseWorkspace) 
                { 
                    return; 
                }
                else
                {
                    //IVersionedWorkspace versionedWorkspace = (IVersionedWorkspace)arcWspace;
                    IVersion2 arcVersion = (IVersion2)arcWspace;

                    lblVersionName.Text = arcVersion.VersionName.ToString();

                    //show message box so user knows what version they are editing on the utrans database
                    MessageBox.Show("You are editing the UTRANS database using the following version: " + arcVersion.VersionName, "Utrans Version", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                //get the workspace as an IWorkspaceEdit
                IWorkspaceEdit arcWspaceEdit = clsGlobals.arcEditor.EditWorkspace as IWorkspaceEdit;

                //get the workspace as a feature workspace
                IFeatureWorkspace arcFeatWspace = arcWspace as IFeatureWorkspace;

                //////get the current document
                ////IMxDocument arcMxDoc = clsGlobals.arcApplication.Document as IMxDocument;

                //////get the focus map
                ////arcMapp = arcMxDoc.FocusMap;

                ////arcActiveView = arcMapp as IActiveView;
                ////arcMapp.ClearSelection();

                //get reference to the layers in the map
                //clear out any reference to the utrans street layer
                clsGlobals.arcGeoFLayerUtransStreets = null;

                //loop through the map layers and get the utrans.statewidestreets, the county roads data, and the detect feature change fc - all into IGeoFeatureLayer(s)
                for (int i = 0; i < arcMapp.LayerCount; i++)
                {
                    if (arcMapp.get_Layer(i) is IGeoFeatureLayer)
                    {
                        try
                        {
                            IFeatureLayer arcFLayer = arcMapp.get_Layer(i) as IFeatureLayer;
                            IFeatureClass arcFClass = arcFLayer.FeatureClass;
                            IObjectClass arcObjClass = arcFClass as IObjectClass;
                            if (arcObjClass.AliasName.ToString().ToUpper() == "UTRANS.TRANSADMIN.ROADS_EDIT")
                            {
                                clsGlobals.arcGeoFLayerUtransStreets = arcMapp.get_Layer(i) as IGeoFeatureLayer;
                                //MessageBox.Show("referenced utrans streets");
                            }
                            if (arcObjClass.AliasName.ToString().ToUpper() == "COUNTY_STREETS")
                            {
                                clsGlobals.arcGeoFLayerCountyStreets = arcMapp.get_Layer(i) as IGeoFeatureLayer;
                                //MessageBox.Show("referenced county streets");
                            }
                            if (arcObjClass.AliasName.ToString().ToUpper() == "DFC_RESULT")
                            {
                                clsGlobals.arcGeoFLayerDfcResult = arcMapp.get_Layer(i) as IGeoFeatureLayer;
                                //MessageBox.Show("referenced dfc results");
                            }
                            if (arcObjClass.AliasName.ToString() == "SGID10.LOCATION.AddressSystemQuadrants")
                            {
                                clsGlobals.arcFLayerAddrSysQuads = arcMapp.get_Layer(i) as IFeatureLayer;
                            }
                            if (arcObjClass.AliasName.ToString() == "SGID10.BOUNDARIES.ZipCodes")
                            {
                                clsGlobals.arcFLayerZipCodes = arcMapp.get_Layer(i) as IFeatureLayer;
                            }
                            if (arcObjClass.AliasName.ToString() == "SGID10.BOUNDARIES.Counties")
                            {
                                clsGlobals.arcFLayerCounties = arcMapp.get_Layer(i) as IFeatureLayer;
                            }
                            if (arcObjClass.AliasName.ToString() == "SGID10.BOUNDARIES.Municipalities")
                            {
                                clsGlobals.arcFLayerMunicipalities = arcMapp.get_Layer(i) as IFeatureLayer;
                            }
                            if (arcObjClass.AliasName.ToString() == "SGID10.BOUNDARIES.MetroTownships")
                            {
                                clsGlobals.arcFLayerMetroTwnShips = arcMapp.get_Layer(i) as IFeatureLayer;
                            }
                        }
                        catch (Exception) { }//in case there is an error looping through layers (sometimes on group layers or dynamic xy layers), just keep going
                        
                    }
                }

                //shouldn't need this code as i've changed the code to check for these layers before i enable the button
                //check that the needed layers are in the map - if not, show message and close the form
                if (clsGlobals.arcGeoFLayerCountyStreets == null)
                {
                    MessageBox.Show("A needed layer is Missing in the map." + Environment.NewLine + "Please add 'COUNTYSTREETS' in order to continue.", "Missing Layer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }
                else if (clsGlobals.arcGeoFLayerDfcResult == null)
                {
                    MessageBox.Show("A needed layer is Missing in the map." + Environment.NewLine + "Please add 'DFC_RESULT' in order to continue.", "Missing Layer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }
                else if (clsGlobals.arcGeoFLayerUtransStreets == null)
                {
                    MessageBox.Show("A needed layer is Missing in the map." + Environment.NewLine + "Please add 'UTRANS.TRANSADMIN.ROADS_EDIT' in order to continue.", "Missing Layer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }
                else if (clsGlobals.arcFLayerAddrSysQuads == null)
                {
                    MessageBox.Show("A needed layer is Missing in the map." + Environment.NewLine + "Please add 'SGID10.LOCATION.AddressSystemQuadrants' in order to continue.", "Missing Layer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }
                else if (clsGlobals.arcFLayerZipCodes == null)
                {
                    MessageBox.Show("A needed layer is Missing in the map." + Environment.NewLine + "Please add 'SGID10.BOUNDARIES.ZipCodes' in order to continue.", "Missing Layer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }
                else if (clsGlobals.arcFLayerCounties == null)
                {
                    MessageBox.Show("A needed layer is Missing in the map." + Environment.NewLine + "Please add 'SGID10.BOUNDARIES.Counties' in order to continue.", "Missing Layer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }
                else if (clsGlobals.arcFLayerMunicipalities == null)
                {
                    MessageBox.Show("A needed layer is Missing in the map." + Environment.NewLine + "Please add 'SGID10.BOUNDARIES.Municipalities' in order to continue.", "Missing Layer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }
                else if (clsGlobals.arcFLayerMetroTwnShips == null)
                {
                    MessageBox.Show("A needed layer is Missing in the map." + Environment.NewLine + "Please add 'SGID10.BOUNDARIES.MetroTownships' in order to continue.", "Missing Layer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }

                //clear the selection in the map, so we can start fresh with the tool and user's inputs
                arcMapp.ClearSelection();
                
                //refresh the map on the selected features
                //arcActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);
                arcActiveView.Refresh();


                //add textboxes to the control list
                ctrlList.Add(this.txtCountyAN_NAME);
                ctrlList.Add(this.txtCountyAN_POSTDIR);
                ctrlList.Add(this.txtCountyA1_PREDIR);
                ctrlList.Add(this.txtCountyA1_NAME);
                ctrlList.Add(this.txtCountyA1_POSTTYPE);
                ctrlList.Add(this.txtCountyA1_POSTDIR);
                ctrlList.Add(this.txtCountyA2_PREDIR);
                ctrlList.Add(this.txtCountyA2_NAME);
                ctrlList.Add(this.txtCountyA2_POSTTYPE);
                ctrlList.Add(this.txtCountyA2_POSTDIR);
                ctrlList.Add(this.txtCountyFROMADDR_L);
                ctrlList.Add(this.txtCountyTOADDR_L);
                ctrlList.Add(this.txtCountyPreDir);
                ctrlList.Add(this.txtCountyFROMADDR_R);
                ctrlList.Add(this.txtCountyTOADDR_R);
                ctrlList.Add(this.txtCountyStName);
                ctrlList.Add(this.txtCountyStType);
                ctrlList.Add(this.txtCountyPOSTDIR);
                ctrlList.Add(this.txtUtranFROMADDR_L);
                ctrlList.Add(this.txtUtranTOADDR_L);
                ctrlList.Add(this.txtUtranPreDir);
                ctrlList.Add(this.txtUtranFROMADDR_R);
                ctrlList.Add(this.txtUtranTOADDR_R);
                ctrlList.Add(this.txtUtransAN_NAME);
                ctrlList.Add(this.txtUtransAN_POSTDIR);
                ctrlList.Add(this.txtUtransA1_PREDIR);
                ctrlList.Add(this.txtUtransA1_NAME);
                ctrlList.Add(this.txtUtransA1_POSTTYPE);
                ctrlList.Add(this.txtUtransA1_POSTDIR);
                ctrlList.Add(this.txtUtransA2_PREDIR);
                ctrlList.Add(this.txtUtransA2_NAME);
                ctrlList.Add(this.txtUtransA2_POSTTYPE);
                ctrlList.Add(this.txtUtransA2_POSTDIR);
                ctrlList.Add(this.txtUtranStName);
                ctrlList.Add(this.txtUtranStType);
                ctrlList.Add(this.txtUtranPOSTDIR);


                //make sure the backcolor of each color is white
                for (int i = 0; i < ctrlList.Count; i++)
                {
                    Control ctrl = ctrlList.ElementAt(i);
                    ctrl.BackColor = Color.White;
                    ctrl.Text = "";
                }

                //update the feature count label on the form
                arcFeatureLayerDef = clsGlobals.arcGeoFLayerDfcResult as IFeatureLayerDefinition;
                arcQFilterLabelCount = new QueryFilter();
                arcQFilterLabelCount.WhereClause = arcFeatureLayerDef.DefinitionExpression;

                int intDfcCount = clsGlobals.arcGeoFLayerDfcResult.DisplayFeatureClass.FeatureCount(arcQFilterLabelCount);
                lblCounter.Text = intDfcCount.ToString();

            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                //throw;
            }
        }




        //this event is called when the selection changes in the map
        private void frmUtransEditor_OnSelectionChanged()
        {
            try
            {
                //test if the logger is working
                //clsGlobals.logger.Trace("test on selection changed");

                //check if the form is open/visible - if not, don't go through this code
                if (clsGlobals.UtransEdior2.Visible == true)
                {
                    //do nothing... proceed into the method
                    //MessageBox.Show("form is visible");
                }
                else
                {
                    //exit out of the method becuase the form is not open
                    return;
                    //MessageBox.Show("form is not visible");
                }

                //hide the copy new segment button
                btnCopyNewSegment.Hide();
                chkShowVertices.Hide();

                //reset the cartocode combobox to nothing
                cboCartoCode.SelectedIndex = -1;
                cboStatusField.SelectedIndex = 0; // show the completed value by default
                groupBox5.Font = fontLabelRegular;

                //enable the textboxes - in case last record was "N" and were disabled
                ////////txtUtranFROMADDR_L.ReadOnly = false;
                ////////txtUtranTOADDR_L.ReadOnly = false;
                ////////txtUtranPreDir.ReadOnly = false;
                ////////txtUtranFROMADDR_R.ReadOnly = false;
                ////////txtUtranTOADDR_R.ReadOnly = false;
                ////////txtUtransAN_NAME.ReadOnly = false;
                ////////txtUtransAN_POSTDIR.ReadOnly = false;
                ////////txtUtransA1_NAME.ReadOnly = false;
                ////////txtUtransA1_POSTTYPE.ReadOnly = false;
                ////////txtUtransA2_NAME.ReadOnly = false;
                ////////txtUtransA2_POSTTYPE.ReadOnly = false;
                ////////txtUtranStName.ReadOnly = false;
                ////////txtUtranStType.ReadOnly = false;
                ////////txtUtranPOSTDIR.ReadOnly = false;

                lblLeftFrom.Enabled = true;
                lblRightFrom.Enabled = true;
                lblLeftTo.Enabled = true;
                lblRightTo.Enabled = true;
                lblPreDir.Enabled = true;
                lblStName.Enabled = true;
                lblStType.Enabled = true;
                lblPOSTDIR.Enabled = true;
                lblAN_NAME.Enabled = true;
                lblAN_POSTDIR.Enabled = true;
                lblA1_NAME.Enabled = true;
                lblA1_PREDIR.Enabled = true;
                lblA1_POSTTYPE.Enabled = true;
                lblA1_POSTDIR.Enabled = true;
                lblA2_PREDIR.Enabled = true;
                lblA2_NAME.Enabled = true;
                lblA2_POSTTYPE.Enabled = true;
                lblA2_POSTDIR.Enabled = true;

                //disable the save to utrans button - until a change has been detected
                //btnSaveToUtrans.Enabled = false;


                //make sure the backcolor of each color is white
                for (int i = 0; i < ctrlList.Count; i++)
                {
                    Control ctrl = ctrlList.ElementAt(i);
                    ctrl.BackColor = Color.White;
                    ctrl.Text = "";
                }

                // revert title to default - incase previous was a udot street
                groupBoxUtransSeg.Text = "Selected UTRANS Road Segment";

                //get the objectids from dfc layer for selecting on corresponding layer
                strCountyOID = "";
                strUtransOID = "";
                strChangeType = "";
                strDFC_RESULT_oid = "";


                //clear utrans existing variables - for reuse
                txtUtransInitialFROMADDR_L = null;
                txtUtransInitialL_TAdd = null;;
                txtUtransInitialFROMADDR_R = null;
                txtUtransInitialTOADDR_R = null;
                txtUtransInitialPreDir = null;
                txtUtransInitialStName = null;
                txtUtransInitialStType = null;
                txtUtransInitialPOSTDIR = null;;
                txtUtransInitialA1_NAME = null;
                txtUtransInitialA1_POSTTYPE = null;
                txtUtransInitialA2_NAME = null;
                txtUtransInitialA2_POSTTYPE = null;
                txtUtransInitialA1_PREDIR = null;
                txtUtransInitialA1_POSTDIR = null;
                txtUtransInitialA2_PREDIR = null;
                txtUtransInitialA2_POSTDIR = null;
                txtUtransInitialAN_NAME = null;
                txtUtransInitialAN_POSTDIR = null;

                arcFeatureSelection = clsGlobals.arcGeoFLayerDfcResult as IFeatureSelection;
                arcSelSet = arcFeatureSelection.SelectionSet;

                //check record is selected in the dfc fc
                if (arcSelSet.Count == 1)
                {
                    //get a cursor of the selected features
                    ICursor arcCursor;
                    arcSelSet.Search(null, false, out arcCursor);

                    //get the first row (there should only be one)
                    IRow arcRow = arcCursor.NextRow();

                    //get the objectids from dfc layer for selecting on corresponding layer
                    strCountyOID = arcRow.get_Value(arcRow.Fields.FindField("UPDATE_FID")).ToString();
                    strUtransOID = arcRow.get_Value(arcRow.Fields.FindField("BASE_FID")).ToString();
                    strChangeType = arcRow.get_Value(arcRow.Fields.FindField("CHANGE_TYPE")).ToString();
                    strDFC_RESULT_oid = arcRow.get_Value(arcRow.Fields.FindField("OBJECTID")).ToString();

                    //populate the change type on the top of the form
                    switch (strChangeType)
                    {
                        case "N":
                            if (strUtransOID == "-1")
                            {
                                lblChangeType.Text = "New";
                            }
                            else
                            {
                                lblChangeType.Text = "New ( Now in UTRANS - Please Verify Attributes and Click Save )";
                            }
                            //lblChangeType.Text = "New";
                            break;
                        case "S":
                            lblChangeType.Text = "Spatial";
                            break;
                        case "A":
                            lblChangeType.Text = "Attribute";
                            break;
                        case "SA":
                            lblChangeType.Text = "Spatial and Attribute";
                            break;
                        case "NC":
                            lblChangeType.Text = "No Change";
                            break;
                        case "D":
                            lblChangeType.Text = "Delation";
                            break;
                        default:
                            lblChangeType.Text = "Unknown";
                            break;
                    }


                    //get the corresponding features
                    IQueryFilter arcCountyQueryFilter = new QueryFilter();
                    arcCountyQueryFilter.WhereClause = "OBJECTID = " + strCountyOID.ToString();
                    //MessageBox.Show("County OID: " + strCountyOID.ToString());

                    IQueryFilter arcUtransQueryFilter = new QueryFilter();
                    arcUtransQueryFilter.WhereClause = "OBJECTID = " + strUtransOID.ToString();
                    //can check if oid = -1 then it's a new record so maybe make backround color on form green or something until user says okay to import, then populate
                    //MessageBox.Show("Utrans OID: " + strUtransOID.ToString());

                    ////// feature cursor using com releaser
                    ////using (ComReleaser comReleaserCountyFeatCur = new ComReleaser())
                    ////{ 
                    ////    IFeatureCursor arcCountyFeatCursor = clsGlobals.arcGeoFLayerCountyStreets.Search(arcCountyQueryFilter, true);
                    ////    comReleaserCountyFeatCur.ManageLifetime(arcCountyFeatCursor);
                    ////    arcCountyFeature = (IFeature)arcCountyFeatCursor.NextFeature();                    
                    ////}
                    IFeatureCursor arcCountyFeatCursor = clsGlobals.arcGeoFLayerCountyStreets.Search(arcCountyQueryFilter, true);
                    arcCountyFeature = (IFeature)arcCountyFeatCursor.NextFeature();  

                    ////// feature cursor using com releaser
                    ////using (ComReleaser comReleaserUtransFeatCur = new ComReleaser())
                    ////{
                    ////    IFeatureCursor arcUtransFeatCursor = clsGlobals.arcGeoFLayerUtransStreets.Search(arcUtransQueryFilter, true);
                    ////    comReleaserUtransFeatCur.ManageLifetime(arcUtransFeatCursor);
                    ////    IFeature arcUtransFeature = (IFeature)arcUtransFeatCursor.NextFeature();                        
                    ////}
                    IFeatureCursor arcUtransFeatCursor = clsGlobals.arcGeoFLayerUtransStreets.Search(arcUtransQueryFilter, true);
                    IFeature arcUtransFeature = (IFeature)arcUtransFeatCursor.NextFeature();     


                    //update the textboxes with the selected dfc row//
                    //make sure the query returned results for county roads
                    if (arcCountyFeature != null)
                    {
                        //update all the text boxes
                        foreach (var ctrl in ctrlList)
                        {
                            if (arcCountyFeature.Fields.FindFieldByAliasName(ctrl.Tag.ToString()) > -1)
                            {
                                ctrl.Text = arcCountyFeature.get_Value(arcCountyFeature.Fields.FindFieldByAliasName(ctrl.Tag.ToString())).ToString().ToUpper();
                            }
                        }

                        //get the county's cartocode
                        //strCountyCartoCode = arcCountyFeature.get_Value(arcCountyFeature.Fields.FindFieldByAliasName("CARTOCODE")).ToString().Trim();
                        clsGlobals.strCountyID = arcCountyFeature.get_Value(arcCountyFeature.Fields.FindField("COUNTY_L")).ToString().Trim();
                    }


                    //make sure the query returned results for utrans roads
                    if (arcUtransFeature != null)
                    {
                        //update all the text boxes
                        foreach (var ctrl in ctrlList)
                        {
                            if (arcUtransFeature.Fields.FindFieldByAliasName(ctrl.Tag.ToString())>-1)
                            {
                                ctrl.Text = arcUtransFeature.get_Value(arcUtransFeature.Fields.FindFieldByAliasName(ctrl.Tag.ToString())).ToString();
                            }
                        }

                        //get utrans cartocode
                        strUtransCartoCode = arcUtransFeature.get_Value(arcUtransFeature.Fields.FindField("CARTOCODE")).ToString().Trim();

                        //also check if u_dot street
                        string checkIfUdotStreet = arcUtransFeature.get_Value(arcUtransFeature.Fields.FindField("DOT_RTNAME")).ToString();
                        if (checkIfUdotStreet != "")
                        {
                            groupBoxUtransSeg.Text = groupBoxUtransSeg.Text + " (UDOT STREET)";
                        }
                    }

                    //call check differnces method
                    checkTextboxDifferneces();

                    //populate the cartocode combobox
                    populateCartoCodeComboBox();

                }
                else //if the user selects more or less than one record in the dfc fc - clear out the textboxes
                {
                    //clear out the textboxes so nothing is populated
                    foreach (var ctrl in ctrlList)
                    {
                        ctrl.Text = "";
                    }

                    //change the attribute type
                    lblChangeType.Text = "Please select one feature from DFC_RESULT layer.";
                }

                //refresh the map on the selected features
                arcActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);

                //populate variables to hold the initial textbox text for utrans streets - in case the user wants to revert to it
                txtUtransInitialFROMADDR_L = txtUtranFROMADDR_L.Text;
                txtUtransInitialL_TAdd = txtUtranTOADDR_L.Text;
                txtUtransInitialFROMADDR_R = txtUtranFROMADDR_R.Text;
                txtUtransInitialTOADDR_R = txtUtranTOADDR_R.Text;
                txtUtransInitialPreDir = txtUtranPreDir.Text;
                txtUtransInitialStName = txtUtranStName.Text;
                txtUtransInitialStType = txtUtranStType.Text;
                txtUtransInitialPOSTDIR = txtUtranPOSTDIR.Text;
                txtUtransInitialA1_NAME = txtUtransA1_NAME.Text;
                txtUtransInitialA1_POSTTYPE = txtUtransA1_POSTTYPE.Text;
                txtUtransInitialA2_NAME = txtUtransA2_NAME.Text;
                txtUtransInitialA2_POSTTYPE = txtUtransA2_POSTTYPE.Text;
                txtUtransInitialA1_PREDIR = txtUtransA1_PREDIR.Text;
                txtUtransInitialA1_POSTDIR = txtUtransA1_POSTDIR.Text;
                txtUtransInitialA2_PREDIR = txtUtransA2_PREDIR.Text;
                txtUtransInitialA2_POSTDIR = txtUtransA2_POSTDIR.Text;
                txtUtransInitialAN_NAME = txtUtransAN_NAME.Text;
                txtUtransInitialAN_POSTDIR = txtUtransAN_POSTDIR.Text;

                //revert labels back to regular (non-italic)
                lblAN_NAME.Font = fontLabelRegular;
                lblAN_POSTDIR.Font = fontLabelRegular;
                lblA1_PREDIR.Font = fontLabelRegular;
                lblA1_NAME.Font = fontLabelRegular;
                lblA1_POSTTYPE.Font = fontLabelRegular;
                lblA1_POSTDIR.Font = fontLabelRegular;
                lblA2_PREDIR.Font = fontLabelRegular;
                lblA2_NAME.Font = fontLabelRegular;
                lblA2_POSTTYPE.Font = fontLabelRegular;
                lblA2_POSTDIR.Font = fontLabelRegular;
                lblLeftFrom.Font = fontLabelRegular;
                lblLeftTo.Font = fontLabelRegular;
                lblPreDir.Font = fontLabelRegular;
                lblRightFrom.Font = fontLabelRegular;
                lblRightTo.Font = fontLabelRegular;
                lblStName.Font = fontLabelRegular;
                lblStType.Font = fontLabelRegular;
                lblPOSTDIR.Font = fontLabelRegular;

                //if it's a new record
                if (strChangeType == "N" & strUtransOID == "-1")
                {
                    //make the textboxes a light red color, indicating there's no attributes for this feature
                    txtUtranFROMADDR_L.BackColor = Color.LightGray;
                    txtUtranTOADDR_L.BackColor = Color.LightGray;
                    txtUtranPreDir.BackColor = Color.LightGray;
                    txtUtranFROMADDR_R.BackColor = Color.LightGray;
                    txtUtranTOADDR_R.BackColor = Color.LightGray;
                    txtUtransAN_NAME.BackColor = Color.LightGray;
                    txtUtransAN_POSTDIR.BackColor = Color.LightGray;
                    txtUtransA1_PREDIR.BackColor = Color.LightGray;
                    txtUtransA1_NAME.BackColor = Color.LightGray;
                    txtUtransA1_POSTTYPE.BackColor = Color.LightGray;
                    txtUtransA1_POSTDIR.BackColor = Color.LightGray;
                    txtUtransA2_PREDIR.BackColor = Color.LightGray;
                    txtUtransA2_NAME.BackColor = Color.LightGray;
                    txtUtransA2_POSTTYPE.BackColor = Color.LightGray;
                    txtUtransA2_POSTDIR.BackColor = Color.LightGray;
                    txtUtranStName.BackColor = Color.LightGray;
                    txtUtranStType.BackColor = Color.LightGray;
                    txtUtranPOSTDIR.BackColor = Color.LightGray;

                    //i could change this to loop the control list and update all the controls with a tag like utrans
                    ////////txtUtranFROMADDR_L.ReadOnly = true;
                    ////////txtUtranTOADDR_L.ReadOnly = true;
                    ////////txtUtranPreDir.ReadOnly = true;
                    ////////txtUtranFROMADDR_R.ReadOnly = true;
                    ////////txtUtranTOADDR_R.ReadOnly = true;
                    ////////txtUtransAN_NAME.ReadOnly = true;
                    ////////txtUtransAN_POSTDIR.ReadOnly = true;
                    ////////txtUtransA1_NAME.ReadOnly = true;
                    ////////txtUtransA1_POSTTYPE.ReadOnly = true;
                    ////////txtUtransA2_NAME.ReadOnly = true;
                    ////////txtUtransA2_POSTTYPE.ReadOnly = true;
                    ////////txtUtranStName.ReadOnly = true;
                    ////////txtUtranStType.ReadOnly = true;
                    ////////txtUtranPOSTDIR.ReadOnly = true;

                    lblLeftFrom.Enabled = false;
                    lblRightFrom.Enabled = false;
                    lblLeftTo.Enabled = false;
                    lblRightTo.Enabled = false;
                    lblPreDir.Enabled = false;
                    lblStName.Enabled = false;
                    lblStType.Enabled = false;
                    lblPOSTDIR.Enabled = false;
                    lblAN_NAME.Enabled = false;
                    lblAN_POSTDIR.Enabled = false;
                    lblA1_PREDIR.Enabled = false;
                    lblA1_NAME.Enabled = false;
                    lblA1_POSTTYPE.Enabled = false;
                    lblA1_POSTDIR.Enabled = false;
                    lblA2_PREDIR.Enabled = false;
                    lblA2_NAME.Enabled = false;
                    lblA2_POSTTYPE.Enabled = false;
                    lblA2_POSTDIR.Enabled = false;

                    //show get new feature button and make save button not enabled
                    btnCopyNewSegment.Visible = true;
                    chkShowVertices.Visible = true;
                    //btnSaveToUtrans.Enabled = false;
                }

            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }



        //populate the cartocode combobox
        private void populateCartoCodeComboBox() 
        {
            try
            {
                //parse the cartocodes to get the values before the hyphen
                //MessageBox.Show("Cartocodes... Utrans: " + strUtransCartoCode + ", County: " + strCountyCartoCode);

                switch (strUtransCartoCode)
                {
                    case "1":
                        //get a refernce to cartocode to see if there will be edits (make it bold on the event handler if there will be edits)
                        intUtransInitialCartoCodeIndex = 0;
                        cboCartoCode.SelectedIndex = 0;
                        break;
                    case "2":
                        intUtransInitialCartoCodeIndex = 1;
                        cboCartoCode.SelectedIndex = 1;
                        break;
                    case "3":
                        intUtransInitialCartoCodeIndex = 2;
                        cboCartoCode.SelectedIndex = 2;
                        break;
                    case "4":
                        intUtransInitialCartoCodeIndex = 3;
                        cboCartoCode.SelectedIndex = 3;
                        break;
                    case "5":
                        intUtransInitialCartoCodeIndex = 4;
                        cboCartoCode.SelectedIndex = 4;
                        break;
                    case "6":
                        intUtransInitialCartoCodeIndex = 5;
                        cboCartoCode.SelectedIndex = 5;
                        break;
                    case "7":
                        intUtransInitialCartoCodeIndex = 6;
                        cboCartoCode.SelectedIndex = 6;
                        break;
                    case "8":
                        intUtransInitialCartoCodeIndex = 7;
                        cboCartoCode.SelectedIndex = 7;
                        break;
                    case "9":
                        intUtransInitialCartoCodeIndex = 8;
                        cboCartoCode.SelectedIndex = 8;
                        break;
                    case "10":
                        intUtransInitialCartoCodeIndex = 9;
                        cboCartoCode.SelectedIndex = 9;
                        break;
                    case "11":
                        intUtransInitialCartoCodeIndex = 10;
                        cboCartoCode.SelectedIndex = 10;
                        break;
                    case "12":
                        intUtransInitialCartoCodeIndex = 11;
                        cboCartoCode.SelectedIndex = 11;
                        break;
                    case "13":
                        intUtransInitialCartoCodeIndex = 12;
                        cboCartoCode.SelectedIndex = 12;
                        break;
                    case "14":
                        intUtransInitialCartoCodeIndex = 13;
                        cboCartoCode.SelectedIndex = 13;
                        break;
                    case "15":
                        intUtransInitialCartoCodeIndex = 14;
                        cboCartoCode.SelectedIndex = 14;
                        break;
                    case "99":
                        intUtransInitialCartoCodeIndex = 15;
                        cboCartoCode.SelectedIndex = 15;
                        break;
                    case "16":
                        intUtransInitialCartoCodeIndex = 16;
                        cboCartoCode.SelectedIndex = 16;
                        break;
                    default:
                        intUtransInitialCartoCodeIndex = -1;
                        cboCartoCode.SelectedIndex = -1;
                        break;
                }

                //get a refernce to cartocode to see if there will be edits (make it bold on the event handler if there will be edits)
                intUtransInitialCartoCodeIndex = cboCartoCode.SelectedIndex;
            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }




        //not using this right now, moved this to the text changed event on the textboxes
        private void checkTextboxDifferneces() 
        {
            try
            {
                if (txtCountyStName.Text.ToUpper().ToString().Trim() != txtUtranStName.Text.ToUpper().ToString().Trim())
                {
                    txtUtranStName.BackColor = Color.LightYellow;
                    txtCountyStName.BackColor = Color.LightYellow;
                    //boolHadDifferenceStName = true;
                }
                if (txtCountyStType.Text.ToUpper().ToString() != txtUtranStType.Text.ToUpper().ToString())
                {
                    txtUtranStType.BackColor = Color.LightYellow;
                    txtCountyStType.BackColor = Color.LightYellow;
                    //lblStType.Font = fontLabelDataMismatch;
                    //boolHadDifferenceStType = true;
                }
                if (txtCountyPOSTDIR.Text.ToUpper().ToString() != txtUtranPOSTDIR.Text.ToUpper().ToString())
                {
                    txtUtranPOSTDIR.BackColor = Color.LightYellow;
                    txtCountyPOSTDIR.BackColor = Color.LightYellow;
                    //lblPOSTDIR.Font = fontLabelDataMismatch;
                    //boolHadDifferencePOSTDIR = true;
                }
                if (txtCountyPreDir.Text.ToUpper().ToString() != txtUtranPreDir.Text.ToUpper().ToString())
                {
                    txtUtranPreDir.BackColor = Color.LightYellow;
                    txtCountyPreDir.BackColor = Color.LightYellow;
                    //lblPreDir.Font = fontLabelDataMismatch;
                    //boolHadDifferencePreDir = true;
                }
                if (txtCountyFROMADDR_L.Text.ToString() != txtUtranFROMADDR_L.Text.ToString())
                {
                    txtUtranFROMADDR_L.BackColor = Color.LightYellow;
                    txtCountyFROMADDR_L.BackColor = Color.LightYellow;
                    //lblLeftFrom.Font = fontLabelDataMismatch;
                    //capture the curent text - incase we want to revert to it
                    //txtUtransExistingFROMADDR_L = txtUtranFROMADDR_L.Text;
                    //boolHadDifferenceFROMADDR_L = true;
                }
                if (txtCountyTOADDR_L.Text.ToString() != txtUtranTOADDR_L.Text.ToString())
                {
                    txtUtranTOADDR_L.BackColor = Color.LightYellow;
                    txtCountyTOADDR_L.BackColor = Color.LightYellow;
                    //lblLeftTo.Font = fontLabelDataMismatch;
                    //boolHadDifferenceTOADDR_L = true;
                }
                if (txtCountyFROMADDR_R.Text.ToString() != txtUtranFROMADDR_R.Text.ToString())
                {
                    txtUtranFROMADDR_R.BackColor = Color.LightYellow;
                    txtCountyFROMADDR_R.BackColor = Color.LightYellow;
                    //lblRightFrom.Font = fontLabelDataMismatch;
                    //boolHadDifferenceFROMADDR_R = true;
                }
                if (txtCountyTOADDR_R.Text.ToString() != txtUtranTOADDR_R.Text.ToString())
                {
                    txtUtranTOADDR_R.BackColor = Color.LightYellow;
                    txtCountyTOADDR_R.BackColor = Color.LightYellow;
                    //lblRightTo.Font = fontLabelDataMismatch;
                    //boolHadDifferenceTOADDR_R = true;
                }
                if (txtCountyAN_NAME.Text.ToUpper().ToString() != txtUtransAN_NAME.Text.ToUpper().ToString())
                {
                    txtUtransAN_NAME.BackColor = Color.LightYellow;
                    txtCountyAN_NAME.BackColor = Color.LightYellow;
                    //lblAcsAlias.Font = fontLabelDataMismatch;
                    //boolHadDifferenceAcsAlias = true;
                }
                if (txtCountyAN_POSTDIR.Text.ToUpper().ToString() != txtUtransAN_POSTDIR.Text.ToUpper().ToString())
                {
                    txtUtransAN_POSTDIR.BackColor = Color.LightYellow;
                    txtCountyAN_POSTDIR.BackColor = Color.LightYellow;
                    //lblAN_POSTDIR.Font = fontLabelDataMismatch;
                    //boolHadDifferenceAscSuf = true;
                }
                if (txtCountyA1_PREDIR.Text.ToUpper().ToString() != txtUtransA1_PREDIR.Text.ToUpper().ToString())
                {
                    txtUtransA1_PREDIR.BackColor = Color.LightYellow;
                    txtCountyA1_PREDIR.BackColor = Color.LightYellow;
                    //lblAlias.Font = fontLabelDataMismatch;
                    //boolHadDifferenceA1_NAME = true;
                }
                if (txtCountyA1_NAME.Text.ToUpper().ToString() != txtUtransA1_NAME.Text.ToUpper().ToString())
                {
                    txtUtransA1_NAME.BackColor = Color.LightYellow;
                    txtCountyA1_NAME.BackColor = Color.LightYellow;
                    //lblAlias.Font = fontLabelDataMismatch;
                    //boolHadDifferenceA1_NAME = true;
                }
                if (txtCountyA1_POSTTYPE.Text.ToUpper().ToString() != txtUtransA1_POSTTYPE.Text.ToUpper().ToString())
                {
                    txtUtransA1_POSTTYPE.BackColor = Color.LightYellow;
                    txtCountyA1_POSTTYPE.BackColor = Color.LightYellow;
                    //lblA1_POSTTYPE.Font = fontLabelDataMismatch;
                    //boolHadDifferenceA1_POSTTYPE = true;
                }
                if (txtCountyA1_POSTDIR.Text.ToUpper().ToString() != txtUtransA1_POSTDIR.Text.ToUpper().ToString())
                {
                    txtUtransA1_POSTDIR.BackColor = Color.LightYellow;
                    txtCountyA1_POSTDIR.BackColor = Color.LightYellow;
                    //lblAlias.Font = fontLabelDataMismatch;
                    //boolHadDifferenceA1_NAME = true;
                }
                if (txtCountyA2_PREDIR.Text.ToUpper().ToString() != txtUtransA2_PREDIR.Text.ToUpper().ToString())
                {
                    txtUtransA2_PREDIR.BackColor = Color.LightYellow;
                    txtCountyA2_PREDIR.BackColor = Color.LightYellow;
                    //lblA2_NAME.Font = fontLabelDataMismatch;
                    //boolHadDifferenceA2_NAME = true;
                }
                if (txtCountyA2_NAME.Text.ToUpper().ToString() != txtUtransA2_NAME.Text.ToUpper().ToString())
                {
                    txtUtransA2_NAME.BackColor = Color.LightYellow;
                    txtCountyA2_NAME.BackColor = Color.LightYellow;
                    //lblA2_NAME.Font = fontLabelDataMismatch;
                    //boolHadDifferenceA2_NAME = true;
                }
                if (txtCountyA2_POSTTYPE.Text.ToUpper().ToString() != txtUtransA2_POSTTYPE.Text.ToUpper().ToString())
                {
                    txtUtransA2_POSTTYPE.BackColor = Color.LightYellow;
                    txtCountyA2_POSTTYPE.BackColor = Color.LightYellow;
                    //lblA2_POSTTYPE.Font = fontLabelDataMismatch;
                    //boolHadDifferenceA2_POSTTYPE = true;
                }
                if (txtCountyA2_POSTDIR.Text.ToUpper().ToString() != txtUtransA2_POSTDIR.Text.ToUpper().ToString())
                {
                    txtUtransA2_POSTDIR.BackColor = Color.LightYellow;
                    txtCountyA2_POSTDIR.BackColor = Color.LightYellow;
                    //lblA2_POSTTYPE.Font = fontLabelDataMismatch;
                    //boolHadDifferenceA2_POSTTYPE = true;
                }
            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);
                
                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }



        //open a hyper link to show the google doc describing the attributes for the utrans schema
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                //open google doc attr doc showing attribute details
                //System.Diagnostics.Process.Start(e.Link.LinkData as string);
                System.Diagnostics.Process.Start("https://docs.google.com/document/d/1ojjqCa1Z6IG6Wj0oAbZatoYsmbKzO9XwdD88-kqm-zQ/edit");
            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }



        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                //cboStatusField.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                //throw;
            }
        }



        private void cboStatusField_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                //cboStatusField.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                //throw;
            }

        }




        //this method handles the double clicks on the labels
        private void lbl_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                //get a reference to the label that was doublecliked
                Label clickedLabel = sender as Label;

                // FROMADDR_L
                if (clickedLabel.Text == "FROMADDR_L")
                {
                    if (txtUtranFROMADDR_L.Text != txtCountyFROMADDR_L.Text)
                    {
                        txtUtranFROMADDR_L.Text = txtCountyFROMADDR_L.Text;
                        return;
                    }
                    if (txtUtranFROMADDR_L.Text == txtCountyFROMADDR_L.Text)
                    {
                        txtUtranFROMADDR_L.Text = txtUtransInitialFROMADDR_L;
                        return;
                    }
                }

                // TOADDR_L
                if (clickedLabel.Text == "TOADDR_L")
                {
                    if (txtUtranTOADDR_L.Text != txtCountyTOADDR_L.Text)
                    {
                        txtUtranTOADDR_L.Text = txtCountyTOADDR_L.Text;
                        return;
                    }
                    if (txtUtranTOADDR_L.Text == txtCountyTOADDR_L.Text)
                    {
                        txtUtranTOADDR_L.Text = txtUtransInitialL_TAdd;
                        return;
                    }
                }

                // FROMADDR_R
                if (clickedLabel.Text == "FROMADDR_R")
                {
                    if (txtUtranFROMADDR_R.Text != txtCountyFROMADDR_R.Text)
                    {
                        txtUtranFROMADDR_R.Text = txtCountyFROMADDR_R.Text;
                        return;
                    }
                    if (txtUtranFROMADDR_R.Text == txtCountyFROMADDR_R.Text)
                    {
                        txtUtranFROMADDR_R.Text = txtUtransInitialFROMADDR_R;
                        return;
                    }
                }

                // TOADDR_R
                if (clickedLabel.Text == "TOADDR_R")
                {
                    if (txtUtranTOADDR_R.Text != txtCountyTOADDR_R.Text)
                    {
                        txtUtranTOADDR_R.Text = txtCountyTOADDR_R.Text;
                        return;
                    }
                    if (txtUtranTOADDR_R.Text == txtCountyTOADDR_R.Text)
                    {
                        txtUtranTOADDR_R.Text = txtUtransInitialTOADDR_R;
                        return;
                    }
                }

                // NAME
                if (clickedLabel.Text == "NAME")
                {
                    if (txtUtranStName.Text != txtCountyStName.Text)
                    {
                        txtUtranStName.Text = txtCountyStName.Text;
                        return;
                    }
                    if (txtUtranStName.Text == txtCountyStName.Text)
                    {
                        txtUtranStName.Text = txtUtransInitialStName;
                        return;
                    }
                }

                // PREDIR
                if (clickedLabel.Text == "PREDIR")
                {
                    if (txtUtranPreDir.Text != txtCountyPreDir.Text)
                    {
                        txtUtranPreDir.Text = txtCountyPreDir.Text;
                        return;
                    }
                    if (txtUtranPreDir.Text == txtCountyPreDir.Text)
                    {
                        txtUtranPreDir.Text = txtUtransInitialPreDir;
                        return;
                    }
                }

                // POSTTYPE
                if (clickedLabel.Text == "POSTTYPE")
                {
                    if (txtUtranStType.Text != txtCountyStType.Text)
                    {
                        txtUtranStType.Text = txtCountyStType.Text;
                        return;
                    }
                    if (txtUtranStType.Text == txtCountyStType.Text)
                    {
                        txtUtranStType.Text = txtUtransInitialStType;
                        return;
                    }
                }

                // POSTDIR
                if (clickedLabel.Text == "POSTDIR")
                {
                    if (txtUtranPOSTDIR.Text != txtCountyPOSTDIR.Text)
                    {
                        txtUtranPOSTDIR.Text = txtCountyPOSTDIR.Text;
                        return;
                    }
                    if (txtUtranPOSTDIR.Text == txtCountyPOSTDIR.Text)
                    {
                        txtUtranPOSTDIR.Text = txtUtransInitialPOSTDIR;
                        return;
                    }
                }

                // A1_PREDIR
                if (clickedLabel.Text == "A1_PREDIR")
                {
                    if (txtUtransA1_PREDIR.Text != txtCountyA1_PREDIR.Text)
                    {
                        txtUtransA1_PREDIR.Text = txtCountyA1_PREDIR.Text;
                        return;
                    }
                    if (txtUtransA1_PREDIR.Text == txtCountyA1_PREDIR.Text)
                    {
                        txtUtransA1_PREDIR.Text = txtUtransInitialA1_PREDIR;
                        return;
                    }
                }

                // A1_NAME
                if (clickedLabel.Text == "A1_NAME")
                {
                    if (txtUtransA1_NAME.Text != txtCountyA1_NAME.Text)
                    {
                        txtUtransA1_NAME.Text = txtCountyA1_NAME.Text;
                        return;
                    }
                    if (txtUtransA1_NAME.Text == txtCountyA1_NAME.Text)
                    {
                        txtUtransA1_NAME.Text = txtUtransInitialA1_NAME;
                        return;
                    }
                }

                // A1_POSTTYPE
                if (clickedLabel.Text == "A1_POSTTYPE")
                {
                    if (txtUtransA1_POSTTYPE.Text != txtCountyA1_POSTTYPE.Text)
                    {
                        txtUtransA1_POSTTYPE.Text = txtCountyA1_POSTTYPE.Text;
                        return;
                    }
                    if (txtUtransA1_POSTTYPE.Text == txtCountyA1_POSTTYPE.Text)
                    {
                        txtUtransA1_POSTTYPE.Text = txtUtransInitialA1_POSTTYPE;
                        return;
                    }
                }

                // A1_POSTDIR
                if (clickedLabel.Text == "A1_POSTDIR")
                {
                    if (txtUtransA1_POSTDIR.Text != txtCountyA1_POSTDIR.Text)
                    {
                        txtUtransA1_POSTDIR.Text = txtCountyA1_POSTDIR.Text;
                        return;
                    }
                    if (txtUtransA1_POSTDIR.Text == txtCountyA1_POSTDIR.Text)
                    {
                        txtUtransA1_POSTDIR.Text = txtUtransInitialA1_POSTDIR;
                        return;
                    }
                }

                // A2_PREDIR
                if (clickedLabel.Text == "A2_PREDIR")
                {
                    if (txtUtransA2_PREDIR.Text != txtCountyA2_PREDIR.Text)
                    {
                        txtUtransA2_PREDIR.Text = txtCountyA2_PREDIR.Text;
                        return;
                    }
                    if (txtUtransA2_PREDIR.Text == txtCountyA2_PREDIR.Text)
                    {
                        txtUtransA2_PREDIR.Text = txtUtransInitialA2_PREDIR;
                        return;
                    }
                }

                // A2_NAME
                if (clickedLabel.Text == "A2_NAME")
                {
                    if (txtUtransA2_NAME.Text != txtCountyA2_NAME.Text)
                    {
                        txtUtransA2_NAME.Text = txtCountyA2_NAME.Text;
                        return;
                    }
                    if (txtUtransA2_NAME.Text == txtCountyA2_NAME.Text)
                    {
                        txtUtransA2_NAME.Text = txtUtransInitialA2_NAME;
                        return;
                    }
                }

                // A2_POSTTYPE
                if (clickedLabel.Text == "A2_POSTTYPE")
                {
                    if (txtUtransA2_POSTTYPE.Text != txtCountyA2_POSTTYPE.Text)
                    {
                        txtUtransA2_POSTTYPE.Text = txtCountyA2_POSTTYPE.Text;
                        return;
                    }
                    if (txtUtransA2_POSTTYPE.Text == txtCountyA2_POSTTYPE.Text)
                    {
                        txtUtransA2_POSTTYPE.Text = txtUtransInitialA2_POSTTYPE;
                        return;
                    }
                }

                // A2_POSTDIR
                if (clickedLabel.Text == "A2_POSTDIR")
                {
                    if (txtUtransA2_POSTDIR.Text != txtCountyA2_POSTDIR.Text)
                    {
                        txtUtransA2_POSTDIR.Text = txtCountyA2_POSTDIR.Text;
                        return;
                    }
                    if (txtUtransA2_POSTDIR.Text == txtCountyA2_POSTDIR.Text)
                    {
                        txtUtransA2_POSTDIR.Text = txtUtransInitialA2_POSTDIR;
                        return;
                    }
                }

                // AN_NAME
                if (clickedLabel.Text == "AN_NAME")
                {
                    if (txtUtransAN_NAME.Text != txtCountyAN_NAME.Text)
                    {
                        txtUtransAN_NAME.Text = txtCountyAN_NAME.Text;
                        return;
                    }
                    if (txtUtransAN_NAME.Text == txtCountyAN_NAME.Text)
                    {
                        txtUtransAN_NAME.Text = txtUtransInitialAN_NAME;
                        return;
                    }
                }

                // AN_POSTDIR
                if (clickedLabel.Text == "AN_POSTDIR")
                {
                    if (txtUtransAN_POSTDIR.Text != txtCountyAN_POSTDIR.Text)
                    {
                        txtUtransAN_POSTDIR.Text = txtCountyAN_POSTDIR.Text;
                        return;
                    }
                    if (txtUtransAN_POSTDIR.Text == txtCountyAN_POSTDIR.Text)
                    {
                        txtUtransAN_POSTDIR.Text = txtUtransInitialAN_POSTDIR;
                        return;
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }



        // the following methods handle textbox text changes // 

        // FROMADDR_L
        private void txtUtranFROMADDR_L_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtranFROMADDR_L.Text.ToUpper().ToString() != txtCountyFROMADDR_L.Text.ToUpper().ToString())
                {
                    txtUtranFROMADDR_L.BackColor = Color.LightYellow;
                    txtCountyFROMADDR_L.BackColor = Color.LightYellow;
                }
                else if (txtUtranFROMADDR_L.Text.ToUpper().ToString() == txtCountyFROMADDR_L.Text.ToUpper().ToString())
                {
                    txtUtranFROMADDR_L.BackColor = Color.White;
                    txtCountyFROMADDR_L.BackColor = Color.White;
                }

                if (txtUtranFROMADDR_L.Text != txtUtransInitialFROMADDR_L)
                {
                    lblLeftFrom.Font = fontLabelHasEdits;
                    //lblLeftFrom.ForeColor = Color.LightSalmon;
                    btnSaveToUtrans.Enabled = true;
                }
                else
                {
                    lblLeftFrom.Font = fontLabelRegular;
                    //lblLeftFrom.ForeColor = Color.Black;
                    //btnSaveToUtrans.Enabled = false;
                    btnSaveToUtrans.Enabled = true;
                }
                //fontLabelHasEdits.Dispose();
                //fontLabelRegular.Dispose();
            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        // TOADDR_L
        private void txtUtranTOADDR_L_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtranTOADDR_L.Text.ToUpper().ToString() != txtCountyTOADDR_L.Text.ToUpper().ToString())
                {
                    txtUtranTOADDR_L.BackColor = Color.LightYellow;
                    txtCountyTOADDR_L.BackColor = Color.LightYellow;
                }
                else if (txtUtranTOADDR_L.Text.ToUpper().ToString() == txtCountyTOADDR_L.Text.ToUpper().ToString())
                {
                    txtUtranTOADDR_L.BackColor = Color.White;
                    txtCountyTOADDR_L.BackColor = Color.White;
                }

                if (txtUtranTOADDR_L.Text != txtUtransInitialL_TAdd)
                {
                    lblLeftTo.Font = fontLabelHasEdits;
                    //lblLeftTo.ForeColor = Color.LightSalmon;
                    btnSaveToUtrans.Enabled = true;
                }
                else
                {
                    lblLeftTo.Font = fontLabelRegular;
                    //lblLeftTo.ForeColor = Color.Black;
                    //btnSaveToUtrans.Enabled = false;
                    btnSaveToUtrans.Enabled = true;
                }
                //fontLabelHasEdits.Dispose();
                //fontLabelRegular.Dispose();
            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        // FROMADDR_R
        private void txtUtranFROMADDR_R_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtranFROMADDR_R.Text.ToUpper().ToString() != txtCountyFROMADDR_R.Text.ToUpper().ToString())
                {
                    txtUtranFROMADDR_R.BackColor = Color.LightYellow;
                    txtCountyFROMADDR_R.BackColor = Color.LightYellow;
                }
                else if (txtUtranFROMADDR_R.Text.ToUpper().ToString() == txtCountyFROMADDR_R.Text.ToUpper().ToString())
                {
                    txtUtranFROMADDR_R.BackColor = Color.White;
                    txtCountyFROMADDR_R.BackColor = Color.White;
                }

                if (txtUtranFROMADDR_R.Text != txtUtransInitialFROMADDR_R)
                {
                    lblRightFrom.Font = fontLabelHasEdits;
                    //lblRightFrom.ForeColor = Color.LightSalmon;
                    btnSaveToUtrans.Enabled = true;
                }
                else
                {
                    lblRightFrom.Font = fontLabelRegular;
                    //lblRightFrom.ForeColor = Color.Black;
                    //btnSaveToUtrans.Enabled = false;
                    btnSaveToUtrans.Enabled = true;
                }
                //fontLabelHasEdits.Dispose();
                //fontLabelRegular.Dispose();
            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        // TOADDR_R
        private void txtUtranTOADDR_R_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtranTOADDR_R.Text.ToUpper().ToString() != txtCountyTOADDR_R.Text.ToUpper().ToString())
                {
                    txtUtranTOADDR_R.BackColor = Color.LightYellow;
                    txtCountyTOADDR_R.BackColor = Color.LightYellow;
                }
                else if (txtUtranTOADDR_R.Text.ToUpper().ToString() == txtCountyTOADDR_R.Text.ToUpper().ToString())
                {
                    txtUtranTOADDR_R.BackColor = Color.White;
                    txtCountyTOADDR_R.BackColor = Color.White;
                }

                if (txtUtranTOADDR_R.Text != txtUtransInitialTOADDR_R)
                {
                    lblRightTo.Font = fontLabelHasEdits;
                    //lblRightTo.ForeColor = Color.LightSalmon;
                    btnSaveToUtrans.Enabled = true;
                }
                else
                {
                    lblRightTo.Font = fontLabelRegular;
                    //lblRightTo.ForeColor = Color.Black;
                    //btnSaveToUtrans.Enabled = false;
                    btnSaveToUtrans.Enabled = true;
                }
                //fontLabelHasEdits.Dispose();
                //fontLabelRegular.Dispose();
            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        // PREDIR
        private void txtUtranPreDir_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtranPreDir.Text.ToUpper().ToString() != txtCountyPreDir.Text.ToUpper().ToString())
                {
                    txtUtranPreDir.BackColor = Color.LightYellow;
                    txtCountyPreDir.BackColor = Color.LightYellow;
                }
                else if (txtUtranPreDir.Text.ToUpper().ToString() == txtCountyPreDir.Text.ToUpper().ToString())
                {
                    txtUtranPreDir.BackColor = Color.White;
                    txtCountyPreDir.BackColor = Color.White;
                }

                if (txtUtranPreDir.Text != txtUtransInitialPreDir)
                {
                    lblPreDir.Font = fontLabelHasEdits;
                    //lblPreDir.ForeColor = Color.LightSalmon;
                    btnSaveToUtrans.Enabled = true;
                }
                else
                {
                    lblPreDir.Font = fontLabelRegular;
                    //lblPreDir.ForeColor = Color.Black;
                    //btnSaveToUtrans.Enabled = false;
                    btnSaveToUtrans.Enabled = true;
                }
                //fontLabelHasEdits.Dispose();
                //fontLabelRegular.Dispose();
            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        // NAME
        private void txtUtranStName_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtranStName.Text.ToUpper().ToString() != txtCountyStName.Text.ToUpper().ToString())
                {
                    txtUtranStName.BackColor = Color.LightYellow;
                    txtCountyStName.BackColor = Color.LightYellow;
                }
                else if (txtUtranStName.Text.ToUpper().ToString() == txtCountyStName.Text.ToUpper().ToString())
                {
                    txtUtranStName.BackColor = Color.White;
                    txtCountyStName.BackColor = Color.White;
                }

                if (txtUtranStName.Text != txtUtransInitialStName)
                {
                    lblStName.Font = fontLabelHasEdits;
                    //lblStName.ForeColor = Color.LightSalmon;
                    btnSaveToUtrans.Enabled = true;
                }
                else
                {
                    lblStName.Font = fontLabelRegular;
                    //lblStName.ForeColor = Color.Black;
                    //btnSaveToUtrans.Enabled = false;
                    btnSaveToUtrans.Enabled = true;
                }
                //fontLabelHasEdits.Dispose();
                //fontLabelRegular.Dispose();
            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        // POSTTYPE
        private void txtUtranStType_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtranStType.Text.ToUpper().ToString() != txtCountyStType.Text.ToUpper().ToString())
                {
                    txtUtranStType.BackColor = Color.LightYellow;
                    txtCountyStType.BackColor = Color.LightYellow;
                }
                else if (txtUtranStType.Text.ToUpper().ToString() == txtCountyStType.Text.ToUpper().ToString())
                {
                    txtUtranStType.BackColor = Color.White;
                    txtCountyStType.BackColor = Color.White;
                }

                if (txtUtranStType.Text != txtUtransInitialStType)
                {
                    lblStType.Font = fontLabelHasEdits;
                    //lblStType.ForeColor = Color.LightSalmon;
                    btnSaveToUtrans.Enabled = true;
                }
                else
                {
                    lblStType.Font = fontLabelRegular;
                    //lblStType.ForeColor = Color.Black;
                    //btnSaveToUtrans.Enabled = false;
                    btnSaveToUtrans.Enabled = true;
                }
                //fontLabelHasEdits.Dispose();
                //fontLabelRegular.Dispose();
            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        // POSTDIR
        private void txtUtranPOSTDIR_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtranPOSTDIR.Text.ToUpper().ToString() != txtCountyPOSTDIR.Text.ToUpper().ToString())
                {
                    txtUtranPOSTDIR.BackColor = Color.LightYellow;
                    txtCountyPOSTDIR.BackColor = Color.LightYellow;
                }
                else if (txtUtranPOSTDIR.Text.ToUpper().ToString() == txtCountyPOSTDIR.Text.ToUpper().ToString())
                {
                    txtUtranPOSTDIR.BackColor = Color.White;
                    txtCountyPOSTDIR.BackColor = Color.White;
                }

                if (txtUtranPOSTDIR.Text != txtUtransInitialPOSTDIR)
                {
                    lblPOSTDIR.Font = fontLabelHasEdits;
                    //lblPOSTDIR.ForeColor = Color.LightSalmon;
                    btnSaveToUtrans.Enabled = true;
                }
                else
                {
                    lblPOSTDIR.Font = fontLabelRegular;
                    //lblPOSTDIR.ForeColor = Color.Black;
                    //btnSaveToUtrans.Enabled = false;
                    btnSaveToUtrans.Enabled = true;
                }
                //fontLabelHasEdits.Dispose();
                //fontLabelRegular.Dispose();
            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        // A1_PREDIR
        private void txtUtransA1_PREDIR_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtransA1_PREDIR.Text.ToUpper().ToString() != txtCountyA1_PREDIR.Text.ToUpper().ToString())
                {
                    txtUtransA1_PREDIR.BackColor = Color.LightYellow;
                    txtCountyA1_PREDIR.BackColor = Color.LightYellow;
                }
                else if (txtUtransA1_PREDIR.Text.ToUpper().ToString() == txtCountyA1_PREDIR.Text.ToUpper().ToString())
                {
                    txtUtransA1_PREDIR.BackColor = Color.White;
                    txtCountyA1_PREDIR.BackColor = Color.White;
                }

                if (txtUtransA1_PREDIR.Text != txtUtransInitialA1_PREDIR)
                {
                    lblA1_PREDIR.Font = fontLabelHasEdits;
                    //lblAlias.ForeColor = Color.LightSalmon;
                    btnSaveToUtrans.Enabled = true;
                }
                else
                {
                    lblA1_PREDIR.Font = fontLabelRegular;
                    //lblAlias.ForeColor = Color.Black;
                    //btnSaveToUtrans.Enabled = false;
                    btnSaveToUtrans.Enabled = true;
                }
                //fontLabelHasEdits.Dispose();
                //fontLabelRegular.Dispose();
            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        // A1_NAME
        private void txtUtransA1_NAME_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtransA1_NAME.Text.ToUpper().ToString() != txtCountyA1_NAME.Text.ToUpper().ToString())
                {
                    txtUtransA1_NAME.BackColor = Color.LightYellow;
                    txtCountyA1_NAME.BackColor = Color.LightYellow;
                }
                else if (txtUtransA1_NAME.Text.ToUpper().ToString() == txtCountyA1_NAME.Text.ToUpper().ToString())
                {
                    txtUtransA1_NAME.BackColor = Color.White;
                    txtCountyA1_NAME.BackColor = Color.White;
                }

                if (txtUtransA1_NAME.Text != txtUtransInitialA1_NAME)
                {
                    lblA1_NAME.Font = fontLabelHasEdits;
                    //lblAlias.ForeColor = Color.LightSalmon;
                    btnSaveToUtrans.Enabled = true;
                }
                else
                {
                    lblA1_NAME.Font = fontLabelRegular;
                    //lblAlias.ForeColor = Color.Black;
                    //btnSaveToUtrans.Enabled = false;
                    btnSaveToUtrans.Enabled = true;
                }
                //fontLabelHasEdits.Dispose();
                //fontLabelRegular.Dispose();
            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

        }

        // A1_POSTTYPE
        private void txtUtransA1_POSTTYPE_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtransA1_POSTTYPE.Text.ToUpper().ToString() != txtCountyA1_POSTTYPE.Text.ToUpper().ToString())
                {
                    txtUtransA1_POSTTYPE.BackColor = Color.LightYellow;
                    txtCountyA1_POSTTYPE.BackColor = Color.LightYellow;
                }
                else if (txtUtransA1_POSTTYPE.Text.ToUpper().ToString() == txtCountyA1_POSTTYPE.Text.ToUpper().ToString())
                {
                    txtUtransA1_POSTTYPE.BackColor = Color.White;
                    txtCountyA1_POSTTYPE.BackColor = Color.White;
                }

                if (txtUtransA1_POSTTYPE.Text != txtUtransInitialA1_POSTTYPE)
                {
                    lblA1_POSTTYPE.Font = fontLabelHasEdits;
                    //lblA1_POSTTYPE.ForeColor = Color.LightSalmon;
                    btnSaveToUtrans.Enabled = true;
                }
                else
                {
                    lblA1_POSTTYPE.Font = fontLabelRegular;
                    //lblA1_POSTTYPE.ForeColor = Color.Black;
                    //btnSaveToUtrans.Enabled = false;
                    btnSaveToUtrans.Enabled = true;
                }
                //fontLabelHasEdits.Dispose();
                //fontLabelRegular.Dispose();
            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        // A1_POSTDIR
        private void txtUtransA1_POSTDIR_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtransA1_POSTDIR.Text.ToUpper().ToString() != txtCountyA1_POSTDIR.Text.ToUpper().ToString())
                {
                    txtUtransA1_POSTDIR.BackColor = Color.LightYellow;
                    txtCountyA1_POSTDIR.BackColor = Color.LightYellow;
                }
                else if (txtUtransA1_POSTDIR.Text.ToUpper().ToString() == txtCountyA1_POSTDIR.Text.ToUpper().ToString())
                {
                    txtUtransA1_POSTDIR.BackColor = Color.White;
                    txtCountyA1_POSTDIR.BackColor = Color.White;
                }

                if (txtUtransA1_POSTDIR.Text != txtUtransInitialA1_POSTDIR)
                {
                    lblA1_POSTDIR.Font = fontLabelHasEdits;
                    //lblA1_POSTTYPE.ForeColor = Color.LightSalmon;
                    btnSaveToUtrans.Enabled = true;
                }
                else
                {
                    lblA1_POSTDIR.Font = fontLabelRegular;
                    //lblA1_POSTTYPE.ForeColor = Color.Black;
                    //btnSaveToUtrans.Enabled = false;
                    btnSaveToUtrans.Enabled = true;
                }
                //fontLabelHasEdits.Dispose();
                //fontLabelRegular.Dispose();
            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }


        // A2_PREDIR
        private void txtUtransA2_PREDIR_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtransA2_PREDIR.Text.ToUpper().ToString() != txtCountyA2_PREDIR.Text.ToUpper().ToString())
                {
                    txtUtransA2_PREDIR.BackColor = Color.LightYellow;
                    txtCountyA2_PREDIR.BackColor = Color.LightYellow;
                }
                else if (txtUtransA2_PREDIR.Text.ToUpper().ToString() == txtCountyA2_PREDIR.Text.ToUpper().ToString())
                {
                    txtUtransA2_PREDIR.BackColor = Color.White;
                    txtCountyA2_PREDIR.BackColor = Color.White;
                }

                if (txtUtransA2_PREDIR.Text != txtUtransInitialA2_PREDIR)
                {
                    lblA2_PREDIR.Font = fontLabelHasEdits;
                    //lblAlias.ForeColor = Color.LightSalmon;
                    btnSaveToUtrans.Enabled = true;
                }
                else
                {
                    lblA2_PREDIR.Font = fontLabelRegular;
                    //lblAlias.ForeColor = Color.Black;
                    //btnSaveToUtrans.Enabled = false;
                    btnSaveToUtrans.Enabled = true;
                }
                //fontLabelHasEdits.Dispose();
                //fontLabelRegular.Dispose();
            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        // A2_NAME
        private void txtUtransA2_NAME_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtransA2_NAME.Text.ToUpper().ToString() != txtCountyA2_NAME.Text.ToUpper().ToString())
                {
                    txtUtransA2_NAME.BackColor = Color.LightYellow;
                    txtCountyA2_NAME.BackColor = Color.LightYellow;
                }
                else if (txtUtransA2_NAME.Text.ToUpper().ToString() == txtCountyA2_NAME.Text.ToUpper().ToString())
                {
                    txtUtransA2_NAME.BackColor = Color.White;
                    txtCountyA2_NAME.BackColor = Color.White;
                }

                if (txtUtransA2_NAME.Text != txtUtransInitialA2_NAME)
                {
                    lblA2_NAME.Font = fontLabelHasEdits;
                    //lblA2_NAME.ForeColor = Color.LightSalmon;
                    btnSaveToUtrans.Enabled = true;
                }
                else
                {
                    lblA2_NAME.Font = fontLabelRegular;
                    //lblA2_NAME.ForeColor = Color.Black;
                    //btnSaveToUtrans.Enabled = false;
                    btnSaveToUtrans.Enabled = true;
                }
                //fontLabelHasEdits.Dispose();
                //fontLabelRegular.Dispose();
            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        // A2_POSTTYPE
        private void txtUtransA2_POSTTYPE_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtransA2_POSTTYPE.Text.ToUpper().ToString() != txtCountyA2_POSTTYPE.Text.ToUpper().ToString())
                {
                    txtUtransA2_POSTTYPE.BackColor = Color.LightYellow;
                    txtCountyA2_POSTTYPE.BackColor = Color.LightYellow;
                }
                else if (txtUtransA2_POSTTYPE.Text.ToUpper().ToString() == txtCountyA2_POSTTYPE.Text.ToUpper().ToString())
                {
                    txtUtransA2_POSTTYPE.BackColor = Color.White;
                    txtCountyA2_POSTTYPE.BackColor = Color.White;
                }

                if (txtUtransA2_POSTTYPE.Text != txtUtransInitialA2_POSTTYPE)
                {
                    lblA2_POSTTYPE.Font = fontLabelHasEdits;
                    //lblA2_POSTTYPE.ForeColor = Color.LightSalmon;
                    btnSaveToUtrans.Enabled = true;
                }
                else
                {
                    lblA2_POSTTYPE.Font = fontLabelRegular;
                    //lblA2_POSTTYPE.ForeColor = Color.Black;
                    //btnSaveToUtrans.Enabled = false;
                    btnSaveToUtrans.Enabled = true;
                }
                //fontLabelHasEdits.Dispose();
                //fontLabelRegular.Dispose();
            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        // A2_POSTDIR
        private void txtUtransA2_POSTDIR_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtransA2_POSTDIR.Text.ToUpper().ToString() != txtCountyA2_POSTDIR.Text.ToUpper().ToString())
                {
                    txtUtransA2_POSTDIR.BackColor = Color.LightYellow;
                    txtCountyA2_POSTDIR.BackColor = Color.LightYellow;
                }
                else if (txtUtransA2_POSTDIR.Text.ToUpper().ToString() == txtCountyA2_POSTDIR.Text.ToUpper().ToString())
                {
                    txtUtransA2_POSTDIR.BackColor = Color.White;
                    txtCountyA2_POSTDIR.BackColor = Color.White;
                }

                if (txtUtransA2_POSTDIR.Text != txtUtransInitialA2_POSTDIR)
                {
                    lblA2_POSTDIR.Font = fontLabelHasEdits;
                    //lblA1_POSTTYPE.ForeColor = Color.LightSalmon;
                    btnSaveToUtrans.Enabled = true;
                }
                else
                {
                    lblA2_POSTDIR.Font = fontLabelRegular;
                    //lblA1_POSTTYPE.ForeColor = Color.Black;
                    //btnSaveToUtrans.Enabled = false;
                    btnSaveToUtrans.Enabled = true;
                }
                //fontLabelHasEdits.Dispose();
                //fontLabelRegular.Dispose();
            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        //AN_NAME
        private void txtUtransAcsAllias_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtransAN_NAME.Text.ToUpper().ToString() != txtCountyAN_NAME.Text.ToUpper().ToString())
                {
                    txtUtransAN_NAME.BackColor = Color.LightYellow;
                    txtCountyAN_NAME.BackColor = Color.LightYellow;
                }
                else if (txtUtransAN_NAME.Text.ToUpper().ToString() == txtCountyAN_NAME.Text.ToUpper().ToString())
                {
                    txtUtransAN_NAME.BackColor = Color.White;
                    txtCountyAN_NAME.BackColor = Color.White;
                }

                if (txtUtransAN_NAME.Text != txtUtransInitialAN_NAME)
                {
                    lblAN_NAME.Font = fontLabelHasEdits;
                    //lblAcsAlias.ForeColor = Color.LightSalmon;
                    btnSaveToUtrans.Enabled = true;
                }
                else
                {
                    lblAN_NAME.Font = fontLabelRegular;
                    //lblAcsAlias.ForeColor = Color.Black;
                    //btnSaveToUtrans.Enabled = false;
                    btnSaveToUtrans.Enabled = true;
                }
                //fontLabelHasEdits.Dispose();
                //fontLabelRegular.Dispose();
            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        // AN_POSTDIR
        private void txtUtransAN_POSTDIR_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtransAN_POSTDIR.Text.ToUpper().ToString() != txtCountyAN_POSTDIR.Text.ToUpper().ToString())
                {
                    txtUtransAN_POSTDIR.BackColor = Color.LightYellow;
                    txtCountyAN_POSTDIR.BackColor = Color.LightYellow;
                }
                else if (txtUtransAN_POSTDIR.Text.ToUpper().ToString() == txtCountyAN_POSTDIR.Text.ToUpper().ToString())
                {
                    txtUtransAN_POSTDIR.BackColor = Color.White;
                    txtCountyAN_POSTDIR.BackColor = Color.White;
                }

                if (txtUtransAN_POSTDIR.Text != txtUtransInitialAN_POSTDIR)
                {
                    lblAN_POSTDIR.Font = fontLabelHasEdits;
                    //lblAN_POSTDIR.ForeColor = Color.LightSalmon;
                    btnSaveToUtrans.Enabled = true;
                }
                else
                {
                    lblAN_POSTDIR.Font = fontLabelRegular;
                    //lblAN_POSTDIR.ForeColor = Color.Black;
                    //btnSaveToUtrans.Enabled = false;
                    btnSaveToUtrans.Enabled = true;
                }
                //fontLabelHasEdits.Dispose();
                //fontLabelRegular.Dispose();
            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }




        // this method forces the house range number texboxes to only accept numeric values
        private void txtUtran_HouseNumber_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
            {
                e.Handled = true;
            }
        }




        // SAVE IN UTRANS BUTTON //
        private void btnSaveToUtrans_Click(object sender, EventArgs e)
        {
            try
            {
                //save the values on the form in the utrans database
                //get the selected dfc layers value for the current utrans oid

                //check if a cartocode has been chosen
                if (cboCartoCode.SelectedIndex == -1) //or maybe check for .text == ""
                {
                    DialogResult dialogResult1 = MessageBox.Show("Warning!  You are saving a street segment that has not been assigned a CARTOCODE." + Environment.NewLine + "All street segments typically require a CARTOCODE value, which can be selected on the drop-down list." + Environment.NewLine + Environment.NewLine + "Would you like to continue the save without a CARTOCODE?", "Format Warning!", MessageBoxButtons.YesNo);
                    if (dialogResult1 == DialogResult.Yes)
                    {
                        // do nothing... continue to saving
                    }
                    else if (dialogResult1 == DialogResult.No) //exit the save operation becuase the user chose to select a cartocode
                    {
                        //exit out and don't proceed to saving...
                        return;
                    }
                }


                //check what's selected in the combobox for status field, if completed is selected then proceed to save, else calc value in dfc field and don't save in utrans
                // calculate status field, if not text = COMPLETED
                IQueryFilter arcQueryFilter_DFC_updateOID = new QueryFilter();
                arcQueryFilter_DFC_updateOID.WhereClause = "OBJECTID = " + strDFC_RESULT_oid;

                //ICalculator arcCalculator = new Calculator();
                //ICursor arcCur_dfcLayer = clsGlobals.arcGeoFLayerDfcResult.FeatureClass.Update(arcQueryFilter_DFC_updateOID, true) as ICursor;

                IFeatureCursor arcFeatCur_dfcLayer = clsGlobals.arcGeoFLayerDfcResult.Search(arcQueryFilter_DFC_updateOID, false);
                IFeature arcFeature_DFC = arcFeatCur_dfcLayer.NextFeature();

                if (arcFeature_DFC == null)
                {
                    MessageBox.Show("Could not find a feature in the DFC_RESULT layer with OID: " + strDFC_RESULT_oid, "OID Not Found", MessageBoxButtons.OK);
                    return;
                }
                
                string strComboBoxTextValue = cboStatusField.Text.ToString();
                switch (strComboBoxTextValue)
                {
                    case "COMPLETED":
                        //do nothing, proceed to saving in utrans database
                        //update the dfc status field after the save, that way we know it was solid, without errors
                        break;
                    case "IGNORE":
                        ////string strCalcExprIgnore = @"""" + strComboBoxTextValue + @"""";
                        
                        //////proceed with calculating values in the dfc table 
                        ////arcCalculator.Cursor = arcCur_dfcLayer;
                        ////arcCalculator.Expression = strCalcExprIgnore;
                        ////arcCalculator.Field = "CURRENT_NOTES";
                        ////arcCalculator.Calculate();
                        ////arcCalculator.ShowErrorPrompt = true;
                        
                        //////clear out the cursor
                        ////arcCur_dfcLayer = null;

                        // save the value to dfc_result layer
                        clsGlobals.arcEditor.StartOperation();
                        arcFeature_DFC.set_Value(arcFeature_DFC.Fields.FindField("CURRENT_NOTES"), strComboBoxTextValue);
                        arcFeature_DFC.Store();
                        clsGlobals.arcEditor.StopOperation("DFC_RESULT Update");

                        //unselect everything in map
                        arcMapp.ClearSelection();

                        //refresh the map layers and data
                        arcActiveView.Refresh();
                        arcActiveView.Refresh();
                        
                        //exit
                        return;
                    case "REVISIT":
                        ////string strCalcExprRevisit = @"""" + strComboBoxTextValue + @"""";

                        //////proceed with calculating values in the dfc table 
                        ////arcCalculator.Cursor = arcCur_dfcLayer;
                        ////arcCalculator.Expression = strCalcExprRevisit;
                        ////arcCalculator.Field = "CURRENT_NOTES";
                        ////arcCalculator.Calculate();
                        ////arcCalculator.ShowErrorPrompt = true;

                        //////clear out the cursor
                        ////arcCur_dfcLayer = null;

                        // save the value to dfc_result layer
                        clsGlobals.arcEditor.StartOperation();
                        arcFeature_DFC.set_Value(arcFeature_DFC.Fields.FindField("CURRENT_NOTES"), strComboBoxTextValue);
                        arcFeature_DFC.Store();
                        clsGlobals.arcEditor.StopOperation("DFC_RESULT Update");

                        //unselect everything in map
                        arcMapp.ClearSelection();

                        //refresh the map layers and data
                        arcActiveView.Refresh();
                        arcActiveView.Refresh();

                        //exit                        
                        return;
                    case "NOTIFY AND IGNORE":
                        ////string strCalcExprInformIgnoreCounty = @"""" + strComboBoxTextValue + @"""";

                        //////proceed with calculating values in the dfc table 
                        ////arcCalculator.Cursor = arcCur_dfcLayer;
                        ////arcCalculator.Expression = strCalcExprInformIgnoreCounty;
                        ////arcCalculator.Field = "CURRENT_NOTES";
                        ////arcCalculator.Calculate();
                        ////arcCalculator.ShowErrorPrompt = true;

                        //////clear out the cursor
                        ////arcCur_dfcLayer = null;

                        // save the value to dfc_result layer
                        clsGlobals.arcEditor.StartOperation();
                        arcFeature_DFC.set_Value(arcFeature_DFC.Fields.FindField("CURRENT_NOTES"), strComboBoxTextValue);
                        arcFeature_DFC.Store();
                        clsGlobals.arcEditor.StopOperation("DFC_RESULT Update");

                        //call google spreadsheet doc
                        clsGlobals.strCountySegment = txtCountyPreDir.Text.Trim() + " " + txtCountyStName.Text.Trim() + " " + txtCountyStType.Text.Trim() + " " + txtCountyPOSTDIR.Text.Trim();
                        clsGlobals.strCountySegmentTrimed = clsGlobals.strCountySegment.Trim();
                        if (txtCountyFROMADDR_L.Text != "")
                        {
                            clsGlobals.strCountyFROMADDR_L = txtCountyFROMADDR_L.Text.ToString().Trim();
                        }
                        else
                        {
                            clsGlobals.strCountyFROMADDR_L = "0";
                        }
                        if (txtCountyTOADDR_L.Text != "")
                        {
                            clsGlobals.strCountyTOADDR_L = txtCountyTOADDR_L.Text.ToString().Trim();
                        }
                        else
                        {
                            clsGlobals.strCountyTOADDR_L = "0";
                        }
                        if (txtCountyFROMADDR_R.Text != "")
                        {
                            clsGlobals.strCountyFROMADDR_R = txtCountyFROMADDR_R.Text.ToString().Trim();
                        }
                        else
                        {
                            clsGlobals.strCountyFROMADDR_R = "0";
                        }
                        if (txtCountyTOADDR_R.Text != "")
                        {
                            clsGlobals.strCountyTOADDR_R = txtCountyTOADDR_R.Text.ToString().Trim();
                        }
                        else
                        {
                            clsGlobals.strCountyTOADDR_R = "0";
                        }

                        //check if null values in utrans streets, if so assign zero
                        strGoogleLogLeftTo = "";
                        strGoogleLogLeftFrom = "";
                        strGoogleLogRightTo = "";
                        strGoogleLogRightFrom = "";
                        
                        if (txtUtranTOADDR_L.Text == "")
	                    {
		                    strGoogleLogLeftTo = "0";
	                    }
                        else
	                    {
                            strGoogleLogLeftTo = txtUtranTOADDR_L.Text;
	                    }
                        if (txtUtranFROMADDR_L.Text == "")
	                    {
		                     strGoogleLogLeftFrom = "0";
	                    }
                        else
	                    {
                            strGoogleLogLeftFrom = txtUtranFROMADDR_L.Text;
	                    }
                        if (txtUtranFROMADDR_R.Text == "")
	                    {
		                    strGoogleLogRightFrom = "0";
	                    }
                        else
	                    {
                            strGoogleLogRightFrom = txtUtranFROMADDR_R.Text;
	                    }
                        if (txtUtranTOADDR_R.Text == "")
	                    {
		                    strGoogleLogRightTo = "0";
	                    }
                        else
	                    {
                            strGoogleLogRightTo = txtUtranTOADDR_R.Text;
	                    }

                        // get city from muni layer for google doc city field
                        clsGlobals.strGoogleSpreadsheetCityField = getCityFromSpatialIntersect(arcCountyFeature);

                        //string together the agrc street segment
                        clsGlobals.strAgrcSegment = strGoogleLogLeftFrom + "-" + strGoogleLogLeftTo + " " + strGoogleLogRightFrom + "-" + strGoogleLogRightTo + " " + txtUtranPreDir.Text.Trim() + " " + txtUtranStName.Text.Trim() + " " + txtUtranStType.Text.Trim() + " " + txtUtranPOSTDIR.Text.Trim();

                        //call the google api to transfer values to the spreadsheet
                        clsUtransEditorStaticClass.AddRowToGoogleSpreadsheet();

                        //unselect everything in map
                        arcMapp.ClearSelection();

                        //refresh the map layers and data
                        arcActiveView.Refresh();
                        arcActiveView.Refresh();

                        //exit method
                        return;
                    case "NOTIFY AND SAVE":
                        ////string strCalcExprInformSaveCounty = @"""" + strComboBoxTextValue + @"""";

                        //////proceed with calculating values in the dfc table 
                        ////arcCalculator.Cursor = arcCur_dfcLayer;
                        ////arcCalculator.Expression = strCalcExprInformSaveCounty;
                        ////arcCalculator.Field = "CURRENT_NOTES";
                        ////arcCalculator.Calculate();
                        ////arcCalculator.ShowErrorPrompt = true;

                        //////clear out the cursor
                        ////arcCur_dfcLayer = null;

                        //call google spreadsheet doc
                        clsGlobals.strCountySegment = txtCountyPreDir.Text.Trim() + " " + txtCountyStName.Text.Trim() + " " + txtCountyStType.Text.Trim() + " " + txtCountyPOSTDIR.Text.Trim();
                        clsGlobals.strCountySegmentTrimed = clsGlobals.strCountySegment.Trim();
                        if (txtCountyFROMADDR_L.Text != "")
                        {
                            clsGlobals.strCountyFROMADDR_L = txtCountyFROMADDR_L.Text.ToString().Trim();
                        }
                        else
                        {
                            clsGlobals.strCountyFROMADDR_L = "0";
                        }
                        if (txtCountyTOADDR_L.Text != "")
                        {
                            clsGlobals.strCountyTOADDR_L = txtCountyTOADDR_L.Text.ToString().Trim();
                        }
                        else
                        {
                            clsGlobals.strCountyTOADDR_L = "0";
                        }
                        if (txtCountyFROMADDR_R.Text != "")
                        {
                            clsGlobals.strCountyFROMADDR_R = txtCountyFROMADDR_R.Text.ToString().Trim();
                        }
                        else
                        {
                            clsGlobals.strCountyFROMADDR_R = "0";
                        }
                        if (txtCountyTOADDR_R.Text != "")
                        {
                            clsGlobals.strCountyTOADDR_R = txtCountyTOADDR_R.Text.ToString().Trim();
                        }
                        else
                        {
                            clsGlobals.strCountyTOADDR_R = "0";
                        }

                        //check if null values in utrans streets, if so assign zero
                        strGoogleLogLeftTo = "";
                        strGoogleLogLeftFrom = "";
                        strGoogleLogRightTo = "";
                        strGoogleLogRightFrom = "";
                        
                        if (txtUtranTOADDR_L.Text == "")
	                    {
		                    strGoogleLogLeftTo = "0";
	                    }
                        else
	                    {
                            strGoogleLogLeftTo = txtUtranTOADDR_L.Text;
	                    }
                        if (txtUtranFROMADDR_L.Text == "")
	                    {
		                     strGoogleLogLeftFrom = "0";
	                    }
                        else
	                    {
                            strGoogleLogLeftFrom = txtUtranFROMADDR_L.Text;
	                    }
                        if (txtUtranFROMADDR_R.Text == "")
	                    {
		                    strGoogleLogRightFrom = "0";
	                    }
                        else
	                    {
                            strGoogleLogRightFrom = txtUtranFROMADDR_R.Text;
	                    }
                        if (txtUtranTOADDR_R.Text == "")
	                    {
		                    strGoogleLogRightTo = "0";
	                    }
                        else
	                    {
                            strGoogleLogRightTo = txtUtranTOADDR_R.Text;
	                    }

                        // get city from muni layer for google doc city field
                        clsGlobals.strGoogleSpreadsheetCityField = getCityFromSpatialIntersect(arcCountyFeature);

                        //string together the agrc street segment
                        clsGlobals.strAgrcSegment = strGoogleLogLeftFrom + "-" + strGoogleLogLeftTo + " " + strGoogleLogRightFrom + "-" + strGoogleLogRightTo + " " + txtUtranPreDir.Text.Trim() + " " + txtUtranStName.Text.Trim() + " " + txtUtranStType.Text.Trim() + " " + txtUtranPOSTDIR.Text.Trim();

                        //call the google api to transfer values to the spreadsheet
                        clsUtransEditorStaticClass.AddRowToGoogleSpreadsheet();

                        //move onto save in utrans
                        break;
                }


                // BEGIN TO SAVE DATA IN UTRANS //

                //get query filter for utrans oid
                IQueryFilter arcUtransEdit_QueryFilter = new QueryFilter();
                arcUtransEdit_QueryFilter.WhereClause = "OBJECTID = " + strUtransOID;

                //get the feaure to update/save
                IFeatureCursor arcUtransEdit_FeatCur = clsGlobals.arcGeoFLayerUtransStreets.Search(arcUtransEdit_QueryFilter, false);
                IFeature arcUtransEdit_Feature = arcUtransEdit_FeatCur.NextFeature();

                //make sure a record is selected for editing
                if (arcUtransEdit_Feature != null)
                {
                    //set the current edit layer to the utrans street layer - this tells the operation what layer gets the new feature
                    IEditLayers arcEditLayers = clsGlobals.arcEditor as IEditLayers;
                    arcEditLayers.SetCurrentLayer(clsGlobals.arcGeoFLayerUtransStreets, 0);

                    //start the edit operation
                    clsGlobals.arcEditor.StartOperation();

                    //loop through the control save the changes to utrans
                    for (int i = 0; i < ctrlList.Count; i++)
                    {
                        Control ctrlCurrent = ctrlList[i];

                        //make sure the control is not for county streets, aka it doesn't contain Co 
                        if (!ctrlCurrent.Tag.ToString().Contains("Co"))
                        {
                            //check for emptly values in the numeric fields and populate with zeros in utrans
                            if (ctrlCurrent.Tag.ToString() == "LeftFromAddress" & ctrlCurrent.Text.ToString() == "")
                            {
                                arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindFieldByAliasName(ctrlCurrent.Tag.ToString()), 0);
                                //break;
                            }
                            else if (ctrlCurrent.Tag.ToString() == "LeftToAddress" & ctrlCurrent.Text.ToString() == "")
                            {
                                arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindFieldByAliasName(ctrlCurrent.Tag.ToString()), 0);
                                //break;
                            }
                            else if (ctrlCurrent.Tag.ToString() == "RightFromAddress" & ctrlCurrent.Text.ToString() == "")
                            {
                                arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindFieldByAliasName(ctrlCurrent.Tag.ToString()), 0);
                                //break;
                            }
                            else if (ctrlCurrent.Tag.ToString() == "RightToAddress" & ctrlCurrent.Text.ToString() == "")
                            {
                                arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindFieldByAliasName(ctrlCurrent.Tag.ToString()), 0);
                                //break;
                            }
                            else
                            {
                                //populate the field with the value in the corresponding textbox
                                arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindFieldByAliasName(ctrlCurrent.Tag.ToString()), ctrlCurrent.Text.Trim());
                            }
                        }
                    }

                    //populate some other fields...

                    //get the midpoint of the line segment for doing spatial queries (intersects)
                    IGeometry arcUtransEdits_geometry = arcUtransEdit_Feature.ShapeCopy;
                    IPolyline arcUtransEdits_polyline = arcUtransEdits_geometry as IPolyline;
                    IPoint arcUtransEdits_midPoint = new ESRI.ArcGIS.Geometry.Point();

                    //get the midpoint of the line, pass it into a point
                    arcUtransEdits_polyline.QueryPoint(esriSegmentExtension.esriNoExtension, 0.5, true, arcUtransEdits_midPoint);
                    //MessageBox.Show("The midpoint of the selected line segment is: " + arcUtransEdits_midPoint.X.ToString() + ", " + arcUtransEdits_midPoint.Y.ToString());

                    // spatial intersect for right and left fields //
                    // ZIPCODE_L and ZIPCODE_R (use iconstructpoint.constructoffset method to offset the midpoint of the line)
                    // test the iconstructpoint.constructtooffset mehtod
                    IConstructPoint arcConstructionPoint_posRight = new PointClass();
                    IConstructPoint arcConstructionPoint_negLeft = new PointClass();
                    
                    // call offset mehtod to get a point along the curve's midpoint - offsetting in the postive position (esri documentation states that positive offset will always return point on the right side of the curve)
                    arcConstructionPoint_posRight.ConstructOffset(arcUtransEdits_polyline, esriSegmentExtension.esriNoExtension, 0.5, true, 15);  // 10 meters is about 33 feet (15 is about 50 feet)
                    IPoint outPoint_posRight = arcConstructionPoint_posRight as IPoint;
                    //MessageBox.Show("for positive/right offset: " + outPoint_posRight.X + " , " + outPoint_posRight.Y);

                    // call offset mehtod to get a point along the curve's midpoint - offsetting in the negative position (esri documentation states that negative offset will always return point on the left-side of curve)
                    arcConstructionPoint_negLeft.ConstructOffset(arcUtransEdits_polyline, esriSegmentExtension.esriNoExtension, 0.5, true, -15);  // -10 meters is about -33 feet (15 is about 50 feet)
                    IPoint outPoint_negLeft = arcConstructionPoint_negLeft as IPoint;
                    //MessageBox.Show("for negative/left offset: " + outPoint_negLeft.X + " , " + outPoint_negLeft.Y);


                    // __LEFT SPATIAL FIELDS__ //
                    ISpatialFilter arcSpatialFilter_left = new SpatialFilter();
                    arcSpatialFilter_left.Geometry = outPoint_negLeft;
                    arcSpatialFilter_left.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

                    // query ZIPCODE layer left
                    IFeatureCursor arcZipCursor_left = clsGlobals.arcFLayerZipCodes.Search(arcSpatialFilter_left, false);
                    IFeature arcFeatureZip_left = arcZipCursor_left.NextFeature();
                    if (arcFeatureZip_left != null)
                    {
                        //update the value in the utrans based on the intersect
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("ZIPCODE_L"), arcFeatureZip_left.get_Value(arcFeatureZip_left.Fields.FindField("ZIP5")));
                        //arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("ZIPCODE_R"), arcFeatureZip_left.get_Value(arcFeatureZip_left.Fields.FindField("ZIP5")));
                        //maybe update the POSTCOMM_L field as well with the "name" field from the zipcodes layer
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("POSTCOMM_L"), arcFeatureZip_left.get_Value(arcFeatureZip_left.Fields.FindField("NAME")).ToString().Trim().ToUpper());
                    }
                    else
                    {
                        MessageBox.Show("A zipcode could not be found on the left side of the segment - based on the segment's midpoint with a 15 meter offset.", "Easy there Turbo!");
                        //give option to leave blank or abort edit operation and return
                        //return;
                    }
                    //clear out variables
                    // release the cursor
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(arcZipCursor_left);
                    //GC.Collect();
                    arcZipCursor_left = null;
                    arcFeatureZip_left = null;


                    // query the MNUI layer left
                    IFeatureCursor arcMuniCursor_left = clsGlobals.arcFLayerMunicipalities.Search(arcSpatialFilter_left, false);
                    IFeature arcFeatureMuni_left = arcMuniCursor_left.NextFeature();

                    if (arcFeatureMuni_left != null)
                    {
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("INCMUNI_L"), arcFeatureMuni_left.get_Value(arcFeatureMuni_left.Fields.FindField("NAME")).ToString().ToUpper().Trim());
                    }
                    else
                    {
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("INCMUNI_L"), "");
                        //MessageBox.Show("A Municipality/City could not be found on the left side of the segment - based on the segment's midpoint with a 15 meter offset.", "Easy there Turbo!");
                    }

                    System.Runtime.InteropServices.Marshal.ReleaseComObject(arcMuniCursor_left);
                    arcMuniCursor_left = null;
                    arcFeatureMuni_left = null;
                    //arcSpatialFilter_left = null;


                    // query the ADDRESS SYSTEM layer left
                    IFeatureCursor arcAddrSysCursor_left = clsGlobals.arcFLayerAddrSysQuads.Search(arcSpatialFilter_left, false);
                    IFeature arcFeatureAddrSys_left = arcAddrSysCursor_left.NextFeature();

                    if (arcFeatureAddrSys_left != null)
                    {
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("ADDRSYS_L"), arcFeatureAddrSys_left.get_Value(arcFeatureAddrSys_left.Fields.FindField("GRID_NAME")).ToString().ToUpper().Trim());
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("QUADRANT_L"), arcFeatureAddrSys_left.get_Value(arcFeatureAddrSys_left.Fields.FindField("QUADRANT")).ToString().ToUpper().Trim());
                    }
                    else
                    {
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("ADDRSYS_L"), "");
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("QUADRANT_L"), "");
                        //MessageBox.Show("A Municipality/City could not be found on the left side of the segment - based on the segment's midpoint with a 15 meter offset.", "Easy there Turbo!");
                    }

                    System.Runtime.InteropServices.Marshal.ReleaseComObject(arcFeatureAddrSys_left);
                    arcAddrSysCursor_left = null;
                    arcFeatureAddrSys_left = null;
                    //arcSpatialFilter_left = null;


                    // query the COUNTY layer left
                    IFeatureCursor arcCountyCursor_left = clsGlobals.arcFLayerCounties.Search(arcSpatialFilter_left, false);
                    IFeature arcFeatureCounty_left = arcCountyCursor_left.NextFeature();

                    if (arcFeatureCounty_left != null)
                    {
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("COUNTY_L"), arcFeatureCounty_left.get_Value(arcFeatureCounty_left.Fields.FindField("FIPS_STR")));
                    }
                    else
                    {
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("COUNTY_L"), "");
                        MessageBox.Show("A county could not be found on the left side of the segment - based on the segment's midpoint with a 15 meter offset.", "Easy there Turbo!");
                        //MessageBox.Show("A Municipality/City could not be found on the left side of the segment - based on the segment's midpoint with a 15 meter offset.", "Easy there Turbo!");
                    }

                    System.Runtime.InteropServices.Marshal.ReleaseComObject(arcFeatureCounty_left);
                    arcCountyCursor_left = null;
                    arcFeatureCounty_left = null;
                    //arcSpatialFilter_left = null;


                    // query the UNINCCOM layer left
                    IFeatureCursor arcMetroAreasCursor_left = clsGlobals.arcFLayerMetroTwnShips.Search(arcSpatialFilter_left, false);
                    IFeature arcFeatureMetroAreas_left = arcMetroAreasCursor_left.NextFeature();

                    if (arcFeatureMetroAreas_left != null)
                    {
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("UNINCCOM_L"), arcFeatureMetroAreas_left.get_Value(arcFeatureMetroAreas_left.Fields.FindField("SHORTDESC")).ToString().ToUpper().Trim());
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(arcFeatureMetroAreas_left);
                    }
                    else
                    {
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("UNINCCOM_L"), "");
                    }

                    arcMetroAreasCursor_left = null;
                    arcFeatureMetroAreas_left = null;
                    //arcSpatialFilter_left = null;


                    // __RIGHT SPATIAL FIELDS__ //
                    ISpatialFilter arcSpatialFilter_right = new SpatialFilter();
                    arcSpatialFilter_right.Geometry = outPoint_posRight;
                    arcSpatialFilter_right.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

                    // query ZIPCODE layer right
                    IFeatureCursor arcZipCursor_right = clsGlobals.arcFLayerZipCodes.Search(arcSpatialFilter_right, false);
                    IFeature arcFeatureZip_right = arcZipCursor_right.NextFeature();
                    if (arcFeatureZip_right != null)
                    {
                        //update the value in the utrans based on the intersect
                        //arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("ZIPCODE_L"), arcFeatureZip_right.get_Value(arcFeatureZip_right.Fields.FindField("ZIP5")));
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("ZIPCODE_R"), arcFeatureZip_right.get_Value(arcFeatureZip_right.Fields.FindField("ZIP5")));
                        //maybe update the POSTCOMM_L field as well with the "name" field from the zipcodes layer
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("POSTCOMM_L"), arcFeatureZip_right.get_Value(arcFeatureZip_right.Fields.FindField("NAME")).ToString().Trim().ToUpper());
                    }
                    else
                    {
                        MessageBox.Show("A zipcode could not be found on the right side of the segment - based on the segment's midpoint with a 15 meter offset.", "Easy there Turbo!");
                        //give option to leave blank or abort edit operation and return
                        //return;
                    }
                    //clear out variables
                    // release the cursor
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(arcFeatureZip_right);
                    //GC.Collect();
                    arcFeatureZip_right = null;
                    arcFeatureZip_right = null;


                    // query the MUNI layer right
                    IFeatureCursor arcMuniCursor_right = clsGlobals.arcFLayerMunicipalities.Search(arcSpatialFilter_right, false);
                    IFeature arcFeatureMuni_right = arcMuniCursor_right.NextFeature();

                    if (arcFeatureMuni_right != null)
                    {
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("INCMUNI_R"), arcFeatureMuni_right.get_Value(arcFeatureMuni_right.Fields.FindField("NAME")).ToString().ToUpper().Trim());
                    }
                    else
                    {
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("INCMUNI_R"), "");
                        //MessageBox.Show("A Municipality/City could not be found on the right side of the segment - based on the segment's midpoint with a 15 meter offset.", "Easy there Turbo!");
                    }

                    System.Runtime.InteropServices.Marshal.ReleaseComObject(arcMuniCursor_right);
                    arcMuniCursor_right = null;
                    arcFeatureMuni_right = null;
                    //arcSpatialFilter_right = null;


                    // query the ADDRESS SYSTEM layer right
                    IFeatureCursor arcAddrSysCursor_right = clsGlobals.arcFLayerAddrSysQuads.Search(arcSpatialFilter_right, false);
                    IFeature arcFeatureAddrSys_right = arcAddrSysCursor_right.NextFeature();

                    if (arcFeatureAddrSys_right != null)
                    {
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("ADDRSYS_R"), arcFeatureAddrSys_right.get_Value(arcFeatureAddrSys_right.Fields.FindField("GRID_NAME")).ToString().ToUpper().Trim());
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("QUADRANT_R"), arcFeatureAddrSys_right.get_Value(arcFeatureAddrSys_right.Fields.FindField("QUADRANT")).ToString().ToUpper().Trim());
                    }
                    else
                    {
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("ADDRSYS_R"), "");
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("QUADRANT_R"), "");
                        //MessageBox.Show("A Municipality/City could not be found on the right side of the segment - based on the segment's midpoint with a 15 meter offset.", "Easy there Turbo!");
                    }

                    System.Runtime.InteropServices.Marshal.ReleaseComObject(arcFeatureAddrSys_right);
                    arcAddrSysCursor_right = null;
                    arcFeatureAddrSys_right = null;
                    //arcSpatialFilter_right = null;


                    // query the COUNTY layer right
                    IFeatureCursor arcCountyCursor_right = clsGlobals.arcFLayerCounties.Search(arcSpatialFilter_right, false);
                    IFeature arcFeatureCounty_right = arcCountyCursor_right.NextFeature();

                    if (arcFeatureCounty_right != null)
                    {
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("COUNTY_R"), arcFeatureCounty_right.get_Value(arcFeatureCounty_right.Fields.FindField("FIPS_STR")));
                    }
                    else
                    {
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("COUNTY_R"), "");
                        MessageBox.Show("A county could not be found on the right side of the segment - based on the segment's midpoint with a 15 meter offset.", "Easy there Turbo!");
                        //MessageBox.Show("A Municipality/City could not be found on the right side of the segment - based on the segment's midpoint with a 15 meter offset.", "Easy there Turbo!");
                    }

                    System.Runtime.InteropServices.Marshal.ReleaseComObject(arcFeatureCounty_right);
                    arcCountyCursor_right = null;
                    arcFeatureCounty_right = null;
                    //arcSpatialFilter_right = null;


                    // query the UNINCCOM layer right
                    IFeatureCursor arcMetroAreasCursor_right = clsGlobals.arcFLayerMetroTwnShips.Search(arcSpatialFilter_right, false);
                    IFeature arcFeatureMetroAreas_right = arcMetroAreasCursor_right.NextFeature();

                    if (arcFeatureMetroAreas_right != null)
                    {
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("UNINCCOM_R"), arcFeatureMetroAreas_right.get_Value(arcFeatureMetroAreas_right.Fields.FindField("SHORTDESC")).ToString().ToUpper().Trim());
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(arcFeatureMetroAreas_right);
                    }
                    else
                    {
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("UNINCCOM_R"), "");
                    }

                    arcMetroAreasCursor_right = null;
                    arcFeatureMetroAreas_right = null;
                    //arcSpatialFilter_right = null;


                    // null out the offset points and spatial filters
                    outPoint_posRight = null;
                    outPoint_negLeft = null;
                    arcSpatialFilter_right = null;
                    arcSpatialFilter_left = null;


                    ////// THIS CODE IS FOR NON RIGHT/LEFT FIELDS, WHICH WE NOLONGER HAVE IN THE NEW NG SCHEMA //
                    ////// ADDRSYS_L and QUADRANT_L
                    ////ISpatialFilter arcSpatialFilter = new SpatialFilter();
                    ////arcSpatialFilter.Geometry = arcUtransEdits_midPoint;
                    ////arcSpatialFilter.GeometryField = "SHAPE";
                    ////arcSpatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                    ////arcSpatialFilter.SubFields = "*";

                    ////IFeatureCursor arcAddrSysCursor = clsGlobals.arcFLayerAddrSysQuads.Search(arcSpatialFilter, false);
                    ////IFeature arcFeatureAddrSys = arcAddrSysCursor.NextFeature();
                    ////if (arcFeatureAddrSys != null)
                    ////{
                    ////    //update the value in the utrans based on the intersect
                    ////    arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("ADDRSYS_L"), arcFeatureAddrSys.get_Value(arcFeatureAddrSys.Fields.FindField("GRID_NAME")).ToString().Trim());
                    ////    arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("QUADRANT_L"), arcFeatureAddrSys.get_Value(arcFeatureAddrSys.Fields.FindField("QUADRANT")).ToString().Trim());
                    ////}
                    ////else
                    ////{
                    ////    MessageBox.Show("The midpoint of the street segment you are trying to update is not within an AddressSystemQuadrants.", "Easy there Turbo!");
                    ////    //give option to leave blank or abort edit operation and return
                    ////    //return;
                    ////}
                    //////clear out variables
                    ////System.Runtime.InteropServices.Marshal.ReleaseComObject(arcAddrSysCursor);
                    ////arcAddrSysCursor = null;
                    ////arcFeatureAddrSys = null;

                    ////// COUNTY_L
                    ////IFeatureCursor arcCountiesCursor = clsGlobals.arcFLayerCounties.Search(arcSpatialFilter, false);
                    ////IFeature arcFeature_County = arcCountiesCursor.NextFeature();
                    ////if (arcFeature_County != null)
                    ////{
                    ////    //update the value in the utrans based on the intersect
                    ////    arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("COUNTY_L"), arcFeature_County.get_Value(arcFeature_County.Fields.FindField("FIPS_STR")));
                    ////}
                    ////else
                    ////{
                    ////    MessageBox.Show("The midpoint of the street segment you are trying to update is not within a County.", "Easy there Turbo!");
                    ////    //give option to leave blank or abort edit operation and return
                    ////    //return;
                    ////}
                    //////clear out variables
                    ////arcCountiesCursor = null;
                    ////arcFeature_County = null;



                    // FULLNAME //
                    //check if street name is numeric
                    int intStName;
                    if (int.TryParse(txtUtranStName.Text, out intStName))
                    {
                        string strFullNameNumeric = txtUtranStName.Text.Trim() + " " + txtUtranPOSTDIR.Text.Trim();

                        //check if POSTDIR is populated and sttype is not
                        if (txtUtranPOSTDIR.Text == "" | txtUtranStType.Text != "")
                        {
                            DialogResult dialogResult2 = MessageBox.Show("Format Warning!  You are saving a numberic street but have conflict with either POSTDIR or POSTTYPE." + Environment.NewLine + "Numberic Streets typically require a POSTDIR value and not a POSTTYPE value." + Environment.NewLine + Environment.NewLine + "Would you like to continue with the save?", "Format Warning!", MessageBoxButtons.YesNo);
                            if (dialogResult2 == DialogResult.Yes)
                            {
                                arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("FULLNAME"), strFullNameNumeric.Trim());
                            }
                            else if (dialogResult2 == DialogResult.No)
                            {
                                return;
                            }
                        }
                        else
                        {
                            arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("FULLNAME"), strFullNameNumeric.Trim());
                        }
                    }
                    else //it's not a numeric street - it's alphabetic
                    {
                        string strFullNameAlpha = txtUtranStName.Text.Trim() + " " + txtUtranStType.Text.Trim();

                        //check if sttype is populated and POSTDIR is not
                        if (txtUtranPOSTDIR.Text != "" | txtUtranStType.Text == "")
                        {
                            DialogResult dialogResult3 = MessageBox.Show("Format Warning!  You are saving an alphabetic street but have conflict with either POSTDIR or POSTTYPE." + Environment.NewLine + "Alphabetic Streets typically require a POSTTYPE and often do not include a POSTDIR value." + Environment.NewLine + Environment.NewLine + "Would you like to continue with the save?", "Format Warning!", MessageBoxButtons.YesNo);
                            if (dialogResult3 == DialogResult.Yes)
                            {
                                arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("FULLNAME"), strFullNameAlpha.Trim());
                            }
                            else if (dialogResult3 == DialogResult.No)
                            {
                                return;
                            }
                        }
                        else
                        {
                            arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("FULLNAME"), strFullNameAlpha.Trim());
                        }
                    }

                    // ACSALIAS //
                    //string strAscAlias = txtUtransAN_NAME.Text.Trim() + " " + txtUtransAN_POSTDIR.Text.Trim();
                    //arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("ACSALIAS"), strAscAlias.Trim());

                    // CARTOCODE
                    if (cboCartoCode.SelectedIndex == 15) //this is the 99 value
                    {
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("CARTOCODE"), 99);
                    }
                    else if (cboCartoCode.SelectedIndex == -1)
                    {
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("CARTOCODE"), null);
                    }
                    else if (cboCartoCode.SelectedIndex == 16) //don't add one (as done in the else) to this case b/c of the 99 value throws-off the index thing, so it's 16
                    {
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("CARTOCODE"), 16);
                    }
                    else
                    {
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("CARTOCODE"), (cboCartoCode.SelectedIndex + 1));
                    }
                    
                    //store the feature if not a duplicate
                    arcUtransEdit_Feature.Store();

                    //stop the edit operation
                    clsGlobals.arcEditor.StopOperation("Street Edit");

                    //get the combobox value in a string
                    ////string strComboBoxTextValueDoubleQuotes = @"""" + strComboBoxTextValue + @"""";

                    ////arcCalculator.Cursor = arcCur_dfcLayer;
                    ////arcCalculator.Expression = strComboBoxTextValueDoubleQuotes;
                    ////arcCalculator.Field = "CURRENT_NOTES";
                    ////arcCalculator.Calculate();
                    ////arcCalculator.ShowErrorPrompt = true;

                    //////clear out the cursor
                    ////arcCur_dfcLayer = null;

                    // save the value to dfc_result layer
                    clsGlobals.arcEditor.StartOperation();
                    arcFeature_DFC.set_Value(arcFeature_DFC.Fields.FindField("CURRENT_NOTES"), strComboBoxTextValue);
                    arcFeature_DFC.Store();
                    clsGlobals.arcEditor.StopOperation("DFC_RESULT Update");

                    //unselect everything in map
                    arcMapp.ClearSelection();

                    //select the utrans street segment for user's visibility in ArcMap
                    //or call the onselection changed to refresh and update the form
                    //select the one record in the above asigned feature layer
                    IFeatureSelection featSelectUtransUpdated = clsGlobals.arcGeoFLayerUtransStreets as IFeatureSelection;
                    featSelectUtransUpdated.SelectFeatures(arcUtransEdit_QueryFilter, esriSelectionResultEnum.esriSelectionResultNew, false);
                    
                    //refresh the map layers and data
                    arcActiveView.Refresh();
                    arcActiveView.Refresh();

                    //update the feature count label on the form
                    //arcFeatureLayerDef = clsGlobals.arcGeoFLayerDfcResult as IFeatureLayerDefinition;
                    arcQFilterLabelCount = new QueryFilter();
                    arcQFilterLabelCount.WhereClause = arcFeatureLayerDef.DefinitionExpression;
                    int intDfcCount = clsGlobals.arcGeoFLayerDfcResult.DisplayFeatureClass.FeatureCount(arcQFilterLabelCount);
                    lblCounter.Text = intDfcCount.ToString();

                    //call selection changed - not sure if needed as there is a new selection above
                    frmUtransEditor_OnSelectionChanged();

                }
                else
                {
                    MessageBox.Show("Oops, an error occurred! Could not find a record in the UTRANS database base to update using the following query: " + arcUtransEdit_QueryFilter.ToString() + "." + Environment.NewLine + "Please check DFC_RESULT selection and try again.", "Error Saving to UTRANS!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            finally
            {
                //stop the edit operation
                clsGlobals.arcEditor.StopOperation("Street Edit");
                GC.Collect();
            }
        }




        //this method is called when the next button is clicked
        private void btnNext_Click(object sender, EventArgs e)
        {
            try
            {
                //variables used in this method
                IFeatureCursor arcFeatCur_zoomTo = null;
                IFeature arcFeature_zoomTo = null;



                //check if any features are selected
                arcFeatureSelection = clsGlobals.arcGeoFLayerDfcResult as IFeatureSelection;
                arcSelSet = arcFeatureSelection.SelectionSet;

                //make sure one feature is selected, else get first record in set
                if (arcSelSet.Count == 1)
                {
                    //get a cursor of the selected features
                    ICursor arcCursor;
                    arcSelSet.Search(null, false, out arcCursor);

                    //get the first row (there should only be one)
                    IRow arcRow = arcCursor.NextRow();

                    //get the objectid from dfc layer
                    string strDFC_ResultOID = arcRow.get_Value(arcRow.Fields.FindField("OBJECTID")).ToString();

                    //select a feature that has a oid greater than the one selected (the next button gets the next feature in the table)
                    IFeatureCursor arcUtransGetNextBtn_FeatCursor = clsGlobals.arcGeoFLayerDfcResult.SearchDisplayFeatures(null, false);
                    IFeature arcUtransGetNextBtn_Feature = arcUtransGetNextBtn_FeatCursor.NextFeature();

                    IQueryFilter arcQueryFilter = new QueryFilter();
                    arcQueryFilter.WhereClause = "OBJECTID > " + strDFC_ResultOID;

                    //get a new feature cursor with all records that are greater than the selected oid
                    IFeatureCursor arcUtransGetNextBtn_FeatCursor2 = clsGlobals.arcGeoFLayerDfcResult.SearchDisplayFeatures(arcQueryFilter, false);
                    IFeature arcUtransGetNextBtn_Feature2 = arcUtransGetNextBtn_FeatCursor2.NextFeature();

                    //get oid of this feature and then pass it into a query filter to select from
                    string strNextOID = arcUtransGetNextBtn_Feature2.get_Value(arcUtransGetNextBtn_Feature2.Fields.FindField("OBJECTID")).ToString();

                    //create query filter for the next highest oid in the table - based on the one that's currently selected
                    IQueryFilter arcQueryFilter2 = null;
                    arcQueryFilter2 = new QueryFilter();
                    arcQueryFilter2.WhereClause = "OBJECTID = " + strNextOID;


                    //select the one record in the above asigned feature layer
                    IFeatureSelection featSelect = clsGlobals.arcGeoFLayerDfcResult as IFeatureSelection;
                    featSelect.SelectFeatures(arcQueryFilter2, esriSelectionResultEnum.esriSelectionResultNew, false);
                    //featSelect.SelectionChanged();

                    //get the selected record as a feature so we can zoom to it below
                    arcFeatCur_zoomTo = clsGlobals.arcGeoFLayerDfcResult.Search(arcQueryFilter2, false);
                    arcFeature_zoomTo = arcFeatCur_zoomTo.NextFeature();


                    //clear out variables
                    arcCursor = null;
                    arcRow = null;
                    strDFC_ResultOID = null;
                    arcUtransGetNextBtn_FeatCursor = null;
                    arcUtransGetNextBtn_Feature = null;
                    arcQueryFilter = null;
                    arcUtransGetNextBtn_FeatCursor2 = null;
                    arcUtransGetNextBtn_Feature2 = null;
                    strNextOID = null;
                    arcQueryFilter2 = null;
                    featSelect = null; 
                }
                else //nothing is selected, so query the whole fc and get first record
                {

                    //select 
                    IFeatureCursor arcUtransGetNextBtn_FeatCursor3 = clsGlobals.arcGeoFLayerDfcResult.SearchDisplayFeatures(null, false);
                    IFeature arcUtransGetNextBtn_Feature3 = arcUtransGetNextBtn_FeatCursor3.NextFeature();

                    IQueryFilter arcQueryFilter3 = new QueryFilter();
                    arcQueryFilter3.WhereClause = "OBJECTID = " + arcUtransGetNextBtn_Feature3.get_Value(arcUtransGetNextBtn_Feature3.Fields.FindField("OBJECTID"));

                    IFeatureSelection featSelect3 = clsGlobals.arcGeoFLayerDfcResult as IFeatureSelection;
                    featSelect3.SelectFeatures(arcQueryFilter3, esriSelectionResultEnum.esriSelectionResultNew, false);
                    //featSelect.SelectionChanged();

                    //get the selected record as a feature so we can zoom to it below
                    arcFeatCur_zoomTo = clsGlobals.arcGeoFLayerDfcResult.Search(arcQueryFilter3, false);
                    arcFeature_zoomTo = arcFeatCur_zoomTo.NextFeature();

                    //clear out variables
                    arcUtransGetNextBtn_FeatCursor3 = null;
                    arcUtransGetNextBtn_Feature3 = null;
                    arcQueryFilter3 = null;
                    featSelect3 = null;
                }


                // zoom to the selected feature //
                //define an envelope to zoom to
                IEnvelope arcEnv = new EnvelopeClass();
                arcEnv = arcFeature_zoomTo.Shape.Envelope;

                arcEnv.Expand(1.5, 1.5, true);
                arcActiveView.Extent = arcEnv;
                arcActiveView.Refresh();


                //call change seleted - not sure if i need to do this, it might be automatic
                frmUtransEditor_OnSelectionChanged();
            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }




        //this method copies the selected county road segment and pastes it into the utrans database
        private void btnCopyNewSegment_Click(object sender, EventArgs e)
        {
            try
            {
                //UID uID = new UID();
                //uID.Value = "esriEditor.Editor";
                //if (clsGlobals.arcApplication == null)
                //    return;

                //IEditor arcEditor = clsGlobals.arcApplication.FindExtensionByCLSID(uID) as IEditor;
                
                //or just use the global reference
                //clsGlobals.arcEditor;

                //get access to the selected feature in county roads dataset 
                IObjectLoader objectLoader = new ObjectLoaderClass();
                IEnumInvalidObject invalidObjectEnum;

                //create query filter to get the new segment (from county fc)
                IQueryFilter arcQueryFilter_loadSegment = new QueryFilter();
                arcQueryFilter_loadSegment.SubFields = "Shape,STATUS,CARTOCODE,FULLNAME,FROMADDR_L,TOADDR_L,FROMADDR_R,TOADDR_R,PARITY_L,PARITY_R,PREDIR,NAME,POSTTYPE,POSTDIR,AN_NAME,AN_POSTDIR,A1_PREDIR,A1_NAME,A1_POSTTYPE,A1_POSTDIR,A2_PREDIR,A2_NAME,A2_POSTTYPE,A2_POSTDIR,QUADRANT_L,QUADRANT_R,STATE_L,STATE_R,COUNTY_L,COUNTY_R,ADDRSYS_L,ADDRSYS_R,POSTCOMM_L,POSTCOMM_R,ZIPCODE_L,ZIPCODE_R,INCMUNI_L,INCMUNI_R,UNINCCOM_L,UNINCCOM_R,NBRHDCOM_L,NBRHDCOM_R,ER_CAD_ZONES,ESN_L,ESN_R,MSAGCOMM_L,MSAGCOMM_R,ONEWAY,VERT_LEVEL,SPEED_LMT,ACCESSCODE,DOT_HWYNAM,DOT_RTNAME,DOT_RTPART,DOT_F_MILE,DOT_T_MILE,DOT_FCLASS,DOT_SRFTYP,DOT_CLASS,DOT_OWN_L,DOT_OWN_R,DOT_AADT,DOT_AADTYR,DOT_THRULANES,BIKE_L,BIKE_R,BIKE_PLN_L,BIKE_PLN_R,BIKE_REGPR,BIKE_NOTES,UNIQUE_ID,LOCAL_UID,UTAHRD_UID,SOURCE,UPDATED,EFFECTIVE,EXPIRE,CREATED,CREATOR,EDITOR,CUSTOMTAGS";
                //OLD SCHEMA >> arcQueryFilter_loadSegment.SubFields = "Shape,ZIPCODE_L,ZIPCODE_R,FROMADDR_L,TOADDR_L,FROMADDR_R,TOADDR_R,PREDIR,NAME,POSTTYPE,POSTDIR,A1_PREDIR,A1_NAME,A1_POSTTYPE,A1_POSTDIR,A2_PREDIR,A2_NAME,A2_POSTTYPE,A2_POSTDIR,AN_NAME,AN_POSTDIR,POSTCOMM_L,ONEWAY,SPEED_LMT_LMT,VERT_LEVEL,DOT_CLASS,MODIFYDATE,COLLDATE,ACCURACY,SOURCE,NOTES,STATUS,ACCESS,USAGENOTES,BIKE_L,BIKE_R,BIKE_NOTES,BIKE_STATUS,GRID1MIL,GRID100K";
                arcQueryFilter_loadSegment.WhereClause = "OBJECTID = " + strCountyOID;

                //get the county roads segment for quering new utrans street segment below
                IFeatureCursor arcFeatCur_CountyLoadSegment = clsGlobals.arcGeoFLayerCountyStreets.Search(arcQueryFilter_loadSegment, false);
                IFeature arcFeature_CountyLoadSegment = arcFeatCur_CountyLoadSegment.NextFeature();

                IFeatureClass arcFeatClassCounty = clsGlobals.arcGeoFLayerCountyStreets.FeatureClass;
                IFeatureClass arcFeaClassUtrans = clsGlobals.arcGeoFLayerUtransStreets.FeatureClass;

                //OutputFields parameter needs to match sub-fields in input queryfilter
                IFields allFields = arcFeaClassUtrans.Fields;
                IFields outFields = new FieldsClass();
                IFieldsEdit outFieldsEdit = outFields as IFieldsEdit;
                // Get the query filter sub-fields as an array
                // and loop through each field in turn,
                // adding it to the ouput fields
                String[] subFields = (arcQueryFilter_loadSegment.SubFields).Split(',');
                for (int j = 0; j < subFields.Length; j++)
                {
                    int fieldID = allFields.FindField(subFields[j]);
                    if (fieldID == -1)
                    {
                        System.Windows.Forms.MessageBox.Show("field not found: " + subFields[j]);
                        return;
                    }
                    outFieldsEdit.AddField(allFields.get_Field(fieldID));
                }


                //load the feature into utrans
                objectLoader.LoadObjects(
                    null,
                    (ITable)arcFeatClassCounty,
                    arcQueryFilter_loadSegment,
                    (ITable)arcFeaClassUtrans,
                    outFields,
                    false,
                    0,
                    false,
                    false,
                    10,
                    out invalidObjectEnum
                );

                //verify that the feature loaded
                IInvalidObjectInfo invalidObject = invalidObjectEnum.Next();
                if (invalidObject != null)
                {
                    System.Windows.Forms.MessageBox.Show("Something went wrong... the County road segment did not load in the Utrans database.");
                }


                //create variables for the address range where clause, in case empty values
                string strFROMADDR_L = arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("FROMADDR_L")).ToString().Trim();
                string strTOADDR_L = arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("TOADDR_L")).ToString().Trim();
                string strFROMADDR_R = arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("FROMADDR_R")).ToString().Trim();
                string strTOADDR_R = arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("TOADDR_R")).ToString().Trim();


                //check for road segment has empty values for street range, if so pass in zero in where clause
                if (strFROMADDR_L == "")
                {
                    strFROMADDR_L = "is null";
                }
                else
                {
                    strFROMADDR_L = "= " + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("FROMADDR_L")).ToString();
                }

                if (strTOADDR_L == "")
                {
                    strTOADDR_L = "is null";
                }
                else
                {
                    strTOADDR_L = "= " + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("TOADDR_L")).ToString();
                }

                if (strFROMADDR_R == "")
                {
                    strFROMADDR_R = "is null";
                }
                else
                {
                    strFROMADDR_R = "= " + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("FROMADDR_R")).ToString();
                }

                if (strTOADDR_R == "")
                {
                    strTOADDR_R = "is null";
                }
                else
                {
                    strTOADDR_R = "= " + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("TOADDR_R")).ToString();
                }

                //select the new feature in the utrans database - based on values in the county street layer
                IQueryFilter arcQueryFilterNewUtransSegment = new QueryFilter();
                arcQueryFilterNewUtransSegment.WhereClause =
                    "FROMADDR_L " + strFROMADDR_L +
                    " AND TOADDR_L " + strTOADDR_L +
                    " AND FROMADDR_R " + strFROMADDR_R +
                    " AND TOADDR_R " + strTOADDR_R +
                    " AND PREDIR = '" + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("PREDIR")) + "'" +
                    " AND NAME = '" + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("NAME")) + "'" +
                    " AND POSTTYPE = '" + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("POSTTYPE")) + "'" +
                    " AND POSTDIR = '" + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("POSTDIR")) + "'";
                    //" AND A1_NAME = '" + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("A1_NAME")) + "'" +
                    //" AND A1_POSTTYPE = '" + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("A1_POSTTYPE")) + "'" +
                    //" AND A2_NAME = '" + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("A2_NAME")) + "'" +
                    //" AND A2_POSTTYPE = '" + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("A2_POSTTYPE")) + "'" +
                    //" AND ACSALIAS = '" + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("ACSALIAS")) + "'" +
                    //" AND AN_POSTDIR = '" + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("AN_POSTDIR")) + "'";

                //create feature cursor for getting new road segment 
                IFeatureCursor arcFeatCur_UtransNewSegment = clsGlobals.arcGeoFLayerUtransStreets.SearchDisplayFeatures(arcQueryFilterNewUtransSegment, false);
                IFeature arcFeature_UtransNewSegment; // = arcFeatCur_UtransNewSegment.NextFeature();

                //check if there are duplicate records in the table - if not preceed, else give message box and return
                int intUtransFeatCount = 0;
                string strNewStreetOID = "";
                
                while ((arcFeature_UtransNewSegment = arcFeatCur_UtransNewSegment.NextFeature()) != null)
                {
                    strNewStreetOID = arcFeature_UtransNewSegment.get_Value(arcFeature_UtransNewSegment.Fields.FindField("OBJECTID")).ToString();
                    intUtransFeatCount = intUtransFeatCount + 1;
                }

                
                //check for duplcate records - use less than two b/c if the number ranges are null it's doesn't find a match in utrans so it's 0
                if (intUtransFeatCount == 1)
                {
                    //calc values in the dfc table to show the new oid

                    IQueryFilter arcQueryFilter_DFC_updateOID = new QueryFilter();
                    arcQueryFilter_DFC_updateOID.WhereClause = "OBJECTID = " + strDFC_RESULT_oid;

                    //proceed with calculating values in the dfc table - 
                    IFeatureCursor arcFeatCursor_DFC = clsGlobals.arcGeoFLayerDfcResult.Search(arcQueryFilter_DFC_updateOID, false);
                    IFeature arcFeat_dFC = arcFeatCursor_DFC.NextFeature();

                    if (arcFeat_dFC == null)
                    {
                        MessageBox.Show("Could not find a feature in the DFC_RESULT layer with OID: " + strDFC_RESULT_oid, "OID Not Found", MessageBoxButtons.OK);
                        return;
                    }

                    clsGlobals.arcEditor.StartOperation();
                    arcFeat_dFC.set_Value(arcFeat_dFC.Fields.FindField("BASE_FID"), strNewStreetOID);
                    arcFeat_dFC.Store();
                    clsGlobals.arcEditor.StopOperation("DFC N OID Update");

                    ////ICalculator arcCalculator = new Calculator();
                    ////ICursor arcCur_dfcLayer = clsGlobals.arcGeoFLayerDfcResult.FeatureClass.Update(arcQueryFilter_DFC_updateOID, true) as ICursor;

                    ////arcCalculator.Cursor = arcCur_dfcLayer;
                    ////arcCalculator.Expression = strNewStreetOID;
                    ////arcCalculator.Field = "BASE_FID";
                    ////arcCalculator.Calculate();
                    ////arcCalculator.ShowErrorPrompt = true;

                    //clear out the cursor
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(arcFeatCursor_DFC);
                    arcFeatCursor_DFC = null;
                }
                else if (intUtransFeatCount > 1)
                {
                    MessageBox.Show("The new road segment that was just copied into the Utrans database has duplicate attributes with an existing segment! Please investigate and proceed as necessary.", "Duplicate Attributes!", MessageBoxButtons.OK, MessageBoxIcon.Warning);


                }
                else if (intUtransFeatCount == 0)
                {
                    MessageBox.Show("Warning... The new road segment that was just copied into the Utrans database could not be found with the following defintion query: " + arcQueryFilterNewUtransSegment.WhereClause.ToString(), "Not Found in Utrans", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }


                //select new feature from utrans
                IQueryFilter arcQueryFilter_NewSteetUtrans = new QueryFilter();
                arcQueryFilter_NewSteetUtrans.WhereClause = "OBJECTID = " + strNewStreetOID;

                IFeatureSelection featSelectUtransUpdated = clsGlobals.arcGeoFLayerUtransStreets as IFeatureSelection;
                featSelectUtransUpdated.SelectFeatures(arcQueryFilter_NewSteetUtrans, esriSelectionResultEnum.esriSelectionResultNew, false);


                if (chkShowVertices.Checked == true)
                {
                    displayVerticesOnNew();
                }


                //refresh the map layers and data
                arcActiveView.Refresh(); //.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);
                arcActiveView.Refresh();

                //select the dfc layer again with now the new object id on the utrans segment (base_fid) now has an oid instead of a "-1" value
                //IFeatureSelection arcFeatSelection_dfcNewUtransOID;

                //call on selection changed
                frmUtransEditor_OnSelectionChanged();


            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void toolTip1_Popup(object sender, PopupEventArgs e)
        {

        }



        //this method is called if the user selects something in the cartocode combobox
        private void cboCartoCode_SelectedIndexChanged(object sender, EventArgs e)
        {
            //make label bold if the selected index is different from intial index (from on-selection-changed)
            if (intUtransInitialCartoCodeIndex != cboCartoCode.SelectedIndex)
            {
                groupBox5.Font = fontLabelHasEdits;
                cboCartoCode.Font = fontLabelRegular; // for some reason you have to set it to regular each time or it's bold - maybe b/c it's a child of groupbox
            }
            else
            {
                groupBox5.Font = fontLabelRegular;
                cboCartoCode.Font = fontLabelRegular; // for some reason you have to set it to regular each time or it's bold - maybe b/c it's a child of groupbox
            }
            
            //fontLabelHasEdits.Dispose();
            //fontLabelRegular.Dispose();
            
        }



        // this method is called when the update oid button is clicked
        private void btnUpdateDfcObjectID_Click(object sender, EventArgs e)
        {
            try
            {
                string strDfcResultSelectedFeatureOID = "";
                string strDfcResultSelectedFeatureExistingBaseFID = "";
                string strUtransSelectedFeatureOID = "";



                // make sure one dfc_result layer is selected
                IFeatureSelection arcFeatureSelectionDFC = clsGlobals.arcGeoFLayerDfcResult as IFeatureSelection;
                ISelectionSet arcSelSetDFC = arcFeatureSelectionDFC.SelectionSet;

                //check if one record is selected in the dfc
                if (arcSelSetDFC.Count == 1)
                {
                    //get a cursor of the selected features
                    ICursor arcCursor;
                    arcSelSetDFC.Search(null, false, out arcCursor);

                    //get the first row (there should only be one)
                    IRow arcRow = arcCursor.NextRow();

                    //get the objectid from dfc layer
                    strDfcResultSelectedFeatureOID = arcRow.get_Value(arcRow.Fields.FindField("OBJECTID")).ToString();
                    strDfcResultSelectedFeatureExistingBaseFID = arcRow.get_Value(arcRow.Fields.FindField("BASE_FID")).ToString();

                    //null out variables
                    arcCursor = null;
                    arcRow = null;
                }
                else
                {
                    MessageBox.Show("Please select only ONE feature from the DFC_RESULT layer.  Note that the feature must overlap the selected Utrans segment.");
                    return;
                }
                

                // make sure one utrans segment is selected
                IFeatureSelection arcFeatureSelectionUtrans = clsGlobals.arcGeoFLayerUtransStreets as IFeatureSelection;
                ISelectionSet arcSelSetUtrans = arcFeatureSelectionUtrans.SelectionSet;

                //check if one record is selected in utrans
                if (arcSelSetUtrans.Count == 1)
                {
                    //get a cursor of the selected features
                    ICursor arcCursor;
                    arcSelSetUtrans.Search(null, false, out arcCursor);

                    //get the first row (there should only be one)
                    IRow arcRow = arcCursor.NextRow();

                    //get the objectid from dfc layer
                    strUtransSelectedFeatureOID = arcRow.get_Value(arcRow.Fields.FindField("OBJECTID")).ToString();

                    //null out variables
                    arcCursor = null;
                    arcRow = null;

                }
                else
                {
                    MessageBox.Show("Please select only ONE feature from the UTRANS.TRANSADMIN.Roads_Edit layer.  Note that the feature must overlap the selected DFC_RESULT segment.");
                    return;
                }


                // update the dfc_result oid with the new oid after the split (populate the previous field with the intial utrans oid)
                IQueryFilter arcQueryFilter_DFC_updateSplitOID = new QueryFilter();
                arcQueryFilter_DFC_updateSplitOID.WhereClause = "OBJECTID = " + strDfcResultSelectedFeatureOID;


                IFeatureCursor arcFCur_DFC = clsGlobals.arcGeoFLayerDfcResult.Search(arcQueryFilter_DFC_updateSplitOID, false);
                IFeature arcFeat_DFC = arcFCur_DFC.NextFeature();

                if (arcFeat_DFC == null)
                {
                    MessageBox.Show("Could not find a feature in the DFC_RESULT layer with OID: " + strDfcResultSelectedFeatureOID, "OID Not Found", MessageBoxButtons.OK);
                    return;
                }

                //create string for use of double quotes in expression
                string strCalcExprNewBaseFID = @"""" + strUtransSelectedFeatureOID + @"""";
                string strCalcExprPrevBaseFID = @"""" + strDfcResultSelectedFeatureExistingBaseFID + @"""";

                //proceed with calculating values in the dfc table - 
                //ICalculator arcCalculator = new Calculator();
                //ICursor arcCur_dfcLayer = clsGlobals.arcGeoFLayerDfcResult.FeatureClass.Update(arcQueryFilter_DFC_updateSplitOID, true) as ICursor;

                //update the BASE_FID field
                ////arcCalculator.Cursor = arcCur_dfcLayer;
                ////arcCalculator.Expression = strCalcExprNewBaseFID;
                ////arcCalculator.Field = "BASE_FID";
                ////arcCalculator.Calculate();
                ////arcCalculator.ShowErrorPrompt = true;

                clsGlobals.arcEditor.StartOperation();
                arcFeat_DFC.set_Value(arcFeat_DFC.Fields.FindField("BASE_FID"), strUtransSelectedFeatureOID);
                arcFeat_DFC.set_Value(arcFeat_DFC.Fields.FindField("PREV__NOTES"), strDfcResultSelectedFeatureExistingBaseFID);
                arcFeat_DFC.Store();
                clsGlobals.arcEditor.StopOperation("DFC OID Update");

                //update the PREV__NOTES field
                //////proceed with calculating values in the dfc table - 
                ////arcCalculator = new Calculator();
                ////arcCur_dfcLayer = clsGlobals.arcGeoFLayerDfcResult.FeatureClass.Update(arcQueryFilter_DFC_updateSplitOID, true) as ICursor;

                ////arcCalculator.Cursor = arcCur_dfcLayer;
                ////arcCalculator.Expression = strCalcExprPrevBaseFID;
                ////arcCalculator.Field = "PREV__NOTES";
                ////arcCalculator.Calculate();
                ////arcCalculator.ShowErrorPrompt = true;

                //show messagebox of what was updated on dfc_result layer
                //MessageBox.Show("The following feature in the DFC_RESULT layer was updated: The record with OBJECTID : " + strDfcResultSelectedFeatureOID + " now contains the value " + strCalcExprNewBaseFID + " for the field BASE_FID.  It replaced the previous value of " + strDfcResultSelectedFeatureExistingBaseFID + ".");

                //null out variables...
                arcFeatureSelectionDFC = null;
                arcSelSetDFC = null;
                arcFeatureSelectionUtrans = null;
                arcSelSetUtrans = null;
                System.Runtime.InteropServices.Marshal.ReleaseComObject(arcFCur_DFC);
                arcFCur_DFC = null;
                arcQueryFilter_DFC_updateSplitOID = null;

                // refresh the map
                arcActiveView.Refresh();
                arcActiveView.Refresh();

            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }



        //show the vertices if the user has the checkbox checked
        public void displayVerticesOnNew() 
        {
            try
            {

                //get the map's graphics layer
                pComGraphicsLayer = arcMapp.BasicGraphicsLayer as ICompositeGraphicsLayer2;
                pCompositeLayer = pComGraphicsLayer as ICompositeLayer;

                //loop through all graphic layers in the map and check for the 'UtransVertices' layer, if found, delete it, in order to start fresh
                for (int i = 0; i < pCompositeLayer.Count; i++)
                {
                    pLayer = pCompositeLayer.get_Layer(i);
                    if (pLayer.Name == "UtransVertices")
                    {
                        pComGraphicsLayer.DeleteLayer("UtransVertices");
                        break;
                    }
                }

                //add a graphics layer to the map, so we can add the symbols to it
                IGraphicsLayer pGraphicsLayer = pComGraphicsLayer.AddLayer("UtransVertices", null);
                arcMapp.ActiveGraphicsLayer = (ILayer)pGraphicsLayer;
                IGraphicsContainer pGraphicsContainer = pComGraphicsLayer.FindLayer("UtransVertices") as IGraphicsContainer;


                //setup marker symbol
                ISimpleMarkerSymbol pSimpleMarker = new SimpleMarkerSymbol();
                ISymbol pSymbolMarker = (ISymbol)pSimpleMarker;
                IRgbColor pRgbColor = new ESRI.ArcGIS.Display.RgbColorClass();
                pRgbColor.Red = 223;
                pRgbColor.Green = 155;
                pRgbColor.Blue = 255;
                pSimpleMarker.Color = pRgbColor;
                pSimpleMarker.Style = esriSimpleMarkerStyle.esriSMSDiamond;
                pSimpleMarker.Size = 8;

                //setup line symbol
                ISimpleLineSymbol pSimpleLineSymbol = new SimpleLineSymbol();
                ISymbol pSymbolLine = (ISymbol)pSimpleLineSymbol;
                pRgbColor = new ESRI.ArcGIS.Display.RgbColor();
                pRgbColor.Red = 0;
                pRgbColor.Green = 255;
                pRgbColor.Blue = 0;
                pSimpleLineSymbol.Color = pRgbColor;
                pSimpleLineSymbol.Style = esriSimpleLineStyle.esriSLSSolid;
                pSimpleLineSymbol.Width = 1;

                //setup simplefill symbol
                ISimpleFillSymbol pSimpleFillSymbol = new SimpleFillSymbol();
                ISymbol pSymbolPolygon = (ISymbol)pSimpleFillSymbol;
                pRgbColor = new ESRI.ArcGIS.Display.RgbColor();
                pRgbColor.Red = 0;
                pRgbColor.Green = 0;
                pRgbColor.Blue = 255;
                pSimpleFillSymbol.Color = pRgbColor;
                pSimpleFillSymbol.Style = esriSimpleFillStyle.esriSFSSolid;

                //get all the street segments in the current map extent in a cursor
                IEnvelope pMapExtent = arcActiveView.Extent;
                ISpatialFilter pQFilter = new SpatialFilter();
                pQFilter.GeometryField = "SHAPE";
                pQFilter.Geometry = pMapExtent;
                pQFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                IFeatureCursor pFCursor = clsGlobals.arcGeoFLayerUtransStreets.Search(pQFilter, true);

                //draw each street segment and then each segments's point collection
                IFeature pFeature = pFCursor.NextFeature();
                IGeometry pGeometry;

                while (pFeature != null)
                {
                    pGeometry = pFeature.Shape;
                    //draw the segment
                    //draw each vertex on the segment
                    IPointCollection pPointCollection = pGeometry as IPointCollection;
                    for (int i = 0; i < pPointCollection.PointCount; i++)
                    {
                        IGeometry pPtGeom = pPointCollection.get_Point(i);
                        IElement pElement = new MarkerElement();
                        pElement.Geometry = pPtGeom;
                        IMarkerElement pMarkerElement = pElement as IMarkerElement;
                        pMarkerElement.Symbol = pSimpleMarker;
                        pGraphicsContainer.AddElement(pElement, 0);
                    }
                    pFeature = pFCursor.NextFeature();
                }

                boolVerticesOn = true;
                btnClearVertices.Visible = true;

                // null out variables
                pLayer = null;
                pComGraphicsLayer = null;
                pCompositeLayer = null;

            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }



        //this button, when clicked clears the map's vertices in the UtransVertices graphic layer, if any
        private void btnClearVertices_Click(object sender, EventArgs e)
        {
            try
            {
                //get the map's graphics layer
                pComGraphicsLayer = arcMapp.BasicGraphicsLayer as ICompositeGraphicsLayer2;
                pCompositeLayer = pComGraphicsLayer as ICompositeLayer;

                //loop through all graphic layers in the map and check for the 'UtransVertices' layer, if found, delete it, in order to start fresh
                for (int i = 0; i < pCompositeLayer.Count; i++)
                {
                    pLayer = pCompositeLayer.get_Layer(i);
                    if (pLayer.Name == "UtransVertices")
                    {
                        pComGraphicsLayer.DeleteLayer("UtransVertices");
                        break;
                    }
                }

                // null out variables
                pLayer = null;
                pComGraphicsLayer = null;
                pCompositeLayer = null;


                boolVerticesOn = false;
                btnClearVertices.Visible = false;

                // refresh the map
                arcActiveView.Refresh();
                arcActiveView.Refresh();
            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void linkLabelDefQuery_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                //open google doc attr doc showing attribute details
                //System.Diagnostics.Process.Start(e.Link.LinkData as string);
                System.Diagnostics.Process.Start("https://docs.google.com/document/d/1h7FTFUEXWobA8fvctgxKLaxr6LslnwVPnGVgPlrHnz0/edit");
            }
            catch (Exception ex)
            {
                //clsGlobals.logger.Error(Environment.NewLine + "Error Message: " + ex.Message + Environment.NewLine + "Error Source: " + ex.Source + Environment.NewLine + "Error Location:" + ex.StackTrace + Environment.NewLine + "Target Site: " + ex.TargetSite);

                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }


        // do a spatial intersect to get the city for the utrans segment (get city from sgid municipality layer)
        private string getCityFromSpatialIntersect(IFeature arcFeature_CountySegment)
        {
            try
            {
                string strReturnCity = "";

                //get the midpoint of the line segment for doing spatial queries (intersects)
                IGeometry arcGeometry = arcFeature_CountySegment.ShapeCopy;
                IPolyline arcPolyline = arcGeometry as IPolyline;
                IPoint arcMidPoint = new ESRI.ArcGIS.Geometry.Point();

                //get the midpoint of the line, pass it into a point
                arcPolyline.QueryPoint(esriSegmentExtension.esriNoExtension, 0.5, true, arcMidPoint);
                //MessageBox.Show("The midpoint of the selected line segment is: " + arcUtransEdits_midPoint.X.ToString() + ", " + arcUtransEdits_midPoint.Y.ToString());

                // spatial intersect for the following fields: ADDRSYS_L, QUADRANT_L, ZIPCODE_L, ZIPCODE_R, COUNTY_L (Maybe POSTCOMM_L)
                // ADDRSYS_L and QUADRANT_L
                ISpatialFilter arcSpatialFilterCity = new SpatialFilter();
                arcSpatialFilterCity.Geometry = arcMidPoint;
                arcSpatialFilterCity.GeometryField = "SHAPE";
                arcSpatialFilterCity.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                arcSpatialFilterCity.SubFields = "*";

                IFeatureCursor arcFC_City = clsGlobals.arcFLayerMunicipalities.Search(arcSpatialFilterCity, false);
                IFeature arcFeature_City = arcFC_City.NextFeature();
                if (arcFeature_City != null)
                {
                    strReturnCity = arcFeature_City.get_Value(arcFeature_City.Fields.FindField("NAME")).ToString().Trim();
                }
                else
                {
                    strReturnCity = "unincorporated";
                }

                // release memeory and variables
                System.Runtime.InteropServices.Marshal.ReleaseComObject(arcFC_City);
                arcFC_City = null;
                arcFeature_City = null;
                arcGeometry = null;
                arcPolyline = null;
                arcMidPoint = null;
                arcSpatialFilterCity = null;
                

                return strReturnCity;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                return "Error Getting City";
            }
        
        }


        private void frmUtransEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                e.Cancel = false;
                this.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

        }

        private void groupBoxCountySeg_Enter(object sender, EventArgs e)
        {

        }
    }
}
