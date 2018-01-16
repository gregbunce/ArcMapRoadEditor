using System;
using System.Drawing;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.ADF.BaseClasses;
using ESRI.ArcGIS.ADF.CATIDs;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.ArcMapUI;
using System.Windows.Forms;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace UtransEditorAGRC
{
    /// <summary>
    /// Summary description for btnAssignDfcOIDs.
    /// </summary>
    [Guid("5ef97ac3-f209-4eeb-9e21-3dc598d069a8")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("UtransEditorAGRC.btnAssignDfcOIDs")]
    public sealed class btnAssignDfcOIDs : BaseCommand
    {
        #region COM Registration Function(s)
        [ComRegisterFunction()]
        [ComVisible(false)]
        static void RegisterFunction(Type registerType)
        {
            // Required for ArcGIS Component Category Registrar support
            ArcGISCategoryRegistration(registerType);

            //
            // TODO: Add any COM registration code here
            //
        }

        [ComUnregisterFunction()]
        [ComVisible(false)]
        static void UnregisterFunction(Type registerType)
        {
            // Required for ArcGIS Component Category Registrar support
            ArcGISCategoryUnregistration(registerType);

            //
            // TODO: Add any COM unregistration code here
            //
        }

        #region ArcGIS Component Category Registrar generated code
        /// <summary>
        /// Required method for ArcGIS Component Category registration -
        /// Do not modify the contents of this method with the code editor.
        /// </summary>
        private static void ArcGISCategoryRegistration(Type registerType)
        {
            string regKey = string.Format("HKEY_CLASSES_ROOT\\CLSID\\{{{0}}}", registerType.GUID);
            MxCommands.Register(regKey);

        }
        /// <summary>
        /// Required method for ArcGIS Component Category unregistration -
        /// Do not modify the contents of this method with the code editor.
        /// </summary>
        private static void ArcGISCategoryUnregistration(Type registerType)
        {
            string regKey = string.Format("HKEY_CLASSES_ROOT\\CLSID\\{{{0}}}", registerType.GUID);
            MxCommands.Unregister(regKey);

        }

        #endregion
        #endregion
        bool blnFoundMatchingUtransSegment;
        IFeatureCursor arcFeatureCursor;
        private IApplication m_application;
        public btnAssignDfcOIDs()
        {
            //
            // TODO: Define values for the public properties
            //
            base.m_category = "AGRC"; //localizable text
            base.m_caption = "Assign DFC_RESULT OIDs with UTRAN's OIDs";  //localizable text
            base.m_message = "Select DFC_RESULT Segments to Assign OIDs from UTRANS (updates OIDs with UTRANS OIDs)";  //localizable text 
            base.m_toolTip = "Assign Selected DFC_RESULT with UTRAN's OIDs";  //localizable text 
            base.m_name = "AssignDfcOIDs";   //unique id, non-localizable (e.g. "MyCategory_ArcMapCommand")
            base.m_bitmap = Properties.Resources.CadastralCreateLineString16;

        }

        #region Overridden class Methods

        /// <summary>
        /// Occurs when this command is created
        /// </summary>
        /// <param name="hook">Instance of the application</param>
        public override void OnCreate(object hook)
        {
            if (hook == null)
                return;

            m_application = hook as IApplication;

            //Disable if it is not ArcMap
            if (hook is IMxApplication)
                base.m_enabled = true;
            else
                base.m_enabled = false;
            
        }


        // enable button if utrans data is in map and editable
        public override bool Enabled
        {
            get
            {
                // check the global bool variable to see if the utrans tool can edit - enable/disable this tool based on the result

                if (clsGlobals.blnCanUseUtransTool == true)
                {
                    return true;
                }
                if (clsGlobals.blnCanUseUtransTool == false)
                {
                    return false;
                }

                // or just do it this way (without the two if statements)
                return clsGlobals.blnCanUseUtransTool;
            }
        }


        /// <summary>
        /// Occurs when this command is clicked
        /// </summary>
        public override void OnClick()
        {
            try
            {
                // get access to the map and see what's selected in the dfc_results layer
                if (clsGlobals.arcGeoFLayerDfcResult != null)
	            {
                    IFeatureSelection arcFeatSelection_DFC = clsGlobals.arcGeoFLayerDfcResult as IFeatureSelection;
                    ISelectionSet arcSelectionSet_DFC = arcFeatSelection_DFC.SelectionSet;

                    // create an enumerator to loop through the selection set
                    IEnumIDs enumIDs = arcSelectionSet_DFC.IDs;
                    int intID = 0;
                    
                    // loop throug the ids (-1 is returned after the last valid ID has been reached)
                    while ((intID = enumIDs.Next()) != -1)
	                {
                        IFeature arcFeature_DFC = clsGlobals.arcGeoFLayerDfcResult.FeatureClass.GetFeature(intID);

                        // create a spatial filter to select all the intersecting utrans segments to see if any are equal
                        ISpatialFilter arcSpatialFilter = new SpatialFilter();
                        arcSpatialFilter.Geometry = arcFeature_DFC.Shape;
                        arcSpatialFilter.GeometryField = clsGlobals.arcGeoFLayerDfcResult.FeatureClass.ShapeFieldName;
                        arcSpatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                        // put a definition query on the dfc layer to only use the selected feature (using its oid)
                        //arcSpatialFilter.WhereClause = "OBJECTID = " + intID.ToString();

                        // execute the spatial filter on the utrans feature class
                        arcFeatureCursor = clsGlobals.arcGeoFLayerUtransStreets.FeatureClass.Search(arcSpatialFilter, true);
                        IFeature arcFeatureIntersectingUtrans = arcFeatureCursor.NextFeature();


                        blnFoundMatchingUtransSegment = false;

                        // loop through all the utrans segments that intersected the dfc selected feature
                        while (arcFeatureIntersectingUtrans != null)
                        {
                            // compare the geometries to see if they are equal
                            // use IRelationalOperator to see if a spatial relationship exists in utrans for the selected dfc feature
                            ESRI.ArcGIS.Geometry.IRelationalOperator relationalOperator = arcFeature_DFC.Shape as IRelationalOperator;
                            bool blnFoundEqualSegment = relationalOperator.Equals(arcFeatureIntersectingUtrans.Shape);

                            // if a matching utrans segment is found
                            if (blnFoundEqualSegment)
                            {
                                //MessageBox.Show("found equal segment in utrans with OID of " + arcFeatureIntersectingUtrans.OID.ToString());

                                arcFeature_DFC.set_Value(arcFeature_DFC.Fields.FindField("PREV__NOTES"), arcFeature_DFC.get_Value(arcFeature_DFC.Fields.FindField("BASE_FID")).ToString());
                                arcFeature_DFC.set_Value(arcFeature_DFC.Fields.FindField("BASE_FID"), arcFeatureIntersectingUtrans.OID.ToString());

                                // found matching utrans segment
                                blnFoundMatchingUtransSegment = true;

                                // break out.. no need to search the others in the cursor
                                break;
                            }
                            else // no matching utrans segment was found
                            {
                                //MessageBox.Show("did not find equal segment in utrans");
                            }

                            arcFeatureIntersectingUtrans = arcFeatureCursor.NextFeature();

                            arcFeature_DFC.Store();
                        }

                        //if no match in utrans was found..
                        if (blnFoundMatchingUtransSegment == false)
                        {
                            arcFeature_DFC.set_Value(arcFeature_DFC.Fields.FindField("PREV__NOTES"), "BASE_FID Was Not Updated - No Overlapping Utrans Segment Found");
                            arcFeature_DFC.Store();
                        }                  
	                }

                    // set it to zero for next use
                    intID = 0;

                    // notify user that not all segments were found in utrans
                    if (blnFoundMatchingUtransSegment == false)
                    {
                        MessageBox.Show("One or more of the selected DFC_RESULT segments could not find an overlapping feature in Utrans.  In that case, the ObjectID was not updated for that segment. Check the DFC_RESULT layer's PREV_NOTES (DfcPrevNotes) field for more info");                          
                    }
 
                    // release the cursor
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(arcFeatureCursor);
                    GC.Collect();

	            }
                else
                {
                    MessageBox.Show("Oops, this tool doesn't have access to the Map's DFC_RESULT layer yet...  To resolve this, just open the Utrans Editor Form then click the button again.");
                    return;
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

        #endregion
    }
}
