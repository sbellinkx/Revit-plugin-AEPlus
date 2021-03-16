﻿using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using OfficeOpenXml;
using System.IO;
using System.Globalization;
using CsvHelper;
using Excel = Microsoft.Office.Interop.Excel;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace E_60Toevoegen
{
    [TransactionAttribute(TransactionMode.ReadOnly)]
    public class CSV_E60Toevoegen : IExternalCommand
    {
        string xlSheetName;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            System.IO.Directory.CreateDirectory(@"c:\\temp\\E_60");
            Result r = Result.Succeeded;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            string MapPath = @"c:\\temp\\E_60\";

            string emptyFirstCellDocument = "";

            // Get UIDocument
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            // Get Document
            Document doc = uidoc.Document;

            FilteredElementCollector col = new FilteredElementCollector(doc)
            .OfClass(typeof(ViewSchedule));

            // Export Options on how the .txt file will look. Text "" is gone
            // FieldDelimiter is TAB replaced with ,            
            ViewScheduleExportOptions opt = new ViewScheduleExportOptions()
            {
                TextQualifier = ExportTextQualifier.None,
                FieldDelimiter = ",",
            };

            // Formating for writing to xlsx
            var format = new ExcelTextFormat()
            {
                Culture = CultureInfo.InvariantCulture,
                // Escape character for values containing the Delimiter
                // ex: "A,Name",1 --> two cells, not three
                TextQualifier = '"'
                // Other properties
                // EOL, DataTypes, Encoding, SkipLinesBeginning/End
            };

            // Creates new excelpackage this 
            using (ExcelPackage excelEngine = new ExcelPackage())
            {
                using (ExcelPackage xlPackage = new ExcelPackage())
                {
                    ExcelWorksheet wbUitzondering = xlPackage.Workbook.Worksheets.Add("Uitzondering");
                    foreach (ViewSchedule vs in col)
                    {
                        // Searches for schedules containing AE E60 M52 en M57 ventilatierooster
                        // dit zijn de schedules waarbij het met aantallen is.
                        if (vs.Name.Contains("AE_E60")
                            || vs.Name.Contains("AE_M52")
                            || vs.Name.Contains("AE_M57_ Ventilatieroosters")
                            || vs.Name.Contains("AE_M57_Toestellen VENT")
                            || vs.Name.Contains("AE_M50_Toestellen HVAC coll"))
                        {
                            if (vs.Name.Length > 30)
                            {
                                xlSheetName = vs.Name.Substring(0, 30);
                            }
                            else
                            {
                                xlSheetName = vs.Name;
                            }

                            //create a WorkSheet
                            ExcelWorksheet ws1 = excelEngine.Workbook.Worksheets.Add(xlSheetName);
                            // Export c:\\temp --> Will be save as
                            string filename = Environment.UserName + vs.Name;
                            vs.Export(MapPath, filename + ".csv", opt);

                            string normalDocument = "";
                            string StringPathFile = MapPath + filename + ".csv";
                            string[] lines = File.ReadAllLines(StringPathFile);
                            char[] delimitChars = { ',' };
                            int i = 1;

                            foreach (string line in lines)
                            {
                                // Gets first 2 row of each Schedule, name and properties.
                                if (i < 3)
                                {
                                    emptyFirstCellDocument += line + Environment.NewLine;
                                    i++;
                                }

                                // Looks for first value if this is null or blank "" 
                                if (line.Split(delimitChars)[0] == ""
                                    || line.Split(delimitChars)[0] == null
                                    || line.Split(delimitChars)[0].Contains(','))
                                {
                                    emptyFirstCellDocument += line + Environment.NewLine;
                                }
                                else
                                {
                                    normalDocument += line + Environment.NewLine;
                                }
                            }
                            // Gets spacing for each schedule.
                            emptyFirstCellDocument += Environment.NewLine + Environment.NewLine;

                            File.WriteAllText(MapPath + filename + ".csv", normalDocument.ToString());
                            FileInfo file = new FileInfo(MapPath + filename + ".csv");
                            // Adds Worksheet as first in the row 
                            ws1.Workbook.Worksheets.MoveToStart(xlSheetName);
                            ws1.Cells["A1"].LoadFromText(file, format);

                            // the path of the file
                            string filePath = "C:\\temp\\E_60\\Excel_E_60.xlsx";

                            // Write the file to the disk
                            FileInfo fi = new FileInfo(filePath);
                            excelEngine.SaveAs(fi);

                            File.WriteAllText(MapPath + "Uitzonderingen.csv", emptyFirstCellDocument);
                            FileInfo fileUitzondering = new FileInfo(MapPath + "Uitzonderingen.csv");
                            wbUitzondering.Cells["A1"].LoadFromText(fileUitzondering, format);

                            string stringPath = "C:\\temp\\E_60\\Uitzonderingen.xlsx";

                            // Write the file to the disk
                            FileInfo fileInfoUitzondering = new FileInfo(stringPath);
                            xlPackage.SaveAs(fileInfoUitzondering);

                            File.Delete(MapPath + filename + ".csv");
                            File.Delete(MapPath + "Uitzonderingen.csv");
                        }
                    }
                    xlPackage.Dispose();
                }
                excelEngine.Dispose();
            }
            return r;
        }
    }
}
