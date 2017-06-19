# EntityFramework.Verify
Generic library to verify an entity model matches a database for the provided Context type.

## Introduction
After using Entity framework with legacy systems, I encountered an issue with customers making modifications to databases that created a mismatch between the entity model we developed with and the database. Changes that rendered them unusable by entity framework.
This library compares the entity model with the SQL database it's been deployed to, and generates a detailed report that outlines missing columns and tables.
I describe the problem on this [Code Review](http://codereview.stackexchange.com/questions/45831/verifying-the-entity-data-model-schema-against-a-production-database) question.

## Usage

1. The library is easy to use, once referenced you simply create an EntityFrameworkTypeRepository constrained to your DbContext:
```C#    
    // With support for Linq2SQL and multiple connection strings    
    // for entity framework DbContext
    var entityRepo = new EntityFrameworkTypeRepository<SomeDbContext>();
 ```
 
- For Linq2Sql where there is no centralised data context with a list of tables, it is necessary to reflect on the entity classes based on their attributes. The Linq2SqlTypeRepository is a helper class that provides an example of extracting entities. 

- Usage is similar to the previous Entity framework example:

 ```C#       
    // get the assembly containing the entities
    var assembly = typeof(SomeEntityClass).Assembly;
    // build a Linq2SqlTypeReporitory with that assembly
    var entityRepo = new Linq2SqlTypeRepository(assembly);           
```

2. Create a table factory based on one or more connection strings

```C#
    // build a table factory from the SQL connection strings
    var tableFactories = new MultiConnectionSqlTableRepository(connectionString1, connectionString2);
```

3. Create a model verification and generate the report object

```C#
    // generate the report
    var verification = new ModelVerification(entityRepo, tableFactories, 5);    
    var summary = verification.GenerateReport();
    // build a report with the summary
    var report = ModelVerification
        .BuildMessage(title, database, summary.Where(s => string.Equals(s.Database, database) && s.HasMissingColumns));
    Console.WriteLine(report);
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
