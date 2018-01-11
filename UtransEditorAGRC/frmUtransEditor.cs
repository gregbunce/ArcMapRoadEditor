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

        string txtUtransInitialL_F_Add;
        string txtUtransInitialL_TAdd;
        string txtUtransInitialR_F_Add;
        string txtUtransInitialR_T_Add;
        string txtUtransInitialPreDir;
        string txtUtransInitialStName;
        string txtUtransInitialStType;
        string txtUtransInitialSufDir;
        string txtUtransInitialAlias1;
        string txtUtransInitialAlias1Type;
        string txtUtransInitialAlias2;
        string txtUtransInitialAlias2Type;
        string txtUtransInitialAcsAlias;
        string txtUtransInitialAscSuf;
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
  
                
                //clear the selection in the map, so we can start fresh with the tool and user's inputs
                arcMapp.ClearSelection();
                
                //refresh the map on the selected features
                //arcActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);
                arcActiveView.Refresh();


                //add textboxes to the control list
                ctrlList.Add(this.txtCountyAcsName);
                ctrlList.Add(this.txtCountyAcsSuf);
                ctrlList.Add(this.txtCountyAlias1);
                ctrlList.Add(this.txtCountyAlias1Type);
                ctrlList.Add(this.txtCountyAlias2);
                ctrlList.Add(this.txtCountyAlias2Type);
                ctrlList.Add(this.txtCountyL_F_Add);
                ctrlList.Add(this.txtCountyL_T_Add);
                ctrlList.Add(this.txtCountyPreDir);
                ctrlList.Add(this.txtCountyR_F_Add);
                ctrlList.Add(this.txtCountyR_T_Add);
                ctrlList.Add(this.txtCountyStName);
                ctrlList.Add(this.txtCountyStType);
                ctrlList.Add(this.txtCountySufDir);
                ctrlList.Add(this.txtUtranL_F_Add);
                ctrlList.Add(this.txtUtranL_T_Add);
                ctrlList.Add(this.txtUtranPreDir);
                ctrlList.Add(this.txtUtranR_F_Add);
                ctrlList.Add(this.txtUtranR_T_Add);
                ctrlList.Add(this.txtUtransAcsName);
                ctrlList.Add(this.txtUtransAcsSuf);
                ctrlList.Add(this.txtUtransAlias1);
                ctrlList.Add(this.txtUtransAlias1Type);
                ctrlList.Add(this.txtUtransAlias2);
                ctrlList.Add(this.txtUtransAlias2Type);
                ctrlList.Add(this.txtUtranStName);
                ctrlList.Add(this.txtUtranStType);
                ctrlList.Add(this.txtUtranSufDir);


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
                ////////txtUtranL_F_Add.ReadOnly = false;
                ////////txtUtranL_T_Add.ReadOnly = false;
                ////////txtUtranPreDir.ReadOnly = false;
                ////////txtUtranR_F_Add.ReadOnly = false;
                ////////txtUtranR_T_Add.ReadOnly = false;
                ////////txtUtransAcsName.ReadOnly = false;
                ////////txtUtransAcsSuf.ReadOnly = false;
                ////////txtUtransAlias1.ReadOnly = false;
                ////////txtUtransAlias1Type.ReadOnly = false;
                ////////txtUtransAlias2.ReadOnly = false;
                ////////txtUtransAlias2Type.ReadOnly = false;
                ////////txtUtranStName.ReadOnly = false;
                ////////txtUtranStType.ReadOnly = false;
                ////////txtUtranSufDir.ReadOnly = false;

                lblLeftFrom.Enabled = true;
                lblRightFrom.Enabled = true;
                lblLeftTo.Enabled = true;
                lblRightTo.Enabled = true;
                lblPreDir.Enabled = true;
                lblStName.Enabled = true;
                lblStType.Enabled = true;
                lblSufDir.Enabled = true;
                lblAcsName.Enabled = true;
                lblAcsSuf.Enabled = true;
                lblAlias.Enabled = true;
                lblAlias1Type.Enabled = true;
                lblAlias2.Enabled = true;
                lblAlias2Type.Enabled = true;

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
                txtUtransInitialL_F_Add = null;
                txtUtransInitialL_TAdd = null;;
                txtUtransInitialR_F_Add = null;
                txtUtransInitialR_T_Add = null;
                txtUtransInitialPreDir = null;
                txtUtransInitialStName = null;
                txtUtransInitialStType = null;
                txtUtransInitialSufDir = null;;
                txtUtransInitialAlias1 = null;
                txtUtransInitialAlias1Type = null;
                txtUtransInitialAlias2 = null;
                txtUtransInitialAlias2Type = null;
                txtUtransInitialAcsAlias = null;
                txtUtransInitialAscSuf = null;

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
                        clsGlobals.strCountyID = arcCountyFeature.get_Value(arcCountyFeature.Fields.FindFieldByAliasName("COFIPS")).ToString().Trim();
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
                txtUtransInitialL_F_Add = txtUtranL_F_Add.Text;
                txtUtransInitialL_TAdd = txtUtranL_T_Add.Text;
                txtUtransInitialR_F_Add = txtUtranR_F_Add.Text;
                txtUtransInitialR_T_Add = txtUtranR_T_Add.Text;
                txtUtransInitialPreDir = txtUtranPreDir.Text;
                txtUtransInitialStName = txtUtranStName.Text;
                txtUtransInitialStType = txtUtranStType.Text;
                txtUtransInitialSufDir = txtUtranSufDir.Text;
                txtUtransInitialAlias1 = txtUtransAlias1.Text;
                txtUtransInitialAlias1Type = txtUtransAlias1Type.Text;
                txtUtransInitialAlias2 = txtUtransAlias2.Text;
                txtUtransInitialAlias2Type = txtUtransAlias2Type.Text;
                txtUtransInitialAcsAlias = txtUtransAcsName.Text;
                txtUtransInitialAscSuf = txtUtransAcsSuf.Text;

                //revert labels back to regular (non-italic)
                lblAcsName.Font = fontLabelRegular;
                lblAcsSuf.Font = fontLabelRegular;
                lblAlias.Font = fontLabelRegular;
                lblAlias1Type.Font = fontLabelRegular;
                lblAlias2.Font = fontLabelRegular;
                lblAlias2Type.Font = fontLabelRegular;
                lblLeftFrom.Font = fontLabelRegular;
                lblLeftTo.Font = fontLabelRegular;
                lblPreDir.Font = fontLabelRegular;
                lblRightFrom.Font = fontLabelRegular;
                lblRightTo.Font = fontLabelRegular;
                lblStName.Font = fontLabelRegular;
                lblStType.Font = fontLabelRegular;
                lblSufDir.Font = fontLabelRegular;

                //if it's a new record
                if (strChangeType == "N" & strUtransOID == "-1")
                {
                    //make the textboxes a light red color, indicating there's no attributes for this feature
                    txtUtranL_F_Add.BackColor = Color.LightGray;
                    txtUtranL_T_Add.BackColor = Color.LightGray;
                    txtUtranPreDir.BackColor = Color.LightGray;
                    txtUtranR_F_Add.BackColor = Color.LightGray;
                    txtUtranR_T_Add.BackColor = Color.LightGray;
                    txtUtransAcsName.BackColor = Color.LightGray;
                    txtUtransAcsSuf.BackColor = Color.LightGray;
                    txtUtransAlias1.BackColor = Color.LightGray;
                    txtUtransAlias1Type.BackColor = Color.LightGray;
                    txtUtransAlias2.BackColor = Color.LightGray;
                    txtUtransAlias2Type.BackColor = Color.LightGray;
                    txtUtranStName.BackColor = Color.LightGray;
                    txtUtranStType.BackColor = Color.LightGray;
                    txtUtranSufDir.BackColor = Color.LightGray;

                    //i could change this to loop the control list and update all the controls with a tag like utrans
                    ////////txtUtranL_F_Add.ReadOnly = true;
                    ////////txtUtranL_T_Add.ReadOnly = true;
                    ////////txtUtranPreDir.ReadOnly = true;
                    ////////txtUtranR_F_Add.ReadOnly = true;
                    ////////txtUtranR_T_Add.ReadOnly = true;
                    ////////txtUtransAcsName.ReadOnly = true;
                    ////////txtUtransAcsSuf.ReadOnly = true;
                    ////////txtUtransAlias1.ReadOnly = true;
                    ////////txtUtransAlias1Type.ReadOnly = true;
                    ////////txtUtransAlias2.ReadOnly = true;
                    ////////txtUtransAlias2Type.ReadOnly = true;
                    ////////txtUtranStName.ReadOnly = true;
                    ////////txtUtranStType.ReadOnly = true;
                    ////////txtUtranSufDir.ReadOnly = true;

                    lblLeftFrom.Enabled = false;
                    lblRightFrom.Enabled = false;
                    lblLeftTo.Enabled = false;
                    lblRightTo.Enabled = false;
                    lblPreDir.Enabled = false;
                    lblStName.Enabled = false;
                    lblStType.Enabled = false;
                    lblSufDir.Enabled = false;
                    lblAcsName.Enabled = false;
                    lblAcsSuf.Enabled = false;
                    lblAlias.Enabled = false;
                    lblAlias1Type.Enabled = false;
                    lblAlias2.Enabled = false;
                    lblAlias2Type.Enabled = false;
                    
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
                if (txtCountySufDir.Text.ToUpper().ToString() != txtUtranSufDir.Text.ToUpper().ToString())
                {
                    txtUtranSufDir.BackColor = Color.LightYellow;
                    txtCountySufDir.BackColor = Color.LightYellow;
                    //lblSufDir.Font = fontLabelDataMismatch;
                    //boolHadDifferenceSufDir = true;
                }
                if (txtCountyPreDir.Text.ToUpper().ToString() != txtUtranPreDir.Text.ToUpper().ToString())
                {
                    txtUtranPreDir.BackColor = Color.LightYellow;
                    txtCountyPreDir.BackColor = Color.LightYellow;
                    //lblPreDir.Font = fontLabelDataMismatch;
                    //boolHadDifferencePreDir = true;
                }
                if (txtCountyL_F_Add.Text.ToString() != txtUtranL_F_Add.Text.ToString())
                {
                    txtUtranL_F_Add.BackColor = Color.LightYellow;
                    txtCountyL_F_Add.BackColor = Color.LightYellow;
                    //lblLeftFrom.Font = fontLabelDataMismatch;
                    //capture the curent text - incase we want to revert to it
                    //txtUtransExistingL_F_Add = txtUtranL_F_Add.Text;
                    //boolHadDifferenceL_F_Add = true;
                }
                if (txtCountyL_T_Add.Text.ToString() != txtUtranL_T_Add.Text.ToString())
                {
                    txtUtranL_T_Add.BackColor = Color.LightYellow;
                    txtCountyL_T_Add.BackColor = Color.LightYellow;
                    //lblLeftTo.Font = fontLabelDataMismatch;
                    //boolHadDifferenceL_T_Add = true;
                }
                if (txtCountyR_F_Add.Text.ToString() != txtUtranR_F_Add.Text.ToString())
                {
                    txtUtranR_F_Add.BackColor = Color.LightYellow;
                    txtCountyR_F_Add.BackColor = Color.LightYellow;
                    //lblRightFrom.Font = fontLabelDataMismatch;
                    //boolHadDifferenceR_F_Add = true;
                }
                if (txtCountyR_T_Add.Text.ToString() != txtUtranR_T_Add.Text.ToString())
                {
                    txtUtranR_T_Add.BackColor = Color.LightYellow;
                    txtCountyR_T_Add.BackColor = Color.LightYellow;
                    //lblRightTo.Font = fontLabelDataMismatch;
                    //boolHadDifferenceR_T_Add = true;
                }
                if (txtCountyAcsName.Text.ToUpper().ToString() != txtUtransAcsName.Text.ToUpper().ToString())
                {
                    txtUtransAcsName.BackColor = Color.LightYellow;
                    txtCountyAcsName.BackColor = Color.LightYellow;
                    //lblAcsAlias.Font = fontLabelDataMismatch;
                    //boolHadDifferenceAcsAlias = true;
                }
                if (txtCountyAcsSuf.Text.ToUpper().ToString() != txtUtransAcsSuf.Text.ToUpper().ToString())
                {
                    txtUtransAcsSuf.BackColor = Color.LightYellow;
                    txtCountyAcsSuf.BackColor = Color.LightYellow;
                    //lblAcsSuf.Font = fontLabelDataMismatch;
                    //boolHadDifferenceAscSuf = true;
                }
                if (txtCountyAlias1.Text.ToUpper().ToString() != txtUtransAlias1.Text.ToUpper().ToString())
                {
                    txtUtransAlias1.BackColor = Color.LightYellow;
                    txtCountyAlias1.BackColor = Color.LightYellow;
                    //lblAlias.Font = fontLabelDataMismatch;
                    //boolHadDifferenceAlias1 = true;
                }
                if (txtCountyAlias1Type.Text.ToUpper().ToString() != txtUtransAlias1Type.Text.ToUpper().ToString())
                {
                    txtUtransAlias1Type.BackColor = Color.LightYellow;
                    txtCountyAlias1Type.BackColor = Color.LightYellow;
                    //lblAlias1Type.Font = fontLabelDataMismatch;
                    //boolHadDifferenceAlias1Type = true;
                }
                if (txtCountyAlias2.Text.ToUpper().ToString() != txtUtransAlias2.Text.ToUpper().ToString())
                {
                    txtUtransAlias2.BackColor = Color.LightYellow;
                    txtCountyAlias2.BackColor = Color.LightYellow;
                    //lblAlias2.Font = fontLabelDataMismatch;
                    //boolHadDifferenceAlias2 = true;
                }
                if (txtCountyAlias2Type.Text.ToUpper().ToString() != txtUtransAlias2.Text.ToUpper().ToString())
                {
                    txtUtransAlias2.BackColor = Color.LightYellow;
                    txtCountyAlias2.BackColor = Color.LightYellow;
                    //lblAlias2Type.Font = fontLabelDataMismatch;
                    //boolHadDifferenceAlias2Type = true;
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

                // L_F_ADD
                if (clickedLabel.Text == "L_F_ADD")
                {
                    if (txtUtranL_F_Add.Text != txtCountyL_F_Add.Text)
                    {
                        txtUtranL_F_Add.Text = txtCountyL_F_Add.Text;
                        return;
                    }
                    if (txtUtranL_F_Add.Text == txtCountyL_F_Add.Text)
                    {
                        txtUtranL_F_Add.Text = txtUtransInitialL_F_Add;
                        return;
                    }
                }

                // L_T_ADD
                if (clickedLabel.Text == "L_T_ADD")
                {
                    if (txtUtranL_T_Add.Text != txtCountyL_T_Add.Text)
                    {
                        txtUtranL_T_Add.Text = txtCountyL_T_Add.Text;
                        return;
                    }
                    if (txtUtranL_T_Add.Text == txtCountyL_T_Add.Text)
                    {
                        txtUtranL_T_Add.Text = txtUtransInitialL_TAdd;
                        return;
                    }
                }

                // R_F_ADD
                if (clickedLabel.Text == "R_F_ADD")
                {
                    if (txtUtranR_F_Add.Text != txtCountyR_F_Add.Text)
                    {
                        txtUtranR_F_Add.Text = txtCountyR_F_Add.Text;
                        return;
                    }
                    if (txtUtranR_F_Add.Text == txtCountyR_F_Add.Text)
                    {
                        txtUtranR_F_Add.Text = txtUtransInitialR_F_Add;
                        return;
                    }
                }

                // R_T_ADD
                if (clickedLabel.Text == "R_T_ADD")
                {
                    if (txtUtranR_T_Add.Text != txtCountyR_T_Add.Text)
                    {
                        txtUtranR_T_Add.Text = txtCountyR_T_Add.Text;
                        return;
                    }
                    if (txtUtranR_T_Add.Text == txtCountyR_T_Add.Text)
                    {
                        txtUtranR_T_Add.Text = txtUtransInitialR_T_Add;
                        return;
                    }
                }

                // STREETNAME
                if (clickedLabel.Text == "STREETNAME")
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

                // STREETTYPE
                if (clickedLabel.Text == "STREETTYPE")
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

                // SUFDIR
                if (clickedLabel.Text == "SUFDIR")
                {
                    if (txtUtranSufDir.Text != txtCountySufDir.Text)
                    {
                        txtUtranSufDir.Text = txtCountySufDir.Text;
                        return;
                    }
                    if (txtUtranSufDir.Text == txtCountySufDir.Text)
                    {
                        txtUtranSufDir.Text = txtUtransInitialSufDir;
                        return;
                    }
                }

                // ALIAS1
                if (clickedLabel.Text == "ALIAS1")
                {
                    if (txtUtransAlias1.Text != txtCountyAlias1.Text)
                    {
                        txtUtransAlias1.Text = txtCountyAlias1.Text;
                        return;
                    }
                    if (txtUtransAlias1.Text == txtCountyAlias1.Text)
                    {
                        txtUtransAlias1.Text = txtUtransInitialAlias1;
                        return;
                    }
                }

                // ALIAS1TYPE
                if (clickedLabel.Text == "ALIAS1TYPE")
                {
                    if (txtUtransAlias1Type.Text != txtCountyAlias1Type.Text)
                    {
                        txtUtransAlias1Type.Text = txtCountyAlias1Type.Text;
                        return;
                    }
                    if (txtUtransAlias1Type.Text == txtCountyAlias1Type.Text)
                    {
                        txtUtransAlias1Type.Text = txtUtransInitialAlias1Type;
                        return;
                    }
                }

                // ALIAS2
                if (clickedLabel.Text == "ALIAS2")
                {
                    if (txtUtransAlias2.Text != txtCountyAlias2.Text)
                    {
                        txtUtransAlias2.Text = txtCountyAlias2.Text;
                        return;
                    }
                    if (txtUtransAlias2.Text == txtCountyAlias2.Text)
                    {
                        txtUtransAlias2.Text = txtUtransInitialAlias2;
                        return;
                    }
                }

                // ALIAS2TYPE
                if (clickedLabel.Text == "ALIAS2TYPE")
                {
                    if (txtUtransAlias2Type.Text != txtCountyAlias2Type.Text)
                    {
                        txtUtransAlias2Type.Text = txtCountyAlias2Type.Text;
                        return;
                    }
                    if (txtUtransAlias2Type.Text == txtCountyAlias2Type.Text)
                    {
                        txtUtransAlias2Type.Text = txtUtransInitialAlias2Type;
                        return;
                    }
                }

                // ACSNAME
                if (clickedLabel.Text == "ACSNAME")
                {
                    if (txtUtransAcsName.Text != txtCountyAcsName.Text)
                    {
                        txtUtransAcsName.Text = txtCountyAcsName.Text;
                        return;
                    }
                    if (txtUtransAcsName.Text == txtCountyAcsName.Text)
                    {
                        txtUtransAcsName.Text = txtUtransInitialAcsAlias;
                        return;
                    }
                }

                // ACSSUF
                if (clickedLabel.Text == "ACSSUF")
                {
                    if (txtUtransAcsSuf.Text != txtCountyAcsSuf.Text)
                    {
                        txtUtransAcsSuf.Text = txtCountyAcsSuf.Text;
                        return;
                    }
                    if (txtUtransAcsSuf.Text == txtCountyAcsSuf.Text)
                    {
                        txtUtransAcsSuf.Text = txtUtransInitialAscSuf;
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

        // L_F_ADD
        private void txtUtranL_F_Add_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtranL_F_Add.Text.ToUpper().ToString() != txtCountyL_F_Add.Text.ToUpper().ToString())
                {
                    txtUtranL_F_Add.BackColor = Color.LightYellow;
                    txtCountyL_F_Add.BackColor = Color.LightYellow;
                }
                else if (txtUtranL_F_Add.Text.ToUpper().ToString() == txtCountyL_F_Add.Text.ToUpper().ToString())
                {
                    txtUtranL_F_Add.BackColor = Color.White;
                    txtCountyL_F_Add.BackColor = Color.White;
                }

                if (txtUtranL_F_Add.Text != txtUtransInitialL_F_Add)
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

        // L_T_ADD
        private void txtUtranL_T_Add_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtranL_T_Add.Text.ToUpper().ToString() != txtCountyL_T_Add.Text.ToUpper().ToString())
                {
                    txtUtranL_T_Add.BackColor = Color.LightYellow;
                    txtCountyL_T_Add.BackColor = Color.LightYellow;
                }
                else if (txtUtranL_T_Add.Text.ToUpper().ToString() == txtCountyL_T_Add.Text.ToUpper().ToString())
                {
                    txtUtranL_T_Add.BackColor = Color.White;
                    txtCountyL_T_Add.BackColor = Color.White;
                }

                if (txtUtranL_T_Add.Text != txtUtransInitialL_TAdd)
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

        // R_F_ADD
        private void txtUtranR_F_Add_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtranR_F_Add.Text.ToUpper().ToString() != txtCountyR_F_Add.Text.ToUpper().ToString())
                {
                    txtUtranR_F_Add.BackColor = Color.LightYellow;
                    txtCountyR_F_Add.BackColor = Color.LightYellow;
                }
                else if (txtUtranR_F_Add.Text.ToUpper().ToString() == txtCountyR_F_Add.Text.ToUpper().ToString())
                {
                    txtUtranR_F_Add.BackColor = Color.White;
                    txtCountyR_F_Add.BackColor = Color.White;
                }

                if (txtUtranR_F_Add.Text != txtUtransInitialR_F_Add)
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

        // R_T_ADD
        private void txtUtranR_T_Add_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtranR_T_Add.Text.ToUpper().ToString() != txtCountyR_T_Add.Text.ToUpper().ToString())
                {
                    txtUtranR_T_Add.BackColor = Color.LightYellow;
                    txtCountyR_T_Add.BackColor = Color.LightYellow;
                }
                else if (txtUtranR_T_Add.Text.ToUpper().ToString() == txtCountyR_T_Add.Text.ToUpper().ToString())
                {
                    txtUtranR_T_Add.BackColor = Color.White;
                    txtCountyR_T_Add.BackColor = Color.White;
                }

                if (txtUtranR_T_Add.Text != txtUtransInitialR_T_Add)
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

        // STREETNAME
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

        // STREETTYPE
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

        // SUFDIR
        private void txtUtranSufDir_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtranSufDir.Text.ToUpper().ToString() != txtCountySufDir.Text.ToUpper().ToString())
                {
                    txtUtranSufDir.BackColor = Color.LightYellow;
                    txtCountySufDir.BackColor = Color.LightYellow;
                }
                else if (txtUtranSufDir.Text.ToUpper().ToString() == txtCountySufDir.Text.ToUpper().ToString())
                {
                    txtUtranSufDir.BackColor = Color.White;
                    txtCountySufDir.BackColor = Color.White;
                }

                if (txtUtranSufDir.Text != txtUtransInitialSufDir)
                {
                    lblSufDir.Font = fontLabelHasEdits;
                    //lblSufDir.ForeColor = Color.LightSalmon;
                    btnSaveToUtrans.Enabled = true;
                }
                else
                {
                    lblSufDir.Font = fontLabelRegular;
                    //lblSufDir.ForeColor = Color.Black;
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

        // ALIAS1
        private void txtUtransAlias1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtransAlias1.Text.ToUpper().ToString() != txtCountyAlias1.Text.ToUpper().ToString())
                {
                    txtUtransAlias1.BackColor = Color.LightYellow;
                    txtCountyAlias1.BackColor = Color.LightYellow;
                }
                else if (txtUtransAlias1.Text.ToUpper().ToString() == txtCountyAlias1.Text.ToUpper().ToString())
                {
                    txtUtransAlias1.BackColor = Color.White;
                    txtCountyAlias1.BackColor = Color.White;
                }

                if (txtUtransAlias1.Text != txtUtransInitialAlias1)
                {
                    lblAlias.Font = fontLabelHasEdits;
                    //lblAlias.ForeColor = Color.LightSalmon;
                    btnSaveToUtrans.Enabled = true;
                }
                else
                {
                    lblAlias.Font = fontLabelRegular;
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

        // ALIAS1TYPE
        private void txtUtransAlias1Type_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtransAlias1Type.Text.ToUpper().ToString() != txtCountyAlias1Type.Text.ToUpper().ToString())
                {
                    txtUtransAlias1Type.BackColor = Color.LightYellow;
                    txtCountyAlias1Type.BackColor = Color.LightYellow;
                }
                else if (txtUtransAlias1Type.Text.ToUpper().ToString() == txtCountyAlias1Type.Text.ToUpper().ToString())
                {
                    txtUtransAlias1Type.BackColor = Color.White;
                    txtCountyAlias1Type.BackColor = Color.White;
                }

                if (txtUtransAlias1Type.Text != txtUtransInitialAlias1Type)
                {
                    lblAlias1Type.Font = fontLabelHasEdits;
                    //lblAlias1Type.ForeColor = Color.LightSalmon;
                    btnSaveToUtrans.Enabled = true;
                }
                else
                {
                    lblAlias1Type.Font = fontLabelRegular;
                    //lblAlias1Type.ForeColor = Color.Black;
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

        // ALIAS2
        private void txtUtransAlias2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtransAlias2.Text.ToUpper().ToString() != txtCountyAlias2.Text.ToUpper().ToString())
                {
                    txtUtransAlias2.BackColor = Color.LightYellow;
                    txtCountyAlias2.BackColor = Color.LightYellow;
                }
                else if (txtUtransAlias2.Text.ToUpper().ToString() == txtCountyAlias2.Text.ToUpper().ToString())
                {
                    txtUtransAlias2.BackColor = Color.White;
                    txtCountyAlias2.BackColor = Color.White;
                }

                if (txtUtransAlias2.Text != txtUtransInitialAlias2)
                {
                    lblAlias2.Font = fontLabelHasEdits;
                    //lblAlias2.ForeColor = Color.LightSalmon;
                    btnSaveToUtrans.Enabled = true;
                }
                else
                {
                    lblAlias2.Font = fontLabelRegular;
                    //lblAlias2.ForeColor = Color.Black;
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

        // ALIAS2TYPE
        private void txtUtransAlias2Type_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtransAlias2Type.Text.ToUpper().ToString() != txtCountyAlias2Type.Text.ToUpper().ToString())
                {
                    txtUtransAlias2Type.BackColor = Color.LightYellow;
                    txtCountyAlias2Type.BackColor = Color.LightYellow;
                }
                else if (txtUtransAlias2Type.Text.ToUpper().ToString() == txtCountyAlias2Type.Text.ToUpper().ToString())
                {
                    txtUtransAlias2Type.BackColor = Color.White;
                    txtCountyAlias2Type.BackColor = Color.White;
                }

                if (txtUtransAlias2Type.Text != txtUtransInitialAlias2Type)
                {
                    lblAlias2Type.Font = fontLabelHasEdits;
                    //lblAlias2Type.ForeColor = Color.LightSalmon;
                    btnSaveToUtrans.Enabled = true;
                }
                else
                {
                    lblAlias2Type.Font = fontLabelRegular;
                    //lblAlias2Type.ForeColor = Color.Black;
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

        //ACSNAME
        private void txtUtransAcsAllias_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtransAcsName.Text.ToUpper().ToString() != txtCountyAcsName.Text.ToUpper().ToString())
                {
                    txtUtransAcsName.BackColor = Color.LightYellow;
                    txtCountyAcsName.BackColor = Color.LightYellow;
                }
                else if (txtUtransAcsName.Text.ToUpper().ToString() == txtCountyAcsName.Text.ToUpper().ToString())
                {
                    txtUtransAcsName.BackColor = Color.White;
                    txtCountyAcsName.BackColor = Color.White;
                }

                if (txtUtransAcsName.Text != txtUtransInitialAcsAlias)
                {
                    lblAcsName.Font = fontLabelHasEdits;
                    //lblAcsAlias.ForeColor = Color.LightSalmon;
                    btnSaveToUtrans.Enabled = true;
                }
                else
                {
                    lblAcsName.Font = fontLabelRegular;
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

        // ACSSUF
        private void txtUtransAcsSuf_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtUtransAcsSuf.Text.ToUpper().ToString() != txtCountyAcsSuf.Text.ToUpper().ToString())
                {
                    txtUtransAcsSuf.BackColor = Color.LightYellow;
                    txtCountyAcsSuf.BackColor = Color.LightYellow;
                }
                else if (txtUtransAcsSuf.Text.ToUpper().ToString() == txtCountyAcsSuf.Text.ToUpper().ToString())
                {
                    txtUtransAcsSuf.BackColor = Color.White;
                    txtCountyAcsSuf.BackColor = Color.White;
                }

                if (txtUtransAcsSuf.Text != txtUtransInitialAscSuf)
                {
                    lblAcsSuf.Font = fontLabelHasEdits;
                    //lblAcsSuf.ForeColor = Color.LightSalmon;
                    btnSaveToUtrans.Enabled = true;
                }
                else
                {
                    lblAcsSuf.Font = fontLabelRegular;
                    //lblAcsSuf.ForeColor = Color.Black;
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
                        clsGlobals.strCountySegment = txtCountyPreDir.Text.Trim() + " " + txtCountyStName.Text.Trim() + " " + txtCountyStType.Text.Trim() + " " + txtCountySufDir.Text.Trim();
                        clsGlobals.strCountySegmentTrimed = clsGlobals.strCountySegment.Trim();
                        if (txtCountyL_F_Add.Text != "")
                        {
                            clsGlobals.strCountyL_F_Add = txtCountyL_F_Add.Text.ToString().Trim();
                        }
                        else
                        {
                            clsGlobals.strCountyL_F_Add = "0";
                        }
                        if (txtCountyL_T_Add.Text != "")
                        {
                            clsGlobals.strCountyL_T_Add = txtCountyL_T_Add.Text.ToString().Trim();
                        }
                        else
                        {
                            clsGlobals.strCountyL_T_Add = "0";
                        }
                        if (txtCountyR_F_Add.Text != "")
                        {
                            clsGlobals.strCountyR_F_Add = txtCountyR_F_Add.Text.ToString().Trim();
                        }
                        else
                        {
                            clsGlobals.strCountyR_F_Add = "0";
                        }
                        if (txtCountyR_T_Add.Text != "")
                        {
                            clsGlobals.strCountyR_T_Add = txtCountyR_T_Add.Text.ToString().Trim();
                        }
                        else
                        {
                            clsGlobals.strCountyR_T_Add = "0";
                        }

                        //check if null values in utrans streets, if so assign zero
                        strGoogleLogLeftTo = "";
                        strGoogleLogLeftFrom = "";
                        strGoogleLogRightTo = "";
                        strGoogleLogRightFrom = "";
                        
                        if (txtUtranL_T_Add.Text == "")
	                    {
		                    strGoogleLogLeftTo = "0";
	                    }
                        else
	                    {
                            strGoogleLogLeftTo = txtUtranL_T_Add.Text;
	                    }
                        if (txtUtranL_F_Add.Text == "")
	                    {
		                     strGoogleLogLeftFrom = "0";
	                    }
                        else
	                    {
                            strGoogleLogLeftFrom = txtUtranL_F_Add.Text;
	                    }
                        if (txtUtranR_F_Add.Text == "")
	                    {
		                    strGoogleLogRightFrom = "0";
	                    }
                        else
	                    {
                            strGoogleLogRightFrom = txtUtranR_F_Add.Text;
	                    }
                        if (txtUtranR_T_Add.Text == "")
	                    {
		                    strGoogleLogRightTo = "0";
	                    }
                        else
	                    {
                            strGoogleLogRightTo = txtUtranR_T_Add.Text;
	                    }

                        // get city from muni layer for google doc city field
                        clsGlobals.strGoogleSpreadsheetCityField = getCityFromSpatialIntersect(arcCountyFeature);

                        //string together the agrc street segment
                        clsGlobals.strAgrcSegment = strGoogleLogLeftFrom + "-" + strGoogleLogLeftTo + " " + strGoogleLogRightFrom + "-" + strGoogleLogRightTo + " " + txtUtranPreDir.Text.Trim() + " " + txtUtranStName.Text.Trim() + " " + txtUtranStType.Text.Trim() + " " + txtUtranSufDir.Text.Trim();

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
                        clsGlobals.strCountySegment = txtCountyPreDir.Text.Trim() + " " + txtCountyStName.Text.Trim() + " " + txtCountyStType.Text.Trim() + " " + txtCountySufDir.Text.Trim();
                        clsGlobals.strCountySegmentTrimed = clsGlobals.strCountySegment.Trim();
                        if (txtCountyL_F_Add.Text != "")
                        {
                            clsGlobals.strCountyL_F_Add = txtCountyL_F_Add.Text.ToString().Trim();
                        }
                        else
                        {
                            clsGlobals.strCountyL_F_Add = "0";
                        }
                        if (txtCountyL_T_Add.Text != "")
                        {
                            clsGlobals.strCountyL_T_Add = txtCountyL_T_Add.Text.ToString().Trim();
                        }
                        else
                        {
                            clsGlobals.strCountyL_T_Add = "0";
                        }
                        if (txtCountyR_F_Add.Text != "")
                        {
                            clsGlobals.strCountyR_F_Add = txtCountyR_F_Add.Text.ToString().Trim();
                        }
                        else
                        {
                            clsGlobals.strCountyR_F_Add = "0";
                        }
                        if (txtCountyR_T_Add.Text != "")
                        {
                            clsGlobals.strCountyR_T_Add = txtCountyR_T_Add.Text.ToString().Trim();
                        }
                        else
                        {
                            clsGlobals.strCountyR_T_Add = "0";
                        }

                        //check if null values in utrans streets, if so assign zero
                        strGoogleLogLeftTo = "";
                        strGoogleLogLeftFrom = "";
                        strGoogleLogRightTo = "";
                        strGoogleLogRightFrom = "";
                        
                        if (txtUtranL_T_Add.Text == "")
	                    {
		                    strGoogleLogLeftTo = "0";
	                    }
                        else
	                    {
                            strGoogleLogLeftTo = txtUtranL_T_Add.Text;
	                    }
                        if (txtUtranL_F_Add.Text == "")
	                    {
		                     strGoogleLogLeftFrom = "0";
	                    }
                        else
	                    {
                            strGoogleLogLeftFrom = txtUtranL_F_Add.Text;
	                    }
                        if (txtUtranR_F_Add.Text == "")
	                    {
		                    strGoogleLogRightFrom = "0";
	                    }
                        else
	                    {
                            strGoogleLogRightFrom = txtUtranR_F_Add.Text;
	                    }
                        if (txtUtranR_T_Add.Text == "")
	                    {
		                    strGoogleLogRightTo = "0";
	                    }
                        else
	                    {
                            strGoogleLogRightTo = txtUtranR_T_Add.Text;
	                    }

                        // get city from muni layer for google doc city field
                        clsGlobals.strGoogleSpreadsheetCityField = getCityFromSpatialIntersect(arcCountyFeature);

                        //string together the agrc street segment
                        clsGlobals.strAgrcSegment = strGoogleLogLeftFrom + "-" + strGoogleLogLeftTo + " " + strGoogleLogRightFrom + "-" + strGoogleLogRightTo + " " + txtUtranPreDir.Text.Trim() + " " + txtUtranStName.Text.Trim() + " " + txtUtranStType.Text.Trim() + " " + txtUtranSufDir.Text.Trim();

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
                            if (ctrlCurrent.Tag.ToString() == "L_F_ADD" & ctrlCurrent.Text.ToString() == "")
                            {
                                arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField(ctrlCurrent.Tag.ToString()), 0);
                                //break;
                            }
                            else if (ctrlCurrent.Tag.ToString() == "L_T_ADD" & ctrlCurrent.Text.ToString() == "")
                            {
                                arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField(ctrlCurrent.Tag.ToString()), 0);
                                //break;
                            }
                            else if (ctrlCurrent.Tag.ToString() == "R_F_ADD" & ctrlCurrent.Text.ToString() == "")
                            {
                                arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField(ctrlCurrent.Tag.ToString()), 0);
                                //break;
                            }
                            else if (ctrlCurrent.Tag.ToString() == "R_T_ADD" & ctrlCurrent.Text.ToString() == "")
                            {
                                arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField(ctrlCurrent.Tag.ToString()), 0);
                                //break;
                            }
                            else
                            {
                                //populate the field with the value in the corresponding textbox
                                arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField(ctrlCurrent.Tag.ToString()), ctrlCurrent.Text.Trim());
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

                    // spatial intersect for the following fields: ADDR_SYS, ADDR_QUAD, ZIPLEFT, ZIPRIGHT, COFIPS (Maybe USPS_PLACE)
                    // ADDR_SYS and ADDR_QUAD
                    ISpatialFilter arcSpatialFilter = new SpatialFilter();
                    arcSpatialFilter.Geometry = arcUtransEdits_midPoint;
                    arcSpatialFilter.GeometryField = "SHAPE";
                    arcSpatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                    arcSpatialFilter.SubFields = "*";

                    IFeatureCursor arcAddrSysCursor = clsGlobals.arcFLayerAddrSysQuads.Search(arcSpatialFilter, false);
                    IFeature arcFeatureAddrSys = arcAddrSysCursor.NextFeature();
                    if (arcFeatureAddrSys != null)
                    {
                        //update the value in the utrans based on the intersect
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("ADDR_SYS"), arcFeatureAddrSys.get_Value(arcFeatureAddrSys.Fields.FindField("GRID_NAME")).ToString().Trim());
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("ADDR_QUAD"), arcFeatureAddrSys.get_Value(arcFeatureAddrSys.Fields.FindField("QUADRANT")).ToString().Trim());
                    }
                    else
                    {
                        MessageBox.Show("The midpoint of the street segment you are trying to update is not within an AddressSystemQuadrants.", "Whoa there Cowboy!");
                        //give option to leave blank or abort edit operation and return
                        //return;
                    }
                    //clear out variables
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(arcAddrSysCursor);
                    arcAddrSysCursor = null;
                    arcFeatureAddrSys = null;

                    // ZIPLEFT and ZIPRIGHT (use iconstructpoint.constructoffset method to offset the midpoint of the line)
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


                    // LEFT - ZIP & MUNICIPALITY(SDE) //
                    // query zipcode layer for a zip on left side of segment
                    ISpatialFilter arcSpatialFilter_leftZip = new SpatialFilter();
                    arcSpatialFilter_leftZip.Geometry = outPoint_negLeft;
                    arcSpatialFilter_leftZip.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

                    IFeatureCursor arcZipCursor_left = clsGlobals.arcFLayerZipCodes.Search(arcSpatialFilter_leftZip, false);
                    IFeature arcFeatureZip_left = arcZipCursor_left.NextFeature();
                    if (arcFeatureZip_left != null)
                    {
                        //update the value in the utrans based on the intersect
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("ZIPLEFT"), arcFeatureZip_left.get_Value(arcFeatureZip_left.Fields.FindField("ZIP5")));
                        //arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("ZIPRIGHT"), arcFeatureZip_left.get_Value(arcFeatureZip_left.Fields.FindField("ZIP5")));
                        //maybe update the usps_place field as well with the "name" field from the zipcodes layer
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("USPS_PLACE"), arcFeatureZip_left.get_Value(arcFeatureZip_left.Fields.FindField("NAME")).ToString().Trim());
                    }
                    else
                    {
                        MessageBox.Show("A zipcode could not be found on the left side of the segment - based on the segment's midpoint with a 15 meter offset.", "Whoa there Cowboy!");
                        //give option to leave blank or abort edit operation and return
                        //return;
                    }
                    //clear out variables
                    // release the cursor
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(arcZipCursor_left);
                    //GC.Collect();
                    arcZipCursor_left = null;
                    arcFeatureZip_left = null;

                    // query the municipal layer
                    IFeatureCursor arcMuniCursor_left = clsGlobals.arcFLayerMunicipalities.Search(arcSpatialFilter_leftZip, false);
                    IFeature arcFeatureMuni_left = arcMuniCursor_left.NextFeature();

                    if (arcFeatureMuni_left != null)
                    {
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("L_CITY"), arcFeatureMuni_left.get_Value(arcFeatureMuni_left.Fields.FindField("NAME")));
                    }
                    else
                    {
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("L_CITY"), "");
                        //MessageBox.Show("A Municipality/City could not be found on the left side of the segment - based on the segment's midpoint with a 15 meter offset.", "Whoa there Cowboy!");
                    }

                    System.Runtime.InteropServices.Marshal.ReleaseComObject(arcMuniCursor_left);
                    arcMuniCursor_left = null;
                    arcFeatureMuni_left = null;
                    arcSpatialFilter_leftZip = null;


                    // RIGHT ZIP & MUNICIPALITY(SDE) //
                    // query zipcode layer for a zipcode on right side of segment // 
                    ISpatialFilter arcSpatialFilter_rightZip = new SpatialFilter();
                    arcSpatialFilter_rightZip.Geometry = outPoint_posRight;
                    arcSpatialFilter_rightZip.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

                    IFeatureCursor arcZipCursor_right = clsGlobals.arcFLayerZipCodes.Search(arcSpatialFilter_rightZip, false);
                    IFeature arcFeatureZip_right = arcZipCursor_right.NextFeature();
                    if (arcFeatureZip_right != null)
                    {
                        //update the value in the utrans based on the intersect
                        //arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("ZIPLEFT"), arcFeatureZip_right.get_Value(arcFeatureZip_right.Fields.FindField("ZIP5")));
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("ZIPRIGHT"), arcFeatureZip_right.get_Value(arcFeatureZip_right.Fields.FindField("ZIP5")));
                        //maybe update the usps_place field as well with the "name" field from the zipcodes layer
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("USPS_PLACE"), arcFeatureZip_right.get_Value(arcFeatureZip_right.Fields.FindField("NAME")).ToString().Trim());
                    }
                    else
                    {
                        MessageBox.Show("A zipcode could not be found on the right side of the segment - based on the segment's midpoint with a 15 meter offset.", "Whoa there Cowboy!");
                        //give option to leave blank or abort edit operation and return
                        //return;
                    }
                    //clear out variables
                    // release the cursor
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(arcFeatureZip_right);
                    //GC.Collect();
                    arcFeatureZip_right = null;
                    arcFeatureZip_right = null;

                    // query the municipal layer
                    IFeatureCursor arcMuniCursor_right = clsGlobals.arcFLayerMunicipalities.Search(arcSpatialFilter_rightZip, false);
                    IFeature arcFeatureMuni_right = arcMuniCursor_right.NextFeature();

                    if (arcFeatureMuni_right != null)
                    {
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("R_CITY"), arcFeatureMuni_right.get_Value(arcFeatureMuni_right.Fields.FindField("NAME")));
                    }
                    else
                    {
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("R_CITY"), "");
                        //MessageBox.Show("A Municipality/City could not be found on the right side of the segment - based on the segment's midpoint with a 15 meter offset.", "Whoa there Cowboy!");
                    }

                    System.Runtime.InteropServices.Marshal.ReleaseComObject(arcMuniCursor_right);
                    arcMuniCursor_right = null;
                    arcFeatureMuni_right = null;
                    arcSpatialFilter_rightZip = null;

                    // null out the offset points
                    outPoint_posRight = null;
                    outPoint_negLeft = null;

                    // COFIPS
                    IFeatureCursor arcCountiesCursor = clsGlobals.arcFLayerCounties.Search(arcSpatialFilter, false);
                    IFeature arcFeature_County = arcCountiesCursor.NextFeature();
                    if (arcFeature_County != null)
                    {
                        //update the value in the utrans based on the intersect
                        arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("COFIPS"), arcFeature_County.get_Value(arcFeature_County.Fields.FindField("FIPS_STR")));
                    }
                    else
                    {
                        MessageBox.Show("The midpoint of the street segment you are trying to update is not within a County.", "Whoa there Cowboy!");
                        //give option to leave blank or abort edit operation and return
                        //return;
                    }
                    //clear out variables
                    arcCountiesCursor = null;
                    arcFeature_County = null;

                    // FULLNAME //
                    //check if street name is numeric
                    int intStName;
                    if (int.TryParse(txtUtranStName.Text, out intStName))
                    {
                        string strFullNameNumeric = txtUtranStName.Text.Trim() + " " + txtUtranSufDir.Text.Trim();

                        //check if sufdir is populated and sttype is not
                        if (txtUtranSufDir.Text == "" | txtUtranStType.Text != "")
                        {
                            DialogResult dialogResult2 = MessageBox.Show("Format Warning!  You are saving a numberic street but have conflict with either SUFDIR or STREETTYPE." + Environment.NewLine + "Numberic Streets typically require a SUFDIR value and not a STREETTYPE value." + Environment.NewLine + Environment.NewLine + "Would you like to continue with the save?", "Format Warning!", MessageBoxButtons.YesNo);
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

                        //check if sttype is populated and sufdir is not
                        if (txtUtranSufDir.Text != "" | txtUtranStType.Text == "")
                        {
                            DialogResult dialogResult3 = MessageBox.Show("Format Warning!  You are saving an alphabetic street but have conflict with either SUFDIR or STREETTYPE." + Environment.NewLine + "Alphabetic Streets typically require a STREETTYPE and often do not include a SUFDIR value." + Environment.NewLine + Environment.NewLine + "Would you like to continue with the save?", "Format Warning!", MessageBoxButtons.YesNo);
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
                    string strAscAlias = txtUtransAcsName.Text.Trim() + " " + txtUtransAcsSuf.Text.Trim();
                    arcUtransEdit_Feature.set_Value(arcUtransEdit_Feature.Fields.FindField("ACSALIAS"), strAscAlias.Trim());

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
                arcQueryFilter_loadSegment.SubFields = "Shape,ZIPLEFT,ZIPRIGHT,L_F_ADD,L_T_ADD,R_F_ADD,R_T_ADD,PREDIR,STREETNAME,STREETTYPE,SUFDIR,ALIAS1,ALIAS1TYPE,ALIAS2,ALIAS2TYPE,ACSALIAS,ACSNAME,ACSSUF,USPS_PLACE,ONEWAY,SPEED,VERTLEVEL,CLASS,MODIFYDATE,COLLDATE,ACCURACY,SOURCE,NOTES,STATUS,ACCESS,USAGENOTES,BIKE_L,BIKE_R,BIKE_NOTES,BIKE_STATUS,GRID1MIL,GRID100K";
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
                string strL_F_add = arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("L_F_ADD")).ToString().Trim();
                string strL_T_add = arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("L_T_ADD")).ToString().Trim();
                string strR_F_add = arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("R_F_ADD")).ToString().Trim();
                string strR_T_add = arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("R_T_ADD")).ToString().Trim();


                //check for road segment has empty values for street range, if so pass in zero in where clause
                if (strL_F_add == "")
                {
                    strL_F_add = "is null";
                }
                else
                {
                    strL_F_add = "= " + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("L_F_ADD")).ToString();
                }

                if (strL_T_add == "")
                {
                    strL_T_add = "is null";
                }
                else
                {
                    strL_T_add = "= " + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("L_T_ADD")).ToString();
                }

                if (strR_F_add == "")
                {
                    strR_F_add = "is null";
                }
                else
                {
                    strR_F_add = "= " + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("R_F_ADD")).ToString();
                }

                if (strR_T_add == "")
                {
                    strR_T_add = "is null";
                }
                else
                {
                    strR_T_add = "= " + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("R_T_ADD")).ToString();
                }

                //select the new feature in the utrans database - based on values in the county street layer
                IQueryFilter arcQueryFilterNewUtransSegment = new QueryFilter();
                arcQueryFilterNewUtransSegment.WhereClause =
                    "L_F_ADD " + strL_F_add +
                    " AND L_T_ADD " + strL_T_add +
                    " AND R_F_ADD " + strR_F_add +
                    " AND R_T_ADD " + strR_T_add +
                    " AND PREDIR = '" + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("PREDIR")) + "'" +
                    " AND STREETNAME = '" + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("STREETNAME")) + "'" +
                    " AND STREETTYPE = '" + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("STREETTYPE")) + "'" +
                    " AND SUFDIR = '" + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("SUFDIR")) + "'";
                    //" AND ALIAS1 = '" + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("ALIAS1")) + "'" +
                    //" AND ALIAS1TYPE = '" + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("ALIAS1TYPE")) + "'" +
                    //" AND ALIAS2 = '" + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("ALIAS2")) + "'" +
                    //" AND ALIAS2TYPE = '" + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("ALIAS2TYPE")) + "'" +
                    //" AND ACSALIAS = '" + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("ACSALIAS")) + "'" +
                    //" AND ACSSUF = '" + arcFeature_CountyLoadSegment.get_Value(arcFeature_CountyLoadSegment.Fields.FindField("ACSSUF")) + "'";

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

                // spatial intersect for the following fields: ADDR_SYS, ADDR_QUAD, ZIPLEFT, ZIPRIGHT, COFIPS (Maybe USPS_PLACE)
                // ADDR_SYS and ADDR_QUAD
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
