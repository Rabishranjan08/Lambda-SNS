using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace ManualEmail
{
    class CSVToDataTable
    {
        public DataTable ConvertCsvToDataTable(string data, int noc)
        {


            DataTable tbl = new DataTable();

            string[] lines = new string[noc];

            var strArray = data.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            var columns = strArray[0].Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            tbl.Columns.Add(new DataColumn("LoanId", typeof(Int64)));
            tbl.Columns.Add(new DataColumn("EmailTemplateID", typeof(Int32)));
            tbl.Columns.Add(new DataColumn("ReasonForForceRequest", typeof(string)));
            tbl.Columns.Add(new DataColumn("RequestedBy", typeof(string)));
            tbl.Columns.Add(new DataColumn("Jira", typeof(Int32)));
            tbl.Columns.Add(new DataColumn("ApprovedBy", typeof(string)));
            tbl.Columns.Add(new DataColumn("ProcessedFlag", typeof(char)));
            tbl.Columns.Add(new DataColumn("ProcessedDate", typeof(DateTime)));
            tbl.Columns.Add(new DataColumn("Text1", typeof(string)));
            tbl.Columns.Add(new DataColumn("Text2", typeof(string)));
            tbl.Columns.Add(new DataColumn("Text3", typeof(string)));
            tbl.Columns.Add(new DataColumn("Text4", typeof(string)));
            tbl.Columns.Add(new DataColumn("Text5", typeof(string)));
            tbl.Columns.Add(new DataColumn("Text6", typeof(string)));
            tbl.Columns.Add(new DataColumn("Text7", typeof(string)));
            tbl.Columns.Add(new DataColumn("Text8", typeof(string)));
            tbl.Columns.Add(new DataColumn("Text9", typeof(string)));
            tbl.Columns.Add(new DataColumn("Text10", typeof(string)));
            tbl.Columns.Add(new DataColumn("Date1", typeof(DateTime)));
            tbl.Columns.Add(new DataColumn("Date2", typeof(DateTime)));
            tbl.Columns.Add(new DataColumn("Date3", typeof(DateTime)));
            tbl.Columns.Add(new DataColumn("Date4", typeof(DateTime)));
            tbl.Columns.Add(new DataColumn("Date5", typeof(DateTime)));
            int i = 0, k = 0;
            foreach (var line in strArray)
            {

                var cols = line.Split(',');

                foreach (string w in cols)
                {
                    lines[k] = w;

                    k++;
                }
                k = 0;
                if (i > 0)
                {
                    DataRow dr = tbl.NewRow();
                    for (int cIndex = 0; cIndex < noc; cIndex++)
                    {
                        dr[cIndex] = lines[cIndex];
                    }

                    tbl.Rows.Add(dr);
                }
                i++;
            }

            return tbl;

        }
    }
}
