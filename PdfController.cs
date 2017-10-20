using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Levo2Reports.Models;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using Rotativa;
using Newtonsoft.Json;
using Rotativa.Options;
using MlkPwgen;

namespace Levo2Reports.Controllers
{

    //  for each table use these values
    public class TableValues
    {
        /// <summary>
        ///  Values for different tables from RGI_CL
        /// </summary>
        public List<string> finalTableHeaders_list = new List<string>();
        public List<List<string>> individualTables_list = new List<List<string>>();

        public bool splitTableInSeveral_bool = false;
        public List<string> whenSplitWhichHeadersAreStatic_list = new List<string>();

        public bool summaryExists_bool = false;
        public string summaryName_str = string.Empty;
        public List<string> summaryValueHeaders_list = new List<string>();
        public bool tableNameWasDisplayed_bool = false;
    }


    public class PdfController : Controller
    {
        Report report = new Report ();
        ReportGeneratorInfo rgi_cl = new ReportGeneratorInfo();

        bool headerDisplayedOnEachPage_bool;

        

        // GET: Pdf
        public ActionResult PrintOut(string id)
        {
            TempData.Put("key", id);

            GenerateCertainReport(id);
            //PopulatingReport(id);
            return View("Index", report);
        }



        public ActionResult PrintOutWithoutDisplayedHeader(string id)
        {
            TempData.Put("key", id);
            GenerateCertainReport(id);
            //PopulatingReport(id);
            return View("IndexNoPreviewedHeader", report);
        }




        void GenerateCertainReport(string id)
        {

            //  to get the stored procedure parameters
            var connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            string sql = String.Format(@"SELECT * FROM tblLevo2Reports WHERE jsonId='{0}'", id);
            //  Get a json-object associated with an ID passed via URL
            string finalResult_str = string.Empty;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Connection.Open();

                SqlDataReader dt = cmd.ExecuteReader();
                if (dt.HasRows)
                {
                    while (dt.Read())
                    {
                        finalResult_str = dt.GetString(1);
                    }
                }
            }

            Console.WriteLine(finalResult_str);
            
            //  Check if what was sent is a report of stored procedure parameter
            //  dont forget to add parameters

            if (finalResult_str.Contains ("iri_list") && finalResult_str.Contains("rp_cl"))
            {

                //  since we can have several 
                rgi_cl = JsonConvert.DeserializeObject<ReportGeneratorInfo>(finalResult_str);

                //  save report name and its page size for rotativa for later
                TempData.Put("rotativaReportSizeType_str", rgi_cl.rp_cl.rotativaReportSizeType_str.ToString());
                TempData.Put("rotativaReportName_str", rgi_cl.rp_cl.rotativaReportName_str);
                TempData.Put("rotativaReportPageOrientation_str", rgi_cl.rp_cl.rotativaReportPageOrientation_str);
                TempData.Put("footerHotelName_str", rgi_cl.rp_cl.footerHotelName_str);



                //  there's only one header
                headerDisplayedOnEachPage_bool = rgi_cl.rp_cl.headerDisplayedOnEachPage_bool;

                Header reportHeader = new Header();

                PopulateHeaderForCertainReport(reportHeader);
                ViewBag.ReportType = "Combined report";
                report.header_cl = reportHeader;



                List<IndividualTable> tables_list = new List<IndividualTable>();

                foreach (IndividualReportInfo iri_cl in rgi_cl.iri_list)
                {
                    IndividualTable reportTable = new IndividualTable();

                    TableValues tv_cl = new TableValues();

                    //  save stuff for splitting the table
                    tv_cl.splitTableInSeveral_bool = iri_cl.shouldSplitTableIntoSeveral_bool;
                    if (tv_cl.splitTableInSeveral_bool == true)
                    {
                        tv_cl.whenSplitWhichHeadersAreStatic_list = iri_cl.staticHeaders_list;
                    }

                    //  save stuff for the summary
                    tv_cl.summaryExists_bool = iri_cl.summaryExists_bool;
                    if (tv_cl.summaryExists_bool == true)
                    {
                        if (iri_cl.summaryName_str != null)
                        {
                            tv_cl.summaryName_str = iri_cl.summaryName_str;
                        }
                        tv_cl.summaryValueHeaders_list = iri_cl.headersNotMentionedInSummaryIfExists_list;
                    }

                    PopulateTableCertainReport(reportTable, iri_cl.storedProcedureName_str, iri_cl.storedProcedureParameters_dict, tv_cl, iri_cl.tableName_str);

                    tables_list.Add(reportTable);
                    report.listOfTables_list = tables_list;


                    //  summary + split
                    //CheckingForSummaryAndSplitting();
                }
                //ViewBag.reportGot = JsonConvert.SerializeObject(report);
            }
            else if (finalResult_str.Contains("header_cl") && finalResult_str.Contains("listOfTables_list") && finalResult_str.Contains("rp_cl"))
            {
                report = JsonConvert.DeserializeObject<Report>(finalResult_str);

                TempData.Put("rotativaReportSizeType_str", report.rp_cl.rotativaReportSizeType_str.ToString());
                TempData.Put("rotativaReportName_str", report.rp_cl.rotativaReportName_str);
                TempData.Put("rotativaReportPageOrientation_str", report.rp_cl.rotativaReportPageOrientation_str);
                TempData.Put("footerHotelName_str", report.rp_cl.footerHotelName_str);



                //  there's only one header
                headerDisplayedOnEachPage_bool = report.rp_cl.headerDisplayedOnEachPage_bool;
            }
            //  check all this stuff for all Individual tables at once
            CheckingForSummaryAndSplitting();
        }



        public void PopulateHeaderForCertainReport(Header dailyProductivityReportHeader)
        {

            dailyProductivityReportHeader.reportType_str = "Daily Productivity Report";
            dailyProductivityReportHeader.hotelLogo_str = "http://somewebsite";
            dailyProductivityReportHeader.displayedOnEachPage_bool = headerDisplayedOnEachPage_bool;

            TempData.Put("headerOnEachPage", dailyProductivityReportHeader.displayedOnEachPage_bool.ToString());

            dailyProductivityReportHeader.headerValues_list = new List<string>
                { "Total GRA", "Total cleaned rooms", "Employee ID", "Employee Name", "Employee job title",
                "Employee groups", "Date"};
            dailyProductivityReportHeader.values_list = new List<string>
                { "14", "104", "kek", "Johnson, Andrea", "W Asst Mngr",
                "27 - 29 / 30 - 32", "09/29/2016"};
        }



        public void PopulateTableCertainReport(IndividualTable it, string storedProcedureName_str, Dictionary <string, string> storedProcedureParameters_dict, TableValues tv_cl, string tableName_str)
        {

            it.entityId_str = string.Empty;
            it.entityName_str = string.Empty;

            // if there's a table name
            if (tableName_str != null && tableName_str != string.Empty && tv_cl.tableNameWasDisplayed_bool == false)
            {
                it.tableName_str = tableName_str;
                tv_cl.tableNameWasDisplayed_bool = true;
            }
            else
            {
                it.tableName_str = string.Empty;
            }






            //  talking to the real database
            GetIndividualTablesAndHeadersForReport(it, storedProcedureName_str, storedProcedureParameters_dict, tv_cl);

            //  Putting in default values
           it.summaryExists_bool = tv_cl.summaryExists_bool;
            if (it.summaryExists_bool == true)
            {
                Dictionary<string, List<string>> summaryValueHeaders_dict = new Dictionary<string, List<string>>
               {
                    //  since we send names of headers that should not be in the summary, we remove them from the whole list of headers to get the ones that should be mentioned
                   {tv_cl.summaryName_str, it.tableValueHeaders_list.Except (tv_cl.summaryValueHeaders_list).ToList () }
               };
                it.summaryValueHeaders_dict = summaryValueHeaders_dict;
                it.summaryValues_list = new List<string>();
            }
           //  I would actually get several tables for table headers, values, summaries
           it.splitTableInSeveral_bool = tv_cl.splitTableInSeveral_bool;
            if (it.splitTableInSeveral_bool == true)
            {
                if (it.tableValueHeaders_list.Count > 10)
                {
                    it.maxNumberOfRows_int = 8;

                    ViewBag.cellWidthInPercentage = (100 / it.maxNumberOfRows_int).ToString () +"%";
                    it.whenSplitWhichHeadersAreStatic_list = tv_cl.whenSplitWhichHeadersAreStatic_list;
                    ViewBag.staticHeaders_list = tv_cl.whenSplitWhichHeadersAreStatic_list;
                }
                else if (it.tableValueHeaders_list.Count > 5)
                {
                    it.maxNumberOfRows_int = 5;
                    ViewBag.cellWidthInPercentage = "20%";
                    it.whenSplitWhichHeadersAreStatic_list = tv_cl.whenSplitWhichHeadersAreStatic_list;
                    ViewBag.staticHeaders_list = tv_cl.whenSplitWhichHeadersAreStatic_list;
                }
                //  if for some weird reasons we wanted to collect enough data, but ended up with few - there's no need to split the table 
                else
                {
                    it.splitTableInSeveral_bool = false;
                }
            }
        }


        void GetIndividualTablesAndHeadersForReport(IndividualTable it, string storedProcedureName_str, Dictionary<string, string> storedProcedureParameters_dict, TableValues tv_cl)
        {
            DateTime _Today = TimeZones.getCustomerTimeZones(1);

            SqlConnection conn = (new Database()).getConnection();
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = storedProcedureName_str;
            //cmd.CommandText = "TestReport";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Connection = conn;

            if (storedProcedureParameters_dict != null)
            {
                foreach (KeyValuePair<string, string> kvp in storedProcedureParameters_dict)
                {
                    cmd.Parameters.AddWithValue("@" + kvp.Key, kvp.Value);
                }
            }
            //  for unique key
            string uniqueKey_str = string.Empty;
            List<string> tableHeaders_list = new List<string>();

            using (SqlDataReader _DataRootReader = cmd.ExecuteReader())
            {
                bool moreResults_bool = true;
                while (moreResults_bool)
                {
                    for (int i = 0; i < _DataRootReader.FieldCount; i++)
                    {
                        //  get clear headers
                        if (_DataRootReader.GetName(i).Contains("_HEADER"))
                        {
                            tableHeaders_list.Add(_DataRootReader.GetName(i));
                        }
                        //  get rows as headers on case there are several. when inside this loop
                        else if (_DataRootReader.GetName(i).Contains("_RowAsHeader"))
                        {
                            while (_DataRootReader.Read())
                            {
                                tableHeaders_list.Add(_DataRootReader[_DataRootReader.GetName(i)].ToString());
                            }

                        }
                    }
                    moreResults_bool = _DataRootReader.NextResult();
                }
            }

            Console.WriteLine(tableHeaders_list);


            Dictionary<string, Dictionary<string, string>> individualTables_dict = new Dictionary<string, Dictionary<string, string>>();

            using (SqlDataReader _DataRootReader = cmd.ExecuteReader())
            {
                bool moreResults_bool = true;
                while (moreResults_bool)
                {
                    while (_DataRootReader.Read())
                    {
                        // for each row we have unique id
                        string uniqueId_str = string.Empty;
                        string headerFromRow_str = string.Empty;
                        for (int i = 0; i < _DataRootReader.FieldCount; i++)
                        {
                            //  make sure if unique ID exists, it's always first in the SELECT
                            //  for each uniqiue ID create new row
                            //  TODO: We can also decide if we want to include this id in the table, tbh
                            if (i == 0)
                            {
                                //  if this one doesn't exists yet in the dictionary, then add it
                                if (_DataRootReader.GetName(i).ToString().Contains ("UniqueKey"))
                                {
                                    //  if the first column's name is UNIQUE KEY, we need to store it
                                    uniqueId_str = _DataRootReader.GetValue(i).ToString();
                                    if (!individualTables_dict.ContainsKey(uniqueId_str))
                                    {
                                        //  if such record is not in the dictionary it, populate it with all the headers and default values
                                        Dictionary<string, string> headerValuesForIndivTable = new Dictionary<string, string>();
                                        foreach (string _th_str in tableHeaders_list)
                                        {
                                            headerValuesForIndivTable.Add(_th_str, "---");
                                        }
                                        individualTables_dict.Add(uniqueId_str, headerValuesForIndivTable);
                                    }
                                }
                            }
                            
                                //  this is where we actually add values to the headers
                                if (_DataRootReader.GetName(i).Contains("_HEADER"))
                                {
                                    if (uniqueId_str != string.Empty)
                                    {
                                        if (individualTables_dict.ContainsKey(uniqueId_str))
                                        {
                                            string value_str = _DataRootReader.GetValue(i).ToString();
                                            //  checking if it's empty or NULL
                                            if (value_str != string.Empty && value_str != null)
                                            {
                                                individualTables_dict[uniqueId_str][_DataRootReader.GetName(i).ToString()] = value_str;
                                            }
                                        }
                                    }
                                }
                                else if (_DataRootReader.GetName(i).Contains("_HeaderFromRow"))
                                {
                                    headerFromRow_str = _DataRootReader.GetValue(i).ToString();
                                }
                                else if (_DataRootReader.GetName(i).Contains("_RowAsValue"))
                                {
                                    string value_str = _DataRootReader.GetValue(i).ToString();
                                    //  checking if it's empty
                                    if (value_str != string.Empty && value_str != null)
                                    {
                                        individualTables_dict[uniqueId_str][headerFromRow_str] = value_str;
                                    }
                                }
                            
                        }
                    }
                    moreResults_bool = _DataRootReader.NextResult();
                }
            }


            //  saving it as List of INDIVIDUAL tables
            foreach (KeyValuePair<string, Dictionary<string, string>> kvp in individualTables_dict)
            {
                Dictionary<string, string> headerRow_dict = kvp.Value;
                List<string> oneRow_list = new List<string>();
                foreach (KeyValuePair<string, string> hr in headerRow_dict)
                {
                    oneRow_list.Add(hr.Value);
                }
                tv_cl.individualTables_list.Add(oneRow_list);
            }

            //  cleaning the name
            foreach (string header_str in tableHeaders_list)
            {
                if (header_str.Contains("_HEADER"))
                {
                    if (!header_str.Contains("UniqueKey"))
                    {
                        tv_cl.finalTableHeaders_list.Add(header_str.Replace("_HEADER", ""));
                    }
                    else
                    {
                        tv_cl.finalTableHeaders_list.Add(header_str.Replace("UniqueKey_HEADER_", ""));
                    }
                }
                else
                {
                    tv_cl.finalTableHeaders_list.Add(header_str);
                }
            }


            it.tableValueHeaders_list = tv_cl.finalTableHeaders_list;

            it.tableValues_list = tv_cl.individualTables_list;
        }


        



        void CheckingForSummaryAndSplitting() {
            
            //  check for summary values
            List<IndividualTable> tables_list = report.listOfTables_list;

            foreach (IndividualTable it in report.listOfTables_list)
            {
                if (it.summaryExists_bool == true)
                {
                    //  get all summary headers from the dictionary
                    List<string> tempSummaryValueHeaders_list =
                        it.summaryValueHeaders_dict.SelectMany(d => d.Value).ToList();

                    //  since summary headers might not be in the exact order, we need to sort them out.
                    //  we get a list of real headers' indexes they correspond to.
                    Dictionary<int, string> summaryHeadersToRealHeaders_dict = new Dictionary<int, string>();

                    foreach (string _summaryHeader_str in tempSummaryValueHeaders_list)
                    {
                        summaryHeadersToRealHeaders_dict.Add(it.tableValueHeaders_list.IndexOf (_summaryHeader_str), _summaryHeader_str);
                    }
                    // sort 'em
                    var l = summaryHeadersToRealHeaders_dict.OrderBy(key => key.Key);
                    Dictionary<int, string> sortedSummaryHeaders_dict = l.ToDictionary((keyItem) => keyItem.Key, (valueItem) => valueItem.Value);

                    Dictionary<int, string> finalHeadersToRealHeaders_dict = new Dictionary<int, string>();
                    List<string> notMentionedSummaryHeadersInBetween_list = new List<string>();

                    //  go through first element in list to last
                    int lastIndexInSortedHeaders_int = sortedSummaryHeaders_dict.Last().Key;
                    int firstIndexInSortedHeaders_int = sortedSummaryHeaders_dict.First().Key;

                    //  check if there're some elements that shoulnt be mentioned inside the summary
                    for (int i = firstIndexInSortedHeaders_int; i <= lastIndexInSortedHeaders_int; i++)
                    {
                        //  if it's not there
                        if (!sortedSummaryHeaders_dict.ContainsKey(i))
                        {
                            string notMentionedHeader_str = it.tableValueHeaders_list[i];
                            finalHeadersToRealHeaders_dict.Add(i, notMentionedHeader_str);
                            notMentionedSummaryHeadersInBetween_list.Add(notMentionedHeader_str);
                        }
                        else
                        {
                            finalHeadersToRealHeaders_dict.Add(i, it.tableValueHeaders_list[i]);
                        }
                    }


                    List<string> summaryValueHeaders_list = new List<string>();
                    foreach (KeyValuePair <int, string> kvp in finalHeadersToRealHeaders_dict)
                    {
                        summaryValueHeaders_list.Add(kvp.Value);
                    }
                    //  dont forget to update the summary list inside a table
                    it.summaryValueHeaders_dict [it.summaryValueHeaders_dict.Keys.ElementAt (0)] = summaryValueHeaders_list;

                    //  to know the position of the very first element, so that it wouldn't just start where it shouldn't
                    string firstHeaderOfSummary_str = summaryValueHeaders_list[0];
                    int whereWeShouldPositionFirstSummaryElement_int =
                        it.tableValueHeaders_list.IndexOf(firstHeaderOfSummary_str);
                    //  since the summary headers might end earlier than the last table header
                    int lastSummaryHeader_int = whereWeShouldPositionFirstSummaryElement_int + summaryValueHeaders_list.Count;

                    List<string> summaryValues_list = new List<string>();

                    //  since it will work ONLY for numeric types, just get on with it.
                    for (int i = whereWeShouldPositionFirstSummaryElement_int; i < lastSummaryHeader_int; i++)
                    {
                        string finalValue_str = string.Empty;
                        //  if it's some header that is in between two headers in the summary and this header shoudnt be mentioned
                        //  count the summary only for the needed headers
                        if (!notMentionedSummaryHeadersInBetween_list.Contains(it.tableValueHeaders_list[i]))
                        {
                            double finalDoubleValue_dbl = 0.00d;
                            for (int j = 0; j < it.tableValues_list.Count; j++)
                            {
                                double currentDoubleValue_dbl = 0.00d;
                                string value_str = it.tableValues_list[j][i];
                                //finalValue_str += value_str;

                                bool isNumeric_bool = double.TryParse(value_str,
                                    NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture,
                                    out currentDoubleValue_dbl);
                                if (isNumeric_bool == true)
                                {
                                    finalDoubleValue_dbl += currentDoubleValue_dbl;
                                }
                                else
                                {
                                    finalValue_str += value_str;
                                }

                            }
                            finalValue_str = finalDoubleValue_dbl.ToString();
                        }
                        summaryValues_list.Add(finalValue_str);
                    }


                    it.summaryValues_list = summaryValues_list;
                }
            }


            //  checking if table needs to be split
            List<IndividualTable> finalListOfTables_list = new List<IndividualTable>();

            foreach (IndividualTable it in report.listOfTables_list)
            {
                if (it.splitTableInSeveral_bool == false)
                {
                    //  if not, then add a table to the final list
                    finalListOfTables_list.Add(it);
                }
                else
                {
                    //  splitting shit

                    //  we need to store the original table since we will always refer to it.
                    IndividualTable originalTable_class = it;
                    IndividualTable finalResultTable_class = new IndividualTable();


                    //  the actual function for splitting up the array
                    List<int> indexesOfStaticHeaders_list = new List<int>();
                    //  get indexes of static headers
                    foreach (string _staticIndex_str in it.whenSplitWhichHeadersAreStatic_list)
                    {
                        indexesOfStaticHeaders_list.Add(it.tableValueHeaders_list.IndexOf(_staticIndex_str));
                    }

                    //  since the number of static columns is predetermined, we need to know how many columns we can add to a table to the static ones
                    int numberOfColumnsToTakeStartingFromSecondTable_int =
                        it.maxNumberOfRows_int - it.whenSplitWhichHeadersAreStatic_list.Count;

                    //  how many full tables we will get (take into consideration that it will depend on a number of dynamic columns added to the static ones)
                    int howManyFullTablesWillGet_int = (int)it.tableValueHeaders_list.Count / (it.maxNumberOfRows_int - indexesOfStaticHeaders_list.Count);

                    //  get the very first table
                    SplittingCurrentTableIntoSeveralFirstTime(originalTable_class, finalResultTable_class, it.maxNumberOfRows_int);
                    //  add it to the resulting collection of tables
                    finalListOfTables_list.Add(finalResultTable_class);

                    //  first start index later
                    int endIndex_int = it.maxNumberOfRows_int;
                    for (int i = 1; i < howManyFullTablesWillGet_int; i++)
                    {
                        IndividualTable _originalTable_class = it;
                        IndividualTable _finalResultTable_class = new IndividualTable();
                        //  startIndex = always the previous endIndex
                        int startIndex_int = endIndex_int;
                        endIndex_int += numberOfColumnsToTakeStartingFromSecondTable_int;
                        //  get the full dynamic tables

                        SplittingCurrentTableIntoSeveralWhenFullTables(_originalTable_class, _finalResultTable_class, startIndex_int, endIndex_int, indexesOfStaticHeaders_list);
                        //  add it to the resulting collection of tables
                        finalListOfTables_list.Add(_finalResultTable_class);
                    }
                    //  for the remaining ones
                    //  if there are any left
                    if (endIndex_int < it.tableValueHeaders_list.Count)
                    {
                        IndividualTable __originalTable_class = it;
                        IndividualTable __finalResultTable_class = new IndividualTable();
                        SplittingCurrentTableIntoSeveralWhenFullTables(__originalTable_class, __finalResultTable_class, endIndex_int,
                            it.tableValueHeaders_list.Count, indexesOfStaticHeaders_list);
                        finalListOfTables_list.Add(__finalResultTable_class);
                    }
                    Console.WriteLine(finalResultTable_class);

                }
            }

            report.listOfTables_list = finalListOfTables_list;
        }




        void SplittingCurrentTableIntoSeveralFirstTime(IndividualTable _originalTable, IndividualTable _finalResult, int endIndex) {

            //  get info from the original table
            _finalResult.entityId_str = _originalTable.entityId_str;
            _finalResult.entityName_str = _originalTable.entityName_str;
            _finalResult.tableName_str = _originalTable.tableName_str;
            //  take a chunk of table headears
            List<string> partOfTableHeaders_list = new List<string>();
            for (int i = 0; i < endIndex; i++)
            {
                string header_str = _originalTable.tableValueHeaders_list[i];
                partOfTableHeaders_list.Add(header_str);
            }
            _finalResult.tableValueHeaders_list = partOfTableHeaders_list;


            //  take a chunk of table rows
            List<List<string>> partsOfRows_list = new List<List<string>>();
            //  for each row in the original table, we take a certain number of columns
            foreach (List<string> tableRow in _originalTable.tableValues_list)
            {
                List<string> partOfRow_list = new List<string>();
                for (int i = 0; i < endIndex; i++)
                {
                    partOfRow_list.Add(tableRow[i]);
                }
                partsOfRows_list.Add(partOfRow_list);
            }

            _finalResult.tableValues_list = partsOfRows_list;

            SplitSummary(_originalTable, _finalResult);
        }


        void SplittingCurrentTableIntoSeveralWhenFullTables(IndividualTable _originalTable, IndividualTable _finalResult,
            int startIndex, int endIndex, List<int> indexesOfStaticElements_list) {

            _finalResult.entityId_str = _originalTable.entityId_str;
            _finalResult.entityName_str = _originalTable.entityName_str;
            _finalResult.tableName_str = _originalTable.tableName_str;
            //  take a chunk of table headears
            List<string> tableHeaders_list = new List<string>();
            List<List<string>> tableValues_list = new List<List<string>>();


            //  take static headers first
            foreach (int indexOfStaticHeader_int in indexesOfStaticElements_list)
            {
                tableHeaders_list.Add(_originalTable.tableValueHeaders_list[indexOfStaticHeader_int]);
            }

            //  take static elements first
            for (int i = 0; i < _originalTable.tableValues_list.Count; i++)
            {
                List<string> partOfRow_list = new List<string>();
                foreach (int indexOfStaticHeader_int in indexesOfStaticElements_list)
                {
                    string header_str = _originalTable.tableValues_list[i][indexOfStaticHeader_int];
                    partOfRow_list.Add(header_str);
                }

                tableValues_list.Add(partOfRow_list);
            }

            //  take the other ones - both headers and values

            for (int i = startIndex; i < endIndex; i++)
            {
                if (i < _originalTable.tableValueHeaders_list.Count)
                {
                    tableHeaders_list.Add(_originalTable.tableValueHeaders_list[i]);
                    for (int j = 0; j < _originalTable.tableValues_list.Count; j++)
                    {
                        tableValues_list[j].Add(_originalTable.tableValues_list[j][i]);
                    }
                }
            }

            _finalResult.tableValueHeaders_list = tableHeaders_list;
            _finalResult.tableValues_list = tableValues_list;

            SplitSummary(_originalTable, _finalResult);
        }


        void SplitSummary(IndividualTable _originalTable, IndividualTable _finalResult)
        {
            //  Save the summary stuff somewhere
            List<string> summaryHeaders_list = new List<string>();
            List<string> summaryValues_list = new List<string>();

            if (_originalTable.summaryExists_bool == true)
            {
                //  grab the summary headers and values from the original table
                summaryHeaders_list = _originalTable.summaryValueHeaders_dict.SelectMany(d => d.Value).ToList();
                summaryValues_list = _originalTable.summaryValues_list;

                _finalResult.summaryExists_bool = true;

                //  get needed summary headers and values
                List<string> localSummaryHeaders_list = new List<string>();
                List<string> localSummaryValues_list = new List<string>();
                foreach (string _valueHeader_str in _finalResult.tableValueHeaders_list)
                {
                    if (summaryHeaders_list.Contains(_valueHeader_str))
                    {
                        localSummaryHeaders_list.Add(_valueHeader_str);
                        int indexOfOriginalSummaryHeader_int = summaryHeaders_list.IndexOf(_valueHeader_str);
                        localSummaryValues_list.Add(summaryValues_list[indexOfOriginalSummaryHeader_int]);
                    }
                }
                //  and save them
                Dictionary<string, List<string>> summaryValueHeaders_dict = new Dictionary<string, List<string>>
                {
                    {string.Empty, localSummaryHeaders_list}
                };
                _finalResult.summaryValueHeaders_dict = summaryValueHeaders_dict;
                _finalResult.summaryValues_list = localSummaryValues_list;
            }
        }


        
        public ActionResult Header(string id) {

            var connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            string sql = String.Format(@"SELECT * FROM tblLevo2Reports WHERE jsonId='{0}'", id);
            //  Get a json-object associated with an ID passed via URL
            string finalResult_str = string.Empty;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Connection.Open();

                SqlDataReader dt = cmd.ExecuteReader();
                if (dt.HasRows)
                {
                    while (dt.Read())
                    {
                        finalResult_str = dt.GetString(1);
                    }
                }
            }

            rgi_cl = Newtonsoft.Json.JsonConvert.DeserializeObject<ReportGeneratorInfo>(finalResult_str);

            //Console.WriteLine(rgi_cl);

            Header certainReportHeader = new Header();

            PopulateHeaderForCertainReport(certainReportHeader);
            report.header_cl = certainReportHeader;
            Console.WriteLine(report);
            return View("PartialHeader", report);
        }


        
        public ActionResult GeneratePdf()
        {
            var value = TempData.Get<string>("key");
            string actionLink_str = string.Empty;
            Console.WriteLine(actionLink_str);

            //  play with the numbers if you need to make a header lower/higher - --header-spacing 10 -T 70mm
            //  don't forget to change the urlaction 
            var headerOnEachPage = TempData.Get<string>("headerOnEachPage");
            string customSwitches = string.Empty;

            // check if http or https
            string absoluteUrl_str = Request.Url.AbsoluteUri;
            string transferProtocol_str = absoluteUrl_str.Contains ("https") ? "https" : "http";


            //  might consider to get rid of this one - --disable-smart-shrinking
            if (headerOnEachPage == "True")
            {
                //  to move a header - --header-spacing 10 -T 70mm. !!! --header-line
                customSwitches = string.Format("--print-media-type --disable-smart-shrinking --load-error-handling ignore --header-spacing 10 -T 70mm --header-html {0} --footer-line --footer-left \"{1}\" --footer-right \"Levo2\" --footer-center \"Page: [page]/[toPage]\" ",
                  Url.Action("Header", "Pdf", new { id = value }, transferProtocol_str), TempData.Get<string>("footerHotelName_str"));
                actionLink_str = "PrintOutWithoutDisplayedHeader/" + value;
                //actionLink_str += "_True";
            }
            else
            {
                customSwitches = string.Format("--print-media-type --disable-smart-shrinking --load-error-handling ignore --footer-line --footer-left \"{0}\" --footer-right \"Levo2\" --footer-center \"Page: [page]/[toPage]\"",
                    TempData.Get<string>("footerHotelName_str"));
                actionLink_str = "PrintOut/" + value;
                //actionLink_str += "_False";
            }

            string rotativaReportSizeType_str = TempData.Get<string>("rotativaReportSizeType_str");
            Size reportSize_rttvSize = (Size)Enum.Parse(typeof(Size), rotativaReportSizeType_str);

            string rotativaReportPageOrientation_str = TempData.Get<string>("rotativaReportPageOrientation_str");
            Orientation reportOrientation_rttvOrnt = (Orientation)Enum.Parse(typeof(Orientation), rotativaReportPageOrientation_str);

            string rotativaReportName_str = TempData.Get<string>("rotativaReportName_str");

            Console.WriteLine(customSwitches);
            return new ActionAsPdf(actionLink_str)
            {
                FileName = rotativaReportName_str,
                PageOrientation = reportOrientation_rttvOrnt,
                PageSize = reportSize_rttvSize,
                CustomSwitches = customSwitches
            };
        }
    }
}