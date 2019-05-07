# ClassPlanner
C# Application that manages a student's classes and information using a local SQL database

I wanted to practice working with a SQL database using C# and Visual Studio. I wanted to create an alternative to my university's degree
works program.

This is is a windows forms application that uses a local SQL database, I used Microsoft's database for development, as long as you have a
master database on a local SQL server this will run.

The application allows you to import a list of your full coursework for your degree using a simple text file. Each line
of the textfile must be formated like so:

Classname CreditHours
 
 or

Classname CreditHours Prerequisite

The application will import the textfile and insert all classes into the sql table
The application has two main data grids: Classes available to take and Classes taken

The user may complete a class from the available grid and will then be prompted to enter the grade achieved(GPA is shown under Classes Taken). The user may also remove classes and place them back in the available catagory.

A window can be opened displaying all classes which the student does not meet the prerequisite for.
