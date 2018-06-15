using System;
using System.Drawing;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.ADF.BaseClasses;
using ESRI.ArcGIS.ADF.CATIDs;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.ArcMapUI;
using System.Windows.Forms;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;

namespace UtransEditorAGRC
{
    /// <summary>
    /// Summary description for btnGoogleStreetViewTool.
    /// </summary>
    [Guid("9c72acd1-2825-44a1-a540-e4f795eae241")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("UtransEditorAGRC.btnGoogleStreetViewTool")]
    public sealed class btnGoogleStreetViewTool : BaseTool
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
        public btnGoogleStreetViewTool()
        {
            //
            // TODO: Define values for the public properties
            //
            base.m_category = "AGRC"; //localizable text 
            base.m_caption = "Google Maps Streetview";  //localizable text 
            base.m_message = "This tool opens up the default web browser to Google Streetview, showing the area clicked in ArcMap.";  //localizable text
            base.m_toolTip = "Google Maps Streetview";  //localizable text
            base.m_name = "UtransGoogleStreetview";   //unique id, non-localizable (e.g. "MyCategory_ArcMapTool")
            base.m_cursor = new System.Windows.Forms.Cursor(GetType(), GetType().Name + ".cur");
            base.m_bitmap = Properties.Resources.btnGoogleStreetViewTool;

            //try
            //{
                //
                // TODO: change resource name if necessary
                //
                //string bitmapResourceName = GetType().Name + ".bmp";
                //base.m_bitmap = new Bitmap(GetType(), bitmapResourceName);
                //base.m_cursor = new System.Windows.Forms.Cursor(GetType(), GetType().Name + ".cur");
            //}
            //catch (Exception ex)
            //{
            //    System.Diagnostics.Trace.WriteLine(ex.Message, "Invalid Bitmap");
            //}
        }

        #region Overridden class Methods

        /// <summary>
        /// Occurs when this tool is created
        /// </summary>
        /// <param name="hook">Instance of the application</param>
        public override void OnCreate(object hook)
        {
            m_application = hook as IApplication;

            //Disable if it is not ArcMap
            if (hook is IMxApplication)
                base.m_enabled = true;
            else
                base.m_enabled = false;

            // TODO:  Add other initialization code
        }

        /// <summary>
        /// Occurs when this tool is clicked
        /// </summary>
        public override void OnClick()
        {
            // TODO: Add btnGoogleStreetViewTool.OnClick implementation
        }


        // this method is called when the tool is active and the user clicks down on the mouse - it captures the x,y coords and then reprojects them to wgs and opens a browser with streetview
        public override void OnMouseDown(int Button, int Shift, int X, int Y)
        {
            try
            {
                //show busy mouse
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;


                //get access to the document (the current mxd), and the active view (data view or layout view), and the focus map (the data frame with focus, aka: the visible map)
                IMxDocument pMxDocument = clsGlobals.arcApplication.Document as IMxDocument;
                IActiveView pActiveView = pMxDocument.ActiveView;
                IMap pMap = pActiveView.FocusMap;

                //IScreenDisplay screenDisplay = pActiveView.ScreenDisplay;
                //IDisplayTransformation displayTransformation = screenDisplay.DisplayTransformation;
                //displayTransformation.ToMapPoint((System.Int32)X, (System.Int32)Y);


                IEnvelope pEnvelope = pActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(X, Y).Envelope;

                //the following lines of code are needed to reproject the map units into google maps lat long
                //Create Spatial Reference Factory
                ISpatialReferenceFactory srFactory = new SpatialReferenceEnvironmentClass();

                IProjectedCoordinateSystem arcProjCoordSysUTM12_North = srFactory.CreateProjectedCoordinateSystem(26912);
                IGeographicCoordinateSystem arcGeographicCoordSysWgs84 = srFactory.CreateGeographicCoordinateSystem(4326);//esriSRGeoCS_WGS1984 4326 WGS 1984.


                //get the point on the map that was clicked on
                IPoint pMapPoint = new ESRI.ArcGIS.Geometry.Point();
                pMapPoint.X = Convert.ToDouble(pEnvelope.XMax);
                pMapPoint.Y = Convert.ToDouble(pEnvelope.YMax);

                //give the arcmap point a spatial reference
                pMapPoint.SpatialReference = arcProjCoordSysUTM12_North;

                //reproject that point to nad83 arizona central state plane
                pMapPoint.Project(arcGeographicCoordSysWgs84);

                //MessageBox.Show(pMapPoint.X + ",   " + pMapPoint.Y, "caption", MessageBoxButtons.OK, MessageBoxIcon.Information);

                //open the default internet browser and pass in the apn number to the assessor's gis website, and then zoom to the apn
                System.Diagnostics.Process.Start("https://maps.google.com/maps?q=&layer=c&cbll=" + pMapPoint.Y + "," + pMapPoint.X + "&cbp=12,0,0,0,0");

                //deactivate the tool
                //this.m_deactivate = true;
                //this.Deactivate();
                //base.Deactivate();
                //this.Deactivate();


            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        public override void OnMouseMove(int Button, int Shift, int X, int Y)
        {
            // TODO:  Add btnGoogleStreetViewTool.OnMouseMove implementation
        }

        public override void OnMouseUp(int Button, int Shift, int X, int Y)
        {
            // TODO:  Add btnGoogleStreetViewTool.OnMouseUp implementation
        }
        #endregion
    }
}
