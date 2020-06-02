using System;
using System.Data;
using System.IO;
using System.Reflection;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace DownloadFolderManager
{
    public class ExcelReader
    {

        public DataTable GetDataTable(string filePath, string sheetName)
        {
            string[,] result;
            ISheet sheet = GetSheetFromExcel(filePath, sheetName);
            return GetDatatable(sheet);
        }
        public string[,] GetMatrixData(string filePath, string sheetName)
        {
            string[,] result = null;
            try
            {
                ISheet sheet = GetSheetFromExcel(filePath, sheetName);

                DataTable dataTable = GetDatatable(sheet);
                int rowCount = dataTable.Rows.Count;
                int colCount = dataTable.Columns.Count;

                result = new string[rowCount, colCount];
                for (int i = 0; i < rowCount; i++)
                {
                    var row = dataTable.Rows[i];
                    for (int j = 0; j < row.ItemArray.Length; j++)
                    {
                        result[i, j] = row[j].ToString();
                    }
                }
            }
            catch
            {
                result = null;
            }
            return result;
        }

        private static DataTable GetDatatable(ISheet sheet)
        {
            var dataTable = new DataTable(sheet.SheetName);

            // write header row
            IRow headerRow = sheet.GetRow(0);
            foreach (ICell headerCell in headerRow)
            {
                dataTable.Columns.Add(headerCell.ToString());
            }

            var colCount = dataTable.Columns.Count;

            // write the rest
            int rowIndex = 0;
            foreach (IRow row in sheet)
            {
                // skip header row
                //if (rowIndex++ == 0) continue;
                rowIndex++;
                DataRow dataRow = dataTable.NewRow();
                for (int i = 0; i < colCount; i++)
                {
                    ICell cell = row.GetCell(i, MissingCellPolicy.RETURN_NULL_AND_BLANK);
                    dataRow[i] = cell?.ToString() ?? string.Empty;
                }
                //dataRow.ItemArray = row.Cells.Select(c => c.ToString()).ToArray();
                dataTable.Rows.Add(dataRow);
            }
            return dataTable;
        }

        private static ISheet GetSheetFromExcel(string filePath, string sheetName)
        {
            IWorkbook workbook;
            if (!File.Exists(filePath)) throw new FileNotFoundException($"Input file not exist or access dinied. Path ={filePath}");

            string fileExtension = Path.GetExtension(filePath);
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (string.IsNullOrEmpty(fileExtension))
                {
                    throw new ArgumentNullException("Input file not exist");
                }
                else if (fileExtension.Equals(".xls"))
                {
                    workbook = new HSSFWorkbook(stream);
                }
                else if (fileExtension.Equals(".xlsx"))
                {
                    workbook = new XSSFWorkbook(stream);
                }
                else
                {
                    throw new FileLoadException($"File is not supported extension. Extension = {fileExtension}");
                }
            }

            // todo
            ISheet sheet = null;
            try
            {
                sheet = workbook.GetSheet(sheetName);
            }
            catch (Exception ex)
            {
                sheet = null;
            }
            if (sheet == null)
            {
                throw new Exception($"Sheet with name {sheetName} not exist");
            }
            return sheet;
        }
    }
}
