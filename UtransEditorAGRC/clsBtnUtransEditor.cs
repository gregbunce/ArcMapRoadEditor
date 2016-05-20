using System;
using System.Drawing;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.ADF.BaseClasses;
using ESRI.ArcGIS.ADF.CATIDs;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.Editor;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Carto;

namespace UtransEditorAGRC
{
    /// <summary>
    /// Summary description for btnUtransEditor.
    /// </summary>
    [Guid("a47c1699-cbdc-48d0-8b47-b9de988a81f9")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("UtransEditorAGRC.btnUtransEditor")]
    public sealed class clsBtnUtransEditor : BaseCommand
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

        private IApplication m_application;
        public clsBtnUtransEditor()
        {
            //
            // TODO: Define values for the public properties
            //
            base.m_category = "AGRC"; //localizable text
            base.m_caption = "UTRANS Editor";  //localizable text
            base.m_message = "This tool assists in the UTRANS update process";  //localizable text 
            base.m_toolTip = "AGRC UTRANS Editor Tool";  //localizable text 
            base.m_name = "UtransEditorTool";   //unique id, non-localizable (e.g. "MyCategory_ArcMapCommand")
            base.m_bitmap = Properties.Resources.clsBtnUtransEditor;

            //try
            //{
            //    //
            //    // TODO: change bitmap name if necessary
            //    //
            //    string bitmapResourceName = GetType().Name + ".bmp";
            //    base.m_bitmap = new Bitmap(GetType(), bitmapResourceName);
            //}
            //catch (Exception ex)
            //{
            //    System.Diagnostics.Trace.WriteLine(ex.Message, "Invalid Bitmap");
            //}
        }

        #region Overridden Class Methods

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

            // TODO:  Add other initialization code
        }


        //enable button if utrans data is in map and editable
        public override bool Enabled
        {
            get
            {
                try
                {
                    //check to see if the street layer is in the map and is editable                              
                    bool isEditable = false;

                    //get a reference to ieditlayers to see which layers are editable
                    IEditLayers arcEditLayers = clsGlobals.arcEditor as IEditLayers;
                    IMap arcMapp = clsGlobals.arcEditor.Map;
                    //make sure there is a map document
                    if (arcMapp == null) { return false; }

                    //loop through all the layers in the map and see if streets is there and editable
                    for (int i = 0; i < arcMapp.LayerCount; i++)
                    {
                        if (arcMapp.get_Layer(i).Name.ToUpper() == "UTRANS.TRANSADMIN.STATEWIDESTREETS")
                        {
                            if (arcEditLayers.IsEditable(arcMapp.get_Layer(i) as IFeatureLayer))
                            {
                                isEditable = true;
                            }
                            else
                                isEditable = false;

                        }
                    }
                    if (isEditable)
                    {
                        return true;
                    }
                    else
                        return false;
                }
                catch (Exception e)
                {
                    //MessageBox.Show(e.Message + " " + e.Source + " " + e.StackTrace + " " + e.TargetSite, "Error!");
                    return false;
                }
            }
        }


        /// <summary>
        /// Occurs when this command is clicked
        /// </summary>
        public override void OnClick()
        {
            // open form
            frmUtransEditor UtransEdior = new frmUtransEditor();
            UtransEdior.Show(new clsModelessDialog(m_application.hWnd));

        }

        #endregion
    }
}