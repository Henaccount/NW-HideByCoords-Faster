//------------------------------------------------------------------
// NavisWorks Sample code
//------------------------------------------------------------------

// (C) Copyright 2009 by Autodesk Inc.

// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.

// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
//------------------------------------------------------------------
//
// This is the 'HideByCoords' Sample.
//
//------------------------------------------------------------------
#region ClipBox

//Add two new namespaces
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Clash;
using Autodesk.Navisworks.Api.DocumentParts;
using Autodesk.Navisworks.Api.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;


namespace ClipBox
{
    [PluginAttribute("ClipBox.AClipBox",                   //Plugin name
                     "ADSK",                                       //4 character Developer ID or GUID
                     ToolTip = "ClipBox.AClipBox tool tip",//The tooltip for the item in the ribbon
                     DisplayName = "ClipBox")]          //Display name for the Plugin in the Ribbon

    public class ClipBox : AddInPlugin                 //Derives from AddInPlugin
    {

        public override int Execute(params string[] parameters)
        {
            string documentspath = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string fileoutpath = documentspath + "\\" + "CoordinationModel.nwd";

            Document doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
            double scalefactor = UnitConversion.ScaleFactor(Units.Meters, doc.Units);

            DocumentSelectionSets myselectionsets = doc.SelectionSets;
            ModelItemCollection clipbox = new ModelItemCollection();

            foreach (SavedItem childItem in ((GroupItem)myselectionsets.RootItem).Children)
            {
                if (childItem.DisplayName == "hidden")
                {
                    SelectionSet oSet = childItem as SelectionSet;
                    doc.Models.SetHidden(oSet.ExplicitModelItems, false);
                    myselectionsets.Remove((GroupItem)myselectionsets.RootItem, oSet);
                }
                else if(childItem.DisplayName == "clipbox")
                {
                    SelectionSet oSet = childItem as SelectionSet;
                    clipbox.AddRange(oSet.ExplicitModelItems);

                }
            }

            Point3D selbbmin, selbbmax;

            if (parameters.Length == 0)
            {               
                ModelItemCollection mysel = doc.CurrentSelection.SelectedItems;
                selbbmin = mysel.BoundingBox().Min;
                selbbmax = mysel.BoundingBox().Max;
            }
            else
            {
                fileoutpath = @parameters[0];
                selbbmin = new Point3D(Convert.ToDouble(parameters[1].Replace("neg", "-")) * scalefactor, Convert.ToDouble(parameters[2].Replace("neg", "-")) * scalefactor, Convert.ToDouble(parameters[3].Replace("neg", "-")) * scalefactor);
                selbbmax = new Point3D(Convert.ToDouble(parameters[4].Replace("neg", "-")) * scalefactor, Convert.ToDouble(parameters[5].Replace("neg", "-")) * scalefactor, Convert.ToDouble(parameters[6].Replace("neg", "-")) * scalefactor);
            }

            if (selbbmin.IsOrigin && selbbmax.IsOrigin) {
                doc.SaveFile(fileoutpath);
                return 0;
            }

            string logfile = documentspath + "\\" + Path.GetFileName(fileoutpath) + ".txt";
            List<string> log = new List<string>();
            //log.Add("scalefactor###" + scalefactor);
            //selbbmin = new Point3D(-7.000, 14.000, 17.000);
            //selbbmax = new Point3D(-5.000, 16.000, 19.000);

            Point3D mid = selbbmin.ToVector3D().Add(selbbmax).ToVector3D().Divide(2).ToPoint3D();
            Vector3D diff = new Point3D(1, 1, 1).ToVector3D();
            double deltaval = 0.01;
            Vector3D delta = new Point3D(deltaval, deltaval, deltaval).ToVector3D();
            double clearance = 0;
            while (diff.X > 0 && diff.Y > 0 && diff.Z > 0)
            {
                clearance += deltaval;
                selbbmax = selbbmax.Subtract(delta);
                selbbmin = selbbmin.Add(delta);
                diff = selbbmax.ToVector3D().Subtract(selbbmin.ToVector3D());
            }

            Transform3D EndTransform = new Transform3D();
            Transform3DComponents Transform3DComponent = EndTransform.Factor();
            Transform3DComponent.Scale = selbbmax.ToVector3D().Subtract(selbbmin.ToVector3D());
            Rotation3D ScaleOrientation = new Rotation3D(new UnitVector3D(0, 0, 1), 0);
            Transform3DComponent.Rotation = ScaleOrientation;
            Transform3DComponent.Translation = mid.ToVector3D();
            EndTransform = Transform3DComponent.Combine();

            doc.Models.OverridePermanentTransform(clipbox, EndTransform, true);

            try
            {
                DocumentClash documentClash = doc.GetClash();
                DocumentClashTests myClashTests = documentClash.TestsData;
                ClashTest myClashTestorg = myClashTests.Tests[0] as ClashTest;
                ClashTest myClashTest = myClashTestorg.CreateCopy() as ClashTest;
                myClashTest.TestType = ClashTestType.Clearance;
                myClashTest.Tolerance = Math.Pow((3 * Math.Pow(clearance, 2)), 0.5);
                myClashTest.Status = ClashTestStatus.New;
                myClashTests.TestsEditTestFromCopy(myClashTestorg, myClashTest);
                myClashTests.TestsRunTest(myClashTestorg);
                myClashTest = myClashTests.Tests[0] as ClashTest;
                ModelItemCollection myClashingElements = new ModelItemCollection();
                for (var i = 0; i < myClashTest.Children.Count; i++)
                {
                    ClashResult myClash = myClashTest.Children[i] as ClashResult;
                    myClashingElements.AddRange(myClash.Selection2);
                }

                myClashingElements.Invert(doc);
                doc.Models.SetHidden(myClashingElements, true);

                SelectionSet allhidden = new SelectionSet(myClashingElements);
                allhidden.DisplayName = "hidden";
                myselectionsets.AddCopy(allhidden);

                myClashTests.TestsClearResults(myClashTest);
                //clipbox.AddRange((doc.SelectionSets.RootItem.Children[0] as SelectionSet).GetSelectedItems());
                doc.Models.ResetPermanentTransform(clipbox);
                doc.SaveFile(fileoutpath);

            }
            catch (Exception e) { log.Add(e.Message + "###" + e.StackTrace); }
            finally { File.WriteAllText(logfile, string.Join("\r\n", log)); }

            return 0;


        }


    }
}
#endregion