# EntityFramework.Verify
Generic library to verify an entity model matches a database for the provided Context type.

##Introduction
After using Entity framework with legacy systems, I encountered an issue with customers making modifications to databases that created a mismatch between the entity model we developed with and the database. Changes that rendered them unusable by entity framework.
This library compares the entity model with the SQL database it's been deployed to, and generates a detailed report that outlines missing columns and tables.
I describe the problem on this [Code Review](http://codereview.stackexchange.com/questions/45831/verifying-the-entity-data-model-schema-against-a-production-database) question.
##Usage
The library is easy to use, once referenced you simply call:
```C#
var verify = new ModelVerification<EdiContext>(connectionString, tolerance);
var results = verify.GenerateReport();
```
The results from this operation is a summary repoprt (IEnumerable of summary objects). It can be output as plain text, or presented as a lookup. I use an accordion in my web applications to present it visually, but simply log verification errors at stratup.
The report is presented as text with the missing tables listed first, followed by the tables with missing columns - 
```
DatabaseName** Has some errors: 
    Missing tables: 
        Table 1
        Table 2
    Table 3 With Missing Columns: 
        missing column 1
        missing column 2
    Table 4 With Missing Columns: 
        missing column 1
        missing column 2
        missing column 3
  ```
