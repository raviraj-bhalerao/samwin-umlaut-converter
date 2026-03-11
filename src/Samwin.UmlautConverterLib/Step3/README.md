# Step 3 – SQL Query Generation

Step 3 implements utilities to generate SQL queries for names that might contain umlauts.  
The goal is to produce safe and efficient queries using either parameterized statements or plain SQL strings, integrating seamlessly with the Step 2 variation generators.

---

# Features

- Generates **parameterized SQL queries** for safe database access
- Generates **plain SQL queries** for ad-hoc or debugging scenarios
- Supports combining multiple names into a single query or generating one query per name
- Uses Step 2 variation generators to produce all possible umlaut permutations
- Safe handling of SQL values with proper escaping

---

# Converter Implementations

This folder contains multiple implementations of Step 3 SQL query generators:

- **ParameterizedSqlGenerator** – Produces parameterized SQL queries with a dictionary of parameters  
- **PlainSqlGenerator** – Produces SQL strings with values embedded, formatted safely  
- **StandardSqlValueFormatter** – Escapes and formats values for embedding into SQL queries  

Each implementation uses an injected variation generator for flexibility and testability.

---

# ParameterizedSqlGenerator

Generates **parameterized SQL queries** for one or more names.

## Implementation

- Expands each name into all possible umlaut variations
- Assigns each variation a unique SQL parameter (`@p0`, `@p1`, …)
- Returns `SqlQuery` objects containing the SQL string and parameters dictionary

## Complexity
If you have m names to process, the total work done by a Step 3 generator is roughly:  
```
O(m × V(n)) = O(m × 2^n)
```
Where:

- m = number of input names  
- n = average number of variations per name
- V(n) = number of variations generated

## Advantages

- Safe from SQL injection
- Can combine all names into a single query or generate individual queries
- Integrates with Step 2 variation generators

## Disadvantages

- Slight overhead due to parameter dictionary construction

## Best Use Case

- Executing queries against databases with names containing umlauts using **parameterized SQL**

---

# PlainSqlGenerator

Generates **plain SQL queries** with formatted values embedded.

## Implementation

- Expands each name into all possible variations
- Formats each variation using an injected SQL value formatter
- Returns SQL strings ready to execute

## Complexity

If you have m names to process, the total work done by a Step 3 generator is roughly:  
```
O(m × V(n)) = O(m × 2^n)
```
Where:

- m = number of input names  
- n = average number of variations per name
- V(n) = number of variations generated

## Advantages

- Easy to read SQL, good for debugging or ad-hoc execution

## Disadvantages

- Slightly less secure than parameterized queries
- Repeated strings may increase memory usage for large input sets

## Best Use Case

- Quick queries for testing or logging
- Scenarios where parameterization is not required

---

# StandardSqlValueFormatter

Formats SQL values safely for embedding into queries.

## Implementation

- Escapes single quotes by doubling them
- Wraps the value in single quotes
- Converts `null` to `NULL`

## Complexity

O(n)

Where n = length of the input string

## Advantages

- Prevents syntax errors in SQL
- Simple, reusable

## Disadvantages

- Does not replace parameterized queries in terms of security

## Best Use Case

- Embedding string values safely in **plain SQL statements**

---

# Example Usage

```csharp
using Samwin.UmlautConverterLib.Step3;
using System.Data.SqlClient;

// Names to search
var names = new[] { "RUESSWURM", "KOESTNER" };

// Parameterized SQL
var generator = new ParameterizedSqlGenerator();
foreach (var query in generator.Generate(names))
{
    Console.WriteLine(query.Sql);
    foreach (var kv in query.Parameters)
        Console.WriteLine($"{kv.Key} = {kv.Value}");
}

// Example: Using parameterized query with ADO.NET
var singleQuery = generator.Generate(new[] { "RUESSWURM" }).First();

using var connection = new SqlConnection("your_connection_string");
using var cmd = new SqlCommand(singleQuery.Sql, connection);

foreach (var p in singleQuery.Parameters)
{
    cmd.Parameters.AddWithValue(p.Key, p.Value);
}
```
# Output Sample
```
Combined parameterised SQL:
SELECT * FROM tbl_phonebook WHERE last_name IN (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12, @p13, @p14, @p15, @p16, @p17, @p18, @p19, @p20, @p21);  

Individual parameterised SQLs:  
SELECT * FROM tbl_phonebook WHERE last_name IN (@p0, @p1);  
SELECT * FROM tbl_phonebook WHERE last_name IN (@p0, @p1, @p2, @p3);  
SELECT * FROM tbl_phonebook WHERE last_name IN (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7);  
SELECT * FROM tbl_phonebook WHERE last_name IN (@p0, @p1, @p2, @p3);  

Combined plain SQL:  
SELECT * FROM tbl_phonebook WHERE last_name IN ('KOESTNER', 'KÖSTNER', 'RUESSWURM', 'RUEßWURM', 'RÜSSWURM', 'RÜßWURM', 'DUERMUELLER', 'DUERMÜLLER', 'DÜRMUELLER', 'DÜRMÜLLER', 'JAEAESKELAEINEN', 'JAEAESKELÄINEN', 'JAEÄSKELAEINEN', 'JAEÄSKELÄINEN', 'JÄAESKELAEINEN', 'JÄAESKELÄINEN', 'JÄÄSKELAEINEN', 'JÄÄSKELÄINEN', 'GROSSSCHAEDL', 'GROSSSCHÄDL', 'GROßSCHAEDL', 'GROßSCHÄDL');  

Individual plain  SQLs:
SELECT * FROM tbl_phonebook WHERE last_name IN ('KOESTNER', 'KÖSTNER');  
SELECT * FROM tbl_phonebook WHERE last_name IN ('RUESSWURM', 'RUEßWURM', 'RÜSSWURM', 'RÜßWURM');  
SELECT * FROM tbl_phonebook WHERE last_name IN ('DUERMUELLER', 'DUERMÜLLER', 'DÜRMUELLER', 'DÜRMÜLLER');  
SELECT * FROM tbl_phonebook WHERE last_name IN ('JAEAESKELAEINEN', 'JAEAESKELÄINEN', 'JAEÄSKELAEINEN', 'JAEÄSKELÄINEN', 'JÄAESKELAEINEN', 'JÄAESKELÄINEN', 'JÄÄSKELAEINEN', 'JÄÄSKELÄINEN');  
SELECT * FROM tbl_phonebook WHERE last_name IN ('GROSSSCHAEDL', 'GROSSSCHÄDL', 'GROßSCHAEDL', 'GROßSCHÄDL');  

```
---

# Design Goals

- Safe and efficient generation of SQL queries for names containing umlauts  
- Flexible integration with Step 2 variation generators  
- Support for both **parameterized** and **plain SQL**  
- Modular, testable architecture  

---
# Notes on Performance

- Step 3 implementations are straightforward and mainly wrap the variation generator output into SQL queries.
- No dedicated performance benchmarks are provided for Step 3.
- Most computational complexity depends on the Step 2 variation generators, which already have been benchmarked in Step 2.
- Step 3 is designed for clarity, safety, and ease of integration rather than performance optimization.

---
# Future Improvements

- Batch SQL generation across multiple tables  
- Integration with ORMs (Entity Framework, Dapper)  
- AI-assisted query suggestions for incomplete or ambiguous names  


# Integration & Enterprise Perspective

- **Backend-Frontend Ready**: SQL query generators can be used directly in web APIs or service layers.
- **Safe & Modular**: Parameterized SQL ensures security; plain SQL variants support logging and debugging.
- **Prepared for Scaling**: Modular design allows integration with batch pipelines, reactive processing, or actor-based services.
- **AI Integration Ready**: Future AI-assisted query suggestion can be implemented without altering core functionality.