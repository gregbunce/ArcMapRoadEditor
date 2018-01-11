using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UtransEditorAGRC
{
    public partial class ExportToIgnoreFC : Form
    {
        IMap arcMapp;
        IFeatureLayer arcFL_CountyStreets = null;
        IFeatureLayer arcFL_UtransStreet = null;
        IFeatureLayer arcFL_DFC = null;
        IFeatureLayer arcFL_IgnoreFGDB = null;
        IFeature arcFeat_DFC = null;
        //string[] strCOFIPS_Array;

        public ExportToIgnoreFC()
        {
            InitializeComponent();
        }


        private void ExportToIgnoreFC_Load(object sender, EventArgs e)
        {
            try
            {
                // loop through the map and populate the combo-boxes with the available feature layers

                //show busy mouse
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;

                //get the current document
                IMxDocument arcMxDoc = clsGlobals.arcApplication.Document as IMxDocument;

                //get the focus map
                arcMapp = arcMxDoc.FocusMap;

                IActiveView arcActiveView = arcMapp as IActiveView;

                ILayer pLayer;
                IFeatureLayer pFeatureLayer;

                //load the comboboxs with the map's polygon layers
                for (int i = 0; i < arcMapp.LayerCount; i++)
                {
                    pLayer = arcMapp.get_Layer(i);

                    if (pLayer is FeatureLayer)
                    {
                        pFeatureLayer = pLayer as IFeatureLayer;

                        if (pFeatureLayer.FeatureClass.ShapeType == ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPolyline)
                        {
                            cboCountyStreets.Items.Add(pFeatureLayer.Name);
                            cboDFC_RESULT.Items.Add(pFeatureLayer.Name);
                            cboIgnoredFC.Items.Add(pFeatureLayer.Name);
                            cboUtransStreets.Items.Add(pFeatureLayer.Name);
                        }
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



        // this method is run when the user clicks the run button
        private void btnRun_Click(object sender, EventArgs e)
        {

            try
            {
                //show busy mouse
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;

                // loop through the map get access to the layers in the combo boxes
                if (cboCountyName.SelectedIndex != -1 & cboCountyStreets.SelectedIndex != -1 & cboDFC_RESULT.SelectedIndex != -1 & cboIgnoredFC.SelectedIndex != -1 & cboUtransStreets.SelectedIndex != -1)
                {
                    //loop through the map's layer and get access to the layer in the census combobox
                    for (int i = 0; i < arcMapp.LayerCount; i++)
                    {
                        if (arcMapp.get_Layer(i).Name == cboCountyStreets.Text)
                        {
                            arcFL_CountyStreets = arcMapp.get_Layer(i) as IFeatureLayer;
                        }
                        if (arcMapp.get_Layer(i).Name == cboDFC_RESULT.Text)
                        {
                            arcFL_DFC = arcMapp.get_Layer(i) as IFeatureLayer;
                        }
                        if (arcMapp.get_Layer(i).Name == cboIgnoredFC.Text)
                        {
                            arcFL_IgnoreFGDB = arcMapp.get_Layer(i) as IFeatureLayer;
                        }
                        if (arcMapp.get_Layer(i).Name == cboUtransStreets.Text)
                        {
                            arcFL_UtransStreet = arcMapp.get_Layer(i) as IFeatureLayer;
                        }
                    }


                    //make sure we have access to all the needed layers
                    if (arcFL_CountyStreets == null)
                    {
                        MessageBox.Show("Please select the correct layer for COUNTY_STREETS", "Layer Name Issue", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    if (arcFL_DFC == null)
                    {
                        MessageBox.Show("Please select the correct layer for DFC_RESULT", "Layer Name Issue", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    if (arcFL_IgnoreFGDB == null)
                    {
                        MessageBox.Show("Please select the correct layer for the FGDB Ignore feature class", "Layer Name Issue", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    if (arcFL_UtransStreet == null)
                    {
                        MessageBox.Show("Please select the correct layer for UTRANS.Roads_Edit", "Layer Name Issue", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }


                    // split the cofips value to get out the number
                    string strCOFIPS_Combobox = null;
                    string[] strCOFIPS_Array = null;
                    string strCOFIPS_Number = null;
                    strCOFIPS_Combobox = cboCountyName.Text;
                    strCOFIPS_Array = strCOFIPS_Combobox.Split('-');
                    strCOFIPS_Number = strCOFIPS_Array[0].ToString().Trim();


                    // set up feature cursor for getting the ignore records from the dfc
                    IQueryFilter arcQF_DFC_Ignore = new QueryFilter();
                    arcQF_DFC_Ignore.WhereClause = "CURRENT_NOTES in ('NOTIFY AND IGNORE', 'IGNORE')";
                    IFeatureCursor arcFeatCursor_DFC = arcFL_DFC.Search(arcQF_DFC_Ignore, false);

                    // loop through the dfc_result layer's "ignore" and 'notify and ignore' features
                    while ((arcFeat_DFC = arcFeatCursor_DFC.NextFeature()) != null)
                    {
                        //create a new feature in the ignore fgdb feature class and give it the geometry of the current dfc_result feature
                        IFeature arcNewFeature = arcFL_IgnoreFGDB.FeatureClass.CreateFeature();
                        arcNewFeature.Shape = arcFeat_DFC.Shape;

                        string strUtransOID = null;
                        string strCountyRoadsOID = null;
                        string strCurrentNotes = null;
                        string strPrevNotes = null;
                        string strChangeType = null;


                        // get the field values from the dfc_result layer
                        strUtransOID = arcFeat_DFC.get_Value(arcFeat_DFC.Fields.FindField("BASE_FID")).ToString();
                        strCountyRoadsOID = arcFeat_DFC.get_Value(arcFeat_DFC.Fields.FindField("UPDATE_FID")).ToString();
                        strCurrentNotes = arcFeat_DFC.get_Value(arcFeat_DFC.Fields.FindField("CURRENT_NOTES")) as string;
                        strPrevNotes = arcFeat_DFC.get_Value(arcFeat_DFC.Fields.FindField("PREV__NOTES")) as string;
                        strChangeType = arcFeat_DFC.get_Value(arcFeat_DFC.Fields.FindField("CHANGE_TYPE")) as string;

                        // set field values for new feature
                        arcNewFeature.set_Value(arcFL_IgnoreFGDB.FeatureClass.Fields.FindField("UPDATE_FID"), strCountyRoadsOID);
                        arcNewFeature.set_Value(arcFL_IgnoreFGDB.FeatureClass.Fields.FindField("BASE_FID"), strUtransOID);
                        arcNewFeature.set_Value(arcFL_IgnoreFGDB.FeatureClass.Fields.FindField("CHANGE_TYPE"), strChangeType);
                        arcNewFeature.set_Value(arcFL_IgnoreFGDB.FeatureClass.Fields.FindField("CURRENT_NOTES"), strCurrentNotes);
                        arcNewFeature.set_Value(arcFL_IgnoreFGDB.FeatureClass.Fields.FindField("PREV__NOTES"), strPrevNotes);
                        arcNewFeature.set_Value(arcFL_IgnoreFGDB.FeatureClass.Fields.FindField("Date_Ignored"), dateTimePickerExportIgnores.Value.ToShortDateString());
                        arcNewFeature.set_Value(arcFL_IgnoreFGDB.FeatureClass.Fields.FindField("COFIPS"), strCOFIPS_Number);

                        // set up query filters and cursors for the utrans segment to get values from
                        if (strUtransOID != "-1")
                        {
                            IQueryFilter arcQF_Utrans = new QueryFilter();
                            arcQF_Utrans.WhereClause = "OBJECTID = " + strUtransOID;
                            IFeatureCursor arcFeatCursor_Utrans = arcFL_UtransStreet.Search(arcQF_Utrans, false);
                            IFeature arcFeat_Utrans = arcFeatCursor_Utrans.NextFeature();

                            if (arcFeat_Utrans != null)
                            {
                                //populate the utrans segment with the needed values
                                arcNewFeature.set_Value(arcFL_IgnoreFGDB.FeatureClass.Fields.FindField("UtransSegment"), "| " +
                                    arcFeat_Utrans.get_Value(arcFeat_Utrans.Fields.FindField("L_F_ADD")).ToString().Trim() + " | " +
                                    arcFeat_Utrans.get_Value(arcFeat_Utrans.Fields.FindField("L_T_ADD")).ToString().Trim() + " | " +
                                    arcFeat_Utrans.get_Value(arcFeat_Utrans.Fields.FindField("R_F_ADD")).ToString().Trim() + " | " +
                                    arcFeat_Utrans.get_Value(arcFeat_Utrans.Fields.FindField("R_T_ADD")).ToString().Trim() + " | " +
                                    arcFeat_Utrans.get_Value(arcFeat_Utrans.Fields.FindField("PREDIR")).ToString().Trim() + " | " +
                                    arcFeat_Utrans.get_Value(arcFeat_Utrans.Fields.FindField("STREETNAME")).ToString().Trim() + " | " +
                                    arcFeat_Utrans.get_Value(arcFeat_Utrans.Fields.FindField("STREETTYPE")).ToString().Trim() + " | " +
                                    arcFeat_Utrans.get_Value(arcFeat_Utrans.Fields.FindField("SUFDIR")).ToString().Trim() + " | " +
                                    arcFeat_Utrans.get_Value(arcFeat_Utrans.Fields.FindField("ALIAS1")).ToString().Trim() + " | " +
                                    arcFeat_Utrans.get_Value(arcFeat_Utrans.Fields.FindField("ALIAS1TYPE")).ToString().Trim() + " | " +
                                    arcFeat_Utrans.get_Value(arcFeat_Utrans.Fields.FindField("ALIAS2")).ToString().Trim() + " | " +
                                    arcFeat_Utrans.get_Value(arcFeat_Utrans.Fields.FindField("ALIAS2TYPE")).ToString().Trim() + " | " +
                                    arcFeat_Utrans.get_Value(arcFeat_Utrans.Fields.FindField("ACSNAME")).ToString().Trim() + " | " +
                                    arcFeat_Utrans.get_Value(arcFeat_Utrans.Fields.FindField("ACSSUF")).ToString().Trim() + " |");

                                //null out query filter and feature cursor
                                System.Runtime.InteropServices.Marshal.ReleaseComObject(arcFeatCursor_Utrans);
                                GC.Collect();
                                arcFeatCursor_Utrans = null;
                                arcQF_Utrans = null;                               
                            }
                            else
                            {
                                //utrans segment not found
                                arcNewFeature.set_Value(arcFL_IgnoreFGDB.FeatureClass.Fields.FindField("UtransSegment"), "None Found");
                            }
                        }
                        else
                        {
                            //utrans segment not found
                            arcNewFeature.set_Value(arcFL_IgnoreFGDB.FeatureClass.Fields.FindField("UtransSegment"), "None Found");
                        }



                        // get the values from the county_streets layer
                        IQueryFilter arcQF_CountyStreets = new QueryFilter();
                        arcQF_CountyStreets.WhereClause = "OBJECTID = " + strCountyRoadsOID;
                        IFeatureCursor arcFeatCursor_CountyStreets = arcFL_CountyStreets.Search(arcQF_CountyStreets, false);
                        IFeature arcFeat_CountyStreets = arcFeatCursor_CountyStreets.NextFeature();

                        if (arcFeat_CountyStreets != null)
                        {
                            //populate the utrans segment with the needed values
                            arcNewFeature.set_Value(arcFL_IgnoreFGDB.FeatureClass.Fields.FindField("CountySegment"), "| " +
                                arcFeat_CountyStreets.get_Value(arcFeat_CountyStreets.Fields.FindField("L_F_ADD")).ToString().Trim() + " | " +
                                arcFeat_CountyStreets.get_Value(arcFeat_CountyStreets.Fields.FindField("L_T_ADD")).ToString().Trim() + " | " +
                                arcFeat_CountyStreets.get_Value(arcFeat_CountyStreets.Fields.FindField("R_F_ADD")).ToString().Trim() + " | " +
                                arcFeat_CountyStreets.get_Value(arcFeat_CountyStreets.Fields.FindField("R_T_ADD")).ToString().Trim() + " | " +
                                arcFeat_CountyStreets.get_Value(arcFeat_CountyStreets.Fields.FindField("PREDIR")).ToString().Trim() + " | " +
                                arcFeat_CountyStreets.get_Value(arcFeat_CountyStreets.Fields.FindField("STREETNAME")).ToString().Trim() + " | " +
                                arcFeat_CountyStreets.get_Value(arcFeat_CountyStreets.Fields.FindField("STREETTYPE")).ToString().Trim() + " | " +
                                arcFeat_CountyStreets.get_Value(arcFeat_CountyStreets.Fields.FindField("SUFDIR")).ToString().Trim() + " | " +
                                arcFeat_CountyStreets.get_Value(arcFeat_CountyStreets.Fields.FindField("ALIAS1")).ToString().Trim() + " | " +
                                arcFeat_CountyStreets.get_Value(arcFeat_CountyStreets.Fields.FindField("ALIAS1TYPE")).ToString().Trim() + " | " +
                                arcFeat_CountyStreets.get_Value(arcFeat_CountyStreets.Fields.FindField("ALIAS2")).ToString().Trim() + " | " +
                                arcFeat_CountyStreets.get_Value(arcFeat_CountyStreets.Fields.FindField("ALIAS2TYPE")).ToString().Trim() + " | " +
                                arcFeat_CountyStreets.get_Value(arcFeat_CountyStreets.Fields.FindField("ACSNAME")).ToString().Trim() + " | " +
                                arcFeat_CountyStreets.get_Value(arcFeat_CountyStreets.Fields.FindField("ACSSUF")).ToString().Trim() + " |");

                            //null out query filter and feature cursor
                            System.Runtime.InteropServices.Marshal.ReleaseComObject(arcFeat_CountyStreets);
                            GC.Collect();
                            arcFeat_CountyStreets = null;
                            arcQF_CountyStreets = null;                            
                        }
                        else
                        {
                            arcNewFeature.set_Value(arcFL_IgnoreFGDB.FeatureClass.Fields.FindField("CountySegment"), "None Found");
                        }


                        // store the new row/feature
                        arcNewFeature.Store();
                    }

                    //null out query filter and feature cursor
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(arcQF_DFC_Ignore);
                    GC.Collect();

                    MessageBox.Show("Done exporting Ignores from " + strCOFIPS_Array[1].ToString().Trim() + " County's DFC_RESULT layer.", "Finished!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    //close the form
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Make sure all the dropdown menus have been choosen.", "Missing Selections", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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
    }
}
