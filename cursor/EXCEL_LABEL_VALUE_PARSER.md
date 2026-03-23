🔧 Cursor Delta Prompt – Excel Label/Value Parser (TIME Work Order Template)



Role:

You are working on a .NET 10 backend for CephasOps-style apps. Implement robust Excel parsing for TIME work-order templates where data is stored as label/value pairs rather than a normal table.



1\. Goal



Implement a reusable Excel parser that:



Accepts an Excel file (stream or path).



Reads a specific worksheet (default: first sheet).



For each row:



Treat Column B as the label.



Find the first non-empty cell to the right (from Column C onward) as the value.



Returns a clean, strongly typed representation (or JSON) of these label/value pairs so the rest of the app can easily consume the information.



This is for TIME work orders like the M1800592 template (Activation Form).



2\. Excel Layout Rules (Must Follow)



Assume the incoming Excel matches this structure:



Labels are in Column B:



Examples:



"TASK:"



"REFERENCE NO:"



"CUSTOMER NAME"



"CONTACT NO."



"SERVICE ADDRESS"



"SERVICE ID"



"PACKAGE"



"BANDWIDTH"



"USERNAME"



"PASSWORD"



"ONU PASSWORD"



"REMARKS"



The actual value for each label is the first non-empty cell in the same row to the right of Column B:



Normally in Column D, but you must not hard-code D.



Always search columns C → last used column and pick the first non-empty cell as the value.



Some rows are section headers, not fields, e.g.:



"CUSTOMER DETAILS"



"ACTIVATION DETAILS"



"FIBER INTERNET"



"VOIP"

These typically have no value in the row. They are used only for grouping.



3\. Parsing Logic (Core Algorithm)



Implement an internal service with logic equivalent to:



Open the workbook from a Stream (preferred) or file path.



Select the first worksheet (or named sheet if configured later).



Iterate over all used rows:



Read label = cell at column B (zero-based index 1).



If label is null/empty → skip the row.



Trim the label string.



For each row with a non-empty label:



Search from Column C (index 2) to the last used column in that row:



Find the first cell with a non-empty value.



If found:



value = that cell’s text.



Add to result dictionary:



result\[label] = value.



If no non-empty value is found:



Treat this row as a section header, not a field.



You must not rely on specific row numbers or specific value columns; the rule is purely:



“Label in column B, value is first non-empty cell to the right on the same row.”



4\. Optional: Section Grouping (Phase 2)



Design the code so that in future we can group by sections:



If a row has a label but no value in any column to the right:



Treat label as a section name.



Store it as currentSection.



For all subsequent rows with both label and value until the next section:



Store them under that currentSection.



Target JSON shape for that mode (for later):



{

&nbsp; "CustomerDetails": {

&nbsp;   "CUSTOMER NAME": "...",

&nbsp;   "CONTACT PERSON": "...",

&nbsp;   "CONTACT NO.": "...",

&nbsp;   "SERVICE ADDRESS": "..."

&nbsp; },

&nbsp; "ActivationDetails": {

&nbsp;   "SERVICE ID": "...",

&nbsp;   "PACKAGE": "...",

&nbsp;   "BANDWIDTH": "...",

&nbsp;   "USERNAME": "...",

&nbsp;   "PASSWORD": "...",

&nbsp;   "ONU PASSWORD": "..."

&nbsp; }

}





For now, implement the flat key/value mode first but keep the design extensible for this grouped mode.



5\. Implementation Requirements



Language / Runtime: .NET 10 (C#).



Library Choice for Excel:



Prefer a free or low-friction library:



MiniExcel or



NPOI



Do not require Microsoft Office / Interop / COM.



Wrap library calls behind an interface so we can switch later if needed.



Create a service interface, for example:



IExcelLabelValueParser



Method 1: Task<IDictionary<string, string>> ParseAsync(Stream excelStream, string? sheetName = null);



(Optional future) Method 2: Task<ExcelSectionsResult> ParseWithSectionsAsync(...) for grouped mode.



The service should:



Handle different row counts robustly.



Trim label and value strings.



Ignore completely empty rows.



Be tolerant of extra formatting or merged cells (use the library’s cell value API, not raw XML).



6\. API Integration



Add an API endpoint like:



POST /api/import/excel-workorder



Accepts file upload (multipart/form-data).



Passes the file stream to IExcelLabelValueParser.ParseAsync.



Returns the parsed key/value pairs as JSON in the response.



Later, we can:



Map the result into a domain model (WorkOrder, ActivationForm, etc.).



Save both the raw Excel and the parsed result.



7\. Testing \& Validation



Create unit tests for the parser with sample Excel files that mimic the TIME work-order template:



Ensure that:



"CUSTOMER NAME" maps to the expected customer name.



"SERVICE ADDRESS" maps to the full address line.



"CONTACT NO." maps correctly.



"SERVICE ID", "PACKAGE", "BANDWIDTH", "USERNAME", "PASSWORD", "ONU PASSWORD", "REMARKS" all parse correctly.



Add a test where:



A row label in Column B has no value to the right → ensure it is ignored or treated as a section header and does not appear as a normal field key/value.



8\. Output \& Refactoring



Ensure the parser is:



Reusable for other similar templates (not hard-coded to a specific workbook name).



Easy to plug into other import flows (e.g. TNB bill Excel, serial number sheets, etc.) by reusing the same “label=col B, value=first right cell” rule or by allowing configuration for which column is the label.



Update or create documentation (e.g. docs/EXCEL\_IMPORT/EXCEL\_LABEL\_VALUE\_PARSER.md) describing:



The parsing rules.



The expected JSON shape.



Example usage in the API.



Apply this entire specification and implement the necessary service, wiring, and tests. Do not change unrelated parts of the system.

