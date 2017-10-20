It was built with ASP.NET MVC using Rotativa PDF library.

The main idea here is simple.
We have a bunch of stored procedures that collect a lot of data (>30). Most of the time, a structure of one table is pretty simple:
- name of the table;
- table header that itself can be a table;
- a bunch of rows and columns;
- there can be plenty of pictures;
- one report can contain several tables whose data is different.

So the question is - how can we avoid having to make views for each report?

Here's the solution.

In each stored procedure we define what everything is:
- header;
- values;
- column names;
- everything combined.

One view is responsible for displaying everything here.

We can also determine many things based on who a customer is:
- logo;
- footer;
- size format;
- how many columns per page max;
- etc.
