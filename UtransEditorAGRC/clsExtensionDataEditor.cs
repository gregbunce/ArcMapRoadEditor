using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.ADF.CATIDs;
using ESRI.ArcGIS.Editor;
using System.Windows.Forms;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.Carto;

namespace UtransEditorAGRC
{
    [Guid("2912e0fd-818f-4652-acb1-d08f415e07d2")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("UtransEditorAGRC.clsExtensionDataEditor")]
    public class clsExtensionDataEditor : IExtension
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
            MxExtension.Register(regKey);

        }
        /// <summary>
        /// Required method for ArcGIS Component Category unregistration -
        /// Do not modify the contents of this method with the code editor.
        /// </summary>
        private static void ArcGISCategoryUnregistration(Type registerType)
        {
            string regKey = string.Format("HKEY_CLASSES_ROOT\\CLSID\\{{{0}}}", registerType.GUID);
            MxExtension.Unregister(regKey);

        }

        #endregion
        #endregion
        private IApplication m_application;

        #region IExtension Members

        /// <summary>
        /// Name of extension. Do not exceed 31 characters
        /// </summary>
        public string Name
        {
            get
            {
                //TODO: Modify string to uniquely identify extension
                return "clsExtensionDataEditor";
            }
        }

        public void Shutdown()
        {
            //TODO: Clean up resources

            //m_application = null;
            clsGlobals.arcApplication = null;
        }

        public void Startup(ref object initializationData)
        {
            try
            {
                //get the application
                clsGlobals.arcApplication = initializationData as IApplication;
                if (clsGlobals.arcApplication == null)
                    return;

                //m_application = initializationData as IApplication;
                //if (m_application == null)
                //    return;

                //get the editor extension
                UID arcUID = new UID();
                arcUID.Value = "esriEditor.Editor";
                clsGlobals.arcEditor = clsGlobals.arcApplication.FindExtensionByCLSID(arcUID) as IEditor3;

                //setup the event handlers

                //start editing
                ((IEditEvents_Event)clsGlobals.arcEditor).OnStartEditing += new IEditEvents_OnStartEditingEventHandler(clsExtensionDataEditor_OnStartEditing);

                //create feature
                ((IEditEvents_Event)clsGlobals.arcEditor).OnCreateFeature += new IEditEvents_OnCreateFeatureEventHandler(clsExtensionDataEditor_OnCreateFeature);

                //change feature
                ((IEditEvents_Event)clsGlobals.arcEditor).OnChangeFeature += new IEditEvents_OnChangeFeatureEventHandler(clsExtensionDataEditor_OnChangeFeature);

                //delete feature
                //((IEditEvents_Event)clsGlobals.arcEditor).OnDeleteFeature +=new IEditEvents_OnDeleteFeatureEventHandler(clsExtensionDataEditor_OnDeleteFeature);

                //stop editing
                ((IEditEvents_Event)clsGlobals.arcEditor).OnStopEditing += new IEditEvents_OnStopEditingEventHandler(clsExtensionDataEditor_OnStopEditing);

                //save edits
                //((IEditEvents2_Event)clsGlobals.arcEditor).OnSaveEdits +=new IEditEvents2_OnSaveEditsEventHandler(clsExtensionDataEditor_OnSaveEdits);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + " " + e.Source + " " + e.StackTrace + " " + e.TargetSite, "Error!");
            }
            finally 
            { 
            }
        }


        //the start editing event handler
        void clsExtensionDataEditor_OnStartEditing()
        {
            try
            {
                ////get the editor workspace
                //IWorkspace arcWspace = clsGlobals.arcEditor.EditWorkspace;

                ////if the workspace is not remote (sde), exit the sub
                //if (arcWspace.Type != esriWorkspaceType.esriRemoteDatabaseWorkspace) { return; }

                ////get the workspace as an IWorkspaceEdit
                //IWorkspaceEdit arcWspaceEdit = clsGlobals.arcEditor.EditWorkspace as IWorkspaceEdit;

                ////get the workspace as a feature workspace
                //IFeatureWorkspace arcFeatWspace = arcWspace as IFeatureWorkspace;

                ////get the current document
                //IMxDocument arcMxDoc = clsGlobals.arcApplication.Document as IMxDocument;

                ////get the focus map
                //IMap arcMapp = arcMxDoc.FocusMap;

                ////clear out any reference to the utrans street layer
                //clsGlobals.arcGeoFLayerUtransStreets = null;


                ////loop through the map layers and get the utrans.statewidestreets, the county roads data, and the detect feature change fc - all into IGeoFeatureLayer(s)
                //for (int i = 0; i < arcMapp.LayerCount; i++)
                //{
                //    if (arcMapp.get_Layer(i) is IGeoFeatureLayer)
                //    {
                //        try
                //        {
                //            IFeatureLayer arcFLayer = arcMapp.get_Layer(i) as IFeatureLayer;
                //            IFeatureClass arcFclass = arcFLayer.FeatureClass;
                //            IObjectclass arcObjclass = arcFclass as IObjectclass;
                //            if (arcObjclass.AliasName.ToString().ToUpper() == "UTRANS.TRANSADMIN.STATEWIDESTREETS")
                //            {
                //                clsGlobals.arcGeoFLayerUtransStreets = arcMapp.get_Layer(i) as IGeoFeatureLayer;
                //                MessageBox.Show("referenced utrans streets");
                //            }
                //            if (arcObjclass.AliasName.ToString().ToUpper() == "COUNTY_STREETS")
                //            {
                //                clsGlobals.arcGeoFLayerCountyStreets = arcMapp.get_Layer(i) as IGeoFeatureLayer;
                //                MessageBox.Show("referenced county streets");
                //            }
                //            if (arcObjclass.AliasName.ToString().ToUpper() == "DFC_RESULT")
                //            {
                //                clsGlobals.arcGeoFLayerDfcResult = arcMapp.get_Layer(i) as IGeoFeatureLayer;
                //                MessageBox.Show("referenced dfc results");
                //            }
                //        }

                //        catch (Exception e) { }//in case there is an error looping through layers (sometimes on group layers or dynamic xy layers), just keep going

                //    }
                //}

            }
            catch (Exception e) 
            {
                MessageBox.Show(e.Message + " " + e.Source + " " + e.StackTrace + " " + e.TargetSite, "Error!");
            }
            finally
            {
            }
        }



        //the on create feature event handler
        void clsExtensionDataEditor_OnCreateFeature(IObject arcObject) 
        {
            try
            {

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + " " + e.Source + " " + e.StackTrace + " " + e.TargetSite, "Error!");
            }
            finally
            {
            }
        }



        //the on change feature event handler
        void clsExtensionDataEditor_OnChangeFeature(IObject arcObject)
        {
            try
            {
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + " " + e.Source + " " + e.StackTrace + " " + e.TargetSite, "Error!");
            }
            finally
            {
            }
        }



        //the on stop editing event handler -- currently only closing the DB connection
        void clsExtensionDataEditor_OnStopEditing(Boolean isSave) 
        {
            try
            {
                //close the utrans editing form if open and visible
                //if (clsGlobals.UtransEdior2 != null)
                //{
                //    if (clsGlobals.UtransEdior2.Visible == true)
                //    {
                //        clsGlobals.UtransEdior2.Close();
                //    }
                //}
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + " " + e.Source + " " + e.StackTrace + " " + e.TargetSite, "Error!");
            }
            finally
            {
            }
        }



        //the on delete feature event handler
        void clsExtensionDataEditor_OnDeleteFeature(IObject arcObject)
        {
            //try
            //{
            //}
            //catch (Exception e)
            //{
            //    MessageBox.Show(e.Message + " " + e.Source + " " + e.StackTrace + " " + e.TargetSite, "Error!");
            //}
            //finally 
            //{ 
            //}
        }



        //the on save edits event handler -- currently not doing anything
        void clsExtensionDataEditor_OnSaveEdits()
        {
            //try
            //{
            //    //not doing anything currently
            //}
            //catch (Exception e)
            //{
            //    MessageBox.Show(e.Message + " " + e.Source + " " + e.StackTrace + " " + e.TargetSite, "Error!");
            //}
            //finally
            //{

            //}        
        }//all code commented out


        #endregion
    }
}
