//   Copyright 2019 Esri
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at

//       http://www.apache.org/licenses/LICENSE-2.0

//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License. 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Core.Data;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using ArcGIS.Desktop.Framework.Dialogs;
using System.Windows.Input;
using ArcGIS.Core.CIM;

namespace CustomPopup
{
    /// <summary>
    /// Implementation of custom pop-up tool.
    /// </summary>
    internal class CustomPopupTool : MapTool
    {
        /// <summary>
        /// Define the tool as a sketch tool that draws a rectangle in screen space on the view.
        /// </summary>
        public CustomPopupTool()
        {
            IsSketchTool = false ;
            SketchType = SketchGeometryType.Point;
            SketchOutputMode = SketchOutputMode.Screen; //required for 3D selection and identify.
            WebInit.SetWebBrowserFeatures(11);
        }
      
        protected override void OnToolMouseDown(MapViewMouseButtonEventArgs e)
        {

            switch (e.ChangedButton)
            {
                case MouseButton.Left:
                    e.Handled = true;
                    break;
            }
        }
        protected override async Task HandleMouseDownAsync(MapViewMouseButtonEventArgs e)
        {
            await QueuedTask.Run(() =>
            {
                var mapPoint = MapPointBuilder.CreateMapPoint(e.ClientPoint.X, e.ClientPoint.Y);
                var result = ActiveMapView.GetFeatures(mapPoint);
                List<PopupContent> popups = new List<PopupContent>();
                foreach (var kvp in result)
                {
                    var layer = kvp.Key as BasicFeatureLayer;
                    if (layer == null)
                        continue;
                    var fields = layer.GetFieldDescriptions().Where(f => f.Name == "DI_JI_HAO");
                    var tableDef = layer.GetTable().GetDefinition();
                    var oidField = tableDef.GetObjectIDField();
                    foreach (var id in kvp.Value)
                    {
                        //获取地级编号
                        //DI_JI_HAO
                        var qf = new QueryFilter() { WhereClause = $"{oidField} = {id}", SubFields = string.Join(",", fields.Select(f => f.Name)) };
                        var rows = layer.Search(qf);
                        if (!rows.MoveNext())
                            continue;
                        using (var row = rows.Current)
                        {
                            foreach (var field in fields)
                            {
                                var val = row[field.Name];
                                if (field.Name == "DI_JI_HAO")
                                {
                                    PopupContent pc = new PopupContent(new Uri("http://59.42.105.34:5001/client/?id=" + val), "林业");
                                    popups.Add(pc);
                                }
                            }
                        }
                    }
                        
                }

                //Flash the features that intersected the sketch geometry.
                MessageBox.Show(popups.ToString());
                ActiveMapView.FlashFeature(result);
                var height = System.Windows.SystemParameters.WorkArea.Height / 2;
                var width = System.Windows.SystemParameters.WorkArea.Width / 2;
                var topLeftCornerPoint = new System.Windows.Point(0, 0);
                var popupDef = new PopupDefinition()
                {
                    Append = true,      // if true new record is appended to existing (if any)
                    Dockable = true,    // if true popup is dockable - if false Append is not applicable
                    Position = topLeftCornerPoint,  // Position of top left corner of the popup (in pixels)
                    Size = new System.Windows.Size(width, height)    // size of the popup (in pixels)
                };

                //Show the custom pop-up with the custom commands and the default pop-up commands. 
                try
                {
                    ActiveMapView.ShowCustomPopup(popups, null, true, popupDef);
                } catch(System.Exception e1)
                {
                    MessageBox.Show(string.Format("{0}", e1));
                }
                
                //return the collection of pop-up content object.

            });
            

        }

        /// <summary>
        /// Called when a sketch is completed.
        /// </summary>
        protected override async Task<bool> OnSketchCompleteAsync(ArcGIS.Core.Geometry.Geometry geometry)
        {
            List<PopupContent> popupContent = await QueuedTask.Run(() =>
            {
                //Get the features that intersect the sketch geometry.
                var mapPoint = geometry as MapPoint;
                var sb = new StringBuilder();
          
                sb.AppendLine(string.Format("OnSketchCompleteAsync X: {0}", mapPoint.X));
                sb.Append(string.Format("Y: {0}", mapPoint.Y));
                if (mapPoint.HasZ)
                {
                    sb.AppendLine();
                    sb.Append(string.Format("Z: {0}", mapPoint.Z));
                }
                MessageBox.Show(sb.ToString());
                
                var result = ActiveMapView.GetFeatures(geometry);

                //For each feature in the result create a new instance of our custom pop-up content class.
                List<PopupContent> popups = new List<PopupContent>();
                foreach (var kvp in result)
                {
                    //kvp.Value.ForEach(id => popups.Add(new DynamicPopupContent(kvp.Key, id)));
                    //kvp.Value.ForEach(id => popups.Add(new PopupContent(new Uri("https://www.google.com/webhp?ie=UTF-8&rct=j"), "xxxx")));
                    //popups.Add(new PopupContent("<b>This text is bold.</b>", "Custom tooltip from HTML string"));
                    
                    var layer = kvp.Key as BasicFeatureLayer;
                    if (layer == null)
                        continue;
                    var fields = layer.GetFieldDescriptions().Where(f => f.Name == "DI_JI_HAO");
                    var tableDef = layer.GetTable().GetDefinition();
                    var oidField = tableDef.GetObjectIDField();
                    foreach (var id in kvp.Value)
                    {
                        //获取地级编号
                        //DI_JI_HAO
                        var qf = new QueryFilter() { WhereClause = $"{oidField} = {id}", SubFields = string.Join(",", fields.Select(f => f.Name)) };
                        var rows = layer.Search(qf);
                        if (!rows.MoveNext())
                            continue;
                        using (var row = rows.Current)
                        {
                            foreach(var field in fields)
                            {
                                var val = row[field.Name];
                                if (field.Name == "DI_JI_HAO")
                                {
                                    PopupContent pc = new PopupContent(new Uri("http://59.42.105.34:5001/client/?id=" + val), "林业");
                                    popups.Add(pc);
                                }
                            }
                        }
                    }
                }

          //Flash the features that intersected the sketch geometry.
          ActiveMapView.FlashFeature(result);

          //return the collection of pop-up content object.
          return popups;
            });
            var height = System.Windows.SystemParameters.WorkArea.Height/2;
            var width = System.Windows.SystemParameters.WorkArea.Width/2;
            var topLeftCornerPoint = new System.Windows.Point(0, 0);
            var popupDef = new PopupDefinition()
            {
                Append = true,      // if true new record is appended to existing (if any)
                Dockable = true,    // if true popup is dockable - if false Append is not applicable
                Position = topLeftCornerPoint,  // Position of top left corner of the popup (in pixels)
                Size = new System.Windows.Size(width, height)    // size of the popup (in pixels)
            };

            //Show the custom pop-up with the custom commands and the default pop-up commands. 
            ActiveMapView.ShowCustomPopup(popupContent, null, true, popupDef);
            return true;
        }

        private bool IsNumericFieldType(FieldType type)
        {
            switch (type)
            {
                case FieldType.Double:
                case FieldType.Integer:
                case FieldType.Single:
                case FieldType.SmallInteger:
                    return true;
                default:
                    return false;
            }
        }
    }

   
}
